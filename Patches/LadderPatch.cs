using UnityEngine;

namespace TOHE;

public class FallFromLadder
{
    public static Dictionary<byte, Vector3> TargetLadderData;
    private static int Chance => (Options.LadderDeathChance as StringOptionItem).GetChance();
    public static void Reset()
    {
        TargetLadderData = [];
    }
    public static void OnClimbLadder(PlayerPhysics player, Ladder source)
    {
        if (!Options.LadderDeath.GetBool() || player.myPlayer.Is(CustomRoles.Solsticer)) return;
        var sourcePos = source.transform.position;
        var targetPos = source.Destination.transform.position;
        //降りているのかを検知
        if (sourcePos.y > targetPos.y)
        {
            int chance = IRandom.Instance.Next(1, 101);
            if (chance <= Chance)
            {
                TargetLadderData[player.myPlayer.PlayerId] = targetPos;
            }
        }
    }
    public static void FixedUpdate(PlayerControl player)
    {
        if (player.Data.Disconnected) return;
        if (TargetLadderData.ContainsKey(player.PlayerId))
        {
            if (Utils.GetDistance(TargetLadderData[player.PlayerId], player.transform.position) < 0.5f)
            {
                if (player.Data.IsDead) return;
                // To put in LateTask, put in a death decision first
                player.Data.IsDead = true;
                _ = new LateTask(() =>
                {
                    Vector2 targetPos = (Vector2)TargetLadderData[player.PlayerId] + new Vector2(0.1f, 0f);
                    ushort num = (ushort)(NetHelpers.XRange.ReverseLerp(targetPos.x) * 65535f);
                    ushort num2 = (ushort)(NetHelpers.YRange.ReverseLerp(targetPos.y) * 65535f);

                    player.SetDeathReason(PlayerState.DeathReason.Fall);

                    CustomRpcSender sender = CustomRpcSender.Create("LadderFallRpc", sendOption: Hazel.SendOption.None);
                    sender.AutoStartRpc(player.NetTransform.NetId, (byte)RpcCalls.SnapTo)
                        .Write(num)
                        .Write(num2)
                      .EndRpc();
                    sender.AutoStartRpc(player.NetId, (byte)RpcCalls.MurderPlayer)
                        .WriteNetObject(player)
                        .Write((int)ExtendedPlayerControl.ResultFlags)
                      .EndRpc();
                    sender.SendMessage();
                    player.NetTransform.SnapTo(targetPos);
                    player.MurderPlayer(player, ExtendedPlayerControl.ResultFlags);
                }, 0.05f, "Ladder Fall Task");
            }
        }
    }
}
[HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.ClimbLadder))]
class LadderPatch
{
    public static void Postfix(PlayerPhysics __instance, Ladder source)
    {
        FallFromLadder.OnClimbLadder(__instance, source);
    }
}