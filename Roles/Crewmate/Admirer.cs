using Hazel;
using System.Collections.Generic;
using TOHE.Roles.AddOns.Crewmate;
using TOHE.Roles.Double;
using TOHE.Roles.Neutral;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate;

public static class Admirer
{
    private static readonly int Id = 30000;
    private static List<byte> playerIdList = new();
    public static bool IsEnable = false;

    public static OptionItem AdmireCooldown;
    public static OptionItem KnowTargetRole;
    public static OptionItem SkillLimit;
    public static Dictionary<byte, int> AdmirerLimit;
    private static Dictionary<byte, List<byte>> AdmiredList;

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.OtherRoles, CustomRoles.Admirer);
        AdmireCooldown = FloatOptionItem.Create(Id + 10, "AdmireCooldown", new(1f, 180f, 1f), 5f, TabGroup.OtherRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Admirer])
            .SetValueFormat(OptionFormat.Seconds);
        KnowTargetRole = BooleanOptionItem.Create(Id + 11, "AdmirerKnowTargetRole", true, TabGroup.OtherRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Admirer]);
        SkillLimit = IntegerOptionItem.Create(Id + 12, "AdmirerSkillLimit", new(0, 100, 1), 1, TabGroup.OtherRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Admirer])
            .SetValueFormat(OptionFormat.Times);
    }
    public static void Init()
    {
        playerIdList = new();
        AdmirerLimit = new();
        AdmiredList = new();
        IsEnable = false;
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        AdmirerLimit.Add(playerId, SkillLimit.GetInt());
        AdmiredList.Add(playerId, new());
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

    private static void SendRPC(byte playerId, byte targetId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncAdmiredList, SendOption.Reliable, -1);
        writer.Write(playerId);
        writer.Write(targetId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader, bool isList)
    {
        byte playerId = reader.ReadByte();
        byte targetId;
        int Limit;
        if (!isList)
        {
            Limit = reader.ReadInt32();
            if (!AdmirerLimit.ContainsKey(playerId))
                AdmirerLimit.Add(playerId, Limit);
            else AdmirerLimit[playerId] = Limit;
        }
        else
        {
            targetId = reader.ReadByte();
            if (!AdmiredList.ContainsKey(playerId))
                AdmiredList.Add(playerId, new());
            else AdmiredList[playerId].Add(targetId);
        }
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
        if (!AdmirerLimit.ContainsKey(killer.PlayerId))
            Add(killer.PlayerId);
        if (!AdmiredList.ContainsKey(killer.PlayerId))
            AdmiredList.Add(killer.PlayerId, new());

        if (AdmirerLimit[killer.PlayerId] < 1) return;
        if (CanBeAdmired(target, killer))
        {
            AdmirerLimit[killer.PlayerId]--;
            SendRPC(killer.PlayerId); //Sync skill
            if (KnowTargetRole.GetBool())
            {
                AdmiredList[killer.PlayerId].Add(target.PlayerId);
                SendRPC(killer.PlayerId, target.PlayerId); //Sync playerId list
            }

            if (!killer.Is(CustomRoles.Madmate) && !killer.Is(CustomRoles.Recruit) && !killer.Is(CustomRoles.Charmed) 
                && !killer.Is(CustomRoles.Infected) && !killer.Is(CustomRoles.Contagious))
            {
                target.RpcSetCustomRole(CustomRoles.Admired);
                killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Admirer), GetString("AdmiredPlayer")));
                target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Admirer), GetString("AdmirerAdmired")));
                Utils.NotifyRoles();
            }
            else if (killer.Is(CustomRoles.Madmate) && target.CanBeMadmate(inGame: true))
            {
                target.RpcSetCustomRole(CustomRoles.Madmate);
                killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Madmate), GetString("AdmiredPlayer")));
                target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Madmate), GetString("AdmirerAdmired")));                
                Utils.NotifyRoles();
            }
            else if (killer.Is(CustomRoles.Recruit) && target.CanBeSidekick())
            {
                target.RpcSetCustomRole(CustomRoles.Recruit);
                killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Recruit), GetString("AdmiredPlayer")));
                target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Recruit), GetString("AdmirerAdmired")));
                Utils.NotifyRoles();
            }
            else if (killer.Is(CustomRoles.Charmed) && target.CanBeCharmed())
            {
                target.RpcSetCustomRole(CustomRoles.Charmed);
                killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Charmed), GetString("AdmiredPlayer")));
                target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Charmed), GetString("AdmirerAdmired")));
                Utils.NotifyRoles();
            }
            else if (killer.Is(CustomRoles.Infected) && target.CanBeInfected())
            {
                target.RpcSetCustomRole(CustomRoles.Infected);
                killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Infected), GetString("AdmiredPlayer")));
                target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Infected), GetString("AdmirerAdmired")));
                Utils.NotifyRoles();
            }
            else if (killer.Is(CustomRoles.Contagious) && target.CanBeInfected())
            {
                target.RpcSetCustomRole(CustomRoles.Contagious);
                killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Contagious), GetString("AdmiredPlayer")));
                target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Contagious), GetString("AdmirerAdmired")));
                Utils.NotifyRoles();
            }
            killer.ResetKillCooldown();
            killer.SetKillCooldown();
            if (!DisableShieldAnimations.GetBool()) 
                killer.RpcGuardAndKill(target);
            target.RpcGuardAndKill(killer);
            target.RpcGuardAndKill(target);
            Logger.Info("设置职业:" + target?.Data?.PlayerName + " = " + target.GetCustomRole().ToString() + " + " + CustomRoles.Admirer.ToString(), "Assign " + CustomRoles.Admirer.ToString());
            Logger.Info($"{killer.GetNameWithRole()} : 剩余{AdmirerLimit[killer.PlayerId]}次仰慕机会", "Admirer");
            return;
        }
        killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Admirer), GetString("AdmirerInvalidTarget")));
        Logger.Info($"{killer.GetNameWithRole()} : 剩余{AdmirerLimit[killer.PlayerId]}次仰慕机会", "Admirer");
        return;
    }
    public static bool KnowRole(PlayerControl player, PlayerControl target)
    {
        if (!KnowTargetRole.GetBool()) return false;
        if (AdmiredList.ContainsKey(player.PlayerId))
        {
            if (AdmiredList[player.PlayerId].Contains(target.PlayerId)) return true;
            return false;
        }
        else if (AdmiredList.ContainsKey(target.PlayerId))
        {
            if (AdmiredList[target.PlayerId].Contains(player.PlayerId)) return true;
            return false;
        }
        else return false;
    }
    public static string GetAdmireLimit(byte playerId) => Utils.ColorString(AdmirerLimit[playerId] >= 1 ? Utils.GetRoleColor(CustomRoles.Admirer).ShadeColor(0.25f) : Color.gray, $"({AdmirerLimit[playerId]})");
    public static bool CanBeAdmired(this PlayerControl pc, PlayerControl admirer)
    {
        if (AdmiredList[admirer.PlayerId].Contains(pc.PlayerId)) return false;

        return pc != null && (pc.GetCustomRole().IsCrewmate() || pc.GetCustomRole().IsImpostor() || pc.GetCustomRole().IsNeutral()) 
            && !pc.Is(CustomRoles.Soulless) && !pc.Is(CustomRoles.Lovers) && !pc.Is(CustomRoles.Loyal) 
            && !((pc.Is(CustomRoles.NiceMini) || pc.Is(CustomRoles.EvilMini)) && Mini.Age < 18)
            && !(pc.GetCustomSubRoles().Contains(CustomRoles.Hurried) && !Hurried.CanBeConverted.GetBool());
    }
}
