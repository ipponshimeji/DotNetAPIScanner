using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using static DotNetAPIScanner.Constants;

namespace DotNetAPIScanner.Comparing {
	public class Comparer {
		#region types

		/// <summary>
		/// The struct to capsule JSON object (Dictionary<string, object>).
		/// The name is 'SourceObject' because JSON object is used to describe
		/// information in the source environment in Checker context.
		/// It caches Name property because the name is used commonly.
		/// </summary>
		protected struct SourceObject {
			#region data

			public static readonly SourceObject Null = new SourceObject();


			public readonly IReadOnlyDictionary<string, object> Object;

			public readonly string Name;

			#endregion


			#region properties

			public bool Valid {
				get {
					return this.Object != null;
				}
			}

			#endregion


			#region initialization & disposal

			public SourceObject(IReadOnlyDictionary<string, object> jsonObj, string name) {
				// check argument
				if (jsonObj == null) {
					throw new ArgumentNullException(nameof(jsonObj));
				}
				// name can be null

				// initialize members
				this.Name = name;
				this.Object = jsonObj;
			}

			// Note that an ArgumentNullException is thrown if jsonObj is not a IReadOnlyDictionary<string, object>.
			public SourceObject(object jsonObj, string name) : this(jsonObj as IReadOnlyDictionary<string, object>, name) {
			}

			// Note that a KeyNotFoundException is thrown if jsonObj does not have 'name' property.
			public SourceObject(IReadOnlyDictionary<string, object> jsonObj) : this(jsonObj, Comparer.GetIndispensableValue<string>(jsonObj, PropNames.Name)) {
			}

			// Note that an ArgumentNullException is thrown if jsonObj is not a IReadOnlyDictionary<string, object>.
			public SourceObject(object jsonObj): this(jsonObj as IReadOnlyDictionary<string, object>) {
			}

			#endregion


			#region methods

			public static ArgumentException CreateNotValidException(string paramName) {
				return new ArgumentException("It does not have valid JSON object.", paramName);
			}

			public static IEnumerable<SourceObject> ConvertToSourceObjectArray(IReadOnlyList<object> jsonArray, bool sort, bool withoutName) {
				// check argument
				if (jsonArray == null) {
					throw new ArgumentNullException(nameof(jsonArray));
				}

				// convert the json array to IEnumerable<SourceObject>
				Func<object, SourceObject> select;
				if (withoutName) {
					// give explicit null name at creating SourceObject
					select = (item => new SourceObject(item, null));
				} else {
					// item's name is get from its 'name' property
					select = (item => new SourceObject(item));
				}

				var objects = jsonArray.Select(select);
				if (sort && withoutName == false) {
					objects = objects.OrderBy(item => item.Name, StringComparer.Ordinal);
				}

				return objects;
			}


			public T GetIndispensableProperty<T>(string propName) {
				// check state
				if (this.Object == null) {
					throw new InvalidOperationException();
				}

				return Comparer.GetIndispensableValue<T>(this.Object, propName);
			}

			// Note that it returns null if the target child array is not found or empty.
			public IEnumerable<SourceObject> GetChildSourceObjects(string childName, bool sort = true, bool withoutName = false) {
				// check argument
				if (childName == null) {
					throw new ArgumentNullException(nameof(childName));
				}

				// check state
				if (this.Object == null) {
					throw new InvalidOperationException();
				}

				// get child array
				IReadOnlyList<object> array = GetIndispensableProperty<IReadOnlyList<object>>(childName);
				if (array == null || array.Count == 0) {
					return null;
				}

				// get SourceObject enumerable from the array
				return ConvertToSourceObjectArray(array, sort, withoutName);
			}

			#endregion
		}

		#endregion


		#region constants

		protected const BindingFlags TargetBindingFlags = (
			BindingFlags.DeclaredOnly
			| BindingFlags.Public
			| BindingFlags.NonPublic
			| BindingFlags.Instance
			| BindingFlags.Static
		);

		#endregion


		#region data

		public static readonly StringComparer NameComparer = StringComparer.Ordinal;


		private bool sort = true;

		#endregion


		#region data - checking state

		protected int ProblemCount { get; private set; } = 0;

		protected SourceObject SourceInfo { get; private set; } = SourceObject.Null;

		protected SourceObject SourceAssembly { get; private set; } = SourceObject.Null;

