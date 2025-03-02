namespace TOHE;

public static class PetsPatch
{
    public static void RpcRemovePet(this PlayerControl pc)
    {
        if (!Options.RemovePetsAtDeadPlayers.GetBool()) return;
        if (pc == null || !pc.Data.IsDead) return;
        if (!GameStates.IsInGame) return;
        if (pc.CurrentOutfit.PetId == "") return;

        pc.RpcSetPet("");
    }
}
