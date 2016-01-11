﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Equality {
	internal static class Common {
		internal static FieldInfo[] GetFields(Type type) {
			var includedFields = type.GetFields(MemberInclusion.Include).Concat(type.GetAutoPropertyBackingFields(MemberInclusion.Include)).Distinct();
			var excludedFields = type.GetFields(MemberInclusion.Exclude).Concat(type.GetAutoPropertyBackingFields(MemberInclusion.Exclude)).Distinct();
			return includedFields.Except(excludedFields).ToArray();
		}

		private static IEnumerable<FieldInfo> GetFields(this Type type, MemberInclusion memberInclusion) {
			return type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
			           .Where(x => x.GetMemberEquality().MemberInclusion == memberInclusion);
		}

		private static IEnumerable<FieldInfo> GetAutoPropertyBackingFields(this Type type, MemberInclusion memberInclusion) {
			return type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
			           .Where(x => x.GetMemberEquality().MemberInclusion == memberInclusion)
			           .Select(AutoPropertyExtensions.GetBackingField)
			           .Where(x => x != null);
		}

		internal static PropertyInfo[] GetProperties(Type type) {
			return type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
			           .Where(x => !x.IsAnAutoProperty())
			           .Where(x => x.IsDefined(typeof(IncludePropertyAttribute), inherit: true))
			           .ToArray();
		}

		internal static IIncludePropertyAttribute GetPropertyComparison(this MemberInfo memberInfo) {
			return memberInfo.GetCustomAttribute<IncludePropertyAttribute>(inherit: true);
		}

		internal static Boolean IsEnumerable(this Type memberType, out Type genericEnumerableType)
		{
			if (typeof (IEnumerable).IsAssignableFrom(memberType)) {
				genericEnumerableType = memberType.GetInterfaces().SingleOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEnumerable<>))?.GetGenericArguments().Single();
				if (genericEnumerableType != null)
					return true;
			}
			genericEnumerableType = null;
			return false;
		}

		internal static Boolean ShouldGetStructural(this MemberInfo memberInfo, Type memberType, out Type genericEnumerableType) {
			genericEnumerableType = null;
			return memberInfo.ResolveCollectionComparison() == CollectionComparison.Structure && memberType != typeof(String) && memberType.IsEnumerable(out genericEnumerableType);
		}

		private static CollectionComparison ResolveCollectionComparison(this CollectionComparison? collectionComparison) {
			return collectionComparison.GetValueOrDefault(CollectionComparison.Structure);
		}

		internal static CollectionComparison ResolveCollectionComparison(this MemberInfo memberInfo) {
			return memberInfo.GetMemberEquality().CollectionComparison.ResolveCollectionComparison();
		}

		internal static IMemberEqualityAttribute GetMemberEquality(this MemberInfo memberInfo) {
			ITypeEqualityAttribute typeEqualityAttribute;
			if (memberInfo.DeclaringType.GetCustomAttribute<MemberEqualityAttribute>(inherit: true) != null)
				typeEqualityAttribute = memberInfo.DeclaringType.GetCustomAttribute<MemberEqualityAttribute>(inherit: true);
			else
				typeEqualityAttribute = new MemberEqualityAttribute(MemberInclusion.Include, CollectionComparison.Structure);
			if ((IMemberEqualityAttribute) memberInfo.GetCustomAttribute<FieldEqualityAttribute>(inherit: true) != null)
				return (IMemberEqualityAttribute) memberInfo.GetCustomAttribute<FieldEqualityAttribute>(inherit: true);
			if ((IMemberEqualityAttribute) memberInfo.GetCustomAttribute<AutoPropertyEqualityAttribute>(inherit: true) != null)
				return (IMemberEqualityAttribute) memberInfo.GetCustomAttribute<AutoPropertyEqualityAttribute>(inherit: true);
			return new InternalMemberEqualityAttribute(typeEqualityAttribute.FieldInclusion, typeEqualityAttribute.CollectionComparison);
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
			var invokeMethodInfo = typeof(TDelegate).GetMethod(nameof(Action.Invoke));
			var returnType = invokeMethodInfo.ReturnType;
			var parameterTypes = invokeMethodInfo.GetParameters().Select(x => x.ParameterType).ToArray();

			ILGenerator ilGenerator;
#if DEBUG
			var methodBuilder = builder.DefineMethod(methodName + "_" + type.Name, MethodAttributes.Public | MethodAttributes.Static, returnType, parameterTypes);

			ilGenerator = methodBuilder.GetILGenerator();
			ilGeneration(type, ilGenerator);
#endif

			var dynamicMethod = new DynamicMethod(methodName + "_" + type.Name, returnType, parameterTypes, restrictedSkipVisibility: true);
			ilGenerator = dynamicMethod.GetILGenerator();
			ilGeneration(type, ilGenerator);
			return dynamicMethod.CreateDelegate<TDelegate>();
		}

		internal static TDelegate CreateDelegate<TDelegate>(this DynamicMethod dm) where TDelegate : class {
			if (!typeof(TDelegate).IsSubclassOf(typeof(MulticastDelegate)))
				throw new ArgumentException("Argument must be a delegate type", nameof(TDelegate));
			return (TDelegate) (Object) dm.CreateDelegate(typeof (TDelegate));
		}

		internal static Action<T> CombineDelegates<T>(this Action<T> a, Action<T> b) => (Action<T>) Delegate.Combine(a, b);
	}
}