using System;
using System.Collections;
using System.Linq.Expressions;

namespace TOHE;

// Credit: https://github.com/dabao40/TheOtherRolesGMIA/blob/main/TheOtherRoles/Utilities/EnumerationHelpers.cs

public static class EnumerationHelpers
{
    /// <summary>
    /// Improves the speed of code in a foreach loop
    /// Used when List has a reference to Il2CppSystem.Collections.Generic
    /// </summary>
    /// <returns>IEnumerable of T</returns>
    public static IEnumerable<T> GetFastEnumerator<T>(this Il2CppSystem.Collections.Generic.List<T> list) where T : Il2CppSystem.Object => new Il2CppListEnumerable<T>(list);
}

public unsafe class Il2CppListEnumerable<T> : IEnumerable<T>, IEnumerator<T> where T : Il2CppSystem.Object
{
    private struct Il2CppListStruct
    {
#pragma warning disable CS0169
#pragma warning disable IDE0051
#pragma warning disable IDE0044
        private IntPtr _unusedPtr1;
        private IntPtr _unusedPtr2;
#pragma warning restore IDE0044
#pragma warning restore IDE0051

#pragma warning disable CS0649
        public IntPtr _items;
        public int _size;
#pragma warning restore CS0649
#pragma warning restore CS0169
    }

    private static readonly int _elemSize;
    private static readonly int _offset;

#pragma warning disable IDE0044
    private static Func<IntPtr, T> _objFactory;
#pragma warning restore IDE0044

    static Il2CppListEnumerable()
    {
        _elemSize = IntPtr.Size;
        _offset = 4 * IntPtr.Size;

        var constructor = typeof(T).GetConstructor(new[] { typeof(IntPtr) });
        var ptr = Expression.Parameter(typeof(IntPtr));
        var create = Expression.New(constructor!, ptr);
        var lambda = Expression.Lambda<Func<IntPtr, T>>(create, ptr);
        _objFactory = lambda.Compile();
    }

    private readonly IntPtr _arrayPointer;
    private readonly int _count;
    private int _index = -1;

    public Il2CppListEnumerable(Il2CppSystem.Collections.Generic.List<T> list)
    {
        var listStruct = (Il2CppListStruct*)list.Pointer;
        _count = listStruct->_size;
        _arrayPointer = listStruct->_items;
    }

    object IEnumerator.Current => Current;
    public T Current { get; private set; }

    public bool MoveNext()
    {
        if (++_index >= _count) return false;
        var refPtr = *(IntPtr*)IntPtr.Add(IntPtr.Add(_arrayPointer, _offset), _index * _elemSize);
        Current = _objFactory(refPtr);
        return true;
    }

    public void Reset()
    {
        _index = -1;
    }

    public System.Collections.Generic.IEnumerator<T> GetEnumerator()
    {
        return this;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return this;
    }

#pragma warning disable CA1816
    public void Dispose()
    {
    }
#pragma warning restore CA1816
}