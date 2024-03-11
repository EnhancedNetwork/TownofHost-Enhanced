using AmongUs.GameOptions;
using Hazel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TOHE.Roles.AddOns.Common;
using TOHE.Roles.Core;
using TOHE.Roles.Crewmate;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Neutral;

internal class HexMaster : RoleBase
{
    public enum SwitchTrigger
    {
        Kill,
        Vent,
        DoubleTrigger,
    };
    public static readonly string[] SwitchTriggerText =
    [
        "TriggerKill", "TriggerVent","TriggerDouble"
    ];

    private const int Id = 16400;
    private static Color RoleColorHex = Utils.GetRoleColor(CustomRoles.HexMaster);
    private static Color RoleColorSpell = Utils.GetRoleColor(CustomRoles.Impostor);

    private static List<byte> playerIdList = [];
    public static bool On;
    public override bool IsEnable => On;

    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;

    private static Dictionary<byte, bool> HexMode = [];
    private static Dictionary<byte, List<byte>> HexedPlayer = [];

    public static OptionItem ModeSwitchAction;
    public static OptionItem HexesLookLikeSpells;
    public static OptionItem HasImpostorVision;
    public static SwitchTrigger NowSwitchTrigger;
    public static void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.HexMaster, 1, zeroOne: false);        
        ModeSwitchAction = StringOptionItem.Create(Id + 10, "WitchModeSwitchAction", SwitchTriggerText, 2, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.HexMaster]);
        HexesLookLikeSpells = BooleanOptionItem.Create(Id + 11, "HexesLookLikeSpells",  false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.HexMaster]);
        HasImpostorVision = BooleanOptionItem.Create(Id + 12, "ImpostorVision",  true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.HexMaster]);
    }
    public override void Init()
    {
        playerIdList = [];
        HexMode = [];
        HexedPlayer = [];
        On = false;
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        On = true;
        HexMode.Add(playerId, false);
        HexedPlayer.Add(playerId, []);
        NowSwitchTrigger = (SwitchTrigger)ModeSwitchAction.GetValue();
        var pc = Utils.GetPlayerById(playerId);
        pc.AddDoubleTrigger();

        if (!AmongUsClient.Instance.AmHost) return;
        CustomRoleManager.MarkOthers.Add(GetHexedMark);
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
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
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetKillOrHex, SendOption.Reliable, -1);
            writer.Write(hexId);
            writer.Write(HexMode[hexId]);
            AmongUsClient.Instance.FinishRpcImmediately(writer);

        }
    }
    public override void ApplyGameOptions(IGameOptions opt, byte playerId) => opt.SetVision(HasImpostorVision.GetBool());

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
    private static bool IsHexMode(byte playerId)
    {
        return HexMode.ContainsKey(playerId) && HexMode[playerId];
    }
    private static void SwitchHexMode(byte playerId, bool kill)
    {
        bool needSwitch = false;
        switch (NowSwitchTrigger)
        {
            case SwitchTrigger.Kill:
                needSwitch = kill;
                break;
            case SwitchTrigger.Vent:
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
    public static bool HaveHexedPlayer()
    {
        foreach (var hexmaster in playerIdList)
        {
            if (HexedPlayer[hexmaster].Count != 0) return true;
        }
        return false;

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
    private static void RemoveHexedPlayer()
    {
        foreach (var hexmaster in playerIdList)
        {
            HexedPlayer[hexmaster].Clear();
            SendRPC(true, hexmaster);
        }
    }
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (Medic.ProtectList.Contains(target.PlayerId)) return false;
        if (target.Is(CustomRoles.Pestilence)) return false;
        if (target.Is(CustomRoles.HexMaster)) return false;

        if (NowSwitchTrigger == SwitchTrigger.DoubleTrigger)
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
    public static void OnCheckForEndVoting(PlayerState.DeathReason deathReason, params byte[] exileIds)
    {
        if (!On || deathReason != PlayerState.DeathReason.Vote) return;
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
    public override void OnPlayerExiled(PlayerControl player, GameData.PlayerInfo exiled)
    {
        RemoveHexedPlayer();
    }
    private string GetHexedMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;

        if (IsHexed(seen.PlayerId) && isForMeeting)
        {
            if (HexesLookLikeSpells.GetBool()) return Utils.ColorString(RoleColorSpell, "†");
            else return Utils.ColorString(RoleColorHex, "乂");
        }
        return string.Empty;
        
    }
    public override string GetLowerText(PlayerControl hexmaster, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        if (hexmaster == null || isForMeeting) return "";

        var str = new StringBuilder();
        if (isForHud)
        {
            str.Append(GetString("WitchCurrentMode"));
        }
        else
        {
            str.Append($"{GetString("Mode")}:");
        }
        if (NowSwitchTrigger == SwitchTrigger.DoubleTrigger)
        {
            str.Append(GetString("HexMasterModeDouble"));
        }
        else
        {
            str.Append(IsHexMode(hexmaster.PlayerId) ? GetString("HexMasterModeHex") : GetString("HexMasterModeKill"));
        }
        return str.ToString();
    }
    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        if (IsHexMode(playerId) && NowSwitchTrigger != SwitchTrigger.DoubleTrigger)
        {
            hud.KillButton.OverrideText($"{GetString("HexButtonText")}");
        }
        else
        {
            hud.KillButton.OverrideText($"{GetString("KillButtonText")}");
        }
    }

    public override void OnEnterVent(PlayerControl pc, Vent vent)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (!On) return;
        if (playerIdList.Contains(pc.PlayerId))
        {
            if (NowSwitchTrigger is SwitchTrigger.Vent)
            {
                SwitchHexMode(pc.PlayerId, false);
            }
        }
    }
}