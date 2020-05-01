using System;
using Zafu.ReflectionScanning;


namespace Scanner {
	public class Reporter: ReflectionHandler, IDisposable {
		#region initialization & disposal

		public Reporter() : base() {
		}

		public virtual void Dispose() {
		}

		#endregion
	}
}
