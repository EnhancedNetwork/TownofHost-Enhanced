using AmongUs.GameOptions;
using System;


namespace TOHE;

public abstract class OptionBackupValue
{
    public abstract void Restore(IGameOptions option);
}

public abstract class OptionBackupValueBase<NameT, ValueT>(NameT name, ValueT value) : OptionBackupValue
where NameT : Enum
{
    public readonly NameT OptionName = name;
    public readonly ValueT Value = value;
}

public class ByteOptionBackupValue(ByteOptionNames name, byte value) : OptionBackupValueBase<ByteOptionNames, byte>(name, value)
{
    public override void Restore(IGameOptions option)
    {
        option.SetByte(OptionName, Value);
    }
}
public class BoolOptionBackupValue(BoolOptionNames name, bool value) : OptionBackupValueBase<BoolOptionNames, bool>(name, value)
{
    public override void Restore(IGameOptions option)
    {
        if (OptionName != BoolOptionNames.GhostsDoTasks)
            option.SetBool(OptionName, Value);
    }
}
public class FloatOptionBackupValue(FloatOptionNames name, float value) : OptionBackupValueBase<FloatOptionNames, float>(name, value)
{
    public override void Restore(IGameOptions option)
    {
        option.SetFloat(OptionName, Value);
    }
}
public class IntOptionBackupValue(Int32OptionNames name, int value) : OptionBackupValueBase<Int32OptionNames, int>(name, value)
{
    public override void Restore(IGameOptions option)
    {
        option.SetInt(OptionName, Value);
    }
}
public class UIntOptionBackupValue(UInt32OptionNames name, uint value) : OptionBackupValueBase<UInt32OptionNames, uint>(name, value)
{
    public override void Restore(IGameOptions option)
    {
        option.SetUInt(OptionName, Value);
    }
}

public class RoleRateBackupValue(RoleTypes type, int maxCount, int chance) : OptionBackupValue
{
    public RoleTypes roleType = type;
    public int maxCount = maxCount;
    public int chance = chance;

    public override void Restore(IGameOptions option)
    {
        option.RoleOptions.SetRoleRate(roleType, maxCount, chance);
    }
}