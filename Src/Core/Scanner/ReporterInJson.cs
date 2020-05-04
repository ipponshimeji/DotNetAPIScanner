using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using static DotNetAPIScanner.Constants;


namespace DotNetAPIScanner.Scanner {
	public class ReporterInJson: ReporterInText {
		#region types

		protected abstract class Scope: IDisposable {
			#region data

			public readonly ReporterInJson Owner;

			public readonly Scope Parent;

			private bool firstItem = true;

			#endregion


			#region properties

			protected TextWriter Writer {
				get {
					return this.Owner.Writer;
				}
			}

			#endregion


			#region initialization & disposal

			public Scope(ReporterInJson owner, Scope parent) {
				// check arguments
				if (owner == null) {
					throw new ArgumentNullException(nameof(owner));
				}
				// parent can be null

				// initialize members
				this.Owner = owner;
				this.Parent = parent;
			}

			public abstract void Dispose();

			#endregion


			#region methods

			protected void Write(string value) {
				this.Writer.Write(value);
			}

			protected void WriteQuotedValue(string value) {
				// check argument
				if (value == null) {
					value = string.Empty;
				}

				// Note that this method does not escape special characters in value.
				TextWriter writer = this.Writer;
				writer.Write("\"");
				writer.Write(value);
				writer.Write("\"");
			}

			protected void WriteNull() {
				Write("null");
			}

			protected void WriteString(string value) {
				WriteQuotedValue(value);
			}

			protected void WriteNumber(long value) {
				this.Writer.Write(value);
			}

			protected void WriteNumber(double value) {
				this.Writer.Write(value);
			}

			protected void WriteBoolean(bool value) {
				this.Writer.Write(value? "true": "false");
			}


			protected void OnItemWriting(bool endOfItem) {
				OnItemWriting(endOfItem, this.firstItem);
				this.firstItem = false;
			}

			#endregion


			#region overridables

			protected abstract void OnItemWriting(bool endOfItem, bool firstItem);

			public virtual void WriteItemHead() {
				OnItemWriting(endOfItem: false);
			}

			public virtual void WriteItem(string value) {
				WriteItemHead();
				if (value == null) {
					WriteNull();
				} else {
					WriteString(value);
				}
			}

			public virtual void WriteItem(long value) {
				WriteItemHead();
				WriteNumber(value);
			}

			public virtual void WriteItem(double value) {
				WriteItemHead();
				WriteNumber(value);
			}

			public virtual void WriteItem(bool value) {
				WriteItemHead();
				WriteBoolean(value);
			}

			public virtual void WriteItemHead(string name) {
				throw new NotSupportedException();
			}

			public virtual void WriteItem(string name, string value) {
				throw new NotSupportedException();
			}

			public virtual void WriteItem(string name, long value) {
				throw new NotSupportedException();
			}

			public virtual void WriteItem(string name, double value) {
				throw new NotSupportedException();
			}

			public virtual void WriteItem(string name, bool value) {
				throw new NotSupportedException();
			}

			#endregion
		}

		protected class RootScope: Scope {
			#region initialization & disposal

			public RootScope(ReporterInJson owner): base(owner, null) {
			}

			public override void Dispose() {
			}

			#endregion


			#region overrides

			protected override void OnItemWriting(bool endOfItem, bool firstItem) {
				if (firstItem == false && endOfItem == false) {
					throw new InvalidOperationException("Only one item can be written.");
				}
			}

 			#endregion
		}

		protected class ArrayScope: Scope {
			#region constants

			public const string ItemMarker = "- ";

			#endregion


			#region initialization & disposal

			public ArrayScope(ReporterInJson owner, Scope parent): base(owner, parent) {
				// check argument
				Debug.Assert(parent != null);

				// open its scope
				this.Writer.Write("[");
				owner.Indent();
			}

			public override void Dispose() {
				// close its scope
				this.Owner.Unindent();
				OnItemWriting(endOfItem: true);
				Write("]");
			}

			#endregion


			#region overrides

			protected override void OnItemWriting(bool endOfItem, bool firstItem) {
				if (firstItem == false && endOfItem == false) {
					this.Writer.Write(",");
				}
				this.Writer.WriteLine();
				this.Owner.WriteIndent();
			}

			#endregion
		}

		protected class ObjectScope: Scope {
			#region constants

