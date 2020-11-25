using System;
using System.Collections.Generic;
using System.Reflection;

namespace DotNetAPIScanner.ReflectionScanning {
	public interface IReflectionFilter {
		IEnumerable<Type> GetTypes(Assembly assembly);
		IEnumerable<FieldInfo> GetFields(Type type);
		IEnumerable<PropertyInfo> GetProperties(Type type);
		IEnumerable<ConstructorInfo> GetConstructors(Type type);
		IEnumerable<MethodInfo> GetMethods(Type type);
		IEnumerable<EventInfo> GetEvents(Type type);
	}
}
