using AmongUs.GameOptions;
using System;
using System.Collections.Generic;
using System.Linq;
using TOHE.Modules;
using TOHE.Roles.AddOns.Common;
using TOHE.Roles.AddOns.Crewmate;
using TOHE.Roles.AddOns.Impostor;
using TOHE.Roles.Crewmate;
using TOHE.Roles.Neutral;
using UnityEngine;

namespace TOHE;

public class PlayerState(byte playerId)
{
    readonly byte PlayerId = playerId;
    public CustomRoles MainRole = CustomRoles.NotAssigned;
    public List<CustomRoles> SubRoles = [];
    public CountTypes countTypes = CountTypes.OutOfGame;
    public bool IsDead { get; set; } = false;
#pragma warning disable IDE1006 // Naming Styles
    public DeathReason deathReason { get; set; } = DeathReason.etc;
#pragma warning restore IDE1006
    public TaskState taskState = new();
    public bool IsBlackOut { get; set; } = false;
    public (DateTime, byte) RealKiller = (DateTime.MinValue, byte.MaxValue);
    public PlainShipRoom LastRoom = null;
    public Dictionary<byte, string> TargetColorData = [];

    public CustomRoles GetCustomRoleFromRoleType()
    {
        var RoleInfo = Utils.GetPlayerInfoById(PlayerId);
        return RoleInfo.Role == null
            ? MainRole
            : RoleInfo.Role.Role switch
            {
                RoleTypes.Crewmate => CustomRoles.Crewmate,
                RoleTypes.Engineer => CustomRoles.Engineer,
                RoleTypes.Scientist => CustomRoles.Scientist,
                RoleTypes.GuardianAngel => CustomRoles.GuardianAngel,
                RoleTypes.Impostor => CustomRoles.Impostor,
                RoleTypes.Shapeshifter => CustomRoles.Shapeshifter,
                _ => CustomRoles.Crewmate,
            };
    }
    public void SetMainRole(CustomRoles role)
    {
        MainRole = role;
        countTypes = role.GetCountTypes();
        var pc = Utils.GetPlayerById(PlayerId);

        if (role == CustomRoles.Opportunist)
        {
            if (AmongUsClient.Instance.AmHost)
            {
                if (!pc.HasImpKillButton(considerVanillaShift: true))
                {
                    var taskstate = pc.GetPlayerTaskState();
                    if (taskstate != null)
                    {
                        GameData.Instance.RpcSetTasks(pc.PlayerId, Array.Empty<byte>());
                        taskstate.CompletedTasksCount = 0;
                        taskstate.AllTasksCount = pc.Data.Tasks.Count;
                        taskstate.hasTasks = true;
                    }
                }
            }
        }
        // check for role addon
        if (pc.Is(CustomRoles.Madmate))
        {
            countTypes = Madmate.MadmateCountMode.GetInt() switch
            {
                0 => CountTypes.OutOfGame,
                1 => CountTypes.Impostor,
                2 => countTypes,
                _ => throw new NotImplementedException()
            };
        }
        if (pc.Is(CustomRoles.Charmed))
        {
            countTypes = Succubus.CharmedCountMode.GetInt() switch
            {
                0 => CountTypes.OutOfGame,
                1 => CountTypes.Succubus,
                2 => countTypes,
                _ => throw new NotImplementedException()
            };
        }
        if (pc.Is(CustomRoles.Recruit))
        {
            countTypes = Jackal.SidekickCountMode.GetInt() switch
            {
                0 => CountTypes.Jackal,
                1 => CountTypes.OutOfGame,
                2 => countTypes,
                _ => throw new NotImplementedException()
            };
        }
        if (pc.Is(CustomRoles.Infected))
        {
            countTypes = CountTypes.Infectious;
        }
        if (pc.Is(CustomRoles.Contagious))
        {
            countTypes = Virus.ContagiousCountMode.GetInt() switch
            {
                0 => CountTypes.OutOfGame,
                1 => CountTypes.Virus,
                2 => countTypes,
                _ => throw new NotImplementedException()
            };
        }
        if (pc.Is(CustomRoles.Rogue))
        {
            countTypes = CountTypes.Rogue;
        }
        if (pc.Is(CustomRoles.Admired))
        {
            countTypes = CountTypes.Crew;
        }
        if (pc.Is(CustomRoles.Soulless))
        {
            countTypes = CountTypes.OutOfGame;
        }

    }
    public void SetSubRole(CustomRoles role, bool AllReplace = false, PlayerControl pc = null)
    {
        if (role == CustomRoles.Cleansed)
        {
            if (pc != null) countTypes = pc.GetCustomRole().GetCountTypes();
            AllReplace = true;
        }
        if (AllReplace)
        {
            var sync = false;
            foreach (var subRole in SubRoles.ToArray())
            {
                if (pc.Is(CustomRoles.Flash))
                {
                    Flash.SetSpeed(pc.PlayerId, true);
                    sync = true;
                }
                SubRoles.Remove(subRole);

                if (sync) Utils.MarkEveryoneDirtySettings();
            }
        }

        if (!SubRoles.Contains(role))
            SubRoles.Add(role);

        switch (role)
        {
            case CustomRoles.LastImpostor:
                SubRoles.Remove(CustomRoles.Mare);
                break;

            case CustomRoles.Madmate:
                countTypes = Madmate.MadmateCountMode.GetInt() switch
                {
                    0 => CountTypes.OutOfGame,
                    1 => CountTypes.Impostor,
                    2 => countTypes,
                    _ => throw new NotImplementedException()
                };
                SubRoles.Remove(CustomRoles.Charmed);
                SubRoles.Remove(CustomRoles.Recruit);
                SubRoles.Remove(CustomRoles.Infected);
                SubRoles.Remove(CustomRoles.Contagious);
                SubRoles.Remove(CustomRoles.Rogue);
                SubRoles.Remove(CustomRoles.Rascal);
                SubRoles.Remove(CustomRoles.Soulless);
                SubRoles.Remove(CustomRoles.Loyal);
                SubRoles.Remove(CustomRoles.Admired);
                break;

            case CustomRoles.Charmed:
                countTypes = Succubus.CharmedCountMode.GetInt() switch
                {
                    0 => CountTypes.OutOfGame,
                    1 => CountTypes.Succubus,
                    2 => countTypes,
                    _ => throw new NotImplementedException()
                };
                SubRoles.Remove(CustomRoles.Madmate);
                SubRoles.Remove(CustomRoles.Recruit);
                SubRoles.Remove(CustomRoles.Infected);
                SubRoles.Remove(CustomRoles.Contagious);
                SubRoles.Remove(CustomRoles.Rogue);
                SubRoles.Remove(CustomRoles.Rascal);
                SubRoles.Remove(CustomRoles.Soulless);
                SubRoles.Remove(CustomRoles.Loyal);
                SubRoles.Remove(CustomRoles.Admired);
                break;

            case CustomRoles.Recruit:
                countTypes = Jackal.SidekickCountMode.GetInt() switch
                {
                    0 => CountTypes.Jackal,
                    1 => CountTypes.OutOfGame,
                    2 => countTypes,
                    _ => throw new NotImplementedException()
                };
                SubRoles.Remove(CustomRoles.Madmate);
                SubRoles.Remove(CustomRoles.Charmed);
                SubRoles.Remove(CustomRoles.Infected);
                SubRoles.Remove(CustomRoles.Contagious);
                SubRoles.Remove(CustomRoles.Rogue);
                SubRoles.Remove(CustomRoles.Rascal);
                SubRoles.Remove(CustomRoles.Soulless);
                SubRoles.Remove(CustomRoles.Loyal);
                SubRoles.Remove(CustomRoles.Loyal);
                SubRoles.Remove(CustomRoles.Admired);
                break;

            case CustomRoles.Infected:
                countTypes = CountTypes.Infectious;
                SubRoles.Remove(CustomRoles.Madmate);
                SubRoles.Remove(CustomRoles.Recruit);
                SubRoles.Remove(CustomRoles.Charmed);
                SubRoles.Remove(CustomRoles.Rogue);
                SubRoles.Remove(CustomRoles.Contagious);
                SubRoles.Remove(CustomRoles.Rascal);
                SubRoles.Remove(CustomRoles.Soulless);
                SubRoles.Remove(CustomRoles.Loyal);
                SubRoles.Remove(CustomRoles.Admired);
                break;

            case CustomRoles.Contagious:
                countTypes = Virus.ContagiousCountMode.GetInt() switch
                {
                    0 => CountTypes.OutOfGame,
                    1 => CountTypes.Virus,
                    2 => countTypes,
                    _ => throw new NotImplementedException()
                };
                SubRoles.Remove(CustomRoles.Madmate);
                SubRoles.Remove(CustomRoles.Recruit);
                SubRoles.Remove(CustomRoles.Rogue);
                SubRoles.Remove(CustomRoles.Charmed);
                SubRoles.Remove(CustomRoles.Infected);
                SubRoles.Remove(CustomRoles.Rascal);
                SubRoles.Remove(CustomRoles.Soulless);
                SubRoles.Remove(CustomRoles.Loyal);
                SubRoles.Remove(CustomRoles.Admired);
                break;

            case CustomRoles.Rogue:
                countTypes = CountTypes.Rogue;
                SubRoles.Remove(CustomRoles.Madmate);
                SubRoles.Remove(CustomRoles.Recruit);
                SubRoles.Remove(CustomRoles.Charmed);
                SubRoles.Remove(CustomRoles.Infected);
                SubRoles.Remove(CustomRoles.Contagious);
                SubRoles.Remove(CustomRoles.Rascal);
                SubRoles.Remove(CustomRoles.Soulless);
                SubRoles.Remove(CustomRoles.Loyal);
                SubRoles.Remove(CustomRoles.Admired);
                break;

            // This exist as it would be possible for them to exist on the same player via Bandit
            // But since Bandit can't vent without Nimble, allowing them to have Circumvent is pointless
            case CustomRoles.Nimble:
                SubRoles.Remove(CustomRoles.Circumvent);
                break;

            case CustomRoles.Admired:
                countTypes = CountTypes.Crew;
                SubRoles.Remove(CustomRoles.Madmate);
                SubRoles.Remove(CustomRoles.Recruit);
                SubRoles.Remove(CustomRoles.Charmed);
                SubRoles.Remove(CustomRoles.Infected);
                SubRoles.Remove(CustomRoles.Contagious);
                SubRoles.Remove(CustomRoles.Rascal);
                SubRoles.Remove(CustomRoles.Soulless);
                SubRoles.Remove(CustomRoles.Loyal);
                SubRoles.Remove(CustomRoles.Rogue);
                break;

            case CustomRoles.Soulless:
                countTypes = CountTypes.OutOfGame;
                SubRoles.Remove(CustomRoles.Madmate);
                SubRoles.Remove(CustomRoles.Recruit);
                SubRoles.Remove(CustomRoles.Charmed);
                SubRoles.Remove(CustomRoles.Infected);
                SubRoles.Remove(CustomRoles.Contagious);
                SubRoles.Remove(CustomRoles.Rascal);
                SubRoles.Remove(CustomRoles.Rogue);
                SubRoles.Remove(CustomRoles.Loyal);
                SubRoles.Remove(CustomRoles.Admired);
                break;
        }
    }
    public void RemoveSubRole(CustomRoles role)
    {
        if (SubRoles.Contains(role))
            SubRoles.Remove(role);
    }

