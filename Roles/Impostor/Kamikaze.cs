using Hazel;
using TOHE.Modules;
using TOHE.Modules.Rpc;
using TOHE.Roles.Core;
using TOHE.Roles.Double;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Impostor;

internal class Kamikaze : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Kamikaze;
    private const int Id = 26900;
    public static readonly HashSet<byte> Playerids = [];
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Kamikaze);
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorSupport;
    //==================================================================\\

    private static OptionItem KillCooldown;
    private static OptionItem OptMaxMarked;
    private static OptionItem CanKillTNA;

    private readonly Dictionary<byte, HashSet<byte>> KamikazedList = [];

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Kamikaze);
        KillCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 25f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Kamikaze])
            .SetValueFormat(OptionFormat.Seconds);
        OptMaxMarked = IntegerOptionItem.Create(Id + 11, "KamikazeMaxMarked", new(1, 14, 1), 14, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Kamikaze])
           .SetValueFormat(OptionFormat.Times);
        CanKillTNA = BooleanOptionItem.Create(Id + 12, "CanKillTNA", false, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Kamikaze]);

    }

    public override void Init()
    {
        Playerids.Clear();
    }

    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(OptMaxMarked.GetInt());

        if (!Playerids.Contains(playerId))
            Playerids.Add(playerId);

        // Double Trigger
        var pc = Utils.GetPlayerById(playerId);
        pc.AddDoubleTrigger();

        if (AmongUsClient.Instance.AmHost)
        {
            CustomRoleManager.CheckDeadBodyOthers.Add(AfterPlayerDeathTasks);
        }
    }

    public override void Remove(byte playerId)
    {
        Playerids.Remove(playerId);

        if (!Playerids.Any())
            CustomRoleManager.CheckDeadBodyOthers.Remove(AfterPlayerDeathTasks);
    }

    private void AfterPlayerDeathTasks(PlayerControl killer, PlayerControl target, bool inMeeting)
    {
        if (target == null || !target.Is(CustomRoles.Kamikaze) || target.IsAlive() || target.IsDisconnected()) return;

        var kId = target.PlayerId;
        var kList = KamikazedList[kId];

        if (!inMeeting)
        {
            foreach (var BABUSHKA in kList)
            {
                var pc = Utils.GetPlayerById(BABUSHKA);
                if (!pc.IsAlive()) continue;
                if (pc.IsTransformedNeutralApocalypse() && !CanKillTNA.GetBool()) continue;

                pc.SetDeathReason(PlayerState.DeathReason.Targeted);
                if (!inMeeting)
                {
                    pc.RpcMurderPlayer(pc);
                }
                else
                {
                    pc.RpcExileV2();
                    Main.PlayerStates[pc.PlayerId].SetDead();
                    pc.Data.IsDead = true;
                }
                pc.SetRealKiller(_Player);
            }
        }
        else
        {
            var deathList = new List<byte>();;
            foreach (var pc in Main.AllAlivePlayerControls)
            {
                if (kList.Contains(pc.PlayerId))
                {
                    if (!Main.AfterMeetingDeathPlayers.ContainsKey(pc.PlayerId))
                    {
                        pc.SetRealKiller(target);
                        deathList.Add(pc.PlayerId);
                    }
                }
            }
            CheckForEndVotingPatch.TryAddAfterMeetingDeathPlayers(PlayerState.DeathReason.Targeted, [.. deathList]);
        }
        kList.Clear();
        SendRPC();
    }

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();

    public override string GetMark(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
        => (KamikazedList.TryGetValue(seer.PlayerId, out var kList) && kList.Contains(seen.PlayerId)) ? Utils.ColorString(Utils.GetRoleColor(CustomRoles.Kamikaze), "∇") : string.Empty;

    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (target.Is(CustomRoles.NiceMini) && Mini.Age < 18)
        {
            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Kamikaze), GetString("KamikazeHostage")));
            return false;
        }

        return killer.CheckDoubleTrigger(target, () =>
        {
            if (killer.GetAbilityUseLimit() >= 1 && !KamikazedList[killer.PlayerId].Contains(target.PlayerId))
            {
                KamikazedList[killer.PlayerId].Add(target.PlayerId);
                killer.RpcGuardAndKill(killer);
                killer.SetKillCooldown(KillCooldown.GetFloat());
                Utils.NotifyRoles(SpecifySeer: killer);
                killer.RpcRemoveAbilityUse();
            }
            else
            {
                killer.RpcMurderPlayer(target);
            }
        });

    }

    private void SendRPC()
    {
        var writer = MessageWriter.Get(SendOption.Reliable);
        writer.WritePacked(KamikazedList.Count);
        foreach (var kList in KamikazedList)
        {
            writer.Write(kList.Key);

            writer.WritePacked(kList.Value.Count);

            foreach (var target in kList.Value)
            {
                writer.Write(target);
            }
        }
        RpcUtils.LateBroadcastReliableMessage(new RpcSyncRoleSkill(PlayerControl.LocalPlayer.NetId, _Player.NetId, writer));
    }

    public override void ReceiveRPC(MessageReader reader, PlayerControl pc)
    {
        var count = reader.ReadPackedInt32();
        KamikazedList.Clear();
        for (int i = 0; i < count; i++)
        {
            var kId = reader.ReadByte();
            HashSet<byte> targets = [];
            var tCount = reader.ReadPackedInt32();

            for (int j = 0; j < tCount; j++)
            {
                targets.Add(reader.ReadByte());
            }

            KamikazedList.Add(kId, targets);
        }
    }
}

