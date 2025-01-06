using Hazel;
using TOHE.Roles.Core;
using UnityEngine;
using static TOHE.Options;
namespace TOHE.Roles.AddOns.Common
{
    public class Allergic : IAddon
    {
        private const int Id = 220000;
        public AddonTypes Type => AddonTypes.Harmful;

        private static OptionItem AllergicDistance;
        private static OptionItem AllergicTime;

        private static Dictionary<byte, byte> allergicTargets = new();
        private Dictionary<byte, float> proximityTimers = new Dictionary<byte, float>();

        public void SetupCustomOption()
        {
            SetupAdtRoleOptions(Id, CustomRoles.Allergic, canSetNum: true, teamSpawnOptions: true);

            AllergicDistance = FloatOptionItem.Create(Id + 10, "AllergicDistance", new(1.0f, 5.0f, 0.1f), 2.0f, TabGroup.Addons, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.Allergic])
                .SetValueFormat(OptionFormat.Multiplier);

            AllergicTime = FloatOptionItem.Create(Id + 11, "AllergicTime", new(1.0f, 10.0f, 0.5f), 3.0f, TabGroup.Addons, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.Allergic])
                .SetValueFormat(OptionFormat.Seconds);
        }

        public void Init() => allergicTargets.Clear();

        public void Add(byte playerId, bool gameIsLoading = true)
        {
            var allergicPlayer = PlayerControl.LocalPlayer;
            if (AmongUsClient.Instance.AmHost && allergicPlayer.IsAlive())
            {
                var eligibleTargets = new List<PlayerControl>();
                foreach (var p in PlayerControl.AllPlayerControls)
                {
                    if (p.PlayerId != playerId && !p.Data.IsDead)
                    {
                        eligibleTargets.Add(p);
                    }
                }


                if (eligibleTargets.Count > 0)
                {
                    var selectedTarget = eligibleTargets[UnityEngine.Random.Range(0, eligibleTargets.Count)];
                    allergicTargets[allergicPlayer.PlayerId] = selectedTarget.PlayerId;
                    proximityTimers[allergicPlayer.PlayerId] = 0f;

                    SendRPC(allergicPlayer.PlayerId, selectedTarget.PlayerId, true);
                    Logger.Info($"{allergicPlayer.GetNameWithRole()} is now allergic to {selectedTarget.GetNameWithRole()}", "Allergic");
                }
                else
                {
                    Logger.Info($"{allergicPlayer.GetNameWithRole()} has no valid targets to be allergic to.", "Allergic");
                }
            }
        }

        private void OnFixedUpdate(PlayerControl player, bool lowLoad, long nowTime)
        {
            if (!player.Is(CustomRoles.Allergic) || player.Data.IsDead) return;

            if (!allergicTargets.TryGetValue(player.PlayerId, out byte targetId))
            {
                Logger.Info("No valid target for allergic player.", "Allergic");
                return;
            }

            PlayerControl targetPlayer = null;
            foreach (var p in PlayerControl.AllPlayerControls)
            {
                if (p.PlayerId == targetId)
                {
                    targetPlayer = p;
                    break;
                }
            }


            if (targetPlayer == null || targetPlayer.Data.IsDead) return;

            var distance = Vector3.Distance(player.transform.position, targetPlayer.transform.position);

            if (distance <= AllergicDistance.GetFloat())
            {
                proximityTimers[player.PlayerId] += Time.deltaTime;

                if (proximityTimers[player.PlayerId] >= AllergicTime.GetFloat())
                {
                    KillAllergic(player);
                    proximityTimers[player.PlayerId] = 0f;
                }
            }
            else
            {
                proximityTimers[player.PlayerId] = 0f;
            }
        }

        private void KillAllergic(PlayerControl player)
        {
            Logger.Info($"{player.GetNameWithRole()} has died due to allergic reaction proximity.", "Allergic");

            player.SetDeathReason(PlayerState.DeathReason.Suicide);
            player.RpcMurderPlayer(player); // Broadcasts the player's death to all clients
        }

        public void Remove(byte playerId)
        {
            if (allergicTargets.ContainsKey(playerId))
            {
                allergicTargets.Remove(playerId);
                Logger.Info($"Removed Allergic target for player {playerId}", "Allergic");
            }
        }

        private static void SendRPC(byte allergicPlayerId, byte targetId, bool setTarget)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(
                PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable);

            writer.Write(setTarget);
            writer.Write(allergicPlayerId);
            writer.Write(targetId);

            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }

        public static string GetMarkOthers(PlayerControl seer, PlayerControl target, bool isForMeeting = false)
        {
            if (allergicTargets.TryGetValue(seer.PlayerId, out byte targetId) && target.PlayerId == targetId)
            {
                return Utils.ColorString(Utils.GetRoleColor(CustomRoles.Allergic), "⚠");
            }
            return string.Empty;
        }
    }
}
