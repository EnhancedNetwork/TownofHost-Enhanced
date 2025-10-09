using Hazel;
using TOHE.Modules.Rpc;
using TOHE.Roles.Core;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;
using TOHE.Modules.ChatManager;

namespace TOHE.Roles.Neutral;

internal class Starspawn : PariahManager
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Starspawn;
    private const int Id = 32500;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Starspawn);
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralPariah;
    //==================================================================\\
    private static readonly Dictionary<byte, bool> HasDaybreak = [];
    private static readonly Dictionary<byte, byte?> Isolated = [];
    private static readonly Dictionary<byte, HashSet<CustomRoles>> VisiterList = [];

    private static OptionItem IsolateCooldown;
    private static OptionItem TryHideMsg;

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Starspawn);
        IsolateCooldown = FloatOptionItem.Create(Id + 10, "Starspawn.IsolateCooldown", new(0f, 180f, 2.5f), 30f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Starspawn])
            .SetValueFormat(OptionFormat.Seconds);
        TryHideMsg = BooleanOptionItem.Create(Id + 5, "StarspawnTryHideMsg", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Starspawn]);
    }

    public bool DaybreakMessage(PlayerControl pc, string msg)
    {
        var originMsg = msg;

        if (!AmongUsClient.Instance.AmHost) return false;
        if (!GameStates.IsMeeting || _Player == null || GameStates.IsExilling) return false;

        msg = msg.ToLower().TrimStart().TrimEnd();

        if (!CheckCommond(ref msg, "db|daybreak", false)) return false;

        if (!pc.IsAlive())
        {
            pc.ShowInfoMessage(false, GetString("DaybreakDead"));
            return true;
        }

        if (TryHideMsg.GetBool())
        {
            GuessManager.TryHideMsg();
            ChatManager.SendPreviousMessagesToAll();
        }
        else if (pc.AmOwner) SendMessage(originMsg, 255, pc.GetRealName());

        if (!HasDaybreak[_Player.PlayerId] && !Main.Daybreak)
        {
            Main.Daybreak = true;
            HasDaybreak[_Player.PlayerId] = false;
            return true;
        }

        return true;
    }

    public override void AfterMeetingTasks()
    {
        if (Main.Daybreak) Main.Daybreak = false;
    }

    public override void Init()
    {
        HasDaybreak.Clear();
        Isolated.Clear();
        VisiterList.Clear();
    }

    public override void Add(byte playerId)
    {
        HasDaybreak[playerId] = true;
        Isolated[playerId] = null;
        VisiterList[playerId] = [];
    }

    public override void Remove(byte playerId)
    {
        HasDaybreak.Remove(playerId);
        Isolated.Remove(playerId);
        VisiterList.Remove(playerId);
    }

    private void SendRPC()
    {
        var writer = MessageWriter.Get(SendOption.Reliable);

        writer.Write(HasDaybreak.Count);

        foreach (var daybreak in HasDaybreak)
        {
            writer.Write(daybreak.Key);
            writer.Write(daybreak.Value);
        }

        writer.Write(Isolated.Count);

        foreach (var isol in Isolated)
        {
            writer.Write(isol.Key);
            writer.Write(isol.Value ?? byte.MaxValue);
        }

        writer.Write(VisiterList.Count);

        foreach (var visList in VisiterList)
        {
            writer.Write(visList.Key);
            writer.Write(visList.Value.Count);
            foreach (var visRole in visList.Value)
            {
                writer.Write((int)visRole);
            }
        }

        RpcUtils.LateBroadcastReliableMessage(new RpcSyncRoleSkill(PlayerControl.LocalPlayer.NetId, _Player.NetId, writer));
    }
    public override void ReceiveRPC(MessageReader reader, PlayerControl NaN)
    {
        HasDaybreak.Clear();
        Isolated.Clear();
        VisiterList.Clear();

        var daybreakCount = reader.ReadInt16();

        for (int i = 0; i < daybreakCount; i++)
        {
            var key = reader.ReadByte();
            var value = reader.ReadBoolean();
            HasDaybreak[key] = value;
        }

        var isolatedCount = reader.ReadInt16();

        for (int i = 0; i < isolatedCount; i++)
        {
            var key = reader.ReadByte();
            var value = reader.ReadByte();
            Isolated[key] = value != byte.MaxValue ? value : null;
        }

        var visiterListCount = reader.ReadInt16();

        for (int i = 0; i < visiterListCount; i++)
        {
            var key = reader.ReadByte();
            HashSet<CustomRoles> value = [];

            var visListCount = reader.ReadInt16();
            for (int j = 0; j < visListCount; j++)
            {
                value.Add((CustomRoles)reader.ReadInt16());
            }

            VisiterList[key] = value;
        }
    }

    public override bool CheckMurderOnOthersTarget(PlayerControl killer, PlayerControl target)
    {
        if (!HasEnabled) return false;

        foreach (var isolated in Isolated)
        {
            if (isolated.Value != target.PlayerId) continue;

            var star = isolated.Key;

            if (!killer.IsPlayerCrewmateTeam() && !star.GetPlayer().IsPlayerCrewmateTeam()) continue;

            VisiterList[star].Add(killer.GetCustomRole());
        }

        SendRPC();

        return false;
    }

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = IsolateCooldown.GetFloat();
    public override bool CanUseKillButton(PlayerControl pc) => !Isolated.TryGetValue(pc.PlayerId, out byte? target) || target == null;
    public override bool ForcedCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (killer.PlayerId == target.PlayerId) return true;

        if (!(Isolated.TryGetValue(killer.PlayerId, out byte? isol) && isol != null))
        {
            killer.ResetKillCooldown();
            killer.SetKillCooldown();
            killer.RpcGuardAndKill(target);

            Isolated[killer.PlayerId] = target.PlayerId;

            SendRPC();
        }

        return false;
    }

    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        Isolated[_Player.PlayerId] = null;

        List<string> visiters = [];

        foreach (var visiter in VisiterList[_Player.PlayerId])
        {
            visiters.Add(visiter.GetColoredTextByRole(visiter.GetActualRoleName()));
        }

        string separator = TranslationController.Instance.currentLanguage.languageID is SupportedLangs.English or SupportedLangs.Russian ? "], [" : "】, 【";

        string msg = $"{separator[^1]}{string.Join(separator, visiters)}{separator[0]}";

        SendMessage(GetString("Starspawn.VisitersMsg") + msg, sendTo: _Player.PlayerId);
    }

    public static bool CheckCommond(ref string msg, string command, bool exact = true)
    {
        var comList = command.Split('|');
        for (int i = 0; i < comList.Length; i++)
        {
            if (exact)
            {
                if (msg == "/" + comList[i]) return true;
            }
            else
            {
                if (msg.StartsWith("/" + comList[i]))
                {
                    msg = msg.Replace("/" + comList[i], string.Empty);
                    return true;
                }
            }
        }
        return false;
    }

    public override void SetAbilityButtonText(HudManager hud, byte id)
    {
        hud.KillButton.OverrideText(GetString("Starspawn.Isolate"));
    }
}
