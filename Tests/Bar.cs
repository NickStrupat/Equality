using System;
using System.Linq;

using Equality;

namespace NUnit.Tests {
	[MemberEqualityDefaults(FieldsAndAutoProperties = Composition.Include, Collections = Comparison.Instance)]
	class Bar : IEquatable<Bar> {
		public Foo Foo;
		public String Text { get; set; }
		public Int64 Number;
		[MemberEquality(Composition = Composition.Include, CollectionComparison = Comparison.Structure)]
		public Int32[] Numbers = { 1, 2, 3 };

		public override Boolean Equals(Object obj) => Equals(obj as Bar);
		public Boolean Equals(Bar other) {
			if (other == null)
				return false;
			if (ReferenceEquals(this, other))
				return true;
			if (typeof(Bar) != other.GetType())
				return false;
			return Equals(this, other);
		}

		private static Boolean Equals(Bar x, Bar y) {
			if (!x.Foo.Equals(y.Foo))
				return false;
			var text = x.Text;
			var text2 = y.Text;
			if (ReferenceEquals(text, null)) {
				if (!ReferenceEquals(text2, null))
					return false;
			}
			else if (!text.Equals(text2))
				return false;
			if (x.Number != y.Number)
				return false;
			var numbers = x.Numbers;
			var numbers2 = y.Numbers;
			if (ReferenceEquals(numbers, null)) {
				if (!ReferenceEquals(numbers2, null))
					return false;
			}
			//else if (!numbers.Equals(numbers2))
			//	return false;
			else if (!numbers.SequenceEqual(numbers2, StructEqualityComparer<Int32>.Default))
				return false;
			return true;
		}

		public override Int32 GetHashCode() {
			unchecked {
				int num = 0x51ed270b;
				num = (num * -1521134295) + Foo.GetHashCode();
				num = (num * -1521134295) + Number.GetHashCode();
				int[] numbers = Numbers;
				if (numbers != null) {
					var numbersHashCode = BasicTests.seed;
					for (var i = 0; i != numbers.Length; ++i)
						numbersHashCode = numbersHashCode * BasicTests.prime + numbers[i].GetHashCode();
					num = num * BasicTests.prime + numbersHashCode;
				}
				string str = Text;
				if (str != null) {
					num = (num * -1521134295) + str.GetHashCode();
				}
				return num;


				//Int32 hashCode = BasicTests.seed;
				//hashCode = hashCode * BasicTests.prime + Foo.GetHashCode();
				//var text = this.Text;
				//if (text != null)
				//	hashCode = hashCode * BasicTests.prime + text.GetHashCode();
				//hashCode = hashCode * BasicTests.prime + Number.GetHashCode();
				//var numbers = this.Numbers;
				//if (numbers != null) {
				//	//hashCode = hashCode * prime + numbers.GetHashCode();
				//	var numbersHashCode = BasicTests.seed;
				//	for (var i = 0; i != numbers.Length; ++i)
				//		numbersHashCode = numbersHashCode * BasicTests.prime + numbers[i].GetHashCode();
				//	hashCode = hashCode * BasicTests.prime + numbersHashCode;
				//}
				//return hashCode;
			}
		}
	}
}