		protected Assembly TargetAssembly { get; private set; } = null;

		protected SourceObject SourceType { get; private set; } = SourceObject.Null;

		protected Type TargetType { get; private set; } = null;

		private ConstructorInfo[] targetTypeConstructors = null;

		private MethodInfo[] targetTypeMethods = null;

		protected SourceObject SourceMember { get; private set; } = SourceObject.Null;

		protected string SourceMemberKind { get; private set; } = null;

		protected int TypeParameterCount { get; private set; } = -1;

		protected IReadOnlyList<SourceObject> SourceParameters { get; private set; } = null;

		#endregion


		#region properties

		public bool Sort {
			get {
				return this.sort;
			}
			set {
				// check state
				// This property cannot be changed when the object is checking.
				EnsureNotBusy(propSetter: true);

				this.sort = value;
			}
		}


		protected bool Busy {
			get {
				return this.SourceInfo.Valid;
			}
		}

		protected IReadOnlyCollection<ConstructorInfo> TargetTypeConstructors {
			get {
				ConstructorInfo[] value = this.targetTypeConstructors;
				if (value == null) {
					// not cached yet
					// cache the value
					Type targetType = this.TargetType;
					if (targetType != null) {
						value = targetType.GetConstructors(TargetBindingFlags);
						if (value == null) {
							// not to call GetConstructors() again
							value = Array.Empty<ConstructorInfo>();
						}
						this.targetTypeConstructors = value;
					}
				}

				return value;
			}
		}

		protected IReadOnlyCollection<MethodInfo> TargetTypeMethods {
			get {
				MethodInfo[] value = this.targetTypeMethods;
				if (value == null) {
					// not cached yet
					// cache the value
					Type targetType = this.TargetType;
					if (targetType != null) {
						value = targetType.GetMethods(TargetBindingFlags);
						if (value == null) {
							// not to call GetMethods() again
							value = Array.Empty<MethodInfo>();
						}
						this.targetTypeMethods = value;
					}
				}

				return value;
			}
		}

		#endregion


		#region events

		public event EventHandler<CompareEventArgs> Report = null;

		#endregion


		#region initialization & disposal

		public Comparer() {
		}

		#endregion


		#region methods

		public int Check(IReadOnlyDictionary<string, object> sourceInfo) {
			// check argument
			if (sourceInfo == null) {
				throw new ArgumentNullException(nameof(sourceInfo));
			}

			// check state
			EnsureNotBusy(propSetter: false);
			Debug.Assert(this.SourceInfo.Valid == false);

			// check the given source info
			this.SourceInfo = new SourceObject(sourceInfo, name: null);
			this.ProblemCount = 0;
			try {
				CheckAssemblies(this.SourceInfo);
			} finally {
				this.SourceInfo = SourceObject.Null;
			}

			return this.ProblemCount;
		}

		#endregion


		#region methods - for derived classes

		protected void EnsureNotBusy(bool propSetter) {
			if (this.Busy) {
				string message = propSetter ? "This property cannot be changed when the checker is checking.": "The checker is checking.";
				throw new InvalidOperationException(message);
			}
		}

		// Note that getting null value cause an InvalidCastException.
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

		protected static bool IsConversionOperator(string name) {
			return NameComparer.Equals(name, Misc.ExplicitOperatorName) || NameComparer.Equals(name, Misc.ImplicitOperatorName);
		}

		protected string GetOverload() {
			// check state
			IReadOnlyList<SourceObject> sourceParams = this.SourceParameters;
			if (sourceParams == null) {
				return null;
			}

			// check state
			Debug.Assert(this.SourceMember.Valid);

			// list up types which define the overload
			string name = this.SourceMember.Name;
			if (NameComparer.Equals(name, Misc.ExplicitOperatorName) || NameComparer.Equals(name, Misc.ImplicitOperatorName)) {
				// op_Explicit or op_Implicit
				// they are overloaded by return type
				return this.SourceMember.GetIndispensableProperty<string>(PropNames.ReturnType);
			} else {
				// other methods are overloaded by parameter types
				// The case of 0 or 1 parameter is handled separetely for efficiency
				switch (sourceParams.Count) {
					case 0:
						return null;
					case 1:
						return sourceParams[0].GetIndispensableProperty<string>(PropNames.Type);
					default:
						StringBuilder buf = new StringBuilder();
						bool first = true;
						foreach (SourceObject param in sourceParams) {
							if (first) {
								first = false;
							} else {
								buf.Append(",");
							}
							buf.Append(param.GetIndispensableProperty<string>(PropNames.Type));
						}
						return buf.ToString();
				}
			}
		}

