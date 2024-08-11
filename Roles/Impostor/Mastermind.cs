using TOHE.Roles.Crewmate;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.Impostor;

internal class Mastermind : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 4100;

    private static readonly HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Any();
    
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorConcealing;
    //==================================================================\\

    private static OptionItem KillCooldown;
    private static OptionItem TimeLimit;
    private static OptionItem Delay;

    private static readonly Dictionary<byte, long> ManipulatedPlayers = [];
    private static readonly Dictionary<byte, long> ManipulateDelays = [];
    private static readonly Dictionary<byte, float> TempKCDs = [];

    private static float ManipulateCD;

    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Mastermind, 1);
        KillCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 25f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Mastermind])
            .SetValueFormat(OptionFormat.Seconds);
        TimeLimit = FloatOptionItem.Create(Id + 12, "MastermindTimeLimit", new(1f, 60f, 1f), 20f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Mastermind])
            .SetValueFormat(OptionFormat.Seconds);
        Delay = FloatOptionItem.Create(Id + 13, "MastermindDelay", new(0f, 30f, 1f), 7f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Mastermind])
            .SetValueFormat(OptionFormat.Seconds);
    }

    public override void Init()
    {
        playerIdList.Clear();
        ManipulatedPlayers.Clear();
        ManipulateDelays.Clear();
        TempKCDs.Clear();
    }

    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        ManipulateCD = KillCooldown.GetFloat() + (TimeLimit.GetFloat() / 2) + (Delay.GetFloat() / 2);

        // Double Trigger
        var pc = GetPlayerById(playerId);
        pc.AddDoubleTrigger();

    }

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();

    public static bool PlayerIsManipulated(PlayerControl pc) => ManipulatedPlayers.ContainsKey(pc.PlayerId);

    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (killer == null || target == null) return false;

        return killer.CheckDoubleTrigger(target, () =>
        {
            killer.SetKillCooldown(time: ManipulateCD);
            if (target.HasKillButton() || CopyCat.playerIdList.Contains(target.PlayerId) || Main.TasklessCrewmate.Contains(target.PlayerId))
            {
                ManipulateDelays.TryAdd(target.PlayerId, GetTimeStamp());
                NotifyRoles(SpecifySeer: killer, SpecifyTarget: target);
                NotifyRoles(SpecifySeer: target, SpecifyTarget: killer);
            }
        });
    }

    public override void OnFixedUpdateLowLoad(PlayerControl mastermind)
    {
        if (ManipulatedPlayers.Count == 0 && ManipulateDelays.Count == 0) return;

        foreach (var x in ManipulateDelays)
        {
            var pc = GetPlayerById(x.Key);

            if (!pc.IsAlive())
            {
                ManipulateDelays.Remove(x.Key);
                continue;
            }
            if (x.Value + Delay.GetInt() < GetTimeStamp())
            {
                ManipulateDelays.Remove(x.Key);
                ManipulatedPlayers.TryAdd(x.Key, GetTimeStamp());

                TempKCDs.TryAdd(pc.PlayerId, pc.killTimer);
                pc.SetKillCooldown(time: 1f);

                NotifyRoles(SpecifySeer: mastermind);
            }
        }

        foreach (var x in ManipulatedPlayers)
        {
            var player = GetPlayerById(x.Key);

            if (!player.IsAlive())
            {
                ManipulatedPlayers.Remove(x.Key);
                TempKCDs.Remove(x.Key);
                continue;
            }
            if (x.Value + TimeLimit.GetInt() < GetTimeStamp())
            {
                ManipulatedPlayers.Remove(x.Key);
                TempKCDs.Remove(x.Key);

                player.SetDeathReason(PlayerState.DeathReason.Suicide);
                player.RpcMurderPlayer(player);
                player.SetRealKiller(mastermind);
                RPC.PlaySoundRPC(mastermind.PlayerId, Sounds.KillSound);
            }

            var time = TimeLimit.GetInt() - (GetTimeStamp() - x.Value);

            player.Notify(string.Format(GetString("ManipulateNotify"), time), 1.1f);
        }
    }

    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        foreach (var x in ManipulatedPlayers)
        {
            var pc = GetPlayerById(x.Key);
            if (pc.IsAlive())
            {
                pc.SetDeathReason(PlayerState.DeathReason.Suicide);
                pc.RpcMurderPlayer(pc);
                pc.SetRealKiller(GetPlayerById(playerIdList.First()));
            }
        }
        ManipulateDelays.Clear();
        ManipulatedPlayers.Clear();
        TempKCDs.Clear();
    }

    public override bool CheckMurderOnOthersTarget(PlayerControl killer, PlayerControl target)
    {
        if (killer == null || target == null) return true;
        if (!PlayerIsManipulated(killer)) return false;

        ManipulatedPlayers.Remove(killer.PlayerId);

        var mastermind = GetPlayerById(playerIdList.First());
        mastermind?.Notify(string.Format(GetString("ManipulatedKilled"), killer.GetRealName()), 4f);
        mastermind?.SetKillCooldown(time: KillCooldown.GetFloat());
        killer.Notify(GetString("SurvivedManipulation"));

        if (target.Is(CustomRoles.Pestilence) || target.Is(CustomRoles.Mastermind))
        {
            target.RpcMurderPlayer(killer);
            TempKCDs.Remove(killer.PlayerId);
            return true;
        }

        killer.RpcMurderPlayer(target);

        _ = new LateTask(() =>
        {
            killer.SetKillCooldown(time: TempKCDs[killer.PlayerId] + Main.AllPlayerKillCooldown[killer.PlayerId]);
            TempKCDs.Remove(killer.PlayerId);
        }, 0.1f, "Set KCD for Manipulated Kill");

        return true;
    }

    public override string PlayerKnowTargetColor(PlayerControl seer, PlayerControl target)
    {
        if (ManipulateDelays.ContainsKey(target.PlayerId))
            return "#00ffa5";

        if (PlayerIsManipulated(target))
            return Main.roleColors[CustomRoles.Arsonist];

        return string.Empty;
    }
}
