using AmongUs.GameOptions;
using UnityEngine;


namespace TOHE;

internal static class CopsAndRobbersManager
{
    public static readonly HashSet<byte> cops = [];
    public static readonly HashSet<byte> robbers = [];
    public static readonly HashSet<byte> captured = [];
    private static readonly Dictionary<byte, int> capturedScore = [];
    private static readonly Dictionary<byte, int> timesCaptured = [];
    private static readonly Dictionary<byte, int> points = [];
    private static readonly Dictionary<byte, float> defaultSpeed = [];
    public static int numCops;

    public static OptionItem CandR_NumCops;
    public static OptionItem CandR_NotifyRobbersWhenCaptured;
    public static OptionItem CandR_InitialPoints;
    public static OptionItem CandR_PointsForCapture;
    public static OptionItem CandR_PointsForRescue;


    public static void SetupCustomOption()
    {
        CandR_NumCops = IntegerOptionItem.Create(67_224_002, "C&R_NumCops", new(1, 5, 1), 2, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.CandR)
            .SetColor(new Color32(0, 0, 200, byte.MaxValue))
            .SetHeader(true);
        CandR_NotifyRobbersWhenCaptured = BooleanOptionItem.Create(67_224_003, "C&R_NotifyRobbersWhenCaptured", true, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.CandR)
            .SetColor(new Color32(0, 0, 200, byte.MaxValue));
        CandR_InitialPoints = IntegerOptionItem.Create(67_224_005, "C&R_InitialPoints", new(0, 50, 1), 3, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.CandR)
            .SetColor(new Color32(0, 0, 200, byte.MaxValue));
        CandR_PointsForCapture = IntegerOptionItem.Create(67_224_006, "C&R_PointsForCapture", new(0, 5, 1), 1, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.CandR)
            .SetColor(new Color32(0, 0, 200, byte.MaxValue));
        CandR_PointsForRescue = IntegerOptionItem.Create(67_224_007, "C&R_PointsForRescue", new(0, 5, 1), 1, TabGroup.ModSettings, false)
            .SetGameMode(CustomGameMode.CandR)
            .SetColor(new Color32(0, 0, 200, byte.MaxValue));
    }

    public enum RoleType
    {
        Cop,
        Robber,
        Captured
    }
    public static RoleTypes RoleBase(CustomRoles role)
    {
        return role switch
        {
            CustomRoles.Cop => RoleTypes.Shapeshifter,
            CustomRoles.Robber => RoleTypes.Engineer,
            _ => RoleTypes.Engineer
        };
    }
    public static bool HasTasks(CustomRoles role)
    {
        return role switch
        {
            CustomRoles.Cop => false,
            CustomRoles.Robber => true,
            _ => false,
        };
    }
    public static void Init()
    {
        if (Options.CurrentGameMode != CustomGameMode.CandR) return;

        cops.Clear();
        robbers.Clear();
        captured.Clear();
        capturedScore.Clear();
        timesCaptured.Clear();
        points.Clear();
        numCops = CandR_NumCops.GetInt();
        defaultSpeed.Clear();
    }
    public static Dictionary<byte, CustomRoles> SetRoles()
    {
        Logger.Warn("---- Started SetRoles c&r ----", "SetRoles");
        Dictionary<byte, CustomRoles> finalRoles = [];
        var random = IRandom.Instance;
        List<PlayerControl> AllPlayers = Main.AllPlayerControls.Shuffle(random).ToList();

        if (Main.EnableGM.Value)
        {
            finalRoles[PlayerControl.LocalPlayer.PlayerId] = CustomRoles.GM;
            AllPlayers.Remove(PlayerControl.LocalPlayer);
        }

        int optImpNum = numCops;
        foreach (PlayerControl pc in AllPlayers)
        {
            if (pc == null) continue;
            if (optImpNum > 0)
            {
                finalRoles[pc.PlayerId] = CustomRoles.Cop;
                RoleType.Cop.Add(pc.PlayerId);
                optImpNum--;
            }
            else
            {
                finalRoles[pc.PlayerId] = CustomRoles.Robber;
                RoleType.Robber.Add(pc.PlayerId);
            }
            Logger.Warn($"set role for {pc.PlayerId}: {finalRoles[pc.PlayerId]}", "SetRoles");
        }
        Logger.Warn("---- finished SetRoles c&r ----", "SetRoles");

        return finalRoles;
    }
    private static void Add(this RoleType role, byte playerId)
    {
        points[playerId] = CandR_InitialPoints.GetInt();
        defaultSpeed[playerId] = Main.AllPlayerSpeed[playerId];
        role.SetCostume(playerId: playerId);

        switch (role)
        {
            case RoleType.Cop:
                cops.Add(playerId);
                capturedScore[playerId] = 0;
                return;

            case RoleType.Robber:
                robbers.Add(playerId);
                timesCaptured[playerId] = 0;
                return;
        }
    }
    private static void AddCaptured(this PlayerControl robber)
    {
        captured.Add(robber.PlayerId);
        RoleType.Captured.SetCostume(playerId: robber.PlayerId);
        Main.AllPlayerSpeed[robber.PlayerId] = Main.MinSpeed;
        robber?.MarkDirtySettings();
    }
    private static void RemoveCaptured(this PlayerControl rescued)
    {
        captured.Remove(rescued.PlayerId);
        RoleType.Robber.SetCostume(playerId: rescued.PlayerId); //for robber
        Main.AllPlayerSpeed[rescued.PlayerId] = defaultSpeed[rescued.PlayerId];
        rescued?.MarkDirtySettings();
    }

