using AmongUs.GameOptions;
using Hazel;
using UnityEngine;
using TOHE.Modules;
using TOHE.Roles.Core;
using static TOHE.Utils;
using static TOHE.Translator;
using static TOHE.Options;
using System.Text;

namespace TOHE.Roles.Crewmate;

internal class Mime : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 28900;
    public static readonly HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Any();
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateKilling;
    //==================================================================\\

    private static OptionItem MirrorCooldown;
    private static OptionItem MirrorUses;
    private static OptionItem MirrorDisappearsAfterMeeting;

    public static readonly Dictionary<byte, HashSet<byte>> MirrorList = [];
    private static CustomRoles StoredRole;

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Mime);
        MirrorCooldown = FloatOptionItem.Create(Id + 10, "AbilityCooldown", new(0f, 180f, 2.5f), 20f, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Mime])
            .SetValueFormat(OptionFormat.Seconds);
        MirrorUses = IntegerOptionItem.Create(Id + 11, "MimeMaxMirrorUses", new(1, 100, 1), 10, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Mime])
            .SetValueFormat(OptionFormat.Times);
        MirrorDisappearsAfterMeeting = BooleanOptionItem.Create(Id + 12, "MimeMirrorDisappearsAfterMeeting", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Mime]);
    }
    public override void Init()
    {
        playerIdList.Clear();
        MirrorList.Clear();
        StoredRole = new CustomRoles();
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        MirrorList[playerId] = [];
        AbilityLimit = MirrorUses.GetInt();
        StoredRole = CustomRoles.Mime;

        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }
    public override void Remove(byte playerId)
    {
        playerIdList.Remove(playerId);
        MirrorList.Remove(playerId);
    }
    public static void SendRPC(PlayerControl player, PlayerControl target)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, -1);
        writer.WritePacked((int)CustomRoles.Mime);
        writer.Write(player.PlayerId);
        writer.Write(target.PlayerId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public override void ReceiveRPC(MessageReader reader, PlayerControl NaN)
    {
        byte MimeId = reader.ReadByte();
        byte MirroredId = reader.ReadByte();
        MirrorList[MimeId].Add(MirroredId);
    }
    public override bool CanUseKillButton(PlayerControl pc) => pc.IsAlive();
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = MirrorCooldown.GetFloat();
    public override string GetProgressText(byte playerId, bool comms) => ColorString( AbilityLimit > 0 ? GetRoleColor(CustomRoles.Mime).ShadeColor(0.25f) : Color.gray, $"({AbilityLimit})");
    public override void ApplyGameOptions(IGameOptions opt, byte playerId) => opt.SetVision(false);
    public override void SetAbilityButtonText(HudManager hud, byte id)
    {
        hud.KillButton.OverrideText(GetString("MimeKillButtonText"));
    }
    private static bool BlackList(CustomRoles role) 
    {
        return role is CustomRoles.War or
            CustomRoles.Pestilence or
            CustomRoles.KillingMachine or 
            CustomRoles.Glitch;

    }

    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (killer == null || target == null) return false;
        if (!StoredRole.Equals(CustomRoles.Mime))
        {
            float tempAbility = AbilityLimit;
            killer.RpcSetCustomRole(StoredRole);
            killer.GetRoleClass()?.OnAdd(killer.PlayerId);
            StoredRole = CustomRoles.Mime;
            killer.GetRoleClass()?.OnCheckMurderAsKiller(killer, target);
            killer.GetRoleClass()?.OnRemove(killer.PlayerId);
            killer.RpcSetCustomRole(StoredRole);
            killer.ResetKillCooldown();
            AbilityLimit = tempAbility;
            return true;
        }
        if (MirrorList[killer.PlayerId].Contains(target.PlayerId) || AbilityLimit <= 0) return false;

        MirrorList[killer.PlayerId].Add(target.PlayerId);
        SendRPC(killer, target);
        NotifyRoles(SpecifySeer: killer);
        AbilityLimit--;
        Logger.Info($"Magic Mirror placed on " + target.GetRealName(), "Mime");

        return false;
    }

    public override bool CheckMurderOnOthersTarget(PlayerControl killer, PlayerControl target)
    {
        var mime = Utils.GetPlayerListByRole(CustomRoles.Mime);
        if (killer == null || target == null || mime == null || !mime.Any()) return true;
        foreach (var (mimeId, unneeded) in MirrorList)
        {
            if (!MirrorList[mimeId].Contains(target.PlayerId)) return false;
        }

        killer.RpcGuardAndKill(target);
        killer.ResetKillCooldown();
        killer.SetKillCooldown();

        
        foreach (var mimes in mime)
        {
            if (mimes == null || !mimes.IsAlive()) continue;
            if (!BlackList(killer.GetCustomRole())) { 
                StoredRole = killer.GetCustomRole();
                mimes.Notify(GetString("MimeKillAttempt"));
                _ = new LateTask(() =>
                {
                    MirrorList[mimes.PlayerId].Remove(target.PlayerId);
                }
                , 10f, "MirrorClearAfterAttack");
            }
            else mimes.Notify(GetString("MimeUnableToStore"));
        }

        NotifyRoles(SpecifySeer: killer, SpecifyTarget: target, ForceLoop: true);
        NotifyRoles(SpecifySeer: target, SpecifyTarget: killer, ForceLoop: true);
        return true;
    }

    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        if (MirrorDisappearsAfterMeeting.GetBool())
        {
            MirrorList.Clear();
        }
    }
    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        if (seer == null || !seer.IsAlive() || isForMeeting || !isForHud || StoredRole == CustomRoles.Mime) return string.Empty;
        return GetString("MimeStoredRole") + GetRoleName(StoredRole);
    }
    public override string GetMark(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
        => MirrorList[seer.PlayerId].Contains(seen.PlayerId) ? ColorString(GetRoleColor(CustomRoles.Mime), "⊠") : string.Empty;
}

