using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DotNetAPIScanner {
	public abstract class TextOutputCommand: Command {
		#region data - arguments

		private string outputFilePath = null;

		private Encoding outputEncoding = null;

		#endregion


		#region properties

		public string OutputFilePath {
			get {
				return this.outputFilePath;
			}
			set {
				SetCommandArgumentProperty<string>(ref this.outputFilePath, value);
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

		#endregion


		#region initialization & disposal

		public TextOutputCommand() : base() {
		}

		#endregion


		#region methods

		protected int OpenOutputWriterAndExecute(Func<TextWriter, int> execute) {
			// check argument
			if (execute == null) {
				throw new ArgumentNullException(nameof(execute));
			}

			string outputFilePath = this.OutputFilePath;
			Encoding outputEncoding = this.OutputEncoding;
			if (string.IsNullOrEmpty(outputFilePath)) {
				// output to the standard output
				if (outputEncoding == null) {
					return execute(Console.Out);
				} else {
					using (StreamWriter writer = new StreamWriter(Console.OpenStandardOutput(), outputEncoding)) {
						return execute(writer);
					}
				}
			} else {
				// output to file
				if (outputEncoding == null) {
					outputEncoding = Encoding.UTF8;
				}
				using (FileStream stream = File.OpenWrite(outputFilePath)) {
					using (StreamWriter writer = new StreamWriter(stream, outputEncoding)) {
						return execute(writer);
					}
				}
			}
		}

		#endregion


		#region overrides

		protected override void HandleOption(string arg, IEnumerator<string> argEnumerator) {
			// check argument
			Debug.Assert(arg != null);

			switch (arg) {
				case "-o":
				case "--output":
					this.OutputFilePath = GetOptionValue(arg, argEnumerator);
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

		#endregion
	}
}
