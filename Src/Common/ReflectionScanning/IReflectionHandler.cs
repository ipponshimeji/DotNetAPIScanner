using System;
using System.Collections.Generic;
using System.Reflection;

namespace DotNetAPIScanner.ReflectionScanning {
	public interface IReflectionHandler {
		void OnAssembliesScanning(IReadOnlyCollection<Assembly> assemblies);

		void OnAssembliesScanned(IReadOnlyCollection<Assembly> assemblies, bool canceled, Exception error);

		void OnAssemblyScanning(Assembly assembly);

		bool OnAssemblyScanned(Assembly assembly, Exception error);

		void OnTypesScanning(IReadOnlyCollection<Type> types);

		void OnTypesScanned(IReadOnlyCollection<Type> types, bool canceled, Exception error);

		void OnTypeScanning(Type type);

		bool OnTypeScanned(Type type, Exception error);

		void OnFieldsScanning(IReadOnlyCollection<FieldInfo> fields);

		void OnFieldsScanned(IReadOnlyCollection<FieldInfo> fields, bool canceled, Exception error);

		bool OnField(FieldInfo field);

		void OnPropertiesScanning(IReadOnlyCollection<PropertyInfo> props);

		void OnPropertiesScanned(IReadOnlyCollection<PropertyInfo> props, bool canceled, Exception error);

		bool OnProperty(PropertyInfo prop);

		void OnConstructorsScanning(IReadOnlyCollection<ConstructorInfo> ctors);

		void OnConstructorsScanned(IReadOnlyCollection<ConstructorInfo> ctors, bool canceled, Exception error);

		bool OnConstructor(ConstructorInfo ctor);

		void OnMethodsScanning(IReadOnlyCollection<MethodInfo> methods);

		void OnMethodsScanned(IReadOnlyCollection<MethodInfo> methods, bool canceled, Exception error);

		bool OnMethod(MethodInfo method);

		void OnEventsScanning(IReadOnlyCollection<EventInfo> events);

		void OnEventsScanned(IReadOnlyCollection<EventInfo> events, bool canceled, Exception error);

		bool OnEvent(EventInfo evt);
	}
}
