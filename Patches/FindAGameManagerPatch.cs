using System;
using System.Text.Json.Serialization;
using AmongUs.Matchmaking;
using Il2CppInterop.Runtime.Attributes;
using Il2CppInterop.Runtime.Injection;
using InnerNet;

namespace TOHE.Patches;

[Serializable]
public class ModFilter
{
    public Guid AcceptedValues;

    public string FilterType { get; } = "mod";
}

[HarmonyPatch(typeof(FindAGameManager), nameof(FindAGameManager.Start))]
internal class FindAGameManagerStartPatch
{
    public static void Postfix(FindAGameManager __instance)
    {
        // TODO: Once vanilla adds this
        // Guid guid = new(Main.PluginGuid4);

        // var filter = new TOHEFilter()
        // {
        //     AcceptedValues = guid,
        // };
        // __instance.classicFilterSet.Filters.Add(new GameFilter("mod", Il2CppHelper.CastFast<ISubFilter>(filter)));

        // __instance.RefreshList();
    }
}