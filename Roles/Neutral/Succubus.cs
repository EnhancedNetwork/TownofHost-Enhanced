using Hazel;
using System.Collections.Generic;
using System.Linq;
using TOHE.Roles.Double;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Neutral;

public static class Succubus
{
    private static readonly int Id = 11200;
    private static List<byte> playerIdList = new();
    public static bool IsEnable = false;

    public static OptionItem CharmCooldown;
    public static OptionItem CharmCooldownIncrese;
    public static OptionItem CharmMax;
    public static OptionItem KnowTargetRole;
    public static OptionItem TargetKnowOtherTarget;
    public static OptionItem CanCharmNeutral;
    public static OptionItem CharmedCountMode;

    public static readonly string[] charmedCountMode =
    {
        "CharmedCountMode.None",
        "CharmedCountMode.Succubus",
        "CharmedCountMode.Original",
    };

    private static int CharmLimit = new();

    public static void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Succubus, 1, zeroOne: false);
        CharmCooldown = FloatOptionItem.Create(Id + 10, "SuccubusCharmCooldown", new(0f, 180f, 2.5f), 30f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Succubus])
            .SetValueFormat(OptionFormat.Seconds);
        CharmCooldownIncrese = FloatOptionItem.Create(Id + 11, "SuccubusCharmCooldownIncrese", new(0f, 180f, 2.5f), 10f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Succubus])
            .SetValueFormat(OptionFormat.Seconds);
        CharmMax = IntegerOptionItem.Create(Id + 12, "SuccubusCharmMax", new(1, 15, 1), 15, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Succubus])
            .SetValueFormat(OptionFormat.Times);
        KnowTargetRole = BooleanOptionItem.Create(Id + 13, "SuccubusKnowTargetRole", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Succubus]);
        TargetKnowOtherTarget = BooleanOptionItem.Create(Id + 14, "SuccubusTargetKnowOtherTarget", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Succubus]);
        CharmedCountMode = StringOptionItem.Create(Id + 17, "CharmedCountMode", charmedCountMode, 1, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Succubus]);
        CanCharmNeutral = BooleanOptionItem.Create(Id + 18, "SuccubusCanCharmNeutral", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Succubus]);
    }
    public static void Init()
    {
        playerIdList = new();
        CharmLimit = new();
        IsEnable = false;
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        CharmLimit = CharmMax.GetInt();
        IsEnable = true;

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }

    private static void SendRPC()
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetSuccubusCharmLimit, SendOption.Reliable, -1);
        writer.Write(CharmLimit);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        CharmLimit = reader.ReadInt32();
    }
    public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = CharmLimit >= 1 ? CharmCooldown.GetFloat() + (CharmMax.GetInt() - CharmLimit) * CharmCooldownIncrese.GetFloat() : 300f;
    public static bool CanUseKillButton(PlayerControl player) => !player.Data.IsDead && CharmLimit >= 1;
    public static void OnCheckMurder(PlayerControl killer, PlayerControl target)
    {
        if (CharmLimit < 1) return;
        if (Mini.Age != 18 && (target.Is(CustomRoles.NiceMini) || target.Is(CustomRoles.EvilMini)))
        {
            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Succubus), GetString("Cantkillkid")));
        }
        else if (CanBeCharmed(target) && Mini.Age == 18 || CanBeCharmed(target) && Mini.Age != 18 && !(target.Is(CustomRoles.NiceMini) || target.Is(CustomRoles.EvilMini)))
        {
            CharmLimit--;
            SendRPC();
            target.RpcSetCustomRole(CustomRoles.Charmed);

            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Succubus), GetString("SuccubusCharmedPlayer")));
            target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Succubus), GetString("CharmedBySuccubus")));
            Utils.NotifyRoles();

            killer.ResetKillCooldown();
            killer.SetKillCooldown();
            if (!DisableShieldAnimations.GetBool()) killer.RpcGuardAndKill(target);
            target.RpcGuardAndKill(killer);
            target.RpcGuardAndKill(target);

            Logger.Info("设置职业:" + target?.Data?.PlayerName + " = " + target.GetCustomRole().ToString() + " + " + CustomRoles.Charmed.ToString(), "Assign " + CustomRoles.Charmed.ToString());
            Logger.Info($"{killer.GetNameWithRole()} : 剩余{CharmLimit}次魅惑机会", "Succubus");
            return;
        }
        killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Succubus), GetString("SuccubusInvalidTarget")));
        Logger.Info($"{killer.GetNameWithRole()} : 剩余{CharmLimit}次魅惑机会", "Succubus");
        return;
    }
    public static bool KnowRole(PlayerControl player, PlayerControl target)
    {
        if (player.Is(CustomRoles.Charmed) && target.Is(CustomRoles.Succubus)) return true;
        if (KnowTargetRole.GetBool() && player.Is(CustomRoles.Succubus) && target.Is(CustomRoles.Charmed)) return true;
        if (TargetKnowOtherTarget.GetBool() && player.Is(CustomRoles.Charmed) && target.Is(CustomRoles.Charmed)) return true;
        return false;
    }
    public static string GetCharmLimit() => Utils.ColorString(CharmLimit >= 1 ? Utils.GetRoleColor(CustomRoles.Succubus).ShadeColor(0.25f) : Color.gray, $"({CharmLimit})");
    public static bool CanBeCharmed(this PlayerControl pc)
    {
        return pc != null && (pc.GetCustomRole().IsCrewmate() || pc.GetCustomRole().IsImpostor() || 
            (CanCharmNeutral.GetBool() && pc.GetCustomRole().IsNeutral())) && !pc.Is(CustomRoles.Charmed) && !pc.Is(CustomRoles.Admired) && !pc.Is(CustomRoles.Loyal) && !pc.Is(CustomRoles.Infectious) && !pc.Is(CustomRoles.Virus) && !pc.Is(CustomRoles.Succubus);
    }
}