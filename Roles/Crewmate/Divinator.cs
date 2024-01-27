using Hazel;
using System;
using System.Collections.Generic;
using System.Linq;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate;

public static class Divinator
{
    private static readonly int Id = 8000;
    private static List<byte> playerIdList = [];
    public static bool IsEnable = false;

    public static OptionItem CheckLimitOpt;
    public static OptionItem AccurateCheckMode;
    public static OptionItem HideVote;
    public static OptionItem ShowSpecificRole;
    public static OptionItem AbilityUseGainWithEachTaskCompleted;
    public static OptionItem RandomActiveRoles;


    public static HashSet<byte> didVote = [];
    public static Dictionary<byte, float> CheckLimit = [];
    public static Dictionary<byte, float> TempCheckLimit = [];
    public static Dictionary<byte, HashSet<byte>> targetList = [];


    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Divinator);
        CheckLimitOpt = IntegerOptionItem.Create(Id + 10, "DivinatorSkillLimit", new(0, 20, 1), 1, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Divinator])
            .SetValueFormat(OptionFormat.Times);
        RandomActiveRoles = BooleanOptionItem.Create(Id + 11, "RandomActiveRoles", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Divinator]);
        AccurateCheckMode = BooleanOptionItem.Create(Id + 12, "AccurateCheckMode", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Divinator]);
        ShowSpecificRole = BooleanOptionItem.Create(Id + 13, "ShowSpecificRole", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Divinator]);
        HideVote = BooleanOptionItem.Create(Id + 14, "DivinatorHideVote", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Divinator]);
        AbilityUseGainWithEachTaskCompleted = FloatOptionItem.Create(Id + 15, "AbilityUseGainWithEachTaskCompleted", new(0f, 5f, 0.1f), 1f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Divinator])
            .SetValueFormat(OptionFormat.Times);
        OverrideTasksData.Create(Id + 20, TabGroup.CrewmateRoles, CustomRoles.Divinator);
    }
    public static void Init()
    {
        playerIdList = [];
        CheckLimit = [];
        TempCheckLimit = [];
        IsEnable = false;
        targetList = [];
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        CheckLimit.TryAdd(playerId, CheckLimitOpt.GetInt());
        IsEnable = true;
        targetList[playerId] = [];

    }
    public static void Remove(byte playerId)
    {
        playerIdList.Remove(playerId);
        CheckLimit.Remove(playerId);
        targetList.Remove(playerId);
    }

    public static void SendRPC(byte playerId, bool isTemp = false, bool voted = false)
    {
        MessageWriter writer;
        if (!isTemp)
        {
            writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetDivinatorLimit, SendOption.Reliable, -1);
            writer.Write(playerId);
            writer.Write(CheckLimit[playerId]);
            writer.Write(voted);
        }
        else
        {
            writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetDivinatorTempLimit, SendOption.Reliable, -1);
            writer.Write(playerId);
            writer.Write(TempCheckLimit[playerId]);
        }
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader, bool isTemp = false)
    {
        byte playerId = reader.ReadByte();
        float limit = reader.ReadSingle();
        if (!isTemp)
        {
            CheckLimit[playerId] = limit;
            bool voted = reader.ReadBoolean();
            if (voted && !didVote.Contains(playerId)) didVote.Add(playerId);
        }
        else
        {
            TempCheckLimit[playerId] = limit;
            didVote.Remove(playerId);
        }
    }

    public static string GetTargetRoleList(HashSet<CustomRoles> roles)
    {
        return string.Join("\n", roles.Select(role => $"    ★ {Utils.GetRoleName(role)}"));
    }

    public static void OnVote(PlayerControl player, PlayerControl target)
    {
        if (player == null || target == null) return;
        if (didVote.Contains(player.PlayerId)) return;
        didVote.Add(player.PlayerId);

        if (CheckLimit[player.PlayerId] < 1)
        {
            Utils.SendMessage(GetString("DivinatorCheckReachLimit"), player.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Divinator), GetString("DivinatorCheckMsgTitle")));
            return;
        }

        if (RandomActiveRoles.GetBool())
        {
            if (!targetList.ContainsKey(player.PlayerId)) targetList[player.PlayerId] = [];
            if (targetList[player.PlayerId].Contains(target.PlayerId))
            {
                Utils.SendMessage(GetString("DivinatorAlreadyCheckedMsg") + "\n\n" + string.Format(GetString("DivinatorCheckLimit"), CheckLimit[player.PlayerId]), player.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Divinator), GetString("DivinatorCheckMsgTitle")));
                return;
            }
        }

        CheckLimit[player.PlayerId] -= 1;
        SendRPC(player.PlayerId, voted: true);

        if (player.PlayerId == target.PlayerId)
        {
            Utils.SendMessage(GetString("DivinatorCheckSelfMsg") + "\n\n" + string.Format(GetString("DivinatorCheckLimit"), CheckLimit[player.PlayerId]), player.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Divinator), GetString("DivinatorCheckMsgTitle")));
            return;
        }

        string msg;

        if ((player.AllTasksCompleted() || AccurateCheckMode.GetBool()) && ShowSpecificRole.GetBool())
        {
            msg = string.Format(GetString("DivinatorCheck.TaskDone"), target.GetRealName(), GetString(target.GetCustomRole().ToString()));
        }
        else if (RandomActiveRoles.GetBool())
        {
            if (!targetList.ContainsKey(player.PlayerId)) targetList[player.PlayerId] = [];
            targetList[player.PlayerId].Add(target.PlayerId);
            var targetRole = target.GetCustomRole();
            var activeRoleList = CustomRolesHelper.AllRoles.Where(role => (role.IsEnable() || role.RoleExist(countDead: true)) && role != targetRole && !role.IsAdditionRole()).ToList();
            var count = Math.Min(4, activeRoleList.Count);
            List<CustomRoles> roleList = [targetRole];
            var rand = IRandom.Instance;
            for (int i = 0; i < count; i++)
            {
                int randomIndex = rand.Next(activeRoleList.Count);
                roleList.Add(activeRoleList[randomIndex]);
                activeRoleList.RemoveAt(randomIndex);
            }
            for (int i = roleList.Count - 1; i > 0; i--)
            {
                int j = rand.Next(0, i + 1);
                (roleList[j], roleList[i]) = (roleList[i], roleList[j]);
            }
            var text = GetTargetRoleList([.. roleList]);
            msg = string.Format(GetString("DivinatorCheck.Result"), target.GetRealName(), text);
        }
        else
        {
            HashSet<HashSet<CustomRoles>> completeRoleList =  [[CustomRoles.CrewmateTOHE,
                CustomRoles.EngineerTOHE,
                CustomRoles.ScientistTOHE,
                CustomRoles.ImpostorTOHE,
                CustomRoles.ShapeshifterTOHE],

                [CustomRoles.Amnesiac,
                CustomRoles.CopyCat,
                CustomRoles.Eraser,
                CustomRoles.Refugee,
                CustomRoles.AntiAdminer,
                CustomRoles.Monitor,
                CustomRoles.Dazzler,
                CustomRoles.Grenadier,
                CustomRoles.Imitator,
                CustomRoles.Bandit,
                CustomRoles.Lighter],

                [CustomRoles.Crusader,
                CustomRoles.Farseer,
                CustomRoles.Arsonist,
                CustomRoles.Assassin,
                CustomRoles.BallLightning,
                CustomRoles.Collector],
                
                [CustomRoles.Capitalism,
                CustomRoles.Counterfeiter,
                CustomRoles.Witness,
                CustomRoles.Greedier,
                CustomRoles.Merchant,
                CustomRoles.SoulCollector,
                CustomRoles.Trickster], 
                
                [CustomRoles.Pestilence,
                CustomRoles.PlagueBearer,
                CustomRoles.Observer,
                CustomRoles.BloodKnight,
                CustomRoles.Guardian,
                CustomRoles.Wildling],
                
                [CustomRoles.Bard,
                CustomRoles.Juggernaut,
                CustomRoles.Reverie,
                CustomRoles.Vigilante,
                CustomRoles.Sans,
                CustomRoles.Minimalism,
                CustomRoles.Berserker,
                CustomRoles.OverKiller],
                
                [CustomRoles.Bloodhound,
                CustomRoles.EvilTracker,
                CustomRoles.Mortician,
                CustomRoles.Tracefinder,
                CustomRoles.Seeker,
                CustomRoles.Tracker,
                CustomRoles.Romantic, 
                CustomRoles.SchrodingersCat], 
                
                [CustomRoles.Bodyguard,
                CustomRoles.Bomber,
                CustomRoles.Agitater,
                CustomRoles.FireWorks,
                CustomRoles.RuthlessRomantic,
                CustomRoles.VengefulRomantic,
                CustomRoles.Lookout,
                CustomRoles.Nuker],
                
                [CustomRoles.BountyHunter,
                CustomRoles.Detective,
                CustomRoles.FFF,
                CustomRoles.Cleaner,
                CustomRoles.Medusa,
                CustomRoles.Psychic],
                
                [CustomRoles.Convict,
                CustomRoles.Executioner,
                CustomRoles.Lawyer,
                CustomRoles.Snitch,
                CustomRoles.Disperser,
                CustomRoles.Doctor],
                
                [CustomRoles.Councillor,
                CustomRoles.Dictator,
                CustomRoles.Judge,
                CustomRoles.CursedSoul,
                CustomRoles.Cleanser,
                CustomRoles.CursedWolf,
                CustomRoles.President,
                CustomRoles.Keeper],
                
                [CustomRoles.Addict,
                CustomRoles.Escapee,
                CustomRoles.Miner,
                CustomRoles.RiftMaker,
                CustomRoles.Bastion,
                CustomRoles.Mole,
                CustomRoles.Chronomancer,
                CustomRoles.Alchemist,
                CustomRoles.Morphling],
                
                [CustomRoles.Gamer,
                CustomRoles.Zombie,
                CustomRoles.CyberStar,
                CustomRoles.SuperStar,
                CustomRoles.Captain,
                CustomRoles.Deathpact,
                CustomRoles.Investigator,
                CustomRoles.Devourer],
                
                [CustomRoles.God,
                CustomRoles.Oracle,
                CustomRoles.Pirate,
                CustomRoles.Visionary,
                CustomRoles.Blackmailer,
                CustomRoles.ParityCop],
                
                [CustomRoles.Anonymous,
                CustomRoles.Mayor,
                CustomRoles.Paranoia,
                CustomRoles.Mastermind,
                CustomRoles.Pickpocket,
                CustomRoles.Spy,
                CustomRoles.Randomizer,
                CustomRoles.Vindicator],
                
                [CustomRoles.Infectious,
                CustomRoles.Virus,
                CustomRoles.Monarch,
                CustomRoles.Revolutionist,
                CustomRoles.Succubus,
                CustomRoles.Enigma],
                
                [CustomRoles.Innocent,
                CustomRoles.Masochist,
                CustomRoles.Inhibitor,
                CustomRoles.SabotageMaster,
                CustomRoles.Shaman,
                CustomRoles.Pixie,
                CustomRoles.Saboteur],
                
                [CustomRoles.Medic,
                CustomRoles.Mario,
                CustomRoles.Jester,
                CustomRoles.Lurker,
                CustomRoles.Swapper,
                CustomRoles.Sunnyboy,
                CustomRoles.Instigator],
                
                [CustomRoles.Mafia,
                CustomRoles.Retributionist,
                CustomRoles.Necromancer,
                CustomRoles.Gangster,
                CustomRoles.Godfather,
                CustomRoles.Glitch,
                //CustomRoles.Luckey,
                CustomRoles.Underdog],
                
                [CustomRoles.EvilGuesser,
                CustomRoles.NiceGuesser,
                CustomRoles.Doomsayer,
                CustomRoles.GuessMaster,
                CustomRoles.DarkHide,
                CustomRoles.Camouflager,
                CustomRoles.Chameleon,
                CustomRoles.Doppelganger],
                
                [CustomRoles.Jackal,
                CustomRoles.Jailer,
                CustomRoles.Sidekick,
                CustomRoles.Maverick,
                CustomRoles.Opportunist,
                CustomRoles.Pursuer,
                CustomRoles.Provocateur],
                
                [CustomRoles.Poisoner,
                CustomRoles.Vampire,
                CustomRoles.DovesOfNeace,
                CustomRoles.ImperiusCurse,
                CustomRoles.Huntsman,
                CustomRoles.Traitor],
                
                [CustomRoles.BoobyTrap,
                CustomRoles.QuickShooter,
                CustomRoles.NSerialKiller,
                CustomRoles.Sheriff,
                CustomRoles.Admirer,
                CustomRoles.Warlock],
                
                [CustomRoles.Divinator,
                CustomRoles.EvilDiviner,
                CustomRoles.PotionMaster,
                //CustomRoles.Occultist, <-- Also removed from divinator LANG 
                CustomRoles.Kamikaze,
                CustomRoles.HexMaster,
                CustomRoles.Witch],
                
                [CustomRoles.Needy,
                CustomRoles.Totocalcio,
                CustomRoles.Pelican,
                CustomRoles.Scavenger,
                CustomRoles.Ludopath,
                CustomRoles.Vulture],
                
                [CustomRoles.Jinx,
                CustomRoles.SwordsMan,
                CustomRoles.Veteran,
                CustomRoles.Pyromaniac,
                CustomRoles.TaskManager,
                CustomRoles.Shroud,
                CustomRoles.Hangman,
                CustomRoles.Pitfall],
                
                [CustomRoles.Mediumshiper,
                CustomRoles.Spiritcaller,
                CustomRoles.Spiritualist,
                CustomRoles.Parasite,
                CustomRoles.Swooper,
                CustomRoles.Wraith],
                
                [CustomRoles.TimeManager,
                CustomRoles.TimeMaster,
                CustomRoles.TimeThief,
                CustomRoles.ShapeMaster,
                CustomRoles.Werewolf,
                CustomRoles.Vampiress,
                CustomRoles.Sniper],
                
                [CustomRoles.Puppeteer,
                CustomRoles.NWitch,
                CustomRoles.Deputy,
                CustomRoles.Transporter,
                CustomRoles.Twister,
                CustomRoles.SerialKiller],
                
                [CustomRoles.Crewpostor,
                CustomRoles.Taskinator,
                CustomRoles.Benefactor,
                CustomRoles.Marshall,
                CustomRoles.Workaholic,
                CustomRoles.Phantom,
                CustomRoles.Solsticer,
                CustomRoles.NiceMini,
                CustomRoles.EvilMini,
                CustomRoles.Terrorist]];

            var targetRole = target.GetCustomRole();
            string text = string.Empty;
            foreach (var roleList in completeRoleList)
            {
                if (roleList.Contains(targetRole))
                {
                    text = GetTargetRoleList(roleList);
                    break;
                }
            }

            if (text == string.Empty)
            {
                msg = string.Format(GetString("DivinatorCheck.Null"), target.GetRealName());
            }
            else
            {
                msg = string.Format(GetString("DivinatorCheck.Result"), target.GetRealName(), text);
            }
        }
        // Fortune Teller
        /*   {
               string text = target.GetCustomRole() switch
               {
                   CustomRoles.TimeThief or
                   CustomRoles.AntiAdminer or
                   CustomRoles.SuperStar or
                   CustomRoles.Mayor or
                   CustomRoles.Vindicator or
                   CustomRoles.Snitch or
                   CustomRoles.Marshall or
                   CustomRoles.Counterfeiter or
                   CustomRoles.God or
                   CustomRoles.Judge or
                   CustomRoles.Observer or
                   CustomRoles.DovesOfNeace or
                   CustomRoles.Virus
                   => "HideMsg",

                   CustomRoles.Miner or
                   CustomRoles.Scavenger or
                   //CustomRoles.Luckey or
                   CustomRoles.Trickster or
                   CustomRoles.Needy or
                   CustomRoles.SabotageMaster or
                   CustomRoles.EngineerTOHE or
                   CustomRoles.Jackal or
                   CustomRoles.Parasite or
                   CustomRoles.Impostor or
               //    CustomRoles.Sidekick or
                   CustomRoles.Mario or
                   CustomRoles.Cleaner or
                   CustomRoles.Crewpostor or
                   CustomRoles.Disperser
                   => "Honest",

                   CustomRoles.SerialKiller or
                   CustomRoles.BountyHunter or
                   CustomRoles.Minimalism or
                   CustomRoles.Sans or
                   CustomRoles.Juggernaut or
                   CustomRoles.SpeedBooster or
                   CustomRoles.Sheriff or
                   CustomRoles.Arsonist or
                   CustomRoles.Innocent or
                   CustomRoles.FFF or
                   CustomRoles.Greedier or
                   CustomRoles.Tracker
                   => "Impulse",

                   CustomRoles.Vampire or
                   CustomRoles.Poisoner or
                   CustomRoles.Assassin or
                   CustomRoles.Escapee or
                   CustomRoles.Sniper or
                   CustomRoles.NSerialKiller or
                   CustomRoles.SwordsMan or
                   CustomRoles.Bodyguard or
                   CustomRoles.Opportunist or
                   CustomRoles.Pelican or
                   CustomRoles.ImperiusCurse
                   => "Weirdo",

                   CustomRoles.EvilGuesser or
                   CustomRoles.Bomber or
                   CustomRoles.Capitalism or
                   CustomRoles.NiceGuesser or
                   CustomRoles.Grenadier or
                   CustomRoles.Terrorist or
                   CustomRoles.Revolutionist or
                   CustomRoles.Gamer or
                   CustomRoles.Eraser or
                   CustomRoles.Farseer
                   => "Blockbuster",

                   CustomRoles.Warlock or
                   CustomRoles.Anonymous or
                   CustomRoles.Mafia or
                   CustomRoles.Retributionist or
                   CustomRoles.Doctor or
                   CustomRoles.ScientistTOHE or
                   CustomRoles.Transporter or
                   CustomRoles.Veteran or
                   CustomRoles.Divinator or
                   CustomRoles.QuickShooter or
                   CustomRoles.Mediumshiper or
                   CustomRoles.Judge or
                   CustomRoles.Wildling or
                   CustomRoles.BloodKnight
                   => "Strong",

                   CustomRoles.Witch or
                   CustomRoles.HexMaster or
                   CustomRoles.Puppeteer or
                   CustomRoles.NWitch or
                   CustomRoles.ShapeMaster or
                   CustomRoles.ShapeshifterTOHE or
                   CustomRoles.Paranoia or
                   CustomRoles.Psychic or
                   CustomRoles.Executioner or
                   CustomRoles.Lawyer or
                   CustomRoles.BallLightning or
                   CustomRoles.Workaholic or
                   CustomRoles.Provocateur
                   => "Incomprehensible",

                   CustomRoles.FireWorks or
                   CustomRoles.EvilTracker or
                   CustomRoles.Gangster or
                   CustomRoles.Dictator or
                   CustomRoles.CyberStar or
                   CustomRoles.Collector or
                   CustomRoles.Sunnyboy or
                   CustomRoles.Bard or
                   CustomRoles.Totocalcio or
                   CustomRoles.Bloodhound
                   => "Enthusiasm",

                   CustomRoles.BoobyTrap or
                   CustomRoles.Zombie or
                   CustomRoles.Mare or
                   CustomRoles.Detective or
                   CustomRoles.TimeManager or
                   CustomRoles.Jester or
                   CustomRoles.Medicaler or
                   CustomRoles.GuardianAngelTOHE or
                   CustomRoles.DarkHide or
                   CustomRoles.CursedWolf or
                   CustomRoles.OverKiller or
                   CustomRoles.Hangman or
                   CustomRoles.Mortician or
                   CustomRoles.Spiritcaller
                   => "Disturbed",

                   CustomRoles.Glitch or
                   CustomRoles.Camouflager or
                   CustomRoles.Wraith or
                   CustomRoles.Swooper
                   => "Glitch",

                   CustomRoles.Succubus
                   => "Love",

                   _ => "None",
               };
               msg = string.Format(GetString("DivinatorCheck." + text), target.GetRealName());
           }*/

        Utils.SendMessage(GetString("DivinatorCheck") + "\n" + msg + "\n\n" + string.Format(GetString("DivinatorCheckLimit"), CheckLimit[player.PlayerId]), player.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Divinator), GetString("DivinatorCheckMsgTitle")));
    }
    public static void OnReportDeadBody()
    {
        didVote.Clear();
        foreach (var divinatorId in CheckLimit.Keys.ToArray())
        {
            TempCheckLimit[divinatorId] = CheckLimit[divinatorId];
            SendRPC(divinatorId, isTemp: true);
        }
    }
}
