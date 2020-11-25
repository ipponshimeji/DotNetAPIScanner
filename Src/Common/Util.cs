using System;
using System.Diagnostics;
using System.Text;

namespace DotNetAPIScanner {
	public static class Util {
		#region methods

		public static string GetTypeDisplayName(Type type) {
			// check argument
			if (type == null) {
				throw new ArgumentNullException(nameof(type));
			}

			// build display name for the type
			if (type.HasElementType) {
				// derived type
				string elementTypeDisplayName = GetTypeDisplayName(type.GetElementType());
				if (type.IsPointer) {
					// pointer
					return $"{elementTypeDisplayName}*";
				} else if (type.IsByRef) {
					// reference
					return $"{elementTypeDisplayName}&";
				} else if (type.IsArray) {
					// array
					StringBuilder buf = new StringBuilder();
					buf.Append(elementTypeDisplayName);
					buf.Append("[");
					buf.Append(',', type.GetArrayRank() - 1);
					buf.Append("]");
					return buf.ToString();
				} else {
					// unknown derived type
					Debug.Assert(false);
					return type.ToString();
				}
			} else if (type.IsGenericParameter) {
				// type parameter
				if (type.DeclaringMethod != null) {
					return $"!!{type.GenericParameterPosition}";
				} else {
					return $"!{type.GenericParameterPosition}";
				}
			} else if (type.IsGenericType == false) {
				// non-generic type
				return type.ToString();
			} else {
				// generic type
				Type[] typeArgs = type.GenericTypeArguments;
				if (typeArgs == null || typeArgs.Length == 0) {
					// open type
					// Do not use type.ToString(), which emits type parameters.
					//   ex. "IEquatable`1[T]"
					return type.FullName;
				} else {
					// closed type
					StringBuilder buf = new StringBuilder();

					// Note that typeArgs must be provided to get name for partial closed type.
					buf.Append(GetTypeDisplayName(type.GetGenericTypeDefinition()));
					buf.Append("[");
					bool first = true;
					foreach (Type arg in typeArgs) {
						if (first) {
							first = false;
						} else {
							buf.Append(",");
						}
						buf.Append(GetTypeDisplayName(arg));
					}
					buf.Append("]");
					return buf.ToString();
				}
			}
		}

		#endregion
	}
}
