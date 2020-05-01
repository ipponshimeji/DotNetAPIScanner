using System;
using System.Collections.Generic;
using System.Reflection;


namespace Zafu.ReflectionScanning {
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
	}
}
