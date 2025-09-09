using AmongUs.GameOptions;

namespace TOHE.Modules.Extensions;

public static class IGameManagerEx
{
    public static void Set(this BoolOptionNames name, bool value, IGameOptions opt) => opt.SetBool(name, value);
    public static void Set(this BoolOptionNames name, bool value, NormalGameOptionsV10 opt)
    {
        if (name is not BoolOptionNames.GhostsDoTasks and not BoolOptionNames.Roles) opt.SetBool(name, value);
    }
    public static void Set(this BoolOptionNames name, bool value, HideNSeekGameOptionsV10 opt) => opt.SetBool(name, value);

    public static void Set(this Int32OptionNames name, int value, IGameOptions opt) => opt.SetInt(name, value);
    public static void Set(this Int32OptionNames name, int value, NormalGameOptionsV10 opt) => opt.SetInt(name, value);
    public static void Set(this Int32OptionNames name, int value, HideNSeekGameOptionsV10 opt) => opt.SetInt(name, value);

    public static void Set(this FloatOptionNames name, float value, IGameOptions opt) => opt.SetFloat(name, value);
    public static void Set(this FloatOptionNames name, float value, NormalGameOptionsV10 opt) => opt.SetFloat(name, value);
    public static void Set(this FloatOptionNames name, float value, HideNSeekGameOptionsV10 opt) => opt.SetFloat(name, value);

    public static void Set(this ByteOptionNames name, byte value, IGameOptions opt) => opt.SetByte(name, value);
    public static void Set(this ByteOptionNames name, byte value, NormalGameOptionsV10 opt) => opt.SetByte(name, value);
    public static void Set(this ByteOptionNames name, byte value, HideNSeekGameOptionsV10 opt) => opt.SetByte(name, value);

    public static void Set(this UInt32OptionNames name, uint value, IGameOptions opt) => opt.SetUInt(name, value);
    public static void Set(this UInt32OptionNames name, uint value, NormalGameOptionsV10 opt) => opt.SetUInt(name, value);
    public static void Set(this UInt32OptionNames name, uint value, HideNSeekGameOptionsV10 opt) => opt.SetUInt(name, value);
}
