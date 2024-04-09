using System;

namespace TOHE;

public interface IRandom
{
    /// <summary>Generates a random number between 0 and maxValue</summary>
    public int Next(int maxValue);
    /// <summary>Generates a random number between minValue and maxValue</summary>
    public int Next(int minValue, int maxValue);

    // == static ==
    // List of classes implementing IRandom
    public static Dictionary<int, Type> randomTypes = new()
    {
        { 0, typeof(NetRandomWrapper) }, //Default
        { 1, typeof(NetRandomWrapper) },
        { 2, typeof(HashRandomWrapper) },
        { 3, typeof(Xorshift) },
        { 4, typeof(MersenneTwister) },
    };

    public static IRandom Instance { get; private set; }
    public static void SetInstance(IRandom instance)
    {
        if (instance != null)
            Instance = instance;
    }

    public static void SetInstanceById(int id)
    {
        if (randomTypes.TryGetValue(id, out var type))
        {
            // Current instance is null or current instance type does not match specified type
            if (Instance == null || Instance.GetType() != type)
            {
                Instance = Activator.CreateInstance(type) as IRandom ?? Instance;
            }
        }
        else Logger.Warn($"Invalid ID: {id}", "IRandom.SetInstanceById");
    }
}