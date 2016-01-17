using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Equality {
	internal static class DictionaryComparer {
		public static Boolean Equals(IDictionary first, IDictionary second) => EqualsCache.GetOrAdd(first.GetType(), EqualsFactory).Invoke(first, second);
		public static Int32 GetHashCode(IDictionary dictionary) => GetHashCodeCache.GetOrAdd(dictionary.GetType(), GetHashCodeFactory).Invoke(dictionary);

		private static DictionaryEquals EqualsFactory(Type type) => Factory<DictionaryEquals>(type, DictionaryEqualsImpl, DictionaryEqualsGenericImplMethodInfo);
		private static DictionaryGetHashCode GetHashCodeFactory(Type type) => Factory<DictionaryGetHashCode>(type, DictionaryHashCodeImpl, DictionaryHashCodeGenericImplMethodInfo);

		private delegate Boolean DictionaryEquals(IDictionary first, IDictionary second);
		private delegate Int32 DictionaryGetHashCode(IDictionary dictionary);

		private static readonly MethodInfo DictionaryHashCodeGenericImplMethodInfo = new DictionaryGetHashCode(DictionaryHashCodeGenericImpl<Object, Object>).Method.GetGenericMethodDefinition();
		private static readonly MethodInfo DictionaryEqualsGenericImplMethodInfo = new DictionaryEquals(DictionaryEqualsGenericImpl<Object, Object>).Method.GetGenericMethodDefinition();

		private static readonly ConcurrentDictionary<Type, DictionaryEquals> EqualsCache = new ConcurrentDictionary<Type, DictionaryEquals>();
		private static readonly ConcurrentDictionary<Type, DictionaryGetHashCode> GetHashCodeCache = new ConcurrentDictionary<Type, DictionaryGetHashCode>();

		private static TDelegate Factory<TDelegate>(Type type, TDelegate impl, MethodInfo genericImpl) where TDelegate : class {
			var genericTypeArguments = type.GenericTypeArguments;
			if (genericTypeArguments.Length != 2)
				return impl;
			var keyType = genericTypeArguments[0];
			var valueType = genericTypeArguments[1];
			return Common.GenerateIL<TDelegate>((t, ilGenerator) => {
				ilGenerator.Emit(OpCodes.Ldarg_0);
				ilGenerator.Emit(OpCodes.Ldarg_1);
				ilGenerator.Emit(OpCodes.Call, genericImpl.MakeGenericMethod(keyType, valueType));
				ilGenerator.Emit(OpCodes.Ret);
			}, type);
		}

		private static Boolean DictionaryEqualsImpl(IDictionary first, IDictionary second) {
			if (first.Count != second.Count)
				return false;
			var firstKeys = first.Keys.Cast<Object>().OrderBy(x => x);
			var secondKeys = second.Keys.Cast<Object>().OrderBy(x => x);
			if (!firstKeys.SequenceEqual(secondKeys))
				return false;
			try {
				foreach (var key in firstKeys)
					if (!first[key].Equals(second[key]))
						return false;
			}
			catch (KeyNotFoundException) {
				return false;
			}
			return true;
		}

		private static Boolean DictionaryEqualsGenericImpl<TKey, TValue>(IDictionary a, IDictionary b) {
			var first = (IDictionary<TKey, TValue>) a;
			var second = (IDictionary<TKey, TValue>) b;
			if (first.Count != second.Count)
				return false;
			var firstKeys = first.Keys.OrderBy(x => x);
			var secondKeys = second.Keys.OrderBy(x => x);
			if (!firstKeys.SequenceEqual(secondKeys))
				return false;
			var comparer = EqualityComparer<TValue>.Default;
			try {
				foreach (var key in firstKeys)
					if (!comparer.Equals(first[key], second[key]))
						return false;
			}
			catch (KeyNotFoundException) {
				return false;
			}
			return true;
		}

		private static Int32 DictionaryHashCodeImpl(IDictionary dictionary) {
			Int32 hashCode = GetHashCodeInternals.Seed;
			var keys = dictionary.Keys.Cast<Object>().OrderBy(x => x);
			foreach (var key in keys) {
				var b = key;
				if (b != null)
					hashCode = hashCode * GetHashCodeInternals.Prime + b.GetHashCode();
				var c = dictionary[key];
				if (c != null)
					hashCode = hashCode * GetHashCodeInternals.Prime + c.GetHashCode();
			}
			return hashCode;
		}

		private static Int32 DictionaryHashCodeGenericImpl<TKey, TValue>(IDictionary a) {
			var dictionary = (IDictionary<TKey, TValue>) a;
			Int32 hashCode = GetHashCodeInternals.Seed;
			var keys = dictionary.Keys.OrderBy(x => x);
			foreach (var key in keys) {
				var b = key;
				if (b != null)
					hashCode = hashCode * GetHashCodeInternals.Prime + b.GetHashCode();
				var c = dictionary[key];
				if (c != null)
					hashCode = hashCode * GetHashCodeInternals.Prime + c.GetHashCode();
			}
			return hashCode;
		}
	}
}