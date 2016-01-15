using System;

using Equality;

#if NUNIT
using NUnit.Framework;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif

namespace Tests {
#if NUNIT
	[TestFixture]
#else
	[TestClass]
#endif
	public class BasicTests {
		internal const Int32 prime = -1521134295;
		internal const Int32 seed = 1374496523;
#if DEBUG
#	if NUNIT
		[SetUp]
#	else
		[TestInitialize]
#	endif
		public void Initialize() { }

#	if NUNIT
		[TearDown]
#	else
		[TestCleanup]
#	endif
		public void Cleanup() => GC.WaitForPendingFinalizers();
#endif

#	if NUNIT
		[Test]
#	else
		[TestMethod]
#	endif
		public void TestStructHashCode() {
			var foo = new Foo(true);
			Assert.AreEqual(foo.GetHashCode(), Struct.GetHashCode(ref foo));
		}

#	if NUNIT
		[Test]
#	else
		[TestMethod]
#	endif
		public void TestClassHashCode() {
			var bar = new Bar { Foo = new Foo(true), Text = "Testing" };
			Assert.AreEqual(bar.GetHashCode(), Class.GetHashCode(bar));
		}

#	if NUNIT
		[Test]
#	else
		[TestMethod]
#	endif
		public void TestStructEquals() {
			var foo = new Foo(true);
			var foo2 = new Foo(false);
			Assert.IsTrue(foo.Equals(foo));
			Assert.IsTrue(!foo.Equals(foo2));
			Assert.IsTrue(Struct.Equals(ref foo, ref foo));
			Assert.IsTrue(!Struct.Equals(ref foo, ref foo2));
		}

#	if NUNIT
		[Test]
#	else
		[TestMethod]
#	endif
		public void TestClassEquals() {
			var bar = new Bar { Text = "Text" };
			var bar2 = new Bar { Text = "Text" };
			var bar3 = new Bar { Text = "What" };
			var bar4 = new Bar { Text = "Who" };
			Assert.IsTrue(bar.Equals(bar));
			Assert.IsTrue(bar.Equals(bar2));
			Assert.IsFalse(bar.Equals(bar3));
			Assert.IsFalse(bar.Equals(bar4));

			Assert.IsTrue(Class.Equals(bar, bar));
			Assert.IsTrue(Class.Equals(bar, bar2));
			Assert.IsFalse(Class.Equals(bar, bar3));
			Assert.IsFalse(Class.Equals(bar, bar4));

			var baz = new Baz() { Yep = "Yep", Text = "Text" };
			var baz2 = new Baz() { Yep = "Yep", Text = "Text" };
			var baz3 = new Baz() { Yep = "Okay", Text = "Word" };
			var baz4 = new Baz() { Yep = "Okay", Text = "Car" };
			Assert.IsFalse(bar.Equals(baz));
		}
	}
}
