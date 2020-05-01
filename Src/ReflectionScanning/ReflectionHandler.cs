using System;
using System.Collections.Generic;
using System.Reflection;


namespace Zafu.ReflectionScanning {
	public class ReflectionHandler: IReflectionHandler {
		#region data

		// The handler which does nothing.
		public static readonly ReflectionHandler Null = new ReflectionHandler();

		#endregion


		#region IReflectionHandler

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

		#endregion
	}
}
