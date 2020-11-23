using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace DotNetAPIScanner.Scanning {
	public class ReporterInIndentedText: ReporterInText {
		#region initialization & disposal

		public ReporterInIndentedText(TextWriter writer, bool disposeWriterOnDispose = true) : base(writer, disposeWriterOnDispose) {
		}

		#endregion


		#region IReflectionHandler

		public override void OnAssemblyScanning(Assembly assembly) {
			// check argument
			if (assembly == null) {
				throw new ArgumentNullException(nameof(assembly));
			}

			WriteLine(assembly.FullName);
			Indent();
		}

		public override bool OnAssemblyScanned(Assembly assembly, Exception error) {
			Unindent();

			return false;	// do not request to cancel
		}

		public override void OnTypeScanning(Type type) {
			// check arguments
			if (type == null) {
				throw new ArgumentNullException(nameof(type));
			}

			WriteLine(type.FullName);
			Indent();
		}

		public override bool OnTypeScanned(Type type, Exception error) {
			Unindent();

			return false;   // do not request to cancel
		}

		public override void OnFieldsScanning(IReadOnlyCollection<FieldInfo> fields) {
			// check arguments
			if (fields == null) {
				throw new ArgumentNullException(nameof(fields));
			}

			if (0 < fields.Count) {
				WriteLine("[Fields]: ");
				Indent();
			}
		}

		public override void OnFieldsScanned(IReadOnlyCollection<FieldInfo> fields, bool canceled, Exception error) {
			// check arguments
			if (fields == null) {
				throw new ArgumentNullException(nameof(fields));
			}

			if (0 < fields.Count) {
				Unindent();
			}
		}

		public override bool OnField(FieldInfo field) {
			// check argument
			if (field == null) {
				throw new ArgumentNullException(nameof(field));
			}

			// write field information
			WriteLine(field.Name);
			Indent();
			try {
				WriteLine($"[Type]: {Util.GetTypeDisplayName(field.FieldType)}");
			} finally {
				Unindent();
			}

			return false;   // do not request to cancel
		}

		public override void OnConstructorsScanning(IReadOnlyCollection<ConstructorInfo> ctors) {
			// check arguments
			if (ctors == null) {
				throw new ArgumentNullException(nameof(ctors));
			}

			if (0 < ctors.Count) {
				WriteLine("[Constructors]: ");
				Indent();
			}
		}

		public override void OnConstructorsScanned(IReadOnlyCollection<ConstructorInfo> ctors, bool canceled, Exception error) {
			// check arguments
			if (ctors == null) {
				throw new ArgumentNullException(nameof(ctors));
			}

			if (0 < ctors.Count) {
				Unindent();
			}
		}

		public override bool OnConstructor(ConstructorInfo ctor) {
			// check argument
			if (ctor == null) {
				throw new ArgumentNullException(nameof(ctor));
			}

			// write constructor information
			WriteLine(ctor.Name);
			Indent();
			try {
				WriteConstructorInfo(ctor);
			} finally {
				Unindent();
			}

			return false;   // do not request to cancel
		}

		private void WriteMethodBaseInfo(MethodBase method) {
			// check argument
			Debug.Assert(method != null);

			WriteLine("[Parameters]: ");
			Indent();
			try {
				foreach (ParameterInfo p in method.GetParameters()) {
					WriteLine(Util.GetTypeDisplayName(p.ParameterType));
				}
			} finally {
				Unindent();
			}
		}

		private void WriteConstructorInfo(ConstructorInfo ctor) {
			// check argument
			Debug.Assert(ctor != null);

			WriteMethodBaseInfo(ctor);
		}

		public override void OnMethodsScanning(IReadOnlyCollection<MethodInfo> methods) {
			// check arguments
			if (methods == null) {
				throw new ArgumentNullException(nameof(methods));
			}

			if (0 < methods.Count) {
				WriteLine("[Methods]: ");
				Indent();
			}
		}

		public override void OnMethodsScanned(IReadOnlyCollection<MethodInfo> methods, bool canceled, Exception error) {
			// check arguments
			if (methods == null) {
				throw new ArgumentNullException(nameof(methods));
			}

			if (0 < methods.Count) {
				Unindent();
			}
		}

		public override bool OnMethod(MethodInfo method) {
			// check argument
			if (method == null) {
				throw new ArgumentNullException(nameof(method));
			}

			// write method information
			WriteLine(method.Name);
			Indent();
			try {
				WriteMethodInfo(method);
			} finally {
				Unindent();
			}

			return false;   // do not request to cancel
		}

		private void WriteMethodInfo(MethodInfo method) {
			// check argument
			Debug.Assert(method != null);

			WriteMethodBaseInfo(method);
			WriteLine($"[ReturnType]: {Util.GetTypeDisplayName(method.ReturnType)}");
		}

		#endregion
	}
}
