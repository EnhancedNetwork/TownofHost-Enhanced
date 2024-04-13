using Hazel;
using System;
using System.Text;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.Crewmate;

internal class FortuneTeller : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 8000;
    private static readonly HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Any();
    public override bool IsEnable => HasEnabled;
    public override CustomRoles ThisRoleBase => CustomRoles.Crewmate;
    //==================================================================\\

    private static OptionItem CheckLimitOpt;
    private static OptionItem AccurateCheckMode;
    private static OptionItem HidesVote;
    private static OptionItem ShowSpecificRole;
    private static OptionItem AbilityUseGainWithEachTaskCompleted;
    private static OptionItem RandomActiveRoles;


    private static readonly HashSet<byte> didVote = [];
    private static readonly Dictionary<byte, float> CheckLimit = [];
    private static readonly Dictionary<byte, float> TempCheckLimit = [];
    private static readonly Dictionary<byte, HashSet<byte>> targetList = [];


    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.FortuneTeller);
        CheckLimitOpt = IntegerOptionItem.Create(Id + 10, "FortuneTellerSkillLimit", new(0, 20, 1), 1, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.FortuneTeller])
            .SetValueFormat(OptionFormat.Times);
        RandomActiveRoles = BooleanOptionItem.Create(Id + 11, "RandomActiveRoles", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.FortuneTeller]);
        AccurateCheckMode = BooleanOptionItem.Create(Id + 12, "AccurateCheckMode", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.FortuneTeller]);
        ShowSpecificRole = BooleanOptionItem.Create(Id + 13, "ShowSpecificRole", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.FortuneTeller]);
        HidesVote = BooleanOptionItem.Create(Id + 14, "FortuneTellerHideVote", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.FortuneTeller]);
        AbilityUseGainWithEachTaskCompleted = FloatOptionItem.Create(Id + 15, "AbilityUseGainWithEachTaskCompleted", new(0f, 5f, 0.1f), 1f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.FortuneTeller])
            .SetValueFormat(OptionFormat.Times);
        OverrideTasksData.Create(Id + 20, TabGroup.CrewmateRoles, CustomRoles.FortuneTeller);
    }
    public override void Init()
    {
        playerIdList.Clear();
        CheckLimit.Clear();
        TempCheckLimit.Clear();
        targetList.Clear();
        didVote.Clear();
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        CheckLimit.TryAdd(playerId, CheckLimitOpt.GetInt());
        targetList[playerId] = [];
    }
    public override void Remove(byte playerId)
    {
        playerIdList.Remove(playerId);
        CheckLimit.Remove(playerId);
        targetList.Remove(playerId);
    }

    public static void SendRPC(byte playerId, bool isTemp = false, bool voted = false)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, -1);
        writer.WritePacked((int)CustomRoles.FortuneTeller);
        writer.Write(isTemp);

        if (!isTemp)
        {
            writer.Write(playerId);
            writer.Write(CheckLimit[playerId]);
            writer.Write(voted);
        }
        else
        {
            writer.Write(playerId);
            writer.Write(TempCheckLimit[playerId]);
        }
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public override void ReceiveRPC(MessageReader reader, PlayerControl NaN)
    {
        bool isTemp = reader.ReadBoolean();
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

    public override bool HideVote(PlayerVoteArea pva) => HidesVote.GetBool() && TempCheckLimit[pva.TargetPlayerId] > 0;
    private static string GetTargetRoleList(HashSet<CustomRoles> roles)
    {
        return string.Join("\n", roles.Select(role => $"    ★ {GetRoleName(role)}"));
    }
    public override bool OnTaskComplete(PlayerControl player, int completedTaskCount, int totalTaskCount)
    {
        if (player.Is(CustomRoles.FortuneTeller) && player.IsAlive())
        {
            CheckLimit[player.PlayerId] += AbilityUseGainWithEachTaskCompleted.GetFloat();
            SendRPC(player.PlayerId);
        }
        return true;
    }
    public override void OnVote(PlayerControl player, PlayerControl target)
    {
        if (player == null || target == null) return;
        if (didVote.Contains(player.PlayerId)) return;
        didVote.Add(player.PlayerId);

        if (CheckLimit[player.PlayerId] < 1)
        {
            SendMessage(GetString("FortuneTellerCheckReachLimit"), player.PlayerId, ColorString(GetRoleColor(CustomRoles.FortuneTeller), GetString("FortuneTellerCheckMsgTitle")));
            return;
        }

        if (RandomActiveRoles.GetBool())
        {
            if (!targetList.ContainsKey(player.PlayerId)) targetList[player.PlayerId] = [];
            if (targetList[player.PlayerId].Contains(target.PlayerId))
            {
                SendMessage(GetString("FortuneTellerAlreadyCheckedMsg") + "\n\n" + string.Format(GetString("FortuneTellerCheckLimit"), CheckLimit[player.PlayerId]), player.PlayerId, ColorString(GetRoleColor(CustomRoles.FortuneTeller), GetString("FortuneTellerCheckMsgTitle")));
                return;
            }
        }

        CheckLimit[player.PlayerId] -= 1;
        SendRPC(player.PlayerId, voted: true);

        if (player.PlayerId == target.PlayerId)
        {
            SendMessage(GetString("FortuneTellerCheckSelfMsg") + "\n\n" + string.Format(GetString("FortuneTellerCheckLimit"), CheckLimit[player.PlayerId]), player.PlayerId, ColorString(GetRoleColor(CustomRoles.FortuneTeller), GetString("FortuneTellerCheckMsgTitle")));
            return;
        }

        string msg;

        if ((player.AllTasksCompleted() || AccurateCheckMode.GetBool()) && ShowSpecificRole.GetBool())
        {
            msg = string.Format(GetString("FortuneTellerCheck.TaskDone"), target.GetRealName(), GetString(target.GetCustomRole().ToString()));
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
            msg = string.Format(GetString("FortuneTellerCheck.Result"), target.GetRealName(), text);
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
                CustomRoles.Telecommunication,
                CustomRoles.Dazzler,
                CustomRoles.Grenadier,
                CustomRoles.Imitator,
                CustomRoles.Bandit,
                CustomRoles.Lighter],

                [CustomRoles.Crusader,
                CustomRoles.Overseer,
                CustomRoles.Arsonist,
                CustomRoles.Ninja,
                CustomRoles.Lightning,
                CustomRoles.Collector,
                CustomRoles.Stealth],
                
                [CustomRoles.Deceiver,
                CustomRoles.Witness,
                CustomRoles.Greedy,
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
                CustomRoles.Arrogance,
                CustomRoles.KillingMachine,
                CustomRoles.Berserker,
                CustomRoles.Butcher],
                
                [CustomRoles.Coroner,
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
                CustomRoles.Fireworker,
                CustomRoles.RuthlessRomantic,
                CustomRoles.VengefulRomantic,
                CustomRoles.Lookout,
                CustomRoles.Nuker],
                
                [CustomRoles.BountyHunter,
                CustomRoles.Detective,
                CustomRoles.Hater,
                CustomRoles.Cleaner,
                CustomRoles.Medusa,
                CustomRoles.Psychic],
                
                [CustomRoles.Executioner,
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
                CustomRoles.Escapist,
                CustomRoles.Miner,
                CustomRoles.RiftMaker,
                CustomRoles.Bastion,
                CustomRoles.Mole,
                CustomRoles.Chronomancer,
                CustomRoles.Alchemist,
                CustomRoles.Morphling],
                
                [CustomRoles.Demon,
                CustomRoles.Zombie,
                CustomRoles.Celebrity,
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
                CustomRoles.Inspector],
                
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
                CustomRoles.Cultist,
                CustomRoles.Enigma,
                CustomRoles.PlagueDoctor],
                
                [CustomRoles.Innocent,
                CustomRoles.Masochist,
                CustomRoles.Inhibitor,
                CustomRoles.Mechanic,
                CustomRoles.Shaman,
                CustomRoles.Pixie,
                CustomRoles.Saboteur],
                
                [CustomRoles.Medic,
                CustomRoles.Vector,
                CustomRoles.Jester,
                CustomRoles.Lurker,
                CustomRoles.Swapper,
                CustomRoles.Sunnyboy,
                CustomRoles.Instigator],
                
                [CustomRoles.Nemesis,
                CustomRoles.Retributionist,
                CustomRoles.Necromancer,
                CustomRoles.Gangster,
                CustomRoles.Godfather,
                CustomRoles.Glitch,
                CustomRoles.Underdog],
                
                [CustomRoles.EvilGuesser,
                CustomRoles.NiceGuesser,
                CustomRoles.Doomsayer,
                CustomRoles.GuessMaster,
                CustomRoles.Stalker,
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
                CustomRoles.Pacifist,
                CustomRoles.SoulCatcher,
                CustomRoles.Huntsman,
                CustomRoles.Traitor],
                
                [CustomRoles.Trapster,
                CustomRoles.QuickShooter,
                CustomRoles.SerialKiller,
                CustomRoles.Sheriff,
                CustomRoles.Admirer,
                CustomRoles.Warlock],
                
                [CustomRoles.FortuneTeller,
                CustomRoles.Consigliere,
                CustomRoles.PotionMaster,
                //CustomRoles.Occultist, <-- Also removed from FortuneTeller LANG 
                CustomRoles.Kamikaze,
                CustomRoles.HexMaster,
                CustomRoles.Witch],
                
                [CustomRoles.LazyGuy,
                CustomRoles.Follower,
                CustomRoles.Pelican,
                CustomRoles.Scavenger,
                CustomRoles.Ludopath,
                CustomRoles.Vulture],
                
                [CustomRoles.Jinx,
                CustomRoles.Knight,
                CustomRoles.Veteran,
                CustomRoles.Pyromaniac,
                CustomRoles.TaskManager,
                CustomRoles.Shroud,
                CustomRoles.Hangman,
                CustomRoles.Pitfall],
                
                [CustomRoles.Medium,
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
                CustomRoles.Deputy,
                CustomRoles.Transporter,
                CustomRoles.Twister,
                CustomRoles.Mercenary,
                CustomRoles.Penguin],
                
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
                msg = string.Format(GetString("FortuneTellerCheck.Null"), target.GetRealName());
            }
            else
            {
                msg = string.Format(GetString("FortuneTellerCheck.Result"), target.GetRealName(), text);
            }
        }

        SendMessage(GetString("FortuneTellerCheck") + "\n" + msg + "\n\n" + string.Format(GetString("FortuneTellerCheckLimit"), CheckLimit[player.PlayerId]), player.PlayerId, ColorString(GetRoleColor(CustomRoles.FortuneTeller), GetString("FortuneTellerCheckMsgTitle")));
    }
    public override string GetProgressText(byte playerId, bool comms)
    {
        var ProgressText = new StringBuilder();
        var taskState4 = Main.PlayerStates?[playerId].TaskState;
        Color TextColor4;
        var TaskCompleteColor4 = Color.green;
        var NonCompleteColor4 = Color.yellow;
        var NormalColor4 = taskState4.IsTaskFinished ? TaskCompleteColor4 : NonCompleteColor4;
        TextColor4 = comms ? Color.gray : NormalColor4;
        string Completed4 = comms ? "?" : $"{taskState4.CompletedTasksCount}";
        Color TextColor41;
        if (CheckLimit[playerId] < 1) TextColor41 = Color.red;
        else TextColor41 = Color.white;
        ProgressText.Append(ColorString(TextColor4, $"({Completed4}/{taskState4.AllTasksCount})"));
        ProgressText.Append(ColorString(TextColor41, $" <color=#ffffff>-</color> {Math.Round(CheckLimit[playerId])}"));
        return ProgressText.ToString();
    }
    public override void OnReportDeadBody(PlayerControl reporter, PlayerControl target)
    {
        didVote.Clear();
        foreach (var FortuneTellerId in CheckLimit.Keys.ToArray())
        {
            TempCheckLimit[FortuneTellerId] = CheckLimit[FortuneTellerId];
            SendRPC(FortuneTellerId, isTemp: true);
        }
    }
}
