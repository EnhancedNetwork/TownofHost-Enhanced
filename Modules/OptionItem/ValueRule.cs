using System;

namespace TOHE;

public abstract class ValueRule<T>(T minValue, T maxValue, T step)
{
    public T MinValue { get; protected set; } = minValue;
    public T MaxValue { get; protected set; } = maxValue;
    public T Step { get; protected set; } = step;

    public ValueRule((T, T, T) tuple)
    : this(tuple.Item1, tuple.Item2, tuple.Item3)
    { }

    public abstract int RepeatIndex(int value);
    public abstract T GetValueByIndex(int index);
    public abstract int GetNearestIndex(T num);
}

public class IntegerValueRule : ValueRule<int>
{
    public IntegerValueRule(int minValue, int maxValue, int step)
    : base(minValue, maxValue, step) { }
    public IntegerValueRule((int, int, int) tuple)
    : base(tuple) { }

    public static implicit operator IntegerValueRule((int, int, int) tuple)
        => new(tuple);

    public override int RepeatIndex(int value)
    {
        int MaxIndex = (MaxValue - MinValue) / Step;
        value %= (MaxIndex + 1);
        if (value < 0) value = MaxIndex;
        return value;
    }

    public override int GetValueByIndex(int index)
        => RepeatIndex(index) * Step + MinValue;

    public override int GetNearestIndex(int num)
    {
        return (int)Math.Round((num - MinValue) / (float)Step);
    }
}

public class FloatValueRule : ValueRule<float>
{
    public FloatValueRule(float minValue, float maxValue, float step)
    : base(minValue, maxValue, step) { }
    public FloatValueRule((float, float, float) tuple)
    : base(tuple) { }

    public static implicit operator FloatValueRule((float, float, float) tuple)
        => new(tuple);

    public override int RepeatIndex(int value)
    {
        int MaxIndex = (int)((MaxValue - MinValue) / Step);
        value %= (MaxIndex + 1);
        if (value < 0) value = MaxIndex;
        return value;
    }

    public override float GetValueByIndex(int index)
        => RepeatIndex(index) * Step + MinValue;

    public override int GetNearestIndex(float num)
    {
        return (int)Math.Round((num - MinValue) / Step);
    }
}