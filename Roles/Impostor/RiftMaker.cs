using AmongUs.GameOptions;
using Hazel;
using TOHE.Roles.Neutral;
using UnityEngine;
using static TOHE.Translator;

namespace TOHE.Roles.Impostor;

internal class RiftMaker : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 27200;
    private static readonly HashSet<byte> Playerids = [];
    public static bool HasEnabled => Playerids.Any();
    
    public override CustomRoles ThisRoleBase => CustomRoles.Shapeshifter;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorConcealing;
    //==================================================================\\

    private static OptionItem SSCooldown;
    private static OptionItem KillCooldown;
    private static OptionItem TPCooldownOpt;
    private static OptionItem RiftRadius;
    private static OptionItem ShowShapeshiftAnimationsOpt;

    private static readonly Dictionary<byte, List<Vector2>> MarkedLocation = [];
    private static readonly Dictionary<byte, long> LastTP = [];
    private static float TPCooldown = new();

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.RiftMaker);
        KillCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 20f, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.RiftMaker])
            .SetValueFormat(OptionFormat.Seconds);
        SSCooldown = FloatOptionItem.Create(Id + 11, GeneralOption.ShapeshifterBase_ShapeshiftCooldown, new(0f, 180f, 2.5f), 25f, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.RiftMaker])
            .SetValueFormat(OptionFormat.Seconds);
        TPCooldownOpt = FloatOptionItem.Create(Id + 12, "TPCooldown", new(5f, 25f, 2.5f), 5f, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.RiftMaker])
            .SetValueFormat(OptionFormat.Seconds);
        RiftRadius = FloatOptionItem.Create(Id + 13, "RiftRadius", new(0.5f, 2f, 0.5f), 1f, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.RiftMaker])
            .SetValueFormat(OptionFormat.Multiplier);
        ShowShapeshiftAnimationsOpt = BooleanOptionItem.Create(Id + 14, GeneralOption.ShowShapeshiftAnimations, true, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.RiftMaker]);
    }

    public override void Init()
    {
        Playerids.Clear();
        MarkedLocation.Clear();
        LastTP.Clear();
        TPCooldown = new();
    }
    public override void Add(byte playerId)
    {
        MarkedLocation[playerId] = [];
        var now = Utils.GetTimeStamp();
        LastTP[playerId] = now;

        TPCooldown = TPCooldownOpt.GetFloat();
        Playerids.Add(playerId);
    }

    private static void SendRPC(byte riftID, int operate)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.RiftMakerSyncData, SendOption.Reliable, -1);
        writer.Write(operate);
        if (operate == 3)
        {
            AmongUsClient.Instance.FinishRpcImmediately(writer);
            return;
        }
        writer.Write(riftID);

        if (operate == 0) //sync markedloaction and last tp
        {
            int length = MarkedLocation[riftID].Count;
            writer.Write(MarkedLocation[riftID][length - 1].x); //x coordinate
            writer.Write(MarkedLocation[riftID][length - 1].y); //y coordinate

            writer.Write(LastTP[riftID].ToString());
        }
        else if (operate == 2) //sync last tp
        {
            writer.Write(LastTP[riftID].ToString());
        }
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        int operate = reader.ReadInt32();
        if (operate == 3)
        {
            var now = Utils.GetTimeStamp();
            foreach (byte pID in LastTP.Keys.ToArray())
            {
                LastTP[pID] = now;
            }
            return;
        }
        byte riftID = reader.ReadByte();
        if (operate == 0) //sync  marked location and last tp
        {
            float xLoc = reader.ReadSingle();
            float yLoc = reader.ReadSingle();
            if (!MarkedLocation.ContainsKey(riftID)) MarkedLocation[riftID] = [];
            if (MarkedLocation[riftID].Count >= 2) MarkedLocation[riftID].RemoveAt(0);
            MarkedLocation[riftID].Add(new Vector2(xLoc, yLoc));

            string stimeStamp = reader.ReadString();
            if (long.TryParse(stimeStamp, out long timeStamp)) LastTP[riftID] = timeStamp;
        }
        else if (operate == 1) //clear marked location
        {
            if (MarkedLocation.ContainsKey(riftID)) MarkedLocation[riftID].Clear();
        }
        else if (operate == 2) //sync last tp
        {
            string stimeStamp = reader.ReadString();
            if (long.TryParse(stimeStamp, out long timeStamp)) LastTP[riftID] = timeStamp;
        }
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.ShapeshifterCooldown = SSCooldown.GetFloat();
        AURoleOptions.ShapeshifterLeaveSkin = true;
        AURoleOptions.ShapeshifterDuration = 1f;
    }
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();

    public override bool OnCheckShapeshift(PlayerControl shapeshifter, PlayerControl target, ref bool resetCooldown, ref bool shouldAnimate)
    {
        // Unshift
        if (shapeshifter.PlayerId == target.PlayerId)
        {
            // No animate unshift
            if (shouldAnimate)
            {
                shouldAnimate = false;
            }
            return true;
        }

        // Always do animation shapeshift
        if (ShowShapeshiftAnimationsOpt.GetBool()) return true;


        DoRifts(shapeshifter, target);
        return false;
    }
    public override void OnShapeshift(PlayerControl shapeshifter, PlayerControl target, bool IsAnimate, bool shapeshifting)
    {
        if (!shapeshifting) return;

        DoRifts(shapeshifter, target);
    }

    private static void DoRifts(PlayerControl shapeshifter, PlayerControl target)
    {
        var shapeshifterId = shapeshifter.PlayerId;
        if (!MarkedLocation.ContainsKey(shapeshifterId)) MarkedLocation[shapeshifterId] = [];

        var currentPos = shapeshifter.GetCustomPosition();
        var totalMarked = MarkedLocation[shapeshifterId].Count;
        if (totalMarked == 1 && Utils.GetDistance(currentPos, MarkedLocation[shapeshifterId][0]) <= 5f)
        {
            shapeshifter.Notify(GetString("RiftsTooClose"));
            return;
        }
        else if (totalMarked == 2 && Utils.GetDistance(currentPos, MarkedLocation[shapeshifterId][1]) <= 5f)
        {
            shapeshifter.Notify(GetString("RiftsTooClose"));
            return;
        }

        if (totalMarked >= 2) MarkedLocation[shapeshifterId].RemoveAt(0);

        MarkedLocation[shapeshifterId].Add(shapeshifter.GetCustomPosition());
        if (MarkedLocation[shapeshifterId].Count == 2) LastTP[shapeshifterId] = Utils.GetTimeStamp();
        shapeshifter.Notify(GetString("RiftCreated"));

        SendRPC(shapeshifterId, 0);
        //sendrpc for marked location and lasttp
    }

    public override void OnCoEnterVent(PlayerPhysics physics, int ventId)
    {
        var player = physics.myPlayer;
        if (player == null) return;

        _ = new LateTask(() =>
        {
            physics?.RpcBootFromVent(ventId);

            MarkedLocation[player.PlayerId].Clear();
            //send rpc for clearing markedlocation
            SendRPC(player.PlayerId, 1);
            player.Notify(GetString("RiftsDestroyed"));

        }, 0.5f, "RiftMakerOnVent");
    }

    public override void OnFixedUpdate(PlayerControl player, bool lowLoad, long nowTime)
    {
        byte playerId = player.PlayerId;
        if (lowLoad || Pelican.IsEaten(playerId) || !player.IsAlive()) return;
        if (!MarkedLocation.TryGetValue(playerId, out var locationList)) return;

        if (locationList.Count != 2) return;

        if (!LastTP.ContainsKey(playerId)) LastTP[playerId] = nowTime;
        if (nowTime - LastTP[playerId] <= TPCooldown) return;

        Vector2 position = player.GetCustomPosition();
        Vector2 TPto;

        if (Utils.GetDistance(position, locationList[0]) <= RiftRadius.GetFloat())
        {
            TPto = locationList[1];
        }
        else if (Utils.GetDistance(position, locationList[1]) <= RiftRadius.GetFloat())
        {
            TPto = locationList[0];
        }
        else return;

        LastTP[playerId] = nowTime;
        //SENDRPC
        SendRPC(playerId, 2);
        player.RpcTeleport(TPto);
        return;
    }

    public override void AfterMeetingTasks()
    {
        var now = Utils.GetTimeStamp();
        foreach (byte riftID in LastTP.Keys.ToArray())
        {
            LastTP[riftID] = now;
        }
        SendRPC(byte.MaxValue, 3);
    }
}
