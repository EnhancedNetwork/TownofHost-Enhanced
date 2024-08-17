namespace TOHE.Modules;

public static class OutfitManager
{
    public static void ResetPlayerOutfit(this PlayerControl player, NetworkedPlayerInfo.PlayerOutfit Outfit = null, uint newLevel = 500, bool force = false)
    {
        Outfit ??= Main.PlayerStates[player.PlayerId].NormalOutfit;

        void Setoutfit()
        {
            var sender = CustomRpcSender.Create(name: $"Reset PlayerOufit for 『{player.Data.PlayerName}』");

            player.SetName(Outfit.PlayerName);
            sender.AutoStartRpc(player.NetId, (byte)RpcCalls.SetName)
                .Write(player.Data.NetId)
                .Write(Outfit.PlayerName)
            .EndRpc();

            Main.AllPlayerNames[player.PlayerId] = Outfit.PlayerName;

            player.SetColor(Outfit.ColorId);
            sender.AutoStartRpc(player.NetId, (byte)RpcCalls.SetColor)
                .Write(player.Data.NetId)
                .Write((byte)Outfit.ColorId)
            .EndRpc();

            player.SetHat(Outfit.HatId, Outfit.ColorId);
            sender.AutoStartRpc(player.NetId, (byte)RpcCalls.SetHatStr)
                .Write(Outfit.HatId)
                .Write(player.GetNextRpcSequenceId(RpcCalls.SetHatStr))
            .EndRpc();

            player.SetSkin(Outfit.SkinId, Outfit.ColorId);
            sender.AutoStartRpc(player.NetId, (byte)RpcCalls.SetSkinStr)
                .Write(Outfit.SkinId)
                .Write(player.GetNextRpcSequenceId(RpcCalls.SetSkinStr))
            .EndRpc();

            player.SetVisor(Outfit.VisorId, Outfit.ColorId);
            sender.AutoStartRpc(player.NetId, (byte)RpcCalls.SetVisorStr)
                .Write(Outfit.VisorId)
                .Write(player.GetNextRpcSequenceId(RpcCalls.SetVisorStr))
            .EndRpc();

            player.SetPet(Outfit.PetId);
            sender.AutoStartRpc(player.NetId, (byte)RpcCalls.SetPetStr)
                .Write(Outfit.PetId)
                .Write(player.GetNextRpcSequenceId(RpcCalls.SetPetStr))
                .EndRpc();

            player.SetNamePlate(Outfit.NamePlateId);
            sender.AutoStartRpc(player.NetId, (byte)RpcCalls.SetNamePlateStr)
                .Write(Outfit.NamePlateId)
                .Write(player.GetNextRpcSequenceId(RpcCalls.SetNamePlateStr))
                .EndRpc();

            if (newLevel != 500)
            {
                player.SetLevel(newLevel);
                sender.AutoStartRpc(player.NetId, (byte)RpcCalls.SetLevel)
                    .WritePacked(newLevel)
                    .EndRpc();
            }

            sender.SendMessage();

            //cannot use currentoutfit type because of mushroom mixup . .
            var OutfitTypeSet = player.CurrentOutfitType != PlayerOutfitType.Shapeshifted ? PlayerOutfitType.Default : PlayerOutfitType.Shapeshifted;

            player.Data.SetOutfit(OutfitTypeSet, Outfit);

            //Used instead of GameData.Instance.DirtyAllData();
            foreach (var innerNetObject in GameData.Instance.AllPlayers)
            {
                innerNetObject.SetDirtyBit(uint.MaxValue);
            }
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
        // Start to set Outfit
        var sender = CustomRpcSender.Create(name: $"SetOutfit({player.Data.PlayerName})");

        if (setName)
        {
            player.SetName(newOutfit.PlayerName);
            sender.AutoStartRpc(player.NetId, (byte)RpcCalls.SetName)
                .Write(player.Data.NetId)
                .Write(newOutfit.PlayerName)
            .EndRpc();

            Main.AllPlayerNames[player.PlayerId] = newOutfit.PlayerName;
        }

        player.SetColor(newOutfit.ColorId);
        sender.AutoStartRpc(player.NetId, (byte)RpcCalls.SetColor)
            .Write(player.Data.NetId)
            .Write((byte)newOutfit.ColorId)
        .EndRpc();

        player.SetHat(newOutfit.HatId, newOutfit.ColorId);
        sender.AutoStartRpc(player.NetId, (byte)RpcCalls.SetHatStr)
        .Write(newOutfit.HatId)
            .Write(player.GetNextRpcSequenceId(RpcCalls.SetHatStr))
        .EndRpc();

        player.SetSkin(newOutfit.SkinId, newOutfit.ColorId);
        sender.AutoStartRpc(player.NetId, (byte)RpcCalls.SetSkinStr)
        .Write(newOutfit.SkinId)
            .Write(player.GetNextRpcSequenceId(RpcCalls.SetSkinStr))
        .EndRpc();

        player.SetVisor(newOutfit.VisorId, newOutfit.ColorId);
        sender.AutoStartRpc(player.NetId, (byte)RpcCalls.SetVisorStr)
            .Write(newOutfit.VisorId)
            .Write(player.GetNextRpcSequenceId(RpcCalls.SetVisorStr))
        .EndRpc();

        player.SetPet(newOutfit.PetId);
        sender.AutoStartRpc(player.NetId, (byte)RpcCalls.SetPetStr)
            .Write(newOutfit.PetId)
            .Write(player.GetNextRpcSequenceId(RpcCalls.SetPetStr))
            .EndRpc();

        if (setNamePlate)
        {
            player.SetNamePlate(newOutfit.NamePlateId);
            sender.AutoStartRpc(player.NetId, (byte)RpcCalls.SetNamePlateStr)
                .Write(newOutfit.NamePlateId)
                .Write(player.GetNextRpcSequenceId(RpcCalls.SetNamePlateStr))
            .EndRpc();
        }

        if (newLevel != 500)
        {
            player.SetLevel(newLevel);
            sender.AutoStartRpc(player.NetId, (byte)RpcCalls.SetLevel)
                .WritePacked(newLevel)
                .EndRpc();
        }

        sender.SendMessage();
    }
}
