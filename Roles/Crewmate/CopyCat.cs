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
    public static bool HasEnabled => playerIdList.Any();
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
        playerIdList.Add(playerId);
        CurrentKillCooldown = KillCooldown.GetFloat();
    }
    public override void Remove(byte playerId) //only to be used when copycat's role is going to be changed permanently
    {
        playerIdList.Remove(playerId);
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
            if (pcRole != CustomRoles.Sidekick || pcRole != CustomRoles.Retributionist)
            {
                if (pcRole != CustomRoles.CopyCat)
                {
                    pc.GetRoleClass()?.OnRemove(pc.PlayerId);
                }
                pc.RpcSetCustomRole(CustomRoles.CopyCat);
                pc.RpcChangeRoleBasis(CustomRoles.CopyCat);
            }
            pc.ResetKillCooldown();
        }
    }

    private static bool BlackList(CustomRoles role)
    {
        return role is CustomRoles.CopyCat or
            CustomRoles.Doomsayer or // CopyCat cannot guessed roles because he can be know others roles players
            CustomRoles.EvilGuesser or
            CustomRoles.NiceGuesser;
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
        if (CopyCrewVar.GetBool())
        {
            switch (role)
            {
                case CustomRoles.Eraser:
                    role = CustomRoles.Cleanser;
                    break;
                case CustomRoles.Nemesis:
                    role = CustomRoles.Retributionist;
                    break;
                case CustomRoles.Visionary:
                    role = CustomRoles.Oracle;
                    break;
                case CustomRoles.Workaholic:
                    role = CustomRoles.Snitch;
                    break;
                case CustomRoles.Sunnyboy:
                    role = CustomRoles.Doctor;
                    break;
                case CustomRoles.Vindicator:
                case CustomRoles.Pickpocket:
                    role = CustomRoles.Mayor;
                    break;
                case CustomRoles.Councillor:
                    role = CustomRoles.Judge;
                    break;
                case CustomRoles.Arrogance:
                case CustomRoles.Juggernaut:
                case CustomRoles.Berserker:
                    role = CustomRoles.Reverie;
                    break;
                case CustomRoles.Taskinator:
                    role = CustomRoles.Benefactor;
                    break;
                case CustomRoles.EvilTracker:
                    role = CustomRoles.TrackerTOHE;
                    break;
                case CustomRoles.AntiAdminer:
                    role = CustomRoles.Telecommunication;
                    break;
                case CustomRoles.Pursuer:
                    role = CustomRoles.Deceiver;
                    break;
                case CustomRoles.Baker:
                    switch (Baker.CurrentBread())
                    {
                        case 0:
                            role = CustomRoles.Overseer;
                            break;
                        case 1:
                            role = CustomRoles.Deputy;
                            break;    
                        case 2:
                            role = CustomRoles.Medic;
                            break;
                    }
                    break;
            }
        }
        if (role.IsCrewmate())
        {
            if (role != CustomRoles.CopyCat)
            {
                killer.RpcSetCustomRole(role);
                killer.RpcChangeRoleBasis(role);
                killer.GetRoleClass()?.OnAdd(killer.PlayerId);
                killer.SyncSettings();
                Main.PlayerStates[killer.PlayerId].InitTask(killer);
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