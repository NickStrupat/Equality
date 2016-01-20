using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace Equality {
	internal static class Common {
		internal static FieldInfo[] GetFields(Type type) {
			var includedFields = type.GetFields(Composition.Include);
			var excludedFields = type.GetFields(Composition.Exclude);
			return includedFields.Except(excludedFields).ToArray();
		}

		private static IEnumerable<FieldInfo> GetFields(this Type type, Composition memberComposition) {
			var backingFields = type.GetAutoPropertyBackingFields();
			return type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
			           .Except(backingFields)
			           .Where(x => x.GetMemberEquality().Composition == memberComposition)
			           .Concat(type.GetAutoPropertyBackingFields(memberComposition));
		}

		private static IEnumerable<FieldInfo> GetAutoPropertyBackingFields(this Type type, Composition? memberComposition = null) {
			return type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
			           .Where(x => memberComposition == null || x.GetMemberEquality().Composition == memberComposition)
			           .Select(x => x.GetBackingField())
			           .Where(x => x != null);
		}

		internal static PropertyInfo[] GetProperties(Type type) {
			return type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
			           .Where(x => x.GetBackingField() == null)
			           .Where(x => x.GetMemberEquality().Composition == Composition.Include)
			           .ToArray();
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
			return memberType != typeof(String) && memberType.IsEnumerable(out genericEnumerableType) && memberInfo.ResolveCollectionComparison() == Comparison.Structure;
		}

		internal static Boolean ShouldRecurse(this MemberInfo memberInfo, Type memberType) {
			return !memberType.IsPrimitive && !memberType.IsEnum && memberType != typeof(String) && memberInfo.GetMemberEquality().Depth == Depth.Recursive;
		}

		internal static Comparison ResolveCollectionComparison(this MemberInfo memberInfo) {
			return memberInfo.GetMemberEquality().CollectionComparison;
		}

		internal struct MemberEquality {
			public Composition Composition { get; set; }
			public Comparison CollectionComparison { get; set; }
			public Depth Depth { get; set; }
		}

		internal static MemberEquality GetMemberEquality(this MemberInfo memberInfo) {
			IMemberEqualityAttribute memberEqualityAttribute = memberInfo.GetCustomAttribute<MemberEqualityAttribute>(inherit: true);
			IMemberEqualityDefaultsAttribute typeDefaults = memberInfo.DeclaringType.GetCustomAttribute<MemberEqualityDefaultsAttribute>(inherit: true);
			return new MemberEquality {
				Composition = memberEqualityAttribute?.MemberComposition ?? typeDefaults?.FieldsAndAutoProperties ?? Composition.Include,
				CollectionComparison = memberEqualityAttribute?.CollectionComparison ?? typeDefaults?.Collections ?? Comparison.Structure,
				Depth = memberEqualityAttribute?.Depth ?? typeDefaults?.Depth ?? Depth.Memberwise
			};
		}

#if DEBUG
		private static readonly DynamicAssembly dynamicAssembly = new DynamicAssembly();

		private class DynamicAssembly : IDisposable {
			private readonly AssemblyName assemblyName;
			private readonly AssemblyBuilder assemblyBuilder;

			public TypeBuilder TypeBuilder { get; }

			public DynamicAssembly()
			{
				assemblyName = new AssemblyName("Debug.EqualityMethods");
				assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndSave, Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase).Replace(@"file:\", String.Empty));
				var moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name, assemblyName.Name + ".dll");
				TypeBuilder = moduleBuilder.DefineType("EqualityMethods", TypeAttributes.Public);
			}

			public void Dispose() {
				var type = TypeBuilder.CreateType();
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
			var methodBuilder = dynamicAssembly.TypeBuilder.DefineMethod($"{methodName}_{type.Name}", MethodAttributes.Public | MethodAttributes.Static, returnType, parameterTypes);

			ilGenerator = methodBuilder.GetILGenerator();
			ilGeneration(type, ilGenerator);
#endif

			var dynamicMethod = new DynamicMethod($"{methodName}_{type.Name}", returnType, parameterTypes, restrictedSkipVisibility: true);
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