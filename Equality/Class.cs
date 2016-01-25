using System;
using System.Reflection;

namespace Equality {
	public static class Class {
		public static Int32 GetHashCode<T>(T @object)  where T : class => GetHashCodeInternal(@object, @object.GetType());

		public static Boolean Equals<T>(T x, T y)      where T : class => EqualsInternal(x, y);

		public static Boolean Equals<T>(T x, Object y) where T : class => EqualsInternal(x, y as T);

		private static Int32 GetHashCodeInternal<T>(T x, Type type) where T : class {
			if (type == typeof (T))
				return GetHashCodeInternals.StaticClassCache<T>.Func.Invoke(x);
			return GetHashCodeInternals.GetDynamicClassHashCode(type).Invoke(x);
		}

		private static Boolean EqualsInternal<T>(T x, T y) where T : class {
			if (x == null || y == null)
				return false;
			if (ReferenceEquals(x, y))
				return true;
			var xType = x.GetType();
			var yType = y.GetType();
			if (xType != yType)
				return false;
			if (xType == typeof(T))
				return EqualsInternals.StaticClassCache<T>.Func.Invoke(x, y);
			return EqualsInternals.GetDynamicClassEquals(xType).Invoke(x, y);
		}

		private class X { }
		internal static readonly MethodInfo GetHashCodeMethodInfo = new GetHashCodeInternals.GetClassHashCode<X>(GetHashCode).Method.GetGenericMethodDefinition();
		internal static readonly MethodInfo EqualsMethodInfo = new EqualsInternals.ClassEquals<X>(Equals).Method.GetGenericMethodDefinition();
		internal static readonly MethodInfo EqualsObjectMethodInfo = new EqualsInternals.ClassEqualsObject<X>(Equals).Method.GetGenericMethodDefinition();
	}
}