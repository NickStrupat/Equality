using System;

namespace Equality {
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
	public class ExcludeMembersByDefault : Attribute {}

	[AttributeUsage(AttributeTargets.Field)]
	public class IncludeFieldAttribute : Attribute { }

	[AttributeUsage(AttributeTargets.Property)]
	public class IncludeAutoPropertyAttribute : Attribute { }


	[AttributeUsage(AttributeTargets.Field)]
	public class ExcludeFieldAttribute : Attribute {}

	[AttributeUsage(AttributeTargets.Property)]
	public class ExcludeAutoPropertyAttribute : Attribute {}

	[AttributeUsage(AttributeTargets.Property)]
	public class IncludePropertyAttribute : Attribute {}
}