		protected ComparingReport CreateReport(string point, string inSource, string inTarget, string remark = null, bool isProblem = true) {
			return new ComparingReport(
				isProblem,
				this.SourceAssembly.Name,
				this.SourceType.Name,
				this.SourceMember.Name,
				this.SourceMemberKind,
				this.TypeParameterCount,
				GetOverload(),
				point,
				inSource,
				inTarget,
				remark
			);
		}

		protected ComparingReport CreateExistenceReport(string remark = null) {
			return CreateReport(Points.Existence, "exists", "does not exist", remark, true);
		}

		#endregion


		#region methods - scanning

		protected void CheckAssemblies(SourceObject sourceInfo) {
			// check argument
			if (sourceInfo.Valid == false) {
				throw SourceObject.CreateNotValidException(nameof(sourceInfo));
			}

			// check each assembly
			IEnumerable<SourceObject> sourceAssemblies = sourceInfo.GetChildSourceObjects(PropNames.Assemblies, sort: this.Sort);
			if (sourceAssemblies != null) {
				foreach (SourceObject sourceAssembly in sourceAssemblies) {
					this.SourceAssembly = sourceAssembly;
					try {
						CheckAssembly(sourceAssembly);
					} finally {
						this.SourceAssembly = SourceObject.Null;
					}
				}
			}
		}

		protected void CheckAssembly(SourceObject sourceAssembly) {
			// check argument
			if (sourceAssembly.Valid == false) {
				throw SourceObject.CreateNotValidException(nameof(sourceAssembly));
			}

			// check existence
			Assembly targetAssembly = GetTargetAssembly(sourceAssembly);
			if (targetAssembly == null) {
				// target assembly not found
				OnReport(CreateExistenceReport());
				return;
			}

			// check the assembly itself
			CheckAssembly(sourceAssembly, targetAssembly);

			// check each type in the assembly
			IEnumerable<SourceObject> sourceTypes = sourceAssembly.GetChildSourceObjects(PropNames.Types, sort: this.Sort);
			if (sourceTypes != null) {
				this.TargetAssembly = targetAssembly;
				try {
					foreach (SourceObject sourceType in sourceTypes) {
						this.SourceType = sourceType;
						try {
							CheckType(sourceType);
						} finally {
							this.SourceType = SourceObject.Null;
						}
					}
				} finally {
					this.TargetAssembly = null;
				}
			}
		}

		protected void CheckType(SourceObject sourceType) {
			// check argument
			if (sourceType.Valid == false) {
				throw SourceObject.CreateNotValidException(nameof(sourceType));
			}

			// check existence
			Type targetType = GetTargetType(sourceType);
			if (targetType == null) {
				// target type not found
				OnReport(CreateExistenceReport());
				return;
			}

			// check type itself
			CheckType(sourceType, targetType);

			// check each member in the type
			IEnumerable<SourceObject> sourceMembers = sourceType.GetChildSourceObjects(PropNames.Members, this.Sort);
			if (sourceMembers != null) {
				this.TargetType = targetType;
				Debug.Assert(this.targetTypeConstructors == null);
				Debug.Assert(this.targetTypeMethods == null);
				try {
					foreach (SourceObject sourceMember in sourceMembers) {
						this.SourceMember = sourceMember;
						try {
							CheckMember(sourceMember);
						} finally {
							this.SourceMember = SourceObject.Null;
						}
					}
				} finally {
					this.targetTypeMethods = null;
					this.targetTypeConstructors = null;
					this.TargetType = null;
				}
			}
		}

		protected void CheckMember(SourceObject sourceMember) {
			// check argument
			if (sourceMember.Valid == false) {
				throw SourceObject.CreateNotValidException(nameof(sourceMember));
			}

			// check depending on kind of the member
			string kind = sourceMember.GetIndispensableProperty<string>(PropNames.Kind);
			this.SourceMemberKind = kind;
			try {
				CheckMember(sourceMember, kind);
			} finally {
				this.SourceMemberKind = null;
			}
		}

