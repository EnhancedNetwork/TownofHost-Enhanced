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
        { 1, typeof(HashRandomWrapper) },
        { 2, typeof(Xorshift) },
        { 3, typeof(MersenneTwister) },
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
            Instance = Activator.CreateInstance(type) as IRandom;
            Logger.Info($"Set IRandom instance to {type.Name}", "IRandom");
        }
        else
        {
            Logger.Warn($"Invalid ID: {id}", "IRandom.SetInstanceById");
            Instance = new NetRandomWrapper();
        }
    }
}
