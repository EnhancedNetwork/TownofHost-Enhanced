using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static TOHE.Options;

namespace TOHE.Roles.Impostor;

public static class OverKiller
{
    private static readonly int Id = 24300;
    public static Dictionary<byte, (int, int, Vector2)> MurderTargetLateTask = [];
    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.OtherRoles, CustomRoles.OverKiller);
    }
    public static void Init()
    {
        MurderTargetLateTask = [];
    }
    public static void OnMurderPlayer(PlayerControl killer, PlayerControl target)
    {
        if (killer.PlayerId == target.PlayerId || target == null) return;

        target.SetRealKiller(killer);
        Main.PlayerStates[target.PlayerId].deathReason = PlayerState.DeathReason.Dismembered;
        target.Data.IsDead = true;

        if (!Main.OverDeadPlayerList.Contains(target.PlayerId)) Main.OverDeadPlayerList.Add(target.PlayerId);
        //var ops = target.GetCustomPosition();
        var rd = IRandom.Instance;

        if (target.Is(CustomRoles.Avanger))
        {
            var pcList = Main.AllAlivePlayerControls.Where(x => x.PlayerId != target.PlayerId); //No need to do extra check cause nobody is winning
            pcList.Do(x =>
            {
                x.Data.IsDead = true;
                x.SetRealKiller(target);
                Main.PlayerStates[x.PlayerId].deathReason = PlayerState.DeathReason.Revenge;
                target.RpcSpecificMurderPlayer(x, x);
            });
            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.None);
            return;
        }

        _ = new LateTask(() =>
        {
            for (int i = 0; i <= 19; i++)
            {
                if (GameStates.IsMeeting) break;
                if (!target.AmOwner)
                    target.MurderPlayer(target, ExtendedPlayerControl.ResultFlags);
                Main.AllAlivePlayerControls.Where(x => x.PlayerId != target.PlayerId && !x.AmOwner)
                .Do(x => target.RpcSpecificMurderPlayer(target, x));
            }
        }, 0.2f, "OverKillerShowBodies"); //25 exactly takes over the whole screen

        _ = new LateTask(() =>
        {
            if (!MurderTargetLateTask.ContainsKey(target.PlayerId))
                MurderTargetLateTask.Add(target.PlayerId, (0, 0, target.GetCustomPosition()));
        }, 0.6f, "OverKillerLateKill");
    }

    public static void OnFixedUpdate(PlayerControl target)
    {
        if (target == null || !target.Data.IsDead) return;
        var ops = MurderTargetLateTask[target.PlayerId].Item3;

        if (MurderTargetLateTask[target.PlayerId].Item1 > 19) //on fix update updates 30 times pre second
        {
            if (MurderTargetLateTask[target.PlayerId].Item2 < 5)
            {   
                var rd = IRandom.Instance;

                Vector2 location = new(ops.x + ((float)(rd.Next(1, 200) - 100) / 100), ops.y + ((float)(rd.Next(1, 200) - 100) / 100));
                target.RpcTeleport(location);
                target.RpcMurderPlayerV3(target);
                MurderTargetLateTask[target.PlayerId] = (0, MurderTargetLateTask[target.PlayerId].Item2 + 1, ops);
            }
            else MurderTargetLateTask.Remove(target.PlayerId);
        }
        else
            MurderTargetLateTask[target.PlayerId] = (MurderTargetLateTask[target.PlayerId].Item1 + 1, MurderTargetLateTask[target.PlayerId].Item2, ops);
    }
}
