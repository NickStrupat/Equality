using System;
using System.Collections.Generic;

namespace Equality {
	public struct ClassEqualityComparer<T> : IEqualityComparer<T> where T : class {
		public static ClassEqualityComparer<T> Default = new ClassEqualityComparer<T>();

		public Boolean Equals(T x, T y) => x.ClassEquals(y);
		public Int32 GetHashCode(T x) => x.GetClassHashCode();
	}
}