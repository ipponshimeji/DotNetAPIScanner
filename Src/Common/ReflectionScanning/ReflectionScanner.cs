using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;


namespace Zafu.ReflectionScanning {
	public class ReflectionScanner {
		#region data

		public readonly IReflectionFilter Filter;

		public IReflectionHandler handler = ReflectionHandler.Null;

		#endregion


		#region creation & disposal

		public ReflectionScanner(IReflectionFilter filter) {
			// check arguments
			if (filter == null) {
				throw new ArgumentNullException(nameof(filter));
			}

			// initialize member
			this.Filter = filter;
		}

		#endregion


		#region methods

		// Listener interface model is prefered to event model here.
		// It would be more efficient for the scanning scenario.

		public void AddHandler(IReflectionHandler handler) {
			// check arguments
			if (handler == null) {
				throw new ArgumentNullException(nameof(handler));
			}

			// check state
			// Currently only one handler can be added.
			if (this.handler != ReflectionHandler.Null) {
				throw new InvalidOperationException();
			}

			this.handler = handler;
		}

		public bool RemoveHandler(IReflectionHandler handler) {
			// check arguments
			if (handler == null) {
				throw new ArgumentNullException(nameof(handler));
			}

			// Currently only one handler can be added.
			if (this.handler == handler) {
				this.handler = ReflectionHandler.Null;
				return true;
			} else {
				return false;
			}
		}


		public bool ScanAssemblies(IReadOnlyCollection<Assembly> assemblies) {
			// check argument
			if (assemblies == null) {
				throw new ArgumentNullException(nameof(assemblies));
			}

			return ScanCollection<Assembly>(assemblies, OnAssembliesScanning, ScanAssembly, OnAssembliesScanned);
		}

		public bool ScanAssembly(Assembly assembly) {
			// check argument
			if (assembly == null) {
				throw new ArgumentNullException(nameof(assembly));
			}

			void scan(Assembly a) {
				ScanTypes(this.Filter.GetTypes(assembly));
			}
			return ScanItem<Assembly>(assembly, OnAssemblyScanning, scan, OnAssemblyScanned);
		}

		public bool ScanTypes(IReadOnlyCollection<Type> types) {
			// check argument
			if (types == null) {
				throw new ArgumentNullException(nameof(types));
			}

			return ScanCollection<Type>(types, OnTypesScanning, ScanType, OnTypesScanned);
		}

		public bool ScanType(Type type) {
			// check argument
			if (type == null) {
				throw new ArgumentNullException(nameof(type));
			}

			void scan(Type t) {
				ScanFields(this.Filter.GetFields(t));
				ScanProperties(this.Filter.GetProperties(t));
				ScanConstructors(this.Filter.GetConstructors(t));
				ScanMethods(this.Filter.GetMethods(t));
				ScanEvents(this.Filter.GetEvents(t));
			}
			return ScanItem<Type>(type, OnTypeScanning, scan, OnTypeScanned);
		}

		public bool ScanFields(IReadOnlyCollection<FieldInfo> fields) {
			// check argument
			if (fields == null) {
				throw new ArgumentNullException(nameof(fields));
			}

			return ScanCollection<FieldInfo>(fields, OnFieldsScanning, ScanField, OnFieldsScanned);
		}

		public bool ScanField(FieldInfo field) {
			// check argument
			if (field == null) {
				throw new ArgumentNullException(nameof(field));
			}

			return OnField(field);
		}

		public bool ScanProperties(IReadOnlyCollection<PropertyInfo> props) {
			// check argument
			if (props == null) {
				throw new ArgumentNullException(nameof(props));
			}

			return ScanCollection<PropertyInfo>(props, OnPropertiesScanning, ScanProperty, OnPropertiesScanned);
		}

		public bool ScanProperty(PropertyInfo prop) {
			// check argument
			if (prop == null) {
				throw new ArgumentNullException(nameof(prop));
			}

			return OnProperty(prop);
		}

		public bool ScanConstructors(IReadOnlyCollection<ConstructorInfo> ctors) {
			// check argument
			if (ctors == null) {
				throw new ArgumentNullException(nameof(ctors));
			}

			return ScanCollection<ConstructorInfo>(ctors, OnConstructorsScanning, ScanConstructor, OnConstructorsScanned);
		}

		public bool ScanConstructor(ConstructorInfo ctor) {
			// check argument
			if (ctor == null) {
				throw new ArgumentNullException(nameof(ctor));
			}

			return OnConstructor(ctor);
		}

		public bool ScanMethods(IReadOnlyCollection<MethodInfo> methods) {
			// check argument
			if (methods == null) {
				throw new ArgumentNullException(nameof(methods));
			}

			return ScanCollection<MethodInfo>(methods, OnMethodsScanning, ScanMethod, OnMethodsScanned);
		}

