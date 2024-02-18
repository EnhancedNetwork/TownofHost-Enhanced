using AmongUs.GameOptions;
using HarmonyLib;
using Hazel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TOHE.Modules;
using TOHE.Roles.AddOns.Common;
using TOHE.Roles.AddOns.Crewmate;
using TOHE.Roles.AddOns.Impostor;
using TOHE.Roles.Crewmate;
using TOHE.Roles.Double;
using TOHE.Roles.Impostor;
using TOHE.Roles.Neutral;
using static TOHE.Translator;

namespace TOHE;

enum CustomRPC
{
    // RpcCalls can increase with each AU version
    // On version 2023.11.28 the last id in RpcCalls: 61
    VersionCheck = 80,
    RequestRetryVersionCheck = 81,
    SyncCustomSettings = 100,
    RestTOHESetting,
    SetDeathReason,
    EndGame,
    PlaySound,
    SetCustomRole,
    SetBountyTarget,
    SyncPuppet,
    SyncHuntsmanTarget,
    SyncPlagueDoctor,
    SyncKami,
    SetKillOrSpell,
    SetKillOrHex,
    SetKillOrCurse,
    //SetCopyCatMiscopyLimit,
    SetCaptainTargetSpeed,
    RevertCaptainTargetSpeed,
    RevertCaptainAllTargetSpeed,
    SetCaptainVotedTarget,
    RevertCaptainVoteRemove,
    SetDousedPlayer,
    setPlaguedPlayer,
    SetNameColorData,
    DoSpell,
    DoHex,
    DoCurse,
    SniperSync,
    SetLoversPlayers,
    SetExecutionerTarget,
    RemoveExecutionerTarget,
    SetLawyerTarget,
    RemoveLawyerTarget,
    SendFireworkerState,
    SetCurrentDousingTarget,
    SetEvilTrackerTarget,
    SetRealKiller,


    // TOHE
    AntiBlackout,
    PlayCustomSound,
    SetKillTimer,
    SyncAllPlayerNames,
    SyncNameNotify,
    ShowPopUp,
    KillFlash,
    DumpLog,
    SyncRoleSkill,

    //Roles
    SetDrawPlayer,
    SetCPTasksDone,
    SetCurrentDrawTarget,
    SetGamerHealth,
    RpcPassBomb,
    SyncRomanticTarget,
    SyncVengefulRomanticTarget,
    SetJailerTarget,
    SetJailerExeLimit,
    SetSoulCollectorLimit,
    SyncSchrodingerData,
    SetPixieTargets,
    SetInspectorLimit,
    KeeperRPC,
    SetPelicanEatenNum,
    SetAlchemistTimer,
    UndertakerLocationSync,
    RiftMakerSyncData,
    SetPursuerSellLimit,
    SetGhostPlayer,
    SetDarkHiderKillCount,
    SetEvilDiviner,
    SetGreedierOE,
    SetCursedWolfSpellCount,
    SetJinxSpellCount,
    SetCollectorVotes,
    TaskinatorMarkedTask,
    BenefactorRPC,
    SetSwapperVotes,
    GuessKill,
    SetMarkedPlayer,
    SetConcealerTimer,
    SetMedicalerProtectList,
    SyncPsychicRedList,
    SetMorticianArrow,
    SetTracefinderArrow,
    Judge,
    Guess,
    PresidentEnd,
    PresidentReveal,
    MeetingKill,
    MafiaRevenge,
    RetributionistRevenge,
    SetWraithTimer,
    SetBKTimer,
    SyncTotocalcioTargetAndTimes,
    SetSuccubusCharmLimit,
    SetCursedSoulCurseLimit,
    SetInvestgatorLimit,
    SyncInvestigator, // Unused
    SetVirusInfectLimit,
    SetRevealedPlayer,
    SetCurrentRevealTarget,
    SetJackalRecruitLimit,
    SetBanditStealLimit,
    SetDoppelgangerStealLimit,
    SetBloodhoundArrow,
    SetBloodhoundkKillerArrow,
    SetVultureArrow,
    SyncVultureBodyAmount,
    SetSpiritcallerSpiritLimit,
    SetDoomsayerProgress,
    SetTrackerTarget,
    SetSeekerTarget,
    SetSeekerPoints,
    SpyRedNameSync,
    SpyRedNameRemove,
    SetPotionMaster,
    SetChameleonTimer,
    SyncAdmiredList,
    SetRememberLimit,
    SetImitateLimit,
    SyncShroud,
    SyncMiniCrewAge,
    QuizmasterMarkPlayer,
    PirateSyncData,
    //FFA
    SyncFFAPlayer,
    SyncFFANameNotify,
    SyncSolsticerNotify
}
public enum Sounds
{
    KillSound,
    TaskComplete,
    TaskUpdateSound,
    ImpTransform,

