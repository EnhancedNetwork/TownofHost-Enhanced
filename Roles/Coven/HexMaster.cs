using Hazel;
using TOHE.Modules.Rpc;
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

    //private static OptionItem ModeSwitchAction;
    private static OptionItem HexesLookLikeSpells;
    //private static OptionItem HasImpostorVision;
    private static OptionItem HexCooldown;
    private static OptionItem CovenCanGetMovingHex;
    private static OptionItem MovingHexPassCooldown;
    private static OptionItem CanKillTNA;

    private static readonly Dictionary<byte, List<byte>> HexedPlayer = [];
    public static byte CurrentHexedPlayer = byte.MaxValue;
    public static byte LastHexedPlayer = byte.MaxValue;
    public static bool HasHexed = false;
    public static long? CurrentHexedPlayerTime = new();
    public static long? HexedTime = new();

    private static readonly Color RoleColorHex = Utils.GetRoleColor(CustomRoles.HexMaster);
    private static readonly Color RoleColorSpell = Utils.GetRoleColor(CustomRoles.Impostor);

    /*
    private enum SwitchTriggerList
    {
        TriggerKill,
        TriggerVent,
        TriggerDouble,
    };
    private static SwitchTriggerList NowSwitchTrigger;
    */

    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.CovenRoles, CustomRoles.HexMaster, 1, zeroOne: false);
        //ModeSwitchAction = StringOptionItem.Create(Id + 10, GeneralOption.ModeSwitchAction, EnumHelper.GetAllNames<SwitchTriggerList>(), 2, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.HexMaster]);
        HexCooldown = FloatOptionItem.Create(Id + 13, "HexMasterHexCooldown", new(0f, 180f, 2.5f), 30f, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.HexMaster])
            .SetValueFormat(OptionFormat.Seconds);
        MovingHexPassCooldown = FloatOptionItem.Create(Id + 15, "HexMasterMovingHexCooldown", new(0f, 5f, 0.25f), 1f, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.HexMaster])
            .SetValueFormat(OptionFormat.Seconds);
        CovenCanGetMovingHex = BooleanOptionItem.Create(Id + 14, "HexMasterCovenCanGetMovingHex", false, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.HexMaster]);
        HexesLookLikeSpells = BooleanOptionItem.Create(Id + 11, "HexesLookLikeSpells", false, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.HexMaster]);
        //HasImpostorVision = BooleanOptionItem.Create(Id + 12, GeneralOption.ImpostorVision,  true, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.HexMaster]);
        CanKillTNA = BooleanOptionItem.Create(Id + 16, "CanKillTNA", false, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.HexMaster]);
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
        HexedPlayer.Add(playerId, []);
        // NowSwitchTrigger = (SwitchTriggerList)ModeSwitchAction.GetValue();

        var pc = Utils.GetPlayerById(playerId);
        pc.AddDoubleTrigger();
        CustomRoleManager.OnFixedUpdateOthers.Add(OnFixedUpdateOthers);
    }

    private static void SendRPC(bool regularHex, byte hexId, byte target = 255, byte oldHex = 255, byte newHex = 255)
    {
        if (regularHex)
        {
            var msg = new RpcDoHex(PlayerControl.LocalPlayer.NetId, hexId, target);
            RpcUtils.LateBroadcastReliableMessage(msg);
        }
        else
        {
            var player = Utils.GetPlayerById(hexId);
            if (player == null) return;

            var writer = MessageWriter.Get(SendOption.Reliable);
            writer.Write(newHex);
            writer.Write(oldHex);
            RpcUtils.LateBroadcastReliableMessage(new RpcSyncRoleSkill(PlayerControl.LocalPlayer.NetId, player.NetId, writer));
        }
    }
    public static void ReceiveRPC(MessageReader reader, bool regularHex)
    {
        if (regularHex)
        {
            var hexmaster = reader.ReadByte();
            var hexedId = reader.ReadByte();
            if (hexedId != 255)
            {
                HexedPlayer[hexmaster].Add(hexedId);
            }
            else
            {
                HexedPlayer[hexmaster].Clear();
            }
        }
        else
        {
            CurrentHexedPlayer = reader.ReadByte();
            LastHexedPlayer = reader.ReadByte();
        }
    }

    //public override void ApplyGameOptions(IGameOptions opt, byte id) => opt.SetVision(HasImpostorVision.GetBool());

    public override bool CanUseKillButton(PlayerControl pc) => true;
    // public override bool CanUseImpostorVentButton(PlayerControl pc) => true;
    public override void SetKillCooldown(byte id) => HexCooldown.GetFloat();

    /*
    private static bool IsHexMode(byte playerId)
    {
        return HexMode.ContainsKey(playerId) && HexMode[playerId];
    }
   
    private static void SwitchHexMode(byte playerId, bool kill)
    {
        bool needSwitch = false;
        switch (NowSwitchTrigger)
        {
            case SwitchTriggerList.TriggerKill:
                needSwitch = kill;
                break;
            case SwitchTriggerList.TriggerVent:
                needSwitch = !kill;
                break;
        }
        if (needSwitch)
        {
            HexMode[playerId] = !HexMode[playerId];
            SendRPC(false, playerId);
            Utils.NotifyRoles(SpecifySeer: Utils.GetPlayerById(playerId));
        }
    }
    */
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
            SendRPC(true, killer.PlayerId, target.PlayerId);
            //キルクールの適正化
            killer.ResetKillCooldown();
            killer.SetKillCooldown();
        }
    }
    private void PassHex(PlayerControl player, PlayerControl target)
    {
        if (!HasHexed) return;
        if (!target.IsAlive()) return;

        var now = GetTimeStamp();
        if (now - CurrentHexedPlayerTime < MovingHexPassCooldown.GetFloat()) return;
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
        CurrentHexedPlayerTime = now;
        MarkEveryoneDirtySettings();


        SendRPC(false, CurrentHexedPlayer, LastHexedPlayer);
        Logger.Msg($"{player.GetNameWithRole()} passed hex to {target.GetNameWithRole()}", "Hex Master Pass");
    }
    public static void ResetHex()
    {
        CurrentHexedPlayer = 254;
        CurrentHexedPlayerTime = new();
        LastHexedPlayer = byte.MaxValue;
        HasHexed = false;
    }
    public override void OnFixedUpdate(PlayerControl player, bool lowLoad, long nowTime, int timerLowLoad)
    {
        if (!lowLoad && CurrentHexedPlayer == 254)
        {
            SendRPC(false, CurrentHexedPlayer, LastHexedPlayer);
            CurrentHexedPlayer = byte.MaxValue;
        }
    }
    public override void OnReportDeadBody(PlayerControl reported, NetworkedPlayerInfo agitatergoatedrole)
    {
        if (CurrentHexedPlayer == byte.MaxValue) return;
        var target = GetPlayerById(CurrentHexedPlayer);
        var killer = _Player;
        if (target == null || killer == null) return;

        HexedPlayer[killer.PlayerId].Add(target.PlayerId);
        SendRPC(true, killer.PlayerId, target.PlayerId);
        ResetHex();
        Logger.Info($"Passing hex ended, {target.GetRealName()} ended with hex on report", "Hex Master");
    }
    public override void AfterMeetingTasks()
    {
        foreach (var hexmaster in HexedPlayer.Keys)
        {
            HexedPlayer[hexmaster].Clear();
            SendRPC(true, hexmaster);
        }
    }
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (target.IsTransformedNeutralApocalypse()) return false;

        /*
        if (NowSwitchTrigger == SwitchTriggerList.TriggerDouble)
        {
            return killer.CheckDoubleTrigger(target, () => { SetHexed(killer, target); });
        }
        if (!IsHexMode(killer.PlayerId))
        {
            SwitchHexMode(killer.PlayerId, true);
            //キルモードなら通常処理に戻る
            return true;
        }
        */
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
        if (!CustomRoles.HexMaster.RoleExist()) return;
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
                var KillRange = ExtendedPlayerControl.GetKillDistances();
                if (min.Value <= KillRange && !player.inVent && !player.inMovingPlat && !target.inVent && !target.inMovingPlat && player.RpcCheckAndMurder(target, true))
                {
                    PassHex(player, target);
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
            if (pc.IsTransformedNeutralApocalypse() && !CanKillTNA.GetBool()) continue;
            var whichId = dic.FirstOrDefault().Key;
            var hexmaster = Utils.GetPlayerById(whichId);
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
                if (pc.GetDeathReason() is not PlayerState.DeathReason.Suicide) Main.AfterMeetingDeathPlayers.Remove(pc.PlayerId);
            }
        }
        CheckForEndVotingPatch.TryAddAfterMeetingDeathPlayers(PlayerState.DeathReason.Hex, [.. hexedIdList]);
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
            SendRPC(true, hexmaster);
        }
    }
    /*
    public override void OnEnterVent(PlayerControl pc, Vent vent)
    {
        if (NowSwitchTrigger is SwitchTriggerList.TriggerVent)
        {
            SwitchHexMode(pc.PlayerId, false);
        }
    }
    */
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
    /*
    public override string GetLowerText(PlayerControl hexmaster, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        if (!hexmaster.IsAlive() || isForMeeting || hexmaster != seen) return string.Empty;

        var str = new StringBuilder();
        if (isForHud)
        {
            str.Append($"{GetString("WitchCurrentMode")}: ");
        }
        else
        {
            str.Append($"{GetString("Mode")}: ");
        }
        if (NowSwitchTrigger == SwitchTriggerList.TriggerDouble)
        {
            str.Append(GetString("HexMasterModeDouble"));
        }
        else
        {
            str.Append(IsHexMode(hexmaster.PlayerId) ? GetString("HexMasterModeHex") : GetString("HexMasterModeKill"));
        }

        return str.ToString();
    }
    */

    public override void SetAbilityButtonText(HudManager hud, byte playerid) => hud.KillButton.OverrideText($"{GetString("HexButtonText")}");

    public override void Remove(byte playerId)
    {
        if (HexedPlayer.ContainsKey(playerId))
        {
            HexedPlayer[playerId].Clear();
            SendRPC(true, playerId);
        }
    }
}
