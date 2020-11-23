using System;
using System.Diagnostics;

namespace DotNetAPIScanner.Comparing {
	public class CompareEventArgs: EventArgs {
		#region data

		public  ComparingReport Report { get; private set; }

		#endregion


		#region initialization & disposal

		public CompareEventArgs(ComparingReport report) {
			// check argument
			if (report == null) {
				throw new ArgumentNullException(nameof(report));
			}

			// initialize member
			this.Report = report;
		}

		#endregion
	}
}
