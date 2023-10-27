using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using TOHE.Roles.Crewmate;
using TOHE.Roles.Neutral;
using UnityEngine;
using static TOHE.Options;
using System.Threading;

namespace TOHE.Roles.Impostor
{
    public static class OverKiller
    {
        private static readonly int Id = 16900;
        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.OtherRoles, CustomRoles.OverKiller);
        }
        public static void OnMurderPlayer(PlayerControl killer, PlayerControl target)
        {
            if (killer.PlayerId == target.PlayerId || target == null) return;

            target.SetRealKiller(killer);
            Main.PlayerStates[target.PlayerId].deathReason = PlayerState.DeathReason.Dismembered;
            target.Data.IsDead = true;

            if (!Main.OverDeadPlayerList.Contains(target.PlayerId)) Main.OverDeadPlayerList.Add(target.PlayerId);
            var ops = target.GetTruePosition();
            var rd = IRandom.Instance;

            _ = new LateTask(() =>
            {
                for (int i = 0; i <= 25; i++)
                {
                    if (GameStates.IsMeeting) break;
                    if (!target.AmOwner)
                        target.MurderPlayer(target, ExtendedPlayerControl.ResultFlags);
                    Main.AllAlivePlayerControls.Where(x => x.PlayerId != target.PlayerId && !x.AmOwner)
                    .Do(x => target.RpcSpecificMurderPlayer(target, x));
                }
            }, 0.3f, "OverKillerShowBodies"); //25 exactly takes over the whole screen

            if (target.Is(CustomRoles.Avanger))
            {
                var pcList = Main.AllAlivePlayerControls.Where(x => x.PlayerId != target.PlayerId || Pelican.IsEaten(x.PlayerId) || Medic.ProtectList.Contains(x.PlayerId) || target.Is(CustomRoles.Pestilence));
                pcList.Do(x => x.SetRealKiller(target));
                pcList.Do(x => Main.PlayerStates[x.PlayerId].deathReason = PlayerState.DeathReason.Revenge);
                pcList.Do(x => x.RpcMurderPlayerV3(x));
            }

            //for (int i = 0; i < 5; i++)
            //{
            //    if (GameStates.IsMeeting) break;
            //    Vector2 location = new(ops.x + ((float)(rd.Next(0, 201) - 100) / 100), ops.y + ((float)(rd.Next(0, 201) - 100) / 100));
            //    location += new Vector2(0, 0.3636f);

            //    target.RpcTeleport(location);
            //    Thread.Sleep(100);
            //    target.RpcMurderPlayerV3(target);
            //}
            //killer.RpcTeleport(ops);

            /*target wont move in clients' view , possibly bcz the delay of snapto & murderplayer
            Niko dk how 2 fix it. leave this out until some one fix it */
        }
    }
}
