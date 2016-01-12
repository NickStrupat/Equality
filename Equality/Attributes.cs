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

	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	public class MemberEqualityAttribute : Attribute, IMemberEqualityAttribute {
		private Composition? composition;
		private Comparison? collectionComparison;

		public Composition Composition { get { return composition.GetValueOrDefault(); } set { composition = value; } }
		public Comparison CollectionComparison { get { return collectionComparison.GetValueOrDefault(); } set { collectionComparison = value; } }

		Composition? IMemberEqualityAttribute.MemberComposition => composition;
		Comparison? IMemberEqualityAttribute.CollectionComparison => collectionComparison;
	}

	internal interface IMemberEqualityAttribute {
		Composition? MemberComposition { get; }
		Comparison? CollectionComparison { get; }
	}

	internal class InternalMemberEqualityAttribute : Attribute, IMemberEqualityAttribute {
		private Composition? composition;
		private Comparison? collectionComparison;

		public Composition Composition { get { return composition.GetValueOrDefault(); } set { composition = value; } }
		public Comparison CollectionComparison { get { return collectionComparison.GetValueOrDefault(); } set { collectionComparison = value; } }

		Composition? IMemberEqualityAttribute.MemberComposition => composition;
		Comparison? IMemberEqualityAttribute.CollectionComparison => collectionComparison;
	}
}