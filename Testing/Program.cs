using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Equality;

namespace Testing {
	//[MemberEqualityDefaults(FieldsAndAutoProperties = Composition.Exclude, Collections = Comparison.Structure)]
	class Program {
		//[MemberEquality(Composition = Composition.Include, CollectionComparison = Comparison.Structure)]
		public String Text { get; set; } = "Hello";

		static void Main(String[] args) {
			var p = new Program();
			var p2 = new Program() {Text = "Goodbye"};
			Console.WriteLine(p.Equals(p2));
		}

		public override Boolean Equals(Object obj) => Class.Equals(this, obj);
		public override Int32 GetHashCode() => Class.GetHashCode(this);
	}
}
