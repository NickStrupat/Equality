using System;

namespace NUnit.Tests {
	struct Foo : IEquatable<Foo> {
		public Boolean a;
		public String b;
		public Int64 Count { get; set; }
		public Bar Bar { get; set; }

		public Foo(Boolean x) {
			a = x;
			b = "Test";
			Count = 42;
			Bar = new Bar();
		}

		public Boolean Equals(Foo o) {
			if (a != o.a)
				return false;
			if (b != o.b)
				return false;
			if (Count != o.Count)
				return false;
			var bar = Bar;
			var bar2 = o.Bar;
			if (ReferenceEquals(bar, null) && ReferenceEquals(bar2, null))
				return true;
			return bar != null && bar.Equals(bar2);
		}

		private static Boolean Equals(ref Foo x, ref Foo y) {
			var bar = x.Bar;
			var bar2 = y.Bar;
			if (ReferenceEquals(bar, null)) {
				if (!ReferenceEquals(bar2, null))
					return false;
			}
			else if (!bar.Equals(bar2))
				return false;

			if (!x.a.Equals(y.a))
				return false;
			if (!Equals(x.Count, y.Count))
				return false;
			return true;
			//return x.Bar.Equals(y.Bar) && x.a.Equals(y.a) && x.Count.Equals(y.Count);
		}

		public override Boolean Equals(Object obj) => obj is Foo && Equals((Foo)obj);

		public override Int32 GetHashCode() {
			unchecked {
				Int32 hashCode = BasicTests.seed;
				hashCode = hashCode * BasicTests.prime + a.GetHashCode();
				var b = this.b;
				if (b != null)
					hashCode = hashCode * BasicTests.prime + b.GetHashCode();
				hashCode = hashCode * BasicTests.prime + Count.GetHashCode();
				var bar = this.Bar;
				if (bar != null)
					hashCode = hashCode * BasicTests.prime + bar.GetHashCode();
				return hashCode;
			}
		}
	}
}