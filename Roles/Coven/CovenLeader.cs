using AmongUs.GameOptions;
using Hazel;
using System.Diagnostics.Metrics;
using System.Text;
using TOHE.Roles.Core;
using TOHE.Roles.Crewmate;
using TOHE.Roles.Double;
using TOHE.Roles.Neutral;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.Coven;

internal class CovenLeader : CovenManager
{
    //===========================SETUP================================\\
    private const int Id = 29800;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.CovenLeader);
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CovenPower;
    //==================================================================\\

    private static OptionItem RetrainCooldown;
    public static OptionItem MaxRetrains;

    public static readonly Dictionary<byte, CustomRoles> retrainPlayer = [];

    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.CovenRoles, CustomRoles.CovenLeader, 1, zeroOne: false);
        MaxRetrains = IntegerOptionItem.Create(Id + 10, "CovenLeaderMaxRetrains", new(1, 15, 1), 2, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.CovenLeader])
            .SetValueFormat(OptionFormat.Times);
        RetrainCooldown = FloatOptionItem.Create(Id + 11, "CovenLeaderRetrainCooldown", new(0f, 180f, 2.5f), 30f, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.CovenLeader])
                .SetValueFormat(OptionFormat.Seconds);
    }
    public override void Init()
    {
        retrainPlayer.Clear();
    }
    public override void Add(byte playerId)
    {
        AbilityLimit = MaxRetrains.GetInt();
    }

    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (killer == null || target == null) return false;
        if (HasNecronomicon(killer)) return true;
        if (killer.IsPlayerCoven() && !target.IsPlayerCoven()) return false;
        var roleList = CustomRolesHelper.AllRoles.Where(role => (role.IsCoven() && (role.IsEnable() && !role.RoleExist(countDead: true)))).ToList();
        retrainPlayer[target.PlayerId] = roleList.RandomElement();
        foreach (byte cov in retrainPlayer.Keys)
        {
            GetPlayerById(cov).SendMessage(string.Format(GetString("RetrainAcceptOffer"), retrainPlayer[cov].ToColoredString()));
        }
        return false;
    }
    public override bool CheckVote(PlayerControl voter, PlayerControl target)
    {
        PlayerControl CL = CustomRoles.CovenLeader.GetPlayerListByRole().First();
        if ((voter == target) && retrainPlayer[voter.PlayerId].IsCoven())
        {
            voter.RpcSetCustomRole(retrainPlayer[voter.PlayerId]);
            CL.SendMessage(string.Format(GetString("CovenLeaderAcceptRetrain"), retrainPlayer[voter.PlayerId].ToColoredString()));
            voter.SendMessage(string.Format(GetString("RetrainAcceptOffer"), retrainPlayer[voter.PlayerId].ToColoredString()));
            retrainPlayer[voter.PlayerId] = CustomRoles.Crewmate;
            AbilityLimit--;
            return true;
        }
        if ((voter != target) && retrainPlayer[voter.PlayerId].IsCoven())
        {
            CL.SendMessage(string.Format(GetString("CovenLeaderDeclineRetrain"), retrainPlayer[voter.PlayerId].ToColoredString()));
            voter.SendMessage(GetString("RetrainDeclineOffer"));
            retrainPlayer[voter.PlayerId] = CustomRoles.Crewmate;
            return true;
        }
        return false;
    }
}
