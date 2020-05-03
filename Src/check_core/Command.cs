using System;
using System.Collections.Generic;
using System.IO;
using Utf8Json;
using DotNetAPIScanner.Checker;


namespace DotNetAPIScanner.Check {
	class Command: CheckCommand {
		#region initialization & disposal

		public Command(TextWriter writer, bool disposeWriterOnDispose) : base(writer, disposeWriterOnDispose) {
		}

		#endregion


		#region methods

		public static int Run(string[] args) {
			// check argument
			if (args.Length < 1) {
				Console.Error.WriteLine("USAGE: check sourceJsonFile");
			}

			int exitCode;
			try {
				using (Command command = new Command(Console.Out, disposeWriterOnDispose: false)) {
					exitCode = command.Check(args[0], sort: true);
				}
			} catch (Exception exception) {
				Console.WriteLine(exception.Message);
				exitCode = -1;
			}

			return exitCode;
		}

		public int Check(string jsonFilePath, bool sort) {
			// check arguments
			if (jsonFilePath == null) {
				throw new ArgumentNullException(nameof(jsonFilePath));
			}

			using (FileStream jsonStream = File.OpenRead(jsonFilePath)) {
				return Check(jsonStream, sort);
			}
		}

		public int Check(Stream jsonStream, bool sort) {
			// check arguments
			if (jsonStream == null) {
				throw new ArgumentNullException(nameof(jsonStream));
			}

			// load source json to be checked
			Dictionary<string, object> source = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonStream);
			return Check(source, sort);
		}

		#endregion
	}
}
