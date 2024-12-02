using AmongUs.GameOptions;
using Hazel;
using System.Text;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;


namespace TOHE.Roles.Neutral;

internal class HexMaster : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 16400;
    private static readonly HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Any();
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralKilling;
    //==================================================================\\

    private static OptionItem ModeSwitchAction;
    private static OptionItem HexesLookLikeSpells;
    private static OptionItem HasImpostorVision;

    private static readonly Dictionary<byte, bool> HexMode = [];
    private static readonly Dictionary<byte, List<byte>> HexedPlayer = [];

    private static readonly Color RoleColorHex = Utils.GetRoleColor(CustomRoles.HexMaster);
    private static readonly Color RoleColorSpell = Utils.GetRoleColor(CustomRoles.Impostor);

    private enum SwitchTriggerList
    {
        TriggerKill,
        TriggerVent,
        TriggerDouble,
    };
    private static SwitchTriggerList NowSwitchTrigger;

    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.HexMaster, 1, zeroOne: false);
        ModeSwitchAction = StringOptionItem.Create(Id + 10, GeneralOption.ModeSwitchAction, EnumHelper.GetAllNames<SwitchTriggerList>(), 2, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.HexMaster]);
        HexesLookLikeSpells = BooleanOptionItem.Create(Id + 11, "HexesLookLikeSpells", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.HexMaster]);
        HasImpostorVision = BooleanOptionItem.Create(Id + 12, GeneralOption.ImpostorVision, true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.HexMaster]);
    }
    public override void Init()
    {
        playerIdList.Clear();
        HexMode.Clear();
        HexedPlayer.Clear();
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        HexMode.Add(playerId, false);
        HexedPlayer.Add(playerId, []);
        NowSwitchTrigger = (SwitchTriggerList)ModeSwitchAction.GetValue();

        var pc = Utils.GetPlayerById(playerId);
        pc.AddDoubleTrigger();
    }

    private static void SendRPC(bool doHex, byte hexId, byte target = 255)
    {
        if (doHex)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.DoHex, SendOption.Reliable, -1);
            writer.Write(hexId);
            writer.Write(target);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        else
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetKillOrSpell, SendOption.Reliable, -1);
            writer.Write(hexId);
            writer.Write(HexMode[hexId]);
            AmongUsClient.Instance.FinishRpcImmediately(writer);

        }
    }
    public static void ReceiveRPC(MessageReader reader, bool doHex)
    {
        if (doHex)
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
            byte playerId = reader.ReadByte();
            HexMode[playerId] = reader.ReadBoolean();
        }
    }

    public override void ApplyGameOptions(IGameOptions opt, byte id) => opt.SetVision(HasImpostorVision.GetBool());

    public override bool CanUseKillButton(PlayerControl pc) => true;
    public override bool CanUseImpostorVentButton(PlayerControl pc) => true;

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
    private static bool IsHexed(byte target)
    {
        foreach (var hexmaster in playerIdList)
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
            killer.SetKillCooldown();
        }
    }
    public override void AfterMeetingTasks()
    {
        foreach (var hexmaster in playerIdList)
        {
            HexedPlayer[hexmaster].Clear();
            SendRPC(true, hexmaster);
        }
    }
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (target.IsTransformedNeutralApocalypse()) return false;

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
        SetHexed(killer, target);

        //スペルに失敗してもスイッチ判定
        SwitchHexMode(killer.PlayerId, true);
        //キル処理終了させる
        return false;
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
                Main.AfterMeetingDeathPlayers.Remove(pc.PlayerId);
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
        foreach (var hexmaster in playerIdList)
        {
            HexedPlayer[hexmaster].Clear();
            SendRPC(true, hexmaster);
        }
    }
    public override void OnEnterVent(PlayerControl pc, Vent vent)
    {
        if (NowSwitchTrigger is SwitchTriggerList.TriggerVent)
        {
            SwitchHexMode(pc.PlayerId, false);
        }
    }
    public override string GetMarkOthers(PlayerControl seer, PlayerControl target, bool isForMeeting = false)
    {
        if (isForMeeting && IsHexed(target.PlayerId))
        {
            if (!HexesLookLikeSpells.GetBool())
            {
                return Utils.ColorString(RoleColorHex, "乂");
            }
            else
            {
                return Utils.ColorString(RoleColorSpell, "†");
            }
        }
        return string.Empty;
    }

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

    public override void SetAbilityButtonText(HudManager hud, byte playerid)
    {
        if (IsHexMode(playerid) && NowSwitchTrigger != SwitchTriggerList.TriggerDouble)
        {
            hud.KillButton.OverrideText($"{GetString("HexButtonText")}");
        }
        else
        {
            hud.KillButton.OverrideText($"{GetString("KillButtonText")}");
        }
    }

    public override void Remove(byte playerId)
    {
        if (HexedPlayer.ContainsKey(playerId))
        {
            HexedPlayer[playerId].Clear();
            SendRPC(true, playerId);
        }
    }
}
