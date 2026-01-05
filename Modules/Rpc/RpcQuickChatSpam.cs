using AmongUs.InnerNet.GameDataMessages;
using AmongUs.QuickChat;
using Hazel;
using Il2CppSystem.Linq;
using System;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Modules.Rpc
{
    class RpcQuickChatSpam : CustomModdedData
    {
        public override GameDataTypes FirstDataType => GameDataTypes.RpcFlag;
        public static QuickChatSpamMode quickChatSpamMode => (QuickChatSpamMode)UseQuickChatSpamCheat.GetInt();

        public static HashSet<StringNames> badSpam = [
            StringNames.MushroomMixupSabotage,
            StringNames.ExileTextSN,
            StringNames.GameStarting,
            StringNames.QCSelfVoted,
            StringNames.More,
            StringNames.RolesHelp_CrewmateRole,
            StringNames.QCAccVoteA,
            StringNames.QCStaBodyWasInA,
            StringNames.QCSelfSawCrewDoVisualTask,
            StringNames.ErrorSanction,
            StringNames.QCResProtected_ANY_QCCrewMe,
            StringNames.MeetingProceeds,
            StringNames.ErrorKicked,
            StringNames.QCStaAWasWithB,
            StringNames.ExileTextSP,
            StringNames.DownloadTestEstTimeHMS,
            StringNames.ExileTextPP,
            StringNames.QCResCloseTo,
            StringNames.PlayerWasKickedBy,
            StringNames.QCStaShapeshifterSkin,
            StringNames.ImpostorsRemainS,
            StringNames.QCResAWasAtBNeg_QCCrewMe_ANY,
            StringNames.QCSelfDoingTask,
            StringNames.AstDestroyed,
            StringNames.QCAccAIsSuspiciousNeg,
            StringNames.QCSawDeadCrewOnVitals,
            StringNames.QCResAWasNeg,
            StringNames.LevelShorthand,
            StringNames.QCQstWhoDidTask,
            StringNames.AddFriendConfirm,
            StringNames.AccountIDDisplay,
            StringNames.CosmicubeProgression,
            StringNames.QCStaAWasWithB_QCCrewMe_ANY,
            StringNames.ImpostorsRemainP,
            StringNames.QCSelfSawCrewVentAtLocation,
            StringNames.QCCrewWasUsingZipline,
            StringNames.FriendCodeSuccess,
            StringNames.QCAccAWasChasingB_ANY_QCCrewMe,
            StringNames.AvailableFor,
            StringNames.SupportIDLabel,
            StringNames.WifiPleaseReturnIn,
            StringNames.MedETA,
            StringNames.PlayerWasBannedBy,
            StringNames.LobbyChangeSettingNotification,
            StringNames.QCResYeetCrew,
            StringNames.QCSelfWasTracking,
            StringNames.EmergencyCount,
            StringNames.QCStaASawB,
            StringNames.QCQstWhatWasADoing,
            StringNames.OtherDownloadError,
            StringNames.QCSelfAmVotingCrew,
            StringNames.QCResIsBeingFramed,
            StringNames.SecondsAbbv,
            StringNames.QCResADid,
            StringNames.QCAccAKilledB,
            StringNames.QCSelfFixedSystem,
            StringNames.DivertPowerTo,
            StringNames.QCResIsRoleMaybe,
            StringNames.ErrorQuickChatMode,
            StringNames.QCAccVoteANeg,
            StringNames.QCAccAKilledB,
            StringNames.QCAccAWasChasingBNeg_QCCrewMe_ANY,
            StringNames.LinkExistingAccountExplain,
            StringNames.DivertPowerTo,
            StringNames.UnlinkAccountExplainConfirm,
            StringNames.NumImpostorsP,
            StringNames.StatsRoleWins,
            StringNames.TagsFilteredSingular,
            StringNames.MedscanWaitingFor,
            StringNames.QCAccAIsLying,
            StringNames.SanctionDuration,
            StringNames.QCResIsBeingFramed,
            StringNames.QCAccAWasChasingBNeg_ANY_QCCrewMe,
            StringNames.OtherDownloadError,
            StringNames.QCAccShapeshited,
            StringNames.QCAccVoteANeg,
            StringNames.QCCrewFixedSystem,
            StringNames.LeftGameError,
            StringNames.RoleChanceAndQuantity,
            StringNames.QCAccIsRoleNeg,
            StringNames.LobbyTimerExpiringHud,
            StringNames.QCSelfWasRole,
            StringNames.QCSelfSawTwoCrew,
            StringNames.QCResAWasAtBNeg,
            StringNames.QCCrewFixedSystem,
            StringNames.CQCrewKilledDeadInSporeCloud,
            StringNames.LobbyTimerExpiringMsg2,
            StringNames.UnblockConfirm,
            StringNames.ExileTextNonConfirm,
            StringNames.QCCrewDidTaskAtLocation,
            StringNames.RoleChanceAndQuantity,
            StringNames.RoleChanceAndQuantity,
            StringNames.QCResADid,
            StringNames.QCAccASawBVentNeg_ANY_QCCrewMe,
            StringNames.QCAccASawBVentNeg,
            StringNames.QCAccASawBVentNeg_ANY_QCCrewMe,
            StringNames.QCAccAWasChasingBNeg_ANY_QCCrewMe,
            StringNames.QCAccAKilledB,
            StringNames.SecLogEntry,
            StringNames.DivertPowerTo,
            StringNames.LobbyTimerExpiringMsg,
            StringNames.OtherDownloadError,
            StringNames.QCCrewShapeshifted,
            StringNames.ErrorInternalServer,
            StringNames.QCResAWasAtBNeg,
            StringNames.QCWhoIsRole,
            StringNames.QCAccIsRole,
            StringNames.RedeemPurcahsedItemsExplain,
            StringNames.RoleChanceAndQuantity,
            StringNames.AbbreviatedDay,
            StringNames.QCResIsBeingFramed,
            StringNames.ExileTextPN,
            StringNames.QCCrewWasPretendingTasks,
            StringNames.LinkExistingAccountExplainConfirm,
            StringNames.QCWhoWasRole,
            StringNames.QCAccASawBVentNeg,
            StringNames.DownloadingLabel,
            StringNames.UserVersionError,
            StringNames.FailPurchaseUnknown,
            StringNames.DownloadingLabel,
            StringNames.XpGainedValue,
            StringNames.QCWhoIsRole,
            StringNames.QCResIsRoleMaybe,
            StringNames.TagsAppliedPlural,
            StringNames.QCAccASawBVent_QCCrewMe_ANY,
            StringNames.ErrorBanned,
            StringNames.DivertPowerTo,
            StringNames.ExileTextPN,
            StringNames.QCQstWhoWasWith,
            StringNames.ErrorQuickChatMode,
            StringNames.UnlinkSuccess,
            StringNames.ErrorBanned,
            StringNames.QCSelfDidTaskAtLocation,
            StringNames.QCStaASelfReported,
            StringNames.QCAccAIsSuspicious,
            StringNames.QCQstWhereWasA,
            StringNames.QCWhoIsRole,
            StringNames.LinkAccountCode,
            StringNames.CQCrewKilledDeadInSporeCloud,
            StringNames.QCStaAWasWithB_ANY_QCCrewMe,
            StringNames.XpGainedValue,
            StringNames.QCSelfAccAtLocation,
            StringNames.QCResAWasAtBNeg,
            StringNames.MedscanCompleteIn,
            StringNames.QCCrewWasPretendingSabotage,
            StringNames.LeftGameError,
            StringNames.ChatRateLimit,
            StringNames.RoleChanceAndQuantity,
            StringNames.ConfirmDeleteAccounts,
            StringNames.QCCrewWasPretendingSabotage,
            StringNames.QCAccAWasChasingBNeg_ANY_QCCrewMe,
            StringNames.ConfirmPurchaseText,
            StringNames.SignInIssueTitle,
            StringNames.QCResAWasAtBNeg,
            StringNames.QCWhoWasRole,
            StringNames.DownloadTestEstTimeMS,
            StringNames.ErrorInvalidName,
            StringNames.SignInIssueTitle,
            StringNames.QCAccAWasChasingBNeg,
            StringNames.QCWhoWasRole,
            StringNames.QCCrewWasPretendingSabotage,
            StringNames.AbbreviatedHour,
            StringNames.QCCrewShapeshifted,
            StringNames.UserVersionError,
            StringNames.QCSelfFixedSystem,
            StringNames.QCAccASawBVentNeg,
            StringNames.ErrorQuickChatMode,
            StringNames.MedscanCompleteIn,
            StringNames.UserVersionError,
            StringNames.GameSecondsAbbrev,
            StringNames.QCResADid,
            StringNames.QCCrewDidTaskAtLocation,
            StringNames.LobbyTimerExpiringMsg,
            StringNames.QCResAWasAtB,
            StringNames.DownloadingLabel,
            StringNames.ExileTextNonConfirm,
            StringNames.QCAccAKilledBNeg,
            StringNames.QCResADid,
            StringNames.QCStaASawB_QCCrewMe_ANY,
            StringNames.QCStaASelfReported,
            StringNames.LobbyTimerExpiringMsg2,
            StringNames.SignInIssueTitle,
            StringNames.DownloadTestEstTimeDHMS,
            StringNames.SignInIssueTitle,
            StringNames.QCWhoIsRole,
            StringNames.ErrorQuickChatMode,
            StringNames.QCWhoIsRole,
            StringNames.QCResAWasAtBNeg,
            StringNames.LeftGameError,
            StringNames.TagsFilteredSingular,
            StringNames.QCAccAWasChasingB,
            StringNames.QCCrewReportedBody,
            StringNames.QCCrewWasPretendingSabotage,
            StringNames.SanctionDuration,
            StringNames.UserLeftGame,
            StringNames.QCResCloseTo_QCCrewMe_ANY,
            StringNames.QCStaADidB,
            StringNames.QCAccAWasChasingBNeg_ANY_QCCrewMe,
            StringNames.WeatherEta,
            StringNames.MeetingVotingBegins,
            StringNames.MeetingVotingEnds,
            StringNames.MeetingHasVoted,
            StringNames.DownloadTestEstTimeS,
            StringNames.ErrorIntentionalLeaving,
            StringNames.RolesHelp_ImpostorRole,
            StringNames.QCCrewCams,
            StringNames.QCCrewWasProtected,
            StringNames.QCWhoWasAt,
            StringNames.QCCoolOutfitCrew,
            StringNames.QCAccASawBVent,
            StringNames.QCAccAIsLyingNeg,
            StringNames.QCAccADidntReport,
            StringNames.QCResAWas,
            StringNames.QCResADidNeg,
            StringNames.QCStaACalledMeeting,
            StringNames.TimeRemaining,
            StringNames.DownloadSizeLabel,
            StringNames.UnlinkAccountExplain,
            StringNames.AbbreviatedMinute,
            StringNames.AbbreviatedSecond,
            StringNames.NewRequests,
            StringNames.UnfriendConfirm,
            StringNames.SignInIssueText,
            StringNames.QCResProtected,
            StringNames.RedeemPopup,
            StringNames.MergeGuestAccountText,
            StringNames.QCAccAKilledBNeg_QCCrewMe_ANY,
            StringNames.QCAccAWasChasingB_QCCrewMe_ANY,
            StringNames.QCAccASawBVentNeg_QCCrewMe_ANY,
            StringNames.QCResAWasAtB_QCCrewMe_ANY,
            StringNames.QCStaADidB_QCCrewMe_ANY,
            StringNames.QCAccIsRole_QCCrewMe_ANY,
            StringNames.QCAccIsRoleNeg_QCCrewMe_ANY,
            StringNames.QCResRipCrew,
            StringNames.SecLogEntryColorblind,
            StringNames.LobbyChangeSettingNotificationRole,
            StringNames.TagsFilteredPlural,
            StringNames.TagsAppliedSingular,
            StringNames.QCSelfSawRoleInLocation,
        ];

        public override void SerializeCustomValues(MessageWriter writer)
        {
            var firstAlivePlayer = Main.AllAlivePlayerControls.OrderBy(x => x.PlayerId).FirstOrDefault() ?? PlayerControl.LocalPlayer;
            var title = "<color=#aaaaff>" + GetString("DefaultSystemMessageTitle") + "</color>";
            var name = firstAlivePlayer?.Data?.PlayerName ?? "Error";

            firstAlivePlayer.Data.PlayerName = title;

            // writer.StartMessage((byte)GameDataTypes.RpcFlag);
            writer.WritePacked(firstAlivePlayer.NetId);
            writer.Write((byte)RpcCalls.SetName);
            writer.Write(firstAlivePlayer.Data.NetId);
            writer.Write(title);
            writer.EndMessage();

            switch (quickChatSpamMode)
            {
                case QuickChatSpamMode.QuickChatSpam_Disabled:
                    Logger.Info("QuickChatSpam disabled but trying to spam?", "SendQuickChatSpam");
                    goto case QuickChatSpamMode.QuickChatSpam_Random20;
                // Send as random 20 here
                case QuickChatSpamMode.QuickChatSpam_Random20:
                    var random = IRandom.Instance;
                    var stringNamesValues = Enum.GetValues(typeof(StringNames)).Cast<StringNames>().Except(badSpam).ToArray();
                    // Logger.Info($"Found {stringNamesValues.Length} spam keys.", "SendQuickChatSpam");

                    // foreach (StringNames value in stringNamesValues)
                    // {
                    //     string translation = DestroyableSingleton<TranslationController>.Instance.GetString(value);
                    //     if (translation.Contains("{0}")) Logger.Info($"StringNames.{value},", "SendQuickChatSpam");
                    // }

                    for (int i = 0; i < 21; i++)
                        {
                            var randomString = stringNamesValues[random.Next(stringNamesValues.Length)];
                            var message = new RpcSendQuickChatMessage(firstAlivePlayer.NetId, new(QuickChatPhraseType.ComplexPhrase, randomString, 0, null));
                            message.Serialize(writer);
                            DestroyableSingleton<HudManager>.Instance.Chat.AddChat(firstAlivePlayer, GetString(randomString), false);
                        }
                    break;
                case QuickChatSpamMode.QuickChatSpam_How2PlayNormal:
                    foreach (var names in Main.how2playN)
                    {
                        var message = new RpcSendQuickChatMessage(firstAlivePlayer.NetId, new(QuickChatPhraseType.ComplexPhrase, names, 0, null));
                        message.Serialize(writer);
                        message.Serialize(writer);
                        DestroyableSingleton<HudManager>.Instance.Chat.AddChat(firstAlivePlayer, GetString(names), false);
                        DestroyableSingleton<HudManager>.Instance.Chat.AddChat(firstAlivePlayer, GetString(names), false);
                    }
                    break;
                case QuickChatSpamMode.QuickChatSpam_How2PlayHidenSeek:
                    foreach (var names in Main.how2playHnS)
                    {
                        var message = new RpcSendQuickChatMessage(firstAlivePlayer.NetId, new(QuickChatPhraseType.ComplexPhrase, names, 0, null));
                        message.Serialize(writer);
                        message.Serialize(writer);
                        DestroyableSingleton<HudManager>.Instance.Chat.AddChat(firstAlivePlayer, GetString(names), false);
                        DestroyableSingleton<HudManager>.Instance.Chat.AddChat(firstAlivePlayer, GetString(names), false);
                    }
                    break;
                case QuickChatSpamMode.QuickChatSpam_EzHacked:
                    foreach (var names in Main.how2playEzHacked)
                    {
                        var message = new RpcSendQuickChatMessage(firstAlivePlayer.NetId, new(QuickChatPhraseType.ComplexPhrase, names, 0, null));
                        message.Serialize(writer);
                        message.Serialize(writer);
                        DestroyableSingleton<HudManager>.Instance.Chat.AddChat(firstAlivePlayer, GetString(names), false);
                        DestroyableSingleton<HudManager>.Instance.Chat.AddChat(firstAlivePlayer, GetString(names), false);
                    }
                    break;
                case QuickChatSpamMode.QuickChatSpam_Empty:
                    {
                        var message = new RpcSendQuickChatMessage(firstAlivePlayer.NetId, new(QuickChatPhraseType.SimplePhrase, StringNames.None, 0, null));
                        for (var i = 0; i < 21; i++)
                        {
                            message.Serialize(writer);
                            DestroyableSingleton<HudManager>.Instance.Chat.AddChat(firstAlivePlayer, GetString(StringNames.None), false);
                        }
                    }
                    break;
            }

            firstAlivePlayer.Data.PlayerName = name;
            writer.StartMessage((byte)GameDataTypes.RpcFlag);
            writer.WritePacked(firstAlivePlayer.NetId);
            writer.Write((byte)RpcCalls.SetName);
            writer.Write(firstAlivePlayer.Data.NetId);
            writer.Write(name);
            // writer.EndMessage();
        }
    }
}
