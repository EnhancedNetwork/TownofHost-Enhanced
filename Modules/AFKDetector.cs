
using System;
using UnityEngine;

namespace TOHE.Modules;

public static class AFKDetector
{
    private static OptionItem EnableDetector;
    private static OptionItem ConsequenceOption;
    private static OptionItem MinPlayersToActivate;
    public static OptionItem ActivateOnStart;

    public static readonly Dictionary<byte, Data> PlayerData = [];
    public static readonly HashSet<byte> ExemptedPlayers = [];
    public static readonly HashSet<byte> ShieldedPlayers = [];
    public static readonly HashSet<byte> TempIgnoredPlayers = [];
    public static int NumAFK;

    public static void SetupCustomOption()
    {
        TextOptionItem.Create(100080, "MenuTitle.AFKDetection", TabGroup.SystemSettings)
            .SetColor(new Color32(0, 255, 165, 255))
            .SetHeader(true);

        EnableDetector = BooleanOptionItem.Create(90, "EnableAFKDetector", true, TabGroup.SystemSettings)
            .SetColor(new Color32(0, 255, 165, 255));

        ConsequenceOption = StringOptionItem.Create(91, "AFKConsequence", [.. Enum.GetNames<Consequence>().Select(x => $"AFKConsequence.{x}")], 0, TabGroup.SystemSettings)
            .SetParent(EnableDetector)
            .SetColor(new Color32(0, 255, 165, 255));

        MinPlayersToActivate = IntegerOptionItem.Create(92, "AFKMinPlayersToActivate", new(1, 15, 1), 1, TabGroup.SystemSettings)
            .SetParent(EnableDetector)
            .SetColor(new Color32(0, 255, 165, 255));

        ActivateOnStart = BooleanOptionItem.Create(93, "AFKActivateOnStart", false, TabGroup.SystemSettings)
            .SetParent(EnableDetector)
            .SetColor(new Color32(0, 255, 165, 255));
    }

    public static void RecordPosition(PlayerControl pc)
    {
        if (!EnableDetector.GetBool() || !GameStates.IsInTask || pc == null || ExemptedPlayers.Contains(pc.PlayerId) || Options.CurrentGameMode is CustomGameMode.FFA) return;

        var waitingTime = 10f;
        if (!pc.IsAlive()) waitingTime += 5f;

        PlayerData[pc.PlayerId] = new()
        {
            LastPosition = pc.GetCustomPosition(),
            Timer = waitingTime
        };

        ShieldedPlayers.Remove(pc.PlayerId);
        TempIgnoredPlayers.Remove(pc.PlayerId);
    }

    public static void OnFixedUpdate(PlayerControl pc)
    {
        if (!EnableDetector.GetBool() || !GameStates.IsInTask || ExileController.Instance || Main.AllAlivePlayerControls.Length < MinPlayersToActivate.GetInt() || pc == null || !PlayerData.TryGetValue(pc.PlayerId, out Data data)) return;

        if (Vector2.Distance(pc.GetCustomPosition(), data.LastPosition) > 0.1f && !TempIgnoredPlayers.Contains(pc.PlayerId))
        {
            SetNotAFK(pc.PlayerId);
            return;
        }

        var lastTimer = (int)Math.Round(data.Timer);
        data.Timer -= Time.fixedDeltaTime;
        var currentTimer = (int)Math.Round(data.Timer);

        if (!data.Counted && data.CurrentPhase == Data.Phase.Warning && data.Timer <= 3f && !pc.IsModded())
        {
            NumAFK++;
            data.Counted = true;

            if (Main.AllAlivePlayerControls.Length / 2 <= NumAFK)
            {
                Logger.SendInGame(Translator.GetString("AFKTooMany"));
                PlayerData.Clear();
                Utils.NotifyRoles(ForceLoop: true);
            }
        }

        if (data.Timer <= 0f)
        {
            switch (data.CurrentPhase)
            {
                case Data.Phase.Detection:
                    data.CurrentPhase = Data.Phase.Warning;
                    data.Timer = 15f;
                    Utils.NotifyRoles(SpecifyTarget: pc);
                    if (pc.IsAlive() && !MeetingStates.FirstMeeting) pc.FixBlackScreen();
                    break;
                case Data.Phase.Warning:
                    data.CurrentPhase = Data.Phase.Consequence;
                    HandleConsequence(pc, (Consequence)ConsequenceOption.GetInt());
                    break;
            }
        }

        if (data.CurrentPhase == Data.Phase.Warning && lastTimer != currentTimer)
            Utils.NotifyRoles(SpecifySeer: pc, SpecifyTarget: pc);
    }

    public static void SetNotAFK(byte id)
    {
        try
        {
            if (ExtendedPlayerControl.BlackScreenWaitingPlayers.Contains(id))
                ExtendedPlayerControl.CancelBlackScreenFix.Add(id);

            PlayerData.Remove(id);
            ShieldedPlayers.Remove(id);

            Utils.NotifyRoles(SpecifyTarget: id.GetPlayer());
        }
        catch (Exception e) { Utils.ThrowException(e); }
    }

    public static string GetSuffix(PlayerControl seer, PlayerControl target)
    {
        if (seer.PlayerId == target.PlayerId && seer.IsAlive() && PlayerData.TryGetValue(seer.PlayerId, out Data seerData) && seerData.CurrentPhase > Data.Phase.Detection)
            return seerData.CurrentPhase == Data.Phase.Warning ? string.Format(Translator.GetString("AFKWarning"), (int)Math.Round(seerData.Timer)) : Translator.GetString("AFKSuffix");

        if (target.IsAlive() && PlayerData.TryGetValue(target.PlayerId, out Data targetData) && targetData.CurrentPhase > Data.Phase.Detection)
            return Translator.GetString("AFKSuffix");

        return string.Empty;
    }

    private static void HandleConsequence(PlayerControl pc, Consequence consequence)
    {
        switch (consequence)
        {
            case Consequence.Shield:
                ShieldedPlayers.Add(pc.PlayerId);
                Utils.NotifyRoles(SpecifyTarget: pc);
                break;
            case Consequence.Kick:
                AmongUsClient.Instance.KickPlayer(pc.OwnerId, false);
                Logger.SendInGame(string.Format(Translator.GetString("AFKKick"), pc.PlayerId.GetPlayerName()));
                break;
            case Consequence.Suicide:
                pc.SetDeathReason(PlayerState.DeathReason.AFK);
                pc.RpcMurderPlayer(pc);
                break;
            case Consequence.Nothing:
                Utils.NotifyRoles(SpecifyTarget: pc);
                break;
        }

        PlayerData.Remove(pc.PlayerId);
    }

    private enum Consequence
    {
        Nothing,
        Shield,
        Suicide,
        Kick
    }

    public class Data
    {
        public enum Phase
        {
            Detection,
            Warning,
            Consequence
        }

        public Vector2 LastPosition { get; init; }
        public float Timer { get; set; }
        public Phase CurrentPhase { get; set; }
        public bool Counted { get; set; }
    }
}