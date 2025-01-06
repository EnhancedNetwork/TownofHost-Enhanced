﻿using System;
using UnityEngine;

namespace TOHE;

// 来源：https://github.com/tugaru1975/TownOfPlus/TOPmods/Zoom.cs 
// 参考：https://github.com/Yumenopai/TownOfHost_Y
[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
public static class Zoom
{
    private static bool ResetButtons = false;
    public static void Postfix()
    {
        //if (PlayerControl.LocalPlayer.Is(RoleType.Impostor) && Options.OperateVisibilityImpostor.GetBool()) return;
        if (GameStates.IsShip && !GameStates.IsMeeting && GameStates.IsCanMove && PlayerControl.LocalPlayer.Data.IsDead || GameStates.IsLobby && GameStates.IsCanMove)
        {
            if (Camera.main.orthographicSize > 3.0f)
                ResetButtons = true;

            if (Input.mouseScrollDelta.y > 0)
            {
                if (Camera.main.orthographicSize > 3.0f)
                {
                    SetZoomSize(times: false);
                }

            }
            if (Input.mouseScrollDelta.y < 0)
            {
                if (GameStates.IsDead || GameStates.IsFreePlay || DebugModeManager.AmDebugger || GameStates.IsLobby ||
                    PlayerControl.LocalPlayer.FriendCode.GetDevUser().DeBug)
                {
                    if (Camera.main.orthographicSize < 18.0f)
                    {
                        SetZoomSize(times: true);
                    }
                }
            }
            Flag.NewFlag("Zoom");
        }
        else
        {
            Flag.Run(() =>
            {
                SetZoomSize(reset: true);
            }, "Zoom");
        }
    }

    private static void SetZoomSize(bool times = false, bool reset = false)
    {
        var size = 1.5f;
        if (!times) size = 1 / size;
        if (reset)
        {
            Camera.main.orthographicSize = 3.0f;
            HudManager.Instance.UICamera.orthographicSize = 3.0f;
            HudManager.Instance.Chat.transform.localScale = Vector3.one;
            if (GameStates.IsMeeting) MeetingHud.Instance.transform.localScale = Vector3.one;
        }
        else
        {
            Camera.main.orthographicSize *= size;
            HudManager.Instance.UICamera.orthographicSize *= size;
        }
        DestroyableSingleton<HudManager>.Instance?.ShadowQuad?.gameObject?.SetActive((reset || Camera.main.orthographicSize == 3.0f) && PlayerControl.LocalPlayer.IsAlive());
        
        if (ResetButtons)
        {
            ResolutionManager.ResolutionChanged.Invoke((float)Screen.width / Screen.height, Screen.width, Screen.height, Screen.fullScreen);
            ResetButtons = false;
        }
    }

    public static void OnFixedUpdate()
        => DestroyableSingleton<HudManager>.Instance?.ShadowQuad?.gameObject?.SetActive((Camera.main.orthographicSize == 3.0f) && PlayerControl.LocalPlayer.IsAlive());
}

public static class Flag
{
    private static readonly List<string> OneTimeList = [];
    private static readonly List<string> FirstRunList = [];
    public static void Run(Action action, string type, bool firstrun = false)
    {
        if (OneTimeList.Contains(type) || (firstrun && !FirstRunList.Contains(type)))
        {
            if (!FirstRunList.Contains(type)) FirstRunList.Add(type);
            OneTimeList.Remove(type);
            action();
        }

    }
    public static void NewFlag(string type)
    {
        if (!OneTimeList.Contains(type)) OneTimeList.Add(type);
    }

    public static void DeleteFlag(string type)
    {
        if (OneTimeList.Contains(type)) OneTimeList.Remove(type);
    }
}