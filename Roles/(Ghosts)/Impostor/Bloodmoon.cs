using AmongUs.GameOptions;
using TOHE.Roles.Core;
using UnityEngine;
using Hazel;
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
    
    public readonly Dictionary<byte, int> PlayerDie = [];
    public readonly Dictionary<byte, long> LastTime = [];
    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Bloodmoon);
        KillCooldown = FloatOptionItem.Create(Id + 10, "KillCooldown", new(2.5f, 120f, 2.5f), 25f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Bloodmoon])
        .SetValueFormat(OptionFormat.Seconds);
        CanKillNum = IntegerOptionItem.Create(Id + 11, "BloodMoonCanKillNum", new(1, 15, 1), 3, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Bloodmoon])
            .SetValueFormat(OptionFormat.Players);
        TimeTilDeath = IntegerOptionItem.Create(Id + 12, "BloodMoonTimeTilDie", new(1, 120, 1), 60, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Bloodmoon])
        .SetValueFormat(OptionFormat.Seconds);
    }
    public override void Add(byte PlayerId)
    {
        AbilityLimit = CanKillNum.GetInt();
        CustomRoleManager.LowerOthers.Add(OthersNameText);

    }
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.GuardianAngelCooldown = KillCooldown.GetFloat();
        AURoleOptions.ProtectionDurationSeconds = 0f;
    }
    public override bool OnCheckProtect(PlayerControl killer, PlayerControl target)
    {
        if (AbilityLimit > 0 
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
    private string OthersNameText(PlayerControl seer, PlayerControl player, bool IsForMeeting, bool isforhud = false) 
    {
        var IsMeeting = GameStates.IsMeeting || IsForMeeting;
        if (IsMeeting || seer != player || player == null) return string.Empty;
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
                player.SetRealKiller(Utils.GetPlayerById(PlayerIds.First())); 
            }
        }

        return PlayerDie.TryGetValue(playerid, out var DeathTimer) ? ColorString(GetRoleColor(CustomRoles.Bloodmoon), GetString("DeathTimer").Replace("{DeathTimer}", DeathTimer.ToString())) : "";
    }
}
