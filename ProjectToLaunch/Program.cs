using System;
using System.Diagnostics;
using System.Linq;

namespace ProjectToLaunch
{
	internal class Program
	{
		public static void Main (string[] args)
		{
			while (true) {
				Console.WriteLine (1);
			}
		}

		public static void EvaluationWithUserBreakInside ()
		{
			Debugger.Break ();
		}
	}
}