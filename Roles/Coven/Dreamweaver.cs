using Hazel;
using InnerNet;
using TOHE.Modules;
using TOHE.Roles.Core;
using UnityEngine;
using static TOHE.MeetingHudStartPatch;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.Coven;

internal class Dreamweaver : CovenManager
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Dreamweaver;
    private const int Id = 31100;
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CovenTrickery;
    //==================================================================\\

    private static OptionItem AbilityCooldown;
    private static OptionItem NeutralsCanBeDreamwoven;
    private static OptionItem ImpostorsCanBeDreamwoven;

    private static readonly Dictionary<byte, HashSet<byte>> DreamwovenList = [];
    private static readonly Dictionary<byte, HashSet<byte>> InsomniaList = [];


    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.CovenRoles, Role, 1, zeroOne: false);
        AbilityCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.AbilityCooldown, new(0f, 180f, 2.5f), 30f, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[Role])
                .SetValueFormat(OptionFormat.Seconds);
        NeutralsCanBeDreamwoven = BooleanOptionItem.Create(Id + 11, "DreamweaverSettings.CanDreamweaveNeutrals", true, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[Role]);
        ImpostorsCanBeDreamwoven = BooleanOptionItem.Create(Id + 12, "DreamweaverSettings.CanDreamweaveImpostors", true, TabGroup.CovenRoles, false).SetParent(CustomRoleSpawnChances[Role]);
    }
    public override void Init()
    {
        DreamwovenList.Clear();
        InsomniaList.Clear();
    }
    public override void Add(byte playerId)
    {
        DreamwovenList[playerId] = [];
        InsomniaList[playerId] = [];
        GetPlayerById(playerId)?.AddDoubleTrigger();
        CustomRoleManager.CheckDeadBodyOthers.Add(OnPlayerDead);
    }
    private static void SendRPC(byte typeId, PlayerControl player, PlayerControl target)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable);
        writer.WriteNetObject(player);
        writer.Write(typeId);
        writer.Write(player.PlayerId);
        writer.Write(target.PlayerId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public override void ReceiveRPC(MessageReader reader, PlayerControl NaN)
    {
        byte typeId = reader.ReadByte();
        byte playerId = reader.ReadByte();
        byte targetId = reader.ReadByte();

        switch (typeId)
        {
            case 0:
                DreamwovenList[playerId].Add(targetId);
                break;
            case 1:
                InsomniaList[playerId].Add(targetId);
                break;
        }
    }
    public override bool CanUseKillButton(PlayerControl pc) => true;
    public override void SetKillCooldown(byte id) => AbilityCooldown.GetFloat();
    public override void SetAbilityButtonText(HudManager hud, byte playerId) =>
        hud.KillButton.OverrideText(GetString("Dreamweaver.KillButtonText"));
    public override Sprite GetKillButtonSprite(PlayerControl player, bool shapeshifting) => CustomButton.Get("Dreamweave");
    // please someone make the sprite look better im begging you - Marg

    public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        if (IsInsomnia(seen.PlayerId))
        {
            return ColorString(GetRoleColor(CustomRoles.Dreamweaver), "<u>☁</u>");
        }
        if (IsDreamwoven(seen.PlayerId))
        {
            isForMeeting = true;
            return ColorString(new Color32(255, 255, 255, 255), "☁");
        }
        return string.Empty;
    }
    public override string GetMarkOthers(PlayerControl seer, PlayerControl target, bool isForMeeting = false)
    {
        if (!_Player) return string.Empty;
        if (seer.PlayerId == _Player.PlayerId) return string.Empty;
        if (IsInsomnia(target.PlayerId) && (seer.IsPlayerCovenTeam() || target.PlayerId == seer.PlayerId || !seer.IsAlive()))
        {
            return ColorString(GetRoleColor(CustomRoles.Dreamweaver), "<u>☁</u>");
        }
        if (IsDreamwoven(target.PlayerId) && (seer.IsPlayerCovenTeam() || target.PlayerId == seer.PlayerId || !seer.IsAlive()))
        {
            isForMeeting = true;
            return ColorString(new Color32(255, 255, 255, 255), "☁");
        }
        return string.Empty;
    }
    private void OnPlayerDead(PlayerControl killer, PlayerControl deadPlayer, bool inMeeting)
    {
        foreach (var player in DreamwovenList.Keys)
        {
            if (deadPlayer.PlayerId == player) ResetInsomnia(player);
        }
        foreach (var player in InsomniaList.Keys)
        {
            if (deadPlayer.PlayerId == player) ResetInsomnia(player);
        }
    }
    private void SetDreamwoven(PlayerControl killer, PlayerControl target)
    {
        if ((target.GetCustomRole().IsNeutral() && !NeutralsCanBeDreamwoven.GetBool()) || (target.GetCustomRole().IsImpostor() && !ImpostorsCanBeDreamwoven.GetBool()) || target.IsPlayerCovenTeam())
        {
            killer.Notify(GetString("Dreamweaver.CantDreamweave"));
            return;
        }
        if (IsDreamwoven(target.PlayerId) || IsInsomnia(target.PlayerId))
        {
            killer.Notify(GetString("Dreamweaver.AlreadyDreamwoven"));
            return;
        }
        DreamwovenList[killer.PlayerId].Add(target.PlayerId);
        SendRPC(0, killer, target);

        killer.ResetKillCooldown();
        killer.SetKillCooldown();
        killer.Notify(string.Format(GetString("Dreamweaver.DreamweaveSuccess"), target.GetRealName()));
    }
    private void SetInsomnia(PlayerControl killer, PlayerControl target)
    {
        InsomniaList[killer.PlayerId].Add(target.PlayerId);
        SendRPC(1, killer, target);
        target.Notify(GetString("Dreamweaver.InsomniaNotification"));
        target.SetAbilityUseLimit(0);
        target.SetKillCooldownV3(999);
        target.ResetKillCooldown();
    }
    private static bool IsDreamwoven(byte target)
    {
        if (DreamwovenList.Count < 1) return false;
        foreach (var player in DreamwovenList.Keys)
        {
            if (DreamwovenList[player].Contains(target)) return true;
        }
        return false;
    }
    public static bool IsInsomnia(byte target)
    {
        if (InsomniaList.Count < 1) return false;
        foreach (var player in InsomniaList.Keys)
        {
            if (InsomniaList[player].Contains(target)) return true;
        }
        return false;
    }
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (killer.CheckDoubleTrigger(target, () => { SetDreamwoven(killer, target); }))
        {
            if (HasNecronomicon(killer))
            {
                if (target.IsPlayerCovenTeam())
                {
                    killer.Notify(GetString("CovenDontKillOtherCoven"));
                    return false;
                }
                else return true;
            }
        }
        return false;
    }
    public override void AfterMeetingTasks()
    {
        foreach (var player in DreamwovenList.Keys)
        {
            if (!GetPlayerById(player).IsAlive())
            {
                ResetInsomnia(player);
                continue;
            }
            else
            {
                foreach (var target in DreamwovenList[player])
                {
                    SetInsomnia(GetPlayerById(player), GetPlayerById(target));
                    DreamwovenList[player].Remove(target);
                }
            }
        }
    }
    public override void OnFixedUpdate(PlayerControl player, bool lowLoad, long nowTime, int timerLowLoad)
    {
        foreach (var target in InsomniaList[player.PlayerId])
        {
            var targetPc = GetPlayerById(target);
            if (targetPc == null || !targetPc.IsAlive())
            {
                InsomniaList[player.PlayerId].Remove(target);
                continue;
            }
            if (targetPc.GetAbilityUseLimit() > 0) targetPc.SetAbilityUseLimit(0);
            if (targetPc.GetKillTimer() < 1)
            {
                targetPc.SetKillCooldownV3(999);
                targetPc.ResetKillCooldown();
            }
        }
    }
    private void ResetInsomnia(byte dreamweaver)
    {
        foreach (var player in InsomniaList[dreamweaver])
        {
            Logger.Info($"{GetPlayerById(player).GetRealName()} has been cleared of Insomnia", "Dreamweaver");
            if (!GameStates.IsMeeting)
            {
                GetPlayerById(player).Notify(GetString("Dreamweaver.DreamweaverDied"));
            }
            else
            {
                SendMessage(GetString("Dreamweaver.DreamweaverDied"), player, ColorString(GetRoleColor(CustomRoles.Dreamweaver), GetString("Dreamweaver").ToUpper()));
            }
        }
        DreamwovenList[dreamweaver].Clear();
        InsomniaList[dreamweaver].Clear();
    }
    public override void OnMeetingHudStart(PlayerControl pc)
    {
        foreach (var player in DreamwovenList[pc.PlayerId])
        {
            AddMsg(GetString("Dreamweaver.DreamwovenWarning"), player, ColorString(GetRoleColor(CustomRoles.Dreamweaver), GetString("Dreamweaver").ToUpper()));
        }
    }
}