    private static void SetCostume(this RoleType opMode, byte playerId)
    {
        if (playerId == byte.MaxValue) return;
        PlayerControl player = Utils.GetPlayerById(playerId);
        if (player == null) return;

        switch (opMode)
        {
            case RoleType.Cop:
                player.RpcSetColor(1); //blue
                player.RpcSetHat("hat_police");
                player.RpcSetSkin("skin_Police");
                break;

            case RoleType.Robber:
                player.RpcSetColor(6); //black
                player.RpcSetHat("hat_pk04_Vagabond");
                player.RpcSetSkin("skin_None");
                break;

            case RoleType.Captured:
                player.RpcSetColor(5); //yellow
                player.RpcSetHat("hat_tombstone");
                player.RpcSetSkin("skin_prisoner");
                player.RpcSetVisor("visor_pk01_DumStickerVisor");
                break;
        }
    }

    public static void OnCopAttack(PlayerControl cop, PlayerControl robber)
    {
        if (cop == null || robber == null || Options.CurrentGameMode != CustomGameMode.CandR) return;
        if (!cop.Is(CustomRoles.Cop) || !robber.Is(CustomRoles.Robber)) return;

        if (captured.Contains(robber.PlayerId))
        {
            cop.Notify("C&R_AlreadyCaptured");
            return;
        }
        if (robber.inVent)
        {
            Logger.Info($"Robber, playerID {robber.PlayerId}, is in a vent, capture blocked", "C&R");
            return;
        }

        robber.AddCaptured();

        if (CandR_NotifyRobbersWhenCaptured.GetBool())
        {
            foreach (byte pid in robbers)
            {
                if (pid == byte.MaxValue) continue;
                PlayerControl pc = Utils.GetPlayerById(pid);
                pc?.KillFlash();
            }
        }

        if (!capturedScore.ContainsKey(cop.PlayerId)) capturedScore[cop.PlayerId] = 0;
        capturedScore[cop.PlayerId]++;

        if (!timesCaptured.ContainsKey(robber.PlayerId)) timesCaptured[robber.PlayerId] = 0;
        timesCaptured[robber.PlayerId]++;

        if (!points.ContainsKey(cop.PlayerId)) points[cop.PlayerId] = CandR_InitialPoints.GetInt();
        points[cop.PlayerId] += CandR_PointsForCapture.GetInt();

        cop.ResetKillCooldown();
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
    class FixedUpdateInGameModeCandRPatch
    {
        public static void Postfix()
        {
            if (!GameStates.IsInTask || Options.CurrentGameMode != CustomGameMode.CandR) return;

            if (!AmongUsClient.Instance.AmHost) return;

            captured.Remove(byte.MaxValue);
            if (!captured.Any()) return;

            robbers.Remove(byte.MaxValue);


            Dictionary<byte, byte> toRemove = [];
            foreach (byte capturedId in captured)
            {
                PlayerControl capturedPC = Utils.GetPlayerById(capturedId);
                if (capturedPC == null) continue;

                var capturedPos = capturedPC.transform.position;

                foreach (byte robberId in robbers)
                {
                    if (captured.Contains(robberId)) continue;
                    PlayerControl robberPC = Utils.GetPlayerById(robberId);
                    if (robberPC == null) continue;

                    float dis = Vector2.Distance(capturedPos, robberPC.transform.position);
                    if (dis < 0.3f)
                    {
                        toRemove[capturedId] = robberId;
                        Logger.Info($"to remove cap {capturedId}, rob: {robberId}", "to Remove fixupdate");
                        break;
                    }
                }
            }

            if (!toRemove.Any()) return;
            foreach ((byte rescued, byte saviour) in toRemove)
            {
                if (!points.ContainsKey(saviour)) points[saviour] = CandR_InitialPoints.GetInt();
                points[saviour] += CandR_PointsForRescue.GetInt();

                Utils.GetPlayerById(rescued).RemoveCaptured();
            }
        }
    }
}