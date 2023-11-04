using AmongUs.GameOptions;
using System.Collections.Generic;

using static TOHE.Options;

namespace TOHE.Roles.Neutral;

public static class Pyromaniac
{
    private static readonly int Id = 12802;
    public static List<byte> playerIdList = new();
    public static List<byte> DousedList = new();
    public static bool IsEnable = false;

    private static OptionItem KillCooldown;
    private static OptionItem DouseCooldown;
    private static OptionItem BurnCooldown;
    public static OptionItem CanVent;
    private static OptionItem HasImpostorVision;

    public static void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Pyromaniac, 1, zeroOne: false);
        KillCooldown = FloatOptionItem.Create(Id + 10, "KillCooldown", new(0f, 180f, 2.5f), 25f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Pyromaniac])
            .SetValueFormat(OptionFormat.Seconds);
        DouseCooldown = FloatOptionItem.Create(Id + 11, "PyroDouseCooldown", new(0f, 180f, 2.5f), 20f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Pyromaniac])
            .SetValueFormat(OptionFormat.Seconds);
        BurnCooldown = FloatOptionItem.Create(Id + 12, "PyroBurnCooldown", new(0f, 180f, 2.5f), 5f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Pyromaniac])
            .SetValueFormat(OptionFormat.Seconds);
        CanVent = BooleanOptionItem.Create(Id + 13, "CanVent", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Pyromaniac]);
        HasImpostorVision = BooleanOptionItem.Create(Id + 14, "ImpostorVision", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Pyromaniac]);
    }
    public static void Init()
    {
        playerIdList = new();
        DousedList = new();
        IsEnable = false;
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        IsEnable = true;

        // Double Trigger
        var pc = Utils.GetPlayerById(playerId);
        pc.AddDoubleTrigger();

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }
    public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
    public static void ApplyGameOptions(IGameOptions opt) => opt.SetVision(HasImpostorVision.GetBool());
    public static bool OnCheckMurder(PlayerControl killer, PlayerControl target)
    {
        if (killer == null) return true;
        if (target == null) return true;

        if (DousedList.Contains(target.PlayerId))
        {
            _ = new LateTask(() => { killer.SetKillCooldown(BurnCooldown.GetFloat()); }, 0.1f);
            return true;
        }
        else
        {
            return killer.CheckDoubleTrigger(target, () => { DousedList.Add(target.PlayerId); killer.SetKillCooldown(DouseCooldown.GetFloat()); Utils.NotifyRoles(SpecifySeer: killer); });
        }
    }
}