		protected void CheckField(SourceObject sourceField) {
			// check argument
			if (sourceField.Valid == false) {
				throw SourceObject.CreateNotValidException(nameof(sourceField));
			}

			// check existence
			FieldInfo targetField = GetTargetField(sourceField);
			if (targetField == null) {
				// target field not found
				OnReport(CreateExistenceReport());
				return;
			}

			// check field
			CheckField(sourceField, targetField);
		}

		protected SourceObject[] GetSourceParameters(SourceObject sourceMethod) {
			// check argument
			if (sourceMethod.Valid == false) {
				throw SourceObject.CreateNotValidException(nameof(sourceMethod));
			}

			IEnumerable<SourceObject> parameters = sourceMethod.GetChildSourceObjects(PropNames.Parameters, sort: false, withoutName: true);
			return (parameters == null) ? Array.Empty<SourceObject>() : parameters.ToArray();
		}

		// Note that this method returns the first matched method.
		// You must narrowed candidate enough before you call this method. 
		protected (T, ParameterInfo[]) SelectTargetMethodByParameterTypes<T>(SourceObject sourceMethod, SourceObject[] sourceParams, IEnumerable<T> candidateMethods) where T : MethodBase {
			// check argument
			if (sourceMethod.Valid == false) {
				throw SourceObject.CreateNotValidException(nameof(sourceMethod));
			}
			if (sourceParams == null) {
				throw new ArgumentNullException(nameof(sourceParams));
			}
			if (candidateMethods == null) {
				throw new ArgumentNullException(nameof(candidateMethods));
			}

			// select methods which have the same parameter count to one of the source method.
			int paramCount = sourceParams.Length;
			IEnumerable<(T, ParameterInfo[])> filteredMethod = candidateMethods.Select(m => (m, m.GetParameters()))
																			   .Where(item => (item.Item2.Length == paramCount));

			// examine parameter types
			// Note that this code returns the first matched method.
			bool examineParameters(ParameterInfo[] target) {
				for (int i = 0; i < sourceParams.Length; ++i) {
					if (CheckTypeRef(sourceParams[i], PropNames.Type, target[i].ParameterType, report: false) == false) {
						return false;
					}
				}
				return true;
			}

			(T, ParameterInfo[]) targetMethod = (null, null);
			foreach ((T methodInfo, ParameterInfo[] parameters) method in filteredMethod) {
				if (examineParameters(method.parameters)) {
					// parameters matches
					targetMethod = method;
					break;
				}
			}

			return targetMethod;
		}

		protected (ConstructorInfo, ParameterInfo[]) SelectTargetConstructor(SourceObject sourceCtor, SourceObject[] sourceParams, IEnumerable<ConstructorInfo> candidateMethods) {
			return SelectTargetMethodByParameterTypes<ConstructorInfo>(sourceCtor, sourceParams, candidateMethods);
		}

		protected (MethodInfo, ParameterInfo[]) SelectTargetMethod(SourceObject sourceMethod, int typeParamCount, SourceObject[] sourceParams, IEnumerable<MethodInfo> candidateMethods) {
			// check argument
			if (sourceMethod.Valid == false) {
				throw SourceObject.CreateNotValidException(nameof(sourceMethod));
			}
			if (TypeParameterCount < 0) {
				throw new ArgumentOutOfRangeException(nameof(TypeParameterCount));
			}
			if (sourceParams == null) {
				throw new ArgumentNullException(nameof(sourceParams));
			}
			if (candidateMethods == null) {
				throw new ArgumentNullException(nameof(candidateMethods));
			}

			// filter candidate methods
			// filter by type parameter count and name
			bool basicFilter(MethodInfo m) {
				return m.GetGenericArguments().Length == typeParamCount && NameComparer.Equals(sourceMethod.Name, m.Name);
			}
			candidateMethods = candidateMethods.Where(basicFilter);
			if (IsConversionOperator(sourceMethod.Name)) {
				// op_Explicit or op_Implicit
				// they are overloaded by its return type
				candidateMethods = candidateMethods.Where(m => CheckTypeRef(sourceMethod, PropNames.ReturnType, m.ReturnType, report: false));
			}

			return SelectTargetMethodByParameterTypes<MethodInfo>(sourceMethod, sourceParams, candidateMethods);
		}

