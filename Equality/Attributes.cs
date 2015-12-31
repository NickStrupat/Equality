using System;

namespace Equality {
	[AttributeUsage(AttributeTargets.Field)]
	public class ExcludeFieldAttribute : Attribute {}

	[AttributeUsage(AttributeTargets.Property)]
	public class ExcludeAutoPropertyAttribute : Attribute {}

	[AttributeUsage(AttributeTargets.Property)]
	public class IncludePropertyAttribute : Attribute {}
}