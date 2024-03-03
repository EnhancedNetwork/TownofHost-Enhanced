using Hazel;
using System.Collections.Generic;
using System.Linq;
using TOHE.Roles.Core;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.CheckForEndVotingPatch;

namespace TOHE.Roles.Crewmate;
internal class Cleanser : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 6600;
    private static bool On = false;
    public override bool IsEnable => On;
    public static bool HasEnabled => CustomRoles.Cleanser.IsClassEnable();
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;

    //==================================================================\\

    public static Dictionary<byte,byte> CleanserTarget = [];
    public static Dictionary<byte, int> CleanserUses = [];
    public static List<byte> CleansedPlayers = [];

    public static List<byte> playerIdList = [];
    public static Dictionary<byte, bool> DidVote = [];

    public static OptionItem CleanserUsesOpt;
    public static OptionItem CleansedCanGetAddon;
    public static OptionItem HidesVote;
    public static OptionItem AbilityUseGainWithEachTaskCompleted;

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Cleanser);
        CleanserUsesOpt = IntegerOptionItem.Create(Id + 10, "MaxCleanserUses", new(1, 14, 1), 3, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Cleanser])
            .SetValueFormat(OptionFormat.Times);
        CleansedCanGetAddon = BooleanOptionItem.Create(Id + 11, "CleansedCanGetAddon", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Cleanser]);
        HidesVote = BooleanOptionItem.Create(Id + 12, "CleanserHideVote", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Cleanser]);
    //    AbilityUseGainWithEachTaskCompleted = IntegerOptionItem.Create(Id + 12, "AbilityUseGainWithEachTaskCompleted", new(0, 5, 1), 1, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Cleanser])
    //        .SetValueFormat(OptionFormat.Times);

    }
    public override void Init()
    {
        playerIdList = [];
        CleanserTarget = [];
        CleanserUses = [];
        CleansedPlayers = [];
        DidVote = [];
        On = false;
    }

    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        CleanserTarget.Add(playerId, byte.MaxValue);
        CleanserUses.Add(playerId, 0);
        DidVote.Add(playerId, false);
        On = true;
    }
    public override void Remove(byte playerId)
    {
        playerIdList.Remove(playerId);
        CleanserTarget.Remove(playerId);
        CleanserUses.Remove(playerId);
        DidVote.Remove(playerId);
    }
    public override bool HideVote(PlayerVoteArea ps) => CheckRole(ps.TargetPlayerId, CustomRoles.Cleanser) && Cleanser.HidesVote.GetBool() && Cleanser.CleanserUses[ps.TargetPlayerId] > 0;
    public override string GetProgressText(byte playerId, bool comms)
    {
        if (!CleanserUses.ContainsKey(playerId)) return "Invalid";
        Color x;
        if (CleanserUsesOpt.GetInt() - CleanserUses[playerId] > 0)
            x = Utils.GetRoleColor(CustomRoles.Cleanser);
        else x = Color.gray;
        return (Utils.ColorString(x, $"({CleanserUsesOpt.GetInt() - CleanserUses[playerId]})"));
    }
    private static void SendRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, -1);
        writer.WritePacked((int)CustomRoles.Cleanser);
        writer.Write(playerId);
        writer.Write(CleanserUses[playerId]);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }

    public static void ReceiveRPC(MessageReader reader)
    {
        byte CleanserId = reader.ReadByte();
        int Limit = reader.ReadInt32();
        if (CleanserUses.ContainsKey(CleanserId))
            CleanserUses[CleanserId] = Limit;
        else
            CleanserUses.Add(CleanserId, 0);
    }

    public override void OnVote(PlayerControl voter, PlayerControl target)
    {
        if (!voter.Is(CustomRoles.Cleanser)) return;
        if (DidVote[voter.PlayerId]) return;
        DidVote[voter.PlayerId] = true;
        if (CleanserUses[voter.PlayerId] >= CleanserUsesOpt.GetInt()) return;
        if (target.PlayerId == voter.PlayerId)
        {
            Utils.SendMessage(GetString("CleanserRemoveSelf"), voter.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Cleanser), GetString("CleanserTitle")));
            return;
        }
        if (target.Is(CustomRoles.Stubborn))
        {
            Utils.SendMessage(GetString("CleanserCantRemove"), voter.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Cleanser), GetString("CleanserTitle")));
            return;
        }
        if (CleanserTarget[voter.PlayerId] != byte.MaxValue) return;

        CleanserUses[voter.PlayerId]++;
        CleanserTarget[voter.PlayerId] = target.PlayerId;
        Logger.Info($"{voter.GetNameWithRole()} cleansed {target.GetNameWithRole()}", "Cleansed");
        CleansedPlayers.Add(target.PlayerId);
        Utils.SendMessage(string.Format(GetString("CleanserRemovedRole"), target.GetRealName()), voter.PlayerId, title: Utils.ColorString(Utils.GetRoleColor(CustomRoles.Cleanser),GetString("CleanserTitle")));
        SendRPC(voter.PlayerId);
    }
    public override void OnReportDeadBody(PlayerControl baba, PlayerControl lilelam)
    {
        foreach (var pid in CleanserTarget.Keys.ToArray())
        {
            CleanserTarget[pid] = byte.MaxValue;
        }
    }
    public static void AfterMeetingTasks(bool notifyPlayer = false)
    {
        if (notifyPlayer) 
        {
            foreach (var pid in CleanserTarget.Keys.ToArray())
            {
                var targetid = CleanserTarget[pid];
                if (targetid == byte.MaxValue) continue;
                var targetpc = Utils.GetPlayerById(targetid);
                if (targetpc == null) continue;

                targetpc.Notify(GetString("LostAddonByCleanser"));
            }
        }
        else
        {
            foreach (var pid in CleanserTarget.Keys.ToArray())
            {
                DidVote[pid] = false;
                if (pid == byte.MaxValue) continue;
                var targetid = CleanserTarget[pid];
                if (targetid == byte.MaxValue) continue;
                var targetpc = Utils.GetPlayerById(targetid);
                if (targetpc == null) continue;
                //var allAddons = targetpc.GetCustomSubRoles();
                targetpc.RpcSetCustomRole(CustomRoles.Cleansed);
                Logger.Info($"Removed all the add ons of {targetpc.GetNameWithRole()}", "Cleanser");
                //foreach (var role in allAddons)
                //{
                //    Main.PlayerStates[targetid].RemoveSubRole(role);

                //}
            }
            Utils.MarkEveryoneDirtySettings();
        }
    }
}