    public void SetDead()
    {
        IsDead = true;
        if (AmongUsClient.Instance.AmHost)
        {
            RPC.SendDeathReason(PlayerId, deathReason);
            if (GameStates.IsMeeting)
            {
                MeetingHud.Instance.CheckForEndVoting();
            }
        }
    }
    public bool IsSuicide => deathReason == DeathReason.Suicide;
    public TaskState TaskState => taskState;
    public void InitTask(PlayerControl player) => taskState.Init(player);
    public void UpdateTask(PlayerControl player) => taskState.Update(player);

    public enum DeathReason
    {
        Kill,
        Vote,
        Suicide,
        Spell,
        Curse,
        Hex,
        FollowingSuicide,
        Bite,
        Poison,
        Bombed,
        Misfire,
        Torched,
        Sniped,
        Revenge,
        Execution,
        Disconnected,
        Fall,

        // TOHE
        Gambled,
        Eaten,
        Sacrifice,
        Quantization,
        Overtired,
        Ashamed,
        PissedOff,
        Dismembered,
        LossOfHead,
        Trialed,
        Infected,
        Jinx,
        Hack,
        Pirate,
        Shrouded,
        Mauled,
        Drained,
        Shattered,
        Trap,
        Targeted,
        Retribution,
        WrongAnswer,

