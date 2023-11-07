using HarmonyLib;
using System;
using UnityEngine;

namespace TOHE;

[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.Start))]
public static class Console_UsePrefix
{
    public static void Prefix(ShipStatus __instance)
    {
        Console[] consoles = UnityEngine.Object.FindObjectsOfType<Console>();

        if(Main.ImpTasks.Value == true)
        { 
        foreach (var console in consoles)
        {
            // Set AllowImpostor to true for each Console instance
            console.AllowImpostor = true;
        }
        
        }
        else
        {
            foreach (var console in consoles)
            {
                // Set AllowImpostor to true for each Console instance
                return;
            }

        }

    }

}