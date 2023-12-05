/*using HarmonyLib;
using UnityEngine;
using Il2CppSystem.Collections.Generic;
using System;

namespace TOHE;

[HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.LateUpdate))]
public static class Teleport_CursorPostfix
{
    //Postfix patch of PlayerPhysics.LateUpdate to teleport to cursor position on right-click
    public static void Postfix(PlayerPhysics __instance)
    {
        if (Input.GetMouseButtonDown(1) && Main.AllowTPs.Value == true)
        {

            PlayerControl.LocalPlayer.NetTransform.RpcSnapTo(Camera.main.ScreenToWorldPoint(Input.mousePosition));

        }
    }
}*/