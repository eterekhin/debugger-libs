// 
// BreakEventInfo.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2011 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;

namespace Mono.Debugging.Client
{
	/// <summary>
	/// This class can be used to manage and get information about a breakpoint
	/// at debug-time. It is intended to be used by DebuggerSession subclasses.
	/// </summary>
	public class BreakEventInfo
	{
		DebuggerSession session;
		int adjustedLine = -1;
		
		/// <summary>
		/// Break event that this instance represents
		/// </summary>
		public BreakEvent BreakEvent { get; internal set; }

		/// <summary>
		/// Gets the status of the break event
		/// </summary>
		public BreakEventStatus Status { get; private set; }

		/// <summary>
		/// Gets a description of the status
		/// </summary>
		public string StatusMessage { get; private set; }
		
		internal void AttachSession (DebuggerSession s, BreakEvent ev)
		{
			session = s;
			BreakEvent = ev;
			session.Breakpoints.NotifyStatusChanged (BreakEvent);
			if (adjustedLine != -1)
				session.AdjustBreakpointLocation ((Breakpoint)BreakEvent, adjustedLine);
		}

		/// <summary>
		/// Adjusts the location of a breakpoint
		/// </summary>
		/// <param name='newLine'>
		/// New line.
		/// </param>
		/// <remarks>
		/// This method can be used to temporarily change source code line of the breakpoint.
		/// This is useful, for example, when two adjacent lines are mapped to a single
		/// native offset. If the breakpoint is set to the first of those lines, the debugger
		/// might end stopping in the second line, because it has the same native offset.
		/// To avoid this confusion situation, the debugger implementation may decide to
		/// adjust the position of the breakpoint, and move it to the second line.
		/// This line adjustment has effect only during the debug session, and is automatically
		/// reset when it terminates.
		/// </remarks>
		public void AdjustBreakpointLocation (int newLine)
		{
			if (session != null)
				session.AdjustBreakpointLocation ((Breakpoint)BreakEvent, newLine);
			else
				adjustedLine = newLine;
		}
		
		public void UpdateHitCount (int count)
		{
			BreakEvent.HitCount = count;
			BreakEvent.NotifyUpdate ();
		}
		
		public void SetStatus (BreakEventStatus s, string statusMessage)
		{
			Status = s;
			StatusMessage = statusMessage;
			if (session != null)
				session.Breakpoints.NotifyStatusChanged (BreakEvent);
		}
		
		public bool RunCustomBreakpointAction (string actionId)
		{
			BreakEventHitHandler h = session.CustomBreakEventHitHandler;
			return h != null && h (actionId, BreakEvent);
		}
		
		public void UpdateLastTraceValue (string value)
		{
			BreakEvent.LastTraceValue = value;
			BreakEvent.NotifyUpdate ();
			if (value != null) {
				if (session.BreakpointTraceHandler != null)
					session.BreakpointTraceHandler (BreakEvent, value);
				else
					session.OnDebuggerOutput (false, value + "\n");
			}
		}
	}
}
