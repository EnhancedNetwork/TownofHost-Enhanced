using TOHE.Roles.Core;
using static TOHE.Options;
using UnityEngine;
using static TOHE.Translator;
using static TOHE.Utils;
using TOHE.Modules;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using AmongUs.GameOptions;
using Hazel;

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
    private static OptionItem DeathsAfterVote;
    private static OptionItem NecroReducedCooldown;
    private static OptionItem Vision;
    private static OptionItem VisionDuration;
    private static OptionItem Speed;
    private static OptionItem SpeedDuration;
    private static OptionItem IncreasedCooldown;
    private static OptionItem RandomFreezeDuration;

    private static byte DebuffID = 10;
    private static float debuffTimer;
    private static float maxDebuffTimer;
    private static float freezeTimer;
    private static byte randPlayer;
    private static bool isFreezing;
    private static readonly Dictionary<byte, float> originalSpeed = [];
    private static readonly Dictionary<byte, NetworkedPlayerInfo.PlayerOutfit> OriginalPlayerSkins = [];



    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.CovenRoles, CustomRoles.Sacrifist, 1, zeroOne: false);
        DebuffCooldown = FloatOptionItem.Create(Id + 10, "SacrifistDebuffCooldown", new(0f, 180f, 2.5f), 30f, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Sacrifist])
            .SetValueFormat(OptionFormat.Seconds);
        Vision = FloatOptionItem.Create(Id + 13, "SacrifistVision", new(0f, 5f, 0.25f), 0.5f, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Sacrifist])
            .SetValueFormat(OptionFormat.Multiplier);
        VisionDuration = FloatOptionItem.Create(Id + 17, "SacrifistVisionDuration", new(0f, 180f, 2.5f), 30f, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Sacrifist])
            .SetValueFormat(OptionFormat.Seconds);
        Speed = FloatOptionItem.Create(Id + 14, "SacrifistSpeed", new(0f, 5f, 0.25f), 0.5f, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Sacrifist])
            .SetValueFormat(OptionFormat.Multiplier);
        SpeedDuration = FloatOptionItem.Create(Id + 18, "SacrifistSpeedDuration", new(0f, 180f, 2.5f), 30f, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Sacrifist])
            .SetValueFormat(OptionFormat.Seconds);
        IncreasedCooldown = FloatOptionItem.Create(Id + 15, "SacrifistIncreasedCooldown", new(0f, 100f, 2.5f), 50f, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Sacrifist])
            .SetValueFormat(OptionFormat.Percent);
        RandomFreezeDuration = FloatOptionItem.Create(Id + 16, "SacrifistFreezeDuration", new(0f, 180f, 2.5f), 30f, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Sacrifist])
            .SetValueFormat(OptionFormat.Seconds);
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
        freezeTimer = 0;
        isFreezing = false;
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
                pc.RpcMurderPlayer(pc);
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
                    randPlayerPC.MarkDirtySettings();
                    originalSpeed.Remove(sacrifist);
                    originalSpeed.Add(sacrifist, Main.AllPlayerSpeed[sacrifist]);
                    Main.AllPlayerSpeed[sacrifist] = Speed.GetFloat();
                    pc.MarkDirtySettings();
                    Logger.Info($"{pc.GetRealName()} Changed Speed for {randPlayerPC.GetRealName} and self", "Sacrifist");
                    pc.Notify(GetString("SacrifistSpeedDebuff"), SpeedDuration.GetFloat());
                    _ = new LateTask(() =>
                    {
                        pc.Notify(GetString("SacrifistSpeedRevert"));
                        Main.AllPlayerSpeed[randPlayer] = originalSpeed[randPlayer];
                        GetPlayerById(randPlayer).SyncSettings();
                        originalSpeed.Remove(randPlayer);
                        Main.AllPlayerSpeed[sacrifist] = originalSpeed[sacrifist];
                        pc.SyncSettings();
                        originalSpeed.Remove(sacrifist);
                    }, SpeedDuration.GetFloat(), "Sacrifist Revert Speed");
                    break;
                // Change Vision
                case 1:
                    pc.Notify(GetString("SacrifistVisionDebuff"), VisionDuration.GetFloat());
                    _ = new LateTask(() =>
                    {
                        DebuffID = 10;
                        pc.Notify(GetString("SacrifistVisionRevert"), 5f);
                    }, VisionDuration.GetFloat(), "Sacrifist Revert Vision");
                    break;
                // Change Cooldown
                case 2:
                    Main.AllPlayerKillCooldown[randPlayer] += Main.AllPlayerKillCooldown[randPlayer] * (IncreasedCooldown.GetFloat() / 100);
                    Main.AllPlayerKillCooldown[sacrifist] += Main.AllPlayerKillCooldown[sacrifist] * (IncreasedCooldown.GetFloat() / 100);
                    maxDebuffTimer += maxDebuffTimer * (IncreasedCooldown.GetFloat() / 100);
                    Logger.Info($"{pc.GetRealName()} Changed Cooldown for {randPlayerPC.GetRealName} and self", "Sacrifist");
                    pc.Notify(GetString("SacrifistCooldownDebuff"), 5f);
                    break;
                // Cant Fix Sabotage (not coding allat, just give them Fool)
                case 3:
                    GetPlayerById(sacrifist).RpcSetCustomRole(CustomRoles.Fool);
                    randPlayerPC.RpcSetCustomRole(CustomRoles.Fool);
                    Logger.Info($"{pc.GetRealName()} Gave Fool to {randPlayerPC.GetRealName} and self", "Sacrifist");
                    pc.Notify(GetString("SacrifistFoolDebuff"), 5f);
                    break;
                // Make one of them call a meeting
                case 4:
                    pc.Notify(GetString("SacrifistMeetingDebuff"), 15f);
                    _ = new LateTask(() =>
                    {
                        switch (rand.Next(0, 2))
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
                    }, 2f, "Sacrifist Call Meeting");
                    break;
                // Can't Report
                case 5:
                    ReportDeadBodyPatch.CanReport[randPlayer] = false;
                    ReportDeadBodyPatch.CanReport[sacrifist] = false;
                    Logger.Info($"{pc.GetRealName()} Made {randPlayerPC.GetRealName} and self unable to report", "Sacrifist");
                    pc.Notify(GetString("SacrifistReportDebuff"), 5f);
                    break;
                // Reset Tasks
                case 6:
                    var taskStateTarget = randPlayerPC.GetPlayerTaskState();
                    randPlayerPC.Data.RpcSetTasks(new Il2CppStructArray<byte>(0)); //Let taskassign patch decide the tasks
                    taskStateTarget.CompletedTasksCount = 0;
                    var taskStateSacrif = pc.GetPlayerTaskState();
                    pc.Data.RpcSetTasks(new Il2CppStructArray<byte>(0)); //Let taskassign patch decide the tasks
                    taskStateSacrif.CompletedTasksCount = 0;
                    pc.Notify(GetString("SacrifistTasksDebuff"), 5f);
                    Logger.Info($"{pc.GetRealName()} Made {randPlayerPC.GetRealName} and self reset tasks", "Sacrifist");
                    break;
                // Swap Skins
                case 7:
                    var temp = new NetworkedPlayerInfo.PlayerOutfit()
                    .Set(pc.GetRealName(), pc.CurrentOutfit.ColorId, pc.CurrentOutfit.HatId, pc.CurrentOutfit.SkinId, pc.CurrentOutfit.VisorId, pc.CurrentOutfit.PetId, pc.CurrentOutfit.NamePlateId);
                    OriginalPlayerSkins.Add(pc.PlayerId, Camouflage.PlayerSkins[pc.PlayerId]);
                    Camouflage.PlayerSkins[pc.PlayerId] = temp;
                    pc.SetNewOutfit(randPlayerPC.CurrentOutfit, setName: true, setNamePlate: true);

                    OriginalPlayerSkins.Add(randPlayer, Camouflage.PlayerSkins[randPlayer]);
                    Camouflage.PlayerSkins[randPlayer] = randPlayerPC.CurrentOutfit;
                    randPlayerPC.SetNewOutfit(temp, setName: true, setNamePlate: true);
                    pc.Notify(GetString("SacrifistSwapSkinsDebuff"), 5f);
                    Logger.Info($"{pc.GetRealName()} swapped outfit with {randPlayerPC.GetRealName}", "Sacrifist");
                    break;
                // Random Freezing (done in different method)
                case 8:
                    isFreezing = true;
                    Logger.Info($"{pc.GetRealName()} and {randPlayerPC.GetRealName} will randomly freeze for duration", "Sacrifist");
                    pc.Notify(string.Format(GetString("SacrifistFreezeDebuff"), RandomFreezeDuration.GetFloat()), RandomFreezeDuration.GetFloat());
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
            }, 3f, "Sacrifist Swap");
        }
    }
    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        var sacrifist = _Player.PlayerId;
        DebuffID = 10;

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
        if (isFreezing)
        {
            if (freezeTimer < RandomFreezeDuration.GetFloat())
            {
                var rand = IRandom.Instance;
                var num = rand.Next(0, 10);
                if (num == 0)
                {
                    originalSpeed.Remove(randPlayer);
                    originalSpeed.Add(randPlayer, Main.AllPlayerSpeed[randPlayer]);
                    Main.AllPlayerSpeed[randPlayer] = 0f;
                    GetPlayerById(randPlayer).MarkDirtySettings();
                    originalSpeed.Remove(player.PlayerId);
                    originalSpeed.Add(player.PlayerId, Main.AllPlayerSpeed[player.PlayerId]);
                    Main.AllPlayerSpeed[player.PlayerId] = 0f;
                    player.MarkDirtySettings();
                }
                else
                {
                    Main.AllPlayerSpeed[randPlayer] = originalSpeed[randPlayer];
                    GetPlayerById(randPlayer).SyncSettings();
                    originalSpeed.Remove(randPlayer);
                    Main.AllPlayerSpeed[player.PlayerId] = originalSpeed[player.PlayerId];
                    player.SyncSettings();
                    originalSpeed.Remove(player.PlayerId);
                }
                freezeTimer += Time.fixedDeltaTime;
            }
            if (freezeTimer >= RandomFreezeDuration.GetFloat())
            {
                Main.AllPlayerSpeed[randPlayer] = originalSpeed[randPlayer];
                GetPlayerById(randPlayer).SyncSettings();
                originalSpeed.Remove(randPlayer);
                Main.AllPlayerSpeed[player.PlayerId] = originalSpeed[player.PlayerId];
                player.SyncSettings();
                originalSpeed.Remove(player.PlayerId);
                isFreezing = false;
                freezeTimer = 0;
            }
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