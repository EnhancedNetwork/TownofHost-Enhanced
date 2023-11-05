using TOHE.Roles.Crewmate;
using static TOHE.Options;

namespace TOHE.Roles.Impostor;

public static class Hangman
{
    private static readonly int Id = 24500;
    public static bool IsEnable = false;

    private static OptionItem ShapeshiftCooldown;
    private static OptionItem ShapeshiftDuration;

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.OtherRoles, CustomRoles.Hangman);
        ShapeshiftCooldown = FloatOptionItem.Create(Id + 2, "ShapeshiftCooldown", new(1f, 180f, 1f), 25f, TabGroup.OtherRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Hangman])
            .SetValueFormat(OptionFormat.Seconds);
        ShapeshiftDuration = FloatOptionItem.Create(Id + 4, "ShapeshiftDuration", new(1f, 60f, 1f), 10f, TabGroup.OtherRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Hangman])
            .SetValueFormat(OptionFormat.Seconds);
    }
    public static void Init()
    {
        IsEnable = false;
    }
    public static void Add(byte playerId)
    {
        IsEnable = true;
    }
    public static void ApplyGameOptions()
    {
        AURoleOptions.ShapeshifterCooldown = ShapeshiftCooldown.GetFloat();
        AURoleOptions.ShapeshifterDuration = ShapeshiftDuration.GetFloat();
    }
    public static bool OnCheckMurder(PlayerControl killer, PlayerControl target)    
    {
    //    if (target.Is(CustomRoles.Bait)) return true;
        if (target.Is(CustomRoles.Pestilence)) return true;
        if (target.Is(CustomRoles.Veteran) && Main.VeteranInProtect.ContainsKey(target.PlayerId)) return true;
        if (Medic.ProtectList.Contains(target.PlayerId)) return false;

        //禁止内鬼刀叛徒
        if (target.Is(CustomRoles.Madmate) && !ImpCanKillMadmate.GetBool())
            return false;

        if (Main.CheckShapeshift.TryGetValue(killer.PlayerId, out var s) && s)
        {
            target.Data.IsDead = true;
            target.SetRealKiller(killer);
            Main.PlayerStates[target.PlayerId].deathReason = PlayerState.DeathReason.LossOfHead;
            target.RpcExileV2();
            Main.PlayerStates[target.PlayerId].SetDead();
            target.SetRealKiller(killer);
            killer.SetKillCooldown();
            return false;
        }
        return true;
    }
}