using System;
using DotNetAPIScanner.Checker;


namespace DotNetAPIScanner.Check {
	class Program {
		static int Main(string[] args) {
			return new Command().Run(args);
		}
	}
}
