using AmongUs.GameOptions;
using TOHE.Roles.Core;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;
using static TOHE.Main;
using TOHE.Modules;


namespace TOHE.Roles.Impostor;

internal class Meteor : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Meteor;
    private const int Id = 35600;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Meteor);
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorKilling;
    //==================================================================\\

    private static OptionItem KillCooldown;
    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Meteor);
        KillCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 20f, TabGroup.ImpostorRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Meteor])
            .SetValueFormat(OptionFormat.Seconds);
    }

    public override void Add(byte playerId)
    {
        // Double Trigger
        var pc = GetPlayerById(playerId);
        pc.AddDoubleTrigger();
    }

    public override void SetKillCooldown(byte id)
    {
        AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
    }

    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        // Use double trigger system
        if (killer.CheckDoubleTrigger(target, () => { }))
        {
            return true;
        }
       
        TriggerMeteorEffect(killer, target);
        return false;
        

    }

    private void TriggerMeteorEffect(PlayerControl killer, PlayerControl originalTarget)
    {
        List<PlayerControl> potentialTargets = new();


        // Find all alive non-imposter, non-immune players except original target
        foreach (var player in Main.AllAlivePlayerControls)
        {
            if (!player.Data.Role.IsImpostor &&
                player.PlayerId != originalTarget.PlayerId &&
                !IsImmune(player))
            {
                potentialTargets.Add(player);
            }
        }

        if (potentialTargets.Count > 0)
        {
            var randomTarget = potentialTargets.RandomElement();

            // Kill the random target 
            randomTarget.SetRealKiller(killer);
            randomTarget.RpcMurderPlayer(randomTarget);
            randomTarget.SetDeathReason(PlayerState.DeathReason.Enflamed);
            Main.PlayerStates[randomTarget.PlayerId].SetDead();

            // Block kill
            killer.RpcGuardAndKill(originalTarget);
           
        }
        else
        {
            // Fallback to normal kill if no valid random targets
            killer.RpcMurderPlayer(originalTarget);
        }

        killer.SetKillCooldown();
    }

    private static bool IsImmune(PlayerControl player)
    {
        // Add immune roles here 
        return player.Is(CustomRoles.Solsticer) ||
        player.Is(CustomRoles.Necromancer) ||
        player.Is(CustomRoles.NiceMini) ||
        player.Is(CustomRoles.LazyGuy) ||
        player.IsTransformedNeutralApocalypse() ||
        player.IsPlayerImpostorTeam() ||
        player.Is(CustomRoles.PunchingBag) ||
        player.Is(CustomRoles.Jinx) ||
        player.Is(CustomRoles.GM);
    }

    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.KillButton.OverrideText(GetString("MeteorKillButtonText"));
    }
}