    Test,
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.HandleRpc))]
internal class RPCHandlerPatch
{
    public static bool TrustedRpc(byte id)
    => (CustomRPC)id is CustomRPC.VersionCheck or CustomRPC.RequestRetryVersionCheck or CustomRPC.AntiBlackout or CustomRPC.Judge or CustomRPC.MeetingKill or CustomRPC.Guess or CustomRPC.PresidentEnd or CustomRPC.MafiaRevenge or CustomRPC.RetributionistRevenge or CustomRPC.SetSwapperVotes or CustomRPC.DumpLog;
    public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] byte callId, [HarmonyArgument(1)] MessageReader reader)
    {
        var rpcType = (RpcCalls)callId;
        MessageReader subReader = MessageReader.Get(reader);
        if (EAC.ReceiveRpc(__instance, callId, reader)) return false;
        Logger.Info($"{__instance?.Data?.PlayerId}({(__instance?.Data?.PlayerId == 0 ? "Host" : __instance?.Data?.PlayerName)}):{callId}({RPC.GetRpcName(callId)})", "ReceiveRPC");
        switch (rpcType)
        {
            case RpcCalls.SetName: //SetNameRPC
                string name = subReader.ReadString();
                if (subReader.BytesRemaining > 0 && subReader.ReadBoolean()) return false;
                Logger.Info("RPC Set Name For Player: " + __instance.GetNameWithRole() + " => " + name, "SetName");
                break;
            case RpcCalls.SetRole: //SetNameRPC
                var role = (RoleTypes)subReader.ReadUInt16();
                Logger.Info("RPC Set Role For Player: " + __instance.GetRealName() + " => " + role, "SetRole");
                break;
            case RpcCalls.SendChat:
                var text = subReader.ReadString();
                Logger.Info($"{__instance.GetNameWithRole().RemoveHtmlTags()}:{text.RemoveHtmlTags()}", "ReceiveChat");
                ChatCommands.OnReceiveChat(__instance, text, out var canceled);
                if (canceled) return false;
                break;
            case RpcCalls.StartMeeting:
                var p = Utils.GetPlayerById(subReader.ReadByte());
                Logger.Info($"{__instance.GetNameWithRole()} => {p?.GetNameWithRole() ?? "null"}", "StartMeeting");
                break;
        }
        if (__instance.PlayerId != 0
    && Enum.IsDefined(typeof(CustomRPC), (int)callId)
    && !TrustedRpc(callId)) //ホストではなく、CustomRPCで、VersionCheckではない
        {
            Logger.Warn($"{__instance?.Data?.PlayerName}:{callId}({RPC.GetRpcName(callId)}) has been canceled because it was sent by someone other than the host", "CustomRPC");
            if (AmongUsClient.Instance.AmHost)
            {
                if (!EAC.ReceiveInvalidRpc(__instance, callId)) return false;
                AmongUsClient.Instance.KickPlayer(__instance.GetClientId(), false);
                Logger.Warn($"Received an uncredited RPC from {__instance?.Data?.PlayerName} and kicked it out", "Kick");
                Logger.SendInGame(string.Format(GetString("Warning.InvalidRpc"), __instance?.Data?.PlayerName));
            }
            return false;
        }
        return true;
    }
    public static void Postfix(PlayerControl __instance, [HarmonyArgument(0)] byte callId, [HarmonyArgument(1)] MessageReader reader)
    {
        // Process nothing but CustomRPC
        if (callId < (byte)CustomRPC.VersionCheck) return;

        var rpcType = (CustomRPC)callId;
        switch (rpcType)
        {
            case CustomRPC.AntiBlackout:
                if (Options.EndWhenPlayerBug.GetBool())
                {
                    Logger.Fatal($"{__instance?.Data?.PlayerName}({__instance.PlayerId}): {reader.ReadString()} 错误，根据设定终止游戏", "Anti-black");
                    ChatUpdatePatch.DoBlockChat = true;
                    Main.OverrideWelcomeMsg = string.Format(GetString("RpcAntiBlackOutNotifyInLobby"), __instance?.Data?.PlayerName, GetString("EndWhenPlayerBug"));
                    _ = new LateTask(() =>
                    {
                        Logger.SendInGame(string.Format(GetString("RpcAntiBlackOutEndGame"), __instance?.Data?.PlayerName));
                    }, 3f, "Anti-Black Msg SendInGame 1");

                    _ = new LateTask(() =>
                    {
                        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Error);
                        GameManager.Instance.LogicFlow.CheckEndCriteria();
                        RPC.ForceEndGame(CustomWinner.Error);
                    }, 5.5f, "Anti-Black End Game 1");
                }
                else if (GameStates.IsOnlineGame)
                {
                    Logger.Fatal($"{__instance?.Data?.PlayerName}({__instance.PlayerId}): Change Role Setting Postfix 错误，根据设定继续游戏", "Anti-black");
                    _ = new LateTask(() =>
                    {
                        Logger.SendInGame(string.Format(GetString("RpcAntiBlackOutIgnored"), __instance?.Data?.PlayerName));
                    }, 3f, "Anti-Black Msg SendInGame 2");
                }
                break;

            case CustomRPC.VersionCheck:
                try
                {
                    Version version = Version.Parse(reader.ReadString());
                    string tag = reader.ReadString();
                    string forkId = reader.ReadString();
                    bool cheating = reader.ReadBoolean();
                    if (__instance.GetClientId() < 0)
                        break;
                    Main.playerVersion[__instance.GetClientId()] = new PlayerVersion(version, tag, forkId);

                    if (Main.VersionCheat.Value && __instance.GetClientId() == AmongUsClient.Instance.HostId) RPC.RpcVersionCheck();

                    if (__instance.GetClientId() == Main.HostClientId && cheating)
                        Main.IsHostVersionCheating = true;

                    if (Main.VersionCheat.Value && AmongUsClient.Instance.AmHost)
                        Main.playerVersion[__instance.GetClientId()] = Main.playerVersion[AmongUsClient.Instance.HostId];

                    // Kick Unmached Player Start
                    if (AmongUsClient.Instance.AmHost)
                    {
                        if (!IsVersionMatch(__instance.GetClientId()))
                        {
                            _ = new LateTask(() =>
                            {
                                if (__instance?.Data?.Disconnected is not null and not true)
                                {
                                    var msg = string.Format(GetString("KickBecauseDiffrentVersionOrMod"), __instance?.Data?.PlayerName);
                                    Logger.Warn(msg, "Version Kick");
                                    Logger.SendInGame(msg);
                                    AmongUsClient.Instance.KickPlayer(__instance.GetClientId(), false);
                                }
                            }, 5f, "Kick Because Diffrent Version Or Mod");
                        }
                    }
                    // Kick Unmached Player End
                }
                catch
                {
                    Logger.Warn($"{__instance?.Data?.PlayerName}({__instance.PlayerId}): バージョン情報が無効です", "RpcVersionCheck");
                    
                    _ = new LateTask(() =>
                    {
                        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.RequestRetryVersionCheck, SendOption.Reliable, __instance.GetClientId());
                        AmongUsClient.Instance.FinishRpcImmediately(writer);
                    }, 1f, "Retry Version Check Task");
                }
                break;

            case CustomRPC.RequestRetryVersionCheck:
                RPC.RpcVersionCheck();
                break;

            case CustomRPC.SyncCustomSettings:
                if (AmongUsClient.Instance.AmHost) break;

                List<OptionItem> listOptions = [];
                List<OptionItem> allOptionsList = [.. OptionItem.AllOptions];

                var startAmount = reader.ReadPackedInt32();
                var lastAmount = reader.ReadPackedInt32();

                var countAllOptions = OptionItem.AllOptions.Count;

                // Add Options
                for (var option = startAmount; option < countAllOptions && option <= lastAmount; option++)
                {
                    listOptions.Add(allOptionsList[option]);
                }

                var countOptions = listOptions.Count;
                Logger.Msg($"StartAmount/LastAmount: {startAmount}/{lastAmount} :--: ListOptionsCount/AllOptions: {countOptions}/{countAllOptions}", "CustomRPC.SyncCustomSettings");

                // Sync Settings
                foreach (var option in listOptions.ToArray())
                {
                    // Set Value Options
                    option.SetValue(reader.ReadPackedInt32());

                    // Set Preset 5 for modded non-host players
                    if (startAmount == 0 && option.Name == "Preset" && option.CurrentValue != 4)
                    {
                        option.SetValue(4); // 4 => Preset 5
                    }
                }
                OptionShower.GetText();
                break;

            case CustomRPC.SetDeathReason:
                RPC.GetDeathReason(reader);
                break;
            case CustomRPC.EndGame:
                RPC.EndGame(reader);
                break;
            case CustomRPC.PlaySound:
                byte playerID = reader.ReadByte();
                Sounds sound = (Sounds)reader.ReadByte();
                RPC.PlaySound(playerID, sound);
                break;
            case CustomRPC.ShowPopUp:
                string msg = reader.ReadString();
                HudManager.Instance.ShowPopUp(msg);
                break;
            case CustomRPC.SetCustomRole:
                byte CustomRoleTargetId = reader.ReadByte();
                CustomRoles role = (CustomRoles)reader.ReadPackedInt32();
                RPC.SetCustomRole(CustomRoleTargetId, role);
                break;
            case CustomRPC.SyncRoleSkill:
                RPC.SyncRoleSkillReader(reader);
                break;
            case CustomRPC.SetBountyTarget:
                BountyHunter.ReceiveRPC(reader);
                break;
            case CustomRPC.SyncPuppet:
                Puppeteer.ReceiveRPC(reader);
                break;
            case CustomRPC.SyncHuntsmanTarget:
                Huntsman.ReceiveRPC(reader);
                break;
             case CustomRPC.SyncKami:
                Kamikaze.ReceiveRPC(reader);
                break;
            case CustomRPC.SetKillOrSpell:
                Witch.ReceiveRPC(reader, false);
                break;
            case CustomRPC.SetKillOrHex:
                HexMaster.ReceiveRPC(reader, false);
                break;
            //case CustomRPC.SetKillOrCurse:
            //    Occultist.ReceiveRPC(reader, false);
            //    break;

            case CustomRPC.SetCaptainTargetSpeed:
                Captain.ReceiveRPCSetSpeed(reader);
                break;
            case CustomRPC.RevertCaptainTargetSpeed:
                Captain.ReceiveRPCRevertSpeed(reader);
                break;
            case CustomRPC.RevertCaptainAllTargetSpeed:
                Captain.ReceiveRPCRevertAllSpeed(reader);
                break;
            case CustomRPC.SetCaptainVotedTarget:
                Captain.ReceiveRPCVoteAdd(reader);
                break;
            case CustomRPC.RevertCaptainVoteRemove:
                Captain.ReceiveRPCVoteRemove(reader);
                break;

        /*    case CustomRPC.SetCopyCatMiscopyLimit:
                CopyCat.ReceiveRPC(reader);
                break; */
            case CustomRPC.SetDousedPlayer:
                byte ArsonistId = reader.ReadByte();
                byte DousedId = reader.ReadByte();
                bool doused = reader.ReadBoolean();
                Main.isDoused[(ArsonistId, DousedId)] = doused;
                break;
            case CustomRPC.setPlaguedPlayer:
                PlagueBearer.ReceiveRPC(reader);
                break;
            case CustomRPC.SetDrawPlayer:
                byte RevolutionistId = reader.ReadByte();
                byte DrawId = reader.ReadByte();
                bool drawed = reader.ReadBoolean();
                Main.isDraw[(RevolutionistId, DrawId)] = drawed;
                break;
            case CustomRPC.SetRevealedPlayer:
                byte FarseerId = reader.ReadByte();
                byte RevealId = reader.ReadByte();
                bool revealed = reader.ReadBoolean();
                Main.isRevealed[(FarseerId, RevealId)] = revealed;
                break;
            case CustomRPC.SetNameColorData:
                NameColorManager.ReceiveRPC(reader);
                break;
            case CustomRPC.DoSpell:
                Witch.ReceiveRPC(reader, true);
                break;
            case CustomRPC.DoHex:
                HexMaster.ReceiveRPC(reader, true);
                break;
            //case CustomRPC.DoCurse:
            //    Occultist.ReceiveRPC(reader, true);
            //    break;
            case CustomRPC.SniperSync:
                Sniper.ReceiveRPC(reader);
                break;
            case CustomRPC.UndertakerLocationSync:
                Undertaker.ReceiveRPC(reader);
                break;
            case CustomRPC.RiftMakerSyncData:
                RiftMaker.ReceiveRPC(reader);
                break;
            case CustomRPC.SetLoversPlayers:
                Main.LoversPlayers.Clear();
                int count = reader.ReadInt32();
                for (int i = 0; i < count; i++)
                    Main.LoversPlayers.Add(Utils.GetPlayerById(reader.ReadByte()));
                break;
            case CustomRPC.SetExecutionerTarget:
                Executioner.ReceiveRPC(reader, SetTarget: true);
                break;
            case CustomRPC.RemoveExecutionerTarget:
                Executioner.ReceiveRPC(reader, SetTarget: false);
                break;
            case CustomRPC.SetLawyerTarget:
                Lawyer.ReceiveRPC(reader, SetTarget: true);
                break;
            case CustomRPC.RemoveLawyerTarget:
                Lawyer.ReceiveRPC(reader, SetTarget: false);
                break;
            case CustomRPC.SendFireworkerState:
                Fireworker.ReceiveRPC(reader);
                break;
            case CustomRPC.SetCurrentDousingTarget:
                byte arsonistId = reader.ReadByte();
                byte dousingTargetId = reader.ReadByte();
                if (PlayerControl.LocalPlayer.PlayerId == arsonistId)
                    Main.currentDousingTarget = dousingTargetId;
                break;
            case CustomRPC.SetCurrentDrawTarget:
                byte arsonistId1 = reader.ReadByte();
                byte doTargetId = reader.ReadByte();
                if (PlayerControl.LocalPlayer.PlayerId == arsonistId1)
                    Main.currentDrawTarget = doTargetId;
                break;
            case CustomRPC.SetEvilTrackerTarget:
                EvilTracker.ReceiveRPC(reader);
                break;
            case CustomRPC.SyncPlagueDoctor:
                PlagueDoctor.ReceiveRPC(reader);
                break;
            case CustomRPC.SetRealKiller:
                byte targetId = reader.ReadByte();
                byte killerId = reader.ReadByte();
                RPC.SetRealKiller(targetId, killerId);
                break;
            case CustomRPC.SetGamerHealth:
                Gamer.ReceiveRPC(reader);
                break;
            case CustomRPC.SetPelicanEatenNum:
                Pelican.ReceiveRPC(reader);
                break;
            case CustomRPC.SetDoomsayerProgress:
                Doomsayer.ReceiveRPC(reader);
                break;
            case CustomRPC.SetTrackerTarget:
                Tracker.ReceiveRPC(reader);
                break;
            case CustomRPC.SetJailerExeLimit:
                Jailer.ReceiveRPC(reader, setTarget: false);
                break;
            case CustomRPC.SetJailerTarget:
                Jailer.ReceiveRPC(reader, setTarget: true);
                break;
            case CustomRPC.SetPursuerSellLimit:
                Pursuer.ReceiveRPC(reader);
                break;
            case CustomRPC.SetJackalRecruitLimit:
                Jackal.ReceiveRPC(reader);
                break;
            case CustomRPC.SetCPTasksDone:
                RPC.CrewpostorTasksRecieveRPC(reader);
                break;
            case CustomRPC.SetBanditStealLimit:
                Bandit.ReceiveRPC(reader);
                break;
            case CustomRPC.SetDoppelgangerStealLimit:
                Doppelganger.ReceiveRPC(reader);
                break;
            case CustomRPC.SyncAdmiredList:
                Admirer.ReceiveRPC(reader, true);
                break;
            case CustomRPC.SetRememberLimit:
                Amnesiac.ReceiveRPC(reader);
                break;
            case CustomRPC.SetImitateLimit:
                Imitator.ReceiveRPC(reader);
                break;
            case CustomRPC.PlayCustomSound:
                CustomSoundsManager.ReceiveRPC(reader);
                break;
            case CustomRPC.SetGhostPlayer:
                BallLightning.ReceiveRPC(reader);
                break;
            case CustomRPC.SetDarkHiderKillCount:
                DarkHide.ReceiveRPC(reader);
                break;
            case CustomRPC.SetGreedierOE:
                Greedier.ReceiveRPC(reader);
                break;
            case CustomRPC.SetCursedWolfSpellCount:
                byte CursedWolfId = reader.ReadByte();
                int GuardNum = reader.ReadInt32();
                if (Main.CursedWolfSpellCount.ContainsKey(CursedWolfId))
                    Main.CursedWolfSpellCount[CursedWolfId] = GuardNum;
                else
                    Main.CursedWolfSpellCount.Add(CursedWolfId, Options.GuardSpellTimes.GetInt());
                break;
            case CustomRPC.SetJinxSpellCount:
                byte JinxId = reader.ReadByte();
                int JinxGuardNum = reader.ReadInt32();
                if (Main.JinxSpellCount.ContainsKey(JinxId))
                    Main.JinxSpellCount[JinxId] = JinxGuardNum;
                else
                    Main.JinxSpellCount.Add(JinxId, Jinx.JinxSpellTimes.GetInt());
                break;
            case CustomRPC.SetCollectorVotes:
                Collector.ReceiveRPC(reader);
                break;
            case CustomRPC.TaskinatorMarkedTask:
                Taskinator.ReceiveRPC(reader);
                break;
            case CustomRPC.BenefactorRPC:
                Benefactor.ReceiveRPC(reader);
                break;
            case CustomRPC.RestTOHESetting:
                OptionItem.AllOptions.ToArray().Where(x => x.Id > 0).Do(x => x.SetValueNoRpc(x.DefaultValue));
                OptionShower.GetText();
                break;
            case CustomRPC.GuessKill:
                GuessManager.RpcClientGuess(Utils.GetPlayerById(reader.ReadByte()));
                break;
            case CustomRPC.SetMarkedPlayer:
                Assassin.ReceiveRPC(reader);
                break;
            case CustomRPC.SetMedicalerProtectList:
                Medic.ReceiveRPCForProtectList(reader);
                break;
            case CustomRPC.SyncPsychicRedList:
                Psychic.ReceiveRPC(reader);
                break;
            case CustomRPC.SetKillTimer:
                float time = reader.ReadSingle();
                PlayerControl.LocalPlayer.SetKillTimer(time);
                break;
            case CustomRPC.SyncFFAPlayer:
                FFAManager.ReceiveRPCSyncFFAPlayer(reader);
                break;
            case CustomRPC.SyncAllPlayerNames:
                Main.AllPlayerNames = [];
                int num = reader.ReadPackedInt32();
                for (int i = 0; i < num; i++)
                    Main.AllPlayerNames.TryAdd(reader.ReadByte(), reader.ReadString());
                break;
            case CustomRPC.SyncFFANameNotify:
                FFAManager.ReceiveRPCSyncNameNotify(reader);
                break;
            case CustomRPC.SyncSolsticerNotify:
                Solsticer.ReceiveRPC(reader);
                break;
            case CustomRPC.SyncShroud:
                Shroud.ReceiveRPC(reader);
                break;
            case CustomRPC.SetMorticianArrow:
                Mortician.ReceiveRPC(reader);
                break;
            case CustomRPC.SetTracefinderArrow:
                Tracefinder.ReceiveRPC(reader);
                break;
            case CustomRPC.SyncNameNotify:
                NameNotifyManager.ReceiveRPC(reader);
                break;
            case CustomRPC.Judge:
                Judge.ReceiveRPC(reader, __instance);
                break;
            case CustomRPC.PresidentEnd:
                President.ReceiveRPC(reader, __instance);
                break;
            case CustomRPC.PresidentReveal:
                President.ReceiveRPC(reader, __instance, isEnd: false);
                break;
            case CustomRPC.MeetingKill:
                Councillor.ReceiveRPC(reader, __instance);
                break;
            case CustomRPC.Guess:
                GuessManager.ReceiveRPC(reader, __instance);
                break;
            case CustomRPC.MafiaRevenge:
                MafiaRevengeManager.ReceiveRPC(reader, __instance);
                break;
            case CustomRPC.RetributionistRevenge:
                RetributionistRevengeManager.ReceiveRPC(reader, __instance);
                break;
            case CustomRPC.SetWraithTimer:
                Wraith.ReceiveRPC(reader);
                break;
            case CustomRPC.SetChameleonTimer:
                Chameleon.ReceiveRPC(reader);
                break;
            case CustomRPC.SetAlchemistTimer:
                Alchemist.ReceiveRPC(reader);
                break;
            case CustomRPC.SetBKTimer:
                BloodKnight.ReceiveRPC(reader);
                break;
            case CustomRPC.SyncTotocalcioTargetAndTimes:
                Totocalcio.ReceiveRPC(reader);
                break;
            case CustomRPC.SyncRomanticTarget:
                Romantic.ReceiveRPC(reader);
                break;
            case CustomRPC.SyncVengefulRomanticTarget:
                VengefulRomantic.ReceiveRPC(reader);
                break;
            case CustomRPC.SetSuccubusCharmLimit:
                Succubus.ReceiveRPC(reader);
                break;
            case CustomRPC.SetCursedSoulCurseLimit:
                CursedSoul.ReceiveRPC(reader);
                break;
            case CustomRPC.SetEvilDiviner:
                EvilDiviner.ReceiveRPC(reader);
                break;
            case CustomRPC.SetPotionMaster:
                PotionMaster.ReceiveRPC(reader);
                break;
            case CustomRPC.SetInvestgatorLimit:
                Investigator.ReceiveRPC(reader);
                break;
            case CustomRPC.SetVirusInfectLimit:
                Virus.ReceiveRPC(reader);
                break;
            case CustomRPC.KillFlash:
                Utils.FlashColor(new(1f, 0f, 0f, 0.3f));
                if (Constants.ShouldPlaySfx()) RPC.PlaySound(PlayerControl.LocalPlayer.PlayerId, Sounds.KillSound);
                break;
            case CustomRPC.DumpLog:
                var target = Utils.GetPlayerById(reader.ReadByte());
                if (target != null && !target.FriendCode.GetDevUser().DeBug)
                {
                    Logger.Info($"Player {target.GetNameWithRole()} used /dump", "RPC_DumpLogger");
                }
                break;
            case CustomRPC.SetBloodhoundArrow:
                Bloodhound.ReceiveRPC(reader);
                break;
            case CustomRPC.SetBloodhoundkKillerArrow:
                Bloodhound.ReceiveRPCKiller(reader);
                break;
            case CustomRPC.SetVultureArrow:
                Vulture.ReceiveRPC(reader);
                break;
            case CustomRPC.SyncVultureBodyAmount:
                Vulture.ReceiveBodyRPC(reader);
                break;
            case CustomRPC.SetSpiritcallerSpiritLimit:
                Spiritcaller.ReceiveRPC(reader);
                break;
            case CustomRPC.SetSeekerTarget:
                Seeker.ReceiveRPC(reader);
                break;
            case CustomRPC.SetSeekerPoints:
                Seeker.ReceiveRPC(reader, setTarget: false);
                break;
            case CustomRPC.SpyRedNameSync:
                Spy.ReceiveRPC(reader);
                break;
            case CustomRPC.SpyRedNameRemove:
                Spy.ReceiveRPC(reader, isRemove: true);
                break;
            case CustomRPC.RpcPassBomb:
                Agitater.ReceiveRPC(reader);
                break;
            //case CustomRPC.SetCleanserCleanLimit:
            //    Cleanser.ReceiveRPC(reader);
            //    break;
            case CustomRPC.PirateSyncData:
                Pirate.ReceiveRPC(reader);
                break;
            case CustomRPC.SetSoulCollectorLimit:
                SoulCollector.ReceiveRPC(reader);
                break;
            case CustomRPC.SyncSchrodingerData:
                SchrodingersCat.ReceiveRPC(reader);
                break;
            case CustomRPC.SetPixieTargets:
                Pixie.ReceiveRPC(reader);
                break;
            case CustomRPC.SetInspectorLimit:
                Inspector.ReceiveRPC(reader);
                break;
            case CustomRPC.KeeperRPC:
                Keeper.ReceiveRPC(reader);
                break;
            case CustomRPC.SetSwapperVotes:
                Swapper.ReceiveSwapRPC(reader, __instance);
                break;
            case CustomRPC.SyncMiniCrewAge:
                Mini.ReceiveRPC(reader);
                break;
            case CustomRPC.QuizmasterMarkPlayer:
                Quizmaster.ReceiveRPC(reader);
                break;
        }
    }

    private static bool IsVersionMatch(int ClientId)
    {
        if (Main.VersionCheat.Value) return true;
        Version version = Main.playerVersion[ClientId].version;
        string tag = Main.playerVersion[ClientId].tag;
        string forkId = Main.playerVersion[ClientId].forkId;
        
        if (version != Main.version
            || tag != $"{ThisAssembly.Git.Commit}({ThisAssembly.Git.Branch})"
            || forkId != Main.ForkId)
            return false;

        return true;
    }
}

