using AmongUs.GameOptions;
using HarmonyLib;
using Hazel;
using System;
using System.Collections.Generic;
using System.Linq;
using TOHE.Modules;
using TOHE.Modules.ChatManager;
using TOHE.Roles.AddOns.Common;
using TOHE.Roles.AddOns.Crewmate;
using TOHE.Roles.AddOns.Impostor;
using TOHE.Roles.Crewmate;
using TOHE.Roles.Double;
using TOHE.Roles.Impostor;
using TOHE.Roles.Neutral;
using static TOHE.Modules.CustomRoleSelector;
using static TOHE.Translator;

namespace TOHE;

[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.CoStartGame))]
internal class ChangeRoleSettings
{
    public static void Postfix(AmongUsClient __instance)
    {
        Main.OverrideWelcomeMsg = "";
        Main.AssignRolesIsStarted = true;

        Logger.Msg("Is Started", "AssignRoles");

        try
        {
            // Note: No positions are set at this time.
            if (GameStates.IsNormalGame)
            {
                Main.NormalOptions.roleOptions.SetRoleRate(RoleTypes.GuardianAngel, 0, 0);
                if (Options.DisableVanillaRoles.GetBool())
                {
                    Main.NormalOptions.roleOptions.SetRoleRate(RoleTypes.Scientist, 0, 0);
                    Main.NormalOptions.roleOptions.SetRoleRate(RoleTypes.Engineer, 0, 0);
                    Main.NormalOptions.roleOptions.SetRoleRate(RoleTypes.Shapeshifter, 0, 0);
                }
            }
            else if (GameStates.IsHideNSeek)
            {
                Main.HideNSeekOptions.NumImpostors = Options.NumImpostorsHnS.GetInt();
                Main.AliveImpostorCount = Main.HideNSeekOptions.NumImpostors;
            }

            Main.PlayerStates = [];

            Main.AllPlayerKillCooldown = [];
            Main.AllPlayerSpeed = [];
            Main.AllPlayerCustomRoles = [];
            Main.WarlockTimer = [];
            Main.AssassinTimer = [];
            Main.isDoused = [];
            Main.isDraw = [];
            Main.isRevealed = [];
            Main.ArsonistTimer = [];
            Main.RevolutionistTimer = [];
            Main.RevolutionistStart = [];
            Main.RevolutionistLastTime = [];
            Main.RevolutionistCountdown = [];
            Main.TimeMasterBackTrack = [];
            Main.TimeMasterNum = [];
            Main.CursedPlayers = [];
            Main.MafiaRevenged = [];
            Main.RetributionistRevenged = [];
            Main.isCurseAndKill = [];
            Main.isCursed = false;
            Main.DetectiveNotify = [];
            Main.SleuthNotify = [];
            Main.ForCrusade = [];
            Main.KillGhoul = [];
            Main.CyberStarDead = [];
            Main.CyberDead = [];
            Main.KilledDiseased = [];
            Main.KilledAntidote = [];
            Main.WorkaholicAlive = [];
            Main.BaitAlive = [];
            Main.BoobyTrapBody = [];
            Main.TasklessCrewmate = [];
            Main.KillerOfBoobyTrapBody = [];
            Main.CleanerBodies = [];
            Main.BurstBodies = [];
            Main.MedusaBodies = [];
            Main.InfectedBodies = [];
            Main.VirusNotify = [];
            Main.ErasedRoleStorage = [];

            Main.LastEnteredVent = [];
            Main.LastEnteredVentLocation = [];
            Main.EscapistLocation = [];

            Main.AfterMeetingDeathPlayers = [];
            Main.ResetCamPlayerList = [];
            Main.clientIdList = [];

            Main.CapitalismAddTask = [];
            Main.CapitalismAssignTask = [];
            Main.CheckShapeshift = [];
            Main.ShapeshiftTarget = [];
            Main.SpeedBoostTarget = [];
            Main.MayorUsedButtonCount = [];
            Main.ParaUsedButtonCount = [];
            Main.MarioVentCount = [];
            Main.VeteranInProtect = [];
            Main.VeteranNumOfUsed = [];
            Main.GrenadierNumOfUsed = [];
            Main.TimeMasterNumOfUsed = [];
            Main.GrenadierBlinding = [];
            Main.LighterNumOfUsed = [];
            Main.Lighter = [];
            Main.AllKillers = [];
            Main.MadGrenadierBlinding = [];
            Main.CursedWolfSpellCount = [];
            Main.BombedVents = [];
            Main.JinxSpellCount = [];
            Main.OverDeadPlayerList = [];
            Main.Provoked = [];
            Main.ShieldPlayer = Options.ShieldPersonDiedFirst.GetBool() ? Main.FirstDied : "";
            Main.FirstDied = "";
            Main.MadmateNum = 0;
            Main.BardCreations = 0;
            Main.MeetingsPassed = 0;
            Main.DovesOfNeaceNumOfUsed = [];
            Main.GodfatherTarget = [];
            Main.BerserkerKillMax = [];
            Main.AwareInteracted = [];
            Main.CrewpostorTasksDone = [];
            Main.ShamanTarget = byte.MaxValue;
            Main.ShamanTargetChoosen = false;
            Main.MeetingIsStarted = false;
            ChatManager.ResetHistory();

            ReportDeadBodyPatch.CanReport = [];

            Options.UsedButtonCount = 0;

            Main.RealOptionsData = new OptionBackupData(GameOptionsManager.Instance.CurrentGameOptions);

            if (GameStates.IsNormalGame)
            {
                GameOptionsManager.Instance.currentNormalGameOptions.ConfirmImpostor = false;
                GameOptionsManager.Instance.currentNormalGameOptions.SetBool(BoolOptionNames.ConfirmImpostor, false);

                MeetingTimeManager.Init();

                Main.DefaultCrewmateVision = Main.RealOptionsData.GetFloat(FloatOptionNames.CrewLightMod);
                Main.DefaultImpostorVision = Main.RealOptionsData.GetFloat(FloatOptionNames.ImpostorLightMod);
            }

            Main.introDestroyed = false;

            RandomSpawn.CustomNetworkTransformPatch.NumOfTP = [];

            Main.LastNotifyNames = [];

            Main.currentDousingTarget = byte.MaxValue;
            Main.currentDrawTarget = byte.MaxValue;
            Main.PlayerColors = [];

            // Clear last exiled
            ExileControllerWrapUpPatch.AntiBlackout_LastExiled = null;

            //名前の記録
            //Main.AllPlayerNames = [];
            RPC.SyncAllPlayerNames();

            Camouflage.Init();

            var invalidColor = Main.AllPlayerControls.Where(p => p.Data.DefaultOutfit.ColorId < 0 || Palette.PlayerColors.Length <= p.Data.DefaultOutfit.ColorId).ToArray();
            if (invalidColor.Length > 0)
            {
                var msg = GetString("Error.InvalidColor");
                Logger.SendInGame(msg);
                msg += "\n" + string.Join(",", invalidColor.Select(p => $"{p.name}"));
                Utils.SendMessage(msg);
                Logger.Error(msg, "CoStartGame");
            }

            foreach (var target in Main.AllPlayerControls)
            {
                foreach (var seer in Main.AllPlayerControls)
                {
                    var pair = (target.PlayerId, seer.PlayerId);
                    Main.LastNotifyNames[pair] = target.name;
                }
            }

            foreach (var pc in Main.AllPlayerControls)
            {
                var colorId = pc.Data.DefaultOutfit.ColorId;
                if (AmongUsClient.Instance.AmHost && Options.FormatNameMode.GetInt() == 1) pc.RpcSetName(Palette.GetColorName(colorId));
                Main.PlayerStates[pc.PlayerId] = new(pc.PlayerId);
                //Main.AllPlayerNames[pc.PlayerId] = pc?.Data?.PlayerName;

                Main.PlayerColors[pc.PlayerId] = Palette.PlayerColors[colorId];
                
                if (GameStates.IsNormalGame)
                    Main.AllPlayerSpeed[pc.PlayerId] = Main.RealOptionsData.GetFloat(FloatOptionNames.PlayerSpeedMod); //移動速度をデフォルトの移動速度に変更
                
                ReportDeadBodyPatch.CanReport[pc.PlayerId] = true;
                ReportDeadBodyPatch.WaitReport[pc.PlayerId] = [];
                pc.cosmetics.nameText.text = pc.name;

                RandomSpawn.CustomNetworkTransformPatch.NumOfTP.Add(pc.PlayerId, 0);
                var outfit = pc.Data.DefaultOutfit;
                Camouflage.PlayerSkins[pc.PlayerId] = new GameData.PlayerOutfit().Set(outfit.PlayerName, outfit.ColorId, outfit.HatId, outfit.SkinId, outfit.VisorId, outfit.PetId, outfit.NamePlateId);
                Main.clientIdList.Add(pc.GetClientId());
            }

            Main.VisibleTasksCount = true;
            if (__instance.AmHost)
            {
                RPC.SyncCustomSettingsRPC();
                Main.RefixCooldownDelay = 0;
            }

            FallFromLadder.Reset();
            BountyHunter.Init();
            Mercenary.Init();
            EvilDiviner.Init();
            Fireworker.Init();
            Sniper.Init();
            Undertaker.Init();
            TimeThief.Init();
            Puppeteer.Init();
            Mastermind.Init();
            Witch.Init();
            HexMaster.Init();
            //Occultist.Init();
            SabotageMaster.Init();
            Executioner.Init();
            Lawyer.Init();
            Jackal.Init();
            Sidekick.Init();
            Bandit.Init();
            Doppelganger.Init();
            RiftMaker.Init();
            Sheriff.Init();
            CopyCat.Init();
            Captain.Init();
            GuessMaster.Init();
            Cleanser.Init();
            SwordsMan.Init();
            EvilTracker.Init();
            Snitch.Init();
            Solsticer.Init();
            Vampire.Init();
            Vampiress.Init();
            Poisoner.Init();
            AntiAdminer.Init();
            Monitor.Init();
            TimeManager.Init();
            LastImpostor.Init();
            TargetArrow.Init();
            LocateArrow.Init();
            DoubleTrigger.Init();
            Workhorse.Init();
            Pelican.Init();
            Counterfeiter.Init();
            Pursuer.Init();
            Gangster.Init();
            Medic.Init();
            Gamer.Init();
            BallLightning.Init();
            DarkHide.Init();
            Greedier.Init();
            Observer.Init();
            Collector.Init();
            Benefactor.Init();
            Taskinator.Init();
            QuickShooter.Init();
            Kamikaze.Init();
            Camouflager.Init();
            Divinator.Init();
            Jailer.Init();
            Oracle.Init();
            Eraser.Init();
            Assassin.Init();
            Arrogance.Init();
            Juggernaut.Init();
            Anonymous.Init();
            Psychic.Init();
            Glitch.Init();
            Huntsman.Init();
            Deputy.Init();
            Investigator.Init();
            Pickpocket.Init();
            Hangman.Init();
            Judge.Init();
            President.Init();
            Councillor.Init();
            Mortician.Init();
            Mediumshiper.Init();
            Swooper.Init();
            Wraith.Init();
            SoulCollector.Init();
            SchrodingersCat.Init();
            BloodKnight.Init();
            Totocalcio.Init();
            Romantic.Init();
            VengefulRomantic.Init();
            RuthlessRomantic.Init();
            Succubus.Init();
            Crusader.Init();
            CursedSoul.Init();
            Admirer.Init();
            Imitator.Init();
            Medusa.Init();
            Marshall.Init();
            Amnesiac.Init();
            Farseer.Init();
            Infectious.Init();
            Monarch.Init();
            Virus.Init();
            Bloodhound.Init();
            Tracker.Init();
            Merchant.Init();
            Pyromaniac.Init();
            SerialKiller.Init();
            Maverick.Init();
            Jinx.Init();
            DoubleShot.Init();
            Dazzler.Init();
            Addict.Init();
            Mole.Init();
            Deathpact.Init();
            Tracefinder.Init();
            Devourer.Init();
            PotionMaster.Init();
            Traitor.Init();
            Spiritualist.Init();
            Vulture.Init();
            Alchemist.Init();
            Stealth.Init();
            PlagueDoctor.Init();
            Penguin.Init();
            Chameleon.Init();
            Wildling.Init();
            Morphling.Init();
            Inspector.Init(); // *giggle* party cop
            Keeper.Init(); // *giggle* party cop
            Spiritcaller.Init();
            Lurker.Init();
            PlagueBearer.Init();
            Reverie.Init();
            Doomsayer.Init();
            Pirate.Init();
            Pixie.Init();
            Shroud.Init();
            Werewolf.Init();
            Necromancer.Init();
            Chronomancer.Init();
            Seeker.Init();
            Pitfall.Init();
            Agitater.Init();
            Swapper.Init();
            Enigma.Init();
            ChiefOfPolice.Init();
            Mini.Init();
            Blackmailer.Init();
            Spy.Init();
            Oiiai.Init();
            Hater.Init();
            Instigator.Init();
            OverKiller.Init();
            Quizmaster.Init();
            Tired.Init();
            Statue.Init();
            Rainbow.Init();
            //Tricky.Init();

            SabotageSystemPatch.SabotageSystemTypeRepairDamagePatch.Initialize();
            DoorsReset.Initialize();

            //FFA
            FFAManager.Init();

            CustomWinnerHolder.Reset();
            AntiBlackout.Reset();
            NameNotifyManager.Reset();

            IRandom.SetInstanceById(Options.RoleAssigningAlgorithm.GetValue());

            MeetingStates.MeetingCalled = false;
            MeetingStates.FirstMeeting = true;
            GameStates.AlreadyDied = false;
            EAC.ReportTimes = [];

            SetEverythingUpPatch.LastWinsText = "";
            SetEverythingUpPatch.LastWinsReason = "";
        }
        catch (Exception ex)
        {
            Utils.ErrorEnd("Change Role Setting Postfix");
            Logger.Fatal(ex.ToString(), "Change Role Setting Postfix");
        }
    }
}
[HarmonyPatch(typeof(RoleManager), nameof(RoleManager.SelectRoles))]
internal class SelectRolesPatch
{
    public static void Prefix()
    {
        if (!AmongUsClient.Instance.AmHost) return;

        if (GameStates.IsHideNSeek)
        {
            if (Main.EnableGM.Value)
            {
                PlayerControl.LocalPlayer.RpcSetCustomRole(CustomRoles.GM);
                PlayerControl.LocalPlayer.RpcSetRole(RoleTypes.Crewmate);
                PlayerControl.LocalPlayer.Data.IsDead = true;
                Main.PlayerStates[PlayerControl.LocalPlayer.PlayerId].SetDead();
            }

            EAC.OriginalRoles = [];
            return;
        }

        try
        {
            //CustomRpcSenderとRpcSetRoleReplacerの初期化
            Dictionary<byte, CustomRpcSender> senders = [];
            foreach (var pc in Main.AllPlayerControls)
            {
                senders[pc.PlayerId] = new CustomRpcSender($"{pc.name}'s SetRole Sender", SendOption.Reliable, false)
                        .StartMessage(pc.GetClientId());
            }
            RpcSetRoleReplacer.StartReplace(senders);

            if (Main.EnableGM.Value)
            {
                PlayerControl.LocalPlayer.RpcSetCustomRole(CustomRoles.GM);
                PlayerControl.LocalPlayer.RpcSetRole(RoleTypes.Crewmate);
                PlayerControl.LocalPlayer.Data.IsDead = true;
                Main.PlayerStates[PlayerControl.LocalPlayer.PlayerId].SetDead();
            }

            EAC.OriginalRoles = [];
            SelectCustomRoles();
            SelectAddonRoles();
            CalculateVanillaRoleCount();

            //指定原版特殊职业数量
            var roleOpt = Main.NormalOptions.roleOptions;
            int ScientistNum = Options.DisableVanillaRoles.GetBool() ? 0 : roleOpt.GetNumPerGame(RoleTypes.Scientist);
            roleOpt.SetRoleRate(RoleTypes.Scientist, ScientistNum + addScientistNum, addScientistNum > 0 ? 100 : roleOpt.GetChancePerGame(RoleTypes.Scientist));
            int EngineerNum = Options.DisableVanillaRoles.GetBool() ? 0 : roleOpt.GetNumPerGame(RoleTypes.Engineer);
            roleOpt.SetRoleRate(RoleTypes.Engineer, EngineerNum + addEngineerNum, addEngineerNum > 0 ? 100 : roleOpt.GetChancePerGame(RoleTypes.Engineer));
            int ShapeshifterNum = Options.DisableVanillaRoles.GetBool() ? 0 : roleOpt.GetNumPerGame(RoleTypes.Shapeshifter);
            roleOpt.SetRoleRate(RoleTypes.Shapeshifter, ShapeshifterNum + addShapeshifterNum, addShapeshifterNum > 0 ? 100 : roleOpt.GetChancePerGame(RoleTypes.Shapeshifter));

            Dictionary<(byte, byte), RoleTypes> rolesMap = [];

            // 注册反职业
            foreach (var kv in RoleResult.Where(x => x.Value.IsDesyncRole()).ToArray())
                AssignDesyncRole(kv.Value, kv.Key, senders, rolesMap, BaseRole: kv.Value.GetDYRole());


            MakeDesyncSender(senders, rolesMap);

        }
        catch (Exception e)
        {
            Utils.ErrorEnd("Select Role Prefix");
            Logger.Fatal(e.Message, "Select Role Prefix");
        }
        //以下、バニラ側の役職割り当てが入る
    }

