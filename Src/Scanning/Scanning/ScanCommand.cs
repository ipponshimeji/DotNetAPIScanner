using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Zafu.ReflectionScanning;


namespace DotNetAPIScanner.Scanner {
	public class ScanCommand {
		public static void Run(string[] args) {
			Assembly[] assemblies = new Assembly[] {
				Assembly.Load("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")
//				Assembly.Load("System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")
			};
			IReflectionFilter filter = Filter.Instance;

			using (Reporter reporter = new ReporterInJson(Console.Out, disposeWriterOnDispose: false)) {
				Scan(filter, reporter, assemblies);
			}
		}

		public static void Scan(IReflectionFilter filter, IReflectionHandler handler, IReadOnlyCollection<Assembly> assemblies) {
			// check arguments
			Debug.Assert(filter != null);
			Debug.Assert(handler != null);
			Debug.Assert(assemblies != null);

			// scan assemblies
			ReflectionScanner scanner = new ReflectionScanner(filter);
			scanner.AddHandler(handler);
			try {
				scanner.ScanAssemblies(assemblies);
			} finally {
				scanner.RemoveHandler(handler);
			}
		}
	}
}
