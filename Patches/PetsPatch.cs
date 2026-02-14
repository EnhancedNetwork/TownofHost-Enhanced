namespace TOHE;

public static class PetsPatch
{
    public static void RpcRemovePet(this PlayerControl pc)
    {
        if (!pc || pc.IsAlive()) return;
        if (!GameStates.IsInGame) return;
        if (!Options.RemovePetsAtDeadPlayers.GetBool()) return;
        if (pc.CurrentOutfit.PetId == "") return;

        pc.RpcSetPet("");
    }
}
