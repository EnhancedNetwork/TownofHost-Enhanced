using AmongUs.GameOptions;
using TOHE.Roles.Core;
using System;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.MeetingHudStartPatch;
using static TOHE.Utils;

namespace TOHE.Roles._Ghosts_.Crewmate;

internal class Telepathy : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 29300;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Telepathy);
    public override CustomRoles ThisRoleBase => CustomRoles.GuardianAngel;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmateGhosts;
    //==================================================================\\

    private static OptionItem AbilityCooldown;
    private static OptionItem AbilityUses;
    private static OptionItem MessageMode;
    public bool HasMessaged; 
    public static Dictionary<byte, byte> TargetPlayer = [];
    private Dictionary<int, string> Determinemessage = [];
    private enum MessageModeCount
    {
        Telepathy_MessageMode_YesNo,
        Telepathy_MessageMode_Message
    }

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Telepathy);
        AbilityCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.GuardianAngelBase_ProtectCooldown, new(2.5f, 120f, 2.5f), 35f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Telepathy])
            .SetValueFormat(OptionFormat.Seconds);
        AbilityUses = IntegerOptionItem.Create(Id + 11, "TelepathyUses", new(1, 99, 1), 14, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Telepathy])
           .SetValueFormat(OptionFormat.Times);
        MessageMode = StringOptionItem.Create(Id + 12, "Telepathy_MessageMode", EnumHelper.GetAllNames<MessageModeCount>(), 0, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Telepathy]);
    }
    public override void Init()
    {
        TargetPlayer.Clear();
    }
    public override void Add(byte playerId)
    {
        AbilityLimit = AbilityUses.GetFloat();
    }
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.GuardianAngelCooldown = AbilityCooldown.GetFloat();
        AURoleOptions.ProtectionDurationSeconds = 0f;
    }
    public override bool OnCheckProtect(PlayerControl angel, PlayerControl target)
    {
        if (AbilityLimit < 1) return false;
        if (TargetPlayer.ContainsValue(target.PlayerId))
        {
            angel.Notify(GetString("TelepathyCantConnect"));
            return false;
        }


        TargetPlayer[angel.PlayerId] = target.PlayerId;
        angel.Notify(string.Format(GetString("TelepathyConnectWith"), target.GetRealName(clientData: true)));
        AbilityLimit--;
        SendSkillRPC();
        angel.RpcResetAbilityCooldown();

        return false;
    }
    public override void OnOtherTargetsReducedToAtoms(PlayerControl DeadPlayer)
    {
        if (TargetPlayer.ContainsValue(DeadPlayer.PlayerId))
        {
            TargetPlayer.Remove(TargetPlayer.First(x => x.Value == DeadPlayer.PlayerId).Key);
            if (_Player != null)
            {
                if (GameStates.IsMeeting || Main.MeetingIsStarted)
                {
                    Utils.SendMessage(GetString("TelepathyTargetDisconnect"), _state.PlayerId);
                }
                else
                {
                    _Player.Notify(GetString("TelepathyTargetDisconnect"));
                }
            }
        }
    }
    public static bool TelepathyMessage(PlayerControl pc, string[] args)
    {
        if (!AmongUsClient.Instance.AmHost || !GameStates.IsMeeting || pc == null) return false;
        if (!TargetPlayer.TryGetValue(pc.PlayerId, out var tar) || GetPlayerById(tar) == null) return false;
        if (args.Length < 2) return false;
        if (((Telepathy)pc.GetRoleClass())?.HasMessaged is null or true) return false;

        Dictionary<int, string> DetermineMessage = ((Telepathy)pc.GetRoleClass()).Determinemessage;


        string[] cmds =  {"/tms"}; // Here you can add custom cmds
        if (!cmds.Any(x => x.Equals(args[0], StringComparison.OrdinalIgnoreCase))) return false;

        switch (MessageMode.GetInt())
        {
            case 0:
                var Validation = (args[1].Equals(GetString("Yes"), StringComparison.OrdinalIgnoreCase) || args[1].Equals(GetString("No"), StringComparison.OrdinalIgnoreCase));
                if (!Validation)
                {
                    SendMessage(GetString("TelepathyYesNoUsage"), pc.PlayerId, title: GetString("TelepathyTitle"));
                    break;
                }
                var confirm = args[1].Equals(GetString("Yes"), StringComparison.OrdinalIgnoreCase);
                ((Telepathy)pc.GetRoleClass()).HasMessaged = true;

                SendMessage(GetString("Telepathy" + (confirm ? "Yes" : "No")), TargetPlayer[pc.PlayerId], ColorString(GetRoleColor(CustomRoles.Telepathy), GetString("TelepathyTitleTarget")));
                SendMessage(GetString("TelepathyConfirmSelf"), pc.PlayerId, ColorString(GetRoleColor(CustomRoles.Telepathy), GetString("TelepathyTitle")));

                break;


            case 1:
                if (!int.TryParse(args[1], out int id) || !DetermineMessage.ContainsKey(id))
                {
                    SendMessage(GetString("TelepathyMODE2Usage"), pc.PlayerId, title: ColorString(GetRoleColor(CustomRoles.Telepathy), GetString("TelepathyTitle")));
                    break;
                }
                ((Telepathy)pc.GetRoleClass()).HasMessaged = true;

                Utils.SendMessage(DetermineMessage[id], TargetPlayer[pc.PlayerId], title: ColorString(GetRoleColor(CustomRoles.Telepathy), GetString("TelepathyTitleTarget")));
                SendMessage(GetString("TelepathyConfirmSelf"), pc.PlayerId, ColorString(GetRoleColor(CustomRoles.Telepathy), GetString("TelepathyTitle")));

                break;
        }


        return true;
    }
    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        HasMessaged = false;
    }
    public override void AfterMeetingTasks()
    {
        TargetPlayer.Clear();
    }

    public override void OnOthersMeetingHudStart(PlayerControl pc)
    {
        if (_Player == null) return;

        switch (MessageMode.GetInt())
        {
            case 0:
                if (TargetPlayer.TryGetValue(_Player.PlayerId, out var t) && t == pc.PlayerId)
                    AddMsg(string.Format(GetString("TelepathyNotifyTarget"), _Player.GetRealName(clientData: true)), pc.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Telepathy), GetString("TelepathyTitle")));

                break;


            case 1:
                if (TargetPlayer.TryGetValue(_Player.PlayerId, out var w) && w == pc.PlayerId)
                    AddMsg(string.Format(GetString("TelepathyNotifyTargetMODE2"), _Player.GetRealName(clientData: true)), pc.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Telepathy), GetString("TelepathyTitle")));

                break;
        }
    }
    public override void OnMeetingHudStart(PlayerControl pc)
    {
        if (_Player == null) return;

        switch (MessageMode.GetInt())
        {
            case 0:
                if (TargetPlayer.TryGetValue(pc.PlayerId, out var tar) && Utils.GetPlayerById(tar) != null)
                    AddMsg(string.Format(GetString("TelepathyNotifySelf"), Utils.GetPlayerById(tar).GetRealName(clientData: true)), pc.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Telepathy), GetString("TelepathyTitle")));
                break;

            case 1:
                if (TargetPlayer.TryGetValue(pc.PlayerId, out var rat) && Utils.GetPlayerById(rat) != null)
                {
                    var msg = DetermineSetMessage(pc, pc.GetRealKiller(), out Determinemessage);
                    AddMsg(string.Format(GetString("TelepathyNotifySelfMODE2"), Utils.GetPlayerById(rat).GetRealName(clientData: true), msg), pc.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Telepathy), GetString("TelepathyTitle")));
                }
                break;
        }
    }

    public override string GetSuffixOthers(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
    {
        if (!isForMeeting) return string.Empty;

        var checkTar = TargetPlayer.Where(x => x.Value == seer.PlayerId);
        if (TargetPlayer.TryGetValue(seer.PlayerId, out var tar) && tar == seen.PlayerId) 
        {
            return ColorString(GetRoleColor(CustomRoles.Telepathy), "¿");
        }
        else if (checkTar.Any())
        {
            var Telepathy = Utils.GetPlayerById(checkTar.First().Key);

            return seen == Telepathy ? ColorString(GetRoleColor(CustomRoles.Telepathy), "¿") : "";
        }

        return string.Empty;
    }
    public override string GetProgressText(byte playerId, bool coms)
        => ColorString(AbilityLimit >= 1 ? GetRoleColor(CustomRoles.Telepathy).ShadeColor(0.25f) : Color.gray, $"({AbilityLimit})");
}
