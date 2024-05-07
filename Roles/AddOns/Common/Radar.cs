using Hazel;
using UnityEngine;
using static TOHE.Options;


namespace TOHE.Roles.AddOns.Common;

public static class Radar
{
    private const int Id = 28200;
    public static bool IsEnable = false;

    public static OptionItem ImpCanBeRadar;
    public static OptionItem CrewCanBeRadar;
    public static OptionItem NeutralCanBeRadar;

    private static Dictionary<byte, byte> ClosestPlayer = [];

    public static void SetupCustomOptions()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Radar, canSetNum: true, tab: TabGroup.Addons);
        ImpCanBeRadar = BooleanOptionItem.Create(Id + 10, "ImpCanBeRadar", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Radar]);
        CrewCanBeRadar = BooleanOptionItem.Create(Id + 11, "CrewCanBeRadar", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Radar]);
        NeutralCanBeRadar = BooleanOptionItem.Create(Id + 12, "NeutralCanBeRadar", true, TabGroup.Addons, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Radar]);
    }

    public static void Init()
    {
        ClosestPlayer.Clear();
        IsEnable = false;
    }
    public static void Add(byte playerId)
    {
        ClosestPlayer[playerId] = byte.MaxValue;
        IsEnable = true;
    }
    public static void Remove(byte playerId)
    {
        ClosestPlayer.Remove(playerId);
    }

    private static void SendRPC(byte playerId, byte previousClosest)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetRadarArrow, SendOption.Reliable, -1);
        writer.Write(playerId);
        writer.Write(previousClosest);
        writer.Write(ClosestPlayer[playerId]);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }

    public static void ReceiveRPC(MessageReader reader)
    {
        byte radarId = reader.ReadByte();
        byte previousClosest = reader.ReadByte();
        TargetArrow.Remove(radarId, previousClosest);
        byte closest = reader.ReadByte();
        TargetArrow.Add(radarId, closest);
        ClosestPlayer[radarId] = closest;
    }

    public static void OnFixedUpdate(PlayerControl radarPC)
    {
        if (radarPC == null || !radarPC.Is(CustomRoles.Radar) || !GameStates.IsInTask) return;
        if (Main.AllAlivePlayerControls.Length <= 1) return;

        if (!ClosestPlayer.ContainsKey(radarPC.PlayerId)) ClosestPlayer[radarPC.PlayerId] = byte.MaxValue;
        byte previousClosest = ClosestPlayer[radarPC.PlayerId];

        byte closestPlayerId = byte.MaxValue;
        float closestDistance = Mathf.Infinity;

        foreach (PlayerControl pc in Main.AllAlivePlayerControls)
        {
            if (pc == radarPC)
                continue;

            float distance = Vector2.Distance(radarPC.GetCustomPosition(), pc.GetCustomPosition());

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestPlayerId = pc.PlayerId;
            }
        }

        if (closestPlayerId == previousClosest) return;

        TargetArrow.Remove(radarPC.PlayerId, previousClosest);
        if (closestPlayerId == byte.MaxValue) return;
        ClosestPlayer[radarPC.PlayerId] = closestPlayerId;
        TargetArrow.Add(radarPC.PlayerId, closestPlayerId);
        SendRPC(radarPC.PlayerId, previousClosest);
        Logger.Info($"Radar: {radarPC.PlayerId} Target: {closestPlayerId}", "Radar Target");
    }
    public static string GetPlayerArrow(PlayerControl seer, PlayerControl target = null, bool isForMeeting = false)
    {
        if (isForMeeting || seer == null) return string.Empty;
        if (!seer.Is(CustomRoles.Radar) || !ClosestPlayer.ContainsKey(seer.PlayerId)) return string.Empty;
        if (target != null && seer.PlayerId != target.PlayerId) return string.Empty;

        string arrow = Utils.ColorString(Utils.GetRoleColor(CustomRoles.Radar), TargetArrow.GetArrows(seer, ClosestPlayer[seer.PlayerId]));
        return arrow;
    }
}

