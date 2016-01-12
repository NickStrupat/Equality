using System;
using System.Collections.Generic;

namespace Equality {
	public class ClassEqualityComparer<T> : IEqualityComparer<T> where T : class {
		public static ClassEqualityComparer<T> Default = new ClassEqualityComparer<T>();

		public Boolean Equals(T x, T y) => Class.Equals(x, y);
		public Int32 GetHashCode(T x) => Class.GetHashCode(x);
	}
}