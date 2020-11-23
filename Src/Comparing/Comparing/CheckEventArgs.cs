using System;
using System.Diagnostics;

namespace DotNetAPIScanner.Comparing {
	public class CheckEventArgs: EventArgs {
		#region data

		public  CheckReport Report { get; private set; }

		#endregion


		#region initialization & disposal

		public CheckEventArgs(CheckReport report) {
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
