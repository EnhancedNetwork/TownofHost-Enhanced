using TOHE.Roles.Core;
using static TOHE.Options;
using UnityEngine;
using static TOHE.Translator;
using static TOHE.Utils;
using TOHE.Modules;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using AmongUs.GameOptions;
using Hazel;
using MS.Internal.Xml.XPath;
using static UnityEngine.GraphicsBuffer;
using TOHE.Roles.Impostor;

namespace TOHE.Roles.Coven;

internal class Sacrifist : CovenManager
{
    //===========================SETUP================================\\
    private const int Id = 30600;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Sacrifist);
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CovenUtility;
    //==================================================================\\

    private static OptionItem DebuffCooldown;
    public static OptionItem DeathsAfterVote;
    public static OptionItem NecroReducedCooldown;
    private static OptionItem Vision;
    private static OptionItem Speed;
    private static OptionItem IncreasedCooldown;

    private static byte DebuffID = 10;
    private static float debuffTimer;
    private static float maxDebuffTimer;
    private static byte randPlayer;
    private static readonly Dictionary<byte, float> originalSpeed = [];
    private static readonly Dictionary<byte, NetworkedPlayerInfo.PlayerOutfit> OriginalPlayerSkins = [];



    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.CovenRoles, CustomRoles.Sacrifist, 1, zeroOne: false);
        DebuffCooldown = FloatOptionItem.Create(Id + 10, "SacrifistDebuffCooldown", new(0f, 180f, 2.5f), 30f, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Sacrifist])
            .SetValueFormat(OptionFormat.Seconds);
        Vision = FloatOptionItem.Create(Id + 13, "SacrifistVision", new(0f, 5f, 0.25f), 0.5f, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Sacrifist])
            .SetValueFormat(OptionFormat.Multiplier);
        Speed = FloatOptionItem.Create(Id + 14, "SacrifistSpeed", new(0f, 5f, 0.25f), 0.5f, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Sacrifist])
            .SetValueFormat(OptionFormat.Multiplier);
        IncreasedCooldown = FloatOptionItem.Create(Id + 15, "SacrifistIncreasedCooldown", new(0f, 100f, 2.5f), 50f, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Sacrifist])
            .SetValueFormat(OptionFormat.Percent);
        DeathsAfterVote = IntegerOptionItem.Create(Id + 11, "SacrifistDeathsAfterVote", new(0, 15, 1), 0, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Sacrifist])
            .SetValueFormat(OptionFormat.Players);
        NecroReducedCooldown = FloatOptionItem.Create(Id + 12, "SacrifistNecroReducedCooldown", new(0f, 100f, 2.5f), 50f, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Sacrifist])
            .SetValueFormat(OptionFormat.Percent);
    }

    public override void Init()
    {
        DebuffID = 10;
        randPlayer = byte.MaxValue;
        originalSpeed.Clear();
        OriginalPlayerSkins.Clear();
    }
    public override void Add(byte playerId)
    {
        debuffTimer = 0;
        maxDebuffTimer = DebuffCooldown.GetFloat();
    }
    public void SendRPC(PlayerControl pc)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, pc.GetClientId());
        writer.Write(DebuffID);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public override void ReceiveRPC(MessageReader reader, PlayerControl NaN)
    {
        DebuffID = reader.ReadByte();
    }
    public override bool CanUseImpostorVentButton(PlayerControl pc) => true;

    // Sacrifist shouldn't be able to kill at all but if there's solo Sacrifist the game is unwinnable so they can kill when solo
    public override bool CanUseKillButton(PlayerControl pc) => Main.AllAlivePlayerControls.Where(pc => pc.Is(Custom_Team.Coven)).Count() == 1;

    public override void OnEnterVent(PlayerControl pc, Vent vent)
    {
        var rand = IRandom.Instance;
        DebuffID = (byte)rand.Next(0, 10);
        if (randPlayer == byte.MaxValue)
        {
            randPlayer = Main.AllAlivePlayerControls.Where(x => !x.Is(Custom_Team.Coven) || !x.Is(CustomRoles.Enchanted)).ToList().RandomElement().PlayerId;
        }
        var randPlayerPC = GetPlayerById(randPlayer);
        var sacrifist = pc.PlayerId;
        if (debuffTimer >= maxDebuffTimer)
        {
            Logger.Info($"{pc.GetRealName()} Started Sacrifice", "Sacrifist");
            if (HasNecronomicon(pc))
            {
                pc.RpcExileV2();
                pc.SetRealKiller(pc);
                pc.SetDeathReason(PlayerState.DeathReason.Suicide);
                Logger.Info($"{pc.GetRealName()} Ultimate Sacrifice", "Sacrifist");
                var covList = Main.AllAlivePlayerControls.Where(x => x.Is(Custom_Team.Coven) || x.Is(CustomRoles.Enchanted));
                foreach (var cov in covList)
                {
                    Main.AllPlayerKillCooldown[cov.PlayerId] -= Main.AllPlayerKillCooldown[cov.PlayerId] * (NecroReducedCooldown.GetFloat() / 100);
                }
                return;
            }
            switch (DebuffID)
            {
                default:
                    break;
                // Change Speed
                case 0:
                    originalSpeed.Remove(randPlayer);
                    originalSpeed.Add(randPlayer, Main.AllPlayerSpeed[randPlayer]);
                    Main.AllPlayerSpeed[randPlayer] = Speed.GetFloat();
                    originalSpeed.Remove(sacrifist);
                    originalSpeed.Add(sacrifist, Main.AllPlayerSpeed[sacrifist]);
                    Main.AllPlayerSpeed[sacrifist] = Speed.GetFloat();
                    Logger.Info($"{pc.GetRealName()} Changed Speed for {randPlayerPC.GetRealName} and self", "Sacrifist");
                    pc.Notify(GetString("SacrifistSpeedDebuff"), 15f);
                    break;
                // Change Vision
                case 1:
                    pc.Notify(GetString("SacrifistVisionDebuff"), 15f);
                    break;
                // Change Cooldown
                case 2:
                    Main.AllPlayerKillCooldown[randPlayer] += Main.AllPlayerKillCooldown[randPlayer] * (IncreasedCooldown.GetFloat() / 100);
                    Main.AllPlayerKillCooldown[sacrifist] += Main.AllPlayerKillCooldown[sacrifist] * (IncreasedCooldown.GetFloat() / 100);
                    maxDebuffTimer += maxDebuffTimer * (IncreasedCooldown.GetFloat() / 100);
                    Logger.Info($"{pc.GetRealName()} Changed Cooldown for {randPlayerPC.GetRealName} and self", "Sacrifist");
                    pc.Notify(GetString("SacrifistCooldownDebuff"), 15f);
                    break;
                // Cant Fix Sabotage (not coding allat, just give them Fool)
                case 3:
                    GetPlayerById(sacrifist).RpcSetCustomRole(CustomRoles.Fool);
                    randPlayerPC.RpcSetCustomRole(CustomRoles.Fool);
                    Logger.Info($"{pc.GetRealName()} Gave Fool to {randPlayerPC.GetRealName} and self", "Sacrifist");
                    pc.Notify(GetString("SacrifistFoolDebuff"), 15f);
                    break;
                // Make one of them call a meeting
                case 4:
                    pc.Notify(GetString("SacrifistMeetingDebuff"), 15f);
                    switch (rand.Next(0,2))
                    {
                        case 0:
                            randPlayerPC.NoCheckStartMeeting(null);
                            Logger.Info($"{pc.GetRealName()} Made Self Call meeting", "Sacrifist");
                            break;
                        case 1:
                            GetPlayerById(sacrifist).NoCheckStartMeeting(null);
                            Logger.Info($"{pc.GetRealName()} Made {randPlayerPC.GetRealName} call meeting", "Sacrifist");
                            break;
                    }
                    break;
                // Can't Report
                case 5:
                    ReportDeadBodyPatch.CanReport[randPlayer] = false;
                    ReportDeadBodyPatch.CanReport[sacrifist] = false;
                    Logger.Info($"{pc.GetRealName()} Made {randPlayerPC.GetRealName} and self unable to report", "Sacrifist");
                    pc.Notify(GetString("SacrifistReportDebuff"), 15f);
                    break;
                // Reset Tasks (inapplicable to Sacrifist)
                case 6:
                    var taskState = pc.GetPlayerTaskState();
                    randPlayerPC.Data.RpcSetTasks(new Il2CppStructArray<byte>(0)); //Let taskassign patch decide the tasks
                    taskState.CompletedTasksCount = 0;
                    pc.Notify(GetString("SacrifistTasksDebuff"), 15f);
                    break;
                // Swap Skins
                case 7:
                    var temp = pc.CurrentOutfit;
                    OriginalPlayerSkins.Add(pc.PlayerId, Camouflage.PlayerSkins[pc.PlayerId]);
                    Camouflage.PlayerSkins[pc.PlayerId] = temp;
                    pc.SetNewOutfit(randPlayerPC.CurrentOutfit, setName: true, setNamePlate: true);

                    OriginalPlayerSkins.Add(randPlayer, Camouflage.PlayerSkins[randPlayer]);
                    Camouflage.PlayerSkins[randPlayer] = randPlayerPC.CurrentOutfit;
                    randPlayerPC.SetNewOutfit(temp, setName: true, setNamePlate: true);
                    Logger.Info($"{pc.GetRealName()} swapped outfit with {randPlayerPC.GetRealName}", "Sacrifist");
                    break;
                // Fake Snitch Arrow to Sacrifist and Target (done in different method)
                case 8:
                    Logger.Info($"{pc.GetRealName()} Caused Arrow to {randPlayerPC.GetRealName} and self for everyone", "Sacrifist");
                    pc.Notify(GetString("SacrifistDoxxDebuff"), 15f);
                    break;
                // Swap Sacrifist and Target (done in different method)
                case 9:
                    Logger.Info($"{pc.GetRealName()} Will Swap with {randPlayerPC.GetRealName} 5s after exiting vent", "Sacrifist");
                    pc.Notify(GetString("SacrifistSwapDebuff"), 15f);
                    break;
            }
            SendRPC(pc);
            debuffTimer = 0;
        }
    }
    public override void OnExitVent(PlayerControl pc, int ventId)
    {
        if (DebuffID == 9)
        {
            _ = new LateTask(() =>
            {
                var randPlayerPC = GetPlayerById(randPlayer);
                if (pc.CanBeTeleported() && randPlayerPC.CanBeTeleported())
                {
                    var originPs = randPlayerPC.GetCustomPosition();
                    randPlayerPC.RpcTeleport(pc.GetCustomPosition());
                    pc.RpcTeleport(originPs);

                    pc.RPCPlayCustomSound("Teleport");
                    randPlayerPC.RPCPlayCustomSound("Teleport");
                }
                else
                {
                    pc.Notify(ColorString(GetRoleColor(CustomRoles.Sacrifist), GetString("ErrorTeleport")));
                }
            }, 5f, "Sacrifist Swap");
        }
    }
    public override string GetSuffixOthers(PlayerControl seer, PlayerControl target, bool isForMeeting = false)
    {
        if (target != null && seer.PlayerId != target.PlayerId) return string.Empty;
        if (randPlayer == byte.MaxValue) return string.Empty;
        if (randPlayer == seer.PlayerId) return string.Empty;
        if (DebuffID != 8) return string.Empty;

        var warning = "⚠";
            warning += TargetArrow.GetArrows(seer, [_Player.PlayerId, randPlayer]);

        return ColorString(GetRoleColor(CustomRoles.Snitch), warning);
    }
    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {

        var sacrifist = _Player.PlayerId;
        DebuffID = 10;

        Main.AllPlayerSpeed[randPlayer] = originalSpeed[randPlayer];
        GetPlayerById(randPlayer).SyncSettings();
        originalSpeed.Remove(randPlayer);
        ReportDeadBodyPatch.CanReport[randPlayer] = true;
        GetPlayerById(randPlayer).ResetKillCooldown();
        Camouflage.PlayerSkins[randPlayer] = OriginalPlayerSkins[randPlayer];

        if (!Camouflage.IsCamouflage)
        {
            PlayerControl pc =
                Main.AllAlivePlayerControls.FirstOrDefault(a => a.PlayerId == randPlayer);

            pc.SetNewOutfit(OriginalPlayerSkins[randPlayer], setName: true, setNamePlate: true);
        }
        randPlayer = byte.MaxValue;
        Logger.Info($"Resetting Debuffs for Affected player", "Sacrifist");


        Main.AllPlayerSpeed[sacrifist] = originalSpeed[sacrifist];
        GetPlayerById(sacrifist).SyncSettings();
        originalSpeed.Remove(sacrifist);
        ReportDeadBodyPatch.CanReport[sacrifist] = true;
        _Player.ResetKillCooldown();
        maxDebuffTimer = DebuffCooldown.GetFloat();
        Camouflage.PlayerSkins[sacrifist] = OriginalPlayerSkins[sacrifist];

        if (!Camouflage.IsCamouflage)
        {
            PlayerControl pc =
                Main.AllAlivePlayerControls.FirstOrDefault(a => a.PlayerId == sacrifist);

            pc.SetNewOutfit(OriginalPlayerSkins[sacrifist], setName: true, setNamePlate: true);
        }
        Logger.Info($"Resetting Debuffs for Sacrifist", "Sacrifist");
    }
    public static void SetVision(PlayerControl player, IGameOptions opt)
    {
        if ((player.PlayerId == randPlayer || player.PlayerId == Utils.GetPlayerListByRole(CustomRoles.Sacrifist).First().PlayerId) && DebuffID == 1)
        {
            opt.SetVision(false);
            opt.SetFloat(FloatOptionNames.CrewLightMod, Vision.GetFloat());
            opt.SetFloat(FloatOptionNames.ImpostorLightMod, Vision.GetFloat());
        }
    }
    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        return debuffTimer.ToString() + "s / " + maxDebuffTimer.ToString() + "s";
    }
    public override void OnFixedUpdate(PlayerControl player, bool lowLoad, long nowTime)
    {
        if (debuffTimer < maxDebuffTimer)
        {
            debuffTimer += Time.fixedDeltaTime;
        }
    }
    public override void OnPlayerExiled(PlayerControl player, NetworkedPlayerInfo exiled)
    {
        if (exiled == null) return;
        if (exiled != _Player) return;

        List<PlayerControl> killPotentials = [];
        var votedForExiled = MeetingHud.Instance.playerStates.Where(a => a.VotedFor == exiled.PlayerId && a.TargetPlayerId != exiled.PlayerId).ToArray();
        foreach (var playerVote in votedForExiled)
        {
            var crewPlayer = Main.AllPlayerControls.FirstOrDefault(a => a.PlayerId == playerVote.TargetPlayerId);
            if (crewPlayer == null || crewPlayer.GetCustomRole().IsCoven() || crewPlayer.GetCustomRole().IsTNA()) return;
            killPotentials.Add(crewPlayer);
        }
        if (killPotentials.Count == 0) return;

        List<byte> killPlayers = [];

        for (int i = 0; i < DeathsAfterVote.GetInt(); i++)
        {
            if (killPotentials.Count == 0) break;

            PlayerControl target = killPotentials.RandomElement();
            target.SetRealKiller(_Player);
            killPlayers.Add(target.PlayerId);
            killPotentials.Remove(target);
        }

        CheckForEndVotingPatch.TryAddAfterMeetingDeathPlayers(PlayerState.DeathReason.Retribution, [.. killPlayers]);
    }
}