using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace Equality {
	public static class Extensions {
		public static Int32   GetStructHashCode<T>(this T @object)     where T : struct => GetHashCodeFuncCache<T>.Func(@object);
		public static Int32   GetClassHashCode <T>(this T @object)     where T : class  => @object.GetClassHashCodeInternal(@object.GetType());
		public static Boolean StructEquals     <T>(this T x, T y)      where T : struct => EqualsFuncCache<T>.Func(x, y);
		public static Boolean ClassEquals      <T>(this T x, T y)      where T : class  => ClassEqualsInternal(x, y);
		public static Boolean StructEquals     <T>(this T x, Object y) where T : struct => y != null && y.GetType() == typeof (T) && EqualsFuncCache<T>.Func(x, (T) y);
		public static Boolean ClassEquals      <T>(this T x, Object y) where T : class  => ClassEqualsInternal(x, y as T);

		private static Int32 GetClassHashCodeInternal<T>(this T @object, Type type) where T : class {
			var func = type == typeof(T) ? GetHashCodeFuncCache<T>.Func : GetHashCodeClassTypeFuncCache.GetOrAdd(type, GetHashCodeFunc<Object>);
			return func(@object);
		}

		private static Boolean ClassEqualsInternal<T>(this T x, T y) where T : class {
			if (x == null || y == null)
				return false;
			if (ReferenceEquals(x, y))
				return true;
			var xType = x.GetType();
			var yType = y.GetType();
			if (xType != yType)
				return false;
			var func = xType == typeof(T) ? EqualsFuncCache<T>.Func : EqualsClassTypeFuncCache.GetOrAdd(xType, GetEqualsFunc<Object>);
			return func(x, y);
		}

		private static class GetHashCodeFuncCache<T> { public static readonly Func<T, Int32> Func = GetHashCodeFunc<T>(typeof(T)); }
		private static readonly ConcurrentDictionary<Type, Func<Object, Int32>> GetHashCodeClassTypeFuncCache = new ConcurrentDictionary<Type, Func<Object, Int32>>();

		private static class EqualsFuncCache<T> { public static readonly Func<T, T, Boolean> Func = GetEqualsFunc<T>(typeof(T)); }
		private static readonly ConcurrentDictionary<Type, Func<Object, Object, Boolean>> EqualsClassTypeFuncCache = new ConcurrentDictionary<Type, Func<Object, Object, Boolean>>();

		private static FieldInfo[] GetFields(Type type) {
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

		private static PropertyInfo[] GetProperties(Type type) {
			return type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
			           .Where(x => x.IsDefined(typeof(IncludePropertyAttribute), inherit: true))
			           .ToArray();
		}

		private static Func<T, T, Boolean> GetEqualsFunc<T>(Type type) {

			////////////////////
			//var assemblyName = new AssemblyName("SomeName");
			//var assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndSave, @"c:");
			//var moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name, assemblyName.Name + ".dll");

			//TypeBuilder builder = moduleBuilder.DefineType("Test", TypeAttributes.Public);
			//var methodBuilder = builder.DefineMethod("DynamicCreate", MethodAttributes.Public, typeof(Boolean), new[] { typeof(T), typeof(T) });
			///* this line is a replacement for your  new DynamicMethod(....)  line of code

			///* GENERATE YOUR IL CODE HERE */
			//var ilGenerator = methodBuilder.GetILGenerator();
			///////////////////

			var dynamicMethod = new DynamicMethod(String.Empty, typeof(Boolean), new[] { typeof(T), typeof(T) }, typeof(T).Module, skipVisibility: true);
			var ilGenerator = dynamicMethod.GetILGenerator();

			var fields = GetFields(type);
			var properties = GetProperties(type);
			GenerateEqualsIL<T>(type, ilGenerator, fields, properties);

			/////////////////////
			//var t = builder.CreateType();
			//assemblyBuilder.Save(assemblyName.Name + ".dll");
			//return null;
			///////////////////

			return (Func<T, T, Boolean>) dynamicMethod.CreateDelegate(typeof(Func<T, T, Boolean>));
		}

		private static void GenerateEqualsIL<T>(Type type, ILGenerator ilGenerator, FieldInfo[] fields, PropertyInfo[] properties) {
			Action<ILGenerator> loadFirstInstance = i => i.Emit(OpCodes.Ldarg_0);
			if (typeof(T) != type) {
				var instanceLocal = ilGenerator.DeclareLocal(type);
				loadFirstInstance = i => i.Emit(OpCodes.Ldloc, instanceLocal);
				ilGenerator.Emit(OpCodes.Ldarg_0);
				ilGenerator.Emit(OpCodes.Castclass, type);
				ilGenerator.Emit(OpCodes.Stloc, instanceLocal);
			}
			Action<ILGenerator> loadSecondInstance = i => i.Emit(OpCodes.Ldarg_1);
			if (typeof(T) != type) {
				var instanceLocal = ilGenerator.DeclareLocal(type);
				loadSecondInstance = i => i.Emit(OpCodes.Ldloc, instanceLocal);
				ilGenerator.Emit(OpCodes.Ldarg_1);
				ilGenerator.Emit(OpCodes.Castclass, type);
				ilGenerator.Emit(OpCodes.Stloc, instanceLocal);
			}

			var retFalse = ilGenerator.DefineLabel();

			foreach (var field in fields)
				EmitMemberEqualityComparison(ilGenerator, loadFirstInstance, loadSecondInstance, ilg => ilg.Emit(field.FieldType.IsValueType && !field.FieldType.IsPrimitive ? OpCodes.Ldflda : OpCodes.Ldfld, field), retFalse, field.FieldType);
			foreach (var property in properties)
				EmitMemberEqualityComparison(ilGenerator, loadFirstInstance, loadSecondInstance, ilg => ilg.Emit(OpCodes.Call, property.GetGetMethod(nonPublic:true)), retFalse, property.PropertyType);

			ilGenerator.Emit(OpCodes.Ldc_I4_1);
			ilGenerator.Emit(OpCodes.Ret);
			
			ilGenerator.MarkLabel(retFalse);
			ilGenerator.Emit(OpCodes.Ldc_I4_0);
			ilGenerator.Emit(OpCodes.Ret);
		}

		private static void EmitMemberEqualityComparison(ILGenerator ilGenerator,
		                                                 Action<ILGenerator> loadFirstInstance,
		                                                 Action<ILGenerator> loadSecondInstance,
		                                                 Action<ILGenerator> emitLoadMember,
		                                                 Label retFalse,
		                                                 Type memberType) {
			loadFirstInstance(ilGenerator);
			emitLoadMember(ilGenerator);
			loadSecondInstance(ilGenerator);
			emitLoadMember(ilGenerator);

			if (memberType.IsPrimitive)
				ilGenerator.Emit(OpCodes.Bne_Un, retFalse);
			else {
				MethodInfo opEquality;
				if (typeof (IEquatable<>).MakeGenericType(memberType).IsAssignableFrom(memberType))
					ilGenerator.Emit(OpCodes.Call, memberType.GetMethod(nameof(Equals), new[] { memberType }));
				else if ((opEquality = memberType.GetMethod("op_Equality", new[] { memberType, memberType })) != null)
					ilGenerator.Emit(OpCodes.Call, opEquality);
				else
					ilGenerator.Emit(OpCodes.Callvirt, memberType.GetMethod(nameof(Equals), new[] { typeof(Object) }));
				ilGenerator.Emit(OpCodes.Brfalse, retFalse);
			}
		}

		private static Func<T, Int32> GetHashCodeFunc<T>(Type type) {
			var dynamicMethod = new DynamicMethod(String.Empty, typeof(Int32), new[] { typeof(T) }, typeof(T).Module, skipVisibility: true);
			var ilGenerator = dynamicMethod.GetILGenerator();

			var fields = GetFields(type);
			var properties = GetProperties(type);
			GenerateHashCodeIL<T>(type, ilGenerator, fields, properties);

			return (Func<T, Int32>) dynamicMethod.CreateDelegate(typeof(Func<T, Int32>));
		}

		private static void GenerateHashCodeIL<T>(Type type, ILGenerator ilGenerator, FieldInfo[] fields, PropertyInfo[] properties) {
			const Int32 prime = 486187739; // number from http://stackoverflow.com/a/2816747/232574
			Action<ILGenerator> loadInstanceOpCode = i => i.Emit(OpCodes.Ldarg_0);
			if (typeof(T) != type) {
				var instanceLocal = ilGenerator.DeclareLocal(type);
				loadInstanceOpCode = i => i.Emit(OpCodes.Ldloc, instanceLocal);
				ilGenerator.Emit(OpCodes.Ldarg_0);
				ilGenerator.Emit(OpCodes.Castclass, type);
				ilGenerator.Emit(OpCodes.Stloc, instanceLocal);
			}
			var objectGetHashCode = typeof(Object).GetMethod(nameof(GetHashCode), Type.EmptyTypes);
			var hashCode = ilGenerator.DeclareLocal(typeof(Int32));
			ilGenerator.Emit(OpCodes.Ldc_I4, prime);
			ilGenerator.Emit(OpCodes.Stloc, hashCode);
			for (var i = 0; i < fields.Length; i++) {
				var field = fields[i];
				Func<Boolean> isValueType = () => field.FieldType.IsValueType;
				Action loadValueTypeMember = () => ilGenerator.Emit(OpCodes.Ldflda, field);
				Action loadReferenceTypeMember = () => ilGenerator.Emit(OpCodes.Ldfld, field);
				var isFirst = i == 0;

				EmitHashCodeIL<T>(ilGenerator, prime, isValueType, isFirst, hashCode, loadInstanceOpCode, loadValueTypeMember, field.FieldType, loadReferenceTypeMember, objectGetHashCode);
			}
			for (var i = 0; i < properties.Length; i++) {
				var property = properties[i];
				Func<Boolean> isValueType = () => property.PropertyType.IsValueType;
				Action loadValueTypeMember = () => ilGenerator.Emit(OpCodes.Call, property.GetGetMethod(nonPublic: true));
				Action loadReferenceTypeMember = loadValueTypeMember;
				var isFirst = i == 0 && !fields.Any();

				EmitHashCodeIL<T>(ilGenerator, prime, isValueType, isFirst, hashCode, loadInstanceOpCode, loadValueTypeMember, property.PropertyType, loadReferenceTypeMember, objectGetHashCode);
			}
			ilGenerator.Emit(OpCodes.Ldloc, hashCode);
			ilGenerator.Emit(OpCodes.Ret);
		}

		private static void EmitHashCodeIL<T>(ILGenerator ilGenerator,
		                                      Int32 prime,
		                                      Func<Boolean> isValueType,
		                                      Boolean isFirst,
		                                      LocalBuilder hashCode,
		                                      Action<ILGenerator> loadInstanceOpCode,
		                                      Action loadValueTypeMember,
		                                      Type memberType,
		                                      Action loadReferenceTypeMember,
		                                      MethodInfo objectGetHashCode) {
			if (isValueType()) {
				if (!isFirst) {
					ilGenerator.Emit(OpCodes.Ldloc, hashCode);
					ilGenerator.Emit(OpCodes.Ldc_I4, prime);
					ilGenerator.Emit(OpCodes.Mul);
				}
				loadInstanceOpCode(ilGenerator);
				loadValueTypeMember();
				ilGenerator.Emit(OpCodes.Call, memberType.GetMethod(nameof(GetHashCode), Type.EmptyTypes));
				if (!isFirst)
					ilGenerator.Emit(OpCodes.Add);
				ilGenerator.Emit(OpCodes.Stloc, hashCode);
			}
			else {
				var label = ilGenerator.DefineLabel();
				var hold = ilGenerator.DeclareLocal(memberType);
				loadInstanceOpCode(ilGenerator);
				loadReferenceTypeMember();
				ilGenerator.Emit(OpCodes.Stloc, hold);
				ilGenerator.Emit(OpCodes.Ldloc, hold);
				ilGenerator.Emit(OpCodes.Brfalse_S, label);

				if (!isFirst) {
					ilGenerator.Emit(OpCodes.Ldloc, hashCode);
					ilGenerator.Emit(OpCodes.Ldc_I4, prime);
					ilGenerator.Emit(OpCodes.Mul);
				}
				ilGenerator.Emit(OpCodes.Ldloc, hold);
				ilGenerator.Emit(OpCodes.Callvirt, objectGetHashCode);
				if (!isFirst)
					ilGenerator.Emit(OpCodes.Add);
				ilGenerator.Emit(OpCodes.Stloc, hashCode);
				ilGenerator.MarkLabel(label);
			}
		}
	}
}
