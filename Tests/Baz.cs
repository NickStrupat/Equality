using System;

using Tests;

namespace Tests {
	class Baz : Bar, IEquatable<Baz> {
		public String Yep { get; set; }

		public override Boolean Equals(Object obj) => Equals(obj as Baz);
		public Boolean Equals(Baz other) {
			if (other == null)
				return false;
			if (ReferenceEquals(this, other))
				return true;
			if (typeof(Bar) != other.GetType())
				return false;
			return Foo.Equals(other.Foo) && Text == other.Text && Yep == other.Yep;
		}

		public override Int32 GetHashCode() {
			unchecked {
				Int32 hashCode = BasicTests.seed;
				hashCode = hashCode * BasicTests.prime + base.GetHashCode();
				var yep = this.Yep;
				if (yep != null)
					hashCode = hashCode * BasicTests.prime + yep.GetHashCode();
				return hashCode;
			}
		}
	}
}