			public const string Separator = ": ";

			#endregion


			#region initialization & disposal

			public ObjectScope(ReporterInJson owner, Scope parent) : base(owner, parent) {
				// check argument
				Debug.Assert(parent != null);

				// open its scope
				Write("{");
				owner.Indent();
			}

			public override void Dispose() {
				// close its scope
				this.Owner.Unindent();
				OnItemWriting(endOfItem: true);
				Write("}");
			}

			#endregion


			#region overrides

			protected override void OnItemWriting(bool endOfItem, bool firstItem) {
				if (firstItem == false && endOfItem == false) {
					this.Writer.Write(",");
				}
				this.Writer.WriteLine();
				this.Owner.WriteIndent();
			}

			public override void WriteItemHead() {
				throw new NotSupportedException();
			}

			public override void WriteItem(string value) {
				throw new NotSupportedException();
			}

			public override void WriteItem(long value) {
				throw new NotSupportedException();
			}

			public override void WriteItem(double value) {
				throw new NotSupportedException();
			}

			public override void WriteItem(bool value) {
				throw new NotSupportedException();
			}

			public override void WriteItemHead(string name) {
				// check argument
				if (name == null) {
					throw new ArgumentNullException(nameof(name));
				}

				OnItemWriting(endOfItem: false);
				WriteQuotedValue(name);
				Write(Separator);
			}

			public override void WriteItem(string name, string value) {
				WriteItemHead(name);
				WriteString(value);
			}

			public override void WriteItem(string name, long value) {
				WriteItemHead(name);
				WriteNumber(value);
			}

			public override void WriteItem(string name, double value) {
				WriteItemHead(name);
				WriteNumber(value);
			}

			public override void WriteItem(string name, bool value) {
				WriteItemHead(name);
				WriteBoolean(value);
			}

			#endregion
		}

		#endregion


		#region data

		private Scope currentScope = null;

		#endregion


		#region properties

		protected Scope CurrentScope {
			get {
				return this.currentScope;
			}
		}

		#endregion


		#region initialization & disposal

		public ReporterInJson(TextWriter writer, bool disposeWriterOnDispose = true) : base(writer, disposeWriterOnDispose) {
			// initialize member
			this.currentScope = new RootScope(this);
		}

		public override void Dispose() {
			// dispose the scope
			Scope scope = this.currentScope;
			this.currentScope = null;
			while (scope != null) {
				Scope nextScope = scope.Parent;
				scope.Dispose();
				scope = nextScope;
			}

			// dispose the base class level
			base.Dispose();
		}

		#endregion


		#region methods

		private void OpenScope(Scope scope) {
			Debug.Assert(scope != null);
			this.currentScope = scope;
		}

		protected void OpenArray() {
			EnsureNotDisposed().WriteItemHead();
			OpenScope(new ArrayScope(this, this.currentScope));
		}

		protected void OpenArray(string name) {
			EnsureNotDisposed().WriteItemHead(name);
			OpenScope(new ArrayScope(this, this.currentScope));
		}

		protected void OpenObject() {
			EnsureNotDisposed().WriteItemHead();
			OpenScope(new ObjectScope(this, this.currentScope));
		}

		protected void OpenObject(string name) {
			EnsureNotDisposed().WriteItemHead(name);
			OpenScope(new ObjectScope(this, this.currentScope));
		}

		private void CloseScope() {
			Scope scope = this.currentScope;
			if (scope != null) {
				this.currentScope = scope.Parent;
				scope.Dispose();
			}
		}

		protected void CloseArray() {
			CloseScope();
		}

		protected void CloseObject() {
			CloseScope();
		}


		protected Scope EnsureNotDisposed() {
			Scope scope = this.currentScope;
			if (scope == null) {
				throw new ObjectDisposedException(null);
			}

			return scope;
		}

		protected void WriteItem(string value) {
			EnsureNotDisposed().WriteItem(value);			
		}

		protected void WriteItem(long value) {
			EnsureNotDisposed().WriteItem(value);
		}

		protected void WriteItem(double value) {
			EnsureNotDisposed().WriteItem(value);
		}

		protected void WriteItem(bool value) {
			EnsureNotDisposed().WriteItem(value);
		}

		protected void WriteItem(string name, string value) {
			EnsureNotDisposed().WriteItem(name, value);
		}

