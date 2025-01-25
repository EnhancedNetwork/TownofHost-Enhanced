using AmongUs.GameOptions;
using Hazel;
using InnerNet;
using TOHE.Roles.Core;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;


namespace TOHE.Roles.Coven;

internal class HexMaster : CovenManager
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.HexMaster;
    private const int Id = 16400;
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CovenKilling;
    //==================================================================\\

    private static OptionItem HexesLookLikeSpells;
    private static OptionItem HexCooldown;
    private static OptionItem CovenCanGetMovingHex;
    private static OptionItem MovingHexPassCooldown;

    public static byte CurrentHexedPlayer = byte.MaxValue;
    public static byte LastHexedPlayer = byte.MaxValue;
    public static bool HasHexed = false;
    public static long? CurrentHexedPlayerTime = new();
    public static long? HexedTime = new();

    private static readonly Dictionary<byte, HashSet<byte>> HexedPlayer = [];

    private static readonly Color RoleColorHex = GetRoleColor(CustomRoles.HexMaster);
    private static readonly Color RoleColorSpell = GetRoleColor(CustomRoles.Impostor);

    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.CovenRoles, CustomRoles.HexMaster, 1, zeroOne: false);
        HexCooldown = FloatOptionItem.Create(Id + 13, "HexMasterHexCooldown", new(0f, 180f, 2.5f), 30f, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.HexMaster])
            .SetValueFormat(OptionFormat.Seconds);
        MovingHexPassCooldown = FloatOptionItem.Create(Id + 15, "HexMasterMovingHexCooldown", new(0f, 5f, 0.25f), 1f, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.HexMaster])
            .SetValueFormat(OptionFormat.Seconds);
        CovenCanGetMovingHex = BooleanOptionItem.Create(Id + 14, "HexMasterCovenCanGetMovingHex", false, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.HexMaster]);
        HexesLookLikeSpells = BooleanOptionItem.Create(Id + 11, "HexesLookLikeSpells", false, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.HexMaster]);
    }
    public override void Init()
    {
        HexedPlayer.Clear();
        CurrentHexedPlayer = byte.MaxValue;
        LastHexedPlayer = byte.MaxValue;
        HasHexed = false;
        CurrentHexedPlayerTime = new();
    }
    public override void Add(byte playerId)
    {
        HexedPlayer[playerId] = [];

        var pc = playerId.GetPlayer();
        pc.AddDoubleTrigger();
        CustomRoleManager.OnFixedUpdateOthers.Add(OnFixedUpdateOthers);
    }
    public override void Remove(byte playerId)
    {
        if (HexedPlayer.ContainsKey(playerId))
        {
            HexedPlayer[playerId].Clear();
            SendRPC(playerId);
        }
    }
    private static void SendRPC(byte hexId, byte target = 255)
    {
        if (!AmongUsClient.Instance.AmHost) return;

        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, -1);
        writer.WriteNetObject(hexId.GetPlayer());
        writer.Write(hexId);
        writer.Write(target);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public override void ReceiveRPC(MessageReader reader, PlayerControl pc)
    {
        var hexmaster = reader.ReadByte();
        var hexedId = reader.ReadByte();
        if (hexedId == 255)
        {
            HexedPlayer[hexmaster].Clear();
        }
        else
        {
            HexedPlayer[hexmaster].Add(hexedId);
        }
    }

    public override bool CanUseKillButton(PlayerControl pc) => true;
    public override void SetKillCooldown(byte id) => HexCooldown.GetFloat();
    private static bool IsHexed(byte target)
    {
        foreach (var hexmaster in HexedPlayer.Keys)
        {
            if (HexedPlayer[hexmaster].Contains(target)) return true;
        }
        return false;
    }
    private static void SetHexed(PlayerControl killer, PlayerControl target)
    {
        if (!IsHexed(target.PlayerId))
        {
            HexedPlayer[killer.PlayerId].Add(target.PlayerId);
            SendRPC(killer.PlayerId, target.PlayerId);

            killer.ResetKillCooldown();
            killer.SetKillCooldown();
        }
    }
    private static void PassHex(PlayerControl player, PlayerControl target, long nowTime)
    {
        if (!HasHexed || !target.IsAlive()) return;
        if (nowTime - CurrentHexedPlayerTime < MovingHexPassCooldown.GetFloat()) return;
        if (target.PlayerId == LastHexedPlayer) return;
        if (!CovenCanGetMovingHex.GetBool() && target.GetCustomRole().IsCovenTeam()) return;

        if (target.Is(CustomRoles.Pestilence))
        {
            target.RpcMurderPlayer(player);
            ResetHex();
            return;
        }

        LastHexedPlayer = CurrentHexedPlayer;
        CurrentHexedPlayer = target.PlayerId;
        CurrentHexedPlayerTime = nowTime;
        MarkEveryoneDirtySettings();

        Logger.Msg($"{player.GetNameWithRole()} passed hex to {target.GetNameWithRole()}", "Hex Master Pass");
    }
    private static void ResetHex()
    {
        CurrentHexedPlayer = byte.MaxValue;
        CurrentHexedPlayerTime = new();
        LastHexedPlayer = byte.MaxValue;
        HasHexed = false;
    }
    public override void OnReportDeadBody(PlayerControl reported, NetworkedPlayerInfo agitatergoatedrole)
    {
        if (CurrentHexedPlayer == byte.MaxValue) return;
        var target = CurrentHexedPlayer.GetPlayer();
        var killer = _Player;
        if (target == null || killer == null) return;

        HexedPlayer[killer.PlayerId].Add(target.PlayerId);
        SendRPC(killer.PlayerId, target.PlayerId);
        ResetHex();
        Logger.Info($"Passing hex ended, {target.GetRealName()} ended with hex on report", "Hex Master");
    }
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (target.IsTransformedNeutralApocalypse()) return false;

        if (!HasNecronomicon(killer))
        {
            if (!target.GetCustomRole().IsCovenTeam()) SetHexed(killer, target);
            return false;
        }
        if (killer.CheckDoubleTrigger(target, () => { SetHexedNecronomicon(killer, target); }))
        {
            if (HasNecronomicon(killer))
            {
                if (target.GetCustomRole().IsCovenTeam())
                {
                    killer.Notify(GetString("CovenDontKillOtherCoven"));
                    return false;
                }
                else return true;
            }
        }
        return false;
    }
    private static void SetHexedNecronomicon(PlayerControl killer, PlayerControl target)
    {
        if (target.GetCustomRole().IsCovenTeam())
        {
            killer.Notify(GetString("CovenDontKillOtherCoven"));
            return;
        }

        CurrentHexedPlayer = target.PlayerId;
        LastHexedPlayer = killer.PlayerId;
        CurrentHexedPlayerTime = GetTimeStamp();
        killer.RpcGuardAndKill(killer);
        killer.Notify(GetString("HexMasterPassNotify"));
        HasHexed = true;
        killer.ResetKillCooldown();
        killer.SetKillCooldown();
    }
    private void OnFixedUpdateOthers(PlayerControl player, bool lowLoad, long nowTime)
    {
        if (lowLoad || !HasHexed || CurrentHexedPlayer != player.PlayerId) return;

        if (!player.IsAlive())
        {
            ResetHex();
        }
        else
        {
            var playerPos = player.GetCustomPosition();
            Dictionary<byte, float> targetDistance = [];
            float dis;

            foreach (var target in Main.AllAlivePlayerControls)
            {
                if (target.PlayerId != player.PlayerId && target.PlayerId != LastHexedPlayer)
                {
                    dis = GetDistance(playerPos, target.transform.position);
                    targetDistance.Add(target.PlayerId, dis);
                }
            }

            if (targetDistance.Any())
            {
                var min = targetDistance.OrderBy(c => c.Value).FirstOrDefault();
                var target = min.Key.GetPlayer();
                var KillRange = GameOptionsData.KillDistances[Mathf.Clamp(GameOptionsManager.Instance.currentNormalGameOptions.KillDistance, 0, 2)];
                if (min.Value <= KillRange && !player.inVent && !player.inMovingPlat && !target.inVent && !target.inMovingPlat && player.RpcCheckAndMurder(target, true))
                {
                    PassHex(player, target, nowTime);
                }
            }
        }
    }
    public override void OnCheckForEndVoting(PlayerState.DeathReason deathReason, params byte[] exileIds)
    {
        foreach (var id in exileIds)
        {
            if (HexedPlayer.ContainsKey(id))
                HexedPlayer[id].Clear();
        }
        var hexedIdList = new List<byte>();
        foreach (var pc in Main.AllAlivePlayerControls)
        {
            var dic = HexedPlayer.Where(x => x.Value.Contains(pc.PlayerId));
            if (!dic.Any()) continue;
            var whichId = dic.FirstOrDefault().Key;
            var hexmaster = whichId.GetPlayer();
            if (hexmaster != null && hexmaster.IsAlive())
            {
                if (!Main.AfterMeetingDeathPlayers.ContainsKey(pc.PlayerId))
                {
                    pc.SetRealKiller(hexmaster);
                    hexedIdList.Add(pc.PlayerId);
                }
            }
            else
            {
                Main.AfterMeetingDeathPlayers.Remove(pc.PlayerId);
            }
        }
        CheckForEndVotingPatch.TryAddAfterMeetingDeathPlayers(PlayerState.DeathReason.Hex, [.. hexedIdList]);
        RemoveHexedPlayer();
    }
    public override void AfterMeetingTasks()
    {
        RemoveHexedPlayer();
    }
    public override void OnPlayerExiled(PlayerControl player, NetworkedPlayerInfo exiled)
    {
        RemoveHexedPlayer();
    }
    private static void RemoveHexedPlayer()
    {
        foreach (var hexmaster in HexedPlayer.Keys)
        {
            HexedPlayer[hexmaster].Clear();
            SendRPC(hexmaster);
        }
    }
    public override string GetMarkOthers(PlayerControl seer, PlayerControl target, bool isForMeeting = false)
    {
        if (isForMeeting && IsHexed(target.PlayerId))
        {
            if (!HexesLookLikeSpells.GetBool())
            {
                return ColorString(RoleColorHex, "乂");
            }
            else
            {
                return ColorString(RoleColorSpell, "†");
            }
        }
        return string.Empty;
    }

    public override void SetAbilityButtonText(HudManager hud, byte playerid) => hud.KillButton.OverrideText($"{GetString("HexButtonText")}");
}
