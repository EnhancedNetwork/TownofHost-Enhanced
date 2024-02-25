using System.Collections.Generic;
using System.Linq;
using TOHE.Roles.Core;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate;

internal class CopyCat : RoleBase
{
    private const int Id = 11500;
    public static List<byte> playerIdList = [];
    public static bool On = false;
    public override bool IsEnable => On;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;

    public static float CurrentKillCooldown = new();

    public static OptionItem KillCooldown;
    public static OptionItem CopyCrewVar;
    public static OptionItem CopyTeamChangingAddon;

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.CopyCat);
        KillCooldown = FloatOptionItem.Create(Id + 10, "CopyCatCopyCooldown", new(0f, 180f, 1f), 15f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.CopyCat])
            .SetValueFormat(OptionFormat.Seconds);
        CopyCrewVar = BooleanOptionItem.Create(Id + 13, "CopyCrewVar", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.CopyCat]);
        CopyTeamChangingAddon = BooleanOptionItem.Create(Id + 14, "CopyTeamChangingAddon", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.CopyCat]);
    }

    public override void Init()
    {
        playerIdList = [];
        CurrentKillCooldown = new();
        On = false;
    }

    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        CurrentKillCooldown = KillCooldown.GetFloat();
        On = true;

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }
    public override void Remove(byte playerId) //only to be used when copycat's role is going to be changed permanently
    {
        playerIdList.Remove(playerId);
        if (!playerIdList.Any()) On = false;
    }
    public override bool CanUseKillButton(PlayerControl pc) => pc.IsAlive();
    public override bool CanUseImpostorVentButton(PlayerControl pc) => playerIdList.Contains(pc.PlayerId);
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = Utils.GetPlayerById(id).IsAlive() ? CurrentKillCooldown : 300f;
    public static void UnAfterMeetingTasks()
    {
        foreach (var player in playerIdList.ToArray())
        {
            var pc = Utils.GetPlayerById(player);
            if (pc == null) continue;
            var role = pc.GetCustomRole();
            ////////////           /*remove the settings for current role*/             /////////////////////
            if (role != CustomRoles.CopyCat) role.GetRoleClass().Remove(pc.PlayerId);
            
            switch (role)
            {
                case CustomRoles.Cleanser:
                    Cleanser.Remove(player);
                    break;
                case CustomRoles.Jailer:
                    Jailer.Remove(pc.PlayerId);
                    break;
                case CustomRoles.Deputy:
                    Deputy.Remove(player);
                    break;
                case CustomRoles.Inspector:
                    Inspector.Remove(player);
                    break;
                case CustomRoles.Medic:
                    Medic.Remove(player);
                    break;
                case CustomRoles.Mediumshiper:
                    Mediumshiper.Remove(player);
                    break;
                case CustomRoles.Merchant:
                    Merchant.Remove(player);
                    break;
                case CustomRoles.Oracle:
                    Oracle.Remove(player);
                    break;
                case CustomRoles.Paranoia:
                    Main.ParaUsedButtonCount.Remove(player);
                    break;
                case CustomRoles.Snitch:
                    Snitch.Remove(player);
                    break;
                case CustomRoles.Counterfeiter:
                    Counterfeiter.Remove(player);
                    break;
                case CustomRoles.SwordsMan:
                    SwordsMan.Remove(player);
                    break;
                case CustomRoles.Sheriff:
                    Sheriff.Remove(player);
                    break;
                case CustomRoles.Crusader:
                    Crusader.Remove(player);
                    break;
                case CustomRoles.Judge:
                    Judge.Remove(player);
                    break;
                case CustomRoles.Divinator:
                    Divinator.Remove(pc.PlayerId);
                    break;
                case CustomRoles.Reverie:
                    Reverie.Remove(pc.PlayerId);
                    break;
                case CustomRoles.President:
                    President.Remove(pc.PlayerId);
                    break;
                case CustomRoles.Spy:
                    Spy.Remove(pc.PlayerId);
                    break;
                case CustomRoles.SabotageMaster:
                    SabotageMaster.Remove(pc.PlayerId);
                    break;
                case CustomRoles.Admirer:
                    Admirer.Remove(pc.PlayerId);
                    break;
                case CustomRoles.Benefactor:
                    Benefactor.Remove(pc.PlayerId);
                    break;
                case CustomRoles.Keeper:
                    Keeper.Remove(pc.PlayerId);
                    break;
                case CustomRoles.Swapper:
                    Swapper.Remove(pc.PlayerId);
                    break;
                case CustomRoles.GuessMaster:
                    GuessMaster.Remove(pc.PlayerId);
                    break;
                case CustomRoles.Enigma:
                    Enigma.Remove(pc.PlayerId);
                    break;
                case CustomRoles.Mortician:
                    Mortician.Remove(pc.PlayerId);
                    break;
                case CustomRoles.Bloodhound:
                    Bloodhound.Remove(pc.PlayerId);
                    break;
                case CustomRoles.Tracefinder:
                    Tracefinder.Remove(pc.PlayerId);
                    break;
                case CustomRoles.Spiritualist:
                    Spiritualist.Remove(pc.PlayerId);
                    break;
                case CustomRoles.Tracker:
                    Tracker.Remove(pc.PlayerId);
                    break;
                case CustomRoles.Monitor:
                    Monitor.Remove(pc.PlayerId);
                    break;
                case CustomRoles.Investigator:
                    Investigator.Remove(pc.PlayerId);
                    break;
                case CustomRoles.Farseer:
                    Farseer.Remove(pc.PlayerId);
                    break;
            }

            if (pc.GetCustomRole() != CustomRoles.Sidekick)
                pc.RpcSetCustomRole(CustomRoles.CopyCat);

            Main.PlayerStates[player].Role.SetKillCooldown(player);
        }
    }

    private static bool BlackList(CustomRoles role)
    {
        return role is CustomRoles.CopyCat or
            //bcoz of vent cd
            CustomRoles.Grenadier or
            CustomRoles.Lighter or
            CustomRoles.DovesOfNeace or
            CustomRoles.Veteran or
            CustomRoles.Bastion or
            CustomRoles.Addict or
            CustomRoles.Chameleon or
            CustomRoles.Alchemist or
            CustomRoles.TimeMaster or
            CustomRoles.Mole;
        //bcoz of single role
        // Other
    }
    private static bool Whitelist(CustomRoles role)
    {   ////////////           /*add the settings for new role*/            ////////////
        /* anything that is assigned in onGameStartedPatch.cs comes here */
        return role is CustomRoles.Cleanser or
            CustomRoles.Jailer or
            CustomRoles.Deputy or
            CustomRoles.Witness or
            CustomRoles.Inspector or
            CustomRoles.Medic or
            CustomRoles.Mediumshiper or
            CustomRoles.Merchant or
            CustomRoles.Oracle or
            CustomRoles.Paranoia or
            CustomRoles.Snitch or
            CustomRoles.Counterfeiter or
            CustomRoles.SwordsMan or
            CustomRoles.Sheriff or
            CustomRoles.Crusader or
            CustomRoles.Judge or
            CustomRoles.Divinator or
            CustomRoles.Reverie or
            CustomRoles.President or
            CustomRoles.Spy or
            CustomRoles.SabotageMaster or
            CustomRoles.Admirer or
            CustomRoles.Benefactor or
            CustomRoles.Keeper or
            CustomRoles.Swapper or
            CustomRoles.GuessMaster or
            CustomRoles.Enigma or
            CustomRoles.Mortician or
            CustomRoles.Bloodhound or
            CustomRoles.Tracefinder or
            CustomRoles.Spiritualist or
            CustomRoles.Tracker or
            CustomRoles.Monitor or
            CustomRoles.Investigator or
            CustomRoles.Farseer;
    }

    public override bool OnCheckMurderAsKiller(PlayerControl pc, PlayerControl tpc)
    {
        CustomRoles role = tpc.GetCustomRole();
        if (BlackList(role))
        {
            pc.Notify(GetString("CopyCatCanNotCopy"));
            pc.ResetKillCooldown();
            return false;
        }
        if (CopyCrewVar.GetBool())
        {
            switch (role)
            {
                case CustomRoles.Eraser:
                    role = CustomRoles.Cleanser;
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
                    role = CustomRoles.Reverie;
                    break;
                case CustomRoles.EvilGuesser:
                case CustomRoles.Doomsayer:
                    role = CustomRoles.NiceGuesser;
                    break;
                case CustomRoles.Taskinator:
                    role = CustomRoles.Benefactor;
                    break;
                case CustomRoles.EvilTracker:
                    role = CustomRoles.Tracker;
                    break;
                case CustomRoles.AntiAdminer:
                    role = CustomRoles.Monitor;
                    break;
                case CustomRoles.Pursuer:
                    role = CustomRoles.Counterfeiter;
                    break;
            }
        }
        if (role.IsCrewmate())
        {
            if (Whitelist(role)) role.GetRoleClass().Add(pc.PlayerId);

            pc.RpcSetCustomRole(role);
            if (CopyTeamChangingAddon.GetBool())
            {
                if (tpc.Is(CustomRoles.Madmate) || tpc.Is(CustomRoles.Rascal)) pc.RpcSetCustomRole(CustomRoles.Madmate);
                if (tpc.Is(CustomRoles.Charmed)) pc.RpcSetCustomRole(CustomRoles.Charmed);
                if (tpc.Is(CustomRoles.Infected)) pc.RpcSetCustomRole(CustomRoles.Infected);
                if (tpc.Is(CustomRoles.Recruit)) pc.RpcSetCustomRole(CustomRoles.Recruit);
                if (tpc.Is(CustomRoles.Contagious)) pc.RpcSetCustomRole(CustomRoles.Contagious);
                if (tpc.Is(CustomRoles.Soulless)) pc.RpcSetCustomRole(CustomRoles.Soulless);
            }

            pc.RpcGuardAndKill(pc);
            pc.Notify(string.Format(GetString("CopyCatRoleChange"), Utils.GetRoleName(role)));
            return false;
        }
        pc.Notify(GetString("CopyCatCanNotCopy"));
        pc.ResetKillCooldown();
        return false;
    }

    public override void SetAbilityButtonText(HudManager hud, byte id)
    {
        hud.ReportButton.OverrideText(GetString("ReportButtonText"));
        hud.KillButton.OverrideText(GetString("CopyButtonText"));
    }
}