using System;
using System.Collections.Generic;

namespace Equality {
	public struct StructEqualityComparer<T> : IEqualityComparer<T> where T : struct {
		public static StructEqualityComparer<T> Default = new StructEqualityComparer<T>();

		public Boolean Equals(T x, T y) => Struct.Equals(ref x, ref y);
		public Int32 GetHashCode(T x) => Struct.GetHashCode(ref x);

		public static Boolean Equals(ref T x, ref T y) => Struct.Equals(ref x, ref y);
		public static Int32 GetHashCode(ref T x) => Struct.GetHashCode(ref x);
	}
}