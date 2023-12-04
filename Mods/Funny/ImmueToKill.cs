using HarmonyLib;
using Hazel;
using InnerNet;

namespace TOHE
{
    [HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.LateUpdate))]
    public static class ImmueToKill
    {
        public static void Postfix()
        {
            var player = PlayerControl.LocalPlayer;
            if (Main.GodMode.Value == true && GameStates.IsInGame)
            {
                if (!player.Data.IsDead && !player.IsProtected())
                {
                    MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(player.NetId, (byte)RpcCalls.ProtectPlayer, SendOption.Reliable);
                    writer.WriteNetObject(player);
                    writer.Write(18);
                    AmongUsClient.Instance.FinishRpcImmediately(writer);

                }
            }
        }

    }
}
