using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Equality {
	internal static class EqualsInternals {
		internal delegate Boolean Equals<T>(ref T x, ref T y);

		internal static class StaticCache<T> {
			public static readonly Equals<T> Func = GetEqualsFunc<T>(typeof (T));
		}

		internal static readonly ConcurrentDictionary<Type, Equals<Object>> DynamicCache = new ConcurrentDictionary<Type, Equals<Object>>();

		internal static Equals<T> GetEqualsFunc<T>(Type type) {
			return Common.GenerateIL<Equals<T>>(GenerateIL<T>, type);
		}

		private static void GenerateIL<T>(Type type, ILGenerator ilGenerator) {
			Action<ILGenerator> loadFirstInstance = i => i.Emit(OpCodes.Ldarg_0);
			if (typeof (T) != type) {
				var instanceLocal = ilGenerator.DeclareLocal(type);
				loadFirstInstance = i => i.Emit(OpCodes.Ldloc, instanceLocal);
				ilGenerator.Emit(OpCodes.Ldarg_0);
				ilGenerator.Emit(OpCodes.Castclass, type);
				ilGenerator.Emit(OpCodes.Stloc, instanceLocal);
			}
			Action<ILGenerator> loadSecondInstance = i => i.Emit(OpCodes.Ldarg_1);
			if (typeof (T) != type) {
				var instanceLocal = ilGenerator.DeclareLocal(type);
				loadSecondInstance = i => i.Emit(OpCodes.Ldloc, instanceLocal);
				ilGenerator.Emit(OpCodes.Ldarg_1);
				ilGenerator.Emit(OpCodes.Castclass, type);
				ilGenerator.Emit(OpCodes.Stloc, instanceLocal);
			}
			if (!type.IsValueType) {
				loadFirstInstance = Common.CombineDelegates(loadFirstInstance, ilg => ilg.Emit(OpCodes.Ldind_Ref));
				loadSecondInstance = Common.CombineDelegates(loadSecondInstance, ilg => ilg.Emit(OpCodes.Ldind_Ref));
			}

			var retFalse = ilGenerator.DefineLabel();
			var localMap = new ConcurrentDictionary<Type, LocalBuilder>();

			var fields = Common.GetFields(type);
			foreach (var field in fields)
				EmitMemberEqualityComparison(ilGenerator, localMap, loadFirstInstance, loadSecondInstance, field, field.FieldType, retFalse);

			var properties = Common.GetProperties(type);
			foreach (var property in properties)
				EmitMemberEqualityComparison(ilGenerator, localMap, loadFirstInstance, loadSecondInstance, property, property.PropertyType, retFalse);

			ilGenerator.Emit(OpCodes.Ldc_I4_1);
			ilGenerator.Emit(OpCodes.Ret);

			ilGenerator.MarkLabel(retFalse);
			ilGenerator.Emit(OpCodes.Ldc_I4_0);
			ilGenerator.Emit(OpCodes.Ret);
		}

		private static void EmitMemberEqualityComparison(ILGenerator ilGenerator,
		                                                 ConcurrentDictionary<Type, LocalBuilder> localMap,
		                                                 Action<ILGenerator> loadFirstInstance,
		                                                 Action<ILGenerator> loadSecondInstance,
		                                                 MemberInfo memberInfo,
		                                                 Type memberType,
		                                                 Label retFalse) {
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
				emitComparison = ilg => ilg.Emit(OpCodes.Bne_Un, retFalse);
			else {
				MethodInfo opEquality;
				if (typeof (IEquatable<>).MakeGenericType(memberType).IsAssignableFrom(memberType)) {
					if (memberType.IsValueType) {
						emitLoadFirstMember = SetEmitLoadFirstMemberForValueType(localMap, memberInfo, memberType, emitLoadFirstMember);
					}
					emitComparison = ilg => ilg.Emit(OpCodes.Call, memberType.GetMethod(nameof(Equals), new[] {memberType}));
				}
				else if ((opEquality = memberType.GetMethod("op_Equality", new[] {memberType, memberType})) != null) {
					emitComparison = ilg => ilg.Emit(OpCodes.Call, opEquality);
				}
				else {
					if (memberType.IsValueType) {
						emitLoadFirstMember = SetEmitLoadFirstMemberForValueType(localMap, memberInfo, memberType, emitLoadFirstMember);
						loadSecondInstance = Common.CombineDelegates(loadSecondInstance, ilg => ilg.Emit(OpCodes.Box, memberType));
					}
					emitComparison = ilg => ilg.Emit(OpCodes.Callvirt, memberType.GetMethod(nameof(Equals), new[] {typeof (Object)}));
				}
				emitComparison = Common.CombineDelegates(emitComparison, ilg => ilg.Emit(OpCodes.Brfalse, retFalse));
			}

			if (memberType.IsValueType) {
				loadFirstInstance(ilGenerator);
				emitLoadFirstMember(ilGenerator);
				loadSecondInstance(ilGenerator);
				emitLoadSecondMember(ilGenerator);
				emitComparison(ilGenerator);
			}
			else {
				throw new NotImplementedException();
				loadFirstInstance(ilGenerator);
				emitLoadFirstMember(ilGenerator);
				var firstHold = ilGenerator.DeclareLocal(memberType);
				ilGenerator.Emit(OpCodes.Stloc, firstHold);

				loadSecondInstance(ilGenerator);
				emitLoadSecondMember(ilGenerator);
				var secondHold = ilGenerator.DeclareLocal(memberType);
				ilGenerator.Emit(OpCodes.Stloc, secondHold);

				var label = ilGenerator.DefineLabel();
				ilGenerator.Emit(OpCodes.Ldloc, firstHold);
				ilGenerator.Emit(OpCodes.Brtrue, label);
				ilGenerator.Emit(OpCodes.Ldloc, secondHold);
				ilGenerator.Emit(OpCodes.Brtrue, label);
				ilGenerator.Emit(OpCodes.Ldc_I4_1);
				ilGenerator.Emit(OpCodes.Ret);

				ilGenerator.MarkLabel(label);
				ilGenerator.Emit(OpCodes.Ldloc, firstHold);
				ilGenerator.Emit(OpCodes.Brfalse, retFalse);

				ilGenerator.Emit(OpCodes.Ldloc, firstHold);
				ilGenerator.Emit(OpCodes.Ldloc, secondHold);
				emitComparison(ilGenerator);
			}
		}

		private static Action<ILGenerator> SetEmitLoadFirstMemberForValueType(ConcurrentDictionary<Type, LocalBuilder> localMap, MemberInfo memberInfo, Type memberType, Action<ILGenerator> emitLoadFirstMember) {
			if (memberInfo is FieldInfo)
				emitLoadFirstMember = ilg => ilg.Emit(OpCodes.Ldflda, (FieldInfo) memberInfo);
			else if (memberInfo is PropertyInfo) {
				emitLoadFirstMember = ilg => {
					                      var local = localMap.GetOrAdd(memberType, ilg.DeclareLocal);
					                      ilg.Emit(OpCodes.Stloc, local);
					                      ilg.Emit(OpCodes.Ldloca, local);
				                      };
			}
			else
				throw new ArgumentOutOfRangeException(nameof(memberInfo), "Must be FieldInfo or PropertyInfo");
			return emitLoadFirstMember;
		}
	}
}