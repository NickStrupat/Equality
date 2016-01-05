﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace Equality {
	internal static class Common {
		internal static FieldInfo[] GetFields(Type type) {
			var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (type.IsDefined(typeof(ExcludeMembersByDefault)))
				return GetFields<IncludeFieldAttribute>(fields).Concat(GetAutoPropertyBackingFields<IncludeAutoPropertyAttribute>(type)).ToArray();
			return fields.Except(GetFields<ExcludeFieldAttribute>(fields)).Except(GetAutoPropertyBackingFields<ExcludeAutoPropertyAttribute>(type)).ToArray();
		}

		private static IEnumerable<FieldInfo> GetFields<TAttribute>(IEnumerable<FieldInfo> fieldInfos) {
			return fieldInfos.Where(x => x.IsDefined(typeof(TAttribute), inherit: true));
		}

		private static IEnumerable<FieldInfo> GetAutoPropertyBackingFields<TAttribute>(Type type) where TAttribute : Attribute {
			return type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
			           .Where(x => x.IsDefined(typeof(TAttribute), inherit: true))
			           .Select(GetBackingField)
			           .Where(x => x != null);
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

#if DEBUG
		private static readonly AssemblyName assemblyName = new AssemblyName("Debug.EqualityMethods");
		private static readonly AssemblyBuilder assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndSave, Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
		private static readonly ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name, assemblyName.Name + ".dll");
		private static readonly TypeBuilder builder = moduleBuilder.DefineType("EqualityMethods", TypeAttributes.Public);
		private static readonly AssemblySaver assemblySaver = new AssemblySaver();

		class AssemblySaver {
			~AssemblySaver() {
				var type = builder.CreateType();
				assemblyBuilder.Save(assemblyName.Name + ".dll");
			}
		}
#endif

		internal static TDelegate GenerateIL<TDelegate>(Action<Type, ILGenerator> ilGeneration, Type type, [CallerMemberName] String methodName = null) where TDelegate : class {
			if (!typeof(TDelegate).IsSubclassOf(typeof(MulticastDelegate)))
				throw new ArgumentException(nameof(TDelegate));
			var invokeMethodInfo = typeof(TDelegate).GetMethod(nameof(Action.Invoke));
			var returnType = invokeMethodInfo.ReturnType;
			var parameterTypes = invokeMethodInfo.GetParameters().Select(x => x.ParameterType).ToArray();

#if DEBUG
			var methodBuilder = builder.DefineMethod(methodName + "_" + type.Name, MethodAttributes.Public | MethodAttributes.Static, returnType, parameterTypes);

			var ilGenerator = methodBuilder.GetILGenerator();
			ilGeneration(type, ilGenerator);
#endif

			var dynamicMethod = new DynamicMethod(methodName + "_" + type.Name, returnType, parameterTypes, typeof(Common).Module, skipVisibility: true);
			ilGenerator = dynamicMethod.GetILGenerator();
			ilGeneration(type, ilGenerator);
			return dynamicMethod.CreateDelegate<TDelegate>();
		}

		internal static TDelegate CreateDelegate<TDelegate>(this DynamicMethod dm) where TDelegate : class => (TDelegate) (Object) dm.CreateDelegate(typeof (TDelegate));
		internal static Action<T> CombineDelegates<T>(Action<T> a, Action<T> b) => (Action<T>) Delegate.Combine(a, b);
	}
}