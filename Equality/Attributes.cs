using System;

namespace Equality {
	public enum Composition { Include, Exclude }
	public enum Comparison { Instance, Structure }
	public enum Depth { Memberwise, Recursive }

	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
	public class MemberEqualityDefaultsAttribute : Attribute, IMemberEqualityDefaultsAttribute {
		private Composition? fieldsAndAutoProperties;
		private Comparison? collections;
		private Depth? depth;

		public Composition FieldsAndAutoProperties { get { return fieldsAndAutoProperties.GetValueOrDefault(); } set { fieldsAndAutoProperties = value; } }
		public Comparison Collections { get { return collections.GetValueOrDefault(); } set { collections = value; } }
		public Depth Depth { get { return depth.GetValueOrDefault(); } set { depth = value; } }

		Composition? IMemberEqualityDefaultsAttribute.FieldsAndAutoProperties => fieldsAndAutoProperties;
		Comparison? IMemberEqualityDefaultsAttribute.Collections => collections;
		Depth? IMemberEqualityDefaultsAttribute.Depth => depth;
	}

	internal interface IMemberEqualityDefaultsAttribute {
		Composition? FieldsAndAutoProperties { get; }
		Comparison? Collections { get; }
		Depth? Depth { get; }
	}

	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	public class MemberEqualityAttribute : Attribute, IMemberEqualityAttribute {
		private Composition? composition;
		private Comparison? collectionComparison;
		private Depth? depth;

		public Composition Composition { get { return composition.GetValueOrDefault(); } set { composition = value; } }
		public Comparison CollectionComparison { get { return collectionComparison.GetValueOrDefault(); } set { collectionComparison = value; } }
		public Depth Depth { get { return depth.GetValueOrDefault(); } set { depth = value; } }

		Composition? IMemberEqualityAttribute.MemberComposition => composition;
		Comparison? IMemberEqualityAttribute.CollectionComparison => collectionComparison;
		Depth? IMemberEqualityAttribute.Depth => depth;
	}

	internal interface IMemberEqualityAttribute {
		Composition? MemberComposition { get; }
		Comparison? CollectionComparison { get; }
		Depth? Depth { get; }
	}
}