using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;


namespace DotNetAPIScanner.Checker {
	public class CheckCommand: IDisposable {
		#region data

		protected TextWriter Writer { get; private set; }

		private readonly bool disposeWriterOnDispose;

		#endregion


		#region initialization & disposal

		public CheckCommand(TextWriter writer, bool disposeWriterOnDispose = true) : base() {
			// check argument
			if (writer == null) {
				throw new ArgumentNullException(nameof(writer));
			}

			// initialize member
			this.Writer = writer;
			this.disposeWriterOnDispose = disposeWriterOnDispose;
		}

		public virtual void Dispose() {
			// dispose this.Writer if necessary
			TextWriter writer = this.Writer;
			this.Writer = null;
			if (writer != null && this.disposeWriterOnDispose) {
				writer.Dispose();
			}
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
			Checker checker = new Checker();
			checker.Sort = sort;
			checker.Report += checker_Report;
			try {
				// write header line to output
				writer.Write("# ");
				writer.WriteLine(CheckReport.GetHeaderLine());

				// check
				return checker.Check(source);
			} finally {
				checker.Report -= checker_Report;
			}
		}

		#endregion


		#region event handlers

		private void checker_Report(object sender, CheckEventArgs e) {
			// check state
			TextWriter writer = this.Writer;
			if (writer == null) {
				return;
			}

			// write the report to the output
			e.Report.WriteTo(writer, quote: true, appendNewLine: true);
		}

		#endregion
	}
}
