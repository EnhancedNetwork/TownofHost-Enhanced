using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AmongUs.GameOptions;
using Rewired;
using TOHE.Roles.Core;
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
        public override bool IsExperimental => true;
        public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.HeartBreaker);
        public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
        public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralChaos;
        //==================================================================\\

        private static OptionItem HeartBreakerCooldown;
        private static OptionItem HeartBreakerKillOtherLover;
        private static OptionItem HeartBreakerTriesMax;
        private static OptionItem HeartBreakerSuicideIfNoLover;

        public override void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.HeartBreaker);
            HeartBreakerCooldown = FloatOptionItem.Create(Id + 10, "HeartBreakerCooldown", new(0f, 180f, 2.5f), 20f, TabGroup.NeutralRoles, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.HeartBreaker])
                .SetValueFormat(OptionFormat.Seconds);
            HeartBreakerTriesMax = IntegerOptionItem.Create(Id + 11, "HeartBreakerTriesMax", new(1, 10, 1), 3, TabGroup.NeutralRoles, false)
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
        public bool IsUseKillButton(PlayerControl pc) => pc.IsAlive() && AbilityLimit > 0;
        public override bool CanUseImpostorVentButton(PlayerControl pc) => false;
        public override string GetProgressText(byte playerId, bool comms)
            => Utils.ColorString(IsUseKillButton(Utils.GetPlayerById(playerId)) ? Utils.GetRoleColor(CustomRoles.HeartBreaker).ShadeColor(0.25f) : Color.gray, $"({AbilityLimit})");
        public override void SetAbilityButtonText(HudManager hud, byte playerId)
        {
            if (!HasLover()) hud.KillButton.OverrideText(GetString("HeartBreakerBreakText"));
            else hud.KillButton.OverrideText(GetString("TriggerKill"));
        }
        public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
        {
            if (killer == null || target == null) return false;
            AbilityLimit--;
            SendSkillRPC();
            if (HasLover()) return true;
            else
            {
                if (target.GetCustomSubRoles().Contains(CustomRoles.Lovers))
                {
                    SetAbilityButtonText(HudManager.Instance, killer.PlayerId);
                    foreach (PlayerControl player in Main.LoversPlayers)
                    {
                        if (player.GetCustomSubRoles().Contains(CustomRoles.Lovers) && player != target && killer != target)
                        {
                            player.GetCustomSubRoles().Remove(CustomRoles.Lovers);
                            Main.LoversPlayers.Remove(player);
                            RPC.SyncLoversPlayers();
                            if (HeartBreakerKillOtherLover.GetBool())
                            {
                                player.SetDeathReason(PlayerState.DeathReason.FollowingSuicide);
                                player.RpcMurderPlayer(player);
                                player.SetRealKiller(killer);
                            }
                            killer.RpcSetCustomRole(CustomRoles.Lovers);
                            Main.LoversPlayers.Add(killer);
                            RPC.SyncLoversPlayers();
                            if (!DisableShieldAnimations.GetBool()) killer.RpcGuardAndKill(target);
                            target.RpcGuardAndKill(killer);
                            target.RpcGuardAndKill(target);
                            break;
                        }
                    }
                }
                else if (HeartBreakerSuicideIfNoLover.GetBool() && AbilityLimit < 1 && !HasLover())
                {
                    killer.SetDeathReason(PlayerState.DeathReason.Suicide);
                    killer.RpcMurderPlayer(killer);
                }
                killer.SetKillCooldown();
                return false;
            }
        }
        public bool HasLover() => Main.LoversPlayers.Contains(_Player);
    }
}
