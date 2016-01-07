using System;

namespace Equality {
	public enum MemberInclusion { Include, Exclude }
	public enum MemberComparison { Structural, Referential }

	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
	public class TypeEqualityAttribute : Attribute, ITypeEqualityAttribute {
		private readonly MemberInclusion fieldInclusion;
		private readonly MemberInclusion autoPropertyInclusion;
		private readonly MemberComparison? memberComparison;

		public TypeEqualityAttribute(MemberInclusion fieldAndAutoPropertyInclusion) : this(fieldAndAutoPropertyInclusion, fieldAndAutoPropertyInclusion, null) { }
		public TypeEqualityAttribute(MemberInclusion fieldAndAutoPropertyInclusion, MemberComparison memberComparison) : this(fieldAndAutoPropertyInclusion, fieldAndAutoPropertyInclusion, memberComparison) { }
		public TypeEqualityAttribute(MemberInclusion fieldInclusion, MemberInclusion autoPropertyInclusion, MemberComparison memberComparison) : this(fieldInclusion, autoPropertyInclusion, (MemberComparison?)memberComparison) { }

		private TypeEqualityAttribute(MemberInclusion fieldInclusion, MemberInclusion autoPropertyInclusion, MemberComparison? memberComparison) {
			this.fieldInclusion = fieldInclusion;
			this.autoPropertyInclusion = autoPropertyInclusion;
			this.memberComparison = memberComparison;
		}

		MemberInclusion ITypeEqualityAttribute.FieldInclusion => fieldInclusion;
		MemberInclusion ITypeEqualityAttribute.AutoPropertyInclusion => autoPropertyInclusion;
		MemberComparison? ITypeEqualityAttribute.MemberComparison => memberComparison;
	}

	internal class MemberEqualityAttribute : Attribute, IMemberEqualityAttribute {
		private readonly MemberInclusion memberInclusion;
		private readonly MemberComparison? memberComparison;

		internal MemberEqualityAttribute(MemberInclusion memberInclusion, MemberComparison? memberComparison) {
			this.memberInclusion = memberInclusion;
			this.memberComparison = memberComparison;
		}

		MemberInclusion IMemberEqualityAttribute.MemberInclusion => memberInclusion;
		MemberComparison? IMemberEqualityAttribute.MemberComparison => memberComparison;
	}

	[AttributeUsage(AttributeTargets.Field)]
	public class FieldEqualityAttribute : Attribute, IFieldEqualityAttribute, IMemberEqualityAttribute {
		private readonly MemberInclusion fieldInclusion;
		private readonly MemberComparison? fieldComparison;

		public FieldEqualityAttribute(MemberInclusion fieldInclusion) : this(fieldInclusion, null) { }
		public FieldEqualityAttribute(MemberInclusion fieldInclusion, MemberComparison fieldComparison) : this(fieldInclusion, (MemberComparison?) fieldComparison) { }

		internal FieldEqualityAttribute(MemberInclusion fieldInclusion, MemberComparison? fieldComparison) {
			this.fieldInclusion = fieldInclusion;
			this.fieldComparison = fieldComparison;
		}

		MemberInclusion IFieldEqualityAttribute.FieldInclusion => fieldInclusion;
		MemberComparison? IFieldEqualityAttribute.FieldComparison => fieldComparison;

		MemberInclusion IMemberEqualityAttribute.MemberInclusion => fieldInclusion;
		MemberComparison? IMemberEqualityAttribute.MemberComparison => fieldComparison;
	}

	[AttributeUsage(AttributeTargets.Property)]
	public class AutoPropertyEqualityAttribute : Attribute, IAutoPropertyEqualityAttribute, IMemberEqualityAttribute {
		private readonly MemberInclusion autoPropertyInclusion;
		private readonly MemberComparison? autoPropertyComparison;

		public AutoPropertyEqualityAttribute(MemberInclusion autoPropertyInclusion) : this(autoPropertyInclusion, null) { }
		public AutoPropertyEqualityAttribute(MemberInclusion autoPropertyInclusion, MemberComparison memberComparison) : this(autoPropertyInclusion, (MemberComparison?) memberComparison) { }

		private AutoPropertyEqualityAttribute(MemberInclusion autoPropertyInclusion, MemberComparison? autoPropertyComparison) {
			this.autoPropertyInclusion = autoPropertyInclusion;
			this.autoPropertyComparison = autoPropertyComparison;
		}

		MemberInclusion IAutoPropertyEqualityAttribute.AutoPropertyInclusion => autoPropertyInclusion;
		MemberComparison? IAutoPropertyEqualityAttribute.AutoPropertyComparison => autoPropertyComparison;

		MemberInclusion IMemberEqualityAttribute.MemberInclusion => autoPropertyInclusion;
		MemberComparison? IMemberEqualityAttribute.MemberComparison => autoPropertyComparison;
	}

	[AttributeUsage(AttributeTargets.Property)]
	public class IncludePropertyAttribute : Attribute, IIncludePropertyAttribute {
		private readonly MemberComparison? propertyComparison;

		public IncludePropertyAttribute() { }
		public IncludePropertyAttribute(MemberComparison propertyComparison) { this.propertyComparison = propertyComparison; }

		MemberComparison? IIncludePropertyAttribute.PropertyComparison => propertyComparison;
	}

	internal interface IMemberInclusionAttribute {
		MemberInclusion MemberInclusion { get; }
	}

	internal interface IMemberComparisonAttribute {
		MemberComparison? MemberComparison { get; }
	}

	internal interface IMemberEqualityAttribute {
		MemberInclusion MemberInclusion { get; }
		MemberComparison? MemberComparison { get; }
	}

	internal interface IFieldEqualityAttribute {
		MemberInclusion FieldInclusion { get; }
		MemberComparison? FieldComparison { get; }
	}

	internal interface IAutoPropertyEqualityAttribute {
		MemberInclusion AutoPropertyInclusion { get; }
		MemberComparison? AutoPropertyComparison { get; }
	}

	internal interface IIncludePropertyAttribute {
		MemberComparison? PropertyComparison { get; }
	}

	internal interface ITypeEqualityAttribute {
		MemberInclusion FieldInclusion { get; }
		MemberInclusion AutoPropertyInclusion { get; }
		MemberComparison? MemberComparison { get; }
	}
}