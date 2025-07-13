using Hazel;
using UnityEngine.UIElements.StyleSheets;

namespace TOHE;

[HarmonyPatch(typeof(GameManager), nameof(GameManager.Serialize))]
internal static class GameManagerSerializeFix
{
    public static bool InitialState = true;
    public static bool Prefix(GameManager __instance, [HarmonyArgument(0)] MessageWriter writer, [HarmonyArgument(1)] bool initialState, ref bool __result)
    {

        InitialState = initialState;


        bool flag = false;
        for (var index = 0; index < __instance.LogicComponents.Count; ++index)
        {
            GameLogicComponent logicComponent = __instance.LogicComponents[index];
            if (initialState || logicComponent.IsDirty)
            {
                flag = true;
                writer.StartMessage((byte)index);
                bool hasBody = logicComponent.Serialize(writer, initialState);
                if (hasBody) writer.EndMessage();
                else writer.CancelMessage();
                logicComponent.ClearDirtyFlag();
            }
        }
        __instance.ClearDirtyBits();
        __result = flag;
        return false;
    }
}
[HarmonyPatch(typeof(LogicOptions), nameof(LogicOptions.Serialize))]
class LogicOptionsSerializePatch
{
    public static bool Prefix(ref bool __result)
    {
        // Block all but the first time and synchronize only with CustomSyncSettings
        if (GameStates.IsInGame)
        {
            __result = false;
            return false;
        }
        else return true;
    }
}
