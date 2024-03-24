using System.Collections.Generic;
using static TOHE.Options;

namespace TOHE.Roles.Impostor;

internal class Lurker : RoleBase
{
    private const int Id = 1900;

    public static bool On;
    public override bool IsEnable => On;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;

    private static OptionItem DefaultKillCooldown;
    private static OptionItem ReduceKillCooldown;

    private static List<byte> playerIdList = [];

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Lurker);
        DefaultKillCooldown = FloatOptionItem.Create(Id + 10, "ArroganceDefaultKillCooldown", new(20f, 180f, 1f), 30f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Lurker])
            .SetValueFormat(OptionFormat.Seconds);
        ReduceKillCooldown = FloatOptionItem.Create(Id + 11, "ArroganceReduceKillCooldown", new(0f, 10f, 1f), 2f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Lurker])
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void Init()
    {
        On = false;
        playerIdList = [];
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        On = true;
    }

    public  override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = DefaultKillCooldown.GetFloat();

    public override void OnEnterVent(PlayerControl pc, Vent vent)
    {
        float newCd = Main.AllPlayerKillCooldown[pc.PlayerId] - ReduceKillCooldown.GetFloat();
        if (newCd <= 0)
        {
            return;
        }

        Main.AllPlayerKillCooldown[pc.PlayerId] = newCd;
        pc.SyncSettings();
    }

    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        killer.ResetKillCooldown();
        killer.SyncSettings();
        return true;
    }
}
