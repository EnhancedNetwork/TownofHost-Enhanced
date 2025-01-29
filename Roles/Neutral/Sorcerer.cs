using AmongUs.GameOptions;
using Hazel;
using TOHE.Roles.Core;
using UnityEngine;
using System.Collections.Generic;

namespace TOHE.Roles.Neutral
{
    internal class Sorcerer : RoleBase
    {
        // ----------- Role Setup ----------- //
        public override CustomRoles Role => CustomRoles.Sorcerer;
        private const int RoleId = 33100;
        public override bool IsDesyncRole => true;
        public override CustomRoles ThisRoleBase => CustomRoles.Impostor; 
        public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralKilling;

        // ----------- Settings ----------- //
        private static float MarkRange = 2.5f; // How close you need to be 
        private bool usedSecondChance = false; // Checking if respawn has been used
        private List<PlayerControl> markedPlayers = new List<PlayerControl>();

        public override void Init() 
        {
            markedPlayers.Clear(); 
            usedSecondChance = false; // Reset second chance
            }

        // Try to mark a player (with distance check)
        public void AttemptMarkPlayer(PlayerControl target) 
        {
            if (target == null) return; 
            if (markedPlayers.Contains(target)) return;

            // Calculate distance 
            float distance = Vector2.Distance(PlayerControl.LocalPlayer.transform.position, target.transform.position);

            if (distance <= MarkRange) {
                markedPlayers.Add(target);
                
            }
        }

        public override void AfterMeetingTasks() 
        {
            if (PlayerControl.LocalPlayer.IsAlive() && !usedSecondChance) 
            {
                PlayerControl.LocalPlayer.RpcRevive();
                usedSecondChance = true;
            }
        }

        // Check if all marked players are dead
        private bool AreAllMarkedPlayersDead() 
        {
            foreach (var player in markedPlayers) {
                if (!player.Data.IsDead) return false;
            }
            return true; // All are dead
        }

        // Check if only 3 players or fewer are alive
        private bool IsLastThreePlayersAlive() 
        {
            int aliveCount = 0;
            foreach (var player in PlayerControl.AllPlayerControls) 
            {
                if (!player.Data.IsDead) 
                {
                    aliveCount++;
                }
            }
            return aliveCount <= 3;
        }

        // Win: all marked players dead + 3 or fewer alive
        private bool CheckWin() 
        {
            return AreAllMarkedPlayersDead() && IsLastThreePlayersAlive();
        }

    }
}
