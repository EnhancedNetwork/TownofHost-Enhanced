using AmongUs.GameOptions;
using Hazel;
using InnerNet;
using TOHE.Modules;
using TOHE.Roles.Neutral;
using UnityEngine;
using static TOHE.Translator;

namespace TOHE.Roles.Impostor;

internal class RiftMaker : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.RiftMaker;
    private const int Id = 27200;

    public override CustomRoles ThisRoleBase => CustomRoles.Shapeshifter;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorConcealing;
    //==================================================================\\

    private static OptionItem SSCooldown;
    private static OptionItem KillCooldown;
    private static OptionItem TPCooldownOpt;
    private static OptionItem RiftRadius;

    private readonly Dictionary<Vector2, RiftPortal> MarkedLocation = [];
    private Vector2 Lastadded = Vector2.zero;
    private static readonly Dictionary<byte, long> LastTP = [];
    private static float TPCooldown = new();

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.RiftMaker);
        KillCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 20f, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.RiftMaker])
            .SetValueFormat(OptionFormat.Seconds);
        SSCooldown = FloatOptionItem.Create(Id + 11, GeneralOption.ShapeshifterBase_ShapeshiftCooldown, new(0f, 180f, 2.5f), 25f, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.RiftMaker])
            .SetValueFormat(OptionFormat.Seconds);
        TPCooldownOpt = FloatOptionItem.Create(Id + 12, "TPCooldown", new(2.5f, 25f, 2.5f), 5f, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.RiftMaker])
            .SetValueFormat(OptionFormat.Seconds);
        RiftRadius = FloatOptionItem.Create(Id + 13, "RiftRadius", new(0.5f, 4f, 0.5f), 1f, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.RiftMaker])
            .SetValueFormat(OptionFormat.Multiplier);
    }

    public override void Init()
    {
        MarkedLocation.Clear();
        LastTP.Clear();
        TPCooldown = new();
    }
    public override void Add(byte playerId)
    {
        var now = Utils.GetTimeStamp();
        LastTP[playerId] = now;

        TPCooldown = TPCooldownOpt.GetFloat();
    }

    public override void SetAbilityButtonText(HudManager hud, byte id) => hud.AbilityButton.OverrideText(Translator.GetString("RiftMakerButtonText"));
    // public override Sprite GetAbilityButtonSprite(PlayerControl player, bool shapeshifting) => CustomButton.Get("Create Rift");

    private void SendRPC(byte riftID, int operate)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, -1);
        writer.WriteNetObject(_Player);
        writer.Write(operate);
        if (operate == 3)
        {
            AmongUsClient.Instance.FinishRpcImmediately(writer);
            return;
        }
        writer.Write(riftID);

        if (operate == 0) //sync markedloaction and last tp
        {
            int length = MarkedLocation.Count;
            writer.Write(MarkedLocation.ElementAt(length - 1).Key.x); //x coordinate
            writer.Write(MarkedLocation.ElementAt(length - 1).Key.y); //y coordinate

            writer.Write(LastTP[riftID].ToString());
        }
        else if (operate == 2) //sync last tp
        {
            writer.Write(LastTP[riftID].ToString());
        }
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public override void ReceiveRPC(MessageReader reader, PlayerControl pc)
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
            if (MarkedLocation.Count >= 2) MarkedLocation.Remove(MarkedLocation.ElementAt(0).Key);
            MarkedLocation.Add(new Vector2(xLoc, yLoc), new(pc.GetCustomPosition(), [], pc.PlayerId));

            string stimeStamp = reader.ReadString();
            if (long.TryParse(stimeStamp, out long timeStamp)) LastTP[riftID] = timeStamp;
        }
        else if (operate == 1) //clear marked location
        {
            MarkedLocation.Clear();
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
    }
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();

    public override void UnShapeShiftButton(PlayerControl shapeshifter)
    {
        var shapeshifterId = shapeshifter.PlayerId;

        var currentPos = shapeshifter.GetCustomPosition();
        var totalMarked = MarkedLocation.Count;
        if (totalMarked == 1 && Utils.GetDistance(currentPos, MarkedLocation.ElementAt(0).Key) <= 5f)
        {
            shapeshifter.Notify(GetString("RiftsTooClose"));
            return;
        }
        else if (totalMarked == 2 && Utils.GetDistance(currentPos, MarkedLocation.ElementAt(1).Key) <= 5f)
        {
            shapeshifter.Notify(GetString("RiftsTooClose"));
            return;
        }

        if (totalMarked >= 2)
        {
            MarkedLocation.First(x => x.Key != Lastadded).Value.Despawn();
            MarkedLocation.Remove(MarkedLocation.First(x => x.Key != Lastadded).Key);
        }

        MarkedLocation.Add(shapeshifter.GetCustomPosition(), new(shapeshifter.GetCustomPosition(), [_state.PlayerId], _state.PlayerId));
        Lastadded = shapeshifter.GetCustomPosition();
        if (MarkedLocation.Count == 2) LastTP[shapeshifterId] = Utils.GetTimeStamp();
        shapeshifter.Notify(GetString("RiftCreated"));

        SendRPC(shapeshifterId, 0);
        //sendrpc for marked location and lasttp
        return;
    }

    public override void OnCoEnterVent(PlayerPhysics physics, int ventId)
    {
        var player = physics.myPlayer;
        if (player == null) return;

        _ = new LateTask(() =>
        {
            physics?.RpcBootFromVent(ventId);

            MarkedLocation.Clear();
            //send rpc for clearing markedlocation
            SendRPC(player.PlayerId, 1);
            player.Notify(GetString("RiftsDestroyed"));

        }, 0.5f, "RiftMakerOnVent");
    }

    public override void OnFixedUpdate(PlayerControl player, bool lowLoad, long nowTime, int timerLowLoad)
    {
        if (player == null) return;
        if (Pelican.IsEaten(player.PlayerId) || !player.IsAlive()) return;

        byte playerId = player.PlayerId;
        if (MarkedLocation.Count != 2) return;

        var now = Utils.GetTimeStamp();
        if (!LastTP.ContainsKey(playerId)) LastTP[playerId] = now;
        if (now - LastTP[playerId] <= TPCooldown) return;

        Vector2 position = player.GetCustomPosition();
        Vector2 TPto;

        if (Vector2.Distance(position, MarkedLocation.ElementAt(0).Key) <= RiftRadius.GetFloat())
        {
            TPto = MarkedLocation.ElementAt(1).Key;
        }
        else if (Vector2.Distance(position, MarkedLocation.ElementAt(1).Key) <= RiftRadius.GetFloat())
        {
            TPto = MarkedLocation.ElementAt(0).Key;
        }
        else return;

        LastTP[playerId] = now;
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
