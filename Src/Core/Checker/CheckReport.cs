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

		public readonly string Point;

		public readonly string InSource;

		public readonly string InTarget;

		public readonly string Remark;

		#endregion


		#region initialization & disposal

		public CheckReport(bool isProblem, string assembly, string type, string member, string point, string inSource, string inTarget, string remark) {
			// check argument
			void normalizeNull(ref string val) {
				if (val == null) {
					val = string.Empty;
				}
			}
			normalizeNull(ref assembly);
			normalizeNull(ref type);
			normalizeNull(ref member);
			normalizeNull(ref point);
			normalizeNull(ref inSource);
			normalizeNull(ref inTarget);
			normalizeNull(ref remark);

			// initialize member
			this.IsProblem = isProblem;
			this.Assembly = assembly;
			this.Type = type;
			this.Member = member;
			this.Point = point;
			this.InSource = inSource;
			this.InTarget = inTarget;
			this.Remark = remark;
		}

		#endregion


		#region methods

		public static string GetHeaderLine() {
			return "is problem,assembly,type,member,point,in source:,in target:,remark";
		}

		public void WriteTo(TextWriter writer, bool quote, bool appendNewLine) {
			bool first = true;
			void write(string value) {
				if (first) {
					first = false;
				} else {
					writer.Write(",");
				}
				if (quote) {
					writer.Write('"');
					writer.Write(value);
					writer.Write('"');
				} else {
					writer.Write(value);
				}
			}

			// write members
			write(this.IsProblem? Misc.Problem: Misc.Information);
			write(this.Assembly);
			write(this.Type);
			write(this.Member);
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
				WriteTo(writer, quote: false, appendNewLine: false);
				return writer.ToString();
			}
		}

		#endregion
	}
}
