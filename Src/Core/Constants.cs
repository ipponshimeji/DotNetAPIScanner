using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;


namespace DotNetAPIScanner {
	public static class Constants {
		#region types

		public static class PropNames {
			#region constants

			public const string Assemblies = "assemblies";

			public const string Framework = "framework";

			public const string Kind = "kind";

			public const string Members = "members";

			public const string Name = "name";

			public const string Parameters = "parameters";

			public const string Type = "type";

			public const string Types = "types";

			public const string Version = "version";

			#endregion
		}

		public static class Kinds {
			#region constants

			public const string Class = "class";

			public const string Constructor = "constructor";

			public const string Delegate = "delegate";

			public const string Enum = "enum";

			public const string Field = "field";

			public const string Interface = "interface";

			public const string Method = "method";

			public const string Struct = "struct";

			#endregion
		}

		public static class Points {
			#region constants

			public const string Existence = "existence";

			public const string Type = "type";

			#endregion
		}

		public static class Misc {
			#region constants

			public const string ConstructorName = ".ctor";

//			public const string StaticConstructorName = ".cctor";

			public const string Problem = "PROBLEM";

			public const string Information = "INFO";

			#endregion
		}

		#endregion
	}
}