    public static void Postfix()
    {
        if (!AmongUsClient.Instance.AmHost) return;

        try
        {
            if (GameStates.IsHideNSeek)
            {
                GameOptionsSender.AllSenders.Clear();
                foreach (var pc in Main.AllPlayerControls)
                {
                    GameOptionsSender.AllSenders.Add(
                        new PlayerGameOptionsSender(pc)
                    );
                }

                //Utils.CountAlivePlayers(true);

                EAC.LogAllRoles();
                Utils.SyncAllSettings();
                SetColorPatch.IsAntiGlitchDisabled = false;

                return;
            }

            List<(PlayerControl, RoleTypes)> newList = [];
            foreach (var sd in RpcSetRoleReplacer.StoragedData.ToArray())
            {
                var kp = RoleResult.FirstOrDefault(x => x.Key.PlayerId == sd.Item1.PlayerId);
                newList.Add((sd.Item1, kp.Value.GetRoleTypes()));
                if (sd.Item2 == kp.Value.GetRoleTypes())
                    Logger.Warn($"Registered original Role => {sd.Item1.GetRealName()}: {sd.Item2}", "Override Role Select");
                else
                    Logger.Warn($"Coverage of original Role => {sd.Item1.GetRealName()}: {sd.Item2} => {kp.Value.GetRoleTypes()}", "Override Role Select");
            }
            if (Main.EnableGM.Value) newList.Add((PlayerControl.LocalPlayer, RoleTypes.Crewmate));
            RpcSetRoleReplacer.StoragedData = newList;

            RpcSetRoleReplacer.Release(); //保存していたSetRoleRpcを一気に書く
            RpcSetRoleReplacer.senders.Do(kvp => kvp.Value.SendMessage());

            // 不要なオブジェクトの削除
            RpcSetRoleReplacer.senders = null;
            RpcSetRoleReplacer.OverriddenSenderList = null;
            RpcSetRoleReplacer.StoragedData = null;

            //Utils.ApplySuffix();

            foreach (var pc in Main.AllPlayerControls)
            {
                pc.Data.IsDead = false; //プレイヤーの死を解除する
                if (Main.PlayerStates[pc.PlayerId].MainRole != CustomRoles.NotAssigned) continue; //既にカスタム役職が割り当てられていればスキップ
                var role = CustomRoles.NotAssigned;
                switch (pc.Data.Role.Role)
                {
                    case RoleTypes.Crewmate:
                        role = CustomRoles.Crewmate;
                        break;
                    case RoleTypes.Impostor:
                        role = CustomRoles.Impostor;
                        break;
                    case RoleTypes.Scientist:
                        role = CustomRoles.Scientist;
                        break;
                    case RoleTypes.Engineer:
                        role = CustomRoles.Engineer;
                        break;
                    case RoleTypes.GuardianAngel:
                        role = CustomRoles.GuardianAngel;
                        break;
                    case RoleTypes.Shapeshifter:
                        role = CustomRoles.Shapeshifter;
                        break;
                    default:
                        Logger.SendInGame(string.Format(GetString("Error.InvalidRoleAssignment"), pc?.Data?.PlayerName));
                        break;
                }
                Main.PlayerStates[pc.PlayerId].SetMainRole(role);
            }

            if (Options.CurrentGameMode == CustomGameMode.FFA)
            {
                foreach (var pair in Main.PlayerStates)
                    ExtendedPlayerControl.RpcSetCustomRole(pair.Key, pair.Value.MainRole);
                goto EndOfSelectRolePatch;
            }

            var rd = IRandom.Instance;

            foreach (var kv in RoleResult)
            {
                if (kv.Value.IsDesyncRole()) continue;
                AssignCustomRole(kv.Value, kv.Key);
            }

            if (CustomRoles.Lovers.IsEnable() && (CustomRoles.Hater.IsEnable() ? -1 : rd.Next(1, 100)) <= Options.LoverSpawnChances.GetInt()) AssignLoversRolesFromList();

            AssignAddonRoles();

            //RPCによる同期
            foreach (var pair in Main.PlayerStates)
            {
                ExtendedPlayerControl.RpcSetCustomRole(pair.Key, pair.Value.MainRole);

                foreach (var subRole in pair.Value.SubRoles.ToArray())
                    ExtendedPlayerControl.RpcSetCustomRole(pair.Key, subRole);
            }

            foreach (var pc in Main.AllPlayerControls)
            {
                if (pc.Data.Role.Role == RoleTypes.Shapeshifter) Main.CheckShapeshift.Add(pc.PlayerId, false);
                switch (pc.GetCustomRole())
                {
                    case CustomRoles.BountyHunter:
                        BountyHunter.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Berserker:
                        Main.BerserkerKillMax[pc.PlayerId] = 0;
                        break;
                    case CustomRoles.Reverie:
                        Reverie.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Mercenary:
                        Mercenary.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Witch:
                        Witch.Add(pc.PlayerId);
                        break;
                    case CustomRoles.HexMaster:
                        HexMaster.Add(pc.PlayerId);
                        break;
                    //case CustomRoles.Occultist:
                    //    Occultist.Add(pc.PlayerId);
                    //    break;
                    case CustomRoles.Crusader:
                        Crusader.Add(pc.PlayerId);
                        Crusader.CrusaderLimit[pc.PlayerId] = Crusader.SkillLimitOpt.GetInt();
                        break;
                    case CustomRoles.Warlock:
                        Main.CursedPlayers.Add(pc.PlayerId, null);
                        Main.isCurseAndKill.Add(pc.PlayerId, false);
                        break;
                    case CustomRoles.Fireworker:
                        Fireworker.Add(pc.PlayerId);
                        break;
                    case CustomRoles.TimeThief:
                        TimeThief.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Camouflager:
                        Camouflager.Add();
                        break;
                    case CustomRoles.Puppeteer:
                        Puppeteer.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Mastermind:
                        Mastermind.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Sniper:
                        Sniper.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Undertaker:
                        Undertaker.Add(pc.PlayerId);
                        break;
                    case CustomRoles.RiftMaker:
                        RiftMaker.Add(pc.PlayerId);
                        break;
               /*     case CustomRoles.Mare:
                        Mare.Add(pc.PlayerId);
                        break; */
                    case CustomRoles.Vampire:
                        Vampire.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Vampiress:
                        Vampiress.Add(pc.PlayerId);
                        break;
                    case CustomRoles.SwordsMan:
                        SwordsMan.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Pickpocket:
                        Pickpocket.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Arsonist:
                        foreach (var ar in Main.AllPlayerControls)
                            Main.isDoused.Add((pc.PlayerId, ar.PlayerId), false);
                        break;
                    case CustomRoles.Revolutionist:
                        foreach (var ar in Main.AllPlayerControls)
                            Main.isDraw.Add((pc.PlayerId, ar.PlayerId), false);
                        break;
                    case CustomRoles.Farseer:
                        foreach (var ar in Main.AllPlayerControls)
                        { 
                            Main.isRevealed.Add((pc.PlayerId, ar.PlayerId), false);
                        }
                        Farseer.RandomRole.Add(pc.PlayerId, Farseer.GetRandomCrewRoleString());
                        Farseer.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Executioner:
                        Executioner.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Medusa:
                        Medusa.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Necromancer:
                        Necromancer.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Lawyer:
                        Lawyer.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Jackal:
                        Jackal.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Sidekick:
                        Sidekick.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Bandit:
                        Bandit.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Doppelganger:
                        Doppelganger.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Poisoner:
                        Poisoner.Add(pc.PlayerId);
                        break;
                    case CustomRoles.PlagueDoctor:
                        PlagueDoctor.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Sheriff:
                        Sheriff.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Marshall:
                        Marshall.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Glitch:
                        Glitch.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Huntsman:
                        Huntsman.Add(pc.PlayerId);
                        break;
                    case CustomRoles.CopyCat:
                        CopyCat.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Cleanser:
                        Cleanser.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Agitater:
                        Agitater.Add(pc.PlayerId);
                        break;
                    case CustomRoles.QuickShooter:
                        QuickShooter.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Mayor:
                        Main.MayorUsedButtonCount[pc.PlayerId] = 0;
                        break;
                    case CustomRoles.Captain:
                        Captain.Add(pc.PlayerId);
                        break;
                    case CustomRoles.GuessMaster:
                        GuessMaster.Add(pc.PlayerId);
                        break;
                    case CustomRoles.TimeMaster:
                        Main.TimeMasterNum[pc.PlayerId] = 0;
                        Main.TimeMasterNumOfUsed.Add(pc.PlayerId, Options.TimeMasterMaxUses.GetInt());
                        break;
                    case CustomRoles.Masochist:
                        Main.MasochistKillMax[pc.PlayerId] = 0;
                        break;
                    case CustomRoles.Paranoia:
                        Main.ParaUsedButtonCount[pc.PlayerId] = 0;
                        break;
                    case CustomRoles.SabotageMaster:
                        SabotageMaster.Add(pc.PlayerId);
                        break;
                    case CustomRoles.EvilTracker:
                        EvilTracker.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Snitch:
                        Snitch.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Solsticer:
                        Solsticer.Add(pc.PlayerId);
                        break;
                    case CustomRoles.AntiAdminer:
                        AntiAdminer.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Monitor:
                        Monitor.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Mario:
                        Main.MarioVentCount[pc.PlayerId] = 0;
                        break;
                    case CustomRoles.TimeManager:
                        TimeManager.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Pelican:
                        Pelican.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Counterfeiter:
                        Counterfeiter.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Jailer:
                        Jailer.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Pursuer:
                        Pursuer.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Gangster:
                        Gangster.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Medic:
                        Medic.Add(pc.PlayerId);
                        break;
                    case CustomRoles.EvilDiviner:
                        EvilDiviner.Add(pc.PlayerId);
                        break;
                    case CustomRoles.PotionMaster:
                        PotionMaster.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Divinator:
                        Divinator.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Oracle:
                        Oracle.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Gamer:
                        Gamer.Add(pc.PlayerId);
                        break;
                    case CustomRoles.BallLightning:
                        BallLightning.Add(pc.PlayerId);
                        break;
                    case CustomRoles.DarkHide:
                        DarkHide.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Stealth:
                        Stealth.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Penguin:
                        Penguin.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Greedier:
                        Greedier.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Collector:
                        Collector.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Taskinator:
                        Taskinator.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Benefactor:
                        Benefactor.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Observer:
                        Observer.Add(pc.PlayerId);
                        break;
                    case CustomRoles.CursedWolf:
                        Main.CursedWolfSpellCount[pc.PlayerId] = Options.GuardSpellTimes.GetInt();
                        break;
                    case CustomRoles.Jinx:
                        Main.JinxSpellCount[pc.PlayerId] = Jinx.JinxSpellTimes.GetInt();
                        Jinx.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Eraser:
                        Eraser.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Assassin:
                        Assassin.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Arrogance:
                        Arrogance.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Juggernaut:
                        Juggernaut.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Anonymous:
                        Anonymous.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Psychic:
                        Psychic.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Hangman:
                        Hangman.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Judge:
                        Judge.Add(pc.PlayerId);
                        break;
                    case CustomRoles.President:
                        President.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Councillor:
                        Councillor.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Mortician:
                        Mortician.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Tracefinder:
                        Tracefinder.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Mediumshiper:
                        Mediumshiper.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Veteran:
                        Main.VeteranNumOfUsed.Add(pc.PlayerId, Options.VeteranSkillMaxOfUseage.GetInt());
                        break;
                    case CustomRoles.Grenadier:
                        Main.GrenadierNumOfUsed.Add(pc.PlayerId, Options.GrenadierSkillMaxOfUseage.GetInt());
                        break;
                    case CustomRoles.Bastion:
                        Main.BastionNumberOfAbilityUses = Options.BastionMaxBombs.GetInt();
                        break;
                    case CustomRoles.Swooper:
                        Swooper.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Wraith:
                        Wraith.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Lighter:
                        Main.LighterNumOfUsed.Add(pc.PlayerId, Options.LighterSkillMaxOfUseage.GetInt());
                        break;
                    case CustomRoles.SoulCollector:
                        SoulCollector.Add(pc.PlayerId);
                        break;
                    case CustomRoles.SchrodingersCat:
                        SchrodingersCat.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Chameleon:
                        Chameleon.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Alchemist:
                        Alchemist.Add(pc.PlayerId);
                        break;
                    case CustomRoles.BloodKnight:
                        BloodKnight.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Totocalcio:
                        Totocalcio.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Romantic:
                        Romantic.Add(pc.PlayerId);
                        break;
                    case CustomRoles.VengefulRomantic:
                        VengefulRomantic.Add(pc.PlayerId);
                        break;
                    case CustomRoles.RuthlessRomantic:
                        RuthlessRomantic.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Succubus:
                        Succubus.Add(pc.PlayerId);
                        break;
                    case CustomRoles.CursedSoul:
                        CursedSoul.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Admirer:
                        Admirer.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Amnesiac:
                        Amnesiac.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Imitator:
                        Imitator.Add(pc.PlayerId);
                        break;
                    case CustomRoles.DovesOfNeace:
                        Main.DovesOfNeaceNumOfUsed.Add(pc.PlayerId, Options.DovesOfNeaceMaxOfUseage.GetInt());
                        break;
                    case CustomRoles.Infectious:
                        Infectious.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Monarch:
                        Monarch.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Deputy:
                        Deputy.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Investigator:
                        Investigator.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Virus:
                        Virus.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Wildling:
                        Wildling.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Bloodhound:
                        Bloodhound.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Tracker:
                        Tracker.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Merchant:
                        Merchant.Add(pc.PlayerId);
                        break;
                    case CustomRoles.SerialKiller:
                        SerialKiller.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Pyromaniac:
                        Pyromaniac.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Kamikaze:
                        Kamikaze.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Werewolf:
                        Werewolf.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Traitor:
                        Traitor.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Shroud:
                        Shroud.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Maverick:
                        Maverick.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Dazzler:
                        Dazzler.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Addict:
                        Addict.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Mole:
                        Mole.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Deathpact:
                        Deathpact.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Morphling:
                        Morphling.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Devourer:
                        Devourer.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Spiritualist:
                        Spiritualist.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Vulture:
                        Vulture.Add(pc.PlayerId);
                        break;
                    case CustomRoles.PlagueBearer:
                        PlagueBearer.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Inspector:
                        Inspector.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Keeper:
                        Keeper.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Spiritcaller:
                        Spiritcaller.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Lurker:
                        Lurker.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Doomsayer:
                        Doomsayer.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Pirate:
                        Pirate.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Pixie:
                        Pixie.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Chronomancer:
                        Chronomancer.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Seeker:
                        Seeker.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Pitfall:
                        Pitfall.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Swapper:
                        Swapper.Add(pc.PlayerId);
                        break;
                    case CustomRoles.ChiefOfPolice:
                        ChiefOfPolice.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Blackmailer:
                        Blackmailer.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Spy:
                        Spy.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Instigator:
                        Instigator.Add(pc.PlayerId);
                        break;
                    case CustomRoles.NiceMini:
                        Mini.Add(pc.PlayerId);
                        break;
                    case CustomRoles.EvilMini:
                        Mini.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Crewpostor:
                        Main.CrewpostorTasksDone[pc.PlayerId] = 0;
                        break;
                    case CustomRoles.Hater:
                        Hater.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Enigma:
                        Enigma.Add(pc.PlayerId);
                        break;
                    case CustomRoles.Quizmaster:
                        Quizmaster.Add(pc.PlayerId);
                        break;
                }
                foreach (var subRole in pc.GetCustomSubRoles().ToArray())
                {
                    switch (subRole)
                    {
                        case CustomRoles.Aware:
                            Main.AwareInteracted[pc.PlayerId] = [];
                            break;
                        case CustomRoles.Oiiai:
                            Oiiai.Add(pc.PlayerId);
                            break;
                        case CustomRoles.Tired:
                            Tired.Add(pc.PlayerId);
                            break;
                        case CustomRoles.Rainbow:
                            Rainbow.Add();
                            break;
                        case CustomRoles.Statue:
                            Statue.Add(pc.PlayerId);
                            break;

                        default:
                            break;
                    }
                }
            }

            EndOfSelectRolePatch:

            HudManager.Instance.SetHudActive(true);
      //      HudManager.Instance.Chat.SetVisible(true);
            List<PlayerControl> AllPlayers = [];
            CustomRpcSender sender = CustomRpcSender.Create("SelectRoles Sender", SendOption.Reliable);
            foreach (var pc in Main.AllPlayerControls)
                pc.ResetKillCooldown();

            //役職の人数を戻す
            var roleOpt = Main.NormalOptions.roleOptions;
            int ScientistNum = Options.DisableVanillaRoles.GetBool() ? 0 : roleOpt.GetNumPerGame(RoleTypes.Scientist);
            ScientistNum -= addScientistNum;
            roleOpt.SetRoleRate(RoleTypes.Scientist, ScientistNum, roleOpt.GetChancePerGame(RoleTypes.Scientist));
            int EngineerNum = Options.DisableVanillaRoles.GetBool() ? 0 : roleOpt.GetNumPerGame(RoleTypes.Engineer);
            EngineerNum -= addEngineerNum;
            roleOpt.SetRoleRate(RoleTypes.Engineer, EngineerNum, roleOpt.GetChancePerGame(RoleTypes.Engineer));
            int ShapeshifterNum = Options.DisableVanillaRoles.GetBool() ? 0 : roleOpt.GetNumPerGame(RoleTypes.Shapeshifter);
            ShapeshifterNum -= addShapeshifterNum;
            roleOpt.SetRoleRate(RoleTypes.Shapeshifter, ShapeshifterNum, roleOpt.GetChancePerGame(RoleTypes.Shapeshifter));

            switch (Options.CurrentGameMode)
            {
                case CustomGameMode.Standard:
                    GameEndCheckerForNormal.SetPredicateToNormal();
                    break;
                case CustomGameMode.FFA:
                    GameEndCheckerForNormal.SetPredicateToFFA();
                    break;
            }

            GameOptionsSender.AllSenders.Clear();
            foreach (var pc in Main.AllPlayerControls)
            {
                GameOptionsSender.AllSenders.Add(
                    new PlayerGameOptionsSender(pc)
                );
            }

            // Added players with positions that have not yet been classified to the list of players requiring ResetCam   
            Main.ResetCamPlayerList.UnionWith(Main.AllPlayerControls
                .Where(p => p.GetCustomRole() is CustomRoles.Arsonist or CustomRoles.Revolutionist or CustomRoles.Sidekick or CustomRoles.Shaman or CustomRoles.Vigilante or CustomRoles.Witness or CustomRoles.Innocent or CustomRoles.Killer)
                .Select(p => p.PlayerId)
                .ToArray());
            EAC.LogAllRoles();

            Utils.CountAlivePlayers(true);
            Utils.SyncAllSettings();
            SetColorPatch.IsAntiGlitchDisabled = false;

            Logger.Msg("Ended", "AssignRoles");
        }
        catch (Exception ex)
        {
            Utils.ErrorEnd("Select Role Postfix");
            Logger.Fatal(ex.ToString(), "Select Role Prefix");
        }
    }
    private static void AssignDesyncRole(CustomRoles role, PlayerControl player, Dictionary<byte, CustomRpcSender> senders, Dictionary<(byte, byte), RoleTypes> rolesMap, RoleTypes BaseRole, RoleTypes hostBaseRole = RoleTypes.Crewmate)
    {
        if (player == null) return;

        var hostId = PlayerControl.LocalPlayer.PlayerId;

        Main.PlayerStates[player.PlayerId].SetMainRole(role);

        var selfRole = player.PlayerId == hostId ? hostBaseRole : BaseRole;
        var othersRole = player.PlayerId == hostId ? RoleTypes.Crewmate : RoleTypes.Scientist;

        //Desync役職視点
        foreach (var target in Main.AllPlayerControls)
            rolesMap[(player.PlayerId, target.PlayerId)] = player.PlayerId != target.PlayerId ? othersRole : selfRole;

        //他者視点
        foreach (var seer in Main.AllPlayerControls.Where(x => player.PlayerId != x.PlayerId).ToArray())
            rolesMap[(seer.PlayerId, player.PlayerId)] = othersRole;

        RpcSetRoleReplacer.OverriddenSenderList.Add(senders[player.PlayerId]);
        //ホスト視点はロール決定
        player.SetRole(othersRole);
        player.Data.IsDead = true;

        Logger.Info($"Registered Role： {player?.Data?.PlayerName} => {role}", "AssignRoles");
    }
    public static void MakeDesyncSender(Dictionary<byte, CustomRpcSender> senders, Dictionary<(byte, byte), RoleTypes> rolesMap)
    {
        foreach (var seer in Main.AllPlayerControls)
        {
            var sender = senders[seer.PlayerId];
            foreach (var target in Main.AllPlayerControls)
            {
                if (rolesMap.TryGetValue((seer.PlayerId, target.PlayerId), out var role))
                {
                    sender.RpcSetRole(seer, role, target.GetClientId());
                }
            }
        }
    }

