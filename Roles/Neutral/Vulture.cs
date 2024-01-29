using AmongUs.GameOptions;
using Hazel;
using System.Collections.Generic;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Neutral;

public static class Vulture
{
    private static readonly int Id = 15600;
    private static List<byte> playerIdList = [];
    public static bool IsEnable = false;

    public static List<byte> UnreportablePlayers = [];
    public static Dictionary<byte, int> BodyReportCount = [];
    public static Dictionary<byte, int> AbilityLeftInRound = [];
    public static Dictionary<byte, long> LastReport = [];

    public static OptionItem ArrowsPointingToDeadBody;
    public static OptionItem NumberOfReportsToWin;
    public static OptionItem CanVent;
    public static OptionItem VultureReportCD;
    public static OptionItem MaxEaten;
    public static OptionItem HasImpVision;

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Vulture);
        ArrowsPointingToDeadBody = BooleanOptionItem.Create(Id + 10, "VultureArrowsPointingToDeadBody", true, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Vulture]);
        NumberOfReportsToWin = IntegerOptionItem.Create(Id + 11, "VultureNumberOfReportsToWin", new(1, 14, 1), 5, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Vulture]);
        CanVent = BooleanOptionItem.Create(Id + 12, "CanVent", true, TabGroup.NeutralRoles, true).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Vulture]);
        VultureReportCD = FloatOptionItem.Create(Id + 13, "VultureReportCooldown", new(0f, 180f, 2.5f), 10f, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Vulture])
                .SetValueFormat(OptionFormat.Seconds);
        MaxEaten = IntegerOptionItem.Create(Id + 14, "VultureMaxEatenInOneRound", new(1, 14, 1), 1, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Vulture]);
        HasImpVision = BooleanOptionItem.Create(Id + 15, "ImpostorVision", true, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Vulture]);
    }
    public static void Init()
    {
        playerIdList = [];
        UnreportablePlayers = [];
        BodyReportCount = [];
        AbilityLeftInRound = [];
        LastReport = [];
        IsEnable = false;
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        IsEnable = true;
        BodyReportCount[playerId] = 0;
        AbilityLeftInRound[playerId] = MaxEaten.GetInt();
        LastReport[playerId] = Utils.GetTimeStamp();
        _ = new LateTask(() =>
        {
            if (GameStates.IsInTask)
            {
                if (!DisableShieldAnimations.GetBool()) Utils.GetPlayerById(playerId).RpcGuardAndKill(Utils.GetPlayerById(playerId));
                Utils.GetPlayerById(playerId).Notify(GetString("VultureCooldownUp"));
            }
            return;
        }, VultureReportCD.GetFloat() + 8f, "Vulture Cooldown Up In Start");  //for some reason that idk vulture cd completes 8s faster when the game starts, so I added 8f for now 
    }

    public static void ApplyGameOptions(IGameOptions opt) => opt.SetVision(HasImpVision.GetBool());

    private static void SendRPC(byte playerId, bool add, Vector3 loc = new())
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetVultureArrow, SendOption.Reliable, -1);
        writer.Write(playerId);
        writer.Write(add);
        if (add)
        {
            writer.Write(loc.x);
            writer.Write(loc.y);
            writer.Write(loc.z);
        }
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }

    public static void ReceiveRPC(MessageReader reader)
    {
        byte playerId = reader.ReadByte();
        bool add = reader.ReadBoolean();
        if (add)
            LocateArrow.Add(playerId, new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()));
        else
            LocateArrow.RemoveAllTarget(playerId);
    }

    public static void Clear()
    {
        foreach (var apc in playerIdList)
        {
            LocateArrow.RemoveAllTarget(apc);
            SendRPC(apc, false);
        }
    }
    public static void AfterMeetingTasks(bool notifyPlayer = false)
    {
        if (notifyPlayer) 
        {
            foreach (var apc in playerIdList)
            {
                var player = Utils.GetPlayerById(apc);
                if (player.IsAlive())
                {
                    _ = new LateTask(() =>
                    {
                        if (GameStates.IsInTask)
                        {
                            if (!DisableShieldAnimations.GetBool()) Utils.GetPlayerById(apc).RpcGuardAndKill(Utils.GetPlayerById(apc));
                            Utils.GetPlayerById(apc).Notify(GetString("VultureCooldownUp"));
                        }
                        return;
                    }, VultureReportCD.GetFloat(), "Vulture Cooldown Up After Meeting");
                }
            }
        }
        else
        {
            foreach (var apc in playerIdList)
            {
                var player = Utils.GetPlayerById(apc);
                if (player.IsAlive())
                {
                    AbilityLeftInRound[apc] = MaxEaten.GetInt();
                    LastReport[apc] = Utils.GetTimeStamp();
                    SendRPC(apc, false);
                }
            }
        }
    }

    public static void OnPlayerDead(PlayerControl target)
    {
        if (!ArrowsPointingToDeadBody.GetBool()) return;

        Vector2 pos = target.transform.position;
        float minDis = float.MaxValue;

        foreach (var pc in Main.AllAlivePlayerControls)
        {
            if (pc.PlayerId == target.PlayerId) continue;
            var dis = Vector2.Distance(pc.transform.position, pos);
            if (dis < minDis && dis < 1.5f)
            {
                minDis = dis;
            }
        }

        foreach (var pc in playerIdList.ToArray())
        {
            var player = Utils.GetPlayerById(pc);
            if (player == null || !player.IsAlive()) continue;
            LocateArrow.Add(pc, target.transform.position);
            SendRPC(pc, true, target.transform.position);
        }
    }

    public static void OnReportDeadBody(PlayerControl pc, GameData.PlayerInfo target)
    {
        BodyReportCount[pc.PlayerId]++;
        AbilityLeftInRound[pc.PlayerId]--;
        Logger.Msg($"target.object {target.Object}, is null? {target.Object == null}","VultureNull");
        if (target.Object != null)
        {
            foreach (var apc in playerIdList)
            {
                LocateArrow.Remove(apc, target.Object.transform.position);
                SendRPC(apc, false);
            }
        }

        pc.Notify(GetString("VultureBodyReported"));
        UnreportablePlayers.Remove(target.PlayerId);
        UnreportablePlayers.Add(target.PlayerId);
        //playerIdList.Remove(target.PlayerId);
    }

    public static string GetTargetArrow(PlayerControl seer, PlayerControl target = null)
    {
        if (seer == null) return string.Empty;
        if (!seer.Is(CustomRoles.Vulture)) return string.Empty;
        if (target != null && seer.PlayerId != target.PlayerId) return string.Empty;
        if (GameStates.IsMeeting) return string.Empty;
        return Utils.ColorString(Color.white, LocateArrow.GetArrows(seer));
    }
}