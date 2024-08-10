using AmongUs.GameOptions;
using TOHE.Roles.Core;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Neutral;

internal class Imitator : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 13000;

    public static readonly HashSet<byte> playerIdList = [];

    public static bool HasEnabled => playerIdList.Any();
    public override bool IsExperimental => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Engineer;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralBenign;
    //==================================================================\\
    public static readonly List<CustomRoles> ImitatorChangeList = [CustomRoles.NiceGuesser, CustomRoles.Detective, CustomRoles.Transporter, CustomRoles.Benefactor, CustomRoles.Bodyguard, CustomRoles.Marshall, CustomRoles.Jester, CustomRoles.Opportunist, CustomRoles.Terrorist, CustomRoles.Taskinator, CustomRoles.Sunnyboy, CustomRoles.EvilGuesser, CustomRoles.Nemesis, CustomRoles.Cleaner, CustomRoles.Crewpostor, CustomRoles.Imitator];

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Imitator);
    }
    
    public override void Init()
    {
        playerIdList.Clear();
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
    }
    public override void Remove(byte playerId)
    {
        playerIdList.Remove(playerId);
    }

    public static void ApplyGameOptions(byte playerId)
    {
        if (!playerIdList.Contains(playerId)) return;
        AURoleOptions.EngineerCooldown = 1f;
    }

    public static void OnExitVent(PlayerControl imitator)
    {
        if (imitator == null || !imitator.IsAlive()) return;

        byte imitatorId = imitator.PlayerId;
        if (!playerIdList.Contains(imitatorId)) return;

        CustomRoles currentRole = imitator.GetCustomRole();
        int index = ImitatorChangeList.IndexOf(currentRole);
        if (index < 0) return;

        index = (index + 1) % ImitatorChangeList.Count;
        CustomRoles newRole = ImitatorChangeList[index];

        if (!imitator.Is(CustomRoles.Imitator)) imitator.GetRoleClass()?.OnRemove(imitatorId);
        imitator.RpcSetCustomRole(newRole);
        imitator.GetRoleClass()?.OnAdd(imitatorId);
        imitator.Notify(string.Format(GetString("ImitatorRoleChange"), Utils.GetRoleName(newRole)));
        Logger.Info($"Role Changed from {currentRole} to {newRole} for {imitatorId}", "Imitator change role");
    }

    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.AbilityButton.buttonLabelText.text = GetString("ImitatorVentButtonText");
    }

}
