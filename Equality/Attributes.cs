using System;

namespace Equality {
	public enum Composition { Include, Exclude }
	public enum Comparison { Instance, Structure }
	public enum Depth { Memberwise, Recursive }

	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
	public class MemberEqualityDefaultsAttribute : Attribute, IMemberEqualityDefaultsAttribute {
		private Composition? fieldsAndAutoProperties;
		private Comparison? collections;

		public Composition FieldsAndAutoProperties { get { return fieldsAndAutoProperties.GetValueOrDefault(); } set { fieldsAndAutoProperties = value; } }
		public Comparison Collections { get { return collections.GetValueOrDefault(); } set { collections = value; } }

		Composition? IMemberEqualityDefaultsAttribute.FieldsAndAutoProperties => fieldsAndAutoProperties;
		Comparison? IMemberEqualityDefaultsAttribute.Collections => collections;
	}

	internal interface IMemberEqualityDefaultsAttribute {
		Composition? FieldsAndAutoProperties { get; }
		Comparison? Collections { get; }
	}

	internal class InternalMemberEqualityAttribute : Attribute, IMemberEqualityAttribute {
		private readonly Composition? member;
		private readonly Comparison? collection;

		Composition? IMemberEqualityAttribute.MemberComposition => member;
		Comparison? IMemberEqualityAttribute.CollectionComparison => collection;
	}

	[AttributeUsage(AttributeTargets.Field)]
	public class FieldEqualityAttribute : Attribute, IMemberEqualityAttribute/*, IFieldEqualityAttribute*/ {
		private Composition? composition;
		private Comparison? collectionComparison;

		public Composition Composition { get { return composition.GetValueOrDefault(); } set { composition = value; } }
		public Comparison CollectionComparison { get { return collectionComparison.GetValueOrDefault(); } set { collectionComparison = value; } }

		Composition? IMemberEqualityAttribute.MemberComposition => composition;
		Comparison? IMemberEqualityAttribute.CollectionComparison => collectionComparison;
	}

	[AttributeUsage(AttributeTargets.Property)]
	public class AutoPropertyEqualityAttribute : Attribute, IMemberEqualityAttribute/*, IAutoPropertyEqualityAttribute*/ {
		private Composition? composition;
		private Comparison? collectionComparison;

		public Composition Composition { get { return composition.GetValueOrDefault(); } set { composition = value; } }
		public Comparison CollectionComparison { get { return collectionComparison.GetValueOrDefault(); } set { collectionComparison = value; } }

		Composition? IMemberEqualityAttribute.MemberComposition => composition;
		Comparison? IMemberEqualityAttribute.CollectionComparison => collectionComparison;
	}

	[AttributeUsage(AttributeTargets.Property)]
	public class IncludePropertyAttribute : Attribute, IIncludePropertyAttribute {
		private readonly Comparison? propertyComparison;

		public IncludePropertyAttribute() { }
		public IncludePropertyAttribute(Comparison propertyComparison) { this.propertyComparison = propertyComparison; }

		Comparison? IIncludePropertyAttribute.PropertyComparison => propertyComparison;
	}

	internal interface IMemberCollectionComparisonAttribute {
		Comparison? CollectionComparison { get; }
	}

	internal interface IMemberEqualityAttribute {
		Composition? MemberComposition { get; }
		Comparison? CollectionComparison { get; }
	}

	internal interface IFieldEqualityAttribute {
		Composition? FieldComposition { get; }
		Comparison? FieldComparison { get; }
	}

	internal interface IAutoPropertyEqualityAttribute {
		Composition? AutoPropertyComposition { get; }
		Comparison? AutoPropertyComparison { get; }
	}

	internal interface IIncludePropertyAttribute {
		Comparison? PropertyComparison { get; }
	}
}