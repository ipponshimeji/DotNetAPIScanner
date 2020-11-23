using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Reflection;


namespace DotNetAPIScanner {
	public class ParameterArrayComparer: IComparer<ParameterInfo[]> {
		#region data

		public static readonly ParameterArrayComparer Instance = new ParameterArrayComparer();

		private static readonly ParameterInfo[] Empty = Array.Empty<ParameterInfo>(); 

		#endregion


		#region IComparer<ParameterInfo[]>

		public int Compare(ParameterInfo[] x, ParameterInfo[] y) {
			// check arguments
			if (x == null) {
				x = Empty;
			}
			if (y == null) {
				y = Empty;
			}

			// first, looks at their length
			int result = x.Length - y.Length;
			if (result == 0) {
				// then, looks at each parameter
				for (int i = 0; i < x.Length; ++i) {
					result = Compare(x[i], y[i]);
					if (result != 0) {
						break;
					}
				}
			}
			return result;
		}

		#endregion


		#region methods

		public int Compare(ParameterInfo x, ParameterInfo y) {
			return StringComparer.OrdinalIgnoreCase.Compare(
				Util.GetTypeDisplayName(x.ParameterType),
				Util.GetTypeDisplayName(y.ParameterType)
			);
		}

		#endregion
	}
}
