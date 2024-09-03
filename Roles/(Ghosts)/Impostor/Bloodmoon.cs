using AmongUs.GameOptions;
using TOHE.Roles.Core;
using TOHE.Roles.Double;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles._Ghosts_.Impostor;

internal class Bloodmoon : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 28100;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Bloodmoon);
    public override CustomRoles ThisRoleBase => CustomRoles.GuardianAngel;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorGhosts;
    //==================================================================\\

    public static OptionItem KillCooldown;
    public static OptionItem CanKillNum;
    private static OptionItem TimeTilDeath;
    
    public static readonly Dictionary<byte, int> PlayerDie = [];
    public static  readonly Dictionary<byte, long> LastTime = [];
    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Bloodmoon);
        KillCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.KillCooldown, new(2.5f, 120f, 2.5f), 25f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Bloodmoon])
        .SetValueFormat(OptionFormat.Seconds);
        CanKillNum = IntegerOptionItem.Create(Id + 11, "BloodMoonCanKillNum", new(1, 15, 1), 3, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Bloodmoon])
            .SetValueFormat(OptionFormat.Players);
        TimeTilDeath = IntegerOptionItem.Create(Id + 12, "BloodMoonTimeTilDie", new(1, 120, 1), 60, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Bloodmoon])
        .SetValueFormat(OptionFormat.Seconds);
    }
    public override void Init()
    {
        PlayerDie.Clear();
        LastTime.Clear();
    }
    public override void Add(byte PlayerId)
    {
        AbilityLimit = CanKillNum.GetInt();
        CustomRoleManager.OnFixedUpdateOthers.Add(OnFixedUpdateOther);

    }
    // EAC bans players when GA uses sabotage
    public override bool CanUseSabotage(PlayerControl pc) => false;
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.GuardianAngelCooldown = KillCooldown.GetFloat();
        AURoleOptions.ProtectionDurationSeconds = 0f;
    }
    public override bool OnCheckProtect(PlayerControl killer, PlayerControl target)
    {
        if (target.Is(CustomRoles.NiceMini) && Mini.Age < 18)
        {
            killer.Notify(ColorString(GetRoleColor(CustomRoles.Gangster), GetString("CantBlood")));
            return true;
        }

        if (AbilityLimit > 0
            && !target.Is(CustomRoles.Jinx)
            && !target.Is(CustomRoles.CursedWolf)
            && !target.IsNeutralApocalypse()
            && killer.RpcCheckAndMurder(target, true)
            && !PlayerDie.ContainsKey(target.PlayerId))
        {
            PlayerDie.Add(target.PlayerId, TimeTilDeath.GetInt());
            LastTime.Add(target.PlayerId, GetTimeStamp());
            killer.RpcResetAbilityCooldown();
            AbilityLimit--;
            SendSkillRPC();
        }
        return false;
    }
    public override string GetProgressText(byte playerId, bool cooms) => ColorString(AbilityLimit > 0  ? GetRoleColor(CustomRoles.Bloodmoon).ShadeColor(0.25f) : Color.gray, $"({AbilityLimit})");
    private void OnFixedUpdateOther(PlayerControl player)
    {
        var IsMeeting = GameStates.IsMeeting;
        var playerid = player.PlayerId;
        if (LastTime.TryGetValue(playerid, out var lastTime) && lastTime + 1 <= GetTimeStamp() && !IsMeeting)
        {
            LastTime[playerid] = GetTimeStamp();
            PlayerDie[playerid]--;
            if (PlayerDie[playerid] <= 0)
            {
                PlayerDie.Remove(playerid);
                LastTime.Remove(playerid);
                player.SetDeathReason(PlayerState.DeathReason.BloodLet);
                player.RpcMurderPlayer(player);
                player.SetRealKiller(_Player);
            }
        }
    }
    public override void OnOtherTargetsReducedToAtoms(PlayerControl DeadPlayer)
    {
        if (LastTime.ContainsKey(DeadPlayer.PlayerId))
            LastTime.Remove(DeadPlayer.PlayerId);

        if (PlayerDie.ContainsKey(DeadPlayer.PlayerId))
            PlayerDie.Remove(DeadPlayer.PlayerId);
    }

    public override string GetLowerTextOthers(PlayerControl seer, PlayerControl player = null, bool isForMeeting = false, bool isForHud = false)
    {
        if (GameStates.IsMeeting || isForMeeting) return string.Empty;
        var playerid = player.PlayerId;

        return PlayerDie.TryGetValue(playerid, out var DeathTimer) ? ColorString(GetRoleColor(CustomRoles.Bloodmoon), GetString("DeathTimer").Replace("{DeathTimer}", DeathTimer.ToString())) : "";

    }
}
