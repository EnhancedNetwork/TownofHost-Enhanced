using Il2CppInterop.Runtime.InteropTypes.Arrays;
using AmongUs.GameOptions;
using System;
using UnityEngine;
using TOHE.Roles.Core;
using TOHE.Roles.Impostor;
using TOHE.Roles.Neutral;
using TOHE.Roles.AddOns.Common;
using TOHE.Roles.AddOns.Impostor;
using static TOHE.Utils;

namespace TOHE;

public class PlayerState(byte playerId)
{
    public readonly byte PlayerId = playerId;
    public RoleBase RoleClass = new DefaultSetup();
    public CustomRoles MainRole = CustomRoles.NotAssigned;
    public List<CustomRoles> SubRoles = [];
    public CountTypes countTypes = CountTypes.OutOfGame;
    public bool IsDead { get; set; } = false;
    public bool Disconnected { get; set; } = false;
#pragma warning disable IDE1006 // Naming Styles
    public DeathReason deathReason { get; set; } = DeathReason.etc;
#pragma warning restore IDE1006
    public TaskState taskState = new();
    public bool IsBlackOut { get; set; } = false;
    public (DateTime, byte) RealKiller = (DateTime.MinValue, byte.MaxValue);
    public PlainShipRoom LastRoom = null;
    public bool HasSpawned { get; set; } = false;
    public Dictionary<byte, string> TargetColorData = [];
    public NetworkedPlayerInfo.PlayerOutfit NormalOutfit;

    public void SetMainRole(CustomRoles role)
    {
        MainRole = role;
        countTypes = role.GetCountTypes();
        RoleClass = role.CreateRoleClass();

        var pc = GetPlayerById(PlayerId);

        if (role == CustomRoles.Opportunist)
        {
            if (AmongUsClient.Instance.AmHost)
            {
                if (!pc.HasImpKillButton(considerVanillaShift: true))
                {
                    var taskstate = pc.GetPlayerTaskState();
                    if (taskstate != null)
                    {
                        pc.Data.RpcSetTasks(new Il2CppStructArray<byte>(0));
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
            countTypes = Cultist.CharmedCountMode.GetInt() switch
            {
                0 => CountTypes.OutOfGame,
                1 => CountTypes.Cultist,
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

                if (sync) MarkEveryoneDirtySettings();
            }
        }

        if (!SubRoles.Contains(role))
            SubRoles.Add(role);
        if (role.IsConverted())
        {
            SubRoles.RemoveAll(AddON => AddON != role && AddON.IsConverted());
            SubRoles.Remove(CustomRoles.Rascal);
            SubRoles.Remove(CustomRoles.Loyal);
            SubRoles.Remove(CustomRoles.Admired);
        }

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
                break;

            case CustomRoles.Charmed:
                countTypes = Cultist.CharmedCountMode.GetInt() switch
                {
                    0 => CountTypes.OutOfGame,
                    1 => CountTypes.Cultist,
                    2 => countTypes,
                    _ => throw new NotImplementedException()
                };
                break;

            case CustomRoles.Recruit:
                countTypes = Jackal.SidekickCountMode.GetInt() switch
                {
                    0 => CountTypes.Jackal,
                    1 => CountTypes.OutOfGame,
                    2 => countTypes,
                    _ => throw new NotImplementedException()
                };
                break;

            case CustomRoles.Infected:
                countTypes = CountTypes.Infectious;
                break;

            case CustomRoles.Contagious:
                countTypes = Virus.ContagiousCountMode.GetInt() switch
                {
                    0 => CountTypes.OutOfGame,
                    1 => CountTypes.Virus,
                    2 => countTypes,
                    _ => throw new NotImplementedException()
                };
                break;

            // This exist as it would be possible for them to exist on the same player via Bandit
            // But since Bandit can't vent without Nimble, allowing them to have Circumvent is pointless
            case CustomRoles.Nimble:
                SubRoles.Remove(CustomRoles.Circumvent);
                break;

            case CustomRoles.Admired:
                countTypes = CountTypes.Crew;
                SubRoles.RemoveAll(AddON => AddON != role && AddON.IsConverted());
                SubRoles.Remove(CustomRoles.Rascal);
                SubRoles.Remove(CustomRoles.Loyal);
                break;

            case CustomRoles.Soulless:
                countTypes = CountTypes.OutOfGame;
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
        var caller = new System.Diagnostics.StackFrame(1, false);
        var callerMethod = caller.GetMethod();
        string callerMethodName = callerMethod.Name;
        string callerClassName = callerMethod.DeclaringType.FullName;
        Logger.Msg($"Player {PlayerId} was dead, activated from: {callerClassName}.{callerMethodName}", "PlayerState.SetDead()");

        IsDead = true;
        if (AmongUsClient.Instance.AmHost)
        {
            RPC.SendDeathReason(PlayerId, deathReason);
            if (GameStates.IsMeeting && MeetingHud.Instance.state is MeetingHud.VoteStates.Discussion or MeetingHud.VoteStates.NotVoted or MeetingHud.VoteStates.Voted)
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
        Slice,
        BloodLet,
        WrongAnswer,

        //Please add all new roles with deathreason & new deathreason in Utils.DeathReasonIsEnable();
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

    public static string GetTaskState()
    {
        var playersWithTasks = Main.PlayerStates.Where(a => a.Value.TaskState.hasTasks).ToArray();
        if (playersWithTasks.Length == 0)
        {
            return "\r\n";
        }

        var randomPlayer = playersWithTasks.RandomElement();
        var taskState = randomPlayer.Value.TaskState;

        Color TextColor;
        var TaskCompleteColor = Color.green;
        var NonCompleteColor = Color.yellow;
        var NormalColor = taskState.IsTaskFinished ? TaskCompleteColor : NonCompleteColor;

        TextColor = Camouflager.AbilityActivated || Camouflage.IsCamouflage ? Color.gray : NormalColor;
        string Completed = Camouflager.AbilityActivated || Camouflage.IsCamouflage ? "?" : $"{taskState.CompletedTasksCount}";

        return $" <size={1.5}>" + ColorString(TextColor, $"({Completed}/{taskState.AllTasksCount})") + "</size>\r\n";
    }

    public void Init(PlayerControl player)
    {
        Logger.Info($"{player.GetNameWithRole().RemoveHtmlTags()}: InitTask", "TaskState.Init");

        if (player == null || player.Data == null || player.Data.Tasks == null) return;

        if (!HasTasks(player.Data, false))
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

        // If not initialized, initialize it
        if (AllTasksCount == -1) Init(player);

        if (!hasTasks) return;

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
    public static bool IsNormalGame => GameOptionsManager.Instance.CurrentGameOptions.GameMode is GameModes.Normal or GameModes.NormalFools;
    public static bool IsHideNSeek => GameOptionsManager.Instance.CurrentGameOptions.GameMode is GameModes.HideNSeek or GameModes.SeekFools;
    public static bool SkeldIsActive => (MapNames)GameOptionsManager.Instance.CurrentGameOptions.MapId == MapNames.Skeld;
    public static bool MiraHQIsActive => (MapNames)GameOptionsManager.Instance.CurrentGameOptions.MapId == MapNames.Mira;
    public static bool PolusIsActive => (MapNames)GameOptionsManager.Instance.CurrentGameOptions.MapId == MapNames.Polus;
    public static bool DleksIsActive => (MapNames)GameOptionsManager.Instance.CurrentGameOptions.MapId == MapNames.Dleks;
    public static bool AirshipIsActive => (MapNames)GameOptionsManager.Instance.CurrentGameOptions.MapId == MapNames.Airship;
    public static bool FungleIsActive => (MapNames)GameOptionsManager.Instance.CurrentGameOptions.MapId == MapNames.Fungle;
    public static bool IsLobby => AmongUsClient.Instance.GameState == InnerNet.InnerNetClient.GameStates.Joined;
    public static bool IsInGame => InGame;
    public static bool IsEnded => AmongUsClient.Instance.GameState == InnerNet.InnerNetClient.GameStates.Ended;
    public static bool IsNotJoined => AmongUsClient.Instance.GameState == InnerNet.InnerNetClient.GameStates.NotJoined;
    public static bool IsOnlineGame => AmongUsClient.Instance.NetworkMode == NetworkModes.OnlineGame;
    public static bool IsVanillaServer
    {
        get
        {
            if (!IsOnlineGame) return false;

            const string Domain = "among.us";

            // From Reactor.gg
            return ServerManager.Instance.CurrentRegion?.TryCast<StaticHttpRegionInfo>() is { } regionInfo &&
                   regionInfo.PingServer.EndsWith(Domain, StringComparison.Ordinal) &&
                   regionInfo.Servers.All(serverInfo => serverInfo.Ip.EndsWith(Domain, StringComparison.Ordinal));
        }
    }
    public static bool IsLocalGame => AmongUsClient.Instance.NetworkMode == NetworkModes.LocalGame;
    public static bool IsFreePlay => AmongUsClient.Instance.NetworkMode == NetworkModes.FreePlay;
    public static bool IsInTask => InGame && !MeetingHud.Instance;
    public static bool IsMeeting => InGame && MeetingHud.Instance;
    public static bool IsVoting => IsMeeting && MeetingHud.Instance.state is MeetingHud.VoteStates.Voted or MeetingHud.VoteStates.NotVoted;
    public static bool IsProceeding => IsMeeting && MeetingHud.Instance.state is MeetingHud.VoteStates.Proceeding;
    public static bool IsExilling => ExileController.Instance != null && !(AirshipIsActive && Minigame.Instance != null && Minigame.Instance.isActiveAndEnabled);
    public static bool IsCountDown => GameStartManager.InstanceExists && GameStartManager.Instance.startState == GameStartManager.StartingStates.Countdown;
    /**********TOP ZOOM.cs***********/
    public static bool IsShip => ShipStatus.Instance != null;
    public static bool IsCanMove => PlayerControl.LocalPlayer?.CanMove is true;
    public static bool IsDead => PlayerControl.LocalPlayer?.Data?.IsDead is true;
}
public static class MeetingStates
{
    public static DeadBody[] DeadBodies = null;
    public static NetworkedPlayerInfo ReportTarget = null;
    public static bool IsEmergencyMeeting => ReportTarget == null;
    public static bool IsExistDeadBody => DeadBodies.Any();
    public static bool MeetingCalled = false;
    public static bool FirstMeeting = true;
}
