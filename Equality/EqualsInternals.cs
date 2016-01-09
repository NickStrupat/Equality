using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Equality {
	internal static class EqualsInternals {
		internal delegate Boolean StructEquals<T>(ref T x, ref T y) where T : struct;
		internal delegate Boolean ClassEquals<in T>(T x, T y) where T : class;

		internal static class StaticStructCache<T> where T : struct {
			public static readonly StructEquals<T> Func = GetStructEqualsFunc<T>(typeof(T));
		}

		internal static class StaticClassCache<T> where T : class {
			public static readonly ClassEquals<T> Func = GetClassEqualsFunc<T>(typeof(T));
		}

		internal static ClassEquals<Object> GetDynamicClassEquals(Type type) => DynamicCache.GetOrAdd(type, GetClassEqualsFunc<Object>);

		private static StructEquals<T> GetStructEqualsFunc<T>(Type type) where T : struct => Common.GenerateIL<StructEquals<T>>(GenerateIL<T>, type);

		private static ClassEquals<T> GetClassEqualsFunc<T>(Type type) where T : class => Common.GenerateIL<ClassEquals<T>>(GenerateIL<T>, type);

		private static readonly ConcurrentDictionary<Type, ClassEquals<Object>> DynamicCache = new ConcurrentDictionary<Type, ClassEquals<Object>>();

		private static Boolean EnumerableEquals<T>(IEnumerable<T> first, IEnumerable<T> second) {
			try {
				var dictionary = first as IDictionary;
				if (dictionary != null)
					return DictionaryComparer.Equals(dictionary, (IDictionary) second);

				var list = first as IList<T>;
				if (list != null)
					return ArrayEquals(list, (IList<T>) second);

				if (first is IStructuralEquatable)
					return StructuralComparisons.StructuralEqualityComparer.Equals(first, second);

				return first.SequenceEqual(second);
			}
			catch (InvalidOperationException) { // Catch any collection changes while enumerating
				return false;
			}
		}
		
		private static Boolean ArrayEquals<T>(IList<T> first, IList<T> second) {
			if (first.Count != second.Count)
				return false;
			var comparer = EqualityComparer<T>.Default;
			for (var i = 0; i < first.Count; i++)
				if (!comparer.Equals(first[i], second[i]))
					return false;
			return true;
		}

		private static void GenerateIL<T>(Type type, ILGenerator ilGenerator) {
			Action<ILGenerator> loadFirstInstance = i => i.Emit(OpCodes.Ldarg_0);
			if (typeof(T) != type) {
				var instanceLocal = ilGenerator.DeclareLocal(type);
				loadFirstInstance = i => i.Emit(OpCodes.Ldloc, instanceLocal);
				ilGenerator.Emit(OpCodes.Ldarg_0);
				ilGenerator.Emit(OpCodes.Castclass, type);
				ilGenerator.Emit(OpCodes.Stloc, instanceLocal);
			}
			Action<ILGenerator> loadSecondInstance = i => i.Emit(OpCodes.Ldarg_1);
			if (typeof(T) != type) {
				var instanceLocal = ilGenerator.DeclareLocal(type);
				loadSecondInstance = i => i.Emit(OpCodes.Ldloc, instanceLocal);
				ilGenerator.Emit(OpCodes.Ldarg_1);
				ilGenerator.Emit(OpCodes.Castclass, type);
				ilGenerator.Emit(OpCodes.Stloc, instanceLocal);
			}

			var firstMemberLocalMap = new ConcurrentDictionary<Type, LocalBuilder>();
			var secondMemberLocalMap = new ConcurrentDictionary<Type, LocalBuilder>();

			var fields = Common.GetFields(type);
			foreach (var field in fields) {
				var nextField = ilGenerator.DefineLabel();
				EmitMemberEqualityComparison(ilGenerator, firstMemberLocalMap, secondMemberLocalMap, loadFirstInstance, loadSecondInstance, field, field.FieldType, nextField);
				ilGenerator.MarkLabel(nextField);
			}

			var properties = Common.GetProperties(type);
			foreach (var property in properties) {
				var nextProperty = ilGenerator.DefineLabel();
				EmitMemberEqualityComparison(ilGenerator, firstMemberLocalMap, secondMemberLocalMap, loadFirstInstance, loadSecondInstance, property, property.PropertyType, nextProperty);
				ilGenerator.MarkLabel(nextProperty);
			}

			ilGenerator.Emit(OpCodes.Ldc_I4_1);
			ilGenerator.Emit(OpCodes.Ret);
		}

		private static void EmitMemberEqualityComparison(ILGenerator ilGenerator,
														 ConcurrentDictionary<Type, LocalBuilder> firstMemberLocalMap,
														 ConcurrentDictionary<Type, LocalBuilder> secondMemberLocalMap,
														 Action<ILGenerator> loadFirstInstance,
														 Action<ILGenerator> loadSecondInstance,
														 MemberInfo memberInfo,
														 Type memberType,
														 Label nextMember) {
			Action<ILGenerator> emitLoadFirstMember;
			Action<ILGenerator> emitLoadSecondMember;
			if (memberInfo is FieldInfo)
				emitLoadFirstMember = emitLoadSecondMember = ilg => ilg.Emit(OpCodes.Ldfld, (FieldInfo) memberInfo);
			else if (memberInfo is PropertyInfo)
				emitLoadFirstMember = emitLoadSecondMember = ilg => ilg.Emit(OpCodes.Call, ((PropertyInfo) memberInfo).GetGetMethod(nonPublic: true));
			else
				throw new ArgumentOutOfRangeException(nameof(memberInfo), "Must be FieldInfo or PropertyInfo");

			Action<ILGenerator> emitComparison = delegate { };
			if (memberType.IsPrimitive)
				emitComparison = ilg => ilg.Emit(OpCodes.Beq, nextMember);
			else {
				MethodInfo opEquality;
				Type t;
				if (typeof(IEquatable<>).MakeGenericType(memberType).IsAssignableFrom(memberType)) {
					if (memberType.IsValueType) {
						emitLoadFirstMember = SetEmitLoadFirstMemberForValueType(firstMemberLocalMap, memberInfo, memberType, emitLoadFirstMember);
					}
					else {
						SetEmitLoadAndCompareForReferenceType(firstMemberLocalMap, secondMemberLocalMap, memberType, nextMember, ref emitLoadFirstMember, ref emitLoadSecondMember, ref emitComparison);
					}
					emitComparison = emitComparison.CombineDelegates(ilg => ilg.Emit(OpCodes.Call, memberType.GetMethod(nameof(Equals), new[] { memberType })));
				}
				else if ((opEquality = memberType.GetMethod("op_Equality", new[] { memberType, memberType })) != null) {
					emitComparison = ilg => ilg.Emit(OpCodes.Call, opEquality);
				}
				else {
					if (memberType.IsValueType) {
						emitLoadFirstMember = SetEmitLoadFirstMemberForValueType(firstMemberLocalMap, memberInfo, memberType, emitLoadFirstMember);
						loadSecondInstance = loadSecondInstance.CombineDelegates(ilg => ilg.Emit(OpCodes.Box, memberType));
					}
					else {
						SetEmitLoadAndCompareForReferenceType(firstMemberLocalMap, secondMemberLocalMap, memberType, nextMember, ref emitLoadFirstMember, ref emitLoadSecondMember, ref emitComparison);
					}
					if (memberInfo.ShouldGetStructuralHashCode(memberType, out t))
						emitComparison = emitComparison.CombineDelegates(ilg => ilg.Emit(OpCodes.Call, typeof(EqualsInternals).GetMethod(nameof(EnumerableEquals), BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(memberType, t)));
					else
						emitComparison = emitComparison.CombineDelegates(ilg => ilg.Emit(OpCodes.Callvirt, memberType.GetMethod(nameof(Equals), new[] { typeof(Object) })));
				}
				emitComparison = emitComparison.CombineDelegates(ilg => ilg.Emit(OpCodes.Brtrue, nextMember));
			}

			loadFirstInstance(ilGenerator);
			emitLoadFirstMember(ilGenerator);
			loadSecondInstance(ilGenerator);
			emitLoadSecondMember(ilGenerator);
			emitComparison(ilGenerator);

			ilGenerator.Emit(OpCodes.Ldc_I4_0);
			ilGenerator.Emit(OpCodes.Ret);
		}

		private static void SetEmitLoadAndCompareForReferenceType(ConcurrentDictionary<Type, LocalBuilder> firstMemberLocalMap,
																  ConcurrentDictionary<Type, LocalBuilder> secondMemberLocalMap,
																  Type memberType,
																  Label nextMember,
																  ref Action<ILGenerator> emitLoadFirstMember,
																  ref Action<ILGenerator> emitLoadSecondMember,
																  ref Action<ILGenerator> emitComparison) {
			LocalBuilder firstLocal = null;
			emitLoadFirstMember = emitLoadFirstMember.CombineDelegates(ilg => {
				firstLocal = firstMemberLocalMap.GetOrAdd(memberType, ilg.DeclareLocal);
				ilg.Emit(OpCodes.Stloc, firstLocal);
			});
			LocalBuilder secondLocal = null;
			emitLoadSecondMember = emitLoadSecondMember.CombineDelegates(ilg => {
				secondLocal = secondMemberLocalMap.GetOrAdd(memberType, ilg.DeclareLocal);
				ilg.Emit(OpCodes.Stloc, secondLocal);
			});
			emitComparison = ilg => {
				var nonNullMemberCompare = ilg.DefineLabel();

				ilg.Emit(OpCodes.Ldloc, firstLocal);
				ilg.Emit(OpCodes.Brtrue, nonNullMemberCompare);

				ilg.Emit(OpCodes.Ldloc, secondLocal);
				ilg.Emit(OpCodes.Brfalse, nextMember);
				ilg.Emit(OpCodes.Ldc_I4_0);
				ilg.Emit(OpCodes.Ret);

				ilg.MarkLabel(nonNullMemberCompare);
				ilg.Emit(OpCodes.Ldloc, firstLocal);
				ilg.Emit(OpCodes.Ldloc, secondLocal);
			};
		}

		private static Action<ILGenerator> SetEmitLoadFirstMemberForValueType(ConcurrentDictionary<Type, LocalBuilder> localMap, MemberInfo memberInfo, Type memberType, Action<ILGenerator> emitLoadFirstMember) {
			if (memberInfo is FieldInfo)
				return ilg => ilg.Emit(OpCodes.Ldflda, (FieldInfo) memberInfo);
			if (memberInfo is PropertyInfo) {
				return emitLoadFirstMember.CombineDelegates(ilg => {
					var local = localMap.GetOrAdd(memberType, ilg.DeclareLocal);
					ilg.Emit(OpCodes.Stloc, local);
					ilg.Emit(OpCodes.Ldloca, local);
				});
			}
			throw new ArgumentOutOfRangeException(nameof(memberInfo), "Must be FieldInfo or PropertyInfo");
		}
	}
}