using TOHE.Roles.Core;
using TOHE.Roles.Crewmate;
using TOHE.Roles.Neutral;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Coven;

internal class Necromancer : CovenManager
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Necromancer;
    private const int Id = 17100;
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Shapeshifter;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CovenUtility;
    //==================================================================\\

    private static OptionItem KillCooldown;
    //private static OptionItem CanVent;
    //private static OptionItem HasImpostorVision;
    private static OptionItem RevengeTime;
    private static OptionItem AbilityDuration;
    private static OptionItem AbilityCooldown;


    public static PlayerControl Killer = null;
    private static bool IsRevenge = false;
    private static int Timer = 0;
    private static bool Success = false;
    private static float tempKillTimer = 0;

    private static readonly Dictionary<byte, List<CustomRoles>> UsedRoles = [];
    private static readonly Dictionary<byte, List<CustomRoles>> OldAddons = [];
    private static float AbilityTimer;
    private static bool canUseAbility;

    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.CovenRoles, CustomRoles.Necromancer, 1, zeroOne: false);
        KillCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 20f, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Necromancer])
            .SetValueFormat(OptionFormat.Seconds);
        RevengeTime = IntegerOptionItem.Create(Id + 11, "NecromancerRevengeTime", new(0, 60, 1), 30, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Necromancer])
            .SetValueFormat(OptionFormat.Seconds);
        //CanVent = BooleanOptionItem.Create(Id + 12, GeneralOption.CanVent, true, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Necromancer]);
        //HasImpostorVision = BooleanOptionItem.Create(Id + 13, GeneralOption.ImpostorVision, true, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Necromancer]);
        AbilityDuration = FloatOptionItem.Create(Id + 14, GeneralOption.AbilityDuration, new(0f, 300f, 2.5f), 60f, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Necromancer])
            .SetValueFormat(OptionFormat.Seconds);
        AbilityCooldown = FloatOptionItem.Create(Id + 15, GeneralOption.AbilityCooldown, new(0f, 180f, 2.5f), 30f, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Necromancer])
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void Init()
    {
        IsRevenge = false;
        Success = false;
        Killer = null;
        tempKillTimer = 0;
        UsedRoles.Clear();
        OldAddons.Clear();
        canUseAbility = false;
        AbilityTimer = 0;
    }
    public override void Add(byte playerId)
    {
        Timer = RevengeTime.GetInt();
        UsedRoles[playerId] = [];
        OldAddons[playerId] = [];
    }
    //public override void ApplyGameOptions(IGameOptions opt, byte id) => opt.SetVision(HasImpostorVision.GetBool());
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
    public override bool CanUseKillButton(PlayerControl pc) => HasNecronomicon(pc) || IsRevenge;
    //public override bool CanUseImpostorVentButton(PlayerControl pc) => CanVent.GetBool();

    public override bool OnCheckMurderAsTarget(PlayerControl killer, PlayerControl target)
    {
        if (IsRevenge) return true;
        if (killer.GetCustomRole().IsCovenTeam()) return true;
        if (!HasNecronomicon(target)) return true;
        if ((killer.Is(CustomRoles.Retributionist) || killer.Is(CustomRoles.Nemesis)) && !killer.IsAlive()) return true;

        _ = new LateTask(target.RpcRandomVentTeleport, 0.01f, "Random Vent Teleport - Necromancer");

        Timer = RevengeTime.GetInt();
        Countdown(Timer, target);
        IsRevenge = true;
        killer.SetKillCooldown();
        killer.Notify(GetString("NecromancerHide"), RevengeTime.GetFloat());
        tempKillTimer = target.killTimer;
        target.SetKillCooldown(time: 1f);
        Killer = killer;

        return false;
    }
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (killer == null || target == null || !killer.IsAlive()) return false;

        if (!IsRevenge) return true;
        else if (target == Killer)
        {
            Success = true;
            killer.Notify(GetString("NecromancerSuccess"));
            killer.SetKillCooldown(KillCooldown.GetFloat() + tempKillTimer);
            IsRevenge = false;
            return true;
        }
        else
        {
            killer.RpcMurderPlayer(killer);
            return false;
        }
    }
    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        return string.Format(GetString(GeneralOption.AbilityCooldown.ToString()) + ": {0:F0}s / {1:F0}s", AbilityTimer, AbilityCooldown.GetFloat());
    }
    public override void UnShapeShiftButton(PlayerControl nm)
    {
        if (nm == null) return;
        if (!canUseAbility)
        {
            nm.Notify(GetString("NecromancerCooldownNotDone"));
            return;
        }
        if (IsRevenge)
        {
            nm.Notify(GetString("NecromancerRevengeInProgress"));
            return;
        }
        var deadPlayers = Main.AllPlayerControls.Where(x => !x.IsAlive());
        List<CustomRoles> deadRoles = [];
        foreach (var deadPlayer in deadPlayers)
        {
            if (BlackList(deadPlayer.GetCustomRole())) continue;
            if (UsedRoles[nm.PlayerId].Contains(deadPlayer.GetCustomRole())) continue;
            deadRoles.Add(deadPlayer.GetCustomRole());
        }
        if (deadRoles.Count < 1)
        {
            nm.Notify(GetString("NecromancerNoUsableRoles"));
            return;
        }
        var role = deadRoles.RandomElement();
        nm.RpcChangeRoleBasis(role);
        nm.RpcSetCustomRole(role, false, false);
        nm.GetRoleClass()?.OnAdd(nm.PlayerId);
        nm.SyncSettings();
        Dictionary<byte, List<CustomRoles>> CurrentAddons = new();
        CurrentAddons[nm.PlayerId] = [];
        foreach (var addon in nm.GetCustomSubRoles())
        {
            CurrentAddons[nm.PlayerId].Add(addon);
        }
        foreach (var addon in CurrentAddons[nm.PlayerId])
        {
            if (!CustomRolesHelper.CheckAddonConfilct(addon, nm))
            {
                OldAddons[nm.PlayerId].Add(addon);
                Main.PlayerStates[nm.PlayerId].RemoveSubRole(addon);
                Logger.Info($"{nm.GetNameWithRole()} had incompatible addon {addon.ToString()}, removing addon", "Necromancer");
            }
        }
        Main.PlayerStates[nm.PlayerId].InitTask(nm);
        nm.RpcGuardAndKill(nm);
        nm.Notify(string.Format(GetString("CopyCatRoleChange"), Utils.GetRoleName(role)));
        _ = new LateTask(() =>
        {
            if (!GameStates.IsMeeting)
                RevertRole(nm, role);
        }, AbilityDuration.GetFloat(), "Necromancer Revert Role");
    }
    private static void RevertRole(PlayerControl nm, CustomRoles role)
    {
        if (nm == null) return;
        if (nm.GetCustomRole() != CustomRoles.Necromancer)
        {
            nm.GetRoleClass()?.OnRemove(nm.PlayerId);
        }
        if (nm.IsAlive())
            nm.RpcChangeRoleBasis(CustomRoles.Necromancer);
        nm.RpcSetCustomRole(CustomRoles.Necromancer, false, false);
        foreach (var addon in OldAddons[nm.PlayerId])
        {
            nm.RpcSetCustomRole(addon, false, false);
        }
        OldAddons[nm.PlayerId].Clear();
        nm.ResetKillCooldown();
        nm.SyncSettings();
        nm.RpcGuardAndKill(nm);
        nm.Notify(string.Format(GetString("CopyCatRoleChange"), Utils.GetRoleName(CustomRoles.Necromancer)));
        UsedRoles[nm.PlayerId].Add(role);
        canUseAbility = false;
        AbilityTimer = 0;
        Logger.Info($"Reverted Role for {nm.GetRealName()}", "Necromancer");
    }

    private static bool BlackList(CustomRoles role)
    {
        return role.IsGhostRole() || role is
            CustomRoles.Veteran or
            CustomRoles.Solsticer or
            CustomRoles.Lawyer or
            CustomRoles.Amnesiac or
            CustomRoles.Imitator or
            CustomRoles.CopyCat or
            CustomRoles.Follower or
            CustomRoles.Romantic or
            CustomRoles.God or
            CustomRoles.Innocent or
            CustomRoles.Jackal or
            CustomRoles.Workaholic or
            CustomRoles.Specter or
            CustomRoles.Marshall or
            CustomRoles.Captain or
            CustomRoles.Retributionist or
            CustomRoles.Nemesis or
            CustomRoles.NiceMini or
            CustomRoles.Mini or
            CustomRoles.EvilMini or
            CustomRoles.SuperStar or
            CustomRoles.RuthlessRomantic or
            CustomRoles.VengefulRomantic or
            CustomRoles.CursedSoul or
            CustomRoles.Provocateur or
            CustomRoles.Specter or
            // Just in case
            CustomRoles.GM or
            CustomRoles.Killer or
            CustomRoles.Coven or
            CustomRoles.Apocalypse or
            CustomRoles.Solsticer or
            CustomRoles.Sunnyboy ||
            (role == CustomRoles.Workaholic && Workaholic.WorkaholicVisibleToEveryone.GetBool()) ||
            (role == CustomRoles.Mayor && Mayor.MayorRevealWhenDoneTasks.GetBool()) ||
            (role == CustomRoles.Executioner && Executioner.KnowTargetRole.GetBool()) ||
            (role == CustomRoles.Doctor && Doctor.VisibleToEveryoneOpt.GetBool());
    }
    public override void OnFixedUpdate(PlayerControl player, bool lowLoad, long nowTime, int timerLowLoad)
    {
        if (AbilityTimer < AbilityCooldown.GetFloat())
        {
            AbilityTimer += Time.fixedDeltaTime;
        }
        else canUseAbility = true;
    }
    public static string NecromancerReminder(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        if (Main.PlayerStates[seen.PlayerId].IsNecromancer && !seen.Is(CustomRoles.Necromancer) && !seer.IsAlive() && seen.IsAlive())
        {
            return $"<size=1.5><i>{CustomRoles.Necromancer.ToColoredString()}</i></size>";
        }
        return string.Empty;
    }
    public static void UnAfterMeetingTasks()
    {
        AbilityTimer = 0;
        foreach (var nm in Main.AllPlayerControls.Where(x => Main.PlayerStates[x.PlayerId].IsNecromancer))
        {
            if (nm.GetCustomRole() != CustomRoles.Necromancer)
            {
                RevertRole(nm, nm.GetCustomRole());
            }
        }
    }
    private static void Countdown(int seconds, PlayerControl player)
    {
        var killer = Killer;
        if (Success || !player.IsAlive() || !killer.IsAlive())
        {
            Timer = RevengeTime.GetInt();
            Success = false;
            Killer = null;
            return;
        }
        if (GameStates.IsMeeting && player.IsAlive())
        {
            player.SetDeathReason(PlayerState.DeathReason.Kill);
            player.RpcExileV2();
            player.Data.IsDead = true;
            player.Data.MarkDirty();
            Main.PlayerStates[player.PlayerId].SetDead();
            player.SetRealKiller(killer);
            Killer = null;
            return;
        }
        if (seconds <= 0)
        {
            player.RpcMurderPlayer(player);
            player.SetRealKiller(killer);
            Killer = null;
            return;
        }
        player.Notify(string.Format(GetString("NecromancerRevenge"), seconds, Killer.GetRealName()), 1.1f);
        Timer = seconds;

        _ = new LateTask(() => { Countdown(seconds - 1, player); }, 1.01f, "Necromancer Countdown");
    }
}
