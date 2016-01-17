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
		//const Int32 Prime = 486187739; // number from http://stackoverflow.com/a/2816747/232574
		internal const Int32 Prime = -1521134295; // http://stackoverflow.com/questions/371328/why-is-it-important-to-override-gethashcode-when-equals-method-is-overridden#comment271215_371348
		internal const Int32 Seed = 1374496523;   // ^^^

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

		private static Int32 GetEnumerableOfClassHashCode<T>(IEnumerable<T> enumerable) where T : class {
			var maybeHashCode = GetSpecializedEnumerableHashCode(enumerable);
			if (maybeHashCode != null)
				return maybeHashCode.GetValueOrDefault();
			
			Int32 hashCode = Seed;
			foreach (var x in enumerable)
				if (!ReferenceEquals(x, null))
					hashCode = hashCode * Prime + x.GetHashCode();
			return hashCode;
		}

		private static Int32 GetEnumerableOfStructHashCode<T>(IEnumerable<T> enumerable) where T : struct {
			var maybeHashCode = GetSpecializedEnumerableHashCode(enumerable);
			if (maybeHashCode != null)
				return maybeHashCode.GetValueOrDefault();

			Int32 hashCode = Seed;
			foreach (var x in enumerable)
				hashCode = hashCode * Prime + x.GetHashCode();
			return hashCode;
		}

		private delegate Int32 GetEnumerableOfClassHashCodeDelegate<in T>(IEnumerable<T> enumerable) where T : class;
		private delegate Int32 GetEnumerableOfStructHashCodeDelegate<in T>(IEnumerable<T> enumerable) where T : struct;

		internal static readonly MethodInfo GetEnumerableOfClassHashCodeMethodInfo = new GetEnumerableOfClassHashCodeDelegate<Object>(GetEnumerableOfClassHashCode).Method.GetGenericMethodDefinition();
		internal static readonly MethodInfo GetEnumerableOfStructHashCodeMethodInfo = new GetEnumerableOfStructHashCodeDelegate<Byte>(GetEnumerableOfStructHashCode).Method.GetGenericMethodDefinition();

		private static Int32? GetSpecializedEnumerableHashCode<T>(IEnumerable<T> enumerable) {
			var dictionary = enumerable as IDictionary;
			if (dictionary != null)
				return DictionaryComparer.GetHashCode(dictionary);

			var list = enumerable as IList<T>;
			if (list != null)
				return GetArrayHashCode(list);

			if (enumerable is IStructuralEquatable)
				return StructuralComparisons.StructuralEqualityComparer.GetHashCode(enumerable);

			return null;
		}

		private static Int32 GetArrayHashCode<T>(IList<T> list) {
			Int32 hashCode = Seed;
			for (var i = 0; i != list.Count; ++i) {
				var a = list[i];
				if (a != null)
					hashCode = hashCode * Prime + a.GetHashCode();
			}
			return hashCode;
		}

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
			ilGenerator.Emit(OpCodes.Ldc_I4, Seed);
			ilGenerator.Emit(OpCodes.Stloc, hashCode);

			var fields = Common.GetFields(type);
			for (var i = 0; i < fields.Length; i++) {
				var field = fields[i];
				Action<ILGenerator> loadValueTypeMember = ilg => ilg.Emit(OpCodes.Ldflda, field);
				Action<ILGenerator> loadReferenceTypeMember = ilg => ilg.Emit(OpCodes.Ldfld, field);
				EmitMemberIL(ilGenerator, Prime, hashCode, loadInstanceOpCode, loadValueTypeMember, field, field.FieldType, loadReferenceTypeMember, objectGetHashCode);
			}

			var localMap = new ConcurrentDictionary<Type, LocalBuilder>();
			var properties = Common.GetProperties(type);
			for (var i = 0; i < properties.Length; i++) {
				var property = properties[i];
				Action<ILGenerator> loadValueTypeMember = ilg => {
					ilg.Emit(OpCodes.Call, property.GetGetMethod(nonPublic: true));
					var hold = localMap.GetOrAdd(property.PropertyType, ilg.DeclareLocal);
					ilg.Emit(OpCodes.Stloc, hold);
					ilg.Emit(OpCodes.Ldloca, hold);
				};
				Action<ILGenerator> loadReferenceTypeMember = loadValueTypeMember;
				EmitMemberIL(ilGenerator, Prime, hashCode, loadInstanceOpCode, loadValueTypeMember, property, property.PropertyType, loadReferenceTypeMember, objectGetHashCode);
			}

			ilGenerator.Emit(OpCodes.Ldloc, hashCode);
			ilGenerator.Emit(OpCodes.Ret);
		}

		private static void EmitMemberIL(ILGenerator ilGenerator,
			                             Int32 prime,
			                             LocalBuilder hashCode,
			                             Action<ILGenerator> loadInstanceOpCode,
			                             Action<ILGenerator> loadValueTypeMember,
			                             MemberInfo memberInfo,
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
				if (memberInfo.ShouldGetStructural(memberType, out t))
					ilGenerator.Emit(OpCodes.Call, MakeGenericGetEnumerableHashCodeMethod(t));
				else if (memberInfo.ShouldRecurse(memberType))
					ilGenerator.Emit(OpCodes.Call, Struct.GetHashCodeMethodInfo.MakeGenericMethod(memberType));
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
				if (memberInfo.ShouldGetStructural(memberType, out t))
					ilGenerator.Emit(OpCodes.Call, MakeGenericGetEnumerableHashCodeMethod(t));
				else if (memberInfo.ShouldRecurse(memberType))
					ilGenerator.Emit(OpCodes.Call, Class.GetHashCodeMethodInfo.MakeGenericMethod(memberType));
				else
					ilGenerator.Emit(OpCodes.Callvirt, objectGetHashCode);
				ilGenerator.Emit(OpCodes.Add);
				ilGenerator.Emit(OpCodes.Stloc, hashCode);
				ilGenerator.MarkLabel(label);
			}
		}

		private static MethodInfo MakeGenericGetEnumerableHashCodeMethod(Type t) {
			return (t.IsValueType ? GetEnumerableOfStructHashCodeMethodInfo : GetEnumerableOfClassHashCodeMethodInfo).MakeGenericMethod(t);
		}
	}
}