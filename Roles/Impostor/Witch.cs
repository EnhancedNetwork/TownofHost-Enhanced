using Hazel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TOHE.Modules;
using TOHE.Roles.Core;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Impostor;

internal class Witch : RoleBase
{
    private const int Id = 2500;
    public static bool On;
    public override bool IsEnable => On;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;

    public static OptionItem ModeSwitchActionOpt;

    private static List<byte> playerIdList = [];
    private static Dictionary<byte, bool> SpellMode = [];
    private static Dictionary<byte, List<byte>> SpelledPlayer = [];

    private enum SwitchTrigger
    {
        TriggerKill,
        TriggerVent,
        TriggerDouble,
    };
    private static SwitchTrigger NowSwitchTrigger;

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Witch);
        ModeSwitchActionOpt = StringOptionItem.Create(Id + 10, "WitchModeSwitchAction", EnumHelper.GetAllNames<SwitchTrigger>(), 2, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Witch]);
    }
    public override void Init()
    {
        playerIdList = [];
        SpellMode = [];
        SpelledPlayer = [];
        On = false;
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        SpellMode.Add(playerId, false);
        SpelledPlayer.Add(playerId, []);
        NowSwitchTrigger = (SwitchTrigger)ModeSwitchActionOpt.GetValue();
        On = true;

        var pc = Utils.GetPlayerById(playerId);
        pc.AddDoubleTrigger();

        if (AmongUsClient.Instance.AmHost)
        {
            CustomRoleManager.MarkOthers.Add(GetSpelledMark);
        }
    }

    private static void SendRPC(bool doSpell, byte witchId, byte target = 255)
    {
        if (doSpell)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.DoSpell, SendOption.Reliable, -1);
            writer.Write(witchId);
            writer.Write(target);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        else
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetKillOrSpell, SendOption.Reliable, -1);
            writer.Write(witchId);
            writer.Write(SpellMode[witchId]);
            AmongUsClient.Instance.FinishRpcImmediately(writer);

        }
    }
    public static void ReceiveRPC(MessageReader reader, bool doSpell)
    {
        if (doSpell)
        {
            var witch = reader.ReadByte();
            var spelledId = reader.ReadByte();
            if (spelledId != 255)
            {
                SpelledPlayer[witch].Add(spelledId);
            }
            else
            {
                SpelledPlayer[witch].Clear();
            }
        }
        else
        {
            byte playerId = reader.ReadByte();
            SpellMode[playerId] = reader.ReadBoolean();
        }
    }

    private static bool IsSpellMode(byte playerId) => SpellMode.ContainsKey(playerId) && SpellMode[playerId];

    private static void SwitchSpellMode(byte playerId, bool kill)
    {
        bool needSwitch = false;
        switch (NowSwitchTrigger)
        {
            case SwitchTrigger.TriggerKill:
                needSwitch = kill;
                break;
            case SwitchTrigger.TriggerVent:
                needSwitch = !kill;
                break;
        }
        if (needSwitch)
        {
            SpellMode[playerId] = !SpellMode[playerId];
            SendRPC(false, playerId);
            Utils.NotifyRoles(SpecifySeer: Utils.GetPlayerById(playerId));
        }
    }

    private static bool IsSpelled(byte target) => SpelledPlayer.Any(x => x.Value.Contains(target));

    private static void SetSpelled(PlayerControl killer, PlayerControl target)
    {
        if (!IsSpelled(target.PlayerId))
        {
            SpelledPlayer[killer.PlayerId].Add(target.PlayerId);
            SendRPC(true, killer.PlayerId, target.PlayerId);
            killer.SetKillCooldown();
            killer.RPCPlayCustomSound("Curse");
            Utils.NotifyRoles(SpecifySeer: killer, SpecifyTarget: target, ForceLoop: true);
        }
    }
    private static void RemoveSpelledPlayer()
    {
        foreach (var witch in playerIdList)
        {
            SpelledPlayer[witch].Clear();
            SendRPC(true, witch);
        }
    }

    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (NowSwitchTrigger == SwitchTrigger.TriggerDouble)
        {
            return killer.CheckDoubleTrigger(target, () => { SetSpelled(killer, target); });
        }
        if (!IsSpellMode(killer.PlayerId))
        {
            SwitchSpellMode(killer.PlayerId, true);
            //キルモードなら通常処理に戻る
            return true;
        }
        SetSpelled(killer, target);

        //スペルに失敗してもスイッチ判定
        SwitchSpellMode(killer.PlayerId, true);
        //キル処理終了させる
        return false;
    }
    public static void OnCheckForEndVoting(PlayerState.DeathReason deathReason, params byte[] exileIds)
    {
        if (!On || deathReason != PlayerState.DeathReason.Vote) return;

        foreach (var id in exileIds)
        {
            if (SpelledPlayer.ContainsKey(id))
                SpelledPlayer[id].Clear();
        }
        var spelledIdList = new List<byte>();
        foreach (var pc in Main.AllAlivePlayerControls)
        {
            var dic = SpelledPlayer.Where(x => x.Value.Contains(pc.PlayerId));
            if (!dic.Any()) continue;
            var whichId = dic.FirstOrDefault().Key;
            var witch = Utils.GetPlayerById(whichId);
            if (witch != null && witch.IsAlive())
            {
                if (!Main.AfterMeetingDeathPlayers.ContainsKey(pc.PlayerId))
                {
                    pc.SetRealKiller(witch);
                    spelledIdList.Add(pc.PlayerId);
                }
            }
            else
            {
                Main.AfterMeetingDeathPlayers.Remove(pc.PlayerId);
            }
        }
        
        CheckForEndVotingPatch.TryAddAfterMeetingDeathPlayers(PlayerState.DeathReason.Spell, [.. spelledIdList]);
        RemoveSpelledPlayer();
    }
    public override void OnPlayerExiled(PlayerControl player, GameData.PlayerInfo exiled)
    {
        RemoveSpelledPlayer();
    }
    private string GetSpelledMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;

        if (IsSpelled(seen.PlayerId) && isForMeeting)
        {
            return Utils.ColorString(Palette.ImpostorRed, "†");
        }
        return string.Empty;
    }
    public override string GetLowerText(PlayerControl witch, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        if (witch == null || isForMeeting) return string.Empty;

        var str = new StringBuilder();
        if (isForHud)
        {
            str.Append($"{GetString("WitchCurrentMode")}: ");
        }
        else
        {
            str.Append($"{GetString("Mode")}: ");
        }
        if (NowSwitchTrigger == SwitchTrigger.TriggerDouble)
        {
            str.Append(GetString("WitchModeDouble"));
        }
        else
        {
            str.Append(IsSpellMode(witch.PlayerId) ? GetString("WitchModeSpell") : GetString("WitchModeKill"));
        }
        return str.ToString();
    }
    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        if (IsSpellMode(playerId) && NowSwitchTrigger != SwitchTrigger.TriggerDouble)
        {
            hud.KillButton.OverrideText(GetString("WitchSpellButtonText"));
        }
        else
        {
            hud.KillButton.OverrideText(GetString("KillButtonText"));
        }
    }

    public override void OnEnterVent(PlayerControl pc, Vent vent)
    {
        if (NowSwitchTrigger is SwitchTrigger.TriggerVent)
        {
            SwitchSpellMode(pc.PlayerId, false);
        }
    }
}