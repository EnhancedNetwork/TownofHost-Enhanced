using AmongUs.GameOptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static TOHE.Options;

namespace TOHE.Roles.Neutral;

internal class Jester : RoleBase
{
    //===========================SETUP================================\\
    private static HashSet<byte> PlayerIds = [];
    public static bool HasEnabled => PlayerIds.Count > 0;
    public override bool IsEnable => HasEnabled;
    public override CustomRoles ThisRoleBase => JesterCanVent.GetBool() ? CustomRoles.Engineer : CustomRoles.Crewmate;

    //==================================================================\\

    public static OptionItem JesterCanUseButton;
    public static OptionItem JesterHasImpostorVision;
    public static OptionItem JesterCanVent;
    public static OptionItem MeetingsNeededForJesterWin;
    public static OptionItem HideJesterVote;
    public static OptionItem SunnyboyChance;

    public static void SetupCustomOptions()
    {
        SetupRoleOptions(14400, TabGroup.NeutralRoles, CustomRoles.Jester);
        JesterCanUseButton = BooleanOptionItem.Create(14402, "JesterCanUseButton", false, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Jester]);
        JesterCanVent = BooleanOptionItem.Create(14403, "CanVent", true, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Jester]);
        JesterHasImpostorVision = BooleanOptionItem.Create(14404, "ImpostorVision", true, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Jester]);
        HideJesterVote = BooleanOptionItem.Create(14405, "HideJesterVote", true, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Jester]);
        MeetingsNeededForJesterWin = IntegerOptionItem.Create(14406, "MeetingsNeededForWin", new(0, 10, 1), 0, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Jester])
            .SetValueFormat(OptionFormat.Times);
        SunnyboyChance = IntegerOptionItem.Create(14407, "SunnyboyChance", new(0, 100, 5), 0, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Jester])
            .SetValueFormat(OptionFormat.Percent);
    }
    public override void Init()
    {
        PlayerIds = [];
    }
    public override void Add(byte playerId)
    {
        PlayerIds.Add(playerId);
    }
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        //Jester
        AURoleOptions.EngineerCooldown = 0f;
        AURoleOptions.EngineerInVentMaxTime = 0f;

        //SunnyBoy
        AURoleOptions.ScientistCooldown = 0f;
        AURoleOptions.ScientistBatteryCharge = 60f;
        
        if (Utils.GetPlayerById(playerId).Is(CustomRoles.Jester))
            opt.SetVision(JesterHasImpostorVision.GetBool());
    }
}