    private static void AssignCustomRole(CustomRoles role, PlayerControl player)
    {
        if (player == null) return;
        SetColorPatch.IsAntiGlitchDisabled = true;

        Main.PlayerStates[player.PlayerId].SetMainRole(role);
        Logger.Info($"Registered Role： {player?.Data?.PlayerName} => {role}", "AssignRoles");

        SetColorPatch.IsAntiGlitchDisabled = false;
    }
    private static void ForceAssignRole(CustomRoles role, List<PlayerControl> AllPlayers, CustomRpcSender sender, RoleTypes BaseRole, RoleTypes hostBaseRole = RoleTypes.Crewmate, bool skip = false, int Count = -1)
    {
        var count = 1;

        if (Count != -1)
            count = Count;
        for (var i = 0; i < count; i++)
        {
            if (AllPlayers.Count <= 0) break;
            var rand = IRandom.Instance;
            var player = AllPlayers[rand.Next(0, AllPlayers.Count)];
            AllPlayers.Remove(player);
            Main.AllPlayerCustomRoles[player.PlayerId] = role;
            if (!skip)
            {
                if (!player.IsModClient())
                {
                    int playerCID = player.GetClientId();
                    sender.RpcSetRole(player, BaseRole, playerCID);
                    //Desyncする人視点で他プレイヤーを科学者にするループ
                    foreach (var pc in PlayerControl.AllPlayerControls.ToArray())
                    {
                        if (pc == player) continue;
                        sender.RpcSetRole(pc, RoleTypes.Scientist, playerCID);
                    }
                    //他視点でDesyncする人の役職を科学者にするループ
                    foreach (var pc in PlayerControl.AllPlayerControls.ToArray())
                    {
                        if (pc == player) continue;
                        if (pc.PlayerId == 0) player.SetRole(RoleTypes.Scientist); //ホスト視点用
                        else sender.RpcSetRole(player, RoleTypes.Scientist, pc.GetClientId());
                    }
                }
                else
                {
                    //ホストは別の役職にする
                    player.SetRole(hostBaseRole); //ホスト視点用
                    sender.RpcSetRole(player, hostBaseRole);
                }
            }
        }
    }

