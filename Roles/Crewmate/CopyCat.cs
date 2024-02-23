using System.Collections.Generic;
using System.Linq;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate;

public static class CopyCat
{
    private static readonly int Id = 11500;
    public static List<byte> playerIdList = [];
    public static bool IsEnable = false;

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

    public static void Init()
    {
        playerIdList = [];
        CurrentKillCooldown = new();
        IsEnable = false;
    }

    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        CurrentKillCooldown = KillCooldown.GetFloat();
        IsEnable = true;

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }
    public static void Remove(byte playerId) //only to be used when copycat's role is going to be changed permanently
    {
        playerIdList.Remove(playerId);
        if (!playerIdList.Any()) IsEnable = false;
    }
    public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = Utils.GetPlayerById(id).IsAlive() ? CurrentKillCooldown : 300f;

    public static void AfterMeetingTasks()
    {
        foreach (var player in playerIdList.ToArray())
        {
            var pc = Utils.GetPlayerById(player);
            if (pc == null) continue;
            var role = pc.GetCustomRole();
            ////////////           /*remove the settings for current role*/             /////////////////////
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
                case CustomRoles.Mayor:
                    Main.MayorUsedButtonCount.Remove(player);
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

            SetKillCooldown(player);
        }
    }

    public static bool BlackList(this CustomRoles role)
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

    public static bool OnCheckMurder(PlayerControl pc, PlayerControl tpc)
    {
        CustomRoles role = tpc.GetCustomRole();
        if (role.BlackList())
        {
            pc.Notify(GetString("CopyCatCanNotCopy"));
            SetKillCooldown(pc.PlayerId);
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
            ////////////           /*add the settings for new role*/            ////////////
            /* anything that is assigned in onGameStartedPatch.cs comes here */
            switch (role)
            {
                case CustomRoles.Cleanser:
                    Cleanser.Add(pc.PlayerId);
                    break;
                case CustomRoles.Jailer:
                    Jailer.Add(pc.PlayerId);
                    break;
                case CustomRoles.Deputy:
                    Deputy.Add(pc.PlayerId);
                    break;
                case CustomRoles.Witness:
                    if (!AmongUsClient.Instance.AmHost) break;
                    if (!Main.ResetCamPlayerList.Contains(pc.PlayerId))
                        Main.ResetCamPlayerList.Add(pc.PlayerId);
                    break;
                case CustomRoles.Inspector:
                    Inspector.Add(pc.PlayerId);
                    break;
                case CustomRoles.Medic:
                    Medic.Add(pc.PlayerId);
                    break;
                case CustomRoles.Mediumshiper:
                    Mediumshiper.Add(pc.PlayerId);
                    break;
                case CustomRoles.Merchant:
                    Merchant.Add(pc.PlayerId);
                    break;
                case CustomRoles.Oracle:
                    Oracle.Add(pc.PlayerId);
                    break;
                case CustomRoles.Paranoia:
                    Main.ParaUsedButtonCount[pc.PlayerId] = 0;
                    break;
                case CustomRoles.Snitch:
                    Snitch.Add(pc.PlayerId);
                    break;

                case CustomRoles.Counterfeiter:
                    Counterfeiter.Add(pc.PlayerId);
                    break;
                case CustomRoles.SwordsMan:
                    SwordsMan.Add(pc.PlayerId);
                    break;
                case CustomRoles.Sheriff:
                    Sheriff.Add(pc.PlayerId);
                    break;
                case CustomRoles.Crusader:
                    Crusader.Add(pc.PlayerId);
                    break;
                case CustomRoles.Judge:
                    Judge.Add(pc.PlayerId);
                    break;
                case CustomRoles.Mayor:
                    Main.MayorUsedButtonCount[pc.PlayerId] = 0;
                    break;
                case CustomRoles.Divinator:
                    Divinator.Add(pc.PlayerId);
                    break;
                case CustomRoles.Reverie:
                    Reverie.Add(pc.PlayerId);
                    break;
                case CustomRoles.President:
                    President.Add(pc.PlayerId);
                    break;
                case CustomRoles.Spy:
                    Spy.Add(pc.PlayerId);
                    break;
                case CustomRoles.SabotageMaster:
                    SabotageMaster.Add(pc.PlayerId);
                    break;
                case CustomRoles.Admirer:
                    Admirer.Add(pc.PlayerId);
                    break;
                case CustomRoles.Benefactor:
                    Benefactor.Add(pc.PlayerId);
                    break;
                case CustomRoles.Keeper:
                    Keeper.Add(pc.PlayerId);
                    break;
                case CustomRoles.Swapper:
                    Swapper.Add(pc.PlayerId);
                    break;
                case CustomRoles.GuessMaster:
                    GuessMaster.Add(pc.PlayerId);
                    break;
                case CustomRoles.Enigma:
                    Enigma.Add(pc.PlayerId);
                    break;
                case CustomRoles.Mortician:
                    Mortician.Add(pc.PlayerId);
                    break;
                case CustomRoles.Bloodhound:
                    Bloodhound.Add(pc.PlayerId);
                    break;
                case CustomRoles.Tracefinder:
                    Tracefinder.Add(pc.PlayerId);
                    break;
                case CustomRoles.Spiritualist:
                    Spiritualist.Add(pc.PlayerId);
                    break;
                case CustomRoles.Tracker:
                    Tracker.Add(pc.PlayerId);
                    break;
                case CustomRoles.Monitor:
                    Monitor.Add(pc.PlayerId);
                    break;
                case CustomRoles.Investigator:
                    Investigator.Add(pc.PlayerId);
                    break;
                case CustomRoles.Farseer:
                    Farseer.Add(pc.PlayerId);
                    break;
            }

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
        SetKillCooldown(pc.PlayerId);
        return false;
    }
}