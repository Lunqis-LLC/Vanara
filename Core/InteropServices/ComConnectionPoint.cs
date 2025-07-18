﻿using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;

namespace Vanara.InteropServices;

/// <summary>
/// Helper class to create an advised COM sink. When this class is constructed, the source is queried for an <see
/// cref="IConnectionPointContainer"/> reference.
/// </summary>
public class ComConnectionPoint : IDisposable
{
	/// <summary>List of connection points.</summary>
	protected readonly Dictionary<Guid, (IConnectionPoint, int)> connectionPoints = [];

	/// <summary>Initializes a new instance of the <see cref="ComConnectionPoint"/> class.</summary>
	/// <param name="source">The COM object from which to query the <see cref="IConnectionPointContainer"/> reference.</param>
	/// <param name="sink">The object which implements the COM interface or interfaces signaled by an event.</param>
	/// <param name="interfaces">The interfaces supported by <paramref name="source"/> that support events.</param>
	public ComConnectionPoint(object source, object? sink, params Type[]? interfaces)
	{
		if (source is not IConnectionPointContainer connectionPointContainer)
			throw new ArgumentException("The source object must be COM object that supports the IConnectionPointContainer interface.", nameof(source));
		Sink = sink ?? this;
		interfaces ??= GetComInterfaces(Sink);
		if (interfaces is null || interfaces.Length < 1) throw new ArgumentOutOfRangeException(nameof(interfaces));

		// Start the event sink
		foreach (var i in interfaces)
		{
			var comappEventsInterfaceId = i.GUID;
			connectionPointContainer.FindConnectionPoint(ref comappEventsInterfaceId, out var connectionPoint);
			if (connectionPoint != null)
			{
				connectionPoint.Advise(Sink, out var cookie);
				connectionPoints.Add(comappEventsInterfaceId, (connectionPoint, cookie));
			}
		}
	}

	/// <summary>Initializes a new instance of the <see cref="ComConnectionPoint"/> class.</summary>
	/// <param name="source">The COM object from which to query the <see cref="IConnectionPointContainer"/> reference.</param>
	/// <param name="sink">
	/// The object which implements the COM interface or interfaces signaled by an event. All COM interfaces implemented by this object will
	/// be used to setup the connection points. If this object implements COM interfaces that cannot be used as connection points, use the
	/// constructor that allows for supported interfaces to be specified.
	/// </param>
	public ComConnectionPoint(object source, object? sink) : this(source, sink, null) { }

	/// <summary>Gets the sink.</summary>
	/// <value>The sink.</value>
	public object Sink { get; protected set; }

	/// <summary>Releases unmanaged and - optionally - managed resources.</summary>
	public void Dispose()
	{
		foreach (var pair in connectionPoints.Values)
		{
			//unhook the event sink
			pair.Item1.Unadvise(pair.Item2);
		}
		connectionPoints.Clear();
		GC.SuppressFinalize(this);
	}

	/// <summary>Attempts to retrieve the connection point and associated cookie for the specified IID.</summary>
	/// <param name="key">The IID of the connection point to retrieve.</param>
	/// <param name="value">
	/// When this method returns, contains a tuple with the connection point and its associated cookie if the key exists; otherwise, the
	/// default value for the tuple.
	/// </param>
	/// <returns><see langword="true"/> if the key exists and the value was successfully retrieved; otherwise, <see langword="false"/>.</returns>
	public virtual bool TryGetValue(Guid key, out (IConnectionPoint icp, int cookie) value) => connectionPoints.TryGetValue(key, out value);

	private static Type[]? GetComInterfaces(object? sink) => sink?.GetType().GetInterfaces().
		Where(i => i.GetCustomAttributes(true).Any(a => a.GetType() == typeof(ComImportAttribute) || a.GetType() == typeof(InterfaceTypeAttribute))).
		ToArray();
}