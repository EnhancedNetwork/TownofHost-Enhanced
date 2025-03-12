using AmongUs.GameOptions;
using Hazel;
using InnerNet;
using System;
using System.Threading.Tasks;
using TOHE.Modules;
using TOHE.Patches;
using TOHE.Roles.AddOns.Impostor;
using TOHE.Roles.Core;
using TOHE.Roles.Coven;
using TOHE.Roles.Crewmate;
using TOHE.Roles.Impostor;
using TOHE.Roles.Neutral;
using static TOHE.Translator;

namespace TOHE;

[Obfuscation(Exclude = true)]
public enum CustomRPC : byte // 174/255 USED
{
    // RpcCalls can increase with each AU version
    // On version 2024.6.18 the last id in RpcCalls: 65

    // Adding Role rpcs that overrides TOHE section and changing BetterCheck will be rejected
    // Sync Role Skill can be used under most cases so you should not make a new rpc unless it's necessary
    // NOTE: Set RPC's that are spammed to "ExtendedPlayerControl.RpcSendOption" to prevent kick due innersloth anti-cheat

    VersionCheck = 80,
    RequestRetryVersionCheck = 81,
    SyncCustomSettings = 100, // AUM use 101 rpc
    SetDeathReason = 102,
    EndGame,
    PlaySound,
    SetCustomRole,

    // TOHE
    AntiBlackout,
    SetRealKiller,
    PlayCustomSound,
    SetKillTimer,
    SyncAllPlayerNames,
    SyncNameNotify,
    ShowPopUp,
    KillFlash,
    DumpLog,
    SetNameColorData,
    GuessKill,
    Judge,
    Guess,
    KNChat = 119, // Kill network chat, may conflicts with judge and guess calls
    CouncillorJudge,
    NemesisRevenge,
    RetributionistRevenge,
    SetFriendCode,
    SyncLobbyTimer,
    SyncPlayerSetting,
    ShowChat,
    SyncShieldPersonDiedFirst,
    RemoveSubRole,
    FixModdedClientCNO,
    SyncGeneralOptions,
    SyncSpeedPlayer,
    Arrow,
    NotificationPopper,
    SyncDeadPassedMeetingList,
    SyncAbilityUseLimit,

    //Roles 
    SyncRoleSkill,
    SetBountyTarget,
    SyncPuppet,
    SetKillOrSpell,
    SetKillOrHex,
    SetDousedPlayer,
    DoSpell,
    DoHex,
    SniperSync,
    SetLoversPlayers,
    SendFireworkerState,
    SetCurrentDousingTarget,
    SetEvilTrackerTarget,
    SetDrawPlayer,

    // BetterAmongUs (BAU) RPC, This is sent to allow other BAU users know who's using BAU!
    BetterCheck = 150,

    SetCrewpostorTasksDone,
    SetCurrentDrawTarget,
    SyncJailerData,
    SetInspectorLimit,
    KeeperRPC,
    SetAlchemistTimer,
    UndertakerLocationSync,
    LightningSetGhostPlayer,
    SetConsigliere,
    SetGreedy,
    BenefactorRPC,
    SetSwapperVotes,
    SetMarkedPlayer,
    PresidentEnd,
    PresidentReveal,
    SetInvestgatorLimit,
    SetOverseerRevealedPlayer,
    SetOverseerTimer,
    SetChameleonTimer,
    SyncAdmiredList,
    DictatorRPC,
    Necronomicon,

    //FFA
    SyncFFAPlayer,
    SyncFFANameNotify,
    //Speed run
    SyncSpeedRunStates,
}
[Obfuscation(Exclude = true)]
public enum Sounds
{
    KillSound,
    TaskComplete,
    TaskUpdateSound,
    ImpTransform,
    SabotageSound,

