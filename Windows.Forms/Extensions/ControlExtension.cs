﻿using System.Linq;
using System.Windows.Forms;
using static Vanara.PInvoke.User32;

namespace Vanara.Extensions;

/// <summary>Control extension methods.</summary>
public static partial class ControlExtension
{
	/// <summary>
	/// Performs an action on a control after its handle has been created. If the control's handle has already been created, the action is executed immediately.
	/// </summary>
	/// <param name="ctrl">This control.</param>
	/// <param name="action">The action to execute.</param>
	public static void CallWhenHandleValid(this Control ctrl, Action<Control> action)
	{
		if (ctrl.IsHandleCreated)
		{
			action(ctrl);
		}
		else if (!ctrl.IsDesignMode())
		{
			void handler(object? sender, EventArgs e)
			{
				if (!ctrl.IsHandleCreated) return;
				ctrl.HandleCreated -= handler;
				action(ctrl);
			}

			ctrl.HandleCreated += handler;
		}
	}

	/// <summary>Enables all children of a control.</summary>
	/// <param name="ctl">This control.</param>
	/// <param name="enabled">If set to <c>true</c> enable all children, otherwise disable all children.</param>
	public static void EnableChildren(this Control ctl, bool enabled)
	{
		foreach (Control sub in ctl.Controls.Cast<Control>())
		{
			if (sub is ButtonBase or ListControl or TextBoxBase)
				sub.Enabled = enabled;
			sub.EnableChildren(enabled);
		}
	}

	/// <summary>Gets the control in the list of parents of type <c>T</c>.</summary>
	/// <typeparam name="T">The <see cref="Control"/> based <see cref="Type"/> of the parent control to retrieve.</typeparam>
	/// <param name="ctrl">This control.</param>
	/// <returns>The parent control matching T or null if not found.</returns>
	public static T? GetParent<T>(this Control ctrl) where T : class
	{
		var p = ctrl.Parent;
		while (p is not null and not T)
			p = p.Parent;
		return p as T;
	}

	/// <summary>Gets the top-most control in the list of parents of type <c>T</c>.</summary>
	/// <typeparam name="T">The <see cref="Control"/> based <see cref="Type"/> of the parent control to retrieve.</typeparam>
	/// <param name="ctrl">This control.</param>
	/// <returns>The top-most parent control matching T or null if not found.</returns>
	public static T? GetTopMostParent<T>(this Control ctrl) where T : class
	{
		var stack = new System.Collections.Generic.Stack<Control>();
		var p = ctrl.Parent;
		while (p != null)
		{
			stack.Push(p);
			p = p.Parent;
		}
		while (stack.Count > 0)
			if ((p = stack.Pop()) is T)
				return p as T;
		return null;
	}

	/// <summary>Gets the right to left property.</summary>
	/// <param name="ctrl">This control.</param>
	/// <returns>Culture defined direction of text for this control.</returns>
	public static RightToLeft GetRightToLeftProperty(this Control ctrl)
	{
		while (ctrl != null)
		{
			if (ctrl.RightToLeft != RightToLeft.Inherit)
				return ctrl.RightToLeft;
			ctrl = ctrl.Parent;
		}
		return RightToLeft.No;
	}

	/// <summary>Determines whether this control is in design mode.</summary>
	/// <param name="ctrl">This control.</param>
	/// <returns><c>true</c> if in design mode; otherwise, <c>false</c>.</returns>
	public static bool IsDesignMode(this Control ctrl)
	{
		var p = ctrl;
		while (p != null)
		{
			var site = p.Site;
			if (site != null && site.DesignMode)
				return true;
			p = p.Parent;
		}
		return false;
	}

	/// <summary>
	/// Gets a string using a message pattern that asks for the string length by sending a GetXXXLen message and then a GetXXXText message.
	/// </summary>
	/// <param name="ctrl">The control.</param>
	/// <param name="getLenMsg">The window message identifier for retrieving the string length.</param>
	/// <param name="getTextMsg">The window message identifier for retrieving the string.</param>
	/// <returns>The string result from the message call.</returns>
	public static string? GetMessageString(this Control ctrl, uint getLenMsg, uint getTextMsg)
	{
		if (!ctrl.IsHandleCreated) return null;
		var cp = ctrl.SendMessage(getLenMsg).ToInt32() + 1;
		var sb = new StringBuilder(cp);
		PInvoke.User32.SendMessage(ctrl.Handle, getTextMsg, ref cp, sb);
		return sb.ToString();
	}

