using HarmonyLib;
using System.Linq;
using static TOHE.Options;

namespace TOHE.Roles.AddOns.Impostor;
public static class Tricky
{
    private static readonly int Id = 19900;
    private static OptionItem EnabledDeathReasons;
    //private static Dictionary<byte, PlayerState.DeathReason> randomReason = [];

    public static void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Tricky, canSetNum: true, tab: TabGroup.Addons);
        EnabledDeathReasons = BooleanOptionItem.Create(Id + 11, "OnlyEnabledDeathReasons", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Tricky]);
    }
    //public static void Init()
    //{
    //    randomReason = [];
    //}
    private static PlayerState.DeathReason ChangeRandomDeath()
    {
        PlayerState.DeathReason[] deathReasons = EnumHelper.GetAllValues<PlayerState.DeathReason>().Where(reason => reason.IsReasonEnabled()).ToArray();
        if (deathReasons.Length == 0 || !deathReasons.Contains(PlayerState.DeathReason.Kill)) deathReasons.AddItem(PlayerState.DeathReason.Kill);
        var random = IRandom.Instance;
        int randomIndex = random.Next(deathReasons.Length);
        return deathReasons[randomIndex];
    }
    private static bool IsReasonEnabled( this PlayerState.DeathReason reason)
    {
        if (reason is PlayerState.DeathReason.Disconnected or PlayerState.DeathReason.etc) return false;
        if (!EnabledDeathReasons.GetBool()) return true;
        return reason switch
        {
            PlayerState.DeathReason.Eaten => (CustomRoles.Pelican.IsEnable()),
            PlayerState.DeathReason.Spell => (CustomRoles.Witch.IsEnable()),
            PlayerState.DeathReason.Hex => (CustomRoles.HexMaster.IsEnable()),
            PlayerState.DeathReason.Curse => (CustomRoles.CursedWolf.IsEnable()),
            PlayerState.DeathReason.Jinx => (CustomRoles.Jinx.IsEnable()),
            PlayerState.DeathReason.Shattered => (CustomRoles.Lovers.IsEnable()),
            PlayerState.DeathReason.Bite => (CustomRoles.Vampire.IsEnable()),
            PlayerState.DeathReason.Poison => (CustomRoles.Poisoner.IsEnable()),
            PlayerState.DeathReason.Bombed => (CustomRoles.Bomber.IsEnable() || CustomRoles.Burst.IsEnable() 
                                || CustomRoles.Trapster.IsEnable() || CustomRoles.Fireworker.IsEnable() || CustomRoles.Bastion.IsEnable()),
            PlayerState.DeathReason.Misfire => (CustomRoles.ChiefOfPolice.IsEnable() || CustomRoles.Counterfeiter.IsEnable() 
                                || CustomRoles.Reverie.IsEnable() || CustomRoles.Sheriff.IsEnable() || CustomRoles.Fireworker.IsEnable() 
                                || CustomRoles.Hater.IsEnable() || CustomRoles.Pursuer.IsEnable() || CustomRoles.Romantic.IsEnable()),
            PlayerState.DeathReason.Torched => (CustomRoles.Arsonist.IsEnable()),
            PlayerState.DeathReason.Sniped => (CustomRoles.Sniper.IsEnable()),
            PlayerState.DeathReason.Revenge => (CustomRoles.Avanger.IsEnable() || CustomRoles.Retributionist.IsEnable() 
                                || CustomRoles.Nemesis.IsEnable() || CustomRoles.Randomizer.IsEnable()),
            PlayerState.DeathReason.Gambled => (CustomRoles.EvilGuesser.IsEnable() || CustomRoles.NiceGuesser.IsEnable() 
                                || GuesserMode.GetBool()),
            PlayerState.DeathReason.Quantization => (CustomRoles.Lightning.IsEnable()),
            //PlayerState.DeathReason.Overtired => (CustomRoles.Workaholic.IsEnable()),
            PlayerState.DeathReason.Ashamed => (CustomRoles.Workaholic.IsEnable()),
            PlayerState.DeathReason.PissedOff => (CustomRoles.Pestilence.IsEnable() || CustomRoles.Provocateur.IsEnable()),
            PlayerState.DeathReason.Dismembered => (CustomRoles.Butcher.IsEnable()),
            PlayerState.DeathReason.LossOfHead => (CustomRoles.Hangman.IsEnable()),
            PlayerState.DeathReason.Trialed => (CustomRoles.Judge.IsEnable() || CustomRoles.Councillor.IsEnable()),
            PlayerState.DeathReason.Infected => (CustomRoles.Infectious.IsEnable()),
            PlayerState.DeathReason.Hack => (CustomRoles.Glitch.IsEnable()),
            PlayerState.DeathReason.Pirate => (CustomRoles.Pirate.IsEnable()),
            PlayerState.DeathReason.Shrouded => (CustomRoles.Shroud.IsEnable()),
            PlayerState.DeathReason.Mauled => (CustomRoles.Werewolf.IsEnable()),
            PlayerState.DeathReason.Suicide => (CustomRoles.Unlucky.IsEnable() || CustomRoles.Ghoul.IsEnable() 
                                || CustomRoles.Terrorist.IsEnable() || CustomRoles.Dictator.IsEnable() 
                                || CustomRoles.Addict.IsEnable() || CustomRoles.Mercenary.IsEnable()
                                || CustomRoles.Mastermind.IsEnable() || CustomRoles.Deathpact.IsEnable()),
            PlayerState.DeathReason.FollowingSuicide => (CustomRoles.Lovers.IsEnable()),
            PlayerState.DeathReason.Execution => (CustomRoles.Jailer.IsEnable()),
            PlayerState.DeathReason.Fall => LadderDeath.GetBool(),
            PlayerState.DeathReason.Sacrifice => (CustomRoles.Bodyguard.IsEnable() || CustomRoles.Revolutionist.IsEnable() 
                                || CustomRoles.Hater.IsEnable()),
            PlayerState.DeathReason.Drained => CustomRoles.Puppeteer.IsEnable(),
            PlayerState.DeathReason.Trap => CustomRoles.Trapster.IsEnable(),
            PlayerState.DeathReason.Targeted => CustomRoles.Kamikaze.IsEnable(),
            PlayerState.DeathReason.Retribution => CustomRoles.Instigator.IsEnable(),
            PlayerState.DeathReason.WrongAnswer => CustomRoles.Quizmaster.IsEnable(),
            PlayerState.DeathReason.Disconnected or PlayerState.DeathReason.Overtired or PlayerState.DeathReason.etc => false,
            PlayerState.DeathReason.Kill or PlayerState.DeathReason.Vote => true,
            _ => true,
        };
    }
    public static void AfterPlayerDeathTasks(PlayerControl target)
    {
        if (target == null) return;
        _ = new LateTask(() =>
        {
            var killer = target.GetRealKiller();
            if (killer == null || !killer.Is(CustomRoles.Tricky)) return;
            
            Main.PlayerStates[target.PlayerId].deathReason = ChangeRandomDeath();
            Main.PlayerStates[target.PlayerId].SetDead();
            Utils.NotifyRoles(SpecifySeer: target);
        }, 0.3f, "Tricky random death reason");
    }
}