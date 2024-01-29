using Hazel;
using System.Collections.Generic;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;
using static UnityEngine.ParticleSystem.PlaybackState;

namespace TOHE.Roles.Crewmate;

public class Warden
{
    private static readonly int Id = 27800;
    public static OptionItem AbilityCooldown;
    public static OptionItem IncreaseSpeed;
    public static void SetupCustomOptions()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Warden);
        AbilityCooldown = FloatOptionItem.Create(Id + 10, "AbilityCooldown", new(0f, 180f, 2.5f), 25f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Warden])
            .SetValueFormat(OptionFormat.Seconds);
        IncreaseSpeed = FloatOptionItem.Create(Id + 11, "WardenIncreaseSpeed", new(1f, 3f, 0.5f), 1f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Warden])
            .SetValueFormat(OptionFormat.Times);
    }
    
    private static List<byte> IsAffected = [];
    public static bool OnCheckProtect(PlayerControl killer, PlayerControl target)
    {
        var getTargetRole = target.GetCustomRole();
        if (CustomRolesHelper.IsSpeedRole(getTargetRole) || IsAffected.Contains(target.PlayerId)) goto Notifiers; // Incompactible speed-roles 

        IsAffected.Add(target.PlayerId);
        target.MarkDirtySettings();
        var tmpSpeed = Main.AllPlayerSpeed[target.PlayerId];
        Main.AllPlayerSpeed[target.PlayerId] = Main.AllPlayerSpeed[target.PlayerId] + IncreaseSpeed.GetFloat();


        _ = new LateTask(() =>
        {
            Main.AllPlayerSpeed[target.PlayerId] = Main.AllPlayerSpeed[target.PlayerId] - Main.AllPlayerSpeed[target.PlayerId] + tmpSpeed;
            target.MarkDirtySettings();
            if (IsAffected.Contains(target.PlayerId)) IsAffected.Remove(target.PlayerId);
        }, 2f);

    Notifiers:
        target.Notify(Utils.ColorString(new Color32(179, 0, 0, byte.MaxValue), GetString("WardenWarn")));

        killer.RpcResetAbilityCooldown();

        return false;
    }

    
}