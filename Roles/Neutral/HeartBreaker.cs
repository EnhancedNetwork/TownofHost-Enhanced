using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AmongUs.GameOptions;
using Rewired;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Neutral
{
    internal class HeartBreaker : RoleBase
    {
        //===========================SETUP================================\\
        private const int Id = 30900;
        public override bool IsDesyncRole => true;
        public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
        public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralEvil;
        //==================================================================\\

        private static OptionItem HeartBreakerCooldown;
        private static OptionItem HeartBreakerKillOtherLover;
        private static OptionItem HeartBreakerTriesMax;
        private static OptionItem HeartBreakerCanKillAfterFindingLover;
        private static OptionItem HeartBreakerSuicideIfNoLover;

        private bool HasLover = false;

        public override void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.HeartBreaker);
            HeartBreakerCooldown = FloatOptionItem.Create(Id + 10, "HeartBreakerCooldown", new(0f, 180f, 2.5f), 20f, TabGroup.NeutralRoles, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.HeartBreaker])
                .SetValueFormat(OptionFormat.Seconds);
            HeartBreakerTriesMax = IntegerOptionItem.Create(Id + 11, "HeartBreakerTriesMax", new(1, 10, 1), 3, TabGroup.NeutralRoles, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.HeartBreaker]);
            HeartBreakerCanKillAfterFindingLover = BooleanOptionItem.Create(Id + 12, "HeartBreakerCanKillAfterFindingLover", true, TabGroup.NeutralRoles, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.HeartBreaker]);
            HeartBreakerSuicideIfNoLover = BooleanOptionItem.Create(Id + 13, "HeartBreakerSuicideIfNoLover", true, TabGroup.NeutralRoles, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.HeartBreaker]);
            HeartBreakerKillOtherLover = BooleanOptionItem.Create(Id + 14, "HeartBreakerKillOtherLover", true, TabGroup.NeutralRoles, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.HeartBreaker]);
        }
        public override void Add(byte playerId)
        {
            AbilityLimit = HeartBreakerTriesMax.GetInt();
        }
        public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = HeartBreakerCooldown.GetFloat();

        public override bool CanUseKillButton(PlayerControl pc) => IsUseKillButton(pc);
        public bool IsUseKillButton(PlayerControl pc)
            => pc.IsAlive() && AbilityLimit > 0 && (!HasLover || HeartBreakerCanKillAfterFindingLover.GetBool());

        public override string GetProgressText(byte playerId, bool comms)
            => Utils.ColorString(IsUseKillButton(Utils.GetPlayerById(playerId)) ? Utils.GetRoleColor(CustomRoles.HeartBreaker).ShadeColor(0.25f) : Color.gray, $"({AbilityLimit})");
        public override void SetAbilityButtonText(HudManager hud, byte playerId)
        {
            if (!HasLover) hud.KillButton.OverrideText(GetString("HeartBreakerBreakText"));
            else hud.KillButton.OverrideText(GetString("TriggerKill"));
        }
        public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
        {
            if (killer == null || target == null) return false;
            if (AbilityLimit < 1)
            {
                killer.ResetKillCooldown();
                return false;
            }
            if (HasLover && HeartBreakerCanKillAfterFindingLover.GetBool()) return true;
            else if (HasLover) return false;
            AbilityLimit--;
            SendSkillRPC();
            if (HasLover && HeartBreakerCanKillAfterFindingLover.GetBool()) return true;
            else
            {
                if (target.GetCustomSubRoles().Contains(CustomRoles.Lovers))
                {
                    HasLover = true;
                    SetAbilityButtonText(HudManager.Instance, killer.PlayerId);
                    foreach (PlayerControl player in Main.LoversPlayers)
                    {
                        PlayerState playerState = Main.PlayerStates[player.PlayerId];
                        if (playerState.SubRoles.Contains(CustomRoles.Lovers) && playerState.PlayerId != target.PlayerId)
                        {
                            playerState.RemoveSubRole(CustomRoles.Lovers);
                            PlayerControl playerControl = Utils.GetPlayerById(playerState.PlayerId);
                            Main.LoversPlayers.Remove(playerControl);
                            RPC.SyncLoversPlayers();
                            if (HeartBreakerKillOtherLover.GetBool())
                            {
                                playerControl.SetDeathReason(PlayerState.DeathReason.FollowingSuicide);
                                playerControl.RpcMurderPlayer(playerControl);
                                playerControl.SetRealKiller(killer);
                            }
                            PlayerState killerPlayerState = Main.PlayerStates[killer.PlayerId];
                            killerPlayerState.SetSubRole(CustomRoles.Lovers);
                            Main.LoversPlayers.Add(killer);
                            RPC.SyncLoversPlayers();
                            break;
                        }
                    }
                }
                else if (HeartBreakerSuicideIfNoLover.GetBool() && AbilityLimit < 1 && !HasLover)
                {
                    killer.SetDeathReason(PlayerState.DeathReason.Suicide);
                    killer.RpcMurderPlayer(killer);
                }
                killer.ResetKillCooldown();
                return false;
            }
        }
    }
}
