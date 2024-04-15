using AmongUs.GameOptions;
using System.Collections.Generic;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.AddOns.Common;

public class Tired
{
    private static readonly int Id = 27300;
    private static Dictionary<byte, bool> playerIdList; // Target Action player for Vision

    public static OptionItem CanBeOnCrew;
    public static OptionItem CanBeOnImp;
    public static OptionItem CanBeOnNeutral;
    private static OptionItem SetVision;
    private static OptionItem SetSpeed;
    private static OptionItem TiredDuration;


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
        playerIdList = [];
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId, false);
    }
    public static void Remove(byte player)
    {
        playerIdList.Remove(player);
    }

    public static void ApplyGameOptions(IGameOptions opt, PlayerControl player)
    {
        if (!playerIdList.ContainsKey(player.PlayerId)) return;

        if (playerIdList[player.PlayerId])
        {
            opt.SetVision(false);
            opt.SetFloat(FloatOptionNames.CrewLightMod, SetVision.GetFloat());
            opt.SetFloat(FloatOptionNames.ImpostorLightMod, SetVision.GetFloat());
        }
        else
        {
            opt.SetVision(false);
            opt.SetFloat(FloatOptionNames.CrewLightMod, Main.DefaultCrewmateVision);
            opt.SetFloat(FloatOptionNames.ImpostorLightMod, Main.DefaultImpostorVision);
        }
    }
    
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
