namespace TOHE;

public static class PetsPatch
{
    public static void RpcRemovePet(this PlayerControl pc)
    {
        if (pc == null || !pc.Data.IsDead) return;
        if (!GameStates.IsInGame) return;
        if (!Options.RemovePetsAtDeadPlayers.GetBool()) return;

        var sender = CustomRpcSender.Create(name: "Remove Pet At Dead Player");

        pc.RpcSetPet("");
        sender.AutoStartRpc(pc.NetId, (byte)RpcCalls.SetPetStr)
            .Write("")
            .EndRpc();
        sender.SendMessage();
    }
}