using TOHE.Modules;
using TOHE.Roles.Core;
using TOHE.Roles.Crewmate;
using TOHE.Roles.Neutral;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.Coven;

internal class CovenLeader : CovenManager
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.CovenLeader;
    private const int Id = 30900;
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CovenPower;
    //==================================================================\\

    private static OptionItem RetrainCooldown;
    public static OptionItem MaxRetrains;

    public static readonly HashSet<byte> List = [];
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
        List.Clear();
    }
    public override void Add(byte playerId)
    {
        List.Add(playerId);
        playerId.SetAbilityUseLimit(MaxRetrains.GetInt());
        GetPlayerById(playerId)?.AddDoubleTrigger();
    }
    public override void Remove(byte playerId)
    {
        List.Remove(playerId);
    }
    public override bool CanUseKillButton(PlayerControl pc) => true;
    public override void SetKillCooldown(byte id) => RetrainCooldown.GetFloat();
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (killer == null || target == null) return false;
        if (killer.CheckDoubleTrigger(target, () => { Retrain(killer, target); }))
        {
            if (HasNecronomicon(killer) && !target.GetCustomRole().IsCovenTeam())
            {
                return true;
            }
            killer.Notify(GetString("CovenDontKillOtherCoven"));
        }
        return false;
    }
    private void Retrain(PlayerControl killer, PlayerControl target)
    {
        if (killer.GetAbilityUseLimit() <= 0)
        {
            killer.Notify(GetString("CovenLeaderNoRetrain"));
            return;
        }
        if (!HasNecronomicon(killer) && killer.IsPlayerCoven() && !target.IsPlayerCoven())
        {
            killer.Notify(GetString("CovenLeaderRetrainNonCoven"));
            return;
        }
        if (HasNecronomicon(killer) && !IsHelper(killer, target))
        {
            killer.Notify(GetString("CovenLeaderRetrainNonHelper"));
            return;
        }

        var roleList = CustomRolesHelper.AllRoles.Where(role => (role.IsCoven() && (role.IsEnable() && !role.RoleExist(countDead: true)))).ToList();
        retrainPlayer[target.PlayerId] = roleList.RandomElement();
        // if every enabled coven role is already in the game then use one of them anyways
        if (retrainPlayer[target.PlayerId] == CustomRoles.Crewmate || retrainPlayer[target.PlayerId] == CustomRoles.CrewmateTOHE)
            retrainPlayer[target.PlayerId] = CustomRolesHelper.AllRoles.Where(role => (role.IsCoven() && (role.IsEnable()))).ToList().RandomElement();
        killer.ResetKillCooldown();
        killer.SetKillCooldown();
        if (IsHelper(killer, target))
        {
            Logger.Info($"Coven Leader directly Retraining [{target.PlayerId}]{target.GetNameWithRole()} => {retrainPlayer[target.PlayerId]}", "CovenLeader");
            target.Notify(string.Format(GetString("CovenLeaderRetrainInGameNotif"), CustomRoles.CovenLeader.ToColoredString(), retrainPlayer[target.PlayerId].ToColoredString()));
            killer.Notify(string.Format(GetString("CovenLeaderRetrainInGame"), target.GetRealName(), retrainPlayer[target.PlayerId].ToColoredString()));
            target.GetRoleClass()?.OnRemove(target.PlayerId);
            target.RpcChangeRoleBasis(retrainPlayer[target.PlayerId]);
            target.RpcSetCustomRole(retrainPlayer[target.PlayerId]);
            target.GetRoleClass()?.OnAdd(target.PlayerId);
            retrainPlayer.Remove(target.PlayerId);
            killer.RpcRemoveAbilityUse();
            return;
        }
        killer.Notify(GetString("CovenLeaderRetrain"));
    }
    public override void OnMeetingHudStart(PlayerControl pc)
    {
        foreach (byte cov in retrainPlayer.Keys)
        {
            SendMessage(string.Format(GetString("RetrainNotification"), CustomRoles.CovenLeader.ToColoredString(), retrainPlayer[cov].ToColoredString()), cov);
        }
    }
    private bool IsHelper(PlayerControl covenLeader, PlayerControl target)
    {
        var covenList = Main.AllPlayerControls.Where(x => x.IsPlayerCovenTeam()).ToList();
        return target.Is(CustomRoles.Enchanted) || (target.Is(CustomRoles.Romantic) && Romantic.BetPlayer.TryGetValue(target.PlayerId, out var romanticTarget) && covenList.Contains(GetPlayerById(romanticTarget))) || (target.Is(CustomRoles.Lawyer) && covenList.Where(x => Lawyer.TargetList.Contains(x.PlayerId)).Any()) || (target.Is(CustomRoles.Medic) && covenList.Where(x => Medic.IsProtected(x.PlayerId)).Any()) || (target.Is(CustomRoles.Lovers) && covenList.Where(x => x.Is(CustomRoles.Lovers)).Any()) || (target.Is(CustomRoles.Monarch) && covenList.Where(x => x.Is(CustomRoles.Knighted)).Any()) || (target.Is(CustomRoles.Follower) && Follower.BetPlayer.TryGetValue(target.PlayerId, out var followerTarget) && covenList.Contains(GetPlayerById(followerTarget))) || (target.Is(CustomRoles.SchrodingersCat) || SchrodingersCat.teammate.TryGetValue(target.PlayerId, out var catTeammate) && covenList.Contains(GetPlayerById(catTeammate))) || (target.Is(CustomRoles.Crusader) && (target.GetRoleClass() is Crusader crus && covenList.Where(x => crus.ForCrusade.Contains(x.PlayerId)).Any())) ||
            /* converters */(target.Is(CustomRoles.CursedSoul) && covenLeader.Is(CustomRoles.Soulless)) || (target.Is(CustomRoles.Admirer) && covenLeader.Is(CustomRoles.Admired)) || (target.Is(CustomRoles.Cultist) && covenLeader.Is(CustomRoles.Charmed)) || (target.Is(CustomRoles.Jackal) && covenLeader.Is(CustomRoles.Recruit)) || (target.Is(CustomRoles.Gangster) && covenLeader.Is(CustomRoles.Madmate)) || (target.Is(CustomRoles.Infectious) && covenLeader.Is(CustomRoles.Infected)) || (target.Is(CustomRoles.Virus) && covenLeader.Is(CustomRoles.Contagious));

    }
}
