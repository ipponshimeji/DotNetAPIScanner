using System;
using System.Diagnostics;
using System.IO;


namespace DotNetAPIScanner.Scanner {
	public class ReporterInText: Reporter {
		#region data

		protected TextWriter Writer { get; private set; }

		private readonly bool disposeWriterOnDispose;

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
	}
}
