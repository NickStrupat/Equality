using System;
using System.Reflection;

namespace Equality {
	public static class Struct {
		public static Int32 GetHashCode<T>(ref T @object)  where T : struct => GetHashCodeInternals.StaticStructCache<T>.Func(ref @object);

		public static Boolean Equals<T>(ref T x, ref T y)  where T : struct => EqualsInternals.StaticStructCache<T>.Func(ref x, ref y);

		public static Boolean Equals<T>(ref T x, Object y) where T : struct {
			if (y == null || y.GetType() != typeof(T))
				return false;
			var o = (T)y;
			return Equals(ref x, ref o);
		}

		private struct X { }
		internal static readonly MethodInfo GetHashCodeMethodInfo = new GetHashCodeInternals.GetStructHashCode<X>(GetHashCode).Method.GetGenericMethodDefinition();
		internal static readonly MethodInfo EqualsMethodInfo = new EqualsInternals.StructEquals<X>(Equals).Method.GetGenericMethodDefinition();
		internal static readonly MethodInfo EqualsObjectMethodInfo = new EqualsInternals.StructEqualsObject<X>(Equals).Method.GetGenericMethodDefinition();
	}
}