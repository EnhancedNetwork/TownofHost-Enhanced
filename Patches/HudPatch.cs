using System.Text;
using TMPro;
using TOHE.Roles.AddOns.Common;
using TOHE.Roles.Core;
using UnityEngine;
using static TOHE.Translator;

namespace TOHE;

[HarmonyPatch(typeof(HudManager), nameof(HudManager.Start))]
class HudManagerStartPatch
{
    public static void Postfix(HudManager __instance)
    {
        __instance.gameObject.AddComponent<OptionShower>().hudManager = __instance;
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
        // Fix vanilla bug when report button displayed in the lobby
        __instance.ReportButton.ToggleVisible(!GameStates.IsLobby && isActive);

        if (!GameStates.IsModHost || GameStates.IsHideNSeek) return;

        IsActive = isActive;

        if (GameStates.IsLobby || !isActive) return;
        if (player == null) return;

        if (player.Is(CustomRoles.Oblivious) || player.Is(CustomRoles.KillingMachine) || Options.CurrentGameMode != CustomGameMode.Standard)
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
            if (pc.inVent || __instance.currentTarget == null || !pc.CanMove || !__instance.isActiveAndEnabled) return true;
            if (!pc.Is(CustomRoles.Swooper) && !pc.Is(CustomRoles.Wraith) && !pc.Is(CustomRoles.Chameleon)) return true;
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
            var RoleWithInfo = $"{player.GetDisplayRoleAndSubName(player, false, false)}:\r\n";
            RoleWithInfo += player.GetRoleInfo();

            var AllText = Utils.ColorString(player.GetRoleColor(), RoleWithInfo);

            switch (Options.CurrentGameMode)
            {
                case CustomGameMode.Standard:

                    var lines = taskText.Split("\r\n</color>\n")[0].Split("\r\n\n")[0].Split("\r\n");
                    StringBuilder sb = new();
                    foreach (var eachLine in lines)
                    {
                        var line = eachLine.Trim();
                        if ((line.StartsWith("<color=#FF1919FF>") || line.StartsWith("<color=#FF0000FF>")) && sb.Length < 1 && !line.Contains('(')) continue;
                        sb.Append(line + "\r\n");
                    }

                    if (sb.Length > 1)
                    {
                        var text = sb.ToString().TrimEnd('\n').TrimEnd('\r');
                        if (!Utils.HasTasks(player.Data, false) && sb.ToString().Count(s => (s == '\n')) >= 1)
                            text = $"{Utils.ColorString(Utils.GetRoleColor(player.GetCustomRole()).ShadeColor(0.2f), GetString("FakeTask"))}\r\n{text}";
                        AllText += $"\r\n\r\n<size=85%>{text}</size>";
                    }

                    if (MeetingStates.FirstMeeting)
                    {
                        AllText += $"\r\n\r\n</color><size=70%>{GetString("PressF1ShowMainRoleDes")}";
                        if (Main.PlayerStates.TryGetValue(PlayerControl.LocalPlayer.PlayerId, out var ps) && ps.SubRoles.Count >= 1)
                            AllText += $"\r\n{GetString("PressF2ShowAddRoleDes")}";
                        AllText += $"\r\n{GetString("PressF3ShowRoleSettings")}";
                        if (ps.SubRoles.Count >= 1)
                            AllText += $"\r\n{GetString("PressF4ShowAddOnsSettings")}";
                        AllText += "</size>";
                    }
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
                    foreach (var id in list2.Where(x => SummaryText2.ContainsKey(x.Item2))) AllText += "\r\n" + SummaryText2[id.Item2];

                    AllText = $"<size=70%>{AllText}</size>";

                    break;
                case CustomGameMode.SpeedRun:
                    var lines2 = taskText.Split("\r\n</color>\n")[0].Split("\r\n\n")[0].Split("\r\n");
                    StringBuilder sb2 = new();
                    foreach (var eachLine in lines2)
                    {
                        var line = eachLine.Trim();
                        if ((line.StartsWith("<color=#FF1919FF>") || line.StartsWith("<color=#FF0000FF>")) && sb2.Length < 1 && !line.Contains('(')) continue;
                        sb2.Append(line + "\r\n");
                    }

                    if (sb2.Length > 1)
                    {
                        var text = sb2.ToString().TrimEnd('\n').TrimEnd('\r');
                        if (!Utils.HasTasks(player.Data, false) && sb2.ToString().Count(s => (s == '\n')) >= 1)
                            text = $"{Utils.ColorString(Utils.GetRoleColor(player.GetCustomRole()).ShadeColor(0.2f), GetString("FakeTask"))}\r\n{text}";
                        AllText += $"\r\n\r\n<size=85%>{text}</size>";
                    }

                    AllText += $"\r\n\r\n<size=80%>{SpeedRun.GetGameState()}</size>";

                    break;
            }

            __instance.taskText.text = AllText;
        }

        // RepairSender display
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
