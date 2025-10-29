using System;
using System.Text;
using TMPro;
using TOHE.Roles.AddOns.Common;
using TOHE.Roles.Core;
using TOHE.Roles.Impostor;
using UnityEngine;
using static TOHE.Translator;

namespace TOHE;

[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
class HudManagerUpdatePatch
{
    public static bool ShowDebugText = false;
    public static int LastCallNotifyRolesPerSecond = 0;
    public static int NowCallNotifyRolesCount = 0;
    public static int LastSetNameDesyncCount = 0;
    public static int LastFPS = 0;
    public static int NowFrameCount = 0;
    public static float FrameRateTimer = 0.0f;
    public static TextMeshPro LowerInfoText;
    public static GameObject TempLowerInfoText;
    public static void Postfix(HudManager __instance)
    {
        if (!GameStates.IsModHost || __instance == null) return;

        var player = PlayerControl.LocalPlayer;
        if (player == null) return;
        //壁抜け
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            if ((!AmongUsClient.Instance.IsGameStarted || !GameStates.IsOnlineGame)
                && player.CanMove)
            {
                player.Collider.offset = new Vector2(0f, 127f);
            }
        }
        //壁抜け解除
        if (player.Collider.offset.y == 127f)
        {
            if (!Input.GetKey(KeyCode.LeftControl) || (AmongUsClient.Instance.IsGameStarted && GameStates.IsOnlineGame))
            {
                player.Collider.offset = new Vector2(0f, -0.3636f);
            }
        }

        if (!AmongUsClient.Instance.IsGameStarted || GameStates.IsHideNSeek) return;

        Utils.CountAlivePlayers(sendLog: false, checkGameEnd: false);

        if (SetHudActivePatch.IsActive)
        {
            if (Options.CurrentGameMode == CustomGameMode.FFA)
            {
                if (LowerInfoText == null)
                {
                    TempLowerInfoText = new GameObject("CountdownText");
                    TempLowerInfoText.transform.position = new Vector3(0f, -2f, 1f);
                    LowerInfoText = TempLowerInfoText.AddComponent<TextMeshPro>();
                    LowerInfoText.alignment = TextAlignmentOptions.Center;
                    LowerInfoText.transform.parent = __instance.transform;
                    LowerInfoText.transform.localPosition = new Vector3(0, -2f, 0);
                    LowerInfoText.overflowMode = TextOverflowModes.Overflow;
                    LowerInfoText.enableWordWrapping = false;
                    LowerInfoText.color = Color.white;
                    LowerInfoText.outlineColor = Color.black;
                    LowerInfoText.outlineWidth = 20000000f;
                    LowerInfoText.fontSize = 2f;
                }
                LowerInfoText.text = FFAManager.GetHudText();
            }
            else if (Options.CurrentGameMode == CustomGameMode.CandR)
            {
                if (LowerInfoText == null)
                {
                    TempLowerInfoText = new GameObject("CountdownText");
                    TempLowerInfoText.transform.position = new Vector3(0f, -2f, 1f);
                    LowerInfoText = TempLowerInfoText.AddComponent<TextMeshPro>();
                    LowerInfoText.alignment = TextAlignmentOptions.Center;
                    LowerInfoText.transform.parent = __instance.transform;
                    LowerInfoText.transform.localPosition = new Vector3(0, -2f, 0);
                    LowerInfoText.overflowMode = TextOverflowModes.Overflow;
                    LowerInfoText.enableWordWrapping = false;
                    LowerInfoText.color = Color.white;
                    LowerInfoText.outlineColor = Color.black;
                    LowerInfoText.outlineWidth = 20000000f;
                    LowerInfoText.fontSize = 2f;
                }
                LowerInfoText.text = CopsAndRobbersManager.GetHudText();
            }
            else if (Options.CurrentGameMode == CustomGameMode.UltimateTeam)
            {
                if (LowerInfoText == null)
                {
                    TempLowerInfoText = new GameObject("CountdownText");
                    TempLowerInfoText.transform.position = new Vector3(0f, -2f, 1f);
                    LowerInfoText = TempLowerInfoText.AddComponent<TextMeshPro>();
                    LowerInfoText.alignment = TextAlignmentOptions.Center;
                    LowerInfoText.transform.parent = __instance.transform;
                    LowerInfoText.transform.localPosition = new Vector3(0, -2f, 0);
                    LowerInfoText.overflowMode = TextOverflowModes.Overflow;
                    LowerInfoText.enableWordWrapping = false;
                    LowerInfoText.color = Color.white;
                    LowerInfoText.outlineColor = Color.black;
                    LowerInfoText.outlineWidth = 20000000f;
                    LowerInfoText.fontSize = 2f;
                }
                LowerInfoText.text = UltimateTeam.GetHudText();
            }else if (Options.CurrentGameMode == CustomGameMode.TrickorTreat)
            {
                if (LowerInfoText == null)
                {
                    TempLowerInfoText = new GameObject("CountdownText");
                    TempLowerInfoText.transform.position = new Vector3(0f, -2f, 1f);
                    LowerInfoText = TempLowerInfoText.AddComponent<TextMeshPro>();
                    LowerInfoText.alignment = TextAlignmentOptions.Center;
                    LowerInfoText.transform.parent = __instance.transform;
                    LowerInfoText.transform.localPosition = new Vector3(0, -2f, 0);
                    LowerInfoText.overflowMode = TextOverflowModes.Overflow;
                    LowerInfoText.enableWordWrapping = false;
                    LowerInfoText.color = Color.white;
                    LowerInfoText.outlineColor = Color.black;
                    LowerInfoText.outlineWidth = 20000000f;
                    LowerInfoText.fontSize = 2f;
                }
                LowerInfoText.text = TrickorTreat.GetHudText();
            }

            if (player.IsAlive())
            {
                // Set default
                __instance.KillButton?.OverrideText(GetString("KillButtonText"));
                __instance.ReportButton?.OverrideText(GetString("ReportButtonText"));
                __instance.SabotageButton?.OverrideText(GetString("SabotageButtonText"));

                player.GetRoleClass()?.SetAbilityButtonText(__instance, player.PlayerId);
                if (Options.CurrentGameMode is CustomGameMode.CandR)
                    CopsAndRobbersManager.SetAbilityButtonText(__instance, player.PlayerId);
                // Set lower info text for modded players
                if (LowerInfoText == null)
                {
                    LowerInfoText = UnityEngine.Object.Instantiate(__instance.KillButton.cooldownTimerText, __instance.transform, true);
                    LowerInfoText.alignment = TextAlignmentOptions.Center;
                    LowerInfoText.transform.localPosition = new(0, -2f, 0);
                    LowerInfoText.overflowMode = TextOverflowModes.Overflow;
                    LowerInfoText.enableWordWrapping = false;
                    LowerInfoText.color = Color.white;
                    LowerInfoText.fontSize = LowerInfoText.fontSizeMax = LowerInfoText.fontSizeMin = 2.8f;
                }
                switch (Options.CurrentGameMode)
                {
                    case CustomGameMode.Standard:
                        var roleClass = player.GetRoleClass();
                        LowerInfoText.text = roleClass?.GetLowerText(player, player, isForMeeting: Main.MeetingIsStarted, isForHud: true) ?? string.Empty;

                        LowerInfoText.text += "\n" + Spurt.GetSuffix(player, true, isformeeting: Main.MeetingIsStarted);
                        break;
                }

                LowerInfoText.enabled = LowerInfoText.text != "" && LowerInfoText.text != string.Empty;

                if ((!AmongUsClient.Instance.IsGameStarted && AmongUsClient.Instance.NetworkMode != NetworkModes.FreePlay) || GameStates.IsMeeting)
                {
                    LowerInfoText.enabled = false;
                }

                if (player.CanUseKillButton())
                {
                    __instance.KillButton.ToggleVisible(player.IsAlive() && GameStates.IsInTask);
                    player.Data.Role.CanUseKillButton = true;
                }
                else
                {
                    __instance.KillButton.SetDisabled();
                    __instance.KillButton.ToggleVisible(false);
                }

                __instance.ImpostorVentButton.ToggleVisible(player.CanUseImpostorVentButton());
                player.Data.Role.CanVent = player.CanUseVents();

                // Sometimes Sabotage button was visible for non-host modded clients
                if (!AmongUsClient.Instance.AmHost && !player.CanUseSabotage())
                    __instance.SabotageButton.Hide();
            }
            else
            {
                __instance.ReportButton.Hide();
                __instance.ImpostorVentButton.Hide();
                __instance.KillButton.Hide();
                __instance.AbilityButton.Show();
                __instance.AbilityButton.OverrideText(GetString(StringNames.HauntAbilityName));
            }
        }


        if (Input.GetKeyDown(KeyCode.Y) && AmongUsClient.Instance.NetworkMode == NetworkModes.FreePlay)
        {
            __instance.ToggleMapVisible(new MapOptions()
            {
                Mode = MapOptions.Modes.Sabotage,
                AllowMovementWhileMapOpen = true
            });
            if (player.AmOwner)
            {
                player.MyPhysics.inputHandler.enabled = true;
                ConsoleJoystick.SetMode_Task();
            }
        }

        if (AmongUsClient.Instance.NetworkMode == NetworkModes.OnlineGame) RepairSender.enabled = false;
        if (Input.GetKeyDown(KeyCode.RightShift) && AmongUsClient.Instance.NetworkMode != NetworkModes.OnlineGame)
        {
            RepairSender.enabled = !RepairSender.enabled;
            RepairSender.Reset();
        }
        if (RepairSender.enabled && AmongUsClient.Instance.NetworkMode != NetworkModes.OnlineGame)
        {
            if (Input.GetKeyDown(KeyCode.Alpha0)) RepairSender.Input(0);
            if (Input.GetKeyDown(KeyCode.Alpha1)) RepairSender.Input(1);
            if (Input.GetKeyDown(KeyCode.Alpha2)) RepairSender.Input(2);
            if (Input.GetKeyDown(KeyCode.Alpha3)) RepairSender.Input(3);
            if (Input.GetKeyDown(KeyCode.Alpha4)) RepairSender.Input(4);
            if (Input.GetKeyDown(KeyCode.Alpha5)) RepairSender.Input(5);
            if (Input.GetKeyDown(KeyCode.Alpha6)) RepairSender.Input(6);
            if (Input.GetKeyDown(KeyCode.Alpha7)) RepairSender.Input(7);
            if (Input.GetKeyDown(KeyCode.Alpha8)) RepairSender.Input(8);
            if (Input.GetKeyDown(KeyCode.Alpha9)) RepairSender.Input(9);
            if (Input.GetKeyDown(KeyCode.Return)) RepairSender.InputEnter();
        }
    }
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.ToggleHighlight))]
class ToggleHighlightPatch
{
    public static void Postfix(PlayerControl __instance /*, [HarmonyArgument(0)] bool active, [HarmonyArgument(1)] RoleTeamTypes team*/)
    {
        if (GameStates.IsHideNSeek) return;

        var player = PlayerControl.LocalPlayer;
        if (!GameStates.IsInTask) return;

        if (player.CanUseKillButton())
        {
            __instance.cosmetics.currentBodySprite.BodySprite.material.SetColor("_OutlineColor", Utils.GetRoleColor(player.GetCustomRole()));
        }
    }
}
[HarmonyPatch(typeof(Vent), nameof(Vent.SetOutline))]
class SetVentOutlinePatch
{
    public static void Postfix(Vent __instance, [HarmonyArgument(1)] ref bool mainTarget)
    {
        if (GameStates.IsHideNSeek) return;

        var player = PlayerControl.LocalPlayer;
        Color color = player.GetRoleColor();
        __instance.myRend.material.SetColor("_OutlineColor", color);
        __instance.myRend.material.SetColor("_AddColor", mainTarget ? color : Color.clear);
    }
}
[HarmonyPatch(typeof(HudManager), nameof(HudManager.SetHudActive))]
[HarmonyPatch([typeof(PlayerControl), typeof(RoleBehaviour), typeof(bool)])]
class SetHudActivePatch
{
    public static bool IsActive = false;
    public static void Postfix(HudManager __instance, [HarmonyArgument(0)] PlayerControl player, [HarmonyArgument(2)] bool isActive)
    {
        // Fix vanilla bug when Report button displayed in the lobby
        __instance.ReportButton.ToggleVisible(!GameStates.IsLobby && isActive);

        if (!GameStates.IsModHost || GameStates.IsHideNSeek) return;

        IsActive = isActive;

        if (GameStates.IsLobby || !isActive) return;
        if (player == null) return;

        if (player.Is(CustomRoles.Oblivious) || Blinder.BlindedPlayers.Contains(player) || player.Is(CustomRoles.KillingMachine))
            __instance.ReportButton.ToggleVisible(false);

        if (player.Is(CustomRoles.Mare) && !Utils.IsActive(SystemTypes.Electrical))
            __instance.KillButton.ToggleVisible(false);

        // Check Toggle visible
        __instance.KillButton.ToggleVisible(player.CanUseKillButton());
        __instance.ImpostorVentButton.ToggleVisible(player.CanUseImpostorVentButton());
        __instance.SabotageButton.ToggleVisible(player.CanUseSabotage());
    }
}
[HarmonyPatch(typeof(VentButton), nameof(VentButton.DoClick))]
class VentButtonDoClickPatch
{
    public static bool Prefix(VentButton __instance)
    {
        if (GameStates.IsHideNSeek) return true;

        var pc = PlayerControl.LocalPlayer;
        {
            if (!pc.Is(CustomRoles.Swooper) || !pc.Is(CustomRoles.Wraith) || !pc.Is(CustomRoles.Chameleon) || pc.inVent || __instance.currentTarget == null || !pc.CanMove || !__instance.isActiveAndEnabled) return true;
            pc?.MyPhysics?.RpcEnterVent(__instance.currentTarget.Id);
            return false;
        }
    }
}
[HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.Show))]
class MapBehaviourShowPatch
{
    public static void Prefix(ref MapOptions opts)
    {
        if (GameStates.IsMeeting || GameStates.IsHideNSeek) return;

        if (opts.Mode is MapOptions.Modes.Normal or MapOptions.Modes.Sabotage)
        {
            var player = PlayerControl.LocalPlayer;

            if (player.CanUseSabotage())
                opts.Mode = MapOptions.Modes.Sabotage;
            else
                opts.Mode = MapOptions.Modes.Normal;
        }
    }
}
[HarmonyPatch(typeof(TaskPanelBehaviour), nameof(TaskPanelBehaviour.SetTaskText))]
class TaskPanelBehaviourPatch
{
    private static readonly StringBuilder sb = new();
    private static readonly StringBuilder sbFinal = new();
    public static void Postfix(TaskPanelBehaviour __instance)
    {
        if (!GameStates.IsModHost) return;
        if (GameStates.IsLobby) return;

        if (GameStates.IsHideNSeek)
        {
            __instance.open = false;
            return;
        }

        PlayerControl player = PlayerControl.LocalPlayer;

        var taskText = __instance.taskText.text;
        if (taskText == "None") return;

        if (player == null) return;

        // Display Description
        if (!player.GetCustomRole().IsVanilla())
        {
            sb.Clear();
            sb.Append($"{player.GetDisplayRoleAndSubName(player, false)}:\r\n");
            sb.Append(player.GetRoleInfo());

            var AllText = Utils.ColorString(player.GetRoleColor(), sb.ToString());

            sb.Clear();
            sb.Append(AllText);

            switch (Options.CurrentGameMode)
            {
                case CustomGameMode.Standard:

                    var lines = taskText.Split("\r\n</color>\n")[0].Split("\r\n\n")[0].Split("\r\n");
                    StringBuilder sb2 = new();
                    foreach (var eachLine in lines)
                    {
                        var line = eachLine.Trim();
                        if ((line.StartsWith("<color=#ff1919ff>") || line.StartsWith("<color=#ff0000ff>")) && sb2.Length < 1 && !line.Contains('(')) continue;
                        sb2.Append(line + "\r\n");
                    }

                    if (sb2.Length > 1)
                    {
                        var text = sb2.ToString().TrimEnd('\n').TrimEnd('\r');
                        if (!Utils.HasTasks(player.Data, false) && sb2.ToString().Any(s => s == '\n'))
                            text = $"{Utils.ColorString(Utils.GetRoleColor(player.GetCustomRole()).ShadeColor(0.2f), GetString("FakeTask"))}\r\n{text}";
                        sb.Append($"\r\n\r\n<size=85%>{text}</size>");
                    }

                    if (MeetingStates.FirstMeeting)
                    {
                        sb.Append($"\r\n\r\n</color><size=70%>{GetString("PressF1ShowMainRoleDes")}");
                        if (Main.PlayerStates.TryGetValue(PlayerControl.LocalPlayer.PlayerId, out var ps) && ps.SubRoles.Count >= 1)
                            sb.Append($"\r\n{GetString("PressF2ShowAddRoleDes")}");
                        sb.Append($"\r\n{GetString("PressF3ShowRoleSettings")}");
                        if (ps.SubRoles.Count >= 1)
                            sb.Append($"\r\n{GetString("PressF4ShowAddOnsSettings")}");
                        sb.Append("</size>");
                    }
                    sbFinal.Clear();
                    sbFinal.Append(sb);
                    break;
                case CustomGameMode.FFA:
                    Dictionary<byte, string> SummaryText2 = [];
                    foreach (var id in Main.PlayerStates.Keys)
                    {
                        string name = Main.AllPlayerNames[id].RemoveHtmlTags().Replace("\r\n", string.Empty);
                        string summary = $"{Utils.GetProgressText(id)}  {Utils.ColorString(Main.PlayerColors[id], name)}";
                        if (Utils.GetProgressText(id).Trim() == string.Empty) continue;
                        SummaryText2[id] = summary;
                    }

                    List<(int, byte)> list2 = [];
                    foreach (var id in Main.PlayerStates.Keys) list2.Add((FFAManager.GetRankOfScore(id), id));
                    list2.Sort();
                    foreach (var id in list2.Where(x => SummaryText2.ContainsKey(x.Item2))) sb.Append("\r\n" + SummaryText2[id.Item2]);

                    sbFinal.Clear();
                    sbFinal.Append($"<size=70%>{sb}</size>");
                    break;
                case CustomGameMode.CandR: //C&R
                    var lines1 = taskText.Split("\r\n</color>\n")[0].Split("\r\n\n")[0].Split("\r\n");
                    StringBuilder sb1 = new();
                    foreach (var eachLine in lines1)
                    {
                        var line = eachLine.Trim();
                        if ((line.StartsWith("<color=#ff1919ff>") || line.StartsWith("<color=#ff0000ff>")) && sb1.Length < 1 && !line.Contains('(')) continue;
                        sb1.Append(line + "\r\n");
                    }

                    if (sb1.Length > 1)
                    {
                        var text = sb1.ToString().TrimEnd('\n').TrimEnd('\r');
                        if (!Utils.HasTasks(player.Data, false) && sb1.ToString().Any(s => s == '\n'))
                            text = $"{Utils.ColorString(Utils.GetRoleColor(player.GetCustomRole()).ShadeColor(0.2f), GetString("FakeTask"))}\r\n{text}";
                        sb.Append($"\r\n\r\n<size=85%>{text}</size>");
                    }
                    sbFinal.Clear();
                    sbFinal.Append(sb);
                    break;
                case CustomGameMode.UltimateTeam:
                    var lines2 = taskText.Split("\r\n</color>\n")[0].Split("\r\n\n")[0].Split("\r\n");
                    StringBuilder sb3 = new();
                    foreach (var eachLine in lines2)
                    {
                        var line = eachLine.Trim();
                        if ((line.StartsWith("<color=#ff1919ff>") || line.StartsWith("<color=#ff0000ff>")) && sb3.Length < 1 && !line.Contains('(')) continue;
                        sb3.Append(line + "\r\n");
                    }

                    if (sb3.Length > 1)
                    {
                        var text = sb3.ToString().TrimEnd('\n').TrimEnd('\r');
                        if (!Utils.HasTasks(player.Data, false) && sb3.ToString().Any(s => s == '\n'))
                            text = $"{Utils.ColorString(Utils.GetRoleColor(player.GetCustomRole()).ShadeColor(0.2f), GetString("FakeTask"))}\r\n{text}";
                        sb.Append($"\r\n\r\n<size=85%>{text}</size>");
                    }
                    sbFinal.Clear();
                    sbFinal.Append(sb);
                    break;
                case CustomGameMode.TrickorTreat:
                    var lines3 = taskText.Split("\r\n</color>\n")[0].Split("\r\n\n")[0].Split("\r\n");
                    StringBuilder sb4 = new();
                    foreach (var eachLine in lines3)
                    {
                        var line = eachLine.Trim();
                        if ((line.StartsWith("<color=#ff1919ff>") || line.StartsWith("<color=#ff0000ff>")) && sb4.Length < 1 && !line.Contains('(')) continue;
                        sb4.Append(line + "\r\n");
                    }

                    if (sb4.Length > 1)
                    {
                        var text = sb4.ToString().TrimEnd('\n').TrimEnd('\r');
                        if (!Utils.HasTasks(player.Data, false) && sb4.ToString().Any(s => s == '\n'))
                            text = $"{Utils.ColorString(Utils.GetRoleColor(player.GetCustomRole()).ShadeColor(0.2f), GetString("FakeTask"))}\r\n{text}";
                        sb.Append($"\r\n\r\n<size=85%>{text}</size>");
                    }
                    sbFinal.Clear();
                    sbFinal.Append(sb);
                    break;

            }

            __instance.taskText.text = sbFinal.ToString();
        }

        // RepairSender Display
        if (RepairSender.enabled && AmongUsClient.Instance.NetworkMode != NetworkModes.OnlineGame)
            __instance.taskText.text = RepairSender.GetText();
    }
}

class RepairSender
{
    public static bool enabled = false;
    public static bool TypingAmount = false;

    public static int SystemType;
    public static int amount;

    public static void Input(int num)
    {
        if (!TypingAmount)
        {
            //SystemType is being entered
            SystemType *= 10;
            SystemType += num;
        }
        else
        {
            //Amount being entered
            amount *= 10;
            amount += num;
        }
    }
    public static void InputEnter()
    {
        if (!TypingAmount)
        {
            //SystemType is being entered
            TypingAmount = true;
        }
        else
        {
            //Amount being entered
            Send();
        }
    }
    public static void Send()
    {
        ShipStatus.Instance.RpcUpdateSystem((SystemTypes)SystemType, (byte)amount);
        Reset();
    }
    public static void Reset()
    {
        TypingAmount = false;
        SystemType = 0;
        amount = 0;
    }
    public static string GetText()
    {
        return SystemType.ToString() + "(" + ((SystemTypes)SystemType).ToString() + ")\r\n" + amount;
    }
}
