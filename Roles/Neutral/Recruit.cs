using AmongUs.GameOptions;

namespace TOHE.Roles.Neutral;

internal class Recruit : RoleBase //Temporarily unused class
{
    public static bool HasEnabled => playerIdList.Count > 0;
    public override bool IsEnable => HasEnabled;
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;

    private static readonly HashSet<byte> playerIdList = [];

    public override void Init()
    {
        playerIdList.Clear();
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
    }

    public override void ApplyGameOptions(IGameOptions opt, byte ico) => opt.SetVision(Jackal.HasImpostorVision.GetBool());

    public override bool KnowRoleTarget(PlayerControl seer, PlayerControl target) => RecruitKnowRole(target);
    public override string PlayerKnowTargetColor(PlayerControl seer, PlayerControl target) => RecruitKnowRole(target) ? Main.roleColors[CustomRoles.Jackal] : string.Empty;
    private static bool RecruitKnowRole(PlayerControl target)
    {
        return target.Is(CustomRoles.Jackal) || target.Is(CustomRoles.Sidekick) || target.Is(CustomRoles.Recruit);
    }
}
