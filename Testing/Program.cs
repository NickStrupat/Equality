using System;

using Equality;

namespace Testing {
	[MemberEqualityDefaults(Composition.Exclude, Comparison.Structure)]
	class Program {
		[MemberEquality(Composition.Include, Comparison.Structure)]
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