        //Please add all new roles with deathreason & new deathreason in Susceptible.CallEnabledAndChange
        etc = -1,
    }

    public byte GetRealKiller()
        => IsDead && RealKiller.Item1 != DateTime.MinValue ? RealKiller.Item2 : byte.MaxValue;

    public int GetKillCount(bool ExcludeSelfKill = false)
    {
        int count = 0;
        foreach (var state in Main.PlayerStates.Values.ToArray())
            if (!(ExcludeSelfKill && state.PlayerId == PlayerId) && state.GetRealKiller() == PlayerId)
                count++;
        return count;
    }
}
public class TaskState
{
    public static int InitialTotalTasks;
    public int AllTasksCount;
    public int CompletedTasksCount;
    public bool hasTasks;
    public int RemainingTasksCount => AllTasksCount - CompletedTasksCount;
    public bool DoExpose => RemainingTasksCount <= Options.SnitchExposeTaskLeft && hasTasks;
    public bool IsTaskFinished => RemainingTasksCount <= 0 && hasTasks;
    public TaskState()
    {
        this.AllTasksCount = -1;
        this.CompletedTasksCount = 0;
        this.hasTasks = false;
    }

    public void Init(PlayerControl player)
    {
        Logger.Info($"{player.GetNameWithRole().RemoveHtmlTags()}: InitTask", "TaskState.Init");

        if (player == null || player.Data == null || player.Data.Tasks == null) return;

        if (!Utils.HasTasks(player.Data, false))
        {
            AllTasksCount = 0;
            return;
        }

        hasTasks = true;
        AllTasksCount = player.Data.Tasks.Count;

        Logger.Info($"{player.GetNameWithRole().RemoveHtmlTags()}: TaskCounts = {CompletedTasksCount}/{AllTasksCount}", "TaskState.Init");
    }
    public void Update(PlayerControl player)
    {
        Logger.Info($"{player.GetNameWithRole().RemoveHtmlTags()}: UpdateTask", "TaskState.Update");
        GameData.Instance.RecomputeTaskCounts();
        Logger.Info($"TotalTaskCounts = {GameData.Instance.CompletedTasks}/{GameData.Instance.TotalTasks}", "TaskState.Update");

        // If not initialized, initialize it
        if (AllTasksCount == -1) Init(player);

        if (!hasTasks) return;

        if (AmongUsClient.Instance.AmHost)
        {
            var playerRole = player.GetCustomRole();
            var playerSubRoles = player.GetCustomSubRoles();

            switch (playerRole)
            {
                //case CustomRoles.SpeedBooster when player.IsAlive():
                //    if ((CompletedTasksCount + 1) <= Options.SpeedBoosterTimes.GetInt())
                //    {
                //        Logger.Info($"Speed Booster: {player.GetNameWithRole().RemoveHtmlTags()} completed the task", "SpeedBooster");
                //        Main.AllPlayerSpeed[player.PlayerId] += Options.SpeedBoosterUpSpeed.GetFloat();
                //        if (Main.AllPlayerSpeed[player.PlayerId] > 3) player.Notify(Translator.GetString("SpeedBoosterSpeedLimit"));
                //        else player.Notify(string.Format(Translator.GetString("SpeedBoosterTaskDone"), Main.AllPlayerSpeed[player.PlayerId].ToString("0.0#####")));
                //    }
                //    break;

                case CustomRoles.Transporter when player.IsAlive():
                    if ((CompletedTasksCount + 1) <= Options.TransporterTeleportMax.GetInt())
                    {
                        Logger.Info($"Transporter: {player.GetNameWithRole().RemoveHtmlTags()} completed the task", "Transporter");

                        var rd = IRandom.Instance;
                        List<PlayerControl> AllAlivePlayer = Main.AllAlivePlayerControls.Where(x => x.CanBeTeleported()).ToList();

                        if (AllAlivePlayer.Count >= 2)
                        {
                            var target1 = AllAlivePlayer[rd.Next(0, AllAlivePlayer.Count)];
                            var positionTarget1 = target1.GetCustomPosition();

                            AllAlivePlayer.Remove(target1);

                            var target2 = AllAlivePlayer[rd.Next(0, AllAlivePlayer.Count)];
                            var positionTarget2 = target2.GetCustomPosition();

                            target1.RpcTeleport(positionTarget2);
                            target2.RpcTeleport(positionTarget1);

                            AllAlivePlayer.Clear();

                            target1.RPCPlayCustomSound("Teleport");
                            target2.RPCPlayCustomSound("Teleport");

                            target1.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Transporter), string.Format(Translator.GetString("TeleportedByTransporter"), target2.GetRealName())));
                            target2.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Transporter), string.Format(Translator.GetString("TeleportedByTransporter"), target1.GetRealName())));
                        }
                        else
                        {
                            player.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Impostor), string.Format(Translator.GetString("ErrorTeleport"), player.GetRealName())));
                        }
                    }
                    break;

                case CustomRoles.Veteran when player.IsAlive():
                    Main.VeteranNumOfUsed[player.PlayerId] += Options.VeteranAbilityUseGainWithEachTaskCompleted.GetFloat();
                    break;

                case CustomRoles.Grenadier when player.IsAlive():
                    Main.GrenadierNumOfUsed[player.PlayerId] += Options.GrenadierAbilityUseGainWithEachTaskCompleted.GetFloat();
                    break;

                case CustomRoles.Alchemist when player.IsAlive():
                    Alchemist.OnTaskComplete(player);
                    break;

                case CustomRoles.Bastion when player.IsAlive():
                    Main.BastionNumberOfAbilityUses += Options.BastionAbilityUseGainWithEachTaskCompleted.GetFloat();
                    break;

                case CustomRoles.Captain when player.IsAlive():
                    Captain.OnTaskComplete(player);
                    break;

                case CustomRoles.Divinator when player.IsAlive():
                    Divinator.CheckLimit[player.PlayerId] += Divinator.AbilityUseGainWithEachTaskCompleted.GetFloat();
                    Divinator.SendRPC(player.PlayerId);
                    break;

                case CustomRoles.Lighter when player.IsAlive():
                    Main.LighterNumOfUsed[player.PlayerId] += Options.LighterAbilityUseGainWithEachTaskCompleted.GetFloat();
                    break;

                case CustomRoles.DovesOfNeace when player.IsAlive():
                    Main.DovesOfNeaceNumOfUsed[player.PlayerId] += Options.DovesOfNeaceAbilityUseGainWithEachTaskCompleted.GetFloat();
                    break;

                case CustomRoles.TimeMaster when player.IsAlive():
                    Main.TimeMasterNumOfUsed[player.PlayerId] += Options.TimeMasterAbilityUseGainWithEachTaskCompleted.GetFloat();
                    break;

                case CustomRoles.Mediumshiper when player.IsAlive():
                    Mediumshiper.ContactLimit[player.PlayerId] += Mediumshiper.MediumAbilityUseGainWithEachTaskCompleted.GetFloat();
                    Mediumshiper.SendRPC(player.PlayerId);
                    break;

                case CustomRoles.Inspector when player.IsAlive():
                    Inspector.MaxCheckLimit[player.PlayerId] += Inspector.InspectAbilityUseGainWithEachTaskCompleted.GetFloat();
                    Inspector.SendRPC(player.PlayerId, 2);
                    break;

                case CustomRoles.Oracle when player.IsAlive():
                    Oracle.CheckLimit[player.PlayerId] += Oracle.OracleAbilityUseGainWithEachTaskCompleted.GetFloat();
                    Oracle.SendRPC(player.PlayerId);
                    break;

                //case CustomRoles.Cleanser when player.IsAlive():
                //    Cleanser.CleanserUses[player.PlayerId] += Cleanser.AbilityUseGainWithEachTaskCompleted.GetInt();
                //    break;

                case CustomRoles.SabotageMaster when player.IsAlive():
                    SabotageMaster.UsedSkillCount[player.PlayerId] -= SabotageMaster.SMAbilityUseGainWithEachTaskCompleted.GetFloat();
                    SabotageMaster.SendRPC(player.PlayerId);
                    break;

                case CustomRoles.Tracker when player.IsAlive():
                    Tracker.TrackLimit[player.PlayerId] += Tracker.TrackerAbilityUseGainWithEachTaskCompleted.GetFloat();
                    Tracker.SendRPC(2, player.PlayerId);
                    break;

                case CustomRoles.Bloodhound when player.IsAlive():
                    Bloodhound.UseLimit[player.PlayerId] += Bloodhound.BloodhoundAbilityUseGainWithEachTaskCompleted.GetFloat();
                    Bloodhound.SendRPCLimit(player.PlayerId, operate: 2);
                    break;

                case CustomRoles.Chameleon when player.IsAlive():
                    Chameleon.UseLimit[player.PlayerId] += Chameleon.ChameleonAbilityUseGainWithEachTaskCompleted.GetFloat();
                    Chameleon.SendRPC(player, isLimit: true);
                    break;

                case CustomRoles.Spy when player.IsAlive():
                    Spy.UseLimit[player.PlayerId] += Spy.SpyAbilityUseGainWithEachTaskCompleted.GetFloat();
                    Spy.SendAbilityRPC(player.PlayerId);
                    break;

                case CustomRoles.Merchant when player.IsAlive():
                    Merchant.OnTaskFinished(player);
                    break;

                case CustomRoles.Workaholic when (CompletedTasksCount + 1) >= AllTasksCount && !(Options.WorkaholicCannotWinAtDeath.GetBool() && !player.IsAlive()):
                    Logger.Info("The Workaholic task is done", "Workaholic");

                    RPC.PlaySoundRPC(player.PlayerId, Sounds.KillSound);
                    foreach (var pc in Main.AllAlivePlayerControls)
                    {
                        if (pc.PlayerId != player.PlayerId)
                        {
                            Main.PlayerStates[pc.PlayerId].deathReason = pc.PlayerId == player.PlayerId ?
                                PlayerState.DeathReason.Overtired : PlayerState.DeathReason.Ashamed;

                            pc.RpcMurderPlayerV3(pc);
                            Main.PlayerStates[pc.PlayerId].SetDead();
                            pc.SetRealKiller(player);
                        }
                    }

                    if (!CustomWinnerHolder.CheckForConvertedWinner(player.PlayerId))
                    {
                        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Workaholic); //Workaholic win
                        CustomWinnerHolder.WinnerIds.Add(player.PlayerId);
                    }
                    break;

                case CustomRoles.Crewpostor:
                    if (Main.CrewpostorTasksDone.ContainsKey(player.PlayerId))
                        Main.CrewpostorTasksDone[player.PlayerId]++;
                    else
                        Main.CrewpostorTasksDone[player.PlayerId] = 0;

                    RPC.CrewpostorTasksSendRPC(player.PlayerId, Main.CrewpostorTasksDone[player.PlayerId]);
                    List<PlayerControl> list = Main.AllAlivePlayerControls.Where(x => x.PlayerId != player.PlayerId && (Options.CrewpostorCanKillAllies.GetBool() || !x.GetCustomRole().IsImpostorTeam())).ToList();

                    if (list.Count <= 0)
                    {
                        Logger.Info($"No target to kill", "Crewpostor");
                    }
                    else if (Main.CrewpostorTasksDone[player.PlayerId] % Options.CrewpostorKillAfterTask.GetInt() != 0 && Main.CrewpostorTasksDone[player.PlayerId] != 0)
                    {
                        Logger.Info($"Crewpostor task done but kill skipped, tasks completed {Main.CrewpostorTasksDone[player.PlayerId]}, but it kills after {Options.CrewpostorKillAfterTask.GetInt()} tasks", "Crewpostor");
                    }
                    else
                    {
                        list = [.. list.OrderBy(x => Vector2.Distance(player.transform.position, x.transform.position))];
                        var target = list[0];

                        if (!target.Is(CustomRoles.Pestilence))
                        {
                            if (!Options.CrewpostorLungeKill.GetBool())
                            {
                                target.SetRealKiller(player);
                                target.RpcCheckAndMurder(target);
                                player.RpcGuardAndKill();
                                Logger.Info("No lunge mode kill", "Crewpostor");
                            }
                            else
                            {
                                target.SetRealKiller(player);
                                player.RpcMurderPlayerV3(target);
                                player.RpcGuardAndKill();
                                Logger.Info("lunge mode kill", "Crewpostor");
                            }
                            Logger.Info($"Crewpostor completed task to kill：{player.GetNameWithRole().RemoveHtmlTags()} => {target.GetNameWithRole().RemoveHtmlTags()}", "Crewpostor");
                        }
                        else
                        {
                            player.SetRealKiller(target);
                            target.RpcMurderPlayerV3(player);
                            player.RpcGuardAndKill();
                            Logger.Info($"Crewpostor tried to kill pestilence (reflected back)：{target.GetNameWithRole().RemoveHtmlTags()} => {player.GetNameWithRole().RemoveHtmlTags()}", "Pestilence Reflect");
                        }
                    }
                    break;
            }


            // Add-Ons
            if (playerSubRoles.Count > 0)
            {
                foreach (var subRole in playerSubRoles)
                {
                    switch (subRole)
                    {
                        case CustomRoles.Unlucky when player.IsAlive():
                            Unlucky.SuicideRand(player);
                            break;
                        
                        case CustomRoles.Tired when player.IsAlive():
                             Tired.AfterActionTasks(player);
                            break;

                        case CustomRoles.Bloodlust when player.IsAlive() && !Alchemist.BloodlustList.ContainsKey(player.PlayerId):
                            Alchemist.BloodlustList[player.PlayerId] = player.PlayerId;
                            player.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Bloodlust), string.Format(Translator.GetString("BloodlustAdded"))));
                            break;

                        case CustomRoles.Ghoul when (CompletedTasksCount + 1) >= AllTasksCount:
                            Ghoul.OnTaskComplete(player);
                            break;
                    }
                }
            }
        }

        // if it's clear, it doesn't count
        if (CompletedTasksCount >= AllTasksCount) return;

        //Solsticer task state is updated by host rpc
        if (player.Is(CustomRoles.Solsticer) && !AmongUsClient.Instance.AmHost) return;


        CompletedTasksCount++;

        // Display only up to the adjusted task amount
        CompletedTasksCount = Math.Min(AllTasksCount, CompletedTasksCount);
        Logger.Info($"{player.GetNameWithRole().RemoveHtmlTags()}: TaskCounts = {CompletedTasksCount}/{AllTasksCount}", "TaskState.Update");

    }
}
public class PlayerVersion(Version ver, string tag_str, string forkId)
{
    public readonly Version version = ver;
    public readonly string tag = tag_str;
    public readonly string forkId = forkId;
#pragma warning disable CA1041 // Provide ObsoleteAttribute message
    [Obsolete] public PlayerVersion(string ver, string tag_str) : this(Version.Parse(ver), tag_str, string.Empty) { }
    [Obsolete] public PlayerVersion(Version ver, string tag_str) : this(ver, tag_str, string.Empty) { }
#pragma warning restore CA1041
    public PlayerVersion(string ver, string tag_str, string forkId) : this(Version.Parse(ver), tag_str, forkId) { }

