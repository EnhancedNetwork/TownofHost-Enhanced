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
}
