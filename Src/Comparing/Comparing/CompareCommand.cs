using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace DotNetAPIScanner.Comparing {
	public abstract class CompareCommand: TextOutputCommand {
		#region types

		public new class TaskKinds: Command.TaskKinds {
			#region constants

			public const string Compare = "compare";

			#endregion
		}

		#endregion


		#region data - arguments

		private Encoding inputEncoding = null;

		private string inputFilePath = null;

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

		public string InputFilePath {
			get {
				return this.inputFilePath;
			}
			set {
				SetCommandArgumentProperty<string>(ref this.inputFilePath, value);
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
			checker.Report += comparer_Report;
			try {
				// write header line to output
				writer.Write("# ");
				writer.WriteLine(ComparingReport.GetHeaderLine());

				// check
				return checker.Compare(source);
			} finally {
				checker.Report -= comparer_Report;
			}
		}

		#endregion


		#region methods

		public int Compare(TextWriter writer) {
			// check argument
			if (writer == null) {
				throw new ArgumentNullException(nameof(writer));
			}

			this.Writer = writer;
			try {
				// load the input file
				IReadOnlyDictionary<string, object> sourceInfo = GetSourceInfo(this.InputFilePath, this.InputEncoding);
				return Compare(sourceInfo);
			} finally {
				this.Writer = null;
			}
		}

		public int Compare() {
			return OpenOutputWriterAndExecute(Compare);
		}

		protected static IReadOnlyDictionary<string, object> OpenInputStreamAndGetSourceInfo(string inputFilePath, Encoding inputEncoding, Func<Stream, Encoding, IReadOnlyDictionary<string, object>> getSourceInfo) {
			// check argument
			if (getSourceInfo == null) {
				throw new ArgumentNullException(nameof(getSourceInfo));
			}

			if (string.IsNullOrEmpty(inputFilePath)) {
				// input from the standard input
				if (inputEncoding == null) {
					inputEncoding = Console.InputEncoding;
				}
				return getSourceInfo(Console.OpenStandardInput(), inputEncoding);
			} else {
				// input from a file
				if (inputEncoding == null) {
					inputEncoding = Encoding.UTF8;
				}
				using (FileStream stream = File.OpenRead(inputFilePath)) {
					return getSourceInfo(stream, inputEncoding);
				}
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
				case "-ie":
				case "--input-encoding":
					this.InputEncoding = Encoding.GetEncoding(GetOptionValue(arg, argEnumerator));
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
				taskKind = TaskKinds.Compare;
			}

			return taskKind;
		}

		protected override int Execute(string taskKind) {
			switch (taskKind) {
				case TaskKinds.Compare:
					return Compare();
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
			writer.WriteLine("Compares the information of public interfaces of the .NET Framework assemblies");
			writer.WriteLine("collected on a .NET environment with ones on the current .NET environment.");
			writer.WriteLine("USAGE:");
			writer.WriteLine($"  {GetCommandName()} [OPTIONS]");
			writer.WriteLine("OPTIONS:");
			writer.WriteLine("  -i, --input-file <file path> ");
			writer.WriteLine("    The input JSON file which describes public interface of .NET Framework assemblies.");
			writer.WriteLine("    Typically it is an output of scan command with '--output-format json' option.");
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

		#endregion


		#region overridables

		protected abstract IReadOnlyDictionary<string, object> GetSourceInfo(string inputFilePath, Encoding encoding);

		protected virtual int Compare(IReadOnlyDictionary<string, object> sourceInfo) {
			// check arguments
			if (sourceInfo == null) {
				throw new ArgumentNullException(nameof(sourceInfo));
			}

			// check state
			TextWriter writer = this.Writer;
			Debug.Assert(writer != null);

			// setup event handler and compare the source information
			Comparer comparer = CreateComparer();
			SetupComparer(comparer);

			comparer.Report += comparer_Report;
			try {
				// write header line to output
				writer.Write("# ");
				writer.WriteLine(ComparingReport.GetHeaderLine());

				// check
				return comparer.Compare(sourceInfo);
			} finally {
				comparer.Report -= comparer_Report;
			}
		}

		protected virtual Comparer CreateComparer() {
			return new Comparer();
		}

		protected virtual void SetupComparer(Comparer comparer) {
			// check argument
			if (comparer == null) {
				throw new ArgumentNullException(nameof(comparer));
			}

			// setup checker
			comparer.Sort = true;
		}

		#endregion


		#region event handlers

		private void comparer_Report(object sender, CompareEventArgs e) {
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
