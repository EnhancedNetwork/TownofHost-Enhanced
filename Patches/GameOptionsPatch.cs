using AmongUs.GameOptions;
using HarmonyLib;
using static TOHE.Translator;

namespace TOHE;

[HarmonyPatch(typeof(RoleOptionSetting), nameof(RoleOptionSetting.UpdateValuesAndText))]
class ChanceChangePatch
{
    public static void Postfix(RoleOptionSetting __instance)
    {
        bool forced = true;
        string DisableText = $" ({GetString("Disabled")})";
        if (__instance.Role.Role == RoleTypes.Scientist)
        {
            __instance.TitleText.color = Utils.GetRoleColor(CustomRoles.Scientist);
        }
        if (__instance.Role.Role == RoleTypes.Engineer)
        {
            __instance.TitleText.color = Utils.GetRoleColor(CustomRoles.Engineer);
        }
        if (__instance.Role.Role == RoleTypes.GuardianAngel)
        {
            // Hide + -, setpoint and advanced buttons
            var tf = __instance.transform;
            tf.Find("Count Plus_TMP").gameObject.active
                = tf.Find("Chance Minus_TMP").gameObject.active
               // = tf.Find("Chance Value_TMP").gameObject.active
                = tf.Find("Chance Plus_TMP").gameObject.active
                //= tf.Find("More Options").gameObject.active
                = false;

            // Add Disable text for Guardian Angel
            if (!__instance.TitleText.text.Contains(DisableText))
                __instance.TitleText.text += DisableText;
            __instance.TitleText.color = Utils.GetRoleColor(CustomRoles.GuardianAngel);
        }
        if (__instance.Role.Role == RoleTypes.Shapeshifter)
        {
            __instance.TitleText.color = Utils.GetRoleColor(CustomRoles.Shapeshifter);
        }

        if (forced)
        {
            __instance.ChanceText.text = DisableText;
        }
    }
}