using System;
using System.Collections.Generic;
using System.Reflection;

namespace DotNetAPIScanner.ReflectionScanning {
	public class ReflectionHandler: IReflectionHandler {
		#region data

		// The handler which does nothing.
		public static readonly ReflectionHandler Null = new ReflectionHandler();

		#endregion


		#region IReflectionHandler

		// Provides dummy implementation which does nothing.

		public virtual void OnAssembliesScanning(IReadOnlyCollection<Assembly> assemblies) {
		}

		public virtual void OnAssembliesScanned(IReadOnlyCollection<Assembly> assemblies, bool canceled, Exception error) {
		}

		public virtual void OnAssemblyScanning(Assembly assembly) {
		}

		public virtual bool OnAssemblyScanned(Assembly assembly, Exception error) {
			return false;
		}

		public virtual void OnTypesScanning(IReadOnlyCollection<Type> types) {
		}

		public virtual void OnTypesScanned(IReadOnlyCollection<Type> types, bool canceled, Exception error) {
		}

		public virtual void OnTypeScanning(Type type) {
		}

		public virtual bool OnTypeScanned(Type type, Exception error) {
			return false;
		}

		public virtual void OnFieldsScanning(IReadOnlyCollection<FieldInfo> fields) {
		}

		public virtual void OnFieldsScanned(IReadOnlyCollection<FieldInfo> fields, bool canceled, Exception error) {
		}

		public virtual bool OnField(FieldInfo field) {
			return false;
		}

		public virtual void OnPropertiesScanning(IReadOnlyCollection<PropertyInfo> props) {
		}

		public virtual void OnPropertiesScanned(IReadOnlyCollection<PropertyInfo> props, bool canceled, Exception error) {
		}

		public virtual bool OnProperty(PropertyInfo prop) {
			return false;
		}

		public virtual void OnConstructorsScanning(IReadOnlyCollection<ConstructorInfo> ctors) {
		}

		public virtual void OnConstructorsScanned(IReadOnlyCollection<ConstructorInfo> ctors, bool canceled, Exception error) {
		}

		public virtual bool OnConstructor(ConstructorInfo ctor) {
			return false;
		}

		public virtual void OnMethodsScanning(IReadOnlyCollection<MethodInfo> methods) {
		}

		public virtual void OnMethodsScanned(IReadOnlyCollection<MethodInfo> methods, bool canceled, Exception error) {
		}

		public virtual bool OnMethod(MethodInfo method) {
			return false;
		}

		public virtual void OnEventsScanning(IReadOnlyCollection<EventInfo> events) {
		}

		public virtual void OnEventsScanned(IReadOnlyCollection<EventInfo> events, bool canceled, Exception error) {
		}

		public virtual bool OnEvent(EventInfo evt) {
			return false;
		}

		#endregion
	}
}
