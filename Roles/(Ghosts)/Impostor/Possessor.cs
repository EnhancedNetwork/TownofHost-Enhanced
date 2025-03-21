using AmongUs.GameOptions;
using TOHE.Roles.Core;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles._Ghosts_.Impostor;

internal class Possessor : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Possessor;
    private const int Id = 28900;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Possessor);
    public override CustomRoles ThisRoleBase => CustomRoles.GuardianAngel;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorGhosts;
    //==================================================================\\
    private static bool controllingPlayer = false;
    private static byte controllingTargetId = byte.MaxValue;
    private static float controllingLastSpeed = float.MinValue;
    private static float possessTime = float.MinValue;

    private static OptionItem PossessCooldown;
    private static OptionItem PossessDuration;
    private static OptionItem AlertRange;
    private static OptionItem FocusRange;

    public override void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Possessor);
        PossessCooldown = FloatOptionItem.Create(Id + 10, "PossessorPossessCooldown", new(2.5f, 120f, 2.5f), 25f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Possessor])
        .SetValueFormat(OptionFormat.Seconds);
        PossessDuration = FloatOptionItem.Create(Id + 11, "PossessorPossessDuration", new(2.5f, 120f, 2.5f), 10f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Possessor])
        .SetValueFormat(OptionFormat.Seconds);
        AlertRange = FloatOptionItem.Create(Id + 12, "PossessorAlertRange", new(1f, 10f, 0.5f), 2.5f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Possessor])
        .SetValueFormat(OptionFormat.Multiplier);
        FocusRange = FloatOptionItem.Create(Id + 13, "PossessorFocusRange", new(5f, 25f, 2.5f), 10f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Possessor])
        .SetValueFormat(OptionFormat.Multiplier);
    }
    public override void Add(byte PlayerId)
    {
        CustomRoleManager.OnFixedUpdateOthers.Add(OnFixedUpdateOther);
    }
    // EAC bans players when GA uses sabotage
    public override bool CanUseSabotage(PlayerControl pc) => false;
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.GuardianAngelCooldown = controllingPlayer ? 0f : PossessCooldown.GetFloat();
        AURoleOptions.ProtectionDurationSeconds = 0f;
    }

    private void OnFixedUpdateOther(PlayerControl target, bool lowLoad, long nowTime)
    {
        if (_Player == null) return;

        if (target.PlayerId == controllingTargetId)
        {
            if (controllingPlayer && possessTime >= 0)
            {
                if (Utils.GetDistance(_Player.GetCustomPosition(), target.GetCustomPosition()) > 5f)
                {
                    _Player.RpcTeleport((_Player.GetCustomPosition() + target.GetCustomPosition()) / 2);
                }

                foreach (var allPlayers in Main.AllAlivePlayerControls.Where(pc => pc != target))
                {
                    if (Utils.GetDistance(target.GetCustomPosition(), allPlayers.GetCustomPosition()) < AlertRange.GetFloat())
                    {
                        controllingPlayer = false;
                    }
                }

                if (Utils.GetDistance(_Player.GetCustomPosition(), target.GetCustomPosition()) < 1f)
                {
                    if (target.MyPhysics.Animations.IsPlayingRunAnimation())
                    {
                        target.RpcTeleport(target.GetCustomPosition());
                        target.MyPhysics.RpcCancelPet();
                    }
                }
                else if (Utils.GetDistance(_Player.GetCustomPosition(), target.GetCustomPosition()) < 3.5f)
                {
                    if (!target.petting && Utils.GetDistance(_Player.GetCustomPosition(), target.GetCustomPosition()) > 1f)
                    {
                        target.MyPhysics.RpcPet(_Player.GetCustomPosition(), new Vector2(500f, 500f));
                    }
                    else if (!target.MyPhysics.Animations.IsPlayingRunAnimation())
                    {
                        target.MyPhysics.RpcCancelPet();
                        target.RpcTeleport(target.GetCustomPosition());
                    }
                }

                possessTime -= Time.deltaTime;
            }
            else
            {
                target.MyPhysics.RpcCancelPet();
                target.RpcTeleport(target.GetCustomPosition());
                Main.AllPlayerSpeed[target.PlayerId] = controllingLastSpeed;
                target.MarkDirtySettings();
                controllingTargetId = byte.MaxValue;
                controllingLastSpeed = float.MinValue;
                _Player.RpcGuardAndKill(_Player);

                if (controllingPlayer)
                {
                    float checkPos = float.MaxValue;
                    foreach (var allPlayers in Main.AllAlivePlayerControls.Where(pc => pc != target))
                    {
                        if (Utils.GetDistance(_Player.GetCustomPosition(), allPlayers.GetCustomPosition()) < checkPos)
                        {
                            checkPos = Utils.GetDistance(_Player.GetCustomPosition(), allPlayers.GetCustomPosition());
                        }
                    }
                    if (checkPos >= FocusRange.GetFloat() && !target.IsTransformedNeutralApocalypse())
                    {
                        target.SetDeathReason(PlayerState.DeathReason.Curse);
                        target.RpcMurderPlayer(target);
                        target.SetRealKiller(_Player);
                    }
                }

                controllingPlayer = false;
                _Player.RpcResetAbilityCooldown();
            }
        }
    }

    public override bool OnCheckProtect(PlayerControl killer, PlayerControl target)
    {
        if (target.GetCustomRole().IsImpostorTeam())
        {
            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Possessor), GetString("DollMaster_CannotPossessImpTeammate")));
            return false;
        }

        if (!controllingPlayer)
        {
            // Cancel if Target is around other players
            foreach (var allPlayers in Main.AllAlivePlayerControls.Where(pc => pc != target))
            {
                if (Utils.GetDistance(target.GetCustomPosition(), allPlayers.GetCustomPosition()) < AlertRange.GetFloat())
                {
                    _Player.RpcResetAbilityCooldown();
                    return false;
                }
            }

            _Player.RpcGuardAndKill(target);
            controllingTargetId = target.PlayerId;
            controllingLastSpeed = Main.AllPlayerSpeed[target.PlayerId];
            Main.AllPlayerSpeed[target.PlayerId] = Main.MinSpeed;
            target.MarkDirtySettings();
            possessTime = PossessDuration.GetFloat();
            controllingPlayer = true;
        }
        else
        {
            controllingPlayer = false;
        }

        return false;
    }
}
