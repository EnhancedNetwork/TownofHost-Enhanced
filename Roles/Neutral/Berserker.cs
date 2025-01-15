using AmongUs.GameOptions;
using Hazel;
using InnerNet;
using TOHE.Modules;
using TOHE.Roles.Core;
using TOHE.Roles.Impostor;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Neutral;

internal class Berserker : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Berserker;
    private const int Id = 600;
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralApocalypse;
    //==================================================================\\

    private static OptionItem BerserkerKillCooldown;
    private static OptionItem BerserkerCanKillTeamate;
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

    public static readonly Dictionary<byte, int> BerserkerKillMax = [];

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
        BerserkerOneCanKillCooldown = BooleanOptionItem.Create(Id + 5, "BerserkerOneCanKillCooldown", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Berserker]);
        BerserkerOneKillCooldown = FloatOptionItem.Create(Id + 6, "BerserkerOneKillCooldown", new(10f, 45f, 2.5f), 15f, TabGroup.NeutralRoles, false).SetParent(BerserkerOneCanKillCooldown)
            .SetValueFormat(OptionFormat.Seconds);
        BerserkerKillCooldownLevel = IntegerOptionItem.Create(Id + 7, "BerserkerLevelRequirement", new(1, 10, 1), 1, TabGroup.NeutralRoles, false).SetParent(BerserkerOneCanKillCooldown)
            .SetValueFormat(OptionFormat.Level);
        BerserkerTwoCanScavenger = BooleanOptionItem.Create(Id + 8, "BerserkerTwoCanScavenger", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Berserker]);
        BerserkerScavengerLevel = IntegerOptionItem.Create(Id + 9, "BerserkerLevelRequirement", new(1, 10, 1), 2, TabGroup.NeutralRoles, false).SetParent(BerserkerTwoCanScavenger)
            .SetValueFormat(OptionFormat.Level);
        BerserkerThreeCanBomber = BooleanOptionItem.Create(Id + 10, "BerserkerThreeCanBomber", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Berserker]);
        BerserkerBomberLevel = IntegerOptionItem.Create(Id + 11, "BerserkerLevelRequirement", new(1, 10, 1), 3, TabGroup.NeutralRoles, false).SetParent(BerserkerThreeCanBomber)
            .SetValueFormat(OptionFormat.Level);
        //BerserkerFourCanFlash = BooleanOptionItem.Create(Id + 11, "BerserkerFourCanFlash", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Berserker]);
        //BerserkerSpeed = FloatOptionItem.Create(611, "BerserkerSpeed", new(1.5f, 5f, 0.25f), 2.5f, TabGroup.NeutralRoles, false).SetParent(BerserkerOneCanKillCooldown)
        //    .SetValueFormat(OptionFormat.Multiplier);
        BerserkerFourCanNotKill = BooleanOptionItem.Create(Id + 12, "BerserkerFourCanNotKill", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Berserker]);
        BerserkerImmortalLevel = IntegerOptionItem.Create(Id + 13, "BerserkerLevelRequirement", new(1, 10, 1), 4, TabGroup.NeutralRoles, false).SetParent(BerserkerFourCanNotKill)
            .SetValueFormat(OptionFormat.Level);
        WarKillCooldown = FloatOptionItem.Create(Id + 14, "WarKillCooldown", new(0f, 150f, 2.5f), 15f, TabGroup.NeutralRoles, false).SetParent(BerserkerFourCanNotKill)
            .SetValueFormat(OptionFormat.Seconds);
        BerserkerCanKillTeamate = BooleanOptionItem.Create(Id + 19, "BerserkerCanKillTeamate", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Berserker]);
    }
    public override void Init()
    {
        BerserkerKillMax.Clear();
    }
    public override void Add(byte playerId)
    {
        Main.AllPlayerKillCooldown[playerId] = BerserkerKillCooldown.GetFloat();
        BerserkerKillMax[playerId] = 0;
    }
    public override void Remove(byte playerId)
    {
        BerserkerKillMax.Remove(playerId);
    }
    private void SendRPC(PlayerControl player)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable);
        writer.WriteNetObject(player);
        writer.WritePacked(BerserkerKillMax[_Player.PlayerId]);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public override void ReceiveRPC(MessageReader reader, PlayerControl NaN)
    {
        var killCount = reader.ReadPackedInt32();

        BerserkerKillMax[_Player.PlayerId] = killCount;
    }
    public override bool OthersKnowTargetRoleColor(PlayerControl seer, PlayerControl target)
        => KnowRoleTarget(seer, target);
    public override bool KnowRoleTarget(PlayerControl seer, PlayerControl target)
        => target.IsNeutralApocalypse() && seer.IsNeutralApocalypse();
    public override string GetProgressText(byte playerId, bool cvooms) => Utils.ColorString(Utils.GetRoleColor(CustomRoles.Berserker).ShadeColor(0.25f), BerserkerKillMax.TryGetValue(playerId, out var x) ? $"({x}/{BerserkerMax.GetInt()})" : "Invalid");
    public override bool CanUseImpostorVentButton(PlayerControl pc) => BerserkerCanVent.GetBool();
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
        => opt.SetVision(BerserkerHasImpostorVision.GetBool());
    public override bool CanUseKillButton(PlayerControl pc) => pc.IsAlive();
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (target.IsNeutralApocalypse() && !BerserkerCanKillTeamate.GetBool()) return false;

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
            Logger.Info("þé©Õ╝╣þêåþé©õ║å", "Boom");
            CustomSoundsManager.RPCPlayCustomSoundAll("Boom");
            foreach (var player in Main.AllAlivePlayerControls)
            {
                if (!player.IsModded())
                    player.KillFlash();

                if (player == killer) continue;
                if (player == target) continue;

                if (Utils.GetDistance(killer.transform.position, player.transform.position) <= Bomber.BomberRadius.GetFloat())
                {
                    if (!target.IsNeutralApocalypse())
                    {
                        Main.PlayerStates[player.PlayerId].deathReason = PlayerState.DeathReason.Bombed;
                        player.RpcMurderPlayer(player);
                        player.SetRealKiller(killer);
                    }
                    if (target.IsNeutralApocalypse() && BerserkerCanKillTeamate.GetBool())
                    {
                        Main.PlayerStates[player.PlayerId].deathReason = PlayerState.DeathReason.Bombed;
                        player.RpcMurderPlayer(player);
                        player.SetRealKiller(killer);
                    }
                }
            }
        }
        if (BerserkerKillMax[killer.PlayerId] >= BerserkerImmortalLevel.GetInt() && BerserkerFourCanNotKill.GetBool() && !killer.Is(CustomRoles.War))
        {
            killer.RpcSetCustomRole(CustomRoles.War);
            killer.GetRoleClass()?.OnAdd(killer.PlayerId);

            killer.Notify(GetString("BerserkerToWar"));
            Main.AllPlayerKillCooldown[killer.PlayerId] = WarKillCooldown.GetFloat();
        }

        SendRPC(killer);
        return noScav;
    }
}
internal class War : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.War;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Berserker);
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralApocalypse;
    //==================================================================\\

    public override bool OthersKnowTargetRoleColor(PlayerControl seer, PlayerControl target)
        => KnowRoleTarget(seer, target);
    public override bool KnowRoleTarget(PlayerControl seer, PlayerControl target)
        => target.IsNeutralApocalypse() && seer.IsNeutralApocalypse();
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = Berserker.WarKillCooldown.GetFloat();
    public override void ApplyGameOptions(IGameOptions opt, byte playerId) => opt.SetVision(Berserker.WarHasImpostorVision.GetBool());
    public override bool CanUseKillButton(PlayerControl pc) => true;
    public override bool CanUseImpostorVentButton(PlayerControl pc) => Berserker.WarCanVent.GetBool();
    public override bool OnCheckMurderAsTarget(PlayerControl killer, PlayerControl target) => false;
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        return CustomRoles.Berserker.GetStaticRoleClass().OnCheckMurderAsKiller(killer, target);
    }

    public override void Remove(byte playerId)
    {
        Berserker.BerserkerKillMax.Remove(playerId);
    }

    public override void ReceiveRPC(MessageReader reader, PlayerControl NaN)
    {
        var killCount = reader.ReadPackedInt32();

        Berserker.BerserkerKillMax[_Player.PlayerId] = killCount;
    }
}
