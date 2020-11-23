using System;
using System.Diagnostics;
using System.IO;
using static DotNetAPIScanner.Constants;


namespace DotNetAPIScanner.Checker {
	public class CheckReport {
		#region data

		public readonly bool IsProblem;

		public readonly string Assembly;

		public readonly string Type;

		public readonly string Member;

		public readonly string MemberKind;

		public readonly int TypeParameterCount;

		public readonly string Overload;

		public readonly string Point;

		public readonly string InSource;

		public readonly string InTarget;

		public readonly string Remark;

		#endregion


		#region initialization & disposal

		public CheckReport(bool isProblem, string assembly, string type, string member, string memberKind, int typeParameterCount, string overload, string point, string inSource, string inTarget, string remark) {
			// check argument
			void normalizeNull(ref string val) {
				if (val == null) {
					val = string.Empty;
				}
			}
			normalizeNull(ref assembly);
			normalizeNull(ref type);
			normalizeNull(ref member);
			normalizeNull(ref memberKind);
			if (typeParameterCount < -1) {
				throw new ArgumentOutOfRangeException(nameof(typeParameterCount));
			}
			normalizeNull(ref overload);
			normalizeNull(ref point);
			normalizeNull(ref inSource);
			normalizeNull(ref inTarget);
			normalizeNull(ref remark);

			// initialize member
			this.IsProblem = isProblem;
			this.Assembly = assembly;
			this.Type = type;
			this.Member = member;
			this.MemberKind = memberKind;
			this.TypeParameterCount = typeParameterCount;
			this.Overload = overload;
			this.Point = point;
			this.InSource = inSource;
			this.InTarget = inTarget;
			this.Remark = remark;
		}

		#endregion


		#region methods

		public static string GetHeaderLine() {
			return "is problem,assembly,type,member,member kind,type parameter count,overload,point,in source env:,in target env:,remark";
		}

		public void WriteTo(TextWriter writer, bool appendNewLine) {
			bool first = true;
			void write(string value) {
				if (first) {
					first = false;
				} else {
					writer.Write(",");
				}
				writer.Write('"');
				writer.Write(value);
				writer.Write('"');
			}

			// write members
			write(this.IsProblem? Misc.Problem: Misc.Information);
			write(this.Assembly);
			write(this.Type);
			write(this.Member);
			write(this.MemberKind);
			write((0 <= this.TypeParameterCount) ? this.TypeParameterCount.ToString() : string.Empty);
			write(this.Overload);
			write(this.Point);
			write(this.InSource);
			write(this.InTarget);
			write(this.Remark);

			// write new line if necessary
			if (appendNewLine) {
				writer.WriteLine();
			}
		}

		#endregion


		#region overrides

		public override string ToString() {
			using (StringWriter writer = new StringWriter()) {
				WriteTo(writer, appendNewLine: false);
				return writer.ToString();
			}
		}

		#endregion
	}
}