internal static class RPC
{
    //SyncCustomSettingsRPC Sender
    public static void SyncCustomSettingsRPC(int targetId = -1)
    {
        if (targetId != -1)
        {
            var client = Utils.GetClientById(targetId);
            if (client == null || client.Character == null || !Main.playerVersion.ContainsKey(client.Id))
            {
                return;
            }
        }

        if (!AmongUsClient.Instance.AmHost || PlayerControl.AllPlayerControls.Count <= 1 || (AmongUsClient.Instance.AmHost == false && PlayerControl.LocalPlayer == null))
        {
            return;
        }

        var amount = OptionItem.AllOptions.Count;
        int divideBy = amount / 4;

        for (var i = 0; i <= 4; i++)
        {
            SyncOptionsBetween(i * divideBy, (i + 1) * divideBy, amount, targetId);
        }
    }

    static void SyncOptionsBetween(int startAmount, int lastAmount, int amountAllOptions, int targetId = -1)
    {
        if (targetId != -1)
        {
            var client = Utils.GetClientById(targetId);
            if (client == null || client.Character == null || !Main.playerVersion.ContainsKey(client.Id))
            {
                return;
            }
        }

        if (!AmongUsClient.Instance.AmHost || PlayerControl.AllPlayerControls.Count <= 1 || (AmongUsClient.Instance.AmHost == false && PlayerControl.LocalPlayer == null))
        {
            return;
        }

        if (amountAllOptions != OptionItem.AllOptions.Count)
        {
            amountAllOptions = OptionItem.AllOptions.Count;
        }

        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncCustomSettings, SendOption.Reliable, targetId);

