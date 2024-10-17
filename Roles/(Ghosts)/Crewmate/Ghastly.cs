using AmongUs.GameOptions;
using TOHE.Roles.Core;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;
using UnityEngine;
using TOHE.Roles.Double;

namespace TOHE.Roles._Ghosts_.Crewmate;

internal class Ghastly : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 22060;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Ghastly);
    public override CustomRoles ThisRoleBase => CustomRoles.GuardianAngel;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateGhosts;
    //==================================================================\\

    private static OptionItem PossessCooldown;
    private static OptionItem MaxPossesions;
    private static OptionItem PossessDur;
    private static OptionItem GhastlySpeed;
    private static OptionItem GhastlyKillAllies;

    private (byte, byte) killertarget = (byte.MaxValue, byte.MaxValue);
    private readonly Dictionary<byte, long> LastTime = [];
    private bool KillerIsChosen = false;

    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Ghastly);
        PossessCooldown = FloatOptionItem.Create(Id + 10, "GhastlyPossessCD", new(2.5f, 120f, 2.5f), 35f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Ghastly])
            .SetValueFormat(OptionFormat.Seconds);
        MaxPossesions = IntegerOptionItem.Create(Id + 11, "GhastlyMaxPossessions", new(1, 99, 1), 10, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Ghastly])
            .SetValueFormat(OptionFormat.Players);
        PossessDur = IntegerOptionItem.Create(Id + 12, "GhastlyPossessionDuration", new(5, 120, 5), 40, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Ghastly])
            .SetValueFormat(OptionFormat.Seconds);
        GhastlySpeed = FloatOptionItem.Create(Id + 13, "GhastlySpeed", new(1.5f, 5f, 0.5f), 2f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Ghastly])
            .SetValueFormat(OptionFormat.Multiplier);
        GhastlyKillAllies = BooleanOptionItem.Create(Id + 14, "GhastlyKillAllies", false, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Ghastly]);
    }
    public override void Add(byte playerId)
    {
        AbilityLimit = MaxPossesions.GetInt();

        CustomRoleManager.OnFixedUpdateOthers.Add(OnFixUpdateOthers);
        CustomRoleManager.CheckDeadBodyOthers.Add(CheckDeadBody);
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.GuardianAngelCooldown = PossessCooldown.GetFloat();
        AURoleOptions.ProtectionDurationSeconds = 0f;
    }
    public override bool OnCheckProtect(PlayerControl angel, PlayerControl target)
    {
        if (target.Is(CustomRoles.NiceMini) && Mini.Age < 18)
        {
            angel.Notify(ColorString(GetRoleColor(CustomRoles.Gangster), GetString("CantPosses")));
            return true;
        }
        if (AbilityLimit <= 0)
        {
            angel.Notify(GetString("GhastlyNoMorePossess"));
            return false;
        }

        var killer = killertarget.Item1;
        var Target = killertarget.Item2;

        if (!KillerIsChosen && !CheckConflicts(target))
        {
            angel.Notify(GetString("GhastlyCannotPossessTarget"));
            return false;
        }

        if (!KillerIsChosen && target.PlayerId != killer)
        {
            TargetArrow.Remove(killer, Target);
            LastTime.Remove(killer);
            killer = target.PlayerId;
            Target = byte.MaxValue;
            KillerIsChosen = true;

            angel.Notify($"\n{GetString("GhastlyChooseTarget")}\n");
        }
        else if (KillerIsChosen && Target == byte.MaxValue && target.PlayerId != killer)
        {
            Target = target.PlayerId;
            AbilityLimit--;
            SendSkillRPC();
            LastTime.Add(killer, GetTimeStamp());

            KillerIsChosen = false;
            GetPlayerById(killer)?.Notify(GetString("GhastlyYouvePosses"));
            angel.Notify($"\n<size=65%>〘{string.Format(GetString("GhastlyPossessedUser"), "</size>" + GetPlayerById(killer).GetRealName())}<size=65%> 〙</size>\n");

            TargetArrow.Add(killer, Target);
            angel.RpcGuardAndKill(target);
            angel.RpcResetAbilityCooldown();

            Logger.Info($" chosen {target.GetRealName()} for : {GetPlayerById(killer).GetRealName()}", "GhastlyTarget");
        }
        else if (target.PlayerId == killer)
        {
            angel.Notify(GetString("GhastlyCannotPossessTarget"));
        }

        killertarget = (killer, Target);

        return false;
    }
    private bool CheckConflicts(PlayerControl target) => target != null && (!GhastlyKillAllies.GetBool() || target.GetCountTypes() != _Player.GetCountTypes());
    
    public override void OnFixedUpdate(PlayerControl player, bool lowLoad, long nowTime)
    {
        if (lowLoad) return;
        var speed = Main.AllPlayerSpeed[player.PlayerId];
        if (speed != GhastlySpeed.GetFloat())
        {
            Main.AllPlayerSpeed[player.PlayerId] = GhastlySpeed.GetFloat();
            player.MarkDirtySettings();
        }
    }
    public void OnFixUpdateOthers(PlayerControl player, bool lowLoad, long nowTime)
    {
        if (!lowLoad && killertarget.Item1 == player.PlayerId 
            && LastTime.TryGetValue(player.PlayerId, out var now) && now + PossessDur.GetInt() <= nowTime)
        {
            _Player?.Notify(string.Format($"\n{ GetString("GhastlyExpired")}\n", player.GetRealName()));
            TargetArrow.Remove(killertarget.Item1, killertarget.Item2);
            LastTime.Remove(player.PlayerId);
            KillerIsChosen = false;
            killertarget = (byte.MaxValue, byte.MaxValue);
        }

    }
    public override bool CheckMurderOnOthersTarget(PlayerControl killer, PlayerControl target)
    {
        var tuple = killertarget;
        if (tuple.Item1 == killer.PlayerId && tuple.Item2 != byte.MaxValue)
        {
            if (tuple.Item2 != target.PlayerId)
            {
                killer.Notify(GetString("GhastlyNotUrTarget"));
                return true;
            }
            else 
            {
                _Player?.Notify(string.Format($"\n{GetString("GhastlyExpired")}\n", killer.GetRealName()));
                TargetArrow.Remove(killertarget.Item1, killertarget.Item2);
                LastTime.Remove(killer.PlayerId);
                KillerIsChosen = false;
                killertarget = (byte.MaxValue, byte.MaxValue);
            }
        }
        return false;
    }

    public override string GetLowerTextOthers(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        if (isForMeeting || (seer != seen && seer.IsAlive())) return string.Empty;

        var (killer, target) = killertarget;

        if (killer == seen.PlayerId && target != byte.MaxValue)
        {
            var arrows = TargetArrow.GetArrows(GetPlayerById(killer), target);
            var tar = target.GetPlayer().GetRealName();
            if (tar == null) return string.Empty;

            var colorstring = ColorString(GetRoleColor(CustomRoles.Ghastly), "<alpha=#88>" + tar + arrows);
            return colorstring;
        }
        return string.Empty;
    }
    private void CheckDeadBody(PlayerControl killer, PlayerControl target, bool inMeeting)
    {
        if (inMeeting) return;

        var tuple = killertarget;
        if (target.PlayerId == tuple.Item1 || target.PlayerId == tuple.Item2)
        {
            _Player?.Notify(string.Format($"\n{GetString("GhastlyExpired")}\n", GetPlayerById(killertarget.Item1)));
            TargetArrow.Remove(killertarget.Item1, killertarget.Item2);
            LastTime.Remove(target.PlayerId);
            KillerIsChosen = false;
            killertarget = (byte.MaxValue, byte.MaxValue);
        }
    }

    public override string GetProgressText(byte playerId, bool cooms)
        => ColorString(AbilityLimit > 0 ? GetRoleColor(CustomRoles.Ghastly).ShadeColor(0.25f) : Color.gray, $"({AbilityLimit})");
}
