﻿using System;

using Equality;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests {
	[TestClass]
	public class BasicTests {
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
				var bar = Bar;
				var bar2 = o.Bar;
				return a == o.a
					&& b == o.b
					&& Count == o.Count
					&& ((ReferenceEquals(bar, null) && ReferenceEquals(bar2, null)) || (bar != null && bar.Equals(bar2)));
			}

			public override Boolean Equals(Object obj) => obj is Foo && Equals((Foo)obj);

			public override Int32 GetHashCode() {
				unchecked {
					const Int32 prime = 486187739;
					Int32 hashCode;
					hashCode = a.GetHashCode();
					var b = this.b;
					if (b != null)
						hashCode = hashCode * prime + b.GetHashCode();
					hashCode = hashCode * prime + Count.GetHashCode();
					var bar = this.Bar;
					if (bar != null)
						hashCode = hashCode * prime + bar.GetHashCode();
					return hashCode;
				}
			}
		}

		class Bar : IEquatable<Bar> {
			public Foo Foo;
			public String Text { get; set; }

			public override Boolean Equals(Object obj) => Equals(obj as Bar);
			public Boolean Equals(Bar other) {
				if (other == null)
					return false;
				if (ReferenceEquals(this, other))
					return true;
				if (typeof(Bar) != other.GetType())
					return false;
				return Foo.Equals(other.Foo) && Text.Equals(other.Text);
			}

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
					const Int32 prime = 486187739;
					Int32 hashCode;
					hashCode = base.GetHashCode();
					var yep = this.Yep;
					if (yep != null)
						hashCode = hashCode * prime + yep.GetHashCode();
					return hashCode;
				}
			}
		}

		[TestMethod]
		public void TestStructHashCode() {
			var foo = new Foo(true);
			Assert.AreEqual(foo.GetHashCode(), Struct.GetHashCode(ref foo));
		}

		[TestMethod]
		public void TestClassHashCode() {
			var bar = new Bar { Foo = new Foo(true), Text = "Testing" };
			Assert.AreEqual(bar.GetHashCode(), Class.GetHashCode(bar));
		}

		[TestMethod]
		public void TestStructEquals() {
			var foo = new Foo(true);
			var foo2 = new Foo(false);
			Assert.IsTrue(foo.Equals(foo));
			Assert.IsTrue(!foo.Equals(foo2));
			Assert.IsTrue(Struct.Equals(ref foo, ref foo));
			Assert.IsTrue(!Struct.Equals(ref foo, ref foo2));
		}

		[TestMethod]
		public void TestClassEquals() {
			var bar  = new Bar { Text = "Text" };
			var bar2 = new Bar { Text = "Text" };
			var bar3 = new Bar { Text = "What" };
			var bar4 = new Bar { Text = "Who" };
			Assert.IsTrue(bar.Equals(bar));
			Assert.IsTrue(bar.Equals(bar2));
			Assert.IsFalse(bar.Equals(bar3));
			Assert.IsFalse(bar.Equals(bar4));
			Assert.AreEqual(bar.Equals(bar),    Class.Equals(bar, bar));
			Assert.AreEqual(bar.Equals(bar),    Class.Equals(bar, bar));
			Assert.AreEqual(bar.Equals(bar2),   Class.Equals(bar, bar2));
			Assert.AreEqual(!bar.Equals(bar3), !Class.Equals(bar, bar3));
			Assert.AreEqual(bar.Equals(bar4),   Class.Equals(bar, bar4));

			var baz = new Baz() { Yep = "Yep", Text = "Text" };
			var baz2 = new Baz() { Yep = "Yep", Text = "Text" };
			var baz3 = new Baz() { Yep = "Okay", Text = "Word" };
			var baz4 = new Baz() { Yep = "Okay", Text = "Car" };
			Assert.IsFalse(bar.Equals(baz));
		}
	}
}
