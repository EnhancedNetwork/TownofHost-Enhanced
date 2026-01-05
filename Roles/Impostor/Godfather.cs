using TOHE.Roles.Core;
using TOHE.Roles.Crewmate;
using TOHE.Roles.Double;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.Impostor;

internal class Godfather : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Godfather;
    private const int Id = 3400;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorSupport;
    //==================================================================\\

    private static OptionItem GodfatherChangeOpt;

    private static readonly HashSet<byte> GodfatherTarget = [];
    private bool Didvote = false;

    [Obfuscation(Exclude = true)]
    private enum GodfatherChangeModeList
    {
        GodfatherCount_Refugee,
        GodfatherCount_Madmate
    }

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Godfather);
        GodfatherChangeOpt = StringOptionItem.Create(Id + 2, "GodfatherTargetCountMode", EnumHelper.GetAllNames<GodfatherChangeModeList>(), 0, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Godfather]);
    }

    public override void Init()
    {
        GodfatherTarget.Clear();
    }
    public override void Add(byte playerId)
    {
        if (AmongUsClient.Instance.AmHost)
        {
            CustomRoleManager.CheckDeadBodyOthers.Add(CheckDeadBody);
        }
    }
    public override void Remove(byte playerId)
    {
        CustomRoleManager.CheckDeadBodyOthers.Remove(CheckDeadBody);
    }

    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target) => GodfatherTarget.Clear();
    private void CheckDeadBody(PlayerControl killer, PlayerControl target, bool inMeeting)
    {
        var godfather = _Player;

        var ChangeAddon = godfather.GetBetrayalAddon(true);
        var ChangeRole = ChangeAddon switch
        {
            CustomRoles.Admired => CustomRoles.Sheriff,
            CustomRoles.Recruit => CustomRoles.Sidekick,
            _ => CustomRoles.Refugee
        };

        if (GodfatherTarget.Contains(target.PlayerId))
        {
            if (!killer.IsAlive() || killer == godfather) return;
            if (GodfatherChangeOpt.GetValue() == 0)
            {
                if ((killer.GetCustomRole()
                    is CustomRoles.NiceMini
                    or CustomRoles.EvilMini && Mini.Age < 18)
                    || killer.Is(CustomRoles.Loyal)) return;

                killer.RpcChangeRoleBasis(ChangeRole);
                killer.GetRoleClass()?.OnRemove(killer.PlayerId);
                killer.RpcSetCustomRole(ChangeRole);
                killer.GetRoleClass()?.OnAdd(killer.PlayerId);
                if (ChangeRole is CustomRoles.Refugee
                    && godfather.GetBetrayalAddon() != CustomRoles.NotAssigned)
                    killer.RpcSetCustomRole(ChangeAddon);
            }
            else
            {
                if (!killer.CanBeRecruitedBy(godfather)) return;
                killer.RpcSetCustomRole(ChangeAddon);
            }

            if (ChangeAddon is CustomRoles.Admired)
            {
                Admirer.AdmiredList[godfather.PlayerId].Add(killer.PlayerId);
                Admirer.SendRPC(godfather.PlayerId, killer.PlayerId);// make sure Admired Godfather can see Sheriff/Admired
            }

            killer.RpcGuardAndKill();
            killer.ResetKillCooldown();
            killer.SetKillCooldown();
            killer.Notify(ColorString(GetRoleColor(ChangeAddon), GetString("GodfatherRefugeeMsg")));
            NotifyRoles(killer);
        }
    }
    public override void AfterMeetingTasks() => Didvote = false;
    public override bool CheckVote(PlayerControl votePlayer, PlayerControl voteTarget)
    {
        if (votePlayer == null || voteTarget == null) return true;
        if (Didvote == true) return false;
        Didvote = true;

        GodfatherTarget.Add(voteTarget.PlayerId);
        SendMessage(GetString("VoteHasReturned"), votePlayer.PlayerId, title: ColorString(GetRoleColor(CustomRoles.Godfather), string.Format(GetString("VoteAbilityUsed"), GetString("Godfather"))), noReplay: true);
        return false;
    }
}
