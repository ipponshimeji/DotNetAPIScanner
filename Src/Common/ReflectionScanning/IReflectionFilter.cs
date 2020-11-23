using System;
using System.Collections.Generic;
using System.Reflection;

namespace DotNetAPIScanner.ReflectionScanning {
	public interface IReflectionFilter {
		Type[] GetTypes(Assembly assembly);
		FieldInfo[] GetFields(Type type);
		PropertyInfo[] GetProperties(Type type);
		ConstructorInfo[] GetConstructors(Type type);
		MethodInfo[] GetMethods(Type type);
		EventInfo[] GetEvents(Type type);
	}
}
