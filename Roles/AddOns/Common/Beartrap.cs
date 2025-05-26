using static TOHE.Options;

namespace TOHE.Roles.AddOns.Common;

public class Trapper : IAddon
{
    public CustomRoles Role => CustomRoles.Trapper;
    private const int Id = 18800;
    public AddonTypes Type => AddonTypes.Helpful;

    public static OptionItem TrapperBlockMoveTime;

    public void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Trapper, canSetNum: true, teamSpawnOptions: true);
        TrapperBlockMoveTime = FloatOptionItem.Create(Id + 13, "FreezeTime", new(1f, 180f, 1f), 5f, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Trapper])
            .SetValueFormat(OptionFormat.Seconds);
    }
    public void Init()
    { }
    public void Add(byte playerId, bool gameIsLoading = true)
    { }
    public void Remove(byte playerId)
    { }
}
public static class TrapperExtension
{
    public static void TrapperKilled(this PlayerControl killer, PlayerControl target)
    {
        Logger.Info($"{target?.Data?.PlayerName} was Trapper", "Trapper");
        var tmpSpeed = Main.AllPlayerSpeed[killer.PlayerId];
        Main.AllPlayerSpeed[killer.PlayerId] = Main.MinSpeed;
        ReportDeadBodyPatch.CanReport[killer.PlayerId] = false;
        killer.MarkDirtySettings();
        _ = new LateTask(() =>
        {
            Main.AllPlayerSpeed[killer.PlayerId] = Main.AllPlayerSpeed[killer.PlayerId] - Main.MinSpeed + tmpSpeed;
            ReportDeadBodyPatch.CanReport[killer.PlayerId] = true;
            killer.MarkDirtySettings();
            RPC.PlaySoundRPC(Sounds.TaskComplete, killer.PlayerId);
        }, Trapper.TrapperBlockMoveTime.GetFloat(), "Trapper BlockMove");
    }
}
