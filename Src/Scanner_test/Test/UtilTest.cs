using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;
using DotNetAPIScanner.Scanner;

namespace DotNetAPIScanner.Scanner.Test {
	public class UtilTest {
		#region samples

		class Sample {
			public static void MethodWithRefParam(ref string str) {
				str = null;
			}
		}

		class GenericSample<TT0, TT1>: Dictionary<TT0, TT1> {
			public class NestedSample<TT2> {
				public static void MethodWithTypeParam<TM>(TT0 tt0, TT2 tt2, TM tm) {
				}
			}

			public static void MethodWithTypeParam<TM0, TM1>(TT0 tt0, TT1 tt1, TM0 tm0, TM1 tm1) {
			}

			unsafe public static void MethodWithRefParam<TM>(
				ref Version normal,
				ref TT1 typeParamForType,
				ref TM typeParamForMethod,
				ref int* pointer,
				ref Version[] array,
				ref IEquatable<string> generic
			) {
			}

			public static void MethodWithArrayParam<TM> (TT1[] typeParamForType, TM[] typeParamForMethod) {
			}
		}

		#endregion


		#region GetTypeDisplayName

		public class GetTypeDisplayName {
			[Fact(DisplayName = "type: normal")]
			public void type_normal() {
				// arrange
				Type type_general = typeof(Version);
				Type type_primitive = typeof(int);

				// act
				string actual_general = Util.GetTypeDisplayName(type_general);
				string actual_primitive = Util.GetTypeDisplayName(type_primitive);

				// assert
				Assert.Equal("System.Version", actual_general);
				Assert.Equal("System.Int32", actual_primitive);
			}

			[Fact(DisplayName = "type: type parameter")]
			public void type_typeParameter() {
				// arrange
				// gets the types of parameters of GenericSample<TT0, TT1>.MethodWithTypeParam<TM0, TM1>(TT0, TT1, TM0, TM1)
				// That is, it returns {typeof(TT0), typeof(TT1), typeof(TM0), typeof(TM1)},
				// though this code is syntactically invalid.
				static IEnumerable<Type> getSamples() {
					Type genericType = typeof(GenericSample<int, string>).GetGenericTypeDefinition();
					MethodInfo method = genericType.GetMethod("MethodWithTypeParam");
					return method.GetParameters().Select(p => p.ParameterType);
				}

				// act
				string[] actuals = getSamples().Select(s => Util.GetTypeDisplayName(s)).ToArray();

				// assert
				// type parameters for type
				Assert.Equal("!0", actuals[0]);
				Assert.Equal("!1", actuals[1]);
				// type parameters for method
				Assert.Equal("!!0", actuals[2]);
				Assert.Equal("!!1", actuals[3]);
			}

			[Fact(DisplayName = "type: type parameter (nested)")]
			public void type_typeParameter_nested() {
				// arrange
				// gets the types of parameters of GenericSample<TT0, TT1>.NestedSample<TT2>.MethodWithTypeParam<TM>(TT0, TT2, TM)
				// That is, it returns {typeof(TT0), typeof(TT2), typeof(TM)},
				// though this code is syntactically invalid.
				static IEnumerable<Type> getSamples() {
					Type genericType = typeof(GenericSample<int, string>.NestedSample<bool>).GetGenericTypeDefinition();
					MethodInfo method = genericType.GetMethod("MethodWithTypeParam");
					return method.GetParameters().Select(p => p.ParameterType);
				}

				// act
				string[] actuals = getSamples().Select(s => Util.GetTypeDisplayName(s)).ToArray();

				// assert
				// type parameters for type
				// Note that TT2 must be "!2" because type parameters of a nested type includes
				// type parameters of its outer type.
				Assert.Equal("!0", actuals[0]);
				Assert.Equal("!2", actuals[1]);
				// type parameters for method
				Assert.Equal("!!0", actuals[2]);
			}

