using System;

using Equality;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests {
	[TestClass]
	public class BasicTests {
		struct Foo {
			Boolean a;
			String b;
			public Int64 Count { get; private set; }

			public Foo(Boolean x) {
				a = true;
				b = "Test";
				Count = 42;
			}

			public override Int32 GetHashCode() {
				unchecked {
					const Int32 prime = 486187739;
					Int32 hashCode;
					hashCode = a.GetHashCode();
					var b = this.b;
					if (b != null)
						hashCode = hashCode * prime + b.GetHashCode();
					hashCode = hashCode * prime + Count.GetHashCode();
					return hashCode;
				}
			}
		}

		class Bar {
			public Foo Foo;
			public String Text { get; set; }

			public override Int32 GetHashCode() {
				unchecked {
					const Int32 prime = 486187739;
					Int32 hashCode;
					hashCode = Foo.GetHashCode();
					var text = this.Text;
					if (text != null)
						hashCode = hashCode * prime + text.GetHashCode();
					return hashCode;
				}
			}
		}

		[TestMethod]
		public void TestStruct() {
			var foo = new Foo(true);
			Assert.AreEqual(foo.GetHashCode(), foo.GetStructHashCode());
		}

		[TestMethod]
		public void TestClass() {
			var bar = new Bar { Foo = new Foo(true), Text = "Testing" };
			Assert.AreEqual(bar.GetHashCode(), bar.GetClassHashCode());
		}
	}
}
