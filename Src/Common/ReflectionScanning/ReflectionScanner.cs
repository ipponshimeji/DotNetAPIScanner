using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace DotNetAPIScanner.ReflectionScanning {
	public class ReflectionScanner {
		#region data

		public readonly IReflectionFilter Filter;

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

		public bool ScanAssemblies(IReflectionHandler handler, IEnumerable<Assembly> assemblies) {
			// check argument
			if (handler == null) {
				throw new ArgumentNullException(nameof(handler));
			}
			if (assemblies == null) {
				throw new ArgumentNullException(nameof(assemblies));
			}

			return ScanItems<Assembly>(handler, assemblies, OnAssembliesScanning, ScanAssembly, OnAssembliesScanned);
		}

		public bool ScanAssembly(IReflectionHandler handler, Assembly assembly) {
			// check argument
			if (handler == null) {
				throw new ArgumentNullException(nameof(handler));
			}
			if (assembly == null) {
				throw new ArgumentNullException(nameof(assembly));
			}

			void scan(IReflectionHandler h, Assembly a) {
				ScanTypes(h, this.Filter.GetTypes(assembly));
			}
			return ScanItem<Assembly>(handler, assembly, OnAssemblyScanning, scan, OnAssemblyScanned);
		}

		public bool ScanTypes(IReflectionHandler handler, IEnumerable<Type> types) {
			// check argument
			if (handler == null) {
				throw new ArgumentNullException(nameof(handler));
			}
			if (types == null) {
				throw new ArgumentNullException(nameof(types));
			}

			return ScanItems<Type>(handler, types, OnTypesScanning, ScanType, OnTypesScanned);
		}

		public bool ScanType(IReflectionHandler handler, Type type) {
			// check argument
			if (handler == null) {
				throw new ArgumentNullException(nameof(handler));
			}
			if (type == null) {
				throw new ArgumentNullException(nameof(type));
			}

			void scan(IReflectionHandler h, Type t) {
				ScanFields(h, this.Filter.GetFields(t));
				ScanProperties(h, this.Filter.GetProperties(t));
				ScanConstructors(h, this.Filter.GetConstructors(t));
				ScanMethods(h, this.Filter.GetMethods(t));
				ScanEvents(h, this.Filter.GetEvents(t));
			}
			return ScanItem<Type>(handler, type, OnTypeScanning, scan, OnTypeScanned);
		}

		public bool ScanFields(IReflectionHandler handler, IEnumerable<FieldInfo> fields) {
			// check argument
			if (handler == null) {
				throw new ArgumentNullException(nameof(handler));
			}
			if (fields == null) {
				throw new ArgumentNullException(nameof(fields));
			}

			return ScanItems<FieldInfo>(handler, fields, OnFieldsScanning, ScanField, OnFieldsScanned);
		}

		public bool ScanField(IReflectionHandler handler, FieldInfo field) {
			// check argument
			if (handler == null) {
				throw new ArgumentNullException(nameof(handler));
			}
			if (field == null) {
				throw new ArgumentNullException(nameof(field));
			}

			return OnField(handler, field);
		}

		public bool ScanProperties(IReflectionHandler handler, IEnumerable<PropertyInfo> props) {
			// check argument
			if (handler == null) {
				throw new ArgumentNullException(nameof(handler));
			}
			if (props == null) {
				throw new ArgumentNullException(nameof(props));
			}

			return ScanItems<PropertyInfo>(handler, props, OnPropertiesScanning, ScanProperty, OnPropertiesScanned);
		}

		public bool ScanProperty(IReflectionHandler handler, PropertyInfo prop) {
			// check argument
			if (handler == null) {
				throw new ArgumentNullException(nameof(handler));
			}
			if (prop == null) {
				throw new ArgumentNullException(nameof(prop));
			}

			return OnProperty(handler, prop);
		}

		public bool ScanConstructors(IReflectionHandler handler, IEnumerable<ConstructorInfo> ctors) {
			// check argument
			if (handler == null) {
				throw new ArgumentNullException(nameof(handler));
			}
			if (ctors == null) {
				throw new ArgumentNullException(nameof(ctors));
			}

			return ScanItems<ConstructorInfo>(handler, ctors, OnConstructorsScanning, ScanConstructor, OnConstructorsScanned);
		}

		public bool ScanConstructor(IReflectionHandler handler, ConstructorInfo ctor) {
			// check argument
			if (handler == null) {
				throw new ArgumentNullException(nameof(handler));
			}
			if (ctor == null) {
				throw new ArgumentNullException(nameof(ctor));
			}

			return OnConstructor(handler, ctor);
		}

		public bool ScanMethods(IReflectionHandler handler, IEnumerable<MethodInfo> methods) {
			// check argument
			if (handler == null) {
				throw new ArgumentNullException(nameof(handler));
			}
			if (methods == null) {
				throw new ArgumentNullException(nameof(methods));
			}

			return ScanItems<MethodInfo>(handler, methods, OnMethodsScanning, ScanMethod, OnMethodsScanned);
		}

		public bool ScanMethod(IReflectionHandler handler, MethodInfo method) {
			// check argument
			if (handler == null) {
				throw new ArgumentNullException(nameof(handler));
			}
			if (method == null) {
				throw new ArgumentNullException(nameof(method));
			}

			return OnMethod(handler, method);
		}

		public bool ScanEvents(IReflectionHandler handler, IEnumerable<EventInfo> events) {
			// check argument
			if (handler == null) {
				throw new ArgumentNullException(nameof(handler));
			}
			if (events == null) {
				throw new ArgumentNullException(nameof(events));
			}

			return ScanItems<EventInfo>(handler, events, OnEventsScanning, ScanEvent, OnEventsScanned);
		}

		public bool ScanEvent(IReflectionHandler handler, EventInfo evt) {
			// check argument
			if (handler == null) {
				throw new ArgumentNullException(nameof(handler));
			}
			if (evt == null) {
				throw new ArgumentNullException(nameof(evt));
			}

			return OnEvent(handler, evt);
		}

		#endregion


		#region methods - patterns

		private bool ScanItems<T>(IReflectionHandler handler, IEnumerable<T> items, Action<IReflectionHandler, IReadOnlyCollection<T>> onScanning, Func<IReflectionHandler, T, bool> scan, Action<IReflectionHandler, IReadOnlyCollection<T>, bool, Exception> onScanned) {
			// check arguments
			Debug.Assert(handler != null);
			Debug.Assert(items != null);
			Debug.Assert(onScanning != null);
			Debug.Assert(scan != null);
			Debug.Assert(onScanned != null);

			// scan the collection
			T[] collection = items.ToArray();
			bool canceled = false;
			Exception error = null;
			onScanning(handler, collection);
			try {
				foreach (T item in collection) {
					canceled = scan(handler, item);
					if (canceled) {
						break;
					}
				}
			} catch (Exception exception) {
				canceled = true;
				error = exception;
				throw;
			} finally {
				onScanned(handler, collection, canceled, error);
			}

			return canceled;
		}

		private bool ScanItem<T>(IReflectionHandler handler, T item, Action<IReflectionHandler, T> onScanning, Action<IReflectionHandler, T> scan, Func<IReflectionHandler, T, Exception, bool> onScanned) where T : class {
			// check argument
			Debug.Assert(handler != null);
			Debug.Assert(item != null);
			Debug.Assert(onScanning != null);
			Debug.Assert(scan != null);
			Debug.Assert(onScanned != null);

			// scan the item
			bool cancelRequested = false;
			Exception error = null;
			onScanning(handler, item);
			try {
				scan(handler, item);
			} catch (Exception exception) {
				error = exception;
				throw;
			} finally {
				cancelRequested = onScanned(handler, item, error);
			}

			return cancelRequested;
		}

		#endregion


		#region overridables

		protected virtual void OnAssembliesScanning(IReflectionHandler handler, IReadOnlyCollection<Assembly> assemblies) {
			// check argument
			Debug.Assert(handler != null);
			Debug.Assert(assemblies != null);

			// call the handler
			handler.OnAssembliesScanning(assemblies);
		}

		protected virtual void OnAssembliesScanned(IReflectionHandler handler, IReadOnlyCollection<Assembly> assemblies, bool canceled, Exception error) {
			// check arguments
			Debug.Assert(handler != null);
			Debug.Assert(assemblies != null);
			// error can be null

			// call the handler
			handler.OnAssembliesScanned(assemblies, canceled, error);
		}

		protected virtual void OnAssemblyScanning(IReflectionHandler handler, Assembly assembly) {
			// check argument
			Debug.Assert(handler != null);
			Debug.Assert(assembly != null);

			// call the handler
			handler.OnAssemblyScanning(assembly);
		}

		protected virtual bool OnAssemblyScanned(IReflectionHandler handler, Assembly assembly, Exception error) {
			// check arguments
			Debug.Assert(handler != null);
			Debug.Assert(assembly != null);
			// error can be null

			// call the handler
			return handler.OnAssemblyScanned(assembly, error);
		}

		protected virtual void OnTypesScanning(IReflectionHandler handler, IReadOnlyCollection<Type> types) {
			// check argument
			Debug.Assert(handler != null);
			Debug.Assert(types != null);

			// call the handler
			handler.OnTypesScanning(types);
		}

		protected virtual void OnTypesScanned(IReflectionHandler handler, IReadOnlyCollection<Type> types, bool canceled, Exception error) {
			// check arguments
			Debug.Assert(handler != null);
			Debug.Assert(types != null);
			// error can be null

			// call the handler
			handler.OnTypesScanned(types, canceled, error);
		}

		protected virtual void OnTypeScanning(IReflectionHandler handler, Type type) {
			// check argument
			Debug.Assert(handler != null);
			Debug.Assert(type != null);

			// call the handler
			handler.OnTypeScanning(type);
		}

		protected virtual bool OnTypeScanned(IReflectionHandler handler, Type type, Exception error) {
			// check arguments
			Debug.Assert(handler != null);
			Debug.Assert(type != null);
			// error can be null

			// call the handler
			return handler.OnTypeScanned(type, error);
		}

		protected virtual void OnFieldsScanning(IReflectionHandler handler, IReadOnlyCollection<FieldInfo> fields) {
			// check argument
			Debug.Assert(handler != null);
			Debug.Assert(fields != null);

			// call the handler
			handler.OnFieldsScanning(fields);
		}

		protected virtual void OnFieldsScanned(IReflectionHandler handler, IReadOnlyCollection<FieldInfo> fields, bool canceled, Exception error) {
			// check arguments
			Debug.Assert(handler != null);
			Debug.Assert(fields != null);
			// error can be null

			// call the handler
			handler.OnFieldsScanned(fields, canceled, error);
		}

		protected virtual bool OnField(IReflectionHandler handler, FieldInfo field) {
			// check argument
			Debug.Assert(handler != null);
			Debug.Assert(field != null);

			// call the handler
			return handler.OnField(field);
		}

		protected virtual void OnPropertiesScanning(IReflectionHandler handler, IReadOnlyCollection<PropertyInfo> props) {
			// check argument
			Debug.Assert(handler != null);
			Debug.Assert(props != null);

			// call the handler
			handler.OnPropertiesScanning(props);
		}

		protected virtual void OnPropertiesScanned(IReflectionHandler handler, IReadOnlyCollection<PropertyInfo> props, bool canceled, Exception error) {
			// check arguments
			Debug.Assert(handler != null);
			Debug.Assert(props != null);
			// error can be null

			// call the handler
			handler.OnPropertiesScanned(props, canceled, error);
		}

		protected virtual bool OnProperty(IReflectionHandler handler, PropertyInfo prop) {
			// check argument
			Debug.Assert(handler != null);
			Debug.Assert(prop != null);

			// call the handler
			return handler.OnProperty(prop);
		}

		protected virtual void OnConstructorsScanning(IReflectionHandler handler, IReadOnlyCollection<ConstructorInfo> ctors) {
			// check argument
			Debug.Assert(handler != null);
			Debug.Assert(ctors != null);

			// call the handler
			handler.OnConstructorsScanning(ctors);
		}

		protected virtual void OnConstructorsScanned(IReflectionHandler handler, IReadOnlyCollection<ConstructorInfo> ctors, bool canceled, Exception error) {
			// check arguments
			Debug.Assert(handler != null);
			Debug.Assert(ctors != null);
			// error can be null

			// call the handler
			handler.OnConstructorsScanned(ctors, canceled, error);
		}

		protected virtual bool OnConstructor(IReflectionHandler handler, ConstructorInfo ctor) {
			// check argument
			Debug.Assert(handler != null);
			Debug.Assert(ctor != null);

			// call the handler
			return handler.OnConstructor(ctor);
		}

		protected virtual void OnMethodsScanning(IReflectionHandler handler, IReadOnlyCollection<MethodInfo> methods) {
			// check argument
			Debug.Assert(handler != null);
			Debug.Assert(methods != null);

			// call the handler
			handler.OnMethodsScanning(methods);
		}

		protected virtual void OnMethodsScanned(IReflectionHandler handler, IReadOnlyCollection<MethodInfo> methods, bool canceled, Exception error) {
			// check arguments
			Debug.Assert(handler != null);
			Debug.Assert(methods != null);
			// error can be null

			// call the handler
			handler.OnMethodsScanned(methods, canceled, error);
		}

		protected virtual bool OnMethod(IReflectionHandler handler, MethodInfo method) {
			// check argument
			Debug.Assert(handler != null);
			Debug.Assert(method != null);

			// call the handler
			return handler.OnMethod(method);
		}

		protected virtual void OnEventsScanning(IReflectionHandler handler, IReadOnlyCollection<EventInfo> events) {
			// check argument
			Debug.Assert(handler != null);
			Debug.Assert(events != null);

			// call the handler
			handler.OnEventsScanning(events);
		}

		protected virtual void OnEventsScanned(IReflectionHandler handler, IReadOnlyCollection<EventInfo> events, bool canceled, Exception error) {
			// check arguments
			Debug.Assert(handler != null);
			Debug.Assert(events != null);
			// error can be null

			// call the handler
			handler.OnEventsScanned(events, canceled, error);
		}

		protected virtual bool OnEvent(IReflectionHandler handler, EventInfo evt) {
			// check argument
			Debug.Assert(handler != null);
			Debug.Assert(evt != null);

			// call the handler
			return handler.OnEvent(evt);
		}

		#endregion
	}
}
