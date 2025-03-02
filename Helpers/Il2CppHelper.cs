using Il2CppInterop.Runtime.InteropTypes;
using System;
using System.Linq.Expressions;

namespace TOHE;

public static class Il2CppHelper
{
    // From https://github.com/TheOtherRolesAU/TheOtherRoles/blob/main/TheOtherRoles/Il2CppHelpers.cs
    private static class CastHelper<T> where T : Il2CppObjectBase
    {
        public static Func<IntPtr, T> Cast;
        static CastHelper()
        {
            var constructor = typeof(T).GetConstructor([typeof(IntPtr)]);
            var ptr = Expression.Parameter(typeof(IntPtr));
            var create = Expression.New(constructor!, ptr);
            var lambda = Expression.Lambda<Func<IntPtr, T>>(create, ptr);
            Cast = lambda.Compile();
        }
    }

    public static Il2CppSystem.Collections.Generic.List<T> ToIl2Cpp<T>(this List<T> sysList)
    {
        Il2CppSystem.Collections.Generic.List<T> list = new();
        foreach (T item in sysList)
        {
            list.Add(item);
        }
        return list;
    }

    public static List<T> ToManaged<T>(this Il2CppSystem.Collections.Generic.List<T> iList)
    {
        return iList.ToArray().ToList();
    }

    public static T CastFast<T>(this Il2CppObjectBase obj) where T : Il2CppObjectBase
    {
        if (obj is T casted) return casted;
        return obj.Pointer.CastFast<T>();
    }

    private static T CastFast<T>(this IntPtr ptr) where T : Il2CppObjectBase
    {
        return CastHelper<T>.Cast(ptr);
    }

    public static bool TryCast<T>(this Il2CppObjectBase obj, out T casted)
        where T : Il2CppObjectBase
    {
        casted = obj.TryCast<T>();
        return casted != null;
    }
}
