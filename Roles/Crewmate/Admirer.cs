using Hazel;
using System.Collections.Generic;
using TOHE.Roles.AddOns.Crewmate;
using TOHE.Roles.AddOns.Impostor;
using TOHE.Roles.Double;
using TOHE.Roles.Neutral;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate;

internal class Admirer : RoleBase
{
    private const int Id = 24800;
    private static List<byte> playerIdList = [];
    public static bool On;
    public override bool IsEnable => On;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;

    public static OptionItem AdmireCooldown;
    public static OptionItem KnowTargetRole;
    public static OptionItem SkillLimit;
    public static Dictionary<byte, int> AdmirerLimit;
    public static Dictionary<byte, List<byte>> AdmiredList;

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Admirer);
        AdmireCooldown = FloatOptionItem.Create(Id + 10, "AdmireCooldown", new(1f, 180f, 1f), 5f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Admirer])
            .SetValueFormat(OptionFormat.Seconds);
        KnowTargetRole = BooleanOptionItem.Create(Id + 11, "AdmirerKnowTargetRole", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Admirer]);
        SkillLimit = IntegerOptionItem.Create(Id + 12, "AdmirerSkillLimit", new(0, 100, 1), 1, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Admirer])
            .SetValueFormat(OptionFormat.Times);
    }
    public override void Init()
    {
        playerIdList = [];
        AdmirerLimit = [];
        AdmiredList = [];
        On = false;
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        AdmirerLimit.Add(playerId, SkillLimit.GetInt());
        AdmiredList.Add(playerId, []);
        On = true;

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }
    public static void Remove(byte playerId)
    {
        playerIdList.Remove(playerId);
        AdmirerLimit.Remove(playerId);
        AdmiredList.Remove(playerId);
    }

    private static void SendRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, -1);
        writer.WritePacked((int)CustomRoles.Admirer);
        writer.Write(playerId);
        writer.Write(AdmirerLimit[playerId]);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }

    public static void SendRPC(byte playerId, byte targetId)
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
                AdmiredList.Add(playerId, []);
            else AdmiredList[playerId].Add(targetId);
        }
    }
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = AdmirerLimit[id] >= 1 ? AdmireCooldown.GetFloat() : 300f;
    public override bool CanUseKillButton(PlayerControl player) => !player.Data.IsDead && (AdmirerLimit.TryGetValue(player.PlayerId, out var x) ? x : 1) >= 1;
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (AdmirerLimit[killer.PlayerId] < 1) return false;
        if (Mini.Age < 18 && (target.Is(CustomRoles.NiceMini) || target.Is(CustomRoles.EvilMini)))
        {
            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Succubus), GetString("CantRecruit")));
            return false;
        }
        if (!AdmirerLimit.ContainsKey(killer.PlayerId))
            Add(killer.PlayerId);
        if (!AdmiredList.ContainsKey(killer.PlayerId))
            AdmiredList.Add(killer.PlayerId, []);

        if (AdmirerLimit[killer.PlayerId] < 1) return false;
        if (CanBeAdmired(target, killer))
        {
            if (KnowTargetRole.GetBool())
            {
                AdmiredList[killer.PlayerId].Add(target.PlayerId);
                SendRPC(killer.PlayerId, target.PlayerId); //Sync playerId list
            }

            if (!killer.Is(CustomRoles.Madmate) && !killer.Is(CustomRoles.Recruit) && !killer.Is(CustomRoles.Charmed)
                && !killer.Is(CustomRoles.Infected) && !killer.Is(CustomRoles.Contagious))
            {
                Logger.Info("Set converted: " + target.GetNameWithRole().RemoveHtmlTags() + " to " + CustomRoles.Admired.ToString(), "Admirer Assign");
                target.RpcSetCustomRole(CustomRoles.Admired);
                killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Admirer), GetString("AdmiredPlayer")));
                target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Admirer), GetString("AdmirerAdmired")));
            }
            else if (killer.Is(CustomRoles.Madmate) && target.CanBeMadmate(inGame: true, forGangster: true))
            {
                Logger.Info("Set converted: " + target.GetNameWithRole().RemoveHtmlTags() + " to " + CustomRoles.Madmate.ToString(), "Admirer Assign");
                target.RpcSetCustomRole(CustomRoles.Madmate);
                killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Madmate), GetString("AdmiredPlayer")));
                target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Madmate), GetString("AdmirerAdmired")));
            }
            else if (killer.Is(CustomRoles.Recruit) && target.CanBeSidekick())
            {
                Logger.Info("Set converted: " + target.GetNameWithRole().RemoveHtmlTags() + " to " + CustomRoles.Recruit.ToString(), "Admirer Assign");
                target.RpcSetCustomRole(CustomRoles.Recruit);
                killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Recruit), GetString("AdmiredPlayer")));
                target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Recruit), GetString("AdmirerAdmired")));
            }
            else if (killer.Is(CustomRoles.Charmed) && target.CanBeCharmed())
            {
                Logger.Info("Set converted: " + target.GetNameWithRole().RemoveHtmlTags() + " to " + CustomRoles.Charmed.ToString(), "Admirer Assign");
                target.RpcSetCustomRole(CustomRoles.Charmed);
                killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Charmed), GetString("AdmiredPlayer")));
                target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Charmed), GetString("AdmirerAdmired")));
            }
            else if (killer.Is(CustomRoles.Infected) && target.CanBeInfected())
            {
                Logger.Info("Set converted: " + target.GetNameWithRole().RemoveHtmlTags() + " to " + CustomRoles.Infected.ToString(), "Admirer Assign");
                target.RpcSetCustomRole(CustomRoles.Infected);
                killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Infected), GetString("AdmiredPlayer")));
                target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Infected), GetString("AdmirerAdmired")));
            }
            else if (killer.Is(CustomRoles.Contagious) && target.CanBeInfected())
            {
                Logger.Info("Set converted: " + target.GetNameWithRole().RemoveHtmlTags() + " to " + CustomRoles.Contagious.ToString(), "Admirer Assign");
                target.RpcSetCustomRole(CustomRoles.Contagious);
                killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Contagious), GetString("AdmiredPlayer")));
                target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Contagious), GetString("AdmirerAdmired")));
            }
            else goto AdmirerFailed;

            AdmirerLimit[killer.PlayerId]--;
            SendRPC(killer.PlayerId); //Sync skill
            killer.ResetKillCooldown();
            killer.SetKillCooldown();
            if (!DisableShieldAnimations.GetBool())
                killer.RpcGuardAndKill(target);
            target.RpcGuardAndKill(killer);
            target.ResetKillCooldown();
            target.SetKillCooldown(forceAnime: true);
            Logger.Info("设置职业:" + target?.Data?.PlayerName + " = " + target.GetCustomRole().ToString() + " + " + CustomRoles.Admirer.ToString(), "Assign " + CustomRoles.Admirer.ToString());
            Logger.Info($"{killer.GetNameWithRole()} : 剩余{AdmirerLimit[killer.PlayerId]}次仰慕机会", "Admirer");
            return false;
        }
    AdmirerFailed:
        SendRPC(killer.PlayerId); //Sync skill
        killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Admirer), GetString("AdmirerInvalidTarget")));
        Logger.Info($"{killer.GetNameWithRole()} : 剩余{AdmirerLimit[killer.PlayerId]}次仰慕机会", "Admirer");
        return false;
    }
    public bool KnowRole(PlayerControl player, PlayerControl target)
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
        if (AdmiredList.ContainsKey(admirer.PlayerId))
        {
            if (AdmiredList[admirer.PlayerId].Contains(pc.PlayerId))
                return false;
        }
        else AdmiredList.Add(admirer.PlayerId, []);

        return pc != null && (pc.GetCustomRole().IsCrewmate() || pc.GetCustomRole().IsImpostor() || pc.GetCustomRole().IsNeutral())
            && !pc.Is(CustomRoles.Soulless) && !pc.Is(CustomRoles.Lovers) && !pc.Is(CustomRoles.Loyal)
            && !((pc.Is(CustomRoles.NiceMini) || pc.Is(CustomRoles.EvilMini)) && Mini.Age < 18)
            && !(pc.GetCustomSubRoles().Contains(CustomRoles.Hurried) && !Hurried.CanBeConverted.GetBool());
    }
}
