using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Equality {
	internal static class GetHashCodeInternals {
		internal delegate Int32 GetHashCode<T>(ref T x);

		internal static class StaticCache<T> {
			public static readonly GetHashCode<T> Func = GetHashCodeFunc<T>(typeof(T));
		}

		internal static readonly ConcurrentDictionary<Type, GetHashCode<Object>> DynamicCache = new ConcurrentDictionary<Type, GetHashCode<Object>>();

		internal static GetHashCode<T> GetHashCodeFunc<T>(Type type) {
			return Common.GenerateIL<GetHashCode<T>>(GenerateIL<T>, type);
		}

		private static void GenerateIL<T>(Type type, ILGenerator ilGenerator) {
			const Int32 prime = 486187739; // number from http://stackoverflow.com/a/2816747/232574
			Action<ILGenerator> loadInstanceOpCode = i => i.Emit(OpCodes.Ldarg_0);
			if (typeof (T) != type) {
				var instanceLocal = ilGenerator.DeclareLocal(type);
				loadInstanceOpCode = i => i.Emit(OpCodes.Ldloc, instanceLocal);
				ilGenerator.Emit(OpCodes.Ldarg_0);
				ilGenerator.Emit(OpCodes.Castclass, type);
				ilGenerator.Emit(OpCodes.Stloc, instanceLocal);
			}
			if (!type.IsValueType)
				loadInstanceOpCode = Common.CombineDelegates(loadInstanceOpCode, ilg => ilg.Emit(OpCodes.Ldind_Ref));

			var objectGetHashCode = typeof (Object).GetMethod(nameof(GetHashCode), Type.EmptyTypes);
			var hashCode = ilGenerator.DeclareLocal(typeof (Int32));
			ilGenerator.Emit(OpCodes.Ldc_I4, prime);
			ilGenerator.Emit(OpCodes.Stloc, hashCode);

			var fields = Common.GetFields(type);
			for (var i = 0; i < fields.Length; i++) {
				var field = fields[i];
				Func<Boolean> isValueType = () => field.FieldType.IsValueType;
				Action loadValueTypeMember = () => ilGenerator.Emit(OpCodes.Ldflda, field);
				Action loadReferenceTypeMember = () => ilGenerator.Emit(OpCodes.Ldfld, field);
				var isFirst = i == 0;

				EmitMemberIL<T>(ilGenerator, prime, isValueType, isFirst, hashCode, loadInstanceOpCode, loadValueTypeMember, field.FieldType, loadReferenceTypeMember, objectGetHashCode);
			}

			var properties = Common.GetProperties(type);
			for (var i = 0; i < properties.Length; i++) {
				var property = properties[i];
				Func<Boolean> isValueType = () => property.PropertyType.IsValueType;
				Action loadValueTypeMember = () => ilGenerator.Emit(OpCodes.Call, property.GetGetMethod(nonPublic: true));
				Action loadReferenceTypeMember = loadValueTypeMember;
				var isFirst = i == 0 && !fields.Any();

				EmitMemberIL<T>(ilGenerator, prime, isValueType, isFirst, hashCode, loadInstanceOpCode, loadValueTypeMember, property.PropertyType, loadReferenceTypeMember, objectGetHashCode);
			}

			ilGenerator.Emit(OpCodes.Ldloc, hashCode);
			ilGenerator.Emit(OpCodes.Ret);
		}

		private static void EmitMemberIL<T>(ILGenerator ilGenerator,
		                                    Int32 prime,
		                                    Func<Boolean> isValueType,
		                                    Boolean isFirst,
		                                    LocalBuilder hashCode,
		                                    Action<ILGenerator> loadInstanceOpCode,
		                                    Action loadValueTypeMember,
		                                    Type memberType,
		                                    Action loadReferenceTypeMember,
		                                    MethodInfo objectGetHashCode) {
			if (isValueType()) {
				if (!isFirst) {
					ilGenerator.Emit(OpCodes.Ldloc, hashCode);
					ilGenerator.Emit(OpCodes.Ldc_I4, prime);
					ilGenerator.Emit(OpCodes.Mul);
				}
				loadInstanceOpCode(ilGenerator);
				loadValueTypeMember();
				ilGenerator.Emit(OpCodes.Call, memberType.GetMethod(nameof(GetHashCode), Type.EmptyTypes));
				if (!isFirst)
					ilGenerator.Emit(OpCodes.Add);
				ilGenerator.Emit(OpCodes.Stloc, hashCode);
			}
			else {
				var label = ilGenerator.DefineLabel();
				var hold = ilGenerator.DeclareLocal(memberType);
				loadInstanceOpCode(ilGenerator);
				loadReferenceTypeMember();
				ilGenerator.Emit(OpCodes.Stloc, hold);
				ilGenerator.Emit(OpCodes.Ldloc, hold);
				ilGenerator.Emit(OpCodes.Brfalse_S, label);

				if (!isFirst) {
					ilGenerator.Emit(OpCodes.Ldloc, hashCode);
					ilGenerator.Emit(OpCodes.Ldc_I4, prime);
					ilGenerator.Emit(OpCodes.Mul);
				}
				ilGenerator.Emit(OpCodes.Ldloc, hold);
				ilGenerator.Emit(OpCodes.Callvirt, objectGetHashCode);
				if (!isFirst)
					ilGenerator.Emit(OpCodes.Add);
				ilGenerator.Emit(OpCodes.Stloc, hashCode);
				ilGenerator.MarkLabel(label);
			}
		}
	}
}