		protected void WriteItem(string name, long value) {
			EnsureNotDisposed().WriteItem(name, value);
		}

		protected void WriteItem(string name, double value) {
			EnsureNotDisposed().WriteItem(name, value);
		}

		protected void WriteItem(string name, bool value) {
			EnsureNotDisposed().WriteItem(name, value);
		}

		#endregion


		#region IReflectionHandler

		public override void OnAssembliesScanning(IReadOnlyCollection<Assembly> assemblies) {
			// start object
			OpenObject();
			WriteItem(PropNames.Framework, RuntimeInformation.FrameworkDescription);
			OpenArray(PropNames.Assemblies);
		}

		public override void OnAssembliesScanned(IReadOnlyCollection<Assembly> assemblies, bool canceled, Exception error) {
			// end object
			CloseArray();	// assemblies
			CloseObject();	// (root)
		}

		public override void OnAssemblyScanning(Assembly assembly) {
			// check argument
			if (assembly == null) {
				throw new ArgumentNullException(nameof(assembly));
			}

			OpenObject();
			WriteItem(PropNames.Name, assembly.FullName);
			OpenArray(PropNames.Types);
		}

		public override bool OnAssemblyScanned(Assembly assembly, Exception error) {
			CloseArray();	// types
			CloseObject();

			return false;	// do not request to cancel
		}

		public override void OnTypeScanning(Type type) {
			// check arguments
			if (type == null) {
				throw new ArgumentNullException(nameof(type));
			}

			OpenObject();
			WriteItem(PropNames.Name, type.FullName);
			OpenArray(PropNames.Members);
		}

		public override bool OnTypeScanned(Type type, Exception error) {
			CloseArray();	// members
			CloseObject();

			return false;   // do not request to cancel
		}

		public override bool OnField(FieldInfo field) {
			// check argument
			if (field == null) {
				throw new ArgumentNullException(nameof(field));
			}

			// write field information
			OpenObject();
			try {
				WriteItem(PropNames.Kind, Kinds.Field);
				WriteItem(PropNames.Name, field.Name);
				WriteItem(PropNames.Type, Util.GetTypeDisplayName(field.FieldType));
			} finally {
				CloseObject();
			}

			return false;   // do not request to cancel
		}

		public override bool OnConstructor(ConstructorInfo ctor) {
			// check argument
			if (ctor == null) {
				throw new ArgumentNullException(nameof(ctor));
			}

			// write field information
			OpenObject();
			try {
				WriteConstructorInfo(ctor);
			} finally {
				CloseObject();
			}

			return false;   // do not request to cancel
		}

		private void WriteMethodBaseInfo(MethodBase method) {
			// check argument
			Debug.Assert(method != null);

			OpenArray(PropNames.Parameters);
			try {
				foreach (ParameterInfo p in method.GetParameters()) {
					OpenObject();
					try {
						WriteItem(PropNames.Type, Util.GetTypeDisplayName(p.ParameterType));
					} finally {
						CloseObject();
					}
				}
			} finally {
				CloseArray();
			}
		}

		private void WriteConstructorInfo(ConstructorInfo ctor) {
			// check argument
			Debug.Assert(ctor != null);

			WriteItem(PropNames.Kind, Kinds.Constructor);
			// Note that static constructor does not appear here because it is not a public interface.
			Debug.Assert(ctor.IsStatic == false);
			WriteItem(PropNames.Name, Misc.ConstructorName);
			WriteMethodBaseInfo(ctor);
		}

		public override bool OnMethod(MethodInfo method) {
			// check argument
			if (method == null) {
				throw new ArgumentNullException(nameof(method));
			}

			// write method information
			OpenObject();
			try {
				WriteMethodInfo(method);
			} finally {
				CloseObject();
			}

			return false;   // do not request to cancel
		}

		private void WriteMethodInfo(MethodInfo method) {
			// check argument
			Debug.Assert(method != null);

			WriteItem(PropNames.Kind, Kinds.Method);
			WriteItem(PropNames.Name, method.Name);
			WriteItem(PropNames.TypeParameterCount, method.GetGenericArguments().Length);
			WriteItem(PropNames.ReturnType, Util.GetTypeDisplayName(method.ReturnType));
			WriteMethodBaseInfo(method);
		}

		#endregion
	}
}
