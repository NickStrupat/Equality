using System;

namespace Equality {
	public static class Struct {
		public static Int32 GetHashCode<T>(ref T @object)  where T : struct => GetHashCodeInternals.StaticCache<T>.Func(ref @object);
		public static Boolean Equals<T>(ref T x, ref T y)  where T : struct => EqualsInternals.StaticCache<T>.Func(ref x, ref y);
		public static Boolean Equals<T>(ref T x, Object y) where T : struct {
			if (y == null || y.GetType() != typeof(T))
				return false;
			var o = (T)y;
			return Equals(ref x, ref o);
		}
	}
}