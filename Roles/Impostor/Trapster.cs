using System.Collections.Generic;

namespace TOHE.Roles.Impostor;

internal class Trapster : RoleBase
{
    private const int Id = -1;
    public static bool On;
    public override bool IsEnable => On;

    private static OptionItem TrapsterKillCooldown;
    private static OptionItem TrapConsecutiveBodies;
    private static OptionItem TrapTrapsterBody;
    private static OptionItem TrapConsecutiveTrapsterBodies;

    private static List<byte> BoobyTrapBody = [];
    private static Dictionary<byte, byte> KillerOfBoobyTrapBody = [];

    public static void SetupCustomOption()
    {
        Options.SetupRoleOptions(2600, TabGroup.ImpostorRoles, CustomRoles.Trapster);
        TrapsterKillCooldown = FloatOptionItem.Create(2602, "KillCooldown", new(2.5f, 180f, 2.5f), 30f, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Trapster])
            .SetValueFormat(OptionFormat.Seconds);
        TrapConsecutiveBodies = BooleanOptionItem.Create(2603, "TrapConsecutiveBodies", true, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Trapster]);
        TrapTrapsterBody = BooleanOptionItem.Create(2604, "TrapTrapsterBody", true, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Trapster]);
        TrapConsecutiveTrapsterBodies = BooleanOptionItem.Create(2605, "TrapConsecutiveBodies", true, TabGroup.ImpostorRoles, false)
            .SetParent(TrapTrapsterBody);
    }

    public override void Init()
    {
        BoobyTrapBody = [];
        KillerOfBoobyTrapBody = [];
        On = false;
    }
    public override void Add(byte playerId)
    {
        On = true;
    }

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = TrapsterKillCooldown.GetFloat();

    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        BoobyTrapBody.Add(target.PlayerId);
        return true;
    }

    public override bool OnCheckReportDeadBody(PlayerControl reporter, PlayerControl target)
    {
        // if trapster dead
        if (target.Is(CustomRoles.Trapster) && TrapTrapsterBody.GetBool() && !reporter.Is(CustomRoles.Pestilence))
        {
            var killerId = target.PlayerId;

            Main.PlayerStates[reporter.PlayerId].deathReason = PlayerState.DeathReason.Trap;
            reporter.SetRealKiller(target);
            reporter.RpcMurderPlayerV3(reporter);
            
            RPC.PlaySoundRPC(killerId, Sounds.KillSound);
            
            if (TrapConsecutiveTrapsterBodies.GetBool())
            {
                BoobyTrapBody.Add(reporter.PlayerId);
            }
            
            return false;
        }

        // if reporter try reported trap body
        if (BoobyTrapBody.Contains(target.PlayerId) && reporter.IsAlive() && !reporter.Is(CustomRoles.Pestilence))
        {
            var killerId = target.PlayerId;
            
            Main.PlayerStates[reporter.PlayerId].deathReason = PlayerState.DeathReason.Trap;
            reporter.SetRealKiller(target);
            reporter.RpcMurderPlayerV3(reporter);
            
            RPC.PlaySoundRPC(killerId, Sounds.KillSound);
            if (TrapConsecutiveBodies.GetBool())
            {
                BoobyTrapBody.Add(reporter.PlayerId);
            }

            return false;
        }

        return true;
    }
}
