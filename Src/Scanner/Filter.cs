using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Zafu.ReflectionScanning;


namespace Scanner {
	public class Filter: IReflectionFilter {
		#region data

		public static readonly Filter Instance = new Filter();


		private const BindingFlags defaultBindingFlags = (
			BindingFlags.DeclaredOnly
			| BindingFlags.Public
			| BindingFlags.NonPublic
			| BindingFlags.Instance
			| BindingFlags.Static
		);

		private static MethodInfo getForwardedTypes = typeof(Assembly).GetMethod("GetForwardedTypes");

		#endregion


		#region IReflectionFilter

		public Type[] GetTypes(Assembly assembly) {
			// check argument
			Debug.Assert(assembly != null);

			// return sorted list of exported types and forwarded types
			return assembly.GetExportedTypes()
						   .Concat(GetForwardedTypes(assembly))
						   .OrderBy(t => t.FullName, StringComparer.Ordinal)
						   .ToArray();
		}

		private static Type[] GetForwardedTypes(Assembly assembly) {
			// check argument
			Debug.Assert(assembly != null);

			// check state
			if (getForwardedTypes == null) {
				// not running on .NET Core
				return Array.Empty<Type>();
			}

			// return forwarded types
			// Note that Assembly.GetForwardedTypes() is defined only in .NET Core,
			// so the method is called through reflection.
			return getForwardedTypes.Invoke(assembly, Array.Empty<object>()) as Type[];
		}

		public FieldInfo[] GetFields(Type type) {
			// check argument
			Debug.Assert(type != null);

			// return list of fields exposed outside the assembly
			return type.GetFields(defaultBindingFlags)
					   .Where(IsExposedField)
					   .OrderBy(f => f.Name, StringComparer.Ordinal)
					   .ToArray();
		}

		private static bool IsExposedField(FieldInfo fieldInfo) {
			// check argument
			Debug.Assert(fieldInfo != null);

			switch (fieldInfo.Attributes & FieldAttributes.FieldAccessMask) {
				case FieldAttributes.Public:
				case FieldAttributes.Family:
				case FieldAttributes.FamORAssem:
					return true;
				default:
					// include FieldAttributes.Private and FieldAttributes.FamAndAssem
					return false;
			}
		}

		public PropertyInfo[] GetProperties(Type type) {
			return Array.Empty<PropertyInfo>();
		}

		public ConstructorInfo[] GetConstructors(Type type) {
			return Array.Empty<ConstructorInfo>();
		}

		public MethodInfo[] GetMethods(Type type) {
			return Array.Empty<MethodInfo>();
		}

		public EventInfo[] GetEvents(Type type) {
			return Array.Empty<EventInfo>();
		}

		#endregion
	}
}
