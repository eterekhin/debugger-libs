using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Mono.Debugger.Soft;

namespace Driver
{
	internal class Program
	{
		private const string PathToMono = @"C:\Program Files\Mono\bin\mono.exe";
		private const string PathToExe = @"..\..\ProjectToLaunch.exe";
		// If you want to change debuggee project just uncomment the constant below and make sure that ProjectToLaunch is built for debug configuration
		// private const string PathToExe = @"..\..\..\ProjectToLaunch\bin\Debug\ProjectToLaunch.exe";

		public static async Task Main (string[] args)
		{
			var d = Directory.GetCurrentDirectory ();
			var tcs = new TaskCompletionSource<VirtualMachine> ();
			VirtualMachineManager.BeginLaunch (new ProcessStartInfo (PathToMono) { Arguments = PathToExe },
				machine => tcs.SetResult (((Task<VirtualMachine>)machine).Result));
			var vm = await tcs.Task;

			vm.EnableEvents (EventType.AssemblyLoad, EventType.UserBreak);
			SubscribeToEvents (vm);

			Console.ReadKey ();
		}

		private static void SubscribeToEvents (VirtualMachine vm)
		{
			Task.Run (() => {
				var eventCounter = 0;
				ThreadMirror threadMirror = null;

				while (true) {
					// It's noticed that for "ProjectToLaunch" application after 2 events processed we start waiting for events, so instead of the waiting let's start an evaluation
					if (eventCounter == 2) {
						HandleUserBreak (threadMirror, vm);
					}

					var set = vm.GetNextEventSet ();
					eventCounter++;
					var shouldContinue = true;
					foreach (var @event in set.Events) {
						switch (@event.EventType) {
						case EventType.UserBreak:
							threadMirror = @event.Thread;
							TopFrameCheck (@event, vm);
							shouldContinue = false;
							break;
						case EventType.VMStart:
						case EventType.VMDeath:
						case EventType.ThreadStart:
						case EventType.ThreadDeath:
						case EventType.AppDomainCreate:
						case EventType.MethodEntry:
						case EventType.MethodExit:
						case EventType.AssemblyLoad:
						case EventType.AssemblyUnload:
						case EventType.Breakpoint:
						case EventType.Step:
						case EventType.TypeLoad:
						case EventType.Exception:
						case EventType.KeepAlive:
						case EventType.UserLog:
						case EventType.Crash:
						case EventType.EnCUpdate:
						case EventType.MethodUpdate:
						case EventType.VMDisconnect:
						case EventType.AppDomainUnload:
						default:
							threadMirror = @event.Thread;
							shouldContinue = true;
							break;
						}
					}

					if (shouldContinue)
						vm.Resume ();
				}
			});
		}

		private static void HandleUserBreak (ThreadMirror threadMirror, VirtualMachine vm)
		{
			Task.Run (async () => {
				// emulating a real situation, an user decides to evaluate the method after 1 sec the debug session is stopped
				await Task.Delay (1000);
				vm.Suspend ();
				var types = vm.GetTypes ("ProjectToLaunch.Program", true);
				var programType = types.First ();
				var method = programType.GetMethods ().Single (x => x.Name.Equals ("EvaluationWithUserBreakInside"));
				programType.BeginInvokeMethod (threadMirror, method, Array.Empty<Value> (), InvokeOptions.None,
					s => { }, null);
			});
		}

		private static void TopFrameCheck (Event @event, VirtualMachine vm)
		{
			var stackFrames = @event.Thread.GetFrames ();
			var stackFrameNames = stackFrames.Select (x => x.Method.FullName).ToArray ();
			// topFrameName is expected to be "Program.EvaluationWithUserBreakInside" but it is not always true for the newest protocol version (2.56). works for 2.51
			var topFrameName = stackFrameNames.FirstOrDefault ();
		}
	}
}