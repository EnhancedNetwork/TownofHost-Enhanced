using AmongUs.GameOptions;
using Hazel;
using TOHE.Modules;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.Coven;

internal class Sacrifist : CovenManager
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Sacrifist;
    private const int Id = 30600;
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Shapeshifter;
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

    private static byte DebuffID = 10;
    private static float debuffTimer;
    private static float maxDebuffTimer;
    private static byte randPlayer;
    private static readonly Dictionary<byte, float> originalSpeed = [];
    private static readonly Dictionary<byte, NetworkedPlayerInfo.PlayerOutfit> OriginalPlayerSkins = [];
    private static readonly Dictionary<byte, List<byte>> VisionChange = [];




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
        VisionChange.Clear();
    }
    public override void Add(byte playerId)
    {
        debuffTimer = 0;
        maxDebuffTimer = DebuffCooldown.GetFloat();
        VisionChange[playerId] = [];
    }
    private static void SendRPC(PlayerControl pc)
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
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (!CanUseKillButton(killer)) return false;
        if (HasNecronomicon(killer) && !target.GetCustomRole().IsCovenTeam())
        {
            return true;
        }
        killer.Notify(GetString("CovenDontKillOtherCoven"));
        return false;
    }
    public override void UnShapeShiftButton(PlayerControl pc)
    {
        var rand = IRandom.Instance;
        DebuffID = (byte)rand.Next(0, 9);
        if (randPlayer == byte.MaxValue)
        {
            randPlayer = Main.AllAlivePlayerControls.Where(x => !x.Is(Custom_Team.Coven) && !x.Is(CustomRoles.Enchanted)).ToList().RandomElement().PlayerId;
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
                    Logger.Info($"{pc.GetRealName()} Changed Speed for {randPlayerPC.GetRealName()} and self", "Sacrifist");
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
                    VisionChange[sacrifist].Add(sacrifist);
                    VisionChange[sacrifist].Add(randPlayer);
                    pc.Notify(GetString("SacrifistVisionDebuff"), VisionDuration.GetFloat());
                    _ = new LateTask(() =>
                    {
                        VisionChange[sacrifist].Remove(sacrifist);
                        VisionChange[sacrifist].Remove(randPlayer);
                        DebuffID = 10;
                        pc.Notify(GetString("SacrifistVisionRevert"), 5f);
                    }, VisionDuration.GetFloat(), "Sacrifist Revert Vision");
                    break;
                // Change Cooldown
                case 2:
                    Main.AllPlayerKillCooldown[randPlayer] += Main.AllPlayerKillCooldown[randPlayer] * (IncreasedCooldown.GetFloat() / 100);
                    Main.AllPlayerKillCooldown[sacrifist] += Main.AllPlayerKillCooldown[sacrifist] * (IncreasedCooldown.GetFloat() / 100);
                    maxDebuffTimer += maxDebuffTimer * (IncreasedCooldown.GetFloat() / 100);
                    Logger.Info($"{pc.GetRealName()} Changed Cooldown for {randPlayerPC.GetRealName()} and self", "Sacrifist");
                    pc.Notify(GetString("SacrifistCooldownDebuff"), 5f);
                    break;
                // Cant Fix Sabotage (not coding allat, just give them Fool)
                case 3:
                    pc.RpcSetCustomRole(CustomRoles.Fool);
                    randPlayerPC.RpcSetCustomRole(CustomRoles.Fool);
                    Logger.Info($"{pc.GetRealName()} Gave Fool to {randPlayerPC.GetRealName()} and self", "Sacrifist");
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
                                Logger.Info($"{pc.GetRealName()} Made {randPlayerPC.GetRealName()} call meeting", "Sacrifist");
                                break;
                        }
                    }, 2f, "Sacrifist Call Meeting");
                    break;
                // Can't Report
                case 5:
                    ReportDeadBodyPatch.CanReport[randPlayer] = false;
                    ReportDeadBodyPatch.CanReport[sacrifist] = false;
                    Logger.Info($"{pc.GetRealName()} Made {randPlayerPC.GetRealName()} and self unable to report", "Sacrifist");
                    pc.Notify(GetString("SacrifistReportDebuff"), 5f);
                    break;
                // Reset Tasks
                case 6:
                    randPlayerPC.RpcResetTasks();
                    pc.RpcResetTasks();
                    pc.Notify(GetString("SacrifistTasksDebuff"), 5f);
                    Logger.Info($"{pc.GetRealName()} Made {randPlayerPC.GetRealName()} and self reset tasks", "Sacrifist");
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
                    Logger.Info($"{pc.GetRealName()} swapped outfit with {randPlayerPC.GetRealName()}", "Sacrifist");
                    break;
                // Swap Sacrifist and Target
                case 8:
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
                    }, 0.01f, "Sacrifist Swap");
                    Logger.Info($"{pc.GetRealName()} Will Swap with {randPlayerPC.GetRealName()} 5s after exiting vent", "Sacrifist");
                    pc.Notify(GetString("SacrifistSwapDebuff"), 15f);
                    break;
            }
            SendRPC(pc);
            debuffTimer = 0;
        }
    }
    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        if (_Player == null) return;
        if (randPlayer == byte.MaxValue) return;
        var sacrifist = _Player.PlayerId;
        DebuffID = 10;

        ReportDeadBodyPatch.CanReport[randPlayer] = true;
        GetPlayerById(randPlayer).ResetKillCooldown();
        if (OriginalPlayerSkins.ContainsKey(randPlayer))
        {
            Camouflage.PlayerSkins[randPlayer] = OriginalPlayerSkins[randPlayer];

            if (!Camouflage.IsCamouflage)
            {
                PlayerControl pc =
                    Main.AllAlivePlayerControls.FirstOrDefault(a => a.PlayerId == randPlayer);

                pc.SetNewOutfit(OriginalPlayerSkins[randPlayer], setName: true, setNamePlate: true);
            }
        }
        randPlayer = byte.MaxValue;
        Logger.Info($"Resetting Debuffs for Affected player", "Sacrifist");


        ReportDeadBodyPatch.CanReport[sacrifist] = true;
        _Player.ResetKillCooldown();
        maxDebuffTimer = DebuffCooldown.GetFloat();
        if (OriginalPlayerSkins.ContainsKey(sacrifist))
        {
            Camouflage.PlayerSkins[sacrifist] = OriginalPlayerSkins[sacrifist];

            if (!Camouflage.IsCamouflage)
            {
                PlayerControl pc =
                    Main.AllAlivePlayerControls.FirstOrDefault(a => a.PlayerId == sacrifist);

                pc.SetNewOutfit(OriginalPlayerSkins[sacrifist], setName: true, setNamePlate: true);
            }
        }
        Logger.Info($"Resetting Debuffs for Sacrifist", "Sacrifist");
    }
    public static void SetVision(PlayerControl player, IGameOptions opt)
    {
        if (VisionChange.Any(a => a.Value.Contains(player.PlayerId) &&
           Main.AllAlivePlayerControls.Any(b => b.PlayerId == a.Key)))
        {
            opt.SetVision(false);
            opt.SetFloat(FloatOptionNames.CrewLightMod, Vision.GetFloat());
            opt.SetFloat(FloatOptionNames.ImpostorLightMod, Vision.GetFloat());
        }
    }
    public override void AfterMeetingTasks()
    {
        debuffTimer = 0;
    }
    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        return GetString("SacrifistDebuffCooldown") + ": " + string.Format("{0:f0}", debuffTimer) + "s / " + string.Format("{0:f0}", maxDebuffTimer) + "s";
    }
    public override void OnFixedUpdate(PlayerControl player, bool lowLoad, long nowTime, int timerLowLoad)
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
    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        if (HasNecronomicon(playerId))
        {
            hud.AbilityButton.OverrideText(GetString("SacrifistNecroShapeshiftButton"));
        }
        else
        {
            hud.AbilityButton.OverrideText(GetString("SacrifistShapeshiftButton"));
        }
    }
}
