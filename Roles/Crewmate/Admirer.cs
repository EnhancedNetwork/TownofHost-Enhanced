using Hazel;
using LibCpp2IL.Elf;
using System.Collections.Generic;
using TOHE.Modules;
using TOHE.Roles.AddOns.Crewmate;
using TOHE.Roles.Double;
using TOHE.Roles.Neutral;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate;

public static class Admirer
{
    private static readonly int Id = 24800;
    private static List<byte> playerIdList = new();
    public static bool IsEnable = false;

    public static OptionItem AdmireCooldown;
    public static OptionItem KnowTargetRole;
    public static OptionItem SkillLimit;
    public static Dictionary<byte, int> AdmirerLimit;

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.OtherRoles, CustomRoles.Admirer);
        AdmireCooldown = FloatOptionItem.Create(Id + 10, "AdmireCooldown", new(1f, 180f, 1f), 5f, TabGroup.OtherRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Admirer])
            .SetValueFormat(OptionFormat.Seconds);
        KnowTargetRole = BooleanOptionItem.Create(Id + 11, "AdmirerKnowTargetRole", true, TabGroup.OtherRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Admirer])
            .SetHidden(true);
        SkillLimit = IntegerOptionItem.Create(Id + 12, "AdmirerSkillLimit", new(0, 100, 1), 1, TabGroup.OtherRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Admirer])
            .SetValueFormat(OptionFormat.Times);
    }
    public static void Init()
    {
        playerIdList = new();
        AdmirerLimit = new();
        IsEnable = false;
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        AdmirerLimit.Add(playerId, SkillLimit.GetInt());
        IsEnable = true;

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }

    private static void SendRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetAdmireLimit, SendOption.Reliable, -1);
        writer.Write(playerId);
        writer.Write(AdmirerLimit[playerId]);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        byte playerId = reader.ReadByte();
        int Limit = reader.ReadInt32();
        if (!AdmirerLimit.ContainsKey(playerId))
            AdmirerLimit.Add(playerId, Limit);
        else AdmirerLimit[playerId] = Limit;
    }
    public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = AdmirerLimit[id] >= 1 ? AdmireCooldown.GetFloat() : 300f;
    public static bool CanUseKillButton(PlayerControl player) => !player.Data.IsDead && (AdmirerLimit.TryGetValue(player.PlayerId, out var x) ? x : 1) >= 1;
    public static void OnCheckMurder(PlayerControl killer, PlayerControl target)
    {
        if (AdmirerLimit[killer.PlayerId] < 1) return;
        if (Mini.Age < 18 && (target.Is(CustomRoles.NiceMini) || target.Is(CustomRoles.EvilMini)))
        {
            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Succubus), GetString("CantRecruit")));
            return;
        }

        var convertSubRole = ConvertManager.GetConvertSubRole(killer, CustomRoles.Admired);
        if (ConvertManager.CanBeConvertSubRole(target, convertSubRole, killer))
        {
            AdmirerLimit[killer.PlayerId]--;
            Logger.Info("设置职业:" + target?.Data?.PlayerName + " = " + target.GetCustomRole().ToString() + " + " + convertSubRole.ToString(), "Admirer Assign");
            target.RpcSetCustomRole(convertSubRole);
            ConvertManager.SetPlayerConverted(killer, target, convertSubRole);
            SendRPC(killer.PlayerId); //Sync skill
            killer.ResetKillCooldown();
            killer.SetKillCooldown(forceAnime: !DisableShieldAnimations.GetBool());
            target.RpcGuardAndKill(killer);
            target.ResetKillCooldown();
            target.SetKillCooldown(forceAnime: true);
            Logger.Info($"{killer.GetNameWithRole()} : 剩余{AdmirerLimit[killer.PlayerId]}次仰慕机会", "Admirer");
        }
        else
        {
            SendRPC(killer.PlayerId); //Sync skill
            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Admirer), GetString("AdmirerInvalidTarget")));
            Logger.Info($"{killer.GetNameWithRole()} : 剩余{AdmirerLimit[killer.PlayerId]}次仰慕机会", "Admirer");
            return;
        }
    }

    /*public static bool KnowRole(PlayerControl player, PlayerControl target) //Admirer knows target's role
    {
        if (!KnowTargetRole.GetBool()) return false;

        if ((player.Is(CustomRoles.Admirer) || player.Is(CustomRoles.Admirer))
            && (target.Is(CustomRoles.Admirer) && target.Is(CustomRoles.Admired)))
        {
            if (ConvertManager.GetConvertSubRole(player, CustomRoles.Admired) == ConvertManager.GetConvertSubRole(target, CustomRoles.Admired))
                return true;
        }

        if (player.Is(CustomRoles.Admirer) || target.Is(CustomRoles.Admirer))
        {
            if (ConvertManager.KnowRole(player, target)) return true;
        }

        return false;
    }*/
    public static string GetAdmireLimit(byte playerId) => Utils.ColorString(AdmirerLimit[playerId] >= 1 ? Utils.GetRoleColor(CustomRoles.Admirer).ShadeColor(0.25f) : Color.gray, $"({AdmirerLimit[playerId]})");
    public static bool CanBeAdmired(this PlayerControl pc, PlayerControl admirer)
    {
        return pc != null && (pc.GetCustomRole().IsCrewmate() || pc.GetCustomRole().IsImpostor() || pc.GetCustomRole().IsNeutral())
            && !pc.Is(CustomRoles.Soulless) && !pc.Is(CustomRoles.Lovers) && !pc.Is(CustomRoles.Loyal)
            && !((pc.Is(CustomRoles.NiceMini) || pc.Is(CustomRoles.EvilMini)) && Mini.Age < 18)
            && !(pc.Is(CustomRoles.Hurried) && !Hurried.CanBeConverted.GetBool());
    }
}
