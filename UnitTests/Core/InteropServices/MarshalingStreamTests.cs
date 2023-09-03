﻿#pragma warning disable CS0618
using NUnit.Framework;
using System.IO;

namespace Vanara.InteropServices.Tests;

[TestFixture()]
    public class MarshalingStreamTests
    {
        [Test()]
        public void MarshalingStreamTest()
        {
            using SafeHGlobalHandle m = new(1024);
            using MarshalingStream ms = new(m, m.Size);
            Assert.That(ms.Capacity, Is.EqualTo((long)m.Size));
        }

        [Test()]
        public void FlushTest()
        {
            using SafeHGlobalHandle m = new(1000);
            using MarshalingStream ms = new(m, m.Size);
            ms.Flush();
        }

        [Test()]
        public void SeekTest()
        {
            using SafeHGlobalHandle m = new(1000);
            using MarshalingStream ms = new(m, m.Size);
            Assert.That(ms.Seek(20, SeekOrigin.Begin), Is.EqualTo(20));
            Assert.That(ms.Seek(20, SeekOrigin.Current), Is.EqualTo(40));
            Assert.That(ms.Seek(-100, SeekOrigin.End), Is.EqualTo(900));
            Assert.That(() => ms.Seek(-1, SeekOrigin.Begin), Throws.ArgumentException);
            Assert.That(() => ms.Seek(1, SeekOrigin.End), Throws.ArgumentException);
        }

        [Test()]
        public void SetLengthTest()
        {
            using SafeHGlobalHandle m = new(1000);
            using MarshalingStream ms = new(m, m.Size);
            Assert.That(() => ms.SetLength(1), Throws.Exception);
        }

        [Test()]
        public void PokeTest()
        {
            using SafeHGlobalHandle m = new(10);
            using MarshalingStream ms = new(m, m.Size);
            Assert.That(() => ms.Write(0x000001FF), Throws.Nothing);
            Assert.That(ms.Position, Is.EqualTo(sizeof(int)));
            ms.Seek(0, SeekOrigin.Begin);
            byte[] ba = new byte[] { 0x2 };
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.That(() => ms.Poke(null, 0), Throws.ArgumentNullException);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.That(() => ms.Poke(ba, 1000), Throws.ArgumentException);
            Assert.That(() => ms.Poke(ba, -1), Throws.TypeOf<ArgumentOutOfRangeException>());
            ms.Poke(ba, 1);
            Assert.That(ms.Read<int>(), Is.EqualTo(0x00000102));
            Assert.That(ms.Read<ulong>, Throws.TypeOf<InsufficientMemoryException>());
        }

        [Test()]
        public void PokeTest1()
        {
            using SafeHGlobalHandle m = new(100);
            using MarshalingStream ms = new(m, m.Size);
            Assert.That(ms.Position, Is.Zero);
            Assert.That(() => ms.Write(new[] { 1L, 2L }), Throws.Nothing);
            byte[] bytes = new byte[] { 0, 0, 0, 0, 0, 0, 0, 3 };
            ms.Write(bytes, 0, bytes.Length);
            Assert.That(ms.Position, Is.EqualTo(sizeof(long) * 2 + 8));
            ms.Seek(0, SeekOrigin.Begin);
            Assert.That(() => ms.Poke(IntPtr.Zero, 1002), Throws.ArgumentException);
            Assert.That(() => ms.Poke(IntPtr.Zero, -1), Throws.TypeOf<ArgumentOutOfRangeException>());
            ms.Poke(IntPtr.Zero, sizeof(long));
            byte[] buf = new byte[24];
            ms.Read(buf, 0, buf.Length);
            Assert.That(buf, Is.EquivalentTo(new byte[] { 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 3 }));
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.That(() => ms.Read(null, 0, 0), Throws.ArgumentNullException);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.That(() => ms.Read(buf, 0, 30), Throws.ArgumentException);
            Assert.That(() => ms.Read(buf, -1, 0), Throws.TypeOf<ArgumentOutOfRangeException>());
            ms.Position = m.Size - 10;
            Assert.That(() => ms.Read(buf, 0, buf.Length), Throws.Nothing);
        }

        [Test]
        public void PropTest()
        {
            using SafeHGlobalHandle m = new(1000);
            using MarshalingStream ms = new(m, m.Size);
            Assert.That(ms.Length, Is.EqualTo(1000));
            Assert.That(ms.CanWrite, Is.True);
            Assert.That(ms.CanSeek, Is.True);
            Assert.That(ms.CanRead, Is.True);
        }

        [Test]
        public void WriteTest()
        {
            using (SafeHGlobalHandle m = new(10))
            using (MarshalingStream ms = new(m, m.Size))
            {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
				Assert.That(() => ms.Write(null, 0, 0), Throws.ArgumentNullException);
				byte[] bytes = new byte[] { 0, 0, 0, 0, 0, 0, 0, 3 };
                Assert.That(() => ms.Write(bytes, 1, 8), Throws.ArgumentException);
                Assert.That(() => ms.Write(bytes, -1, 8), Throws.TypeOf<ArgumentOutOfRangeException>());
                Assert.That(() => ms.Write(bytes, 1, -8), Throws.TypeOf<ArgumentOutOfRangeException>());
                Assert.That(() => ms.Write(new byte[22]), Throws.TypeOf<InsufficientMemoryException>());
                ms.Write((SafeHGlobalHandle?)null);
                Assert.That(ms.Position, Is.Zero);
                ms.Write((string[]?)null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
                Assert.That(ms.Position, Is.Zero);
                Assert.That(() => ms.Write(0L), Throws.Nothing);
            }
            using (SafeHGlobalHandle m = new(100))
            using (MarshalingStream ms = new(m, m.Size))
            {
                ms.Write(new[] { "A", "B", "C" });
                Assert.That(ms.Position, Is.GreaterThan(0));
                Assert.That(() => ms.Write(new byte[100], 0, 100), Throws.ArgumentException);
            }
        }
    }