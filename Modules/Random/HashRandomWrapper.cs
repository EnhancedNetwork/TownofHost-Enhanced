namespace TOHE;

public class HashRandomWrapper : IRandom
{
    public int Next(int minValue, int maxValue) => HashRandom.Next(minValue, maxValue);
    public int Next(int maxValue) => HashRandom.Next(maxValue);
    public static uint Next() => HashRandom.Next();
    public static int FastNext(int maxValue) => HashRandom.FastNext(maxValue);
}
