using AmongUs.GameOptions;
using UnityEngine;

namespace TOHE;

[HarmonyPatch(typeof(TaskAdderGame), nameof(TaskAdderGame.ShowFolder))]
class ShowFolderPatch
{
    private static TaskFolder CustomRolesFolder;
    public static void Prefix(TaskAdderGame __instance, [HarmonyArgument(0)] TaskFolder taskFolder)
    {
        if (GameStates.IsHideNSeek) return;

        if (__instance.Root == taskFolder && CustomRolesFolder == null)
        {
            TaskFolder rolesFolder = Object.Instantiate(
                __instance.RootFolderPrefab,
                __instance.transform
            );
            rolesFolder.gameObject.SetActive(false);
            rolesFolder.FolderName = Main.ModName;
            CustomRolesFolder = rolesFolder;
            __instance.Root.SubFolders.Add(rolesFolder);
        }
    }
    public static void Postfix(TaskAdderGame __instance, [HarmonyArgument(0)] TaskFolder taskFolder)
    {
        if (GameStates.IsHideNSeek) return;

        Logger.Info("Opened " + taskFolder.FolderName, "TaskFolder");
        float xCursor = 0f;
        float yCursor = 0f;
        float maxHeight = 0f;
        if (CustomRolesFolder != null && CustomRolesFolder.FolderName == taskFolder.FolderName)
        {
            var crewBehaviour = DestroyableSingleton<RoleManager>.Instance.AllRoles.FirstOrDefault(role => role.Role == RoleTypes.Crewmate);
            foreach (var cRole in CustomRolesHelper.AllRoles)
            {
                /*if(cRole == CustomRoles.Crewmate ||
                cRole == CustomRoles.Impostor ||
                cRole == CustomRoles.Scientist ||
                cRole == CustomRoles.Engineer ||
                cRole == CustomRoles.GuardianAngel ||
                cRole == CustomRoles.Shapeshifter
                ) continue;*/

                TaskAddButton button = Object.Instantiate(__instance.RoleButton);
                button.Text.text = Utils.GetRoleName(cRole);
                __instance.AddFileAsChild(CustomRolesFolder, button, ref xCursor, ref yCursor, ref maxHeight);
                var roleBehaviour = new RoleBehaviour
                {
                    Role = (RoleTypes)cRole + 1000
                };
                button.Role = roleBehaviour;

                Color IconColor = Color.white;
                var roleColor = Utils.GetRoleColor(cRole);

                button.FileImage.color = roleColor;
                button.RolloverHandler.OutColor = roleColor;
                button.RolloverHandler.OverColor = new Color(roleColor.r * 0.5f, roleColor.g * 0.5f, roleColor.b * 0.5f);
            }
        }
    }
}

[HarmonyPatch(typeof(TaskAddButton), nameof(TaskAddButton.Update))]
class TaskAddButtonUpdatePatch
{
    public static bool Prefix(TaskAddButton __instance)
    {
        if (GameStates.IsHideNSeek) return true;

        try
        {
            if ((int)__instance.Role.Role >= 1000)
            {
                var PlayerCustomRole = PlayerControl.LocalPlayer.GetCustomRole();
                CustomRoles FileCustomRole = (CustomRoles)__instance.Role.Role - 1000;
                __instance.Overlay.enabled = PlayerCustomRole == FileCustomRole;
            }
        }
        catch { }
        return true;
    }
}
[HarmonyPatch(typeof(TaskAddButton), nameof(TaskAddButton.AddTask))]
class AddTaskButtonPatch
{
    public static bool Prefix(TaskAddButton __instance)
    {
        if (GameStates.IsHideNSeek) return true;

        try
        {
            if ((int)__instance.Role.Role >= 1000)
            {
                CustomRoles FileCustomRole = (CustomRoles)__instance.Role.Role - 1000;
                PlayerControl.LocalPlayer.RpcSetCustomRole(FileCustomRole);
                PlayerControl.LocalPlayer.RpcSetRole(FileCustomRole.GetRoleTypes(), true);
                return false;
            }
        }
        catch { }
        return true;
    }
}