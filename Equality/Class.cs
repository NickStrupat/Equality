using System;

namespace Equality {
	public static class Class {
		public static Int32 GetHashCode<T>(T @object)  where T : class => GetClassHashCodeInternal(@object, @object.GetType());
		public static Boolean Equals<T>(T x, T y)      where T : class => ClassEqualsInternal(x, y);
		public static Boolean Equals<T>(T x, Object y) where T : class => ClassEqualsInternal(x, y as T);

		private static Int32 GetClassHashCodeInternal<T>(T @object, Type type) where T : class {
			if (type == typeof (T))
				return GetHashCodeInternals.StaticCache<T>.Func.Invoke(ref @object);
			Object o = @object;
			return GetHashCodeInternals.DynamicCache.GetOrAdd(type, GetHashCodeInternals.GetHashCodeFunc<Object>).Invoke(ref o);
		}

		private static Boolean ClassEqualsInternal<T>(T x, T y) where T : class {
			if (x == null || y == null)
				return false;
			if (ReferenceEquals(x, y))
				return true;
			var xType = x.GetType();
			var yType = y.GetType();
			if (xType != yType)
				return false;
			if (xType == typeof(T))
				return EqualsInternals.StaticCache<T>.Func.Invoke(ref x, ref y);
			Object o = x;
			Object o2 = y;
			return EqualsInternals.DynamicCache.GetOrAdd(xType, EqualsInternals.GetEqualsFunc<Object>).Invoke(ref o, ref o2);
		}
	}
}