    private static void AssignLoversRolesFromList()
    {
        if (CustomRoles.Lovers.IsEnable())
        {
            //Loversを初期化
            Main.LoversPlayers.Clear();
            Main.isLoversDead = false;
            //ランダムに2人選出
            AssignLoversRoles();
        }
    }
    private static void AssignLoversRoles(int RawCount = -1)
    {
        var allPlayers = new List<PlayerControl>();
        foreach (var pc in Main.AllPlayerControls)
        {
            if (pc.Is(CustomRoles.GM) 
                || (pc.HasSubRole() && pc.GetCustomSubRoles().Count >= Options.NoLimitAddonsNumMax.GetInt()) 
                || pc.Is(CustomRoles.Ntr) 
                || pc.Is(CustomRoles.Dictator) 
                || pc.Is(CustomRoles.God) 
                || pc.Is(CustomRoles.Hater) 
                || pc.Is(CustomRoles.Sunnyboy)
                || pc.Is(CustomRoles.Bomber)
                || pc.Is(CustomRoles.Nuker) 
                || pc.Is(CustomRoles.Provocateur)
                || pc.Is(CustomRoles.RuthlessRomantic)
                || pc.Is(CustomRoles.Romantic)
                || pc.Is(CustomRoles.VengefulRomantic)
                || (pc.GetCustomRole().IsCrewmate() && !Options.CrewCanBeInLove.GetBool())
                || (pc.GetCustomRole().IsNeutral() && !Options.NeutralCanBeInLove.GetBool())
                || (pc.GetCustomRole().IsImpostor() && !Options.ImpCanBeInLove.GetBool()))
                continue;
            allPlayers.Add(pc);
        }
        var role = CustomRoles.Lovers;
        var rd = IRandom.Instance;
        var count = Math.Clamp(RawCount, 0, allPlayers.Count);
        if (RawCount == -1) count = Math.Clamp(role.GetCount(), 0, allPlayers.Count);
        if (count <= 0) return;
        for (var i = 0; i < count; i++)
        {
            var player = allPlayers[rd.Next(0, allPlayers.Count)];
            Main.LoversPlayers.Add(player);
            allPlayers.Remove(player);
            Main.PlayerStates[player.PlayerId].SetSubRole(role);
            Logger.Info($"Registered Lovers: {player?.Data?.PlayerName} = {player.GetCustomRole()} + {role}", "Assign Lovers");
        }
        RPC.SyncLoversPlayers();
    }
    public static void AssignSubRoles(CustomRoles role, int RawCount = -1)
    {
        var allPlayers = Main.AllAlivePlayerControls.Where(x => CustomRolesHelper.CheckAddonConfilct(role, x)).ToList();
        var count = Math.Clamp(RawCount, 0, allPlayers.Count);
        if (RawCount == -1) count = Math.Clamp(role.GetCount(), 0, allPlayers.Count);
        if (count <= 0) return;
        for (var i = 0; i < count; i++)
        {
            // if the number of all players is 0
            if (allPlayers.Count <= 0) return;

            // Select player
            var player = allPlayers[IRandom.Instance.Next(allPlayers.Count)];
            allPlayers.Remove(player);

            // Set Add-on
            Main.PlayerStates[player.PlayerId].SetSubRole(role);
            Logger.Info($"Registered Add-on: {player?.Data?.PlayerName} = {player.GetCustomRole()} + {role}", $"Assign {role}");
        }
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSetRole))]
    private class RpcSetRoleReplacer
    {
        public static bool doReplace = false;
        public static Dictionary<byte, CustomRpcSender> senders;
        public static List<(PlayerControl, RoleTypes)> StoragedData = [];
        // List of Senders that do not require additional writing because SetRoleRpc has already been written by another process such as Position Desync
        public static List<CustomRpcSender> OverriddenSenderList;
        public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] RoleTypes roleType)
        {
            if (doReplace && senders != null)
            {
                StoragedData.Add((__instance, roleType));
                return false;
            }
            else return true;
        }
        public static void Release()
        {
            foreach (var sender in senders)
            {
                if (OverriddenSenderList.Contains(sender.Value)) continue;
                if (sender.Value.CurrentState != CustomRpcSender.State.InRootMessage)
                    throw new InvalidOperationException("A CustomRpcSender had Invalid State.");

                foreach (var pair in StoragedData)
                {
                    pair.Item1.SetRole(pair.Item2);
                    sender.Value.AutoStartRpc(pair.Item1.NetId, (byte)RpcCalls.SetRole, Utils.GetPlayerById(sender.Key).GetClientId())
                        .Write((ushort)pair.Item2)
                        .EndRpc();
                }
                sender.Value.EndMessage();
            }
            doReplace = false;
        }
        public static void StartReplace(Dictionary<byte, CustomRpcSender> senders)
        {
            RpcSetRoleReplacer.senders = senders;
            StoragedData = [];
            OverriddenSenderList = [];
            doReplace = true;
        }
    }
}