			[Fact(DisplayName = "type: generic")]
			public void type_generic() {
				// arrange
				string sampleTypeName = $"{typeof(UtilTest).FullName}+GenericSample`2";
				// GenericSample<string, int>
				Type type_closed = typeof(GenericSample<string, int>);
				// declaring GenericSample<TT0, TT1>
				Type type_open = type_closed.GetGenericTypeDefinition();
				// using Dictionary<TT0, TT1> where TT0 and TT1 are type parameters of outer type
				Type type_paramed = type_open.BaseType;
				// GenericSample<string, int>.NestedSample<bool>
				Type type_nested_closed = typeof(GenericSample<string, int>.NestedSample<bool>);
				// GenericSample<string, int>.NestedSample<TT2>
				Type type_nested_partial = type_closed.GetNestedType("NestedSample`1");
				// GenericSample<TT0, TT1>.NestedSample<TT2>
				Type type_nested_open = type_open.GetNestedType("NestedSample`1");

				// act
				string actual_closed = Util.GetTypeDisplayName(type_closed);
				string actual_opened = Util.GetTypeDisplayName(type_open);
				string actual_paramed = Util.GetTypeDisplayName(type_paramed);
				string actual_nested_closed = Util.GetTypeDisplayName(type_nested_closed);
				string actual_nested_partial = Util.GetTypeDisplayName(type_nested_partial);
				string actual_nested_open = Util.GetTypeDisplayName(type_nested_open);

				// assert
				// Note that notation for nested generic types is not C# style but IL style.
				// That is:
				//   not "Outer<Arg0,Arg1>" but "Outer`2[Arg0,Arg1]"
				//   not "Outer<Arg0,Arg1>.Inner<Arg2>" but "Outer`2+Inner`1[Arg0,Arg1,Arg2]"
				Assert.Equal($"{sampleTypeName}[System.String,System.Int32]", actual_closed);
				Assert.Equal(sampleTypeName, actual_opened);
				Assert.Equal("System.Collections.Generic.Dictionary`2[!0,!1]", actual_paramed);
				Assert.Equal($"{sampleTypeName}+NestedSample`1[System.String,System.Int32,System.Boolean]", actual_nested_closed);
				Assert.Equal($"{sampleTypeName}+NestedSample`1", actual_nested_partial);	// actualy open type
				Assert.Equal($"{sampleTypeName}+NestedSample`1", actual_nested_open);
			}

			[Fact(DisplayName = "type: pointer")]
			public void type_pointer() {
				// arrange
				Type type = typeof(int*);

				// act
				string actual = Util.GetTypeDisplayName(type);

				// assert
				Assert.Equal("System.Int32*", actual);
			}

			[Fact(DisplayName = "type: reference")]
			public void type_reference() {
				// arrange
				// gets types of parameters of GenericSample<TT0, TT1>.MethodWithRefParam<TM>() methods.
				static IEnumerable<Type> getSamples() {
					Type genericType = typeof(GenericSample<string, int>).GetGenericTypeDefinition();
					MethodInfo sampleMethod = genericType.GetMethod("MethodWithRefParam");
					return sampleMethod.GetParameters().Select(p => p.ParameterType);
				}

				// act
				string[] actuals = getSamples().Select(s => Util.GetTypeDisplayName(s)).ToArray();

				// assert
				// normal
				Assert.Equal("System.Version&", actuals[0]);
				// type parameter for type
				Assert.Equal("!1&", actuals[1]);
				// type parameter for method
				Assert.Equal("!!0&", actuals[2]);
				// pointer
				Assert.Equal("System.Int32*&", actuals[3]);
				// array
				Assert.Equal("System.Version[]&", actuals[4]);
				// generic
				Assert.Equal("System.IEquatable`1[System.String]&", actuals[5]);
			}

			[Fact(DisplayName = "type: array")]
			public void type_array() {
				// arrange
				Type type_simple = typeof(Version[]);
				Type type_multidim = typeof(Version[,,,]);
				Type type_jagged = typeof(Version[][]);
				Type type_pointer = typeof(int*[]);
				Type type_generic = typeof(IEquatable<string>[]); 

				// gets types of parameters of GenericSample<TT0, TT1>.MethodWithArrayParam<TM>(TT1[], TM[]) methods.
				// That is, it returns (typeof(TT1[]), typeof(TM[])), though this code is syntactically invalid.
				static (Type, Type) getTypeParamSamples() {
					Type genericType = typeof(GenericSample<string, int>).GetGenericTypeDefinition();
					MethodInfo sampleMethod = genericType.GetMethod("MethodWithArrayParam");
					ParameterInfo[] p = sampleMethod.GetParameters();
					return (p[0].ParameterType, p[1].ParameterType);
				}
				(Type type_typeParamForType, Type type_typeParamForMethod) = getTypeParamSamples();

				// act
				string actual_simple = Util.GetTypeDisplayName(type_simple);
				string actual_multidim = Util.GetTypeDisplayName(type_multidim);
				string actual_jagged = Util.GetTypeDisplayName(type_jagged);
				string actual_pointer = Util.GetTypeDisplayName(type_pointer);
				string actual_generic = Util.GetTypeDisplayName(type_generic);
				string actual_typeParamForType = Util.GetTypeDisplayName(type_typeParamForType);
				string actual_typeParamForMethod = Util.GetTypeDisplayName(type_typeParamForMethod);

				// assert
				Assert.Equal("System.Version[]", actual_simple);
				Assert.Equal("System.Version[,,,]", actual_multidim);
				Assert.Equal("System.Version[][]", actual_jagged);
				Assert.Equal("System.Int32*[]", actual_pointer);
				Assert.Equal("System.IEquatable`1[System.String][]", actual_generic);
				Assert.Equal("!1[]", actual_typeParamForType);
				Assert.Equal("!!0[]", actual_typeParamForMethod);
			}
		}

		#endregion
	}
}
