using AmongUs.GameOptions;
using InnerNet;
using Microsoft.Extensions.Logging;
using TOHE.Modules;
using TOHE.Roles.Core;
using static TOHE.Options;

namespace TOHE.Roles.Neutral;

internal class Slenderman : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Slenderman;
    private const int Id = 36000;
    public static bool HasEnabled = CustomRoleManager.HasEnabled(CustomRoles.Slenderman);
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralKilling;
    //==================================================================\\

    private static OptionItem KillCooldown;
    public static OptionItem DarkenRadius;
    public static OptionItem DarkenedVision;
    
    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Slenderman, 1, zeroOne: false);
        KillCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 20f, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Slenderman])
            .SetValueFormat(OptionFormat.Seconds);
        DarkenRadius = IntegerOptionItem.Create(Id + 11, "DarkenRadius360", new(2, 5, 1), 3, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Slenderman])
            .SetValueFormat(OptionFormat.Multiplier);
        DarkenedVision = FloatOptionItem.Create(Id + 12, "DarkenedVision360", new(1f, 5f, 0.2f), 1f, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Slenderman])
            .SetValueFormat(OptionFormat.Multiplier);
    }

    public static PlayerControl Slender;
    
    public override void Add(byte playerId)
    {
        Slender = Utils.GetPlayerById(playerId);
        CustomRoleManager.OnFixedUpdateOthers.Add(OnFixedUpdateOthers);
    }
    
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();

    public override bool CanUseKillButton(PlayerControl pc) => true;
    public override bool CanUseImpostorVentButton(PlayerControl pc) => true;

    public static float Decimal;
    private static readonly HashSet<byte> ReducedVisionPlayers = [];
    
    public static void SetReducedVision(IGameOptions opt, PlayerControl target)
    {
        if (ReducedVisionPlayers.Contains(target.PlayerId))
        { 
            opt.SetVision(false);
            opt.SetFloat(FloatOptionNames.CrewLightMod, DarkenedVision.GetFloat());
            opt.SetFloat(FloatOptionNames.ImpostorLightMod, DarkenedVision.GetFloat());
            target.MarkDirtySettings();
        }
    }
    
    public void OnFixedUpdateOthers(PlayerControl player, bool lowLoad, long nowTime)
    {
        foreach (var player2 in Main.AllAlivePlayerControls)
        {
            if (Utils.GetDistance(player.transform.position, player2.transform.position) <= DarkenRadius.GetInt())
            {
                if (!player2.Is(CustomRoles.Slenderman)) return; 
                Decimal = Utils.GetDistance(player.transform.position, player2.transform.position) / DarkenRadius.GetInt();
                
                if (ReducedVisionPlayers.Contains(player.PlayerId)) return;
                ReducedVisionPlayers.Add(player.PlayerId);
            }
        }
    }
}
