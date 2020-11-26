using System;
using DotNetAPIScanner.Scanning;

namespace DotNetAPIScanner.Scan {
	class Command: ScanCommand {
		#region overrides

		protected override string GetCommandName() {
			return "dotnet scan.dll";
		}

		#endregion
	}
}
