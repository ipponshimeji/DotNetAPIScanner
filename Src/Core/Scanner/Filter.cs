using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Zafu.ReflectionScanning;


namespace DotNetAPIScanner.Scanner {
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

			// return list of fields exposed outside of the assembly
			return type.GetFields(defaultBindingFlags)
					   .Where(IsExposedField)
					   .OrderBy(f => f.Name, StringComparer.Ordinal)
					   .ToArray();
		}

		private static bool IsExposedField(FieldInfo field) {
			// check argument
			Debug.Assert(field != null);

			switch (field.Attributes & FieldAttributes.FieldAccessMask) {
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
			// properties are checked by its accessor methods.
			return Array.Empty<PropertyInfo>();
		}

		public ConstructorInfo[] GetConstructors(Type type) {
			// check argument
			Debug.Assert(type != null);

			// return list of constructors exposed outside of the assembly
			return type.GetConstructors(defaultBindingFlags)
					   .Where(IsExposedConstructor)
					   .OrderBy(c => c.GetParameters(), ParameterArrayComparer.Instance)
					   .ToArray();
		}

		private static bool IsExposedMethodBase(MethodBase method) {
			// check argument
			Debug.Assert(method != null);

			switch (method.Attributes & MethodAttributes.MemberAccessMask) {
				case MethodAttributes.Public:
				case MethodAttributes.Family:
				case MethodAttributes.FamORAssem:
					return true;
				default:
					// include MethodAttributes.Private, FamAndAssem, Assembly
					return false;
			}
		}

		private static bool IsExposedConstructor(ConstructorInfo ctor) {
			// check argument
			Debug.Assert(ctor != null);

			return IsExposedMethodBase(ctor);
		}

		public MethodInfo[] GetMethods(Type type) {
			// check argument
			Debug.Assert(type != null);

			// return list of methods exposed outside of the assembly
			// Note that the list contains special methods for Properties, Events and Operators.
			// ex. get_*, set_*, add_*, remove_* and op_*
			return type.GetMethods(defaultBindingFlags)
					   .Where(IsExposedMethod)
					   .OrderBy(m => m.Name, StringComparer.Ordinal)
					   .ThenBy(m => m.GetParameters(), ParameterArrayComparer.Instance)
					   .ToArray();
		}

		private static bool IsExposedMethod(MethodInfo method) {
			// check argument
			Debug.Assert(method != null);

			return IsExposedMethodBase(method);
		}

		public EventInfo[] GetEvents(Type type) {
			// events are checked by its add/remove methods.
			return Array.Empty<EventInfo>();
		}

		#endregion
	}
}
