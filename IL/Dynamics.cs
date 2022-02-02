using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using MoreCommands.Utils;

namespace MoreCommands.IL;

public static class Dynamics
{
    private static readonly IDictionary<FieldInfo, Delegate> GetterCache = new Dictionary<FieldInfo, Delegate>();
    private static readonly IDictionary<FieldInfo, Delegate> SetterCache = new Dictionary<FieldInfo, Delegate>();

    // GETTERS
    public static Func<T> CreateStaticGetter<TOwner, T>(string fieldName, bool isPublic = false) => CreateStaticGetter<T>(typeof(TOwner), fieldName, isPublic);
    
    public static Func<T> CreateStaticGetter<T>(Type owner, string fieldName, bool isPublic = false) => (Func<T>) CreateGetter<object, T>(owner, fieldName, true, isPublic);

    public static Func<TOwner, T> CreateGetter<TOwner, T>(string fieldName, bool isPublic = false) => (Func<TOwner, T>) CreateGetter<TOwner, T>(typeof(TOwner), fieldName, false, isPublic);
    
    // SETTERS
    public static Action<T> CreateStaticSetter<TOwner, T>(string fieldName, bool isPublic = false) => CreateStaticSetter<T>(typeof(TOwner), fieldName, isPublic);
    
    public static Action<T> CreateStaticSetter<T>(Type owner, string fieldName, bool isPublic = false) => (Action<T>) CreateSetter<object, T>(owner, fieldName, true, isPublic);
    
    public static Action<TOwner, T> CreateSetter<TOwner, T>(string fieldName, bool isPublic = false) => (Action<TOwner, T>) CreateSetter<TOwner, T>(typeof(TOwner), fieldName, false, isPublic);
    
    // INVOKERS
    public static T CreateStaticInvoker<TOwner, T>(string name, bool isPublic = false, params Type[] parameterTypes) where T : Delegate => CreateStaticInvoker<T>(typeof(TOwner), name, isPublic, parameterTypes);
    
    public static T CreateStaticInvoker<T>(Type owner, string name, bool isPublic = false, params Type[] parameterTypes) where T : Delegate => CreateInvoker<T>(owner, name, true, isPublic, parameterTypes);
    
    public static T CreateInvoker<TOwner, T>(string name, bool isPublic = false, params Type[] parameterTypes) where T : Delegate => CreateInvoker<T>(typeof(TOwner), name, false, isPublic, parameterTypes);

    // INTERNALS
    private static Delegate CreateGetter<TOwner, T>(Type owner, string fieldName, bool isStatic, bool isPublic) 
    {
        (FieldInfo field, DynamicMethod accessor) = CreateAccessor(owner, false, fieldName, isStatic, isPublic);
        if (GetterCache.ContainsKey(field)) return isStatic ? (Func<T>) GetterCache[field] : (Func<TOwner, T>) GetterCache[field];
        
        ILGenerator c = accessor.GetILGenerator();
        
        if (!isStatic) c.Emit(OpCodes.Ldarg_0); // Load this
        c.Emit(isStatic ? OpCodes.Ldsfld : OpCodes.Ldfld, field); // Load the field
        c.Emit(OpCodes.Ret); // Return the field

        return GetterCache[field] = isStatic ? accessor.CreateDelegate<Func<T>>() : accessor.CreateDelegate<Func<TOwner, T>>();
    }
    
    private static Delegate CreateSetter<TOwner, T>(Type owner, string fieldName, bool isStatic, bool isPublic)
    {
        (FieldInfo field, DynamicMethod accessor) = CreateAccessor(owner, true, fieldName, isStatic, isPublic);
        if (SetterCache.ContainsKey(field)) return isStatic ? (Action<T>) SetterCache[field] : (Action<TOwner, T>) SetterCache[field];
        
        ILGenerator c = accessor.GetILGenerator();
        
        if (!isStatic) c.Emit(OpCodes.Ldarg_0); // Load this
        c.Emit(isStatic ? OpCodes.Ldarg_0 : OpCodes.Ldarg_1); // Load the first arg (second if first is this)
        c.Emit(isStatic ? OpCodes.Stsfld : OpCodes.Stfld, field); // Store the value in the field
        c.Emit(OpCodes.Ret); // Return from the method

        return SetterCache[field] = isStatic ? accessor.CreateDelegate<Action<T>>() : accessor.CreateDelegate<Action<TOwner, T>>();
    }

    private static (FieldInfo field, DynamicMethod accessor) CreateAccessor(Type owner, bool set, string fieldName, bool isStatic, bool isPublic)
    {
        BindingFlags flags = isPublic ? BindingFlags.Public : BindingFlags.NonPublic;
        if (isStatic) flags |= BindingFlags.Static;
        else flags |= BindingFlags.Instance;

        FieldInfo field = owner.GetField(fieldName, flags);
        if (field is null) throw new ArgumentException("No field could be found in the given type of the given name with the given binding flags.", nameof(fieldName));
        if ((set ? SetterCache : GetterCache).ContainsKey(field)) return (field, null);

        DynamicMethod accessor = new($"MC_SYNTH_{(set ? "SET" : "GET")}__" + fieldName, MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard,
            set ? null : field.FieldType, set ? isStatic ? new[] { field.FieldType } : new[] { owner, field.FieldType } : isStatic ? Array.Empty<Type>() : new[] { owner },
            owner, true);

        return (field, accessor);
    }
    
    private static T CreateInvoker<T>(Type owner, string name, bool isStatic, bool isPublic, Type[] parameterTypes) where T : Delegate
    {
        Type typeOfT = typeof(T);
        if (!isStatic && (typeOfT.GetGenericArguments().Length == 0 || typeOfT.GetGenericArguments()[0] != owner))
            throw new ArgumentException("First argument of invoker must be of the same type as its owner for non-static methods.");
        
        return Util.GetMethod(owner, name, isStatic, isPublic, parameterTypes).CreateDelegate<T>();
    }
}