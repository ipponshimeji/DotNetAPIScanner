using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Utf8Json;
using DotNetAPIScanner.Comparing;

namespace DotNetAPIScanner.Compare {
	class Command: CheckCommand {
		#region overrides

		// GetSourceInfo is implemented here because it seems that a .NET Framework app
		// which uses UTF8Json does not work on .NET Core runtime.
		// So this command links UTF8Json with .NET Core runtime.
		protected override IReadOnlyDictionary<string, object> GetSourceInfo(string inputFilePath, Encoding encoding) {
			// check arguments
			// inputFilePath can be null
			if (encoding != null && encoding != Encoding.UTF8) {
				throw new ArgumentException("Only UTF-8 is supported.", nameof(encoding));
			}

			if (string.IsNullOrEmpty(inputFilePath)) {
				// load from the standard input
				using (Stream stream = Console.OpenStandardInput()) {
					return JsonSerializer.Deserialize<Dictionary<string, object>>(stream);
				}
			} else {
				// load from file
				using (FileStream stream = File.OpenRead(inputFilePath)) {
					return JsonSerializer.Deserialize<Dictionary<string, object>>(stream);
				}
			}
		}

		#endregion
	}
}
