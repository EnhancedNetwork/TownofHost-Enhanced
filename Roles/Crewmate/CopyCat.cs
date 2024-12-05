using TOHE.Roles.Core;
using TOHE.Roles.Neutral;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate;

internal class CopyCat : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 11500;
    public static readonly HashSet<byte> playerIdList = [];

    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmatePower;
    //==================================================================\\

    private static OptionItem KillCooldown;
    private static OptionItem CopyCrewVar;
    private static OptionItem CopyTeamChangingAddon;

    private static float CurrentKillCooldown = new();

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.CopyCat);
        KillCooldown = FloatOptionItem.Create(Id + 10, "CopyCatCopyCooldown", new(0f, 180f, 1f), 15f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.CopyCat])
            .SetValueFormat(OptionFormat.Seconds);
        CopyCrewVar = BooleanOptionItem.Create(Id + 13, "CopyCrewVar", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.CopyCat]);
        CopyTeamChangingAddon = BooleanOptionItem.Create(Id + 14, "CopyTeamChangingAddon", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.CopyCat]);
    }

    public override void Init()
    {
        playerIdList.Clear();
        CurrentKillCooldown = new();
    }

    public override void Add(byte playerId)
    {
        if (!playerIdList.Contains(playerId))
            playerIdList.Add(playerId);
        CurrentKillCooldown = KillCooldown.GetFloat();
    }
    public override void Remove(byte playerId) //only to be used when copycat's role is going to be changed permanently
    {
        // Copy cat role wont be removed for now i guess
        // playerIdList.Remove(playerId);
    }
    public static bool CanCopyTeamChangingAddon() => CopyTeamChangingAddon.GetBool();
    public static bool NoHaveTask(byte playerId, bool ForRecompute) => playerIdList.Contains(playerId) && (playerId.GetPlayer().GetCustomRole().IsDesyncRole() || ForRecompute);
    public override bool CanUseKillButton(PlayerControl pc) => true;
    public override bool CanUseImpostorVentButton(PlayerControl pc) => playerIdList.Contains(pc.PlayerId);
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = Utils.GetPlayerById(id).IsAlive() ? CurrentKillCooldown : 300f;
    public static void UnAfterMeetingTasks()
    {
        foreach (var playerId in playerIdList.ToArray())
        {
            var pc = playerId.GetPlayer();
            if (pc == null) continue;

            if (!pc.IsAlive())
            {
                if (!pc.HasGhostRole())
                {
                    pc.RpcSetCustomRole(CustomRoles.CopyCat);
                }
                continue;
            }
            ////////////           /*remove the settings for current role*/             /////////////////////

            var pcRole = pc.GetCustomRole();
            if (pcRole is not CustomRoles.Sidekick and not CustomRoles.Retributionist)
            {
                if (pcRole != CustomRoles.CopyCat)
                {
                    pc.GetRoleClass()?.OnRemove(pc.PlayerId);
                }
                pc.RpcChangeRoleBasis(CustomRoles.CopyCat);
                pc.RpcSetCustomRole(CustomRoles.CopyCat);
            }
            pc.ResetKillCooldown();
        }
    }

    private static bool BlackList(CustomRoles role)
    {
        return role is CustomRoles.CopyCat or
            CustomRoles.Doomsayer or // CopyCat cannot guessed roles because he can be know others roles players
            CustomRoles.EvilGuesser or
            CustomRoles.NiceGuesser or
            CustomRoles.Baker or CustomRoles.Famine;
    }

    public override bool ForcedCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        CustomRoles role = target.GetCustomRole();
        if (BlackList(role))
        {
            killer.Notify(GetString("CopyCatCanNotCopy"));
            killer.ResetKillCooldown();
            return false;
        }
        if (target.Is(CustomRoles.Narc))
        {
            killer.RpcChangeRoleBasis(CustomRoles.Sheriff);
            killer.RpcSetCustomRole(CustomRoles.Sheriff);
            killer.GetRoleClass()?.OnAdd(killer.PlayerId);
            killer.SyncSettings();
            Main.PlayerStates[killer.PlayerId].InitTask(killer);
            killer.RpcGuardAndKill(killer);
            killer.Notify(string.Format(GetString("CopyCatRoleChange"), Utils.GetRoleName(CustomRoles.Sheriff)));
            return false;
        }
        if (CopyCrewVar.GetBool())
        {
            role = role switch
            {
                CustomRoles.Stealth and not CustomRoles.Narc => CustomRoles.Grenadier,
                CustomRoles.TimeThief and not CustomRoles.Narc => CustomRoles.TimeManager,
                CustomRoles.Consigliere and not CustomRoles.Narc => CustomRoles.Overseer,
                CustomRoles.Mercenary and not CustomRoles.Narc => CustomRoles.Addict,
                CustomRoles.Miner and not CustomRoles.Narc => CustomRoles.Mole,
                CustomRoles.PotionMaster => CustomRoles.Overseer,
                CustomRoles.Twister and not CustomRoles.Narc => CustomRoles.TimeMaster,
                CustomRoles.Disperser and not CustomRoles.Narc => CustomRoles.Transporter,
                CustomRoles.Eraser and not CustomRoles.Narc => CustomRoles.Cleanser,
                CustomRoles.Visionary and not CustomRoles.Narc => CustomRoles.Oracle,
                CustomRoles.Workaholic => CustomRoles.Snitch,
                CustomRoles.Sunnyboy => CustomRoles.Doctor,
                CustomRoles.Councillor and not CustomRoles.Narc => CustomRoles.Judge,
                CustomRoles.Taskinator => CustomRoles.Benefactor,
                CustomRoles.EvilTracker and not CustomRoles.Narc => CustomRoles.TrackerTOHE,
                CustomRoles.AntiAdminer and not CustomRoles.Narc => CustomRoles.Telecommunication,
                CustomRoles.Pursuer => CustomRoles.Deceiver,
                (CustomRoles.CursedWolf and not CustomRoles.Narc) or CustomRoles.Jinx => CustomRoles.Veteran,
                (CustomRoles.Swooper and not CustomRoles.Narc) or CustomRoles.Wraith => CustomRoles.Chameleon,
                (CustomRoles.Vindicator and not CustomRoles.Narc) or CustomRoles.Pickpocket => CustomRoles.Mayor,
                (CustomRoles.Arrogance and not CustomRoles.Narc) or CustomRoles.Juggernaut or CustomRoles.Berserker => CustomRoles.Reverie
                _ => role
            };
        }
        if (role.IsCrewmate())
        {
            if (role != CustomRoles.CopyCat)
            {
                killer.RpcChangeRoleBasis(role);
                killer.RpcSetCustomRole(role);
                killer.GetRoleClass()?.OnAdd(killer.PlayerId);
                killer.SyncSettings();
            }
            if (CopyTeamChangingAddon.GetBool())
            {
                if (target.Is(CustomRoles.Madmate) || target.Is(CustomRoles.Rascal)) killer.RpcSetCustomRole(CustomRoles.Madmate);
                if (target.Is(CustomRoles.Charmed)) killer.RpcSetCustomRole(CustomRoles.Charmed);
                if (target.Is(CustomRoles.Infected)) killer.RpcSetCustomRole(CustomRoles.Infected);
                if (target.Is(CustomRoles.Recruit)) killer.RpcSetCustomRole(CustomRoles.Recruit);
                if (target.Is(CustomRoles.Contagious)) killer.RpcSetCustomRole(CustomRoles.Contagious);
                if (target.Is(CustomRoles.Soulless)) killer.RpcSetCustomRole(CustomRoles.Soulless);
            }
            killer.RpcGuardAndKill(killer);
            killer.Notify(string.Format(GetString("CopyCatRoleChange"), Utils.GetRoleName(role)));
            return false;

        }
        killer.Notify(GetString("CopyCatCanNotCopy"));
        killer.ResetKillCooldown();
        return false;
    }

    public override void SetAbilityButtonText(HudManager hud, byte id)
    {
        hud.ReportButton.OverrideText(GetString("ReportButtonText"));
        hud.KillButton.OverrideText(GetString("CopyButtonText"));
    }
}
