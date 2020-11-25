using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using DotNetAPIScanner.ReflectionScanning;

namespace DotNetAPIScanner.Scanning {
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
			new ReflectionScanner(filter).ScanAssemblies(handler, assemblies);
		}
	}
}
