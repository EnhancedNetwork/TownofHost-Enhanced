using TOHE.Roles.Core;
using TOHE.Roles.Coven;
using TOHE.Roles.Neutral;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate;

internal class CopyCat : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.CopyCat;
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
    private static readonly Dictionary<byte, List<CustomRoles>> OldAddons = [];

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
        OldAddons.Clear();
    }

    public override void Add(byte playerId)
    {
        if (!playerIdList.Contains(playerId))
            playerIdList.Add(playerId);
        CurrentKillCooldown = KillCooldown.GetFloat();
        OldAddons[playerId] = [];
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
                    pc.RpcSetCustomRole(CustomRoles.CopyCat, false, false);
                }
                continue;
            }
            ////////////           /*remove the settings for current role*/             /////////////////////

            var pcRole = pc.GetCustomRole();
            if (pcRole is not CustomRoles.Sidekick and not CustomRoles.Jackal and not CustomRoles.Refugee && !(!pc.IsAlive() && pcRole is CustomRoles.Retributionist))
            {
                if (pcRole != CustomRoles.CopyCat)
                {
                    pc.GetRoleClass()?.OnRemove(pc.PlayerId);
                    pc.RpcChangeRoleBasis(CustomRoles.CopyCat);
                    pc.RpcSetCustomRole(CustomRoles.CopyCat, false, false);
                    foreach (var addon in OldAddons[pc.PlayerId])
                    {
                        pc.RpcSetCustomRole(addon, false, false);
                    }
                }
            }
            pc.ResetKillCooldown();
            pc.SetKillCooldown();
            OldAddons[pc.PlayerId].Clear();
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
            killer.SetKillCooldown();
            return false;
        }
        if (CopyCrewVar.GetBool())
        {
            role = role switch
            {
                CustomRoles.Stealth or CustomRoles.Medusa => CustomRoles.Grenadier,
                CustomRoles.TimeThief => CustomRoles.TimeManager,
                CustomRoles.Consigliere => CustomRoles.Overseer,
                CustomRoles.Mercenary => CustomRoles.Addict,
                CustomRoles.Miner => CustomRoles.Mole,
                CustomRoles.Godfather => CustomRoles.ChiefOfPolice,
                CustomRoles.Twister => CustomRoles.TimeMaster,
                CustomRoles.Disperser => CustomRoles.Transporter,
                CustomRoles.Eraser => CustomRoles.Cleanser,
                CustomRoles.Visionary => CustomRoles.Oracle,
                CustomRoles.Workaholic => CustomRoles.Snitch,
                CustomRoles.Sunnyboy => CustomRoles.Doctor,
                CustomRoles.Councillor => CustomRoles.Judge,
                CustomRoles.Taskinator => CustomRoles.Benefactor,
                CustomRoles.EvilTracker => CustomRoles.TrackerTOHE,
                CustomRoles.AntiAdminer => CustomRoles.Telecommunication,
                CustomRoles.Pursuer => CustomRoles.Deceiver,
                CustomRoles.CursedWolf => CustomRoles.Veteran,
                CustomRoles.Swooper or CustomRoles.Wraith => CustomRoles.Chameleon,
                CustomRoles.Vindicator or CustomRoles.Pickpocket => CustomRoles.Mayor,
                CustomRoles.Opportunist or CustomRoles.BloodKnight or CustomRoles.Wildling => CustomRoles.Guardian,
                CustomRoles.Cultist or CustomRoles.Virus or CustomRoles.Gangster => CustomRoles.Admirer,
                CustomRoles.Arrogance or CustomRoles.Juggernaut or CustomRoles.Berserker => CustomRoles.Reverie,
                CustomRoles.Baker when Baker.CurrentBread() is 0 => CustomRoles.Overseer,
                CustomRoles.Baker when Baker.CurrentBread() is 1 => CustomRoles.Deputy,
                CustomRoles.Baker when Baker.CurrentBread() is 2 => CustomRoles.Medic,
                CustomRoles.PotionMaster when PotionMaster.CurrentPotion() is 0 => CustomRoles.Overseer,
                CustomRoles.PotionMaster when PotionMaster.CurrentPotion() is 1 => CustomRoles.Medic,
                CustomRoles.Sacrifist => CustomRoles.Alchemist,
                CustomRoles.MoonDancer => CustomRoles.Merchant,
                CustomRoles.Ritualist => CustomRoles.Admirer,
                CustomRoles.Jinx => CustomRoles.Crusader,
                CustomRoles.Trickster or CustomRoles.Illusionist => CustomRolesHelper.AllRoles.Where(role => role.IsEnable() && !role.IsAdditionRole() && role.IsCrewmate() && !BlackList(role)).ToList().RandomElement(),
                _ => role
            };
        }
        if (role.IsCrewmate())
        {
            if (role != CustomRoles.CopyCat)
            {
                killer.RpcChangeRoleBasis(role);
                killer.RpcSetCustomRole(role, false, false);
                killer.GetRoleClass()?.OnAdd(killer.PlayerId);
                killer.SyncSettings();
                Dictionary<byte, List<CustomRoles>> CurrentAddons = new();
                CurrentAddons[killer.PlayerId] = [];
                foreach (var addon in killer.GetCustomSubRoles())
                {
                    CurrentAddons[killer.PlayerId].Add(addon);
                }
                foreach (var addon in CurrentAddons[killer.PlayerId])
                {
                    if (!CustomRolesHelper.CheckAddonConfilct(addon, killer))
                    {
                        OldAddons[killer.PlayerId].Add(addon);
                        Main.PlayerStates[killer.PlayerId].RemoveSubRole(addon);
                        Logger.Info($"{killer.GetNameWithRole()} had incompatible addon {addon.ToString()}, removing addon", "CopyCat");
                    }
                }
            }
            if (CopyTeamChangingAddon.GetBool())
            {
                if (target.Is(CustomRoles.Madmate) || target.Is(CustomRoles.Rascal)) killer.RpcSetCustomRole(CustomRoles.Madmate);
                if (target.Is(CustomRoles.Charmed)) killer.RpcSetCustomRole(CustomRoles.Charmed);
                if (target.Is(CustomRoles.Infected)) killer.RpcSetCustomRole(CustomRoles.Infected);
                if (target.Is(CustomRoles.Recruit)) killer.RpcSetCustomRole(CustomRoles.Recruit);
                if (target.Is(CustomRoles.Contagious)) killer.RpcSetCustomRole(CustomRoles.Contagious);
                if (target.Is(CustomRoles.Soulless)) killer.RpcSetCustomRole(CustomRoles.Soulless);
                if (target.Is(CustomRoles.Admired)) killer.RpcSetCustomRole(CustomRoles.Admired);
                if (target.Is(CustomRoles.Enchanted)) killer.RpcSetCustomRole(CustomRoles.Enchanted);
            }
            killer.RpcGuardAndKill(killer);
            killer.Notify(string.Format(GetString("CopyCatRoleChange"), Utils.GetRoleName(role)));
            return false;

        }
        killer.Notify(GetString("CopyCatCanNotCopy"));
        killer.ResetKillCooldown();
        killer.SetKillCooldown();
        return false;
    }

    public override void SetAbilityButtonText(HudManager hud, byte id)
    {
        hud.ReportButton.OverrideText(GetString("ReportButtonText"));
        hud.KillButton.OverrideText(GetString("CopyButtonText"));
    }
}
