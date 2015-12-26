using System;

using Equality;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests {
	[TestClass]
	public class BasicTests {
		struct Foo : IEquatable<Foo> {
			public Boolean a;
			public String b;
			public Int64 Count { get; set; }

			public Foo(Boolean x) {
				a = x;
				b = "Test";
				Count = 42;
			}

			public Boolean Equals(Foo o) {
				return a == o.a
					&& b == o.b
					&& Count == o.Count;
			}

			public override Boolean Equals(Object obj) => Equals((Foo)obj);

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
		public void TestStructHashCode() {
			var foo = new Foo(true);
			Assert.AreEqual(foo.GetHashCode(), foo.GetStructHashCode());
		}

		[TestMethod]
		public void TestClassHashCode() {
			var bar = new Bar { Foo = new Foo(true), Text = "Testing" };
			Assert.AreEqual(bar.GetHashCode(), bar.GetClassHashCode());
		}

		[TestMethod]
		public void TestStructEquals() {
			var foo = new Foo(true);
			var foo2 = new Foo(false);
			Assert.IsTrue(foo.Equals(foo));
			Assert.IsTrue(!foo.Equals(foo2));
			Assert.IsTrue(foo.StructEquals(foo));
			Assert.IsTrue(!foo.StructEquals(foo2));
		}
	}
}