	/// <summary>Retrieves the window styles.</summary>
	/// <param name="ctrl">The control.</param>
	/// <returns>The window styles</returns>
	public static int GetStyle(this Control ctrl) => GetWindowLongAuto(ctrl.Handle, WindowLongFlags.GWL_STYLE).ToInt32();

	/// <summary>Removes the mnemonic, if one exists, from the string.</summary>
	/// <param name="str">The string.</param>
	/// <returns>A mnemonic free string.</returns>
	public static string? RemoveMnemonic(this string? str)
	{
		if (string.IsNullOrEmpty(str)) return str;
		for (int i = 0; i < str!.Length; i++)
			if (str[i] == '&')
				if (i < str.Length - 1 && str[i +  1] == '&')
					i++;
				else
					return str.Remove(i, 1);
		return str;
	}

	/// <summary>
	/// <para>
	/// Sends the specified message to a window or windows. The <c>SendMessage</c> function calls the window procedure for the specified window and does not
	/// return until the window procedure has processed the message.
	/// </para>
	/// <para>
	/// To send a message and return immediately, use the <c>SendMessageCallback</c> or <c>SendNotifyMessage</c> function. To post a message to a thread's
	/// message queue and return immediately, use the <c>PostMessage</c> or <c>PostThreadMessage</c> function.
	/// </para>
	/// </summary>
	/// <param name="wnd">
	/// <para>
	/// A window whose window procedure will receive the message.
	/// </para>
	/// <para>
	/// Message sending is subject to UIPI. The thread of a process can send messages only to message queues of threads in processes of lesser or equal
	/// integrity level.
	/// </para>
	/// </param>
	/// <param name="msg">The message to be sent.</param>
	/// <param name="wParam">Additional message-specific information.</param>
	/// <param name="lParam">Additional message-specific information.</param>
	/// <returns>The return value specifies the result of the message processing; it depends on the message sent.</returns>
	public static IntPtr SendMessage(this IWin32Window wnd, uint msg, IntPtr wParam = default, IntPtr lParam = default) =>
		wnd.Handle != IntPtr.Zero && wnd.Handle.ToInt32() != -1
			? PInvoke.User32.SendMessage(wnd.Handle, msg, wParam, lParam)
			: IntPtr.Zero;

	/// <summary>Sets the windows styles.</summary>
	/// <param name="ctrl">The control.</param>
	/// <param name="style">The style flags.</param>
	/// <param name="on">if set to <c>true</c> add the style, otherwise remove it.</param>
	public static void SetStyle(this Control ctrl, int style, bool on = true)
	{
		var href = ctrl.Handle;
		int oldstyle = GetWindowLongAuto(href, WindowLongFlags.GWL_STYLE).ToInt32();
		if ((oldstyle & style) != style && on)
			SetWindowLong(href, WindowLongFlags.GWL_STYLE, new IntPtr(oldstyle | style));
		else if ((oldstyle & style) == style && !on)
			SetWindowLong(href, WindowLongFlags.GWL_STYLE, new IntPtr(oldstyle & ~style));
		ctrl.Refresh();
	}

	/// <summary>Converts a <see cref="PInvoke.MSG"/> structure to a <see cref="Message"/> structure.</summary>
	/// <param name="msg">The MSG instance.</param>
	/// <returns>An equivalent <see cref="Message"/> structure.</returns>
	public static Message ToMessage(this PInvoke.MSG msg) => Message.Create((IntPtr)msg.hwnd, (int)msg.message, msg.wParam, msg.lParam);

	/// <summary>Converts a <see cref="Message"/> structure to a <see cref="PInvoke.MSG"/> structure.</summary>
	/// <param name="msg">The Message instance.</param>
	/// <returns>An equivalent <see cref="PInvoke.MSG"/> structure.</returns>
	public static PInvoke.MSG ToMSG(this Message msg) => new() { message = (uint)msg.Msg, hwnd = msg.HWnd, wParam = msg.WParam, lParam = msg.LParam };
}