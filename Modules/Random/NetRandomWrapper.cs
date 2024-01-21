using System;

namespace TOHE;

public class NetRandomWrapper(Random instance) : IRandom
{
    public Random wrapping = instance;

    public NetRandomWrapper() : this(new Random())
    { }
    public NetRandomWrapper(int seed) : this(new Random(seed))
    { }

    public int Next(int minValue, int maxValue) => wrapping.Next(minValue, maxValue);
    public int Next(int maxValue) => wrapping.Next(maxValue);
    public int Next() => wrapping.Next();
}