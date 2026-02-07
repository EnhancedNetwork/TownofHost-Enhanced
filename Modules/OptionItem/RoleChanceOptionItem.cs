using System;

namespace TOHE;

public class RoleChanceOptionItem(int id, CustomRoles role, string name, int defaultValue, TabGroup tab, bool isSingleValue, string[] selections, bool vanilla, bool useGetString) : StringOptionItem(id, name, defaultValue, tab, isSingleValue, selections, vanilla, useGetString)
{
    public CustomRoles Role = role;

    public override bool GetBool() => CurrentValue != 0 || Role.RoleExist(countDead: true);

    public static RoleChanceOptionItem Create(int id, CustomRoles role, string[] selections, int defaultIndex, TabGroup tab, bool isSingleValue = false, bool vanillaText = false, bool useGetString = true)
    {
        return new RoleChanceOptionItem(id, role, role.ToString(), defaultIndex, tab, isSingleValue, selections, vanillaText, useGetString);
    }
}