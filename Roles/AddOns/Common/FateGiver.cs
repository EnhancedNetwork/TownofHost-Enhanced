using static TOHE.Options;

namespace TOHE.Roles.AddOns.Common;

public class FateGiver : IAddon
{
    private const int Id = 29000;
    public AddonTypes Type => AddonTypes.Misc;

    public static Dictionary<byte, (byte, PlayerControl)> SkillRecords;
    public static OptionItem ImpCanBeFateGiver;
    public static OptionItem CrewCanBeFateGiver;
    public static OptionItem NeutralCanBeFateGiver;

    public static OptionItem NormalVoteChance;
    public static OptionItem DoubleVoteChance;
    public static OptionItem HalfVoteChance;
    public static OptionItem VoidBallotChance;
    public static OptionItem SelfVoteChance;

    public static OptionItem FunnyMode;
    public static OptionItem InfluencedChance;
    public static OptionItem StealerChance;
    public static OptionItem MayorChance;
    public static OptionItem VindicatorChance;
    public static OptionItem EraserChance;
    public static OptionItem FortuneTellerChance;
    public static OptionItem OracleChance;
    public static OptionItem CopyCatChance; // Copy random one's vote num
    public static OptionItem DictatorChance;

    public void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.FateGiver, canSetNum: true, tab: TabGroup.Addons);
        ImpCanBeFateGiver = BooleanOptionItem.Create(Id + 10, "ImpCanBeFateGiver", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.FateGiver]);
        CrewCanBeFateGiver = BooleanOptionItem.Create(Id + 11, "CrewCanBeFateGiver", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.FateGiver]);
        NeutralCanBeFateGiver = BooleanOptionItem.Create(Id + 12, "NeutralCanBeFateGiver", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.FateGiver]);

        NormalVoteChance = IntegerOptionItem.Create(Id + 13, "FGNormalVoteChance", (0, 100, 5), 0, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.FateGiver]);
        DoubleVoteChance = IntegerOptionItem.Create(Id + 14, "FGDoubleVoteChance", (0, 100, 5), 0, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.FateGiver]);
        HalfVoteChance = IntegerOptionItem.Create(Id + 15, "FGHalfVoteChance", (0, 100, 5), 0, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.FateGiver]);
        VoidBallotChance = IntegerOptionItem.Create(Id + 16, "FGVoidBallotChance", (0, 100, 5), 0, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.FateGiver]);
        SelfVoteChance = IntegerOptionItem.Create(Id + 17, "FGSelfVoteChance", (0, 100, 5), 0, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.FateGiver]);

        FunnyMode = BooleanOptionItem.Create(Id + 18, "FGFunnyMode", false, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.FateGiver]);
        InfluencedChance = IntegerOptionItem.Create(Id + 19, "FGInfluencedChance", (0, 100, 5), 0, TabGroup.Addons, false).SetParent(FunnyMode);
        StealerChance = IntegerOptionItem.Create(Id + 20, "FGStealerChance", (0, 100, 5), 0, TabGroup.Addons, false).SetParent(FunnyMode);
        MayorChance = IntegerOptionItem.Create(Id + 21, "FGMayorChance", (0, 100, 5), 0, TabGroup.Addons, false).SetParent(FunnyMode);
        VindicatorChance = IntegerOptionItem.Create(Id + 22, "FGVindicatorChance", (0, 100, 5), 0, TabGroup.Addons, false).SetParent(FunnyMode);
        EraserChance = IntegerOptionItem.Create(Id + 23, "FGEraserChance", (0, 100, 5), 0, TabGroup.Addons, false).SetParent(FunnyMode);
        FortuneTellerChance = IntegerOptionItem.Create(Id + 24, "FGFortuneTellerChance", (0, 100, 5), 0, TabGroup.Addons, false).SetParent(FunnyMode);
        OracleChance = IntegerOptionItem.Create(Id + 25, "FGOracleChance", (0, 100, 5), 0, TabGroup.Addons, false).SetParent(FunnyMode);
        CopyCatChance = IntegerOptionItem.Create(Id + 26, "FGCopyCatChance", (0, 100, 5), 0, TabGroup.Addons, false).SetParent(FunnyMode);
        DictatorChance = IntegerOptionItem.Create(Id + 27, "FGDictatorChance", (0, 100, 5), 0, TabGroup.Addons, false).SetParent(FunnyMode);
    }

    public static byte pickskill()
    {
        // Normal Vote -> Dictator Vote, 0 -> 13
        int[] chances =
        [
        NormalVoteChance.GetInt(),
        DoubleVoteChance.GetInt(),
        HalfVoteChance.GetInt(),
        VoidBallotChance.GetInt(),
        SelfVoteChance.GetInt()
        ];

        int randompool = chances.Sum();

        if (FunnyMode.GetBool())
        {
            int[] funnyChances =
            [
            InfluencedChance.GetInt(),
            StealerChance.GetInt(),
            MayorChance.GetInt(),
            VindicatorChance.GetInt(),
            EraserChance.GetInt(),
            FortuneTellerChance.GetInt(),
            OracleChance.GetInt(),
            CopyCatChance.GetInt(),
            DictatorChance.GetInt()
            ];
            chances = [.. chances, .. funnyChances]; // GPT told me to do this lol
            randompool += funnyChances.Sum();
        }

        if (randompool == 0) return 0;

        int randomint = IRandom.Instance.Next(0, randompool);

        for (byte i = 0; i < chances.Length; i++)
        {
            if (randomint < chances[i])
                return i;

            randomint -= chances[i];
        }

        return 0; // Default fallback, should never be hit if randompool is correct.
    }

    public static void OnVote(PlayerControl votePlayer, PlayerControl voteTarget = null)
    {
        var skill = pickskill();

        if (voteTarget == null)
        {
            for (int i = 0; i < 10; i++)
            {
                if (skill is not (9 or 10 or 11 or 13))
                {
                    break;
                }

                skill = pickskill();
            }

            if (skill is (9 or 10 or 11 or 13))
            {
                skill = 0;
            }
        }

        Logger.Info($"{votePlayer.PlayerId} + {votePlayer.GetNameWithRole()} used skill {skill} on {voteTarget?.PlayerId ?? 253}", "FateGiver");
        if (SkillRecords.ContainsKey(votePlayer.PlayerId))
        {
            SkillRecords[votePlayer.PlayerId] = (skill, voteTarget);
        }
        else
        {
            SkillRecords.Add(votePlayer.PlayerId, (skill, voteTarget));
        }
    }
}
