using HarmonyLib;
using Il2CppSystem.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TOHE.Patches;

//[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
//class FixedUpdateInHidenSeekPatch
//{
//    private static long LastFixedUpdate;
//    public static void Postfix(PlayerControl __instance)
//    {
//        if (GameStates.IsNormalGame) return;

//        var now = Utils.GetTimeStamp();

//        if (LastFixedUpdate == now) return;
//        LastFixedUpdate = now;

//        var player = __instance;
//    }
//}