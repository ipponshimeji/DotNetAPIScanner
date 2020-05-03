using System;
using System.Diagnostics;
using System.IO;


namespace DotNetAPIScanner.Scanner {
	public class ReporterInText: Reporter {
		#region constants

		public const int DefaultIndentWidth = 2;

		public const int MaxIndentWidth = 32;

		public const int MaxIndentLevel = 128;

		#endregion


		#region data

		protected TextWriter Writer { get; private set; }

		private readonly bool disposeWriterOnDispose;

		private int indentLevel = 0;

		private int indentWidth = DefaultIndentWidth;

		#endregion


		#region properties

		public int IndentWidth {
			get {
				return this.indentWidth;
			}
			set {
				// check argument
				if (value < 0 || MaxIndentWidth < value) {
					throw new ArgumentOutOfRangeException(nameof(value));
				}

				this.indentWidth = value;
			}
		}

		#endregion


		#region initialization & disposal

		public ReporterInText(TextWriter writer, bool disposeWriterOnDispose = true) : base() {
			// check argument
			if (writer == null) {
				throw new ArgumentNullException(nameof(writer));
			}

			// initialize member
			this.Writer = writer;
			this.disposeWriterOnDispose = disposeWriterOnDispose;
		}

		public override void Dispose() {
			// dispose this.Writer if necessary
			TextWriter writer = this.Writer;
			this.Writer = null;
			if (writer != null && this.disposeWriterOnDispose) {
				writer.Dispose();
			}

			base.Dispose();
		}

		#endregion


		#region methods

		protected void Indent() {
			// check state
			if (this.indentLevel < 0 || MaxIndentLevel <= this.indentLevel) {
				throw new InvalidOperationException();
			}

			++this.indentLevel;
		}

		protected void Unindent() {
			// check state
			if (this.indentLevel <= 0) {
				throw new InvalidOperationException();
			}

			--this.indentLevel;
		}

		protected void WriteIndent() {
			// write spaces for indentation
			int len = this.indentLevel * this.IndentWidth;
			for (int i = 0; i < len; ++i) {
				this.Writer.Write(' ');
			}
		}

		protected void WriteLine(string value) {
			// check argument
			// line can be null

			WriteIndent();
			this.Writer.WriteLine(value);
		}

		#endregion
	}
}