		public bool ScanMethod(MethodInfo method) {
			// check argument
			if (method == null) {
				throw new ArgumentNullException(nameof(method));
			}

			return OnMethod(method);
		}

		public bool ScanEvents(IReadOnlyCollection<EventInfo> events) {
			// check argument
			if (events == null) {
				throw new ArgumentNullException(nameof(events));
			}

			return ScanCollection<EventInfo>(events, OnEventsScanning, ScanEvent, OnEventsScanned);
		}

		public bool ScanEvent(EventInfo evt) {
			// check argument
			if (evt == null) {
				throw new ArgumentNullException(nameof(evt));
			}

			return OnEvent(evt);
		}

		#endregion


		#region methods - patterns

		private bool ScanCollection<T>(IReadOnlyCollection<T> collection, Action<IReadOnlyCollection<T>> onScanning, Func<T, bool> scan, Action<IReadOnlyCollection<T>, bool, Exception> onScanned) {
			// check arguments
			Debug.Assert(collection != null);
			Debug.Assert(onScanning != null);
			Debug.Assert(scan != null);
			Debug.Assert(onScanned != null);

			// scan the collection
			bool canceled = false;
			Exception error = null;
			onScanning(collection);
			try {
				foreach (T item in collection) {
					canceled = scan(item);
					if (canceled) {
						break;
					}
				}
			} catch (Exception exception) {
				canceled = true;
				error = exception;
				throw;
			} finally {
				onScanned(collection, canceled, error);
			}

			return canceled;
		}

		private bool ScanItem<T>(T item, Action<T> onScanning, Action<T> scan, Func<T, Exception, bool> onScanned) where T : class {
			// check argument
			Debug.Assert(item != null);
			Debug.Assert(onScanning != null);
			Debug.Assert(scan != null);
			Debug.Assert(onScanned != null);

			// scan the item
			bool cancelRequested = false;
			Exception error = null;
			onScanning(item);
			try {
				scan(item);
			} catch (Exception exception) {
				error = exception;
				throw;
			} finally {
				cancelRequested = onScanned(item, error);
			}

			return cancelRequested;
		}

		#endregion


		#region overridables

		protected virtual void OnAssembliesScanning(IReadOnlyCollection<Assembly> assemblies) {
			// check argument
			Debug.Assert(assemblies != null);

			// check state
			IReflectionHandler handler = this.handler;
			Debug.Assert(handler != null);

			// call the handler
			handler.OnAssembliesScanning(assemblies);
		}

		protected virtual void OnAssembliesScanned(IReadOnlyCollection<Assembly> assemblies, bool canceled, Exception error) {
			// check arguments
			Debug.Assert(assemblies != null);
			// error can be null

			// check state
			IReflectionHandler handler = this.handler;
			Debug.Assert(handler != null);

			// call the handler
			handler.OnAssembliesScanned(assemblies, canceled, error);
		}

		protected virtual void OnAssemblyScanning(Assembly assembly) {
			// check argument
			Debug.Assert(assembly != null);

			// check state
			IReflectionHandler handler = this.handler;
			Debug.Assert(handler != null);

			// call the handler
			handler.OnAssemblyScanning(assembly);
		}

		protected virtual bool OnAssemblyScanned(Assembly assembly, Exception error) {
			// check arguments
			Debug.Assert(assembly != null);
			// error can be null

			// check state
			IReflectionHandler handler = this.handler;
			Debug.Assert(handler != null);

			// call the handler
			return handler.OnAssemblyScanned(assembly, error);
		}

		protected virtual void OnTypesScanning(IReadOnlyCollection<Type> types) {
			// check argument
			Debug.Assert(types != null);

			// check state
			IReflectionHandler handler = this.handler;
			Debug.Assert(handler != null);

			// call the handler
			handler.OnTypesScanning(types);
		}

		protected virtual void OnTypesScanned(IReadOnlyCollection<Type> types, bool canceled, Exception error) {
			// check arguments
			Debug.Assert(types != null);
			// error can be null

			// check state
			IReflectionHandler handler = this.handler;
			Debug.Assert(handler != null);

			// call the handler
			handler.OnTypesScanned(types, canceled, error);
		}

		protected virtual void OnTypeScanning(Type type) {
			// check argument
			Debug.Assert(type != null);

			// check state
			IReflectionHandler handler = this.handler;
			Debug.Assert(handler != null);

			// call the handler
			handler.OnTypeScanning(type);
		}

		protected virtual bool OnTypeScanned(Type type, Exception error) {
			// check arguments
			Debug.Assert(type != null);
			// error can be null

			// check state
			IReflectionHandler handler = this.handler;
			Debug.Assert(handler != null);

			// call the handler
			return handler.OnTypeScanned(type, error);
		}

