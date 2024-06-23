namespace TOHE;

public static class ConversionHelper
{
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
}