    Test,
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.ShouldProcessRpc))]
class ShouldProcessRpcPatch
{
    /*
     * Sinse stupid AU code added check process rpc for outfit players, so need patch this
     * Always return true because the check is absolutely pointless
     */
    public static bool Prefix(PlayerControl __instance, RpcCalls rpc, byte sequenceId, ref bool __result)
    {
        if (rpc is RpcCalls.SetSkinStr)
            Logger.Info($"Player Id: {__instance.PlayerId} - Old skin sequenceId {__instance.Data.DefaultOutfit.SkinSequenceId} - New skin sequenceId {sequenceId}", "ShouldProcessRpc");

        __result = true;
        return false;
    }
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.HandleRpc))]
internal class RPCHandlerPatch
{
    public static bool TrustedRpc(byte id)
    => (CustomRPC)id is CustomRPC.VersionCheck
        or CustomRPC.RequestRetryVersionCheck
        or CustomRPC.AntiBlackout
        or CustomRPC.Judge
        or CustomRPC.CouncillorJudge
        or CustomRPC.NemesisRevenge
        or CustomRPC.RetributionistRevenge
        or CustomRPC.Guess
        or CustomRPC.PresidentEnd
        or CustomRPC.SetSwapperVotes
        or CustomRPC.DumpLog
        or CustomRPC.SetFriendCode
        or CustomRPC.BetterCheck
        or CustomRPC.DictatorRPC;
    public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] byte callId, [HarmonyArgument(1)] MessageReader reader)
    {
        var rpcType = (RpcCalls)callId;
        MessageReader subReader = MessageReader.Get(reader);
        if (EAC.PlayerControlReceiveRpc(__instance, callId, reader)) return false;
        Logger.Info($"{__instance?.Data?.PlayerId}({(__instance.IsHost() ? "Host" : __instance?.Data?.PlayerName)}):{callId}({RPC.GetRpcName(callId)})", "ReceiveRPC");
        switch (rpcType)
        {
            case RpcCalls.SetName: //SetNameRPC
                subReader.ReadUInt32();
                string name = subReader.ReadString();
                if (subReader.BytesRemaining > 0 && subReader.ReadBoolean()) return false;
                Logger.Info("RPC Set Name For Player: " + __instance.GetNameWithRole() + " => " + name, "SetName");
                break;
            case RpcCalls.SetRole: //SetRoleRPC
                var role = (RoleTypes)subReader.ReadUInt16();
                var canOverriddenRole = subReader.ReadBoolean();
                Logger.Info("RPC Set Role For Player: " + __instance.GetRealName() + " => " + role + " CanOverrideRole: " + canOverriddenRole, "SetRole");
                break;
            case RpcCalls.SendChat: // Free chat
                var text = subReader.ReadString();
                Logger.Info($"{__instance.GetNameWithRole().RemoveHtmlTags()}:{text.RemoveHtmlTags()}", "ReceiveChat");
                ChatCommands.OnReceiveChat(__instance, text, out var canceled);
                if (canceled) return false;
                break;
            case RpcCalls.SendQuickChat:
                Logger.Info($"{__instance.GetNameWithRole().RemoveHtmlTags()}:Some message from quick chat", "ReceiveChat");
                ChatCommands.OnReceiveChat(__instance, "Some message from quick chat", out var canceledQuickChat);
                if (canceledQuickChat) return false;
                break;
            case RpcCalls.StartMeeting:
                var p = Utils.GetPlayerById(subReader.ReadByte());
                Logger.Info($"{__instance.GetNameWithRole()} => {p?.GetNameWithRole() ?? "null"}", "StartMeeting");
                break;
        }
        if (!__instance.IsHost() &&
            ((Enum.IsDefined(typeof(CustomRPC), callId) && !TrustedRpc(callId)) // Is Custom RPC
            || (!Enum.IsDefined(typeof(CustomRPC), callId) && !Enum.IsDefined(typeof(RpcCalls), callId)))) //Is not Custom RPC and not Vanilla RPC
        {
            Logger.Warn($"{__instance?.Data?.PlayerName}:{callId}({RPC.GetRpcName(callId)}) has been canceled because it was sent by someone other than the host", "CustomRPC");
            if (AmongUsClient.Instance.AmHost)
            {
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
                CriticalErrorManager.ReadRpc(__instance, reader);
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

                    if (!Main.playerVersion.TryGetValue(__instance.GetClientId(), out _))
                    {
                        RPC.RpcVersionCheck();
                    }

                    Main.playerVersion[__instance.GetClientId()] = new PlayerVersion(version, tag, forkId);

                    if (Main.VersionCheat.Value && __instance.GetClientId() == AmongUsClient.Instance.HostId) RPC.RpcVersionCheck();

                    if (__instance.GetClientId() == Main.HostClientId && cheating)
                        Main.IsHostVersionCheating = true;

                    if (Main.VersionCheat.Value && AmongUsClient.Instance.AmHost)
                        Main.playerVersion[__instance.GetClientId()] = Main.playerVersion[AmongUsClient.Instance.HostId];

                    // Kick Unmached Player Start
                    if (AmongUsClient.Instance.AmHost)
                    {
                        if (!IsVersionMatch(__instance.GetClientId()) && !Main.VersionCheat.Value)
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

            case CustomRPC.SetFriendCode:
                RPC.SetFriendCode(__instance, reader.ReadString());
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
                break;

            case CustomRPC.RemoveSubRole:
                byte targetId = reader.ReadByte();
                var Subrole = (CustomRoles)reader.ReadPackedInt32();
                Main.PlayerStates[targetId].RemoveSubRole(Subrole);
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
                string message = reader.ReadString();
                string title = reader.ReadString();

                // add title
                if (title != "")
                    message = $"{title}\n{message}";

                HudManager.Instance.ShowPopUp(message);
                break;
            case CustomRPC.SetCustomRole:
                byte CustomRoleTargetId = reader.ReadByte();
                int roleId = reader.ReadPackedInt32();
                CustomRoles role = (CustomRoles)roleId;
                Logger.Info($"playerId: {CustomRoleTargetId} - roleid {roleId} - role: {role}", "CustomRPC.SetCustomRole");
                RPC.SetCustomRole(CustomRoleTargetId, role);
                break;
            case CustomRPC.SyncLobbyTimer:
                GameStartManagerPatch.timer = reader.ReadPackedInt32();
                break;
            case CustomRPC.SyncRoleSkill:
                RPC.SyncRoleSkillReader(reader);
                break;
            case CustomRPC.Arrow:
                {
                    if (reader.ReadBoolean()) TargetArrow.ReceiveRPC(reader);
                    else LocateArrow.ReceiveRPC(reader);
                }
                break;
            case CustomRPC.NotificationPopper:
                {
                    var item = reader.ReadPackedInt32();
                    var playSound = reader.ReadBoolean();

                    var key = OptionItem.AllOptions[item];

                    NotificationPopperPatch.AddSettingsChangeMessage(item, key, playSound);
                }
                break;
            case CustomRPC.SyncAbilityUseLimit:
                {
                    var pc = Utils.GetPlayerById(reader.ReadByte());
                    pc.SetAbilityUseLimit(reader.ReadSingle(), rpc: false);
                }
                break;
            case CustomRPC.SetBountyTarget:
                BountyHunter.ReceiveRPC(reader);
                break;
            case CustomRPC.SyncPuppet:
                Puppeteer.ReceiveRPC(reader);
                break;
            case CustomRPC.SetKillOrSpell:
                Witch.ReceiveRPC(reader, false);
                break;
            case CustomRPC.SetKillOrHex:
                HexMaster.ReceiveRPC(reader, false);
                break;
            case CustomRPC.ShowChat:
                var clientId = reader.ReadPackedUInt32();
                var show = reader.ReadBoolean();
                if (AmongUsClient.Instance.ClientId == clientId)
                {
                    HudManager.Instance.Chat.SetVisible(show);
                }
                break;
            case CustomRPC.SetDrawPlayer:
                Revolutionist.ReceiveDrawPlayerRPC(reader);
                break;
            case CustomRPC.SetOverseerRevealedPlayer:
                Overseer.ReceiveSetRevealedPlayerRPC(reader);
                break;
            case CustomRPC.SetOverseerTimer:
                Overseer.ReceiveTimerRPC(reader);
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
            case CustomRPC.SniperSync:
                Sniper.ReceiveRPC(reader);
                break;
            case CustomRPC.UndertakerLocationSync:
                Undertaker.ReceiveRPC(reader);
                break;
            case CustomRPC.SetLoversPlayers:
                Main.LoversPlayers.Clear();
                int count = reader.ReadInt32();
                for (int i = 0; i < count; i++)
                    Main.LoversPlayers.Add(Utils.GetPlayerById(reader.ReadByte()));
                break;
            case CustomRPC.BetterCheck: // Better Among Us RPC
                {
                    var SetBetterUser = reader.ReadBoolean(); // Used to set player as better user, boolean is used for a future for BAU later on.
                    var IsBetterHost = reader.ReadBoolean(); // Used to set the player as better host, this should never be flagged for a TOHE lobby, if it is it's a spoofed RPC
                    var Signature = reader.ReadString(); // Used to verify that the RPC isn't spoofed, only possible in BAU mod due to a special signature that can't really be replicated easily
                    var Version = reader.ReadString(); // Used to read players BAU version

                    if (IsBetterHost)
                    {
                        EAC.Report(__instance, "BetterCheck set as BetterHost");
                        EAC.HandleCheat(__instance, "BetterCheck set as BetterHost");
                        break;
                    }

                    if (string.IsNullOrEmpty(Signature) || string.IsNullOrEmpty(Version))
                    {
                        EAC.Report(__instance, "BetterCheck invalid info");
                        EAC.HandleCheat(__instance, "BetterCheck invalid info");
                        break;
                    }

                    Main.BAUPlayers[__instance.Data] = __instance.Data.Puid;
                }
                break;
            case CustomRPC.SendFireworkerState:
                Fireworker.ReceiveRPC(reader);
                break;
            case CustomRPC.SetCurrentDousingTarget:
                Arsonist.ReceiveCurrentDousingTargetRPC(reader);
                break;
            case CustomRPC.SetDousedPlayer:
                Arsonist.ReceiveSetDousedPlayerRPC(reader);
                break;
            case CustomRPC.SetCurrentDrawTarget:
                Revolutionist.ReceiveSetCurrentDrawTarget(reader);
                break;
            case CustomRPC.SetEvilTrackerTarget:
                EvilTracker.ReceiveRPC(reader);
                break;
            case CustomRPC.SetRealKiller:
                byte tarid = reader.ReadByte();
                byte killerId = reader.ReadByte();
                RPC.SetRealKiller(tarid, killerId);
                break;
            //case CustomRPC.SetTrackerTarget:
            //    Tracker.ReceiveRPC(reader);
            //    break;
            case CustomRPC.SyncJailerData:
                Jailer.ReceiveRPC(reader);
                break;
            case CustomRPC.SetCrewpostorTasksDone:
                Crewpostor.ReceiveRPC(reader);
                break;
            case CustomRPC.SyncAdmiredList:
                Admirer.ReceiveRPC(reader);
                break;
            case CustomRPC.PlayCustomSound:
                CustomSoundsManager.ReceiveRPC(reader);
                break;
            case CustomRPC.LightningSetGhostPlayer:
                Lightning.ReceiveRPC(reader);
                break;
            case CustomRPC.SetGreedy:
                Greedy.ReceiveRPC(reader);
                break;
            case CustomRPC.BenefactorRPC:
                Benefactor.ReceiveRPC(reader);
                break;
            case CustomRPC.GuessKill:
                GuessManager.RpcClientGuess(Utils.GetPlayerById(reader.ReadByte()));
                break;
            case CustomRPC.SetMarkedPlayer:
                Ninja.ReceiveRPC(reader);
                break;
            case CustomRPC.SyncGeneralOptions:
                byte paciefID = reader.ReadByte();
                //playerstate:
                {
                    int rolaId = reader.ReadPackedInt32();
                    bool isdead = reader.ReadBoolean();
                    bool IsDC = reader.ReadBoolean();
                    int deathReasonId = reader.ReadPackedInt32();

                    CustomRoles rola = (CustomRoles)rolaId;
                    PlayerState.DeathReason drip = (PlayerState.DeathReason)deathReasonId;

                    if (Main.PlayerStates.ContainsKey(paciefID))
                    {
                        var state = Main.PlayerStates[paciefID];
                        state.MainRole = rola;
                        state.IsDead = isdead;
                        state.Disconnected = IsDC;
                        state.deathReason = drip;
                    }
                }
                float Killcd = reader.ReadSingle();
                float speed = reader.ReadSingle();

                Main.AllPlayerKillCooldown[paciefID] = Killcd;
                Main.AllPlayerSpeed[paciefID] = speed;
                break;
            case CustomRPC.SyncSpeedPlayer:
                byte readerPlayerId = reader.ReadByte();
                float newSpeed = reader.ReadSingle();
                Main.AllPlayerSpeed[readerPlayerId] = newSpeed;
                break;
            case CustomRPC.SyncPlayerSetting:
                byte playerid = reader.ReadByte();
                int rlId = reader.ReadPackedInt32();
                CustomRoles rl = (CustomRoles)rlId;
                if (Main.PlayerStates.ContainsKey(playerid))
                {
                    Main.PlayerStates[playerid].MainRole = rl;
                }
                break;

            case CustomRPC.SetKillTimer:
                float time = reader.ReadSingle();
                PlayerControl.LocalPlayer.SetKillTimer(time);
                break;
            case CustomRPC.SyncFFAPlayer:
                FFAManager.ReceiveRPCSyncFFAPlayer(reader);
                break;
            case CustomRPC.SyncSpeedRunStates:
                SpeedRun.HandleSyncSpeedRunStates(reader);
                break;
            case CustomRPC.SyncAllPlayerNames:
                Main.AllPlayerNames.Clear();
                Main.AllClientRealNames.Clear();
                int num = reader.ReadPackedInt32();
                for (int i = 0; i < num; i++)
                    Main.AllPlayerNames.TryAdd(reader.ReadByte(), reader.ReadString());
                int num2 = reader.ReadPackedInt32();
                for (int i = 0; i < num2; i++)
                    Main.AllClientRealNames.TryAdd(reader.ReadInt32(), reader.ReadString());
                break;
            case CustomRPC.SyncFFANameNotify:
                FFAManager.ReceiveRPCSyncNameNotify(reader);
                break;
            case CustomRPC.SyncNameNotify:
                NameNotifyManager.ReceiveRPC(reader);
                break;
            case CustomRPC.Judge:
                Judge.ReceiveRPC_Custom(reader, __instance);
                break;
            case CustomRPC.PresidentEnd:
                President.ReceiveRPC(reader, __instance);
                break;
            case CustomRPC.PresidentReveal:
                President.ReceiveRPC(reader, __instance, isEnd: false);
                break;
            case CustomRPC.CouncillorJudge:
                Councillor.ReceiveRPC_Custom(reader, __instance);
                break;
            case CustomRPC.Guess:
                GuessManager.ReceiveRPC(reader, __instance);
                break;
            case CustomRPC.NemesisRevenge:
                Nemesis.ReceiveRPC_Custom(reader, __instance);
                break;
            case CustomRPC.RetributionistRevenge:
                Retributionist.ReceiveRPC_Custom(reader, __instance);
                break;
            case CustomRPC.SetChameleonTimer:
                Chameleon.ReceiveRPC_Custom(reader);
                break;
            case CustomRPC.SetAlchemistTimer:
                Alchemist.ReceiveRPC(reader);
                break;
            case CustomRPC.SetConsigliere:
                Consigliere.ReceiveRPC(reader);
                break;
            case CustomRPC.SetInvestgatorLimit:
                Investigator.ReceiveRPC(reader);
                break;
            case CustomRPC.KillFlash:
                Utils.FlashColor(new(1f, 0f, 0f, 0.3f));
                var playKillSound = reader.ReadBoolean();
                if (Constants.ShouldPlaySfx()) RPC.PlaySound(PlayerControl.LocalPlayer.PlayerId, playKillSound ? Sounds.KillSound : Sounds.SabotageSound);
                break;
            case CustomRPC.DumpLog:
                var target = Utils.GetPlayerById(reader.ReadByte());
                if (target != null && !target.FriendCode.GetDevUser().DeBug)
                {
                    Logger.Info($"Player {target.GetNameWithRole()} used /dump", "RPC_DumpLogger");
                }
                break;
            case CustomRPC.FixModdedClientCNO:
                var CNO = reader.ReadNetObject<PlayerControl>();
                bool active = reader.ReadBoolean();
                if (CNO != null)
                {
                    CNO.transform.FindChild("Names").FindChild("NameText_TMP").gameObject.SetActive(active);
                    CNO.Collider.enabled = false;
                }
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
            case CustomRPC.DictatorRPC:
                Dictator.OnReceiveDictatorRPC(reader, __instance);
                break;
            case CustomRPC.SyncShieldPersonDiedFirst:
                Main.FirstDied = reader.ReadString();
                Main.FirstDiedPrevious = reader.ReadString();
                break;
            case CustomRPC.SyncDeadPassedMeetingList:
                Main.DeadPassedMeetingPlayers.Clear();
                var pnum = reader.ReadPackedInt32();
                for (int i = 0; i < pnum; i++)
                    Main.DeadPassedMeetingPlayers.Add(reader.ReadByte());
                break;
            case CustomRPC.Necronomicon:
                CovenManager.ReceiveNecroRPC(reader);
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

[HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.HandleRpc))]
internal class PlayerPhysicsRPCHandlerPatch
{
    private static bool HasVent(int ventId) => ShipStatus.Instance.AllVents.Any(v => v.Id == ventId);
    private static bool HasLadder(int ladderId) => ShipStatus.Instance.Ladders.Any(l => l.Id == ladderId);

    public static bool Prefix(PlayerPhysics __instance, byte callId, MessageReader reader)
    {
        //var rpcType = (RpcCalls)callId;
        //MessageReader subReader = MessageReader.Get(reader);

        if (EAC.PlayerPhysicsRpcCheck(__instance, callId, reader)) return false;

        var player = __instance.myPlayer;

        if (!player)
        {
            Logger.Warn("Received Physics RPC without a player", "PlayerPhysics_ReceiveRPC");
            return false;
        }

        if (!Main.MeetingIsStarted)
        {
            //__instance.myPlayer.walkingToVent = true;
            VentSystemDeterioratePatch.ForceUpadate = true;
        }

        Logger.Info($"{player.PlayerId}({(__instance.IsHost() ? "Host" : player.Data.PlayerName)}):{callId}({RPC.GetRpcName(callId)})", "PlayerPhysics_ReceiveRPC");
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
        else if (!Main.AllPlayerControls.Any(pc => pc.IsNonHostModdedClient()))
        {
            return;
        }

        if (!AmongUsClient.Instance.AmHost || PlayerControl.AllPlayerControls.Count <= 1 || (AmongUsClient.Instance.AmHost == false && PlayerControl.LocalPlayer == null))
        {
            return;
        }

        var amount = OptionItem.AllOptions.Count;
        int divideBy = amount / 10;

        for (var i = 0; i <= 10; i++)
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
        else if (!Main.AllPlayerControls.Any(pc => pc.IsNonHostModdedClient()))
        {
            return;
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
        //Logger.Msg($"StartAmount/LastAmount: {startAmount}/{lastAmount} :--: ListOptionsCount/AllOptions: {countListOptions}/{amountAllOptions}", "SyncOptionsBetween");

        // Sync Settings
        foreach (var option in listOptions.ToArray())
        {
            writer.WritePacked(option.GetValue());
        }

        AmongUsClient.Instance.FinishRpcImmediately(writer);
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
        writer.WritePacked(Main.AllClientRealNames.Count);
        foreach (var name in Main.AllClientRealNames)
        {
            writer.Write(name.Key);
            writer.Write(name.Value);
        }
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ShowPopUp(this PlayerControl pc, string message, string title = "")
    {
        if (!AmongUsClient.Instance.AmHost) return;
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.ShowPopUp, SendOption.Reliable, pc.GetClientId());
        writer.Write(message);
        writer.Write(title);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void RpcSetFriendCode(string fc)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpc(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetFriendCode, SendOption.None);
        writer.Write(fc);
        writer.EndMessage();
        SetFriendCode(PlayerControl.LocalPlayer, fc);
    }
    public static void SetFriendCode(PlayerControl target, string fc)
    {
        if (GameStates.IsVanillaServer) return;
        if (target.GetClient() != null && target.GetClient().FriendCode != string.Empty) return;
        // On Niko233's region this is not needed lol
        target.FriendCode = fc;
        target.Data.FriendCode = fc;
        target.GetClient().FriendCode = fc;
        target.Data.MarkDirty();
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
        writer.Write((int)deathReason);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void GetDeathReason(MessageReader reader)
    {
        var playerId = reader.ReadByte();
        var deathReason = reader.ReadInt32();
        Main.PlayerStates[playerId].deathReason = (PlayerState.DeathReason)deathReason;
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
            Utils.NotifyGameEnding();

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
                case Sounds.SabotageSound:
                    SoundManager.Instance.PlaySound(ShipStatus.Instance.SabotageSound, false, 0.8f);
                    break;
            }
        }
    }
    public static void SetCustomRole(byte targetId, CustomRoles role)
    {
        if (role < CustomRoles.NotAssigned)
        {
            Main.PlayerStates[targetId].SetMainRole(role);
            targetId.GetRoleClassById()?.OnAdd(targetId);
        }
        else if (role >= CustomRoles.NotAssigned)   //500:NoSubRole 501~:SubRole 
        {
            Main.PlayerStates[targetId].SetSubRole(role);

            switch (role)
            {
                case CustomRoles.LastImpostor:
                    LastImpostor.AddMidGame(targetId);
                    break;
            }
        }

        if (!AmongUsClient.Instance.IsGameOver)
            DestroyableSingleton<HudManager>.Instance.SetHudActive(true);
        //    HudManager.Instance.Chat.SetVisible(true);

        if (PlayerControl.LocalPlayer.PlayerId == targetId) RemoveDisableDevicesPatch.UpdateDisableDevices();
    }
    public static void SyncRoleSkillReader(MessageReader reader)
    {
        try
        {
            var pc = reader.ReadNetObject<PlayerControl>();

            switch (pc.GetRoleClass())
            {
                default:
                    pc.GetRoleClass().ReceiveRPC(reader, pc);
                    break;
            }
        }
        catch (Exception error)
        {
            Logger.Error($" Error RPC:{error}", "SyncRoleSkillReader");
        }
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
    public static void SyncDeadPassedMeetingList()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        var writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncDeadPassedMeetingList, SendOption.Reliable, -1);
        writer.WritePacked(Main.DeadPassedMeetingPlayers.Count);
        foreach (var dead in Main.DeadPassedMeetingPlayers)
        {
            writer.Write(dead);
        }
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void SendRpcLogger(uint targetNetId, byte callId, SendOption sendOption, int targetClientId = -1)
    {
        if (!DebugModeManager.AmDebugger) return;
        string rpcName = GetRpcName(callId);
        string from = targetNetId.ToString();
        string target = targetClientId.ToString();
        try
        {
            target = targetClientId < 0 ? "All" : AmongUsClient.Instance.GetClient(targetClientId).PlayerName;
            from = Main.AllPlayerControls.FirstOrDefault(c => c.NetId == targetNetId)?.GetRealName(clientData: true);
        }
        catch { }

        Logger.Info($"FromNetID:{targetNetId}({from}) TargetClientID:{targetClientId}({target}) CallID:{callId}({rpcName}) SendOption:{sendOption}", "SendRPC");
    }
    public static string GetRpcName(byte callId)
    {
        string rpcName;
        if ((rpcName = Enum.GetName(typeof(RpcCalls), callId)) != null) { }
        else if ((rpcName = Enum.GetName(typeof(CustomRPC), callId)) != null) { }
        else rpcName = callId.ToString();
        return rpcName;
    }
    public static void SetRealKiller(byte targetId, byte killerId)
    {
        var state = Main.PlayerStates[targetId];
        state.RealKiller.Item1 = DateTime.Now;
        state.RealKiller.Item2 = killerId;
        state.RoleofKiller = Main.PlayerStates.TryGetValue(killerId, out var kState) ? kState.MainRole : CustomRoles.NotAssigned;

        if (!AmongUsClient.Instance.AmHost) return;
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetRealKiller, SendOption.Reliable, -1);
        writer.Write(targetId);
        writer.Write(killerId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
}
[HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.StartRpc))]
internal class StartRpcPatch
{
    public static void Prefix(/*InnerNet.InnerNetClient __instance,*/ [HarmonyArgument(0)] uint targetNetId, [HarmonyArgument(1)] byte callId, [HarmonyArgument(2)] SendOption option = SendOption.Reliable)
    {
        RPC.SendRpcLogger(targetNetId, callId, option);
    }
}
[HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.StartRpcImmediately))]
public class StartRpcImmediatelyPatch
{
    public static void Prefix(InnerNetClient __instance, [HarmonyArgument(0)] uint targetNetId, [HarmonyArgument(1)] byte callId, [HarmonyArgument(2)] SendOption option, [HarmonyArgument(3)] int targetClientId)
    {
        RPC.SendRpcLogger(targetNetId, callId, option, targetClientId);
    }
}