        writer.WritePacked(startAmount);
        writer.WritePacked(lastAmount);

        List<OptionItem> listOptions = [];
        List<OptionItem> allOptionsList = [.. OptionItem.AllOptions];

        // Add Options
        for (var option = startAmount; option < amountAllOptions && option <= lastAmount; option++)
        {
            listOptions.Add(allOptionsList[option]);
        }

        var countListOptions = listOptions.Count;
        Logger.Msg($"StartAmount/LastAmount: {startAmount}/{lastAmount} :--: ListOptionsCount/AllOptions: {countListOptions}/{amountAllOptions}", "SyncOptionsBetween");

        // Sync Settings
        foreach (var option in listOptions.ToArray())
        {
            writer.WritePacked(option.GetValue());
        }

        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }

    // Not Used
    public static void SyncCustomSettingsRPCforOneOption(OptionItem option)
    {
        List<OptionItem> allOptions = new(OptionItem.AllOptions);
        var placement = allOptions.IndexOf(option);
        if (placement != -1)
            SyncOptionsBetween(placement, placement, OptionItem.AllOptions.Count);
    }
    public static void PlaySoundRPC(byte PlayerID, Sounds sound)
    {
        if (AmongUsClient.Instance.AmHost)
            PlaySound(PlayerID, sound);
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.PlaySound, SendOption.Reliable, -1);
        writer.Write(PlayerID);
        writer.Write((byte)sound);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void SyncAllPlayerNames()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncAllPlayerNames, SendOption.Reliable, -1);
        writer.WritePacked(Main.AllPlayerNames.Count);
        foreach (var name in Main.AllPlayerNames)
        {
            writer.Write(name.Key);
            writer.Write(name.Value);
        }
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ShowPopUp(this PlayerControl pc, string msg)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.ShowPopUp, SendOption.Reliable, pc.GetClientId());
        writer.Write(msg);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ExileAsync(PlayerControl player)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(player.NetId, (byte)RpcCalls.Exiled, SendOption.Reliable, -1);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        player.Exiled();
    }
    public static async void RpcVersionCheck()
    {
        try
        {
            while (PlayerControl.LocalPlayer == null || AmongUsClient.Instance.HostId < 0 || PlayerControl.LocalPlayer.GetClientId() < 0) await Task.Delay(500);
            var hostId = AmongUsClient.Instance.HostId;
            if (Main.playerVersion.ContainsKey(hostId) || !Main.VersionCheat.Value)
            {
                bool cheating = Main.VersionCheat.Value;
                MessageWriter writer = AmongUsClient.Instance.StartRpc(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.VersionCheck, SendOption.Reliable);
                writer.Write(cheating ? Main.playerVersion[hostId].version.ToString() : Main.PluginVersion);
                writer.Write(cheating ? Main.playerVersion[hostId].tag : $"{ThisAssembly.Git.Commit}({ThisAssembly.Git.Branch})");
                writer.Write(cheating ? Main.playerVersion[hostId].forkId : Main.ForkId);
                writer.Write(cheating);
                writer.EndMessage();
            }
            Main.playerVersion[PlayerControl.LocalPlayer.GetClientId()] = new PlayerVersion(Main.PluginVersion, $"{ThisAssembly.Git.Commit}({ThisAssembly.Git.Branch})", Main.ForkId);
        }
        catch
        {
            Logger.Error("Error while trying to send RPCVersionCheck, retry later", "RpcVersionCheck");
            _ = new LateTask(() => RpcVersionCheck(), 1f, "Retry RPCVersionCheck");
        }
    }
    public static async void RpcRequestRetryVersionCheck()
    {
        while (PlayerControl.LocalPlayer == null || AmongUsClient.Instance.GetHost() == null) await Task.Delay(500);
        var hostId = AmongUsClient.Instance.HostId;
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.RequestRetryVersionCheck, SendOption.Reliable, hostId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void SendDeathReason(byte playerId, PlayerState.DeathReason deathReason)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetDeathReason, SendOption.Reliable, -1);
        writer.Write(playerId);
        writer.WritePacked((int)deathReason);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void GetDeathReason(MessageReader reader)
    {
        var playerId = reader.ReadByte();
        var deathReason = (PlayerState.DeathReason)reader.ReadInt32();
        Main.PlayerStates[playerId].deathReason = deathReason;
        Main.PlayerStates[playerId].IsDead = true;
    }
    public static void ForceEndGame(CustomWinner win)
    {
        if (ShipStatus.Instance == null) return;
        try { CustomWinnerHolder.ResetAndSetWinner(win); }
        catch { }
        if (AmongUsClient.Instance.AmHost)
        {
            ShipStatus.Instance.enabled = false;
            try { GameManager.Instance.LogicFlow.CheckEndCriteria(); }
            catch { }
            try { GameManager.Instance.RpcEndGame(GameOverReason.ImpostorDisconnect, false); }
            catch { }
        }
    }
    public static void EndGame(MessageReader reader)
    {
        try
        {
            CustomWinnerHolder.ReadFrom(reader);
        }
        catch (Exception ex)
        {
            Logger.Error($"正常にEndGameを行えませんでした。\n{ex}", "EndGame", false);
        }
    }
    public static void PlaySound(byte playerID, Sounds sound)
    {
        if (PlayerControl.LocalPlayer.PlayerId == playerID)
        {
            switch (sound)
            {
                case Sounds.KillSound:
                    SoundManager.Instance.PlaySound(PlayerControl.LocalPlayer.KillSfx, false, 1f);
                    break;
                case Sounds.TaskComplete:
                    SoundManager.Instance.PlaySound(DestroyableSingleton<HudManager>.Instance.TaskCompleteSound, false, 1f);
                    break;
                case Sounds.TaskUpdateSound:
                    SoundManager.Instance.PlaySound(DestroyableSingleton<HudManager>.Instance.TaskUpdateSound, false, 1f);
                    break;
                case Sounds.ImpTransform:
                    SoundManager.Instance.PlaySound(DestroyableSingleton<HnSImpostorScreamSfx>.Instance.HnSOtherImpostorTransformSfx, false, 0.8f);
                    break;
            }
        }
    }
    public static void SetCustomRole(byte targetId, CustomRoles role)
    {
        if (role < CustomRoles.NotAssigned)
        {
            Main.PlayerStates[targetId].SetMainRole(role);
        }
        else if (role >= CustomRoles.NotAssigned)   //500:NoSubRole 501~:SubRole 
        {
            Main.PlayerStates[targetId].SetSubRole(role);
        }
        switch (role)
        {
            case CustomRoles.BountyHunter:
                BountyHunter.Add(targetId);
                break;
            case CustomRoles.Mercenary:
                Mercenary.Add(targetId);
                break;
            case CustomRoles.Fireworker:
                Fireworker.Add(targetId);
                break;
            case CustomRoles.TimeThief:
                TimeThief.Add(targetId);
                break;
            case CustomRoles.Puppeteer:
                Puppeteer.Add(targetId);
                break;
            case CustomRoles.Mastermind:
                Mastermind.Add(targetId);
                break;
            case CustomRoles.Sniper:
                Sniper.Add(targetId);
                break;
            case CustomRoles.Undertaker:
                Undertaker.Add(targetId);
                break;
            case CustomRoles.RiftMaker:
                RiftMaker.Add(targetId);
                break;
            case CustomRoles.Crusader:
                Crusader.Add(targetId);
                break;
        /*    case CustomRoles.Mare:
                Mare.Add(targetId);
                break; */
            case CustomRoles.EvilTracker:
                EvilTracker.Add(targetId);
                break;
            case CustomRoles.Witch:
                Witch.Add(targetId);
                break;
            case CustomRoles.Vampire:
                Vampire.Add(targetId);
                break;
            case CustomRoles.Vampiress:
                Vampiress.Add(targetId);
                break;
            case CustomRoles.Executioner:
                Executioner.Add(targetId);
                break;
            case CustomRoles.Farseer:
                Farseer.Add(targetId);
                break;
            case CustomRoles.Lawyer:
                Lawyer.Add(targetId);
                break;
            case CustomRoles.HexMaster:
                HexMaster.Add(targetId);
                break;
            //case CustomRoles.Occultist:
            //    Occultist.Add(targetId);
            //    break;
            case CustomRoles.Camouflager:
                Camouflager.Add();
                break;
            case CustomRoles.Jackal:
                Jackal.Add(targetId);
                break;
            case CustomRoles.Sidekick:
                Sidekick.Add(targetId);
                break;
            case CustomRoles.Bandit:
                Bandit.Add(targetId);
                break;
            case CustomRoles.Doppelganger:
                Doppelganger.Add(targetId);
                break;
            case CustomRoles.Poisoner:
                Poisoner.Add(targetId);
                break;
            case CustomRoles.PlagueDoctor:
                PlagueDoctor.Add(targetId);
                break;
            case CustomRoles.Sheriff:
                Sheriff.Add(targetId);
                break;
            case CustomRoles.CopyCat:
                CopyCat.Add(targetId);
                break;
            case CustomRoles.Captain:
                Captain.Add(targetId);
                break;
            case CustomRoles.GuessMaster:
                GuessMaster.Add(targetId);
                break;
            case CustomRoles.Pickpocket:
                Pickpocket.Add(targetId);
                break;
            case CustomRoles.Cleanser:
                Cleanser.Add(targetId);
                break;
            case CustomRoles.SoulCollector:
                SoulCollector.Add(targetId);
                break;
            case CustomRoles.SchrodingersCat:
                SchrodingersCat.Add(targetId);
                break;
            case CustomRoles.Agitater:
                Agitater.Add(targetId);
                break;
            case CustomRoles.QuickShooter:
                QuickShooter.Add(targetId);
                break;
            case CustomRoles.Observer:
                Observer.Add(targetId);
                break;
            case CustomRoles.SwordsMan:
                SwordsMan.Add(targetId);
                break;
            case CustomRoles.SabotageMaster:
                SabotageMaster.Add(targetId);
                break;
            case CustomRoles.Snitch:
                Snitch.Add(targetId);
                break;
            case CustomRoles.Chronomancer:
                Chronomancer.Add(targetId);
                break;
            case CustomRoles.Medusa:
                Medusa.Add(targetId);
                break;
            case CustomRoles.Necromancer:
                Necromancer.Add(targetId);
                break;
            case CustomRoles.Marshall:
                Marshall.Add(targetId);
                break;
            case CustomRoles.AntiAdminer:
                AntiAdminer.Add(targetId);
                break;
            case CustomRoles.Monitor:
                Monitor.Add(targetId);
                break;
            case CustomRoles.LastImpostor:
                LastImpostor.Add(targetId);
                break;
            case CustomRoles.Aware:
                Aware.Add(targetId);
                break;
            case CustomRoles.Crewpostor:
                Main.CrewpostorTasksDone[targetId] = 0;
                break;
            case CustomRoles.TimeManager:
                TimeManager.Add(targetId);
                break;
            case CustomRoles.Workhorse:
                Workhorse.Add(targetId);
                break;
            case CustomRoles.Pelican:
                Pelican.Add(targetId);
                break;
            case CustomRoles.Glitch:
                Glitch.Add(targetId);
                break;
            case CustomRoles.Counterfeiter:
                Counterfeiter.Add(targetId);
                break;
            case CustomRoles.Jailer:
                Jailer.Add(targetId);
                break;
            case CustomRoles.Pursuer:
                Pursuer.Add(targetId);
                break;
            case CustomRoles.Gangster:
                Gangster.Add(targetId);
                break;
            case CustomRoles.EvilDiviner:
                EvilDiviner.Add(targetId);
                break;
            case CustomRoles.PotionMaster:
                PotionMaster.Add(targetId);
                break;
            case CustomRoles.Medic:
                Medic.Add(targetId);
                break;
            case CustomRoles.Divinator:
                Divinator.Add(targetId);
                break;
            case CustomRoles.Oracle:
                Oracle.Add(targetId);
                break;
            case CustomRoles.Gamer:
                Gamer.Add(targetId);
                break;
            case CustomRoles.BallLightning:
                BallLightning.Add(targetId);
                break;
            case CustomRoles.DarkHide:
                DarkHide.Add(targetId);
                break;
            case CustomRoles.Stealth:
                Stealth.Add(targetId);
                break;
            case CustomRoles.Penguin:
                Penguin.Add(targetId);
                break;
            case CustomRoles.Greedier:
                Greedier.Add(targetId);
                break;
            case CustomRoles.Collector:
                Collector.Add(targetId);
                break;
            case CustomRoles.Taskinator:
                Taskinator.Add(targetId);
                break;
            case CustomRoles.Benefactor:
                Benefactor.Add(targetId);
                break;
            case CustomRoles.CursedWolf:
                Main.CursedWolfSpellCount[targetId] = Options.GuardSpellTimes.GetInt();
                break;
            case CustomRoles.Jinx:
                Main.JinxSpellCount[targetId] = Jinx.JinxSpellTimes.GetInt();
                Jinx.Add(targetId);
                break;
            case CustomRoles.Eraser:
                Eraser.Add(targetId);
                break;
            case CustomRoles.Assassin:
                Assassin.Add(targetId);
                break;
            case CustomRoles.Arrogance:
                Arrogance.Add(targetId);
                break;
            case CustomRoles.Juggernaut:
                Juggernaut.Add(targetId);
                break;
            case CustomRoles.Reverie:
                Reverie.Add(targetId);
                break;
            case CustomRoles.Anonymous:
                Anonymous.Add(targetId);
                break;
            case CustomRoles.Psychic:
                Psychic.Add(targetId);
                break;
            case CustomRoles.Hangman:
                Hangman.Add(targetId);
                break;
            case CustomRoles.Judge:
                Judge.Add(targetId);
                break;
            case CustomRoles.President:
                President.Add(targetId);
                break;
            case CustomRoles.Inspector:
                Inspector.Add(targetId);
                break;
            case CustomRoles.Keeper:
                Keeper.Add(targetId);
                break;
            case CustomRoles.Councillor:
                Councillor.Add(targetId);
                break;
            case CustomRoles.Mortician:
                Mortician.Add(targetId);
                break;
            case CustomRoles.Tracefinder:
                Tracefinder.Add(targetId);
                break;
            case CustomRoles.Mediumshiper:
                Mediumshiper.Add(targetId);
                break;
            case CustomRoles.Veteran:
                Main.VeteranNumOfUsed.Add(targetId, Options.VeteranSkillMaxOfUseage.GetInt());
                break;
            case CustomRoles.Grenadier:
                Main.GrenadierNumOfUsed.Add(targetId, Options.GrenadierSkillMaxOfUseage.GetInt());
                break;
            case CustomRoles.Lighter:
                Main.LighterNumOfUsed.Add(targetId, Options.LighterSkillMaxOfUseage.GetInt());
                break;
            case CustomRoles.Bastion:
                Main.BastionNumberOfAbilityUses = Options.BastionMaxBombs.GetInt();
                break;
            case CustomRoles.TimeMaster:
                Main.TimeMasterNumOfUsed.Add(targetId, Options.TimeMasterMaxUses.GetInt());
                break;
            case CustomRoles.Swooper:
                Swooper.Add(targetId);
                break;
            case CustomRoles.Wraith:
                Wraith.Add(targetId);
                break;
            case CustomRoles.Chameleon:
                Chameleon.Add(targetId);
                break;
            case CustomRoles.BloodKnight:
                BloodKnight.Add(targetId);
                break;
            case CustomRoles.Alchemist:
                Alchemist.Add(targetId);
                break;
            case CustomRoles.Totocalcio:
                Totocalcio.Add(targetId);
                break;
            case CustomRoles.Romantic:
                Romantic.Add(targetId);
                break;
            case CustomRoles.VengefulRomantic:
                VengefulRomantic.Add(targetId);
                break;
            case CustomRoles.RuthlessRomantic:
                RuthlessRomantic.Add(targetId);
                break;
            case CustomRoles.Succubus:
                Succubus.Add(targetId);
                break;
            case CustomRoles.CursedSoul:
                CursedSoul.Add(targetId);
                break;
            case CustomRoles.Admirer:
                Admirer.Add(targetId);
                break;
            case CustomRoles.Amnesiac:
                Amnesiac.Add(targetId);
                break;
            case CustomRoles.Imitator:
                Imitator.Add(targetId);
                break;
            case CustomRoles.DovesOfNeace:
                Main.DovesOfNeaceNumOfUsed.Add(targetId, Options.DovesOfNeaceMaxOfUseage.GetInt());
                break;
            case CustomRoles.Infectious:
                Infectious.Add(targetId);
                break;
            case CustomRoles.Monarch:
                Monarch.Add(targetId);
                break;
            case CustomRoles.Deputy:
                Deputy.Add(targetId);
                break;
            case CustomRoles.Investigator:
                Investigator.Add(targetId);
                break;
            case CustomRoles.Virus:
                Virus.Add(targetId);
                break;
            case CustomRoles.Bloodhound:
                Bloodhound.Add(targetId); 
                break;
            case CustomRoles.Vulture:
                Vulture.Add(targetId); 
                break;
            case CustomRoles.PlagueBearer:
                PlagueBearer.Add(targetId);
                break;
            case CustomRoles.Tracker:
                Tracker.Add(targetId);
                break;
            case CustomRoles.Merchant:
                Merchant.Add(targetId);
                break;
            case CustomRoles.SerialKiller:
                SerialKiller.Add(targetId);
                break;
            case CustomRoles.Pyromaniac:
                Pyromaniac.Add(targetId);
                break;
            case CustomRoles.Werewolf:
                Werewolf.Add(targetId);
                break;
            case CustomRoles.Traitor:
                Traitor.Add(targetId);
                break;
            case CustomRoles.Huntsman:
                Huntsman.Add(targetId);
                break;
            case CustomRoles.Kamikaze:
                Kamikaze.Add(targetId);
                break;
            case CustomRoles.Shroud:
                Shroud.Add(targetId);
                break;
            case CustomRoles.Maverick:
                Maverick.Add(targetId);
                break;
            case CustomRoles.Dazzler:
                Dazzler.Add(targetId);
                break;
            case CustomRoles.Addict:
                Addict.Add(targetId);
                break;
            case CustomRoles.Mole:
                Mole.Add(targetId);
                break;
            case CustomRoles.Deathpact:
                Deathpact.Add(targetId);
                break;
            case CustomRoles.Wildling:
                Wildling.Add(targetId);
                break;
            case CustomRoles.Morphling:
                Morphling.Add(targetId);
                break;
            case CustomRoles.Devourer:
                Devourer.Add(targetId);
                break;
            case CustomRoles.Spiritualist:
                Spiritualist.Add(targetId);
                break;
            case CustomRoles.Spiritcaller:
                Spiritcaller.Add(targetId);
                break;
            case CustomRoles.Lurker:
                Lurker.Add(targetId);
                break;
            case CustomRoles.Doomsayer:
                Doomsayer.Add(targetId);
                break;
            case CustomRoles.Pirate:
                Pirate.Add(targetId);
                break;
            case CustomRoles.Pixie:
                Pixie.Add(targetId);
                break;
            case CustomRoles.Seeker:
                Seeker.Add(targetId);
                break;
            case CustomRoles.Pitfall:
                Pitfall.Add(targetId);
                break;
            case CustomRoles.Swapper: 
                Swapper.Add(targetId);
                break;
            case CustomRoles.ChiefOfPolice:
                ChiefOfPolice.Add(targetId);
                break;
            case CustomRoles.NiceMini:
                Mini.Add(targetId);
                break;
            case CustomRoles.EvilMini:
                Mini.Add(targetId);
                break;
            case CustomRoles.Blackmailer:
                Blackmailer.Add(targetId);
                break;
            case CustomRoles.Diseased:
                Diseased.Add();
                break;
            case CustomRoles.Antidote:
                Antidote.Add();
                break;
            case CustomRoles.Burst:
                Burst.Add();
                break;
            case CustomRoles.Fool:
                Fool.Add();
                break;
            case CustomRoles.Ghoul:
                Ghoul.Add();
                break;
            case CustomRoles.Spy:
                Spy.Add(targetId);
                break;
            case CustomRoles.Enigma:
                Enigma.Add(targetId);
                break;
            case CustomRoles.Instigator:
                Instigator.Add(targetId);
                break;
            case CustomRoles.Quizmaster:
                Quizmaster.Add(targetId);
                break;
            case CustomRoles.Rainbow:
                Rainbow.Add();
                break;
            case CustomRoles.Statue:
                Statue.Add(targetId);
                break;

        }
        HudManager.Instance.SetHudActive(true);
    //    HudManager.Instance.Chat.SetVisible(true);
        if (PlayerControl.LocalPlayer.PlayerId == targetId) RemoveDisableDevicesPatch.UpdateDisableDevices();
    }
    public static void SyncRoleSkillReader(MessageReader reader)
    {
        CustomRoles role = (CustomRoles)reader.ReadPackedInt32();
        Logger.Info($"Received Sync Role Skill RPC for role {role}", "SyncRoleSkillReader");

        switch (role)
        {
            //Crew Roles
            case CustomRoles.Admirer:
                Admirer.ReceiveRPC(reader, false);
                break;
            case CustomRoles.Bloodhound:
                Bloodhound.ReceiveRPCLimit(reader);
                break;
            case CustomRoles.Chameleon:
                Chameleon.ReceiveRPC(reader);
                break;
            case CustomRoles.ChiefOfPolice:
                ChiefOfPolice.ReceiveRPC(reader);
                break;
            case CustomRoles.Cleanser:
                Cleanser.ReceiveRPC(reader);
                break;
            case CustomRoles.Counterfeiter:
                Counterfeiter.ReceiveRPC(reader);
                break;
            case CustomRoles.Crusader:
                Crusader.ReceiveRPC(reader);
                break;
            case CustomRoles.Deputy:
                Deputy.ReceiveRPC(reader);
                break; 
                //Waiting for fix
            case CustomRoles.Divinator:
                Divinator.ReceiveRPC(reader);
                break;
            case CustomRoles.Medic:
                Medic.ReceiveRPC(reader);
                break;
            case CustomRoles.Mediumshiper:
                Mediumshiper.ReceiveRPC(reader);
                break;
            case CustomRoles.Monarch:
                Monarch.ReceiveRPC(reader);
                break;
            case CustomRoles.Oracle:
                Oracle.ReceiveRPC(reader);
                break;
            case CustomRoles.SabotageMaster:
                SabotageMaster.ReceiveRPC(reader);
                break;
            case CustomRoles.Sheriff:
                Sheriff.ReceiveRPC(reader);
                break;
            case CustomRoles.Spy:
                Spy.ReceiveRPC(reader, isAbility: true);
                break;
            case CustomRoles.Swapper:
                Swapper.ReceiveSkillRPC(reader);
                break;
            case CustomRoles.SwordsMan:
                SwordsMan.ReceiveRPC(reader);
                break;

            //Impostors
            case CustomRoles.Anonymous:
                Anonymous.ReceiveRPC(reader);
                break;
            //case CustomRoles.Councillor:
            //    break;
            //Wait for bug fix
            case CustomRoles.Eraser:
                Eraser.ReceiveRPC(reader);
                break;
            case CustomRoles.Gangster:
                Gangster.ReceiveRPC(reader);
                break;
            //case CustomRoles.Instigator:
            //    break;
            //Wait for bug fix
            case CustomRoles.Penguin:
                Penguin.ReceiveRPC(reader);
                break;
            case CustomRoles.QuickShooter:
                QuickShooter.ReceiveRPC(reader);
                break;
            case CustomRoles.Stealth:
                Stealth.ReceiveRPC(reader);
                break;
            case CustomRoles.Swooper:
                Swooper.ReceiveRPC(reader);
                break;
            case CustomRoles.Wildling:
                Wildling.ReceiveRPC(reader);
                break;
            //case CustomRoles.Witch:
            //    break;
            //Merge the two rpc into one

            default:
                Logger.Error($"Role {role} can not be handled!", "SyncRoleSkillReader");
                break;
        }
    }
    public static void RpcDoSpell(byte targetId, byte killerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.DoSpell, SendOption.Reliable, -1);
        writer.Write(targetId);
        writer.Write(killerId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void SyncLoversPlayers()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetLoversPlayers, SendOption.Reliable, -1);
        writer.Write(Main.LoversPlayers.Count);
        foreach (var lp in Main.LoversPlayers)
        {
            writer.Write(lp.PlayerId);
        }
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void SendRpcLogger(uint targetNetId, byte callId, int targetClientId = -1)
    {
        if (!DebugModeManager.AmDebugger) return;
        string rpcName = GetRpcName(callId);
        string from = targetNetId.ToString();
        string target = targetClientId.ToString();
        try
        {
            target = targetClientId < 0 ? "All" : AmongUsClient.Instance.GetClient(targetClientId).PlayerName;
            from = Main.AllPlayerControls.FirstOrDefault(c => c.NetId == targetNetId)?.Data?.PlayerName;
        }
        catch { }
        Logger.Info($"FromNetID:{targetNetId}({from}) TargetClientID:{targetClientId}({target}) CallID:{callId}({rpcName})", "SendRPC");
    }
    public static string GetRpcName(byte callId)
    {
        string rpcName;
        if ((rpcName = Enum.GetName(typeof(RpcCalls), callId)) != null) { }
        else if ((rpcName = Enum.GetName(typeof(CustomRPC), callId)) != null) { }
        else rpcName = callId.ToString();
        return rpcName;
    }
    public static void SetCurrentDousingTarget(byte arsonistId, byte targetId)
    {
        if (PlayerControl.LocalPlayer.PlayerId == arsonistId)
        {
            Main.currentDousingTarget = targetId;
        }
        else
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetCurrentDousingTarget, SendOption.Reliable, -1);
            writer.Write(arsonistId);
            writer.Write(targetId);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
    }
    public static void CrewpostorTasksSendRPC(byte cpID, int tasksDone)
    {
        if (PlayerControl.LocalPlayer.PlayerId == cpID)
        {
            if (Main.CrewpostorTasksDone.ContainsKey(cpID))
                Main.CrewpostorTasksDone[cpID] = tasksDone;
            else Main.CrewpostorTasksDone[cpID] = 0;
        }
        else
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetCPTasksDone, SendOption.Reliable, -1);
            writer.Write(cpID);
            writer.WritePacked(tasksDone);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
    }
    public static void CrewpostorTasksRecieveRPC(MessageReader reader)
    {
        byte PlayerId = reader.ReadByte();
        int tasksDone = reader.ReadInt32();
        if (Main.CrewpostorTasksDone.ContainsKey(PlayerId))
            Main.CrewpostorTasksDone[PlayerId] = tasksDone;
        else
            Main.CrewpostorTasksDone.Add(PlayerId, 0);
    }
    public static void SetCurrentDrawTarget(byte arsonistId, byte targetId)
    {
        if (PlayerControl.LocalPlayer.PlayerId == arsonistId)
        {
            Main.currentDrawTarget = targetId;
        }
        else
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetCurrentDrawTarget, SendOption.Reliable, -1);
            writer.Write(arsonistId);
            writer.Write(targetId);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
    }
    public static void SetCurrentRevealTarget(byte arsonistId, byte targetId)
    {
        if (PlayerControl.LocalPlayer.PlayerId == arsonistId)
        {
            Main.currentDrawTarget = targetId;
        }
        else
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetCurrentRevealTarget, SendOption.Reliable, -1);
            writer.Write(arsonistId);
            writer.Write(targetId);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
    }
    public static void SendRPCCursedWolfSpellCount(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetCursedWolfSpellCount, SendOption.Reliable, -1);
        writer.Write(playerId);
        writer.WritePacked(Main.CursedWolfSpellCount[playerId]);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void SendRPCJinxSpellCount(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetJinxSpellCount, SendOption.Reliable, -1);
        writer.Write(playerId);
        writer.WritePacked(Main.JinxSpellCount[playerId]);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ResetCurrentDousingTarget(byte arsonistId) => SetCurrentDousingTarget(arsonistId, 255);
    public static void ResetCurrentDrawTarget(byte arsonistId) => SetCurrentDrawTarget(arsonistId, 255);
    public static void ResetCurrentRevealTarget(byte arsonistId) => SetCurrentRevealTarget(arsonistId, 255);
    public static void SetRealKiller(byte targetId, byte killerId)
    {
        var state = Main.PlayerStates[targetId];
        state.RealKiller.Item1 = DateTime.Now;
        state.RealKiller.Item2 = killerId;

        if (!AmongUsClient.Instance.AmHost) return;
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetRealKiller, SendOption.Reliable, -1);
        writer.Write(targetId);
        writer.Write(killerId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
}
[HarmonyPatch(typeof(InnerNet.InnerNetClient), nameof(InnerNet.InnerNetClient.StartRpc))]
internal class StartRpcPatch
{
    public static void Prefix(/*InnerNet.InnerNetClient __instance,*/ [HarmonyArgument(0)] uint targetNetId, [HarmonyArgument(1)] byte callId)
    {
        RPC.SendRpcLogger(targetNetId, callId);
    }
}
[HarmonyPatch(typeof(InnerNet.InnerNetClient), nameof(InnerNet.InnerNetClient.StartRpcImmediately))]
internal class StartRpcImmediatelyPatch
{
    public static void Prefix(/*InnerNet.InnerNetClient __instance,*/ [HarmonyArgument(0)] uint targetNetId, [HarmonyArgument(1)] byte callId, [HarmonyArgument(3)] int targetClientId = -1)
    {
        RPC.SendRpcLogger(targetNetId, callId, targetClientId);
    }
}