    public bool IsEqual(PlayerVersion pv)
    {
        return pv.version == version && pv.tag == tag;
    }
}
public static class GameStates
{
    public static bool InGame = false;
    public static bool AlreadyDied = false;
    /**********Check Game Status***********/
    public static bool IsModHost => Main.playerVersion.ContainsKey(AmongUsClient.Instance.HostId);
    public static bool IsNormalGame => GameOptionsManager.Instance.CurrentGameOptions.GameMode == GameModes.Normal;
    public static bool IsHideNSeek => GameOptionsManager.Instance.CurrentGameOptions.GameMode == GameModes.HideNSeek;
    public static bool SkeldIsActive => (MapNames)GameOptionsManager.Instance.CurrentGameOptions.MapId == MapNames.Skeld;
    public static bool MiraHQIsActive => (MapNames)GameOptionsManager.Instance.CurrentGameOptions.MapId == MapNames.Mira;
    public static bool PolusIsActive => (MapNames)GameOptionsManager.Instance.CurrentGameOptions.MapId == MapNames.Polus;
    public static bool DleksIsActive => (MapNames)GameOptionsManager.Instance.CurrentGameOptions.MapId == MapNames.Dleks;
    public static bool AirshipIsActive => (MapNames)GameOptionsManager.Instance.CurrentGameOptions.MapId == MapNames.Airship;
    public static bool FungleIsActive => (MapNames)GameOptionsManager.Instance.CurrentGameOptions.MapId == MapNames.Fungle;
    public static bool IsLobby => AmongUsClient.Instance.GameState == AmongUsClient.GameStates.Joined;
    public static bool IsInGame => InGame;
    public static bool IsEnded => AmongUsClient.Instance.GameState == AmongUsClient.GameStates.Ended;
    public static bool IsNotJoined => AmongUsClient.Instance.GameState == AmongUsClient.GameStates.NotJoined;
    public static bool IsOnlineGame => AmongUsClient.Instance.NetworkMode == NetworkModes.OnlineGame;
    public static bool IsLocalGame => AmongUsClient.Instance.NetworkMode == NetworkModes.LocalGame;
    public static bool IsFreePlay => AmongUsClient.Instance.NetworkMode == NetworkModes.FreePlay;
    public static bool IsInTask => InGame && !MeetingHud.Instance;
    public static bool IsMeeting => InGame && MeetingHud.Instance;
    public static bool IsVoting => IsMeeting && MeetingHud.Instance.state is MeetingHud.VoteStates.Voted or MeetingHud.VoteStates.NotVoted;
    public static bool IsProceeding => IsMeeting && MeetingHud.Instance.state is MeetingHud.VoteStates.Proceeding;
    public static bool IsExilling => ExileController.Instance != null && !(AirshipIsActive && SpawnInMinigame.Instance.isActiveAndEnabled);
    public static bool IsCountDown => GameStartManager.InstanceExists && GameStartManager.Instance.startState == GameStartManager.StartingStates.Countdown;
    /**********TOP ZOOM.cs***********/
    public static bool IsShip => ShipStatus.Instance != null;
    public static bool IsCanMove => PlayerControl.LocalPlayer?.CanMove is true;
    public static bool IsDead => PlayerControl.LocalPlayer?.Data?.IsDead is true;
}
public static class MeetingStates
{
    public static DeadBody[] DeadBodies = null;
    public static GameData.PlayerInfo ReportTarget = null;
    public static bool IsEmergencyMeeting => ReportTarget == null;
    public static bool IsExistDeadBody => DeadBodies.Length > 0;
    public static bool MeetingCalled = false;
    public static bool FirstMeeting = true;
}