		protected void CheckConstructor(SourceObject sourceCtor) {
			// check argument
			if (sourceCtor.Valid == false) {
				throw SourceObject.CreateNotValidException(nameof(sourceCtor));
			}

			// get source parameters
			SourceObject[] sourceParams = GetSourceParameters(sourceCtor);
			this.SourceParameters = sourceParams;
			try {
				// check existence
				(ConstructorInfo targetCtor, ParameterInfo[] targetParams) = GetTargetConstructor(sourceCtor, sourceParams);
				if (targetCtor == null) {
					// target method not found
					OnReport(CreateExistenceReport());
					return;
				}

				// check constructor itself
				CheckConstructor(sourceCtor, targetCtor);

				// check each parameter in the constructor
				Debug.Assert(sourceParams.Length == targetParams.Length);
				for (int i = 0; i < sourceParams.Length; ++i) {
					CheckParameter(sourceParams[i], targetParams[i], i);
				}
			} finally {
				this.SourceParameters = null;
			}
		}

		protected void CheckMethod(SourceObject sourceMethod) {
			// check argument
			if (sourceMethod.Valid == false) {
				throw SourceObject.CreateNotValidException(nameof(sourceMethod));
			}

			// get source parameters
			int typeParamCount = (int)sourceMethod.GetIndispensableProperty<double>(PropNames.TypeParameterCount);
			SourceObject[] sourceParams = GetSourceParameters(sourceMethod);

			this.TypeParameterCount = typeParamCount;
			this.SourceParameters = sourceParams;
			try {
				// check existence
				(MethodInfo targetMethod, ParameterInfo[] targetParams) = GetTargetMethod(sourceMethod, typeParamCount, sourceParams);
				if (targetMethod == null) {
					// target method not found
					OnReport(CreateExistenceReport());
					return;
				}

				// check method itself
				CheckMethod(sourceMethod, targetMethod);

				// check each parameter in the method
				Debug.Assert(sourceParams.Length == targetParams.Length);
				for (int i = 0; i < sourceParams.Length; ++i) {
					CheckParameter(sourceParams[i], targetParams[i], i);
				}
			} finally {
				this.SourceParameters = null;
				this.TypeParameterCount = -1;
			}
		}

		#endregion


		#region overridables

		protected virtual void OnReport(ComparingReport report) {
			// check argument
			if (report == null) {
				throw new ArgumentNullException(nameof(report));
			}

			// fire Report event
			EventHandler<CompareEventArgs> handler = this.Report;
			if (handler != null) {
				CompareEventArgs e = new CompareEventArgs(report);
				try {
					handler(this, e);
				} catch {
					// continue (not fatal)
				}
			}
		}

		protected virtual Assembly GetTargetAssembly(SourceObject sourceAssembly) {
			// check argument
			if (sourceAssembly.Valid == false) {
				throw SourceObject.CreateNotValidException(nameof(sourceAssembly));
			}

			// try to get target assembly
			try {
				return Assembly.Load(sourceAssembly.Name);
			} catch (IOException) {
				// target assembly not found
				return null;
			}
		}

		protected virtual void CheckAssembly(SourceObject sourceAssembly, Assembly targetAssembly) {
			// check argument
			if (sourceAssembly.Valid == false) {
				throw SourceObject.CreateNotValidException(nameof(sourceAssembly));
			}
			if (targetAssembly == null) {
				throw new ArgumentNullException(nameof(targetAssembly));
			}

			// any checks on assembly itself?
		}

		protected virtual Type GetTargetType(SourceObject sourceType) {
			// check argument
			if (sourceType.Valid == false) {
				throw SourceObject.CreateNotValidException(nameof(sourceType));
			}

			// check state
			Debug.Assert(this.TargetAssembly != null);

			// try to get target type
			return this.TargetAssembly.GetType(sourceType.Name);
		}

		protected virtual void CheckType(SourceObject sourceType, Type targetType) {
			// check argument
			if (sourceType.Valid == false) {
				throw SourceObject.CreateNotValidException(nameof(sourceType));
			}
			if (targetType == null) {
				throw new ArgumentNullException(nameof(targetType));
			}

			// any checks on assembly itself?
		}

