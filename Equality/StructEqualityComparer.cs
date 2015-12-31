using System;
using System.Collections.Generic;

namespace Equality {
	public struct StructEqualityComparer<T> : IEqualityComparer<T> where T : struct {
		public static StructEqualityComparer<T> Default = new StructEqualityComparer<T>();

		public Boolean Equals(T x, T y) => x.StructEquals(y);
		public Int32 GetHashCode(T x) => x.GetStructHashCode();
	}
}