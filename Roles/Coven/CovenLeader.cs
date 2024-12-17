using Hazel;
using TOHE.Roles.Core;
using UnityEngine;
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

    public override void ReceiveRPC(MessageReader reader, PlayerControl NaN)
    {
        AbilityLimit = reader.ReadSingle();
    }
    public override bool CanUseKillButton(PlayerControl pc) => pc.IsAlive();
    public override string GetProgressText(byte playerId, bool comms)
        => ColorString(AbilityLimit >= 1 ? GetRoleColor(CustomRoles.CovenLeader).ShadeColor(0.25f) : Color.gray, $"({AbilityLimit})");
    public override void SetKillCooldown(byte id) => RetrainCooldown.GetFloat();
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (killer == null || target == null) return false;
        if (HasNecronomicon(killer))
        {
            if (target.GetCustomRole().IsCovenTeam())
            {
                killer.Notify(GetString("CovenDontKillOtherCoven"));
                return false;
            }
            else return true;
        }
        if (AbilityLimit <= 0)
        {
            killer.Notify(GetString("CovenLeaderNoRetrain"));
            return false;
        }
        if (killer.IsPlayerCoven() && !target.IsPlayerCoven())
        {
            killer.Notify(GetString("CovenLeaderRetrainNonCoven"));
            return false;
        }
        var roleList = CustomRolesHelper.AllRoles.Where(role => (role.IsCoven() && (role.IsEnable() && !role.RoleExist(countDead: true)))).ToList();
        retrainPlayer[target.PlayerId] = roleList.RandomElement();
        // if every enabled coven role is already in the game then use one of them anyways
        if (retrainPlayer[target.PlayerId] == CustomRoles.Crewmate || retrainPlayer[target.PlayerId] == CustomRoles.CrewmateTOHE) 
            retrainPlayer[target.PlayerId] = CustomRolesHelper.AllRoles.Where(role => (role.IsCoven() && (role.IsEnable()))).ToList().RandomElement();
        foreach (byte cov in retrainPlayer.Keys)
        {
            SendMessage(string.Format(GetString("RetrainNotification"), CustomRoles.CovenLeader.ToColoredString(), retrainPlayer[cov].ToColoredString()), cov);
        }
        killer.Notify(GetString("CovenLeaderRetrain"));
        killer.ResetKillCooldown();
        killer.SetKillCooldown();
        return false;
    }

}
