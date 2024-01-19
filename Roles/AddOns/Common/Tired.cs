using Hazel;
using System.Collections.Generic;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.AddOns.Common;

public class Tired
{
    private static readonly int Id = 27300;
    public static OptionItem CanBeOnCrew;
    public static OptionItem CanBeOnImp;
    public static OptionItem CanBeOnNeutral;
    public static OptionItem SetVision;
    public static OptionItem SetSpeed;
    public static OptionItem TiredDuration;

    public static Dictionary<byte, bool> playerIdList; // Target Action player for Vision


    public static void SetupCustomOptions()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Tired, canSetNum: true, tab: TabGroup.Addons);
        SetVision = FloatOptionItem.Create(Id + 10, "TiredVision", new(0f, 2f, 0.25f), 0.25f, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Tired])
             .SetValueFormat(OptionFormat.Multiplier);
        SetSpeed = FloatOptionItem.Create(Id + 11, "TiredSpeed", new(0.25f, 3f, 0.25f), 0.75f, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Tired])
           .SetValueFormat(OptionFormat.Multiplier);
        TiredDuration = FloatOptionItem.Create(Id + 12, "TiredDur", new(2f, 15f, 0.5f), 5f, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Tired])
                .SetValueFormat(OptionFormat.Seconds);
        CanBeOnImp = BooleanOptionItem.Create(Id + 13, "ImpCanBeTired", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Tired]);
        CanBeOnCrew = BooleanOptionItem.Create(Id + 14, "CrewCanBeTired", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Tired]);
        CanBeOnNeutral = BooleanOptionItem.Create(Id + 15, "NeutralCanBeTired", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Tired]);
    }

    public static void Init()
    {
        playerIdList = new();
    }
    
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId, false);
    }
    
    public static void SendRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.TiredRPC, SendOption.Reliable, -1);
        writer.Write(playerId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    
   // public static void ReceiveRPC(MessageReader reader)
  //  {
   //     byte playerid = reader.ReadByte();
   // } // Do I need this?
    
    public static void AfterActionTasks(PlayerControl player)
    {
        // Speed
        player.Notify(GetString("TiredNotify"));
        player.MarkDirtySettings();
        var tmpSpeed = Main.AllPlayerSpeed[player.PlayerId];
        Main.AllPlayerSpeed[player.PlayerId] = SetSpeed.GetFloat();

        // Vision
        playerIdList[player.PlayerId] = true;

        _ = new LateTask(() =>
        {
            Main.AllPlayerSpeed[player.PlayerId] = Main.AllPlayerSpeed[player.PlayerId] - SetSpeed.GetFloat() + tmpSpeed;
            player.MarkDirtySettings();
            playerIdList[player.PlayerId] = false;
        }, TiredDuration.GetFloat());
    }
}
