using Hazel;
using System.Collections.Generic;
using TOHE.Roles.Neutral;
using UnityEngine;
using static TOHE.Translator;

namespace TOHE.Roles.Impostor;

public static class RiftMaker
{
    private static readonly int Id = 26800;
    //private static List<byte> playerIdList = new();
    public static bool IsEnable = false;

    public static OptionItem SSCooldown;
    public static OptionItem KillCooldown;
    public static OptionItem TPCooldownOpt;

    public static Dictionary<byte, List<Vector2>> MarkedLocation = new();
    public static Dictionary<byte, long> LastTP = new();
    private static float TPCooldown = new();
    public static void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.RiftMaker);
        KillCooldown = FloatOptionItem.Create(Id + 10, "KillCooldown", new(0f, 180f, 2.5f), 20f, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.RiftMaker])
            .SetValueFormat(OptionFormat.Seconds);
        SSCooldown = FloatOptionItem.Create(Id + 11, "ShapeshiftCooldown", new(0f, 180f, 2.5f), 25f, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.RiftMaker])
            .SetValueFormat(OptionFormat.Seconds);
        TPCooldownOpt = FloatOptionItem.Create(Id + 11, "TPCooldown", new(5f, 25f, 2.5f), 5f, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.RiftMaker])
            .SetValueFormat(OptionFormat.Seconds);
    }

    public static void Init()
    {
        IsEnable = false;
        MarkedLocation = new();
        LastTP = new();
        TPCooldown = new();
    }
    public static void Add(byte playerId)
    {
        IsEnable = true;
        MarkedLocation[playerId] = new();
        var now = Utils.GetTimeStamp();
        LastTP[playerId] = now;
        TPCooldown = TPCooldownOpt.GetFloat();
    }

    public static void SendRPC(byte riftID, int operate)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.RiftMakerSyncData, SendOption.Reliable, -1);
        writer.Write(operate);
        if (operate == 0)
        {
            writer.Write(riftID);
            int length = MarkedLocation[riftID].Count;
            writer.Write(MarkedLocation[riftID][length - 1].x); //x coordinate
            writer.Write(MarkedLocation[riftID][length - 1].y); //y coordinate

            writer.Write(LastTP[riftID].ToString());
        }
        if (operate == 0)
        {
            writer.Write(riftID);
        }
    }
    public static void RecieveRPC(MessageReader reader)
    {
        int operate = reader.ReadInt32();
        if (operate == 0)
        {
            byte riftID = reader.ReadByte();
            float xLoc = reader.ReadSingle();
            float yLoc = reader.ReadSingle();
            if (!MarkedLocation.ContainsKey(riftID)) MarkedLocation[riftID] = new();
            if (MarkedLocation[riftID].Count >= 2) MarkedLocation[riftID].RemoveAt(0);
            MarkedLocation[riftID].Add(new Vector2(xLoc, yLoc));

            string stimeStamp = reader.ReadString();
            if (long.TryParse(stimeStamp, out long timeStamp)) LastTP[riftID] = timeStamp;
        }
        if (operate == 1)
        {
            byte riftID = reader.ReadByte();
            if (MarkedLocation.ContainsKey(riftID)) MarkedLocation[riftID].Clear();
        }
    }

    public static void ApplyGameOptions()
    {
        AURoleOptions.ShapeshifterCooldown = SSCooldown.GetFloat();
        AURoleOptions.ShapeshifterDuration = 1f;
    }
    public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();

    public static void OnShapeshift(PlayerControl pc, bool IsShapeshifting)
    {
        if (!IsEnable || !pc.IsAlive() || !IsShapeshifting) return;
        if (!pc.Is(CustomRoles.RiftMaker)) return;

        if (!MarkedLocation.ContainsKey(pc.PlayerId)) MarkedLocation[pc.PlayerId] = new();

        var currentPos = pc.GetCustomPosition();
        var totalMarked = MarkedLocation[pc.PlayerId].Count;
        if (totalMarked == 1 && Vector2.Distance(currentPos, MarkedLocation[pc.PlayerId][0]) <= 4f)
        {
            pc.Notify(GetString("IncorrectMarks"));
            return;
        }
        else if (totalMarked == 2 && Vector2.Distance(currentPos, MarkedLocation[pc.PlayerId][1]) <= 4f)
        {
            pc.Notify(GetString("IncorrectMarks"));
            return;
        }

        if (totalMarked >= 2) MarkedLocation[pc.PlayerId].RemoveAt(0);

        MarkedLocation[pc.PlayerId].Add(pc.GetCustomPosition());
        if (MarkedLocation[pc.PlayerId].Count == 2) LastTP[pc.PlayerId] = Utils.GetTimeStamp();
        pc.Notify(GetString("MarkDone"));

        SendRPC(pc.PlayerId, 0);
        //sendrpc for marked location and lasttp
    }

    public static void OnVent(PlayerControl pc, int ventId)
    {
        if (!IsEnable || pc == null) return;
        if (!pc.Is(CustomRoles.RiftMaker)) return;

        MarkedLocation[pc.PlayerId].Clear();
        //send rpc for clearing markedlocation
        SendRPC(pc.PlayerId, 1);
        pc.Notify(GetString("MarksCleared"));

        _ = new LateTask(() =>
        {
            pc.MyPhysics?.RpcBootFromVent(ventId);
        }, 0.5f, "RiftMakerOnVent");

    }


    public static void OnFixedUpdate(PlayerControl player)
    {
        if (!GameStates.IsInTask) return;
        if (player == null) return;
        if (Pelican.IsEaten(player.PlayerId) || !player.IsAlive()) return;
        if (!player.Is(CustomRoles.RiftMaker)) return;
        byte playerId = player.PlayerId;
        if (!MarkedLocation.ContainsKey(playerId)) MarkedLocation[playerId] = new();
        if (MarkedLocation[playerId].Count != 2) return;

        if (Vector2.Distance(MarkedLocation[playerId][0], MarkedLocation[playerId][1]) <= 4f)
        {
            player.Notify(GetString("IncorrectMarks"));
            MarkedLocation[playerId].RemoveAt(1);
            return;
        }
        var now = Utils.GetTimeStamp();
        if (!LastTP.ContainsKey(playerId)) LastTP[playerId] = now;
        if (LastTP[playerId] + TPCooldown > now) return;

        Vector2 position = player.GetCustomPosition();
        Vector2 TPto = MarkedLocation[playerId][0];

        if (position == MarkedLocation[playerId][0])
        {
            TPto = MarkedLocation[playerId][1];
        }
        else if (position == MarkedLocation[playerId][1])
        {
            TPto = MarkedLocation[playerId][0];
        }
        else return;

        LastTP[playerId] = now;
        player.RpcTeleport(TPto);
        //SENDRPC
        return;
    }
}
