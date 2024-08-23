using UnityEngine;
using TOHE.Modules;
using TOHE.Roles.Impostor;
using static TOHE.Options;
using static TOHE.Translator;
using AmongUs.GameOptions;
using TOHE.Roles.Core;

namespace TOHE.Roles.Neutral;

internal class Berserker : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 600;

    private static readonly HashSet<byte> PlayerIds = [];
    public static bool HasEnabled => PlayerIds.Any();
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralApocalypse;
    //==================================================================\\

    private static OptionItem BerserkerKillCooldown;
    private static OptionItem BerserkerMax;
    private static OptionItem BerserkerOneCanKillCooldown;
    private static OptionItem BerserkerKillCooldownLevel;
    private static OptionItem BerserkerOneKillCooldown;
    private static OptionItem BerserkerTwoCanScavenger;
    private static OptionItem BerserkerScavengerLevel;
    private static OptionItem BerserkerThreeCanBomber;
    private static OptionItem BerserkerBomberLevel;
    //public static OptionItem BerserkerFourCanFlash;
    //public static OptionItem BerserkerSpeed;
    private static OptionItem BerserkerFourCanNotKill;
    private static OptionItem BerserkerImmortalLevel;
    public static OptionItem WarKillCooldown;
    private static OptionItem BerserkerHasImpostorVision;
    public static OptionItem WarHasImpostorVision;
    private static OptionItem BerserkerCanVent;
    public static OptionItem WarCanVent;

    private static readonly Dictionary<byte, int> BerserkerKillMax = [];

    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Berserker, 1, zeroOne: false);
        BerserkerKillCooldown = FloatOptionItem.Create(Id + 2, "BerserkerKillCooldown", new(25f, 250f, 2.5f), 35f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Berserker])
            .SetValueFormat(OptionFormat.Seconds);
        BerserkerMax = IntegerOptionItem.Create(Id + 3, "BerserkerMax", new(1, 10, 1), 4, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Berserker])
            .SetValueFormat(OptionFormat.Level);
        BerserkerHasImpostorVision = BooleanOptionItem.Create(Id + 15, "BerserkerHasImpostorVision", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Berserker]);
        WarHasImpostorVision = BooleanOptionItem.Create(Id + 16, "WarHasImpostorVision", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Berserker]);
        BerserkerCanVent = BooleanOptionItem.Create(Id + 17, "BerserkerCanVent", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Berserker]);
        WarCanVent = BooleanOptionItem.Create(Id + 18, "WarCanVent", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Berserker]);
        BerserkerOneCanKillCooldown = BooleanOptionItem.Create(Id + 4, "BerserkerOneCanKillCooldown", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Berserker]);
        BerserkerOneKillCooldown = FloatOptionItem.Create(Id + 5, "BerserkerOneKillCooldown", new(10f, 45f, 2.5f), 15f, TabGroup.NeutralRoles, false).SetParent(BerserkerOneCanKillCooldown)
            .SetValueFormat(OptionFormat.Seconds);
        BerserkerKillCooldownLevel = IntegerOptionItem.Create(Id + 6, "BerserkerLevelRequirement", new(1, 10, 1), 1, TabGroup.NeutralRoles, false).SetParent(BerserkerOneCanKillCooldown)
            .SetValueFormat(OptionFormat.Level);
        BerserkerTwoCanScavenger = BooleanOptionItem.Create(Id + 7, "BerserkerTwoCanScavenger", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Berserker]);
        BerserkerScavengerLevel = IntegerOptionItem.Create(Id + 8, "BerserkerLevelRequirement", new(1, 10, 1), 2, TabGroup.NeutralRoles, false).SetParent(BerserkerTwoCanScavenger)
            .SetValueFormat(OptionFormat.Level);
        BerserkerThreeCanBomber = BooleanOptionItem.Create(Id + 9, "BerserkerThreeCanBomber", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Berserker]);
        BerserkerBomberLevel = IntegerOptionItem.Create(Id + 10, "BerserkerLevelRequirement", new(1, 10, 1), 3, TabGroup.NeutralRoles, false).SetParent(BerserkerThreeCanBomber)
            .SetValueFormat(OptionFormat.Level);
        //BerserkerFourCanFlash = BooleanOptionItem.Create(Id + 11, "BerserkerFourCanFlash", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Berserker]);
        //BerserkerSpeed = FloatOptionItem.Create(611, "BerserkerSpeed", new(1.5f, 5f, 0.25f), 2.5f, TabGroup.NeutralRoles, false).SetParent(BerserkerOneCanKillCooldown)
        //    .SetValueFormat(OptionFormat.Multiplier);
        BerserkerFourCanNotKill = BooleanOptionItem.Create(Id + 12, "BerserkerFourCanNotKill", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Berserker]);
        BerserkerImmortalLevel = IntegerOptionItem.Create(Id + 13, "BerserkerLevelRequirement", new(1, 10, 1), 4, TabGroup.NeutralRoles, false).SetParent(BerserkerFourCanNotKill)
            .SetValueFormat(OptionFormat.Level);
        WarKillCooldown = FloatOptionItem.Create(Id + 14, "WarKillCooldown", new(0f, 150f, 2.5f), 15f, TabGroup.NeutralRoles, false).SetParent(BerserkerFourCanNotKill)
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void Init()
    {
        BerserkerKillMax.Clear();
        PlayerIds.Clear();
    }
    public override void Add(byte playerId)
    {
        BerserkerKillMax[playerId] = 0;
        PlayerIds.Add(playerId);
    }
    public override void Remove(byte playerId)
    {
        BerserkerKillMax.Remove(playerId);
    }

    public override bool OthersKnowTargetRoleColor(PlayerControl seer, PlayerControl target) 
        => KnowRoleTarget(seer, target);
    public override bool KnowRoleTarget(PlayerControl seer, PlayerControl target) 
        => (target.IsNeutralApocalypse() && seer.IsNeutralApocalypse());
    public override string GetProgressText(byte playerId, bool cvooms) => Utils.ColorString(Utils.GetRoleColor(CustomRoles.Berserker).ShadeColor(0.25f), BerserkerKillMax.TryGetValue(playerId, out var x) ? $"({x}/{BerserkerMax.GetInt()})" : "Invalid");
    public override void SetKillCooldown(byte id) 
        => Main.AllPlayerKillCooldown[id] = BerserkerKillCooldown.GetFloat();
    public override bool CanUseImpostorVentButton(PlayerControl pc) => BerserkerCanVent.GetBool();
    public override void ApplyGameOptions(IGameOptions opt, byte playerId) 
        => opt.SetVision(BerserkerHasImpostorVision.GetBool());
    public override bool CanUseKillButton(PlayerControl pc) => pc.IsAlive();
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (target.IsNeutralApocalypse()) return false;
        bool noScav = true;
        if (BerserkerKillMax[killer.PlayerId] < BerserkerMax.GetInt())
        {
            BerserkerKillMax[killer.PlayerId]++;
            killer.Notify(string.Format(GetString("BerserkerLevelChanged"), BerserkerKillMax[killer.PlayerId]));
            Logger.Info($"Increased the lvl to {BerserkerKillMax[killer.PlayerId]}", "CULTIVATOR");
        }
        else
        {
            killer.Notify(GetString("BerserkerMaxReached"));
            Logger.Info($"Max level reached lvl =  {BerserkerKillMax[killer.PlayerId]}", "CULTIVATOR");

        }

        if (BerserkerKillMax[killer.PlayerId] >= BerserkerKillCooldownLevel.GetInt() && BerserkerOneCanKillCooldown.GetBool())
        {
            Main.AllPlayerKillCooldown[killer.PlayerId] = BerserkerOneKillCooldown.GetFloat();
            killer.SetKillCooldown();
        }

        if (BerserkerKillMax[killer.PlayerId] >= BerserkerScavengerLevel.GetInt() && BerserkerTwoCanScavenger.GetBool())
        {
            killer.RpcTeleport(target.GetCustomPosition());
            RPC.PlaySoundRPC(killer.PlayerId, Sounds.KillSound);
            target.RpcTeleport(ExtendedPlayerControl.GetBlackRoomPosition());
            
            Main.PlayerStates[target.PlayerId].SetDead();
            target.RpcMurderPlayer(target);
            target.SetRealKiller(killer);

            killer.SetKillCooldownV2();
            target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Berserker), GetString("KilledByBerserker")));
            noScav = false;
        }

        if (BerserkerKillMax[killer.PlayerId] >= BerserkerBomberLevel.GetInt() && BerserkerThreeCanBomber.GetBool())
        {
            Logger.Info("炸弹爆炸了", "Boom");
            CustomSoundsManager.RPCPlayCustomSoundAll("Boom");
            foreach (var player in Main.AllAlivePlayerControls)
            {
                if (!player.IsModClient())
                    player.KillFlash();

                if (player == killer) continue;
                if (player == target) continue;

                if (Utils.GetDistance(killer.transform.position, player.transform.position) <= Bomber.BomberRadius.GetFloat())
                {
                    Main.PlayerStates[player.PlayerId].deathReason = PlayerState.DeathReason.Bombed;
                    player.RpcMurderPlayer(player);
                    player.SetRealKiller(killer);
                }
            }
        }
        if (BerserkerKillMax[killer.PlayerId] >= BerserkerImmortalLevel.GetInt() && BerserkerFourCanNotKill.GetBool()&& !killer.Is(CustomRoles.War))
        {
            killer.RpcSetCustomRole(CustomRoles.War);
            killer.Notify(GetString("BerserkerToWar"));
            Main.AllPlayerKillCooldown[killer.PlayerId] = WarKillCooldown.GetFloat();
        }

        return noScav;
    }
    public override bool OnRoleGuess(bool isUI, PlayerControl target, PlayerControl guesser, CustomRoles role, ref bool guesserSuicide)
    {
        if (!ApocCanGuessApoc.GetBool() && target.IsNeutralApocalypse() && guesser.IsNeutralApocalypse())
        {
            guesser.ShowInfoMessage(isUI, GetString("GuessApocRole"));
            return true;
        }
        return false;
    }
}
internal class War : RoleBase
{
    //===========================SETUP================================\\
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Berserker);
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralApocalypse;
    //==================================================================\\

    public override bool OthersKnowTargetRoleColor(PlayerControl seer, PlayerControl target) 
        => KnowRoleTarget(seer, target);
    public override bool KnowRoleTarget(PlayerControl seer, PlayerControl target)
        => (target.IsNeutralApocalypse() && seer.IsNeutralApocalypse());
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = Berserker.WarKillCooldown.GetFloat();
    public override void ApplyGameOptions(IGameOptions opt, byte playerId) => opt.SetVision(Berserker.WarHasImpostorVision.GetBool());
    public override bool CanUseKillButton(PlayerControl pc) => true;
    public override bool CanUseImpostorVentButton(PlayerControl pc) => Berserker.WarCanVent.GetBool();
    public override bool OnCheckMurderAsTarget(PlayerControl killer, PlayerControl target) => false;
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        return CustomRoles.Berserker.GetStaticRoleClass().OnCheckMurderAsKiller(killer, target);
    }
    public override bool OnRoleGuess(bool isUI, PlayerControl target, PlayerControl guesser, CustomRoles role, ref bool guesserSuicide)
    {
        if (!TransformedNeutralApocalypseCanBeGuessed.GetBool())
        {
            guesser.ShowInfoMessage(isUI, GetString("GuessImmune"));
            return true;
        }
        return false;
    }
}