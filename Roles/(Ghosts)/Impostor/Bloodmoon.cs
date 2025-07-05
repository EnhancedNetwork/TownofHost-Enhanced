using AmongUs.GameOptions;
using Hazel;
using TOHE.Modules;
using TOHE.Modules.Rpc;
using TOHE.Roles.Core;
using TOHE.Roles.Double;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles._Ghosts_.Impostor;

internal class Bloodmoon : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Bloodmoon;
    private const int Id = 28100;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Bloodmoon);
    public override CustomRoles ThisRoleBase => CustomRoles.GuardianAngel;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorGhosts;
    //==================================================================\\

    public static OptionItem KillCooldown;
    public static OptionItem CanKillNum;
    private static OptionItem TimeTilDeath;

    private readonly Dictionary<byte, int> PlayerDie = [];
    private readonly Dictionary<byte, long> LastTime = [];

    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Bloodmoon);
        KillCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.KillCooldown, new(2.5f, 120f, 2.5f), 25f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Bloodmoon])
        .SetValueFormat(OptionFormat.Seconds);
        CanKillNum = IntegerOptionItem.Create(Id + 11, "BloodMoonCanKillNum", new(1, 15, 1), 3, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Bloodmoon])
            .SetValueFormat(OptionFormat.Players);
        TimeTilDeath = IntegerOptionItem.Create(Id + 12, "BloodMoonTimeTilDie", new(1, 120, 1), 60, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Bloodmoon])
        .SetValueFormat(OptionFormat.Seconds);
    }
    public override void Init()
    {
        PlayerDie.Clear();
        LastTime.Clear();
    }
    public override void Add(byte playerId)
    {
        playerId.SetAbilityUseLimit(CanKillNum.GetInt());
        CustomRoleManager.OnFixedUpdateOthers.Add(OnFixedUpdateOther);
        CustomRoleManager.CheckDeadBodyOthers.Add(CheckDeadBody);
    }
    // EAC bans players when GA uses sabotage
    public override bool CanUseSabotage(PlayerControl pc) => false;
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.GuardianAngelCooldown = KillCooldown.GetFloat();
        AURoleOptions.ProtectionDurationSeconds = 0f;
    }
    private void SendRPC(byte targetId, bool add)
    {
        var writer = MessageWriter.Get(SendOption.Reliable);
        writer.Write(add);
        writer.Write(targetId);
        RpcUtils.LateBroadcastReliableMessage(new RpcSyncRoleSkill(PlayerControl.LocalPlayer.NetId, _Player.NetId, writer));
    }
    public override void ReceiveRPC(MessageReader reader, PlayerControl pc)
    {
        bool add = reader.ReadBoolean();
        byte targetId = reader.ReadByte();

        if (add)
            PlayerDie.Add(targetId, TimeTilDeath.GetInt());
        else
            PlayerDie.Remove(targetId);
    }
    public override bool OnCheckProtect(PlayerControl killer, PlayerControl target)
    {
        if (target.Is(CustomRoles.NiceMini) && Mini.Age < 18)
        {
            killer.Notify(ColorString(GetRoleColor(CustomRoles.Gangster), GetString("CantBlood")));
            return true;
        }

        if (killer.GetAbilityUseLimit() > 0
            && !target.Is(CustomRoles.Jinx)
            && !target.Is(CustomRoles.CursedWolf)
            && !target.IsNeutralApocalypse()
            && killer.RpcCheckAndMurder(target, true)
            && !PlayerDie.ContainsKey(target.PlayerId))
        {
            RPC.PlaySoundRPC(Sounds.SabotageSound, target.PlayerId);
            PlayerDie.Add(target.PlayerId, TimeTilDeath.GetInt());
            LastTime.Add(target.PlayerId, GetTimeStamp());
            killer.RpcResetAbilityCooldown();
            killer.RpcRemoveAbilityUse();
            SendRPC(target.PlayerId, true);
        }
        return false;
    }

    private void OnFixedUpdateOther(PlayerControl player, bool lowLoad, long nowTime)
    {
        if (lowLoad || _Player == null) return;

        var playerid = player.PlayerId;
        if (LastTime.TryGetValue(playerid, out var lastTime) && lastTime + 1 <= nowTime)
        {
            LastTime[playerid] = nowTime;
            PlayerDie[playerid]--;
            if (PlayerDie[playerid] <= 0)
            {
                PlayerDie.Remove(playerid);
                LastTime.Remove(playerid);
                RPC.PlaySoundRPC(Sounds.KillSound, _Player.PlayerId);
                player.SetDeathReason(PlayerState.DeathReason.BloodLet);
                player.RpcMurderPlayer(player);
                player.SetRealKiller(_Player);
            }
        }
    }
    private void CheckDeadBody(PlayerControl killer, PlayerControl target, bool inMeeting)
    {
        var DeadPlayerId = target.PlayerId;

        if (LastTime.ContainsKey(DeadPlayerId))
            LastTime.Remove(DeadPlayerId);

        if (PlayerDie.ContainsKey(DeadPlayerId))
        {
            PlayerDie.Remove(DeadPlayerId);
            SendRPC(DeadPlayerId, false);
        }
    }

    public override string GetLowerTextOthers(PlayerControl seer, PlayerControl player = null, bool isForMeeting = false, bool isForHud = false)
        => !isForMeeting && PlayerDie.TryGetValue(player.PlayerId, out var DeathTimer) ? ColorString(GetRoleColor(CustomRoles.Bloodmoon), GetString("DeathTimer").Replace("{DeathTimer}", DeathTimer.ToString())) : string.Empty;
}
