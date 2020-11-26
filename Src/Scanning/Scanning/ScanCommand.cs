using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using DotNetAPIScanner.ReflectionScanning;

namespace DotNetAPIScanner.Scanning {
	public abstract class ScanCommand: TextOutputCommand {
		#region types

		public new class TaskKinds: Command.TaskKinds {
			#region constants

			public const string Scan = "scan";

			#endregion
		}

		public class FormatNames {
			#region constants

			public const string Json = "json";

			public const string Text = "text";

			#endregion
		}

		#endregion


		#region data - arguments

		private List<Assembly> assemblies = new List<Assembly>();

		private string outputFormat = null;

		#endregion


		#region properties

		public IReadOnlyList<Assembly> Assemblies {
			get {
				return this.assemblies;
			}
		}

		public string OutputFormat {
			get {
				return this.outputFormat;
			}
			set {
				SetCommandArgumentProperty<string>(ref this.outputFormat, value);
			}
		}

		#endregion


		#region methods

		public static void Scan(IReflectionFilter filter, IReflectionHandler handler, IReadOnlyCollection<Assembly> assemblies) {
			// check arguments
			if (filter == null) {
				throw new ArgumentNullException(nameof(filter));
			}
			if (handler == null) {
				throw new ArgumentNullException(nameof(handler));
			}
			if (assemblies == null) {
				throw new ArgumentNullException(nameof(assemblies));
			}

			// scan assemblies
			new ReflectionScanner(filter).ScanAssemblies(handler, assemblies);
		}

		#endregion


		#region overrides

		protected override void HandleOption(string arg, IEnumerator<string> argEnumerator) {
			// check argument
			Debug.Assert(arg != null);

			switch (arg) {
				case "-of":
				case "--output-format":
					this.OutputFormat = GetOptionValue(arg, argEnumerator);
					break;
				default:
					base.HandleOption(arg, argEnumerator);
					break;
			}
		}

		protected override void HandleNormalArgument(string arg, IEnumerator<string> argEnumerator) {
			// ex. arg = "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"
			Assembly assembly = Assembly.Load(arg);	// will throw exception if the assembly is not found
			this.assemblies.Add(assembly);
		}

		protected override string OnExecuting() {
			// prepare the base class level
			string taskKind = base.OnExecuting();

			// prepare this class level
			if (taskKind == null) {
				if (0 < this.assemblies.Count) {
					// actually no preparation required
					taskKind = TaskKinds.Scan;
				} else {
					taskKind = TaskKinds.Help;
				}
			}

			return taskKind;
		}

		protected override int Execute(string taskKind) {
			switch (taskKind) {
				case TaskKinds.Scan:
					return OpenOutputWriterAndExecute(this.Scan);
				default:
					return base.Execute(taskKind);
			}
		}

		protected override void WriteHelpTo(TextWriter writer) {
			// check argument
			if (writer == null) {
				throw new ArgumentNullException(nameof(writer));
			}

			// write help message
			// Write each line using writer.WriteLine to end with appropriate new line char
			// in the current environment. (that is, do not use here document)
			//                12345678901234567890123456789012345678901234567890123456789012345678901234567890
			writer.WriteLine("Dump public interfaces of the specified assemblies");
			writer.WriteLine("in the current .NET environment.");
			writer.WriteLine("USAGE:");
			writer.WriteLine($"  {GetCommandName()} [OPTIONS] assemblyName ...");
			writer.WriteLine("OPTIONS:");
			writer.WriteLine("  -o, --output <file path>");
			writer.WriteLine("    The path of the output file.");
			writer.WriteLine("    The command outputs to the standard out if this option is not specified.");
			writer.WriteLine("  -of, --output-format <format>");
			writer.WriteLine("    The format of the output.");
			writer.WriteLine("    Currently only json or text are supported for <format>.");
			writer.WriteLine("    The default is json.");
			writer.WriteLine("  -oe, --output-encoding <encoding name>");
			writer.WriteLine("    The encoding of the output file.");
			writer.WriteLine("    If this option is not specified, the output encoding is:");
			writer.WriteLine("      UTF-8 if the output is file, or");
			writer.WriteLine("      encoding of Console.Out if the output is the standard output.");
		}

		#endregion


		#region overridables

		protected virtual int Scan(TextWriter writer) {
			// check argument
			if (writer == null) {
				throw new ArgumentNullException(nameof(writer));
			}

			using (Reporter reporter = GetReporter(this.OutputFormat, writer)) {
				Scan(Filter.Instance, reporter, this.Assemblies);
			}

			return 0;
		}

		protected virtual Reporter GetReporter(string outputFormat, TextWriter writer) {
			// check argument
			if (outputFormat == null) {
				outputFormat = FormatNames.Json;
			}
			if (writer == null) {
				throw new ArgumentNullException(nameof(writer));
			}

			switch (outputFormat) {
				case FormatNames.Json:
					return new ReporterInJson(writer, disposeWriterOnDispose: false);
				case FormatNames.Text:
					return new ReporterInIndentedText(writer, disposeWriterOnDispose: false);
				default:
					throw new Exception($"Unrecognized output format: {outputFormat}");
			}
		}

		#endregion
	}
}
