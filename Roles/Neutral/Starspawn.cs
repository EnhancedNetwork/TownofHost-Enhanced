using Hazel;
using TOHE.Modules.Rpc;
using TOHE.Roles.Core;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;
using TOHE.Modules.ChatManager;
using TOHE.Patches;

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

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Starspawn);
        IsolateCooldown = FloatOptionItem.Create(Id + 10, "Starspawn.IsolateCooldown", new(0f, 180f, 2.5f), 30f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Starspawn])
            .SetValueFormat(OptionFormat.Seconds);
    }

    // "db|daybreak"
    public static void DaybreakCommand(PlayerControl pc, string commandKey, string msg, string[] args)
    {
        if (!AmongUsClient.Instance.AmHost)
        {
            ChatCommands.RequestCommandProcessingFromHost(msg, commandKey);
            return;
        }

        if (!GameStates.IsMeeting || pc == null || GameStates.IsExilling) return;
        if (!pc.Is(CustomRoles.Starspawn)) return;

        if (!pc.IsAlive())
        {
            pc.ShowInfoMessage(false, GetString("DaybreakDead"));
            return;
        }

        if (!HasDaybreak[pc.PlayerId] && !Main.Daybreak)
        {
            Main.Daybreak = true;
            HasDaybreak[pc.PlayerId] = false;
            return;
        }
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

    private void SendRPC(byte playerId)
    {
        var writer = MessageWriter.Get(SendOption.Reliable);

        writer.Write(playerId);
        writer.Write(HasDaybreak.ContainsKey(playerId) && HasDaybreak[playerId]);

        writer.Write(Isolated.ContainsKey(playerId) ? Isolated[playerId] ?? byte.MaxValue : byte.MaxValue);

        if (VisiterList.ContainsKey(playerId))
        {
            writer.Write(VisiterList[playerId].Count);
            foreach (var visRole in VisiterList[playerId])
            {
                writer.Write((int)visRole);
            }
        }
        else
            writer.Write(0);

        RpcUtils.LateBroadcastReliableMessage(new RpcSyncRoleSkill(PlayerControl.LocalPlayer.NetId, _Player.NetId, writer));
    }
    public override void ReceiveRPC(MessageReader reader, PlayerControl NaN)
    {
        var playerId = reader.ReadByte();

        HasDaybreak[playerId] = reader.ReadBoolean();

        var isolate = reader.ReadByte();
        Isolated[playerId] = isolate != byte.MaxValue ? isolate : null;

        var visiterCount = reader.ReadInt16();

        if (!VisiterList.ContainsKey(playerId))
            VisiterList[playerId] = [];
        
        for (int i = 0; i < visiterCount; i++)
        {
            VisiterList[playerId].Add((CustomRoles)reader.ReadInt32());
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

            SendRPC(star);
        }

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

            SendRPC(killer.PlayerId);
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

        VisiterList[_Player.PlayerId].Clear();

        string separator = TranslationController.Instance.currentLanguage.languageID is SupportedLangs.English or SupportedLangs.Russian ? "], [" : "】, 【";

        string msg = $"{separator[^1]}{string.Join(separator, visiters)}{separator[0]}";

        SendMessage(GetString("Starspawn.VisitersMsg") + msg, sendTo: _Player.PlayerId, addToHistory: true);

        SendRPC(_Player.PlayerId);
    }

    public override void SetAbilityButtonText(HudManager hud, byte id)
    {
        hud.KillButton.OverrideText(GetString("Starspawn.Isolate"));
    }
}
