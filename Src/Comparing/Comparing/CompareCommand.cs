using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace DotNetAPIScanner.Comparing {
	public abstract class CompareCommand: Command {
		#region types

		public new class TaskKinds: Command.TaskKinds {
			#region constants

			public const string Check = "check";

			#endregion
		}

		#endregion


		#region data - arguments

		private Encoding inputEncoding = null;

		private Encoding outputEncoding = null;

		private string inputFilePath = null;

		private string outputFilePath = null;

		#endregion


		#region data - state

		protected TextWriter Writer { get; private set; } = null;

		#endregion


		#region properties

		public Encoding InputEncoding {
			get {
				return this.inputEncoding;
			}
			set {
				SetCommandArgumentProperty<Encoding>(ref this.inputEncoding, value);
			}
		}

		public Encoding OutputEncoding {
			get {
				return this.outputEncoding;
			}
			set {
				SetCommandArgumentProperty<Encoding>(ref this.outputEncoding, value);
			}
		}

		public string InputFilePath {
			get {
				return this.inputFilePath;
			}
			set {
				SetCommandArgumentProperty<string>(ref this.inputFilePath, value);
			}
		}

		public string OutputFilePath {
			get {
				return this.outputFilePath;
			}
			set {
				SetCommandArgumentProperty<string>(ref this.outputFilePath, value);
			}
		}

		#endregion


		#region initialization & disposal

		public CompareCommand() : base() {
		}

		#endregion


		#region methods

		public int Check(IReadOnlyDictionary<string, object> source, bool sort) {
			// check arguments
			if (source == null) {
				throw new ArgumentNullException(nameof(source));
			}

			// check state
			TextWriter writer = this.Writer;
			if (writer == null) {
				throw new ObjectDisposedException(null);
			}

			// check the source
			Comparer checker = new Comparer();
			checker.Sort = sort;
			checker.Report += checker_Report;
			try {
				// write header line to output
				writer.Write("# ");
				writer.WriteLine(ComparingReport.GetHeaderLine());

				// check
				return checker.Check(source);
			} finally {
				checker.Report -= checker_Report;
			}
		}

		#endregion


		#region overrides

		protected override void HandleOption(string arg, IEnumerator<string> argEnumerator) {
			// check argument
			Debug.Assert(arg != null);

			switch (arg) {
				case "-i":
				case "--input":
					this.InputFilePath = GetOptionValue(arg, argEnumerator);
					break;
				case "-o":
				case "--output":
					this.OutputFilePath = GetOptionValue(arg, argEnumerator);
					break;
				case "-ie":
				case "--input-encoding":
					this.InputEncoding = Encoding.GetEncoding(GetOptionValue(arg, argEnumerator));
					break;
				case "-oe":
				case "--output-encoding":
					this.OutputEncoding = Encoding.GetEncoding(GetOptionValue(arg, argEnumerator));
					break;
				default:
					base.HandleOption(arg, argEnumerator);
					break;
			}
		}

		protected override string OnExecuting() {
			// prepare the base class level
			string taskKind = base.OnExecuting();

			// prepare this class level
			if (taskKind == null) {
				// actually no preparation required
				taskKind = TaskKinds.Check;
			}

			return taskKind;
		}

		protected override int Execute(string taskKind) {
			switch (taskKind) {
				case TaskKinds.Check:
					return Check();
				default:
					return base.Execute(taskKind);
			}
		}

		protected override void WriteHelpTo(TextWriter writer) {
			// check argument
			if (writer == null) {
				throw new ArgumentNullException(nameof(writer));
			}

			writer.WriteLine("");
			writer.WriteLine("USAGE:");
			writer.WriteLine($"  {GetCommandName()} [OPTIONS]");
			writer.WriteLine("OPTIONS:");
			writer.WriteLine("  -i, --input-file <file path> ");
			writer.WriteLine("    The input JSON file which describes public interface of .NET Framework assemblies.");
			writer.WriteLine("    Typically it is an output of scan_fw command with -json option.");
			writer.WriteLine("  -ie, --input-encoding <encoding name>");
			writer.WriteLine("    The encoding of the input file.");
			writer.WriteLine("    Currently only UTF-8 is supported if it is specified.");
			writer.WriteLine("  -o, --output-file <file path>");
			writer.WriteLine("    The output CSV file which describes difference of public interface");
			writer.WriteLine("    between .NET Framework of the input and current .NET environment.");
			writer.WriteLine("    The command outputs to the standard out if this option is not specified.");
			writer.WriteLine("  -oe, --output-encoding <encoding name>");
			writer.WriteLine("    The encoding of the output file.");
			writer.WriteLine("    If this option is not specified, the output encoding is:");
			writer.WriteLine("      UTF-8 if the output is file, or");
			writer.WriteLine("      encoding of Console.Out if the output is the standard output.");
		}

		protected override string GetCommandName() {
			return "dotnet check_core.dll";
		}

		#endregion


		#region overridables

		protected virtual int Check() {
			// load the input file
			IReadOnlyDictionary<string, object> sourceInfo = GetSourceInfo(this.InputFilePath, this.InputEncoding);

			// check the source information
			// setup writer to write report and perform checking
			int check(TextWriter w) {
				this.Writer = w;
				try {
					return Check(sourceInfo);
				} finally {
					this.Writer = null;
				}
			}

			string outputFilePath = this.OutputFilePath;
			Encoding outputEncoding = this.OutputEncoding;
			if (string.IsNullOrEmpty(outputFilePath)) {
				// report to the standard output
				if (outputEncoding == null) {
					return check(Console.Out);
				} else {
					Console.Error.Write(outputEncoding);
					using (StreamWriter writer = new StreamWriter(Console.OpenStandardOutput(), outputEncoding)) {
						return check(writer);
					}
				}
			} else {
				// report to file
				if (outputEncoding == null) {
					outputEncoding = Encoding.UTF8;
				}
				using (FileStream stream = File.OpenWrite(outputFilePath)) {
					using (StreamWriter writer = new StreamWriter(stream, outputEncoding)) {
						return check(writer);
					}
				}
			}
		}

		protected abstract IReadOnlyDictionary<string, object> GetSourceInfo(string inputFilePath, Encoding encoding);

		protected virtual int Check(IReadOnlyDictionary<string, object> sourceInfo) {
			// check arguments
			if (sourceInfo == null) {
				throw new ArgumentNullException(nameof(sourceInfo));
			}

			// check state
			TextWriter writer = this.Writer;
			Debug.Assert(writer != null);

			// setup event handler and check the source information
			Comparer checker = CreateChecker();
			SetupChecker(checker);

			checker.Report += checker_Report;
			try {
				// write header line to output
				writer.Write("# ");
				writer.WriteLine(ComparingReport.GetHeaderLine());

				// check
				return checker.Check(sourceInfo);
			} finally {
				checker.Report -= checker_Report;
			}
		}

		protected virtual Comparer CreateChecker() {
			return new Comparer();
		}

		protected virtual void SetupChecker(Comparer checker) {
			// check argument
			if (checker == null) {
				throw new ArgumentNullException(nameof(checker));
			}

			// setup checker
			checker.Sort = true;
		}

		#endregion


		#region event handlers

		private void checker_Report(object sender, CompareEventArgs e) {
			// check state
			TextWriter writer = this.Writer;
			if (writer == null) {
				return;
			}

			// write the report to the output
			e.Report.WriteTo(writer, appendNewLine: true);
		}

		#endregion
	}
}
