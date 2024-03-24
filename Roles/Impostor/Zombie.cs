using AmongUs.GameOptions;
using System.Collections.Generic;

namespace TOHE.Roles.Impostor;

internal class Zombie : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 23900;
    private static readonly HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Count > 0;
    public override bool IsEnable => HasEnabled;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    //==================================================================\\

    private static OptionItem ZombieKillCooldown;
    private static OptionItem ZombieSpeedReduce;

    public static void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Zombie);
        ZombieKillCooldown = FloatOptionItem.Create(Id + 3, "KillCooldown", new(0f, 180f, 2.5f), 5f, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Zombie])
            .SetValueFormat(OptionFormat.Seconds);
        ZombieSpeedReduce = FloatOptionItem.Create(Id + 4, "ZombieSpeedReduce", new(0.0f, 1.0f, 0.1f), 0.1f, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Zombie])
            .SetValueFormat(OptionFormat.Multiplier);
    }
    public override void Init()
    {
        playerIdList.Clear();
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        opt.SetFloat(FloatOptionNames.ImpostorLightMod, 0.2f);
    }
    public override void SetKillCooldown(byte id)
    {
        Main.AllPlayerKillCooldown[id] = ZombieKillCooldown.GetFloat();
        Main.AllPlayerSpeed[id] -= ZombieSpeedReduce.GetFloat();
    }

    public static void CheckRealVotes(PlayerControl target, ref int VoteNum)
    {
        if (target.Is(CustomRoles.Zombie))
        {
            VoteNum = 0;
        }
    }
}