		protected virtual void OnFieldsScanning(IReadOnlyCollection<FieldInfo> fields) {
			// check argument
			Debug.Assert(fields != null);

			// check state
			IReflectionHandler handler = this.handler;
			Debug.Assert(handler != null);

			// call the handler
			handler.OnFieldsScanning(fields);
		}

		protected virtual void OnFieldsScanned(IReadOnlyCollection<FieldInfo> fields, bool canceled, Exception error) {
			// check arguments
			Debug.Assert(fields != null);
			// error can be null

			// check state
			IReflectionHandler handler = this.handler;
			Debug.Assert(handler != null);

			// call the handler
			handler.OnFieldsScanned(fields, canceled, error);
		}

		protected virtual bool OnField(FieldInfo field) {
			// check argument
			Debug.Assert(field != null);

			// check state
			IReflectionHandler handler = this.handler;
			Debug.Assert(handler != null);

			// call the handler
			return handler.OnField(field);
		}

		protected virtual void OnPropertiesScanning(IReadOnlyCollection<PropertyInfo> props) {
			// check argument
			Debug.Assert(props != null);

			// check state
			IReflectionHandler handler = this.handler;
			Debug.Assert(handler != null);

			// call the handler
			handler.OnPropertiesScanning(props);
		}

		protected virtual void OnPropertiesScanned(IReadOnlyCollection<PropertyInfo> props, bool canceled, Exception error) {
			// check arguments
			Debug.Assert(props != null);
			// error can be null

			// check state
			IReflectionHandler handler = this.handler;
			Debug.Assert(handler != null);

			// call the handler
			handler.OnPropertiesScanned(props, canceled, error);
		}

		protected virtual bool OnProperty(PropertyInfo prop) {
			// check argument
			Debug.Assert(prop != null);

			// check state
			IReflectionHandler handler = this.handler;
			Debug.Assert(handler != null);

			// call the handler
			return handler.OnProperty(prop);
		}

		protected virtual void OnConstructorsScanning(IReadOnlyCollection<ConstructorInfo> ctors) {
			// check argument
			Debug.Assert(ctors != null);

			// check state
			IReflectionHandler handler = this.handler;
			Debug.Assert(handler != null);

			// call the handler
			handler.OnConstructorsScanning(ctors);
		}

		protected virtual void OnConstructorsScanned(IReadOnlyCollection<ConstructorInfo> ctors, bool canceled, Exception error) {
			// check arguments
			Debug.Assert(ctors != null);
			// error can be null

			// check state
			IReflectionHandler handler = this.handler;
			Debug.Assert(handler != null);

			// call the handler
			handler.OnConstructorsScanned(ctors, canceled, error);
		}

		protected virtual bool OnConstructor(ConstructorInfo ctor) {
			// check argument
			Debug.Assert(ctor != null);

			// check state
			IReflectionHandler handler = this.handler;
			Debug.Assert(handler != null);

			// call the handler
			return handler.OnConstructor(ctor);
		}

		protected virtual void OnMethodsScanning(IReadOnlyCollection<MethodInfo> methods) {
			// check argument
			Debug.Assert(methods != null);

			// check state
			IReflectionHandler handler = this.handler;
			Debug.Assert(handler != null);

			// call the handler
			handler.OnMethodsScanning(methods);
		}

		protected virtual void OnMethodsScanned(IReadOnlyCollection<MethodInfo> methods, bool canceled, Exception error) {
			// check arguments
			Debug.Assert(methods != null);
			// error can be null

			// check state
			IReflectionHandler handler = this.handler;
			Debug.Assert(handler != null);

			// call the handler
			handler.OnMethodsScanned(methods, canceled, error);
		}

		protected virtual bool OnMethod(MethodInfo method) {
			// check argument
			Debug.Assert(method != null);

			// check state
			IReflectionHandler handler = this.handler;
			Debug.Assert(handler != null);

			// call the handler
			return handler.OnMethod(method);
		}

		protected virtual void OnEventsScanning(IReadOnlyCollection<EventInfo> events) {
			// check argument
			Debug.Assert(events != null);

			// check state
			IReflectionHandler handler = this.handler;
			Debug.Assert(handler != null);

			// call the handler
			handler.OnEventsScanning(events);
		}

		protected virtual void OnEventsScanned(IReadOnlyCollection<EventInfo> events, bool canceled, Exception error) {
			// check arguments
			Debug.Assert(events != null);
			// error can be null

			// check state
			IReflectionHandler handler = this.handler;
			Debug.Assert(handler != null);

			// call the handler
			handler.OnEventsScanned(events, canceled, error);
		}

		protected virtual bool OnEvent(EventInfo evt) {
			// check argument
			Debug.Assert(evt != null);

			// check state
			IReflectionHandler handler = this.handler;
			Debug.Assert(handler != null);

			// call the handler
			return handler.OnEvent(evt);
		}

		#endregion
	}
}
