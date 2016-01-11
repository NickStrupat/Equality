using System;

namespace Equality {
	public enum MemberInclusion { Include, Exclude }
	public enum CollectionComparison { Structure, Instance }
	//public enum Depth { Memberwise, Recursive }

	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
	public class MemberEqualityAttribute : Attribute, ITypeEqualityAttribute {
		private readonly MemberInclusion fieldInclusion;
		private readonly MemberInclusion autoPropertyInclusion;
		private readonly CollectionComparison? collectionComparison;

		public MemberEqualityAttribute(MemberInclusion fieldAndAutoPropertyInclusion) : this(fieldAndAutoPropertyInclusion, fieldAndAutoPropertyInclusion, null) { }
		public MemberEqualityAttribute(MemberInclusion fieldAndAutoPropertyInclusion, CollectionComparison collectionComparison) : this(fieldAndAutoPropertyInclusion, fieldAndAutoPropertyInclusion, collectionComparison) { }
		public MemberEqualityAttribute(MemberInclusion fieldInclusion, MemberInclusion autoPropertyInclusion, CollectionComparison collectionComparison) : this(fieldInclusion, autoPropertyInclusion, (CollectionComparison?)collectionComparison) { }

		private MemberEqualityAttribute(MemberInclusion fieldInclusion, MemberInclusion autoPropertyInclusion, CollectionComparison? collectionComparison) {
			this.fieldInclusion = fieldInclusion;
			this.autoPropertyInclusion = autoPropertyInclusion;
			this.collectionComparison = collectionComparison;
		}

		MemberInclusion ITypeEqualityAttribute.FieldInclusion => fieldInclusion;
		MemberInclusion ITypeEqualityAttribute.AutoPropertyInclusion => autoPropertyInclusion;
		CollectionComparison? ITypeEqualityAttribute.CollectionComparison => collectionComparison;
	}

	internal class InternalMemberEqualityAttribute : Attribute, IMemberEqualityAttribute {
		private readonly MemberInclusion memberInclusion;
		private readonly CollectionComparison? collectionComparison;

		internal InternalMemberEqualityAttribute(MemberInclusion memberInclusion, CollectionComparison? collectionComparison) {
			this.memberInclusion = memberInclusion;
			this.collectionComparison = collectionComparison;
		}

		MemberInclusion IMemberEqualityAttribute.MemberInclusion => memberInclusion;
		CollectionComparison? IMemberEqualityAttribute.CollectionComparison => collectionComparison;
	}

	[AttributeUsage(AttributeTargets.Field)]
	public class FieldEqualityAttribute : Attribute, IMemberEqualityAttribute, IFieldEqualityAttribute {
		private readonly MemberInclusion fieldInclusion;
		private readonly CollectionComparison? fieldComparison;

		public FieldEqualityAttribute(MemberInclusion fieldInclusion) : this(fieldInclusion, null) { }
		public FieldEqualityAttribute(MemberInclusion fieldInclusion, CollectionComparison fieldComparison) : this(fieldInclusion, (CollectionComparison?) fieldComparison) { }

		internal FieldEqualityAttribute(MemberInclusion fieldInclusion, CollectionComparison? fieldComparison) {
			this.fieldInclusion = fieldInclusion;
			this.fieldComparison = fieldComparison;
		}

		MemberInclusion IFieldEqualityAttribute.FieldInclusion => ((IMemberEqualityAttribute) this).MemberInclusion;
		CollectionComparison? IFieldEqualityAttribute.FieldComparison => ((IMemberEqualityAttribute) this).CollectionComparison;

		MemberInclusion IMemberEqualityAttribute.MemberInclusion => fieldInclusion;
		CollectionComparison? IMemberEqualityAttribute.CollectionComparison => fieldComparison;
	}

	[AttributeUsage(AttributeTargets.Property)]
	public class AutoPropertyEqualityAttribute : Attribute, IMemberEqualityAttribute, IAutoPropertyEqualityAttribute {
		private readonly MemberInclusion autoPropertyInclusion;
		private readonly CollectionComparison? autoPropertyComparison;

		public AutoPropertyEqualityAttribute(MemberInclusion autoPropertyInclusion) : this(autoPropertyInclusion, null) { }
		public AutoPropertyEqualityAttribute(MemberInclusion autoPropertyInclusion, CollectionComparison collectionComparison) : this(autoPropertyInclusion, (CollectionComparison?) collectionComparison) { }

		private AutoPropertyEqualityAttribute(MemberInclusion autoPropertyInclusion, CollectionComparison? autoPropertyComparison) {
			this.autoPropertyInclusion = autoPropertyInclusion;
			this.autoPropertyComparison = autoPropertyComparison;
		}

		MemberInclusion IAutoPropertyEqualityAttribute.AutoPropertyInclusion => ((IMemberEqualityAttribute) this).MemberInclusion;
		CollectionComparison? IAutoPropertyEqualityAttribute.AutoPropertyComparison => ((IMemberEqualityAttribute) this).CollectionComparison;

		MemberInclusion IMemberEqualityAttribute.MemberInclusion => autoPropertyInclusion;
		CollectionComparison? IMemberEqualityAttribute.CollectionComparison => autoPropertyComparison;
	}

	[AttributeUsage(AttributeTargets.Property)]
	public class IncludePropertyAttribute : Attribute, IIncludePropertyAttribute {
		private readonly CollectionComparison? propertyComparison;

		public IncludePropertyAttribute() { }
		public IncludePropertyAttribute(CollectionComparison propertyComparison) { this.propertyComparison = propertyComparison; }

		CollectionComparison? IIncludePropertyAttribute.PropertyComparison => propertyComparison;
	}

	internal interface IMemberInclusionAttribute {
		MemberInclusion MemberInclusion { get; }
	}

	internal interface IMemberComparisonAttribute {
		CollectionComparison? CollectionComparison { get; }
	}

	internal interface IMemberEqualityAttribute {
		MemberInclusion MemberInclusion { get; }
		CollectionComparison? CollectionComparison { get; }
	}

	internal interface IFieldEqualityAttribute {
		MemberInclusion FieldInclusion { get; }
		CollectionComparison? FieldComparison { get; }
	}

	internal interface IAutoPropertyEqualityAttribute {
		MemberInclusion AutoPropertyInclusion { get; }
		CollectionComparison? AutoPropertyComparison { get; }
	}

	internal interface IIncludePropertyAttribute {
		CollectionComparison? PropertyComparison { get; }
	}

	internal interface ITypeEqualityAttribute {
		MemberInclusion FieldInclusion { get; }
		MemberInclusion AutoPropertyInclusion { get; }
		CollectionComparison? CollectionComparison { get; }
	}
}