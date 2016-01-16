using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Equality {
	public static class Struct {
		public static Int32 GetHashCode<T>(T @object) where T : struct => GetHashCode(ref @object);

		public static Int32 GetHashCode<T>(ref T @object)  where T : struct => GetHashCodeInternals.StaticStructCache<T>.Func(ref @object);

		public static Boolean Equals<T>(ref T x, ref T y)  where T : struct {
			if (ReferenceEquals(ref x, ref y))
				return true;
			return EqualsInternals.StaticStructCache<T>.Func(ref x, ref y);
		}

		public static Boolean Equals<T>(ref T x, Object y) where T : struct {
			if (y == null || y.GetType() != typeof(T))
				return false;
			var o = (T)y;
			return Equals(ref x, ref o);
		}

		public static Boolean ReferenceEquals<T>(ref T x, ref T y) where T : struct => Cache<T>.Func(ref x, ref y);

		private static class Cache<T> where T : struct {
			public delegate Boolean ReferenceEqualsDelegate(ref T a, ref T b);

			public static readonly ReferenceEqualsDelegate Func = Common.GenerateIL<ReferenceEqualsDelegate>(ILGeneration, typeof(T));

			private static readonly FieldInfo typedReferenceValueFieldInfo = typeof(TypedReference).GetField("Value", BindingFlags.NonPublic | BindingFlags.Instance);

			private static void ILGeneration(Type type, ILGenerator ilGenerator) {
				ilGenerator.Emit(OpCodes.Ldarg_0);
				ilGenerator.Emit(OpCodes.Mkrefany, typeof(T));
				ilGenerator.Emit(OpCodes.Ldfld, typedReferenceValueFieldInfo);

				ilGenerator.Emit(OpCodes.Ldarg_1);
				ilGenerator.Emit(OpCodes.Mkrefany, typeof(T));
				ilGenerator.Emit(OpCodes.Ldfld, typedReferenceValueFieldInfo);

				ilGenerator.Emit(OpCodes.Call, typeof(IntPtr).GetMethod("op_Equality", new[] { typeof(IntPtr), typeof(IntPtr) }));
				ilGenerator.Emit(OpCodes.Ret);
			}
		}

		private struct X { }
		internal static readonly MethodInfo GetHashCodeMethodInfo = new GetHashCodeInternals.GetStructHashCode<X>(GetHashCode).Method.GetGenericMethodDefinition();
		internal static readonly MethodInfo EqualsMethodInfo = new EqualsInternals.StructEquals<X>(Equals).Method.GetGenericMethodDefinition();
		internal static readonly MethodInfo EqualsObjectMethodInfo = new EqualsInternals.StructEqualsObject<X>(Equals).Method.GetGenericMethodDefinition();
	}
}