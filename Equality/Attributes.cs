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

		public MemberEqualityDefaultsAttribute() { }
		public MemberEqualityDefaultsAttribute(Composition fieldsAndAutoProperties) { this.fieldsAndAutoProperties = fieldsAndAutoProperties; }
		public MemberEqualityDefaultsAttribute(Composition fieldsAndAutoProperties, Comparison collections) { this.fieldsAndAutoProperties = fieldsAndAutoProperties; this.collections = collections; }
		public MemberEqualityDefaultsAttribute(Composition fieldsAndAutoProperties, Depth depth) { this.fieldsAndAutoProperties = fieldsAndAutoProperties; this.depth = depth; }
		public MemberEqualityDefaultsAttribute(Composition fieldsAndAutoProperties, Comparison collections, Depth depth) { this.fieldsAndAutoProperties = fieldsAndAutoProperties; this.collections = collections; this.depth = depth; }
		public MemberEqualityDefaultsAttribute(Comparison collections) { this.collections = collections; }
		public MemberEqualityDefaultsAttribute(Comparison collections, Depth depth) { this.collections = collections; this.depth = depth; }
		public MemberEqualityDefaultsAttribute(Depth depth) { this.depth = depth; }
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

		public MemberEqualityAttribute() { }
		public MemberEqualityAttribute(Composition composition) { this.composition = composition; }
		public MemberEqualityAttribute(Composition composition, Comparison collectionComparison) { this.composition = composition; this.collectionComparison = collectionComparison; }
		public MemberEqualityAttribute(Composition composition, Depth depth) { this.composition = composition; this.depth = depth; }
		public MemberEqualityAttribute(Composition composition, Comparison collectionComparison, Depth depth) { this.composition = composition; this.collectionComparison = collectionComparison; this.depth = depth; }
		public MemberEqualityAttribute(Comparison collectionComparison) { this.collectionComparison = collectionComparison; }
		public MemberEqualityAttribute(Comparison collectionComparison, Depth depth) { this.collectionComparison = collectionComparison; this.depth = depth; }
		public MemberEqualityAttribute(Depth depth) { this.depth = depth; }
	}

	internal interface IMemberEqualityAttribute {
		Composition? MemberComposition { get; }
		Comparison? CollectionComparison { get; }
		Depth? Depth { get; }
	}
}