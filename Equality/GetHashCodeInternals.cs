using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Security.Cryptography.X509Certificates;

namespace Equality {
	internal static class GetHashCodeInternals {
		internal delegate Int32 GetStructHashCode<T>(ref T x) where T : struct;
		internal delegate Int32 GetClassHashCode<in T>(T x) where T : class;

		internal static class StaticStructCache<T> where T : struct {
			public static readonly GetStructHashCode<T> Func = GetStructHashCodeFunc<T>(typeof(T));
		}

		internal static class StaticClassCache<T> where T : class {
			public static readonly GetClassHashCode<T> Func = GetClassHashCodeFunc<T>(typeof(T));
		}

		internal static GetClassHashCode<Object> GetDynamicClassEquals(Type type) => DynamicCache.GetOrAdd(type, GetClassHashCodeFunc<Object>);

		private static GetStructHashCode<T> GetStructHashCodeFunc<T>(Type type) where T : struct => Common.GenerateIL<GetStructHashCode<T>>(GenerateIL<T>, type);

		private static GetClassHashCode<T> GetClassHashCodeFunc<T>(Type type) where T : class => Common.GenerateIL<GetClassHashCode<T>>(GenerateIL<T>, type);

		private static readonly ConcurrentDictionary<Type, GetClassHashCode<Object>> DynamicCache = new ConcurrentDictionary<Type, GetClassHashCode<Object>>();

		private static Int32 GetEnumerableHashCode<T>(IEnumerable<T> x) {
			var dictionary = x as IDictionary;
			if (dictionary != null)
				return GetDictionaryHashCode(dictionary);
			var list = x as IList<T>;
			if (list != null)
				return GetArrayHashCode(list);
			if (x is IStructuralEquatable)
				return StructuralComparisons.StructuralEqualityComparer.GetHashCode(x);
			Int32 hashCode = seed;
			foreach (var a in x)
				if (a != null)
					hashCode = hashCode * prime + a.GetHashCode();
			return hashCode;
		}

		private static Int32 GetArrayHashCode<T>(IList<T> list) {
			Int32 hashCode = seed;
			for (var i = 0; i != list.Count; ++i) {
				var a = list[i];
				if (a != null)
					hashCode = hashCode * prime + a.GetHashCode();
			}
			return hashCode;
		}

		private static Int32 GetDictionaryHashCode(IDictionary dictionary) {
			Int32 hashCode = seed;
			var keys = dictionary.Keys.Cast<Object>().OrderBy(x => x);
			foreach (var key in keys) {
				var b = key;
				if (b != null)
					hashCode = hashCode*prime + b.GetHashCode();
				var c = dictionary[key];
				if (c != null)
					hashCode = hashCode * prime + c.GetHashCode();
			}
			return hashCode;
		}

		//const Int32 prime = 486187739; // number from http://stackoverflow.com/a/2816747/232574
		const Int32 prime = -1521134295; // http://stackoverflow.com/questions/371328/why-is-it-important-to-override-gethashcode-when-equals-method-is-overridden#comment271215_371348
		const Int32 seed = 1374496523;   // ^^^

		private static void GenerateIL<T>(Type type, ILGenerator ilGenerator) {
			Action<ILGenerator> loadInstanceOpCode = i => i.Emit(OpCodes.Ldarg_0);
			if (typeof (T) != type) {
				var instanceLocal = ilGenerator.DeclareLocal(type);
				loadInstanceOpCode = i => i.Emit(OpCodes.Ldloc, instanceLocal);
				ilGenerator.Emit(OpCodes.Ldarg_0);
				ilGenerator.Emit(OpCodes.Castclass, type);
				ilGenerator.Emit(OpCodes.Stloc, instanceLocal);
			}

			var objectGetHashCode = typeof (Object).GetMethod(nameof(GetHashCode), Type.EmptyTypes);
			var hashCode = ilGenerator.DeclareLocal(typeof (Int32));
			ilGenerator.Emit(OpCodes.Ldc_I4, seed);
			ilGenerator.Emit(OpCodes.Stloc, hashCode);

			var fields = Common.GetFields(type);
			for (var i = 0; i < fields.Length; i++) {
				var field = fields[i];
				Action<ILGenerator> loadValueTypeMember = ilg => ilg.Emit(OpCodes.Ldflda, field);
				Action<ILGenerator> loadReferenceTypeMember = ilg => ilg.Emit(OpCodes.Ldfld, field);
				var isFirst = i == 0;

				EmitMemberIL(ilGenerator, prime, isFirst, hashCode, loadInstanceOpCode, loadValueTypeMember, field.FieldType, loadReferenceTypeMember, objectGetHashCode);
			}

			var properties = Common.GetProperties(type);
			for (var i = 0; i < properties.Length; i++) {
				var property = properties[i];
				Action<ILGenerator> loadValueTypeMember = ilg => ilg.Emit(OpCodes.Call, property.GetGetMethod(nonPublic: true));
				Action<ILGenerator> loadReferenceTypeMember = loadValueTypeMember;
				var isFirst = i == 0 && !fields.Any();

				EmitMemberIL(ilGenerator, prime, isFirst, hashCode, loadInstanceOpCode, loadValueTypeMember, property.PropertyType, loadReferenceTypeMember, objectGetHashCode);
			}

			ilGenerator.Emit(OpCodes.Ldloc, hashCode);
			ilGenerator.Emit(OpCodes.Ret);
		}

		private static void EmitMemberIL(ILGenerator ilGenerator,
			                                Int32 prime,
			                                Boolean isFirst,
			                                LocalBuilder hashCode,
			                                Action<ILGenerator> loadInstanceOpCode,
			                                Action<ILGenerator> loadValueTypeMember,
			                                Type memberType,
			                                Action<ILGenerator> loadReferenceTypeMember,
			                                MethodInfo objectGetHashCode) {
			Type t;
			if (memberType.IsValueType) {
				ilGenerator.Emit(OpCodes.Ldloc, hashCode);
				ilGenerator.Emit(OpCodes.Ldc_I4, prime);
				ilGenerator.Emit(OpCodes.Mul);
				loadInstanceOpCode(ilGenerator);
				loadValueTypeMember(ilGenerator);
				if (memberType != typeof(String) && memberType.IsEnumberable(out t))
					ilGenerator.Emit(OpCodes.Call, typeof(GetHashCodeInternals).GetMethod(nameof(GetEnumerableHashCode), BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(t));
				else
					ilGenerator.Emit(OpCodes.Call, memberType.GetMethod(nameof(GetHashCode), Type.EmptyTypes));
				ilGenerator.Emit(OpCodes.Add);
				ilGenerator.Emit(OpCodes.Stloc, hashCode);
			}
			else {
				var label = ilGenerator.DefineLabel();
				var hold = ilGenerator.DeclareLocal(memberType);
				loadInstanceOpCode(ilGenerator);
				loadReferenceTypeMember(ilGenerator);
				ilGenerator.Emit(OpCodes.Stloc, hold);
				ilGenerator.Emit(OpCodes.Ldloc, hold);
				ilGenerator.Emit(OpCodes.Brfalse_S, label);
				ilGenerator.Emit(OpCodes.Ldloc, hashCode);
				ilGenerator.Emit(OpCodes.Ldc_I4, prime);
				ilGenerator.Emit(OpCodes.Mul);
				ilGenerator.Emit(OpCodes.Ldloc, hold);
				if (memberType != typeof(String) && memberType.IsEnumberable(out t))
					ilGenerator.Emit(OpCodes.Call, typeof(GetHashCodeInternals).GetMethod(nameof(GetEnumerableHashCode), BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(t));
				else
					ilGenerator.Emit(OpCodes.Callvirt, objectGetHashCode);
				ilGenerator.Emit(OpCodes.Add);
				ilGenerator.Emit(OpCodes.Stloc, hashCode);
				ilGenerator.MarkLabel(label);
			}
		}
	}
}