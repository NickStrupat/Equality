# Equality
The last .NET equality solution you'll ever need. Automatically produces equality comparison and hash-code generation for any type by emitting IL based on your type. Emitted code is cached and specialized for struct and class types. Specify fields and auto-properties to ignore, as well as properties you want to include by applying attributes.

###Usage

Given the following types:

	class Foo {
		public String Text { get; set; }
		public Int32 Count { get; set; }
		public Bar Bar { get; set; }

		public override Boolean Equals(Object obj) => Class.Equals(this, obj);
		public override Int32 GetHashCode() => Class.GetHashCode(this);
	}

	struct Bar : IEquatable<Bar> {
		public Int32 Number;
		public Boolean Flag;

		public Boolean Equals(Bar other) => Struct.Equals(ref this, ref other);
		public override Boolean Equals(Object obj) => Struct.Equals(ref this, obj);
		public override Int32 GetHashCode() => Struct.GetHashCode(ref this);
	}

The following Equals and GetHashCode implementations are generated, cached, and called at runtime (these are decompiled straight from the assembly which saves the dynamic methods to disk in DEBUG mode)

```csharp
	public static bool GetClassEqualsFunc_Foo(Foo foo1, Foo foo2) {
		string str = foo1.<Text>k__BackingField;
		string str2 = foo2.<Text>k__BackingField;
		if (str == null)
			if (str2 != null)
				return false;
		else if (!str.Equals(str2))
			return false;
		if (foo1.<Count>k__BackingField != foo2.<Count>k__BackingField)
			return false;
		if (!foo1.<Bar>k__BackingField.Equals(foo2.<Bar>k__BackingField))
			return false;
		return true;
	}

	public static bool GetStructEqualsFunc_Bar(ref Bar barRef1, ref Bar barRef2) {
		if (barRef1.Number != barRef2.Number)
			return false;
		if (barRef1.Flag != barRef2.Flag)
			return false;
		return true;
	}

	public static int GetClassHashCodeFunc_Foo(Foo foo1) {
		int num = 0x51ed270b;
		string str = foo1.<Text>k__BackingField;
		if (str != null)
			num = (num * -1521134295) + str.GetHashCode();
		num = (num * -1521134295) + foo1.<Count>k__BackingField.GetHashCode();
		return ((num * -1521134295) + foo1.<Bar>k__BackingField.GetHashCode());
	}

	public static int GetStructHashCodeFunc_Bar(ref Bar barRef1) {
		int num = 0x51ed270b;
		num = (num * -1521134295) + barRef1.Number.GetHashCode();
		return ((num * -1521134295) + barRef1.Flag.GetHashCode());
	}
```

###TODO

- ~~Handle enumerables~~
- ~~Revamp the attribute system for more configurability (and incorporate the comparison settings)~~
- ~~Optimize Dictionary structural comparison so it doesn't box value types while comparing~~ (needs testing)
- Fluent-style API for configuration (probably won't do this)
- AOP tool to IL-weave the GetHashCode and Equals overrides into user classes to call the respective 'Equality' methods
	- Fody
	- PostSharp