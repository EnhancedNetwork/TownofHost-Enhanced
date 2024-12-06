using System;

namespace TOHE;

public static class EnumHelper
{
    /// <summary>
    /// Get all values of enum
    /// </summary>
    /// <typeparam name="T">Type of enum to be obtained</typeparam>
    /// <returns>All values of T</returns>
    public static T[] GetAllValues<T>() where T : Enum => Enum.GetValues(typeof(T)) as T[];
    /// <summary>
    /// Get all names in enum
    /// </summary>
    /// <typeparam name="T">Type of enum to be obtained</typeparam>
    /// <returns>All values of T</returns>
    public static string[] GetAllNames<T>() where T : Enum => Enum.GetNames(typeof(T));


    /// <summary>
    /// Splits all values in the enum to equal segments
    /// </summary>
    /// <typeparam name="TEnum">Type of enum to be obtained</typeparam>
    /// <param name="chunkSize">The number of elements each chunk should contain. </param>
    /// <returns>A list of arrays, each containing up to <paramref name="chunkSize"/> elements of the enum type.</returns>
    public static List<TEnum[]> Achunk<TEnum>(int chunkSize, bool shuffle = false, Func<TEnum, bool> exclude = null) where TEnum : Enum
    {
        List<TEnum[]> chunkedList = [];
        TEnum[] allValues = GetAllValues<TEnum>();
        if (shuffle) allValues = allValues.Shuffle().ToArray();
        if (exclude != null) allValues = allValues.Where(exclude).ToArray();

        for (int i = 0; i < allValues.Length; i += chunkSize)
        {
            TEnum[] chunk = new TEnum[Math.Min(chunkSize, allValues.Length - i)];
            Array.Copy(allValues, i, chunk, 0, chunk.Length);
            chunkedList.Add(chunk);
        }

        return chunkedList;

    }
}
