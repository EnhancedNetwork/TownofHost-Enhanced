using AmongUs.InnerNet.GameDataMessages;
using TOHE.Modules.Rpc;

namespace TOHE.Modules;

public static class OutfitManager
{
    public static void ResetPlayerOutfit(this PlayerControl player, NetworkedPlayerInfo.PlayerOutfit Outfit = null, bool setNamePlate = false, uint newLevel = 500, bool force = false)
    {
        Outfit ??= Main.PlayerStates[player.PlayerId].NormalOutfit;

        void Setoutfit()
        {
            if (player == null || Outfit == null) return;

            player.SetName(Outfit.PlayerName);
            Main.AllPlayerNames[player.PlayerId] = Outfit.PlayerName;
            RPC.SyncAllPlayerNames();

            player.SetColor(Outfit.ColorId);
            player.SetHat(Outfit.HatId, Outfit.ColorId);
            player.SetSkin(Outfit.SkinId, Outfit.ColorId);
            player.SetVisor(Outfit.VisorId, Outfit.ColorId);
            player.SetPet(Outfit.PetId);

            if (setNamePlate)
            {
                player.SetNamePlate(Outfit.NamePlateId);
            }

            if (newLevel != 500)
            {
                player.SetLevel(newLevel);
                var setLevel = new RpcSetLevelMessage(player.NetId, newLevel);
                RpcUtils.LateBroadcastReliableMessage(setLevel);
            }

            var setOutfit = new RpcSetOutfit(player.NetId, player.Data.NetId, Outfit, true, setNamePlate);
            RpcUtils.LateBroadcastReliableMessage(setOutfit);

            //cannot use currentoutfit type because of mushroom mixup . .
            var OutfitTypeSet = player.CurrentOutfitType != PlayerOutfitType.Shapeshifted ? PlayerOutfitType.Default : PlayerOutfitType.Shapeshifted;

            player.Data.SetOutfit(OutfitTypeSet, Outfit);

            player.Data.MarkDirty();
        }

        if (player.CheckCamoflague() && !force)
        {
            Main.LateOutfits[player.PlayerId] = Setoutfit;
        }
        else
        {
            Main.LateOutfits.Remove(player.PlayerId);
            Setoutfit();
        }
    }

    public static void SetNewOutfit(this PlayerControl player, NetworkedPlayerInfo.PlayerOutfit newOutfit, bool setName = true, bool setNamePlate = true, uint newLevel = 500)
    {
        if (setName)
        {
            player.SetName(newOutfit.PlayerName);
            Main.AllPlayerNames[player.PlayerId] = newOutfit.PlayerName;

            RPC.SyncAllPlayerNames();
        }

        player.SetColor(newOutfit.ColorId);
        player.SetHat(newOutfit.HatId, newOutfit.ColorId);
        player.SetSkin(newOutfit.SkinId, newOutfit.ColorId);
        player.SetVisor(newOutfit.VisorId, newOutfit.ColorId);
        player.SetPet(newOutfit.PetId);

        if (setNamePlate)
        {
            player.SetNamePlate(newOutfit.NamePlateId);
        }

        if (newLevel != 500)
        {
            player.SetLevel(newLevel);
            var setLevel = new RpcSetLevelMessage(player.NetId, newLevel);
            RpcUtils.LateBroadcastReliableMessage(setLevel);
        }

        var setOutfit = new RpcSetOutfit(player.NetId, player.Data.NetId, newOutfit, setName, setNamePlate);
        RpcUtils.LateBroadcastReliableMessage(setOutfit);
    }
}
