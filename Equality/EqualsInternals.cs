using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Reflection.Emit;

namespace Equality {
	internal static class EqualsInternals {
		internal delegate Boolean Equals<T>(ref T x, ref T y);

		internal static class StaticCache<T> {
			public static readonly Equals<T> Func = GetEqualsFunc<T>(typeof (T));
		}

		internal static readonly ConcurrentDictionary<Type, Equals<Object>> DynamicCache = new ConcurrentDictionary<Type, Equals<Object>>();

		internal static Equals<T> GetEqualsFunc<T>(Type type) {

			////////////////////
			//var assemblyName = new AssemblyName("SomeName");
			//var assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndSave, @"c:");
			//var moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name, assemblyName.Name + ".dll");

			//TypeBuilder builder = moduleBuilder.DefineType("Test", TypeAttributes.Public);
			//var methodBuilder = builder.DefineMethod("DynamicCreate", MethodAttributes.Public, typeof(Boolean), new[] { typeof(T).MakeByRefType(), typeof(T).MakeByRefType() });
			///* this line is a replacement for your  new DynamicMethod(....)  line of code

			///* GENERATE YOUR IL CODE HERE */
			//var ilGenerator = methodBuilder.GetILGenerator();
			///////////////////

			var refOfTType = typeof (T).MakeByRefType();
			var dynamicMethod = new DynamicMethod(String.Empty, typeof (Boolean), new[] {refOfTType, refOfTType}, typeof (T).Module, skipVisibility: true);
			var ilGenerator = dynamicMethod.GetILGenerator();

			var fields = Common.GetFields(type);
			var properties = Common.GetProperties(type);
			GenerateIL<T>(type, ilGenerator, fields, properties);

			/////////////////////
			//var t = builder.CreateType();
			//assemblyBuilder.Save(assemblyName.Name + ".dll");
			//return null;
			///////////////////

			return (Equals<T>) dynamicMethod.CreateDelegate(typeof (Equals<T>));
		}

		private static void GenerateIL<T>(Type type, ILGenerator ilGenerator, FieldInfo[] fields, PropertyInfo[] properties) {
			Action<ILGenerator> loadFirstInstance = i => i.Emit(OpCodes.Ldarg_0);
			if (typeof (T) != type) {
				var instanceLocal = ilGenerator.DeclareLocal(type);
				loadFirstInstance = i => i.Emit(OpCodes.Ldloc, instanceLocal);
				ilGenerator.Emit(OpCodes.Ldarg_0);
				ilGenerator.Emit(OpCodes.Castclass, type);
				ilGenerator.Emit(OpCodes.Stloc, instanceLocal);
			}
			Action<ILGenerator> loadSecondInstance = i => i.Emit(OpCodes.Ldarg_1);
			if (typeof (T) != type) {
				var instanceLocal = ilGenerator.DeclareLocal(type);
				loadSecondInstance = i => i.Emit(OpCodes.Ldloc, instanceLocal);
				ilGenerator.Emit(OpCodes.Ldarg_1);
				ilGenerator.Emit(OpCodes.Castclass, type);
				ilGenerator.Emit(OpCodes.Stloc, instanceLocal);
			}

			var retFalse = ilGenerator.DefineLabel();
			var localMap = new ConcurrentDictionary<Type, LocalBuilder>();
			foreach (var field in fields)
				EmitMemberEqualityComparison(ilGenerator, localMap, loadFirstInstance, loadSecondInstance, field, field.FieldType, retFalse);
			foreach (var property in properties)
				EmitMemberEqualityComparison(ilGenerator, localMap, loadFirstInstance, loadSecondInstance, property, property.PropertyType, retFalse);

			ilGenerator.Emit(OpCodes.Ldc_I4_1);
			ilGenerator.Emit(OpCodes.Ret);

			ilGenerator.MarkLabel(retFalse);
			ilGenerator.Emit(OpCodes.Ldc_I4_0);
			ilGenerator.Emit(OpCodes.Ret);
		}

		private static void EmitMemberEqualityComparison(ILGenerator ilGenerator,
			ConcurrentDictionary<Type, LocalBuilder> localMap,
			Action<ILGenerator> loadFirstInstance,
			Action<ILGenerator> loadSecondInstance,
			MemberInfo memberInfo,
			Type memberType,
			Label retFalse) {
			Action<ILGenerator> emitLoadFirstMember;
			Action<ILGenerator> emitLoadSecondMember;
			if (memberInfo is FieldInfo)
				emitLoadFirstMember = emitLoadSecondMember = ilg => ilg.Emit(OpCodes.Ldfld, (FieldInfo) memberInfo);
			else
				emitLoadFirstMember = emitLoadSecondMember = ilg => ilg.Emit(OpCodes.Call, ((PropertyInfo) memberInfo).GetGetMethod(nonPublic: true));

			Action<ILGenerator> emitComparison = delegate { };
			if (memberType.IsPrimitive)
				emitComparison = ilg => ilg.Emit(OpCodes.Bne_Un, retFalse);
			else {
				MethodInfo opEquality;
				if (typeof (IEquatable<>).MakeGenericType(memberType).IsAssignableFrom(memberType)) {
					if (memberType.IsValueType) {
						emitLoadFirstMember = SetEmitLoadFirstMemberForValueType(localMap, memberInfo, memberType, emitLoadFirstMember);
					}
					else {
						//emitComparison = CombineDelegates(emitComparison, ilg => );
					}
					emitComparison = ilg => ilg.Emit(OpCodes.Call, memberType.GetMethod(nameof(Equals), new[] {memberType}));
				}
				else if ((opEquality = memberType.GetMethod("op_Equality", new[] {memberType, memberType})) != null) {
					emitComparison = ilg => ilg.Emit(OpCodes.Call, opEquality);
				}
				else {
					if (memberType.IsValueType) {
						emitLoadFirstMember = SetEmitLoadFirstMemberForValueType(localMap, memberInfo, memberType, emitLoadFirstMember);
						loadSecondInstance = CombineDelegates(loadSecondInstance, ilg => ilg.Emit(OpCodes.Box, memberType));
					}
					emitComparison = ilg => ilg.Emit(OpCodes.Callvirt, memberType.GetMethod(nameof(Equals), new[] {typeof (Object)}));
				}
				emitComparison = CombineDelegates(emitComparison, ilg => ilg.Emit(OpCodes.Brfalse, retFalse));
			}

			loadFirstInstance(ilGenerator);
			emitLoadFirstMember(ilGenerator);
			loadSecondInstance(ilGenerator);
			emitLoadSecondMember(ilGenerator);
			emitComparison(ilGenerator);
		}

		private static Action<T> CombineDelegates<T>(Action<T> a, Action<T> b) => (Action<T>) Delegate.Combine(a, b);

		private static Action<ILGenerator> SetEmitLoadFirstMemberForValueType(ConcurrentDictionary<Type, LocalBuilder> localMap, MemberInfo memberInfo, Type memberType, Action<ILGenerator> emitLoadFirstMember) {
			if (memberInfo is FieldInfo)
				emitLoadFirstMember = ilg => ilg.Emit(OpCodes.Ldflda, (FieldInfo) memberInfo);
			else {
				emitLoadFirstMember = ilg => {
					                      var local = localMap.GetOrAdd(memberType, ilg.DeclareLocal);
					                      ilg.Emit(OpCodes.Stloc, local);
					                      ilg.Emit(OpCodes.Ldloca, local);
				                      };
			}
			return emitLoadFirstMember;
		}
	}
}