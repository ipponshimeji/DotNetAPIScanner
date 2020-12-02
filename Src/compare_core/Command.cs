using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Utf8Json;
using DotNetAPIScanner.Comparing;

namespace DotNetAPIScanner.Compare {
	class Command: CompareCommand {
		#region overrides

		// GetSourceInfo is implemented here because it seems that a .NET Framework app
		// which uses UTF8Json does not work on .NET Core runtime.
		// So this command links UTF8Json with .NET Core runtime.
		protected override IReadOnlyDictionary<string, object> GetSourceInfo(string inputFilePath, Encoding inputEncoding) {
			// check arguments
			// inputFilePath can be null
			// adjust the encoding
			if (inputEncoding == null) {
				inputEncoding = string.IsNullOrEmpty(InputFilePath) ? Console.InputEncoding : Encoding.UTF8;
			}
			if (inputEncoding != Encoding.UTF8) {
				throw new ArgumentException("Only UTF-8 is supported.", nameof(inputEncoding));
			}

			static IReadOnlyDictionary<string, object> getSourceInfo(Stream stream, Encoding encoding) {
				return JsonSerializer.Deserialize<Dictionary<string, object>>(stream);
			}
			return OpenInputStreamAndGetSourceInfo(inputFilePath, inputEncoding, getSourceInfo);
		}

		protected override string GetCommandName() {
			return "dotnet compare.dll";
		}

		#endregion
	}
}