		protected virtual void CheckMember(SourceObject sourceMember, string kind) {
			// check argument
			if (sourceMember.Valid == false) {
				throw SourceObject.CreateNotValidException(nameof(sourceMember));
			}
			if (string.IsNullOrEmpty(kind)) {
				throw new ArgumentNullException(nameof(kind));
			}

			// check depending on kind of the member
			switch (kind) {
				case Kinds.Field:
					CheckField(sourceMember);
					break;
				case Kinds.Constructor:
					CheckConstructor(sourceMember);
					break;
				case Kinds.Method:
					CheckMethod(sourceMember);
					break;
				default:
					throw new ArgumentException($"Unknown value for '{PropNames.Kind}' property: {kind}", nameof(sourceMember));
			}
		}

		protected virtual FieldInfo GetTargetField(SourceObject sourceField) {
			// check argument
			if (sourceField.Valid == false) {
				throw SourceObject.CreateNotValidException(nameof(sourceField));
			}

			// check state
			Debug.Assert(this.TargetAssembly != null);

			// try to get target field
			return this.TargetType.GetField(sourceField.Name, TargetBindingFlags);
		}

		protected virtual void CheckField(SourceObject sourceField, FieldInfo targetField) {
			// check argument
			if (sourceField.Valid == false) {
				throw SourceObject.CreateNotValidException(nameof(sourceField));
			}
			if (targetField == null) {
				throw new ArgumentNullException(nameof(targetField));
			}

			// check field type
			CheckTypeRef(sourceField, PropNames.Type, targetField.FieldType);
		}

		protected virtual (ConstructorInfo, ParameterInfo[]) GetTargetConstructor(SourceObject sourceCtor, SourceObject[] sourceParams) {
			return SelectTargetConstructor(sourceCtor, sourceParams, this.TargetTypeConstructors);
		}

		protected virtual void CheckConstructor(SourceObject sourceCtor, ConstructorInfo targetCtor) {
			// check argument
			if (sourceCtor.Valid == false) {
				throw SourceObject.CreateNotValidException(nameof(sourceCtor));
			}
			if (targetCtor == null) {
				throw new ArgumentNullException(nameof(targetCtor));
			}

			// any checks?
		}

		protected virtual (MethodInfo, ParameterInfo[]) GetTargetMethod(SourceObject sourceMethod, int typeParamCount, SourceObject[] sourceParams) {
			return SelectTargetMethod(sourceMethod, typeParamCount, sourceParams, this.TargetTypeMethods);
		}

		protected virtual void CheckMethod(SourceObject sourceMethod, MethodInfo targetMethod) {
			// check argument
			if (sourceMethod.Valid == false) {
				throw SourceObject.CreateNotValidException(nameof(sourceMethod));
			}
			if (targetMethod == null) {
				throw new ArgumentNullException(nameof(targetMethod));
			}

			// check return type
			CheckTypeRef(sourceMethod, PropNames.ReturnType, targetMethod.ReturnType, point: "return type");
		}

		protected virtual void CheckParameter(SourceObject sourceParam, ParameterInfo targetParam, int index) {
			// check argument
			if (sourceParam.Valid == false) {
				throw SourceObject.CreateNotValidException(nameof(sourceParam));
			}
			if (targetParam == null) {
				throw new ArgumentNullException(nameof(targetParam));
			}

			// any check?
		}

		protected virtual bool CheckTypeRef(SourceObject source, string key, Type targetType, bool report = true, string point = null) {
			// argument checks
			if (source.Valid == false) {
				throw SourceObject.CreateNotValidException(nameof(source));
			}
			if (key == null) {
				throw new ArgumentNullException(nameof(key));
			}
			if (targetType == null) {
				throw new ArgumentNullException(nameof(targetType));
			}
			if (point == null) {
				point = Points.Type;
			}

			// check type
			string sourceTypeName = source.GetIndispensableProperty<string>(key);
			// Note that type name is not simple targetType.FullName.
			// For example, type parameter for type should be in "!0" form,
			// while its FullName is null.
			string targetTypeName = Util.GetTypeDisplayName(targetType);
			bool equal = StringComparer.Ordinal.Equals(sourceTypeName, targetTypeName);
			if (equal == false && report) {
				// report difference
				OnReport(CreateReport(point, sourceTypeName, targetTypeName));
			}

			return equal;
		}

		#endregion
	}
}
