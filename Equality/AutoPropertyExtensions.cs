using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Equality {
	public static class AutoPropertyExtensions {
		const String Prefix = "<";
		const String Suffix = ">k__BackingField";

		private static String GetBackingFieldName(String propertyName) => $"{Prefix}{propertyName}{Suffix}";

		private static String GetAutoPropertyName(String fieldName) {
			var match = Regex.Match(fieldName, $"{Prefix}(.+?){Suffix}");
			return match.Success ? match.Groups[1].Value : null;
		}

		public static Boolean IsAnAutoProperty(this PropertyInfo propertyInfo) => propertyInfo.GetBackingField() != null;

		public static Boolean IsBackingFieldOfAnAutoProperty(this FieldInfo propertyInfo) => propertyInfo.GetAutoProperty() != null;

		public static FieldInfo GetBackingField(this PropertyInfo propertyInfo) {
			if (propertyInfo == null)
				throw new ArgumentNullException(nameof(propertyInfo));
			if (!propertyInfo.CanRead || !propertyInfo.GetGetMethod(nonPublic: true).IsDefined(typeof(CompilerGeneratedAttribute), inherit: true))
				return null;
			var backingFieldName = GetBackingFieldName(propertyInfo.Name);
			var backingField = propertyInfo.DeclaringType?.GetField(backingFieldName, BindingFlags.Instance | BindingFlags.NonPublic);
			if (backingField == null)
				return null;
			if (!backingField.IsDefined(typeof(CompilerGeneratedAttribute), inherit: true))
				return null;
			return backingField;
		}

		public static PropertyInfo GetAutoProperty(this FieldInfo fieldInfo) {
			if (fieldInfo == null)
				throw new ArgumentNullException(nameof(fieldInfo));
			if (!fieldInfo.IsDefined(typeof(CompilerGeneratedAttribute), inherit: true))
				return null;
			var autoPropertyName = GetAutoPropertyName(fieldInfo.Name);
			if (autoPropertyName == null)
				return null;
			var autoProperty = fieldInfo.DeclaringType?.GetProperty(autoPropertyName);
			if (autoProperty == null)
				return null;
			if (!autoProperty.CanRead || !autoProperty.GetGetMethod(nonPublic: true).IsDefined(typeof(CompilerGeneratedAttribute), inherit: true))
				return null;
			return autoProperty;
		}
	}
}
