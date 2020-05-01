using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Scanner {
	public class ReporterInIndentedText: ReporterInText {
		#region constants

		public const int DefaultIndentWidth = 2;

		public const int MaxIndentWidth = 32;

		public const int MaxIndentLevel = 128;

		#endregion


		#region data

		private int indentLevel = 0;

		private int indentWidth = DefaultIndentWidth;

		#endregion


		#region properties

		public int IndentWidth {
			get {
				return this.indentWidth;
			}
			set {
				// check argument
				if (value < 0 || MaxIndentWidth < value) {
					throw new ArgumentOutOfRangeException(nameof(value));
				}

				this.indentWidth = value;
			}
		}

		#endregion


		#region initialization & disposal

		public ReporterInIndentedText(TextWriter writer, bool disposeWriterOnDispose = true) : base(writer, disposeWriterOnDispose) {
		}

		#endregion


		#region methods

		protected void Indent() {
			// check state
			if (this.indentLevel < 0 || MaxIndentLevel <= this.indentLevel) {
				throw new InvalidOperationException();
			}

			++this.indentLevel;
		}

		protected void Unindent() {
			// check state
			if (this.indentLevel <= 0) {
				throw new InvalidOperationException();
			}

			--this.indentLevel;
		}

		protected void WriteLine(string value) {
			// check argument
			// line can be null

			// write spaces for indentation
			int len = this.indentLevel * this.IndentWidth;
			for (int i = 0; i < len; ++i) {
				this.Writer.Write(' ');
			}

			// write the value
			this.Writer.WriteLine(value);
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

			WriteLine(field.Name);
			Indent();
			try {
				WriteLine($"[Type]: {Util.GetTypeDisplayName(field.FieldType)}");
			} finally {
				Unindent();
			}
			return false;   // do not request to cancel
		}

		#endregion
	}
}
