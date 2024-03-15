using AmongUs.GameOptions;
using System.Collections.Generic;
using System.Linq;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Neutral;

internal class Pickpocket : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 17400;
    public static HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Count > 0;
    public override bool IsEnable => HasEnabled;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;

    //==================================================================\\

    private static OptionItem KillCooldown;
    public static OptionItem CanVent;
    private static OptionItem HasImpostorVision;
    public static OptionItem VotesPerKill;

    public static void SetupCustomOption()
    {
        //Pickpocketは1人固定
        SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Pickpocket, 1, zeroOne: false);
        KillCooldown = FloatOptionItem.Create(Id + 10, "KillCooldown", new(0f, 180f, 2.5f), 20f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Pickpocket])
            .SetValueFormat(OptionFormat.Seconds);
        CanVent = BooleanOptionItem.Create(Id + 11, "CanVent", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Pickpocket]);
        HasImpostorVision = BooleanOptionItem.Create(Id + 13, "ImpostorVision", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Pickpocket]);
        VotesPerKill = FloatOptionItem.Create(Id + 12, "VotesPerKill", new(0.1f, 10f, 0.1f), 0.5f, TabGroup.NeutralRoles, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Pickpocket]);
    }
    public override void Init()
    {
        playerIdList = [];
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
    public override void ApplyGameOptions(IGameOptions opt, byte id) => opt.SetVision(HasImpostorVision.GetBool());
    public override bool CanUseKillButton(PlayerControl pc) => true;
    public override bool CanUseImpostorVentButton(PlayerControl pc) => CanVent.GetBool();

    public override int AddRealVotesNum(PlayerVoteArea ps)
    {
        return (int)(Main.AllPlayerControls.Count(x => x.GetRealKiller()?.PlayerId == ps.TargetPlayerId) * Pickpocket.VotesPerKill.GetFloat());
    }
    public override void OnMurder(PlayerControl killer, PlayerControl target)
    {
        if (killer.Is(CustomRoles.Pickpocket) && killer.PlayerId != target.PlayerId)
            killer.Notify(string.Format(GetString("PickpocketGetVote"), ((Main.AllPlayerControls.Count(x => x.GetRealKiller()?.PlayerId == killer.PlayerId) + 1) * Pickpocket.VotesPerKill.GetFloat()).ToString("0.0#####")));
    }
}
