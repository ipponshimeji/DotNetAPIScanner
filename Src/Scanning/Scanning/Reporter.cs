using System;
using DotNetAPIScanner.ReflectionScanning;

namespace DotNetAPIScanner.Scanning {
	public class Reporter: ReflectionHandler, IDisposable {
		#region initialization & disposal

		public Reporter() : base() {
		}

		public virtual void Dispose() {
		}

		#endregion
	}
}
