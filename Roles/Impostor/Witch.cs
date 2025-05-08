using Hazel;
using System.Text;
using TOHE.Modules;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Impostor;

internal class Witch : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Witch;
    private const int Id = 2500;
    private static readonly HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Any();

    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorKilling;
    //==================================================================\\

    public static OptionItem ModeSwitchActionOpt;
    private static OptionItem CanKillTNA;

    private static readonly Dictionary<byte, bool> SpellMode = [];
    private static readonly Dictionary<byte, HashSet<byte>> SpelledPlayer = [];

    [Obfuscation(Exclude = true)]
    private enum SwitchTriggerList
    {
        TriggerKill,
        TriggerVent,
        TriggerDouble,
    };
    private static SwitchTriggerList NowSwitchTrigger;

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Witch);
        ModeSwitchActionOpt = StringOptionItem.Create(Id + 10, GeneralOption.ModeSwitchAction, EnumHelper.GetAllNames<SwitchTriggerList>(), 2, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Witch]);
        CanKillTNA = BooleanOptionItem.Create(Id + 11, "CanKillTNA", false, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Witch]);
    }
    public override void Init()
    {
        playerIdList.Clear();
        SpellMode.Clear();
        SpelledPlayer.Clear();
    }
    public override void Add(byte playerId)
    {
        if (!playerIdList.Contains(playerId))
            playerIdList.Add(playerId);
        SpellMode.Add(playerId, false);
        SpelledPlayer.Add(playerId, []);
        NowSwitchTrigger = (SwitchTriggerList)ModeSwitchActionOpt.GetValue();

        var pc = Utils.GetPlayerById(playerId);
        pc.AddDoubleTrigger();

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

    private static void SwitchSpellMode(byte playerId, bool kill)
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
            SpellMode[playerId] = !SpellMode[playerId];
            SendRPC(false, playerId);
            Utils.NotifyRoles(SpecifySeer: Utils.GetPlayerById(playerId));
        }
    }

    private static bool IsSpellMode(byte playerId) => SpellMode.TryGetValue(playerId, out var isSpellMode) && isSpellMode;

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
        if (NowSwitchTrigger == SwitchTriggerList.TriggerDouble)
        {
            return killer.CheckDoubleTrigger(target, () => { SetSpelled(killer, target); });
        }

        if (!IsSpellMode(killer.PlayerId))
        {
            SwitchSpellMode(killer.PlayerId, true);
            return true;
        }
        SetSpelled(killer, target);
        SwitchSpellMode(killer.PlayerId, true);

        return false;
    }
    public override void OnCheckForEndVoting(PlayerState.DeathReason deathReason, params byte[] exileIds)
    {
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
            if (pc.IsTransformedNeutralApocalypse() && !CanKillTNA.GetBool()) continue;
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
    public override void OnPlayerExiled(PlayerControl player, NetworkedPlayerInfo exiled)
    {
        RemoveSpelledPlayer();
    }
    public override void OnEnterVent(PlayerControl pc, Vent vent)
    {
        if (NowSwitchTrigger is SwitchTriggerList.TriggerVent)
        {
            SwitchSpellMode(pc.PlayerId, false);
        }
    }

    public override string GetMarkOthers(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
    {
        seen ??= seer;

        if (isForMeeting && IsSpelled(seen.PlayerId))
        {
            return Utils.ColorString(Palette.ImpostorRed, "†");
        }
        return string.Empty;
    }
    public override string GetLowerText(PlayerControl witch, PlayerControl seen, bool isForMeeting = false, bool isForHud = false)
    {
        if (!witch.IsAlive() || isForMeeting || witch != seen) return string.Empty;

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
            str.Append(GetString("WitchModeDouble"));
        }
        else
        {
            str.Append(IsSpellMode(witch.PlayerId) ? GetString("WitchModeSpell") : GetString("KillButtonText"));
        }
        return str.ToString();
    }
    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        if (IsSpellMode(playerId) && NowSwitchTrigger != SwitchTriggerList.TriggerDouble)
        {
            hud.KillButton.OverrideText(GetString("WitchSpellButtonText"));
        }
        else
        {
            hud.KillButton.OverrideText(GetString("KillButtonText"));
        }
    }

    public override void Remove(byte playerId)
    {
        if (SpelledPlayer.ContainsKey(playerId))
        {
            SpelledPlayer[playerId].Clear();
            SendRPC(true, playerId);
        }
    }
}
