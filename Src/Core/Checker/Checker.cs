using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using static DotNetAPIScanner.Constants;


namespace DotNetAPIScanner.Checker {
	public class Checker {
		#region types

		protected struct NamedObject {
			#region data

			public static readonly NamedObject Null = new NamedObject();


			public readonly string Name;

			public readonly IReadOnlyDictionary<string, object> Object;

			#endregion


			#region initialization & disposal

			public NamedObject(IReadOnlyDictionary<string, object> obj) {
				// check argument
				if (obj == null) {
					throw new ArgumentNullException(nameof(obj));
				}

				// initialize members
				this.Name = Checker.GetIndispensableValue<string>(obj, PropNames.Name);
				this.Object = obj;
			}

			#endregion


			#region methods

			public T GetIndispensableValue<T>(string key) {
				// check state
				if (this.Object == null) {
					throw new InvalidOperationException();
				}

				return Checker.GetIndispensableValue<T>(this.Object, key);
			}

			public IEnumerable<NamedObject> GetChildNamedObjects(string key, bool sort) {
				// check state
				if (this.Object == null) {
					throw new InvalidOperationException();
				}

				return Checker.GetChildNamedObjects(this.Object, key, sort);
			}

			#endregion
		}

		#endregion


		#region constants

		protected const BindingFlags DefaultBindingFlags = (
			BindingFlags.DeclaredOnly
			| BindingFlags.Public
			| BindingFlags.NonPublic
			| BindingFlags.Instance
			| BindingFlags.Static
		);

		#endregion


		#region data

		public readonly bool Sort = true;

		protected IReadOnlyDictionary<string, object> Source { get; private set; } = null;

		protected NamedObject SourceAssembly { get; private set; } = NamedObject.Null;

		protected Assembly TargetAssembly { get; private set; } = null;

		protected NamedObject SourceType { get; private set; } = NamedObject.Null;

		protected Type TargetType { get; private set; } = null;

		protected NamedObject SourceMember { get; private set; } = NamedObject.Null;

		protected MemberInfo TargetMember { get; private set; } = null;


		#endregion


		#region events

		public event EventHandler<CheckEventArgs> Report = null;

		#endregion


		#region initialization & disposal

		public Checker(IReadOnlyDictionary<string, object> source, bool sort = true) {
			// check argument
			if (source == null) {
				throw new ArgumentNullException(nameof(source));
			}

			// initialize member
			this.Sort = sort;
			this.Source = source;
		}

		#endregion


		#region methods

		public int Check() {
			CheckAssemblies(this.Source);

			return 0;	// TODO: return 1 if it reports at least one problem
		}


		protected static T GetIndispensableValue<T>(IReadOnlyDictionary<string, object> dictionary, string key) {
			// check argument
			if (dictionary == null) {
				throw new ArgumentNullException(nameof(dictionary));
			}

			object orgValue;
			if (dictionary.TryGetValue(key, out orgValue) == false) {
				throw new KeyNotFoundException($"The indispensable value for key '{key}' is missing.");
			}
			if (!(orgValue is T)) {
				throw new InvalidCastException($"The type of the indispensable value for key '{key}' is not '{typeof(T).FullName}'.");
			}

			return (T)orgValue;
		}

		protected static IEnumerable<NamedObject> GetNamedObjects(IReadOnlyList<object> array, bool sort) {
			// check argument
			if (array == null) {
				throw new ArgumentNullException(nameof(array));
			}

			var objects = array.Select(item => (item as IReadOnlyDictionary<string, object>))
							   .Where(item => (item != null))
							   .Select(item => new NamedObject(item));
			if (sort) {
				objects = objects.OrderBy(item => item.Name, StringComparer.Ordinal);
			}

			return objects;
		}

		protected static IEnumerable<NamedObject> GetChildNamedObjects(IReadOnlyDictionary<string, object> dictionary, string key, bool sort) {
			// check argument
			if (dictionary == null) {
				throw new ArgumentNullException(nameof(dictionary));
			}
			if (key == null) {
				throw new ArgumentNullException(nameof(key));
			}

			// get child array
			IReadOnlyList<object> array = GetIndispensableValue<IReadOnlyList<object>>(dictionary, key);
			if (array == null || array.Count == 0) {
				return null;
			}

			// get NamedObject enumerable from the array
			return GetNamedObjects(array, sort);
		}

		protected CheckReport CreateReport(string point, string inSource, string inTarget, string remark = null, bool isProblem = true) {
			return new CheckReport(
				isProblem,
				this.SourceAssembly.Name,
				this.SourceType.Name,
				this.SourceMember.Name,
				point,
				inSource,
				inTarget,
				remark
			);
		}

		protected CheckReport CreateExistenceReport(string remark = null) {
			return CreateReport(Points.Existence, "exists", "does not exist", remark, true);
		}

		#endregion


		#region overrides

		protected virtual void OnReport(CheckReport report) {
			// check argument
			if (report == null) {
				throw new ArgumentNullException(nameof(report));
			}

			// fire Report event
			EventHandler<CheckEventArgs> handler = this.Report;
			if (handler != null) {
				CheckEventArgs e = new CheckEventArgs(report);
				try {
					handler(this, e);
				} catch {
					// continue (not fatal)
				}
			}
		}


		protected virtual void CheckAssemblies(IReadOnlyDictionary<string, object> source) {
			// check argument
			if (source == null) {
				throw new ArgumentNullException(nameof(source));
			}

			// check each assembly
			IEnumerable<NamedObject> sourceAssemblies = GetChildNamedObjects(source, PropNames.Assemblies, this.Sort);
			if (sourceAssemblies != null) {
				foreach (NamedObject sourceAssembly in sourceAssemblies) {
					this.SourceAssembly = sourceAssembly;
					try {
						CheckAssembly(sourceAssembly);
					} finally {
						this.SourceAssembly = NamedObject.Null;
					}
				}
			}
		}

		protected virtual void CheckAssembly(NamedObject sourceAssembly) {
			// check argument
			if (sourceAssembly.Object == null) {
				throw new ArgumentNullException(nameof(sourceAssembly));
			}

			// check existence
			Assembly targetAssembly;
			try {
				targetAssembly = Assembly.Load(sourceAssembly.Name);
			} catch (IOException) {
				// target assembly not found
				OnReport(CreateExistenceReport());
				return;
			}

			// check types
			IEnumerable<NamedObject> sourceTypes = sourceAssembly.GetChildNamedObjects(PropNames.Types, this.Sort);
			if (sourceTypes != null) {
				this.TargetAssembly = targetAssembly;
				try {
					foreach (NamedObject sourceType in sourceTypes) {
						this.SourceType = sourceType;
						try {
							CheckType(sourceType);
						} finally {
							this.SourceType = NamedObject.Null;
						}
					}
				} finally {
					this.TargetAssembly = null;
				}
			}
		}

		protected virtual void CheckType(NamedObject sourceType) {
			// check argument
			if (sourceType.Object == null) {
				throw new ArgumentNullException(nameof(sourceType));
			}

			// state checks
			Debug.Assert(this.TargetAssembly != null);

			// check existence
			Type targetType = this.TargetAssembly.GetType(sourceType.Name);
			if (targetType == null) {
				// target type not found
				OnReport(CreateExistenceReport());
				return;
			}

			// check members
			IEnumerable<NamedObject> sourceMembers = sourceType.GetChildNamedObjects(PropNames.Members, this.Sort);
			if (sourceMembers != null) {
				this.TargetType = targetType;
				try {
					foreach (NamedObject sourceMember in sourceMembers) {
						this.SourceMember = sourceMember;
						try {
							CheckMember(sourceMember);
						} finally {
							this.SourceMember = NamedObject.Null;
						}
					}
				} finally {
					this.TargetType = null;
				}
			}
		}

		protected virtual void CheckMember(NamedObject sourceMember) {
			// check argument
			if (sourceMember.Object == null) {
				throw new ArgumentNullException(nameof(sourceMember));
			}

			// check depends on kind of the member
			string kind = sourceMember.GetIndispensableValue<string>(PropNames.Kind);
			switch (kind) {
				case Kinds.Field:
				case Kinds.Constructor:
				case Kinds.Method:
					break;
				default:
					throw new ArgumentException($"Unknown value for '{PropNames.Kind}' property: {kind}", nameof(sourceMember));
			}
		}

		protected virtual void CheckField(NamedObject sourceField) {
			// check argument
			if (sourceField.Object == null) {
				throw new ArgumentNullException(nameof(sourceField));
			}

			// check existence
			FieldInfo targetField = this.TargetType.GetField(sourceField.Name, DefaultBindingFlags);
			if (targetField == null) {
				// target field not found
				OnReport(CreateExistenceReport());
				return;
			}
		}

		#endregion
	}
}
