using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Equality {
	internal static class Common {
		internal static FieldInfo[] GetFields(Type type) {
			var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			var backingFieldsOfExcludedAutoProperties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
															.Where(x => x.IsDefined(typeof(ExcludeAutoPropertyAttribute), inherit:true))
															.Select(GetBackingField)
															.Where(x => x != null);
			return fields.Except(backingFieldsOfExcludedAutoProperties).ToArray();
		}

		private static FieldInfo GetBackingField(PropertyInfo pi) {
			if (!pi.CanRead || !pi.GetGetMethod(nonPublic:true).IsDefined(typeof(CompilerGeneratedAttribute), inherit:true))
				return null;
			var backingField = pi.DeclaringType?.GetField($"<{pi.Name}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
			if (backingField == null)
				return null;
			if (!backingField.IsDefined(typeof(CompilerGeneratedAttribute), inherit:true))
				return null;
			return backingField;
		}

		internal static PropertyInfo[] GetProperties(Type type) {
			return type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
			           .Where(x => x.IsDefined(typeof(IncludePropertyAttribute), inherit: true))
			           .ToArray();
		}
	}
}
