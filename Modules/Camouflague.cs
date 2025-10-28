using AmongUs.Data;
using TOHE.Modules;
using TOHE.Roles.Impostor;

namespace TOHE;

static class PlayerOutfitExtension
{
    public static NetworkedPlayerInfo.PlayerOutfit Set(this NetworkedPlayerInfo.PlayerOutfit instance, string playerName, int colorId, string hatId, string skinId, string visorId, string petId, string nameplateId)
    {
        instance.PlayerName = playerName;
        instance.ColorId = colorId;
        instance.HatId = hatId;
        instance.SkinId = skinId;
        instance.VisorId = visorId;
        instance.PetId = petId;
        instance.NamePlateId = nameplateId;
        return instance;
    }
    public static bool Compare(this NetworkedPlayerInfo.PlayerOutfit instance, NetworkedPlayerInfo.PlayerOutfit targetOutfit)
    {
        return instance.ColorId == targetOutfit.ColorId &&
                instance.HatId == targetOutfit.HatId &&
                instance.SkinId == targetOutfit.SkinId &&
                instance.VisorId == targetOutfit.VisorId &&
                instance.PetId == targetOutfit.PetId;

    }
    public static string GetString(this NetworkedPlayerInfo.PlayerOutfit instance)
    {
        return $"{instance.PlayerName} Color:{instance.ColorId} {instance.HatId} {instance.SkinId} {instance.VisorId} {instance.PetId}";
    }
    public static NetworkedPlayerInfo.PlayerOutfit GetRandomOutfit()
    {
        var random = IRandom.Instance;
        var tempChanceSetRandomSkin = random.Next(0, 101);

        return Options.KPDCamouflageMode.GetValue() switch
        {
            // Random outfit
            2 => new NetworkedPlayerInfo.PlayerOutfit()
            {
                ColorId = random.Next(Palette.PlayerColors.Length),
                HatId = HatManager.Instance.allHats[tempChanceSetRandomSkin >= random.Next(0, 101) ? random.Next(0, HatManager.Instance.allHats.Length) : 0].ProdId,
                SkinId = HatManager.Instance.allSkins[tempChanceSetRandomSkin >= random.Next(0, 101) ? random.Next(0, HatManager.Instance.allSkins.Length) : 0].ProdId,
                VisorId = HatManager.Instance.allVisors[tempChanceSetRandomSkin >= random.Next(0, 101) ? random.Next(0, HatManager.Instance.allVisors.Length) : 0].ProdId,
                PetId = HatManager.Instance.allPets[tempChanceSetRandomSkin >= random.Next(0, 101) ? random.Next(0, HatManager.Instance.allPets.Length) : 0].ProdId,
                //NamePlateId = HatManager.Instance.allNamePlates[tempChanceSetRandomSkin >= random.Next(0, 101) ? random.Next(0, HatManager.Instance.allNamePlates.Length) : 0].ProdId,
            },

            3 => new NetworkedPlayerInfo.PlayerOutfit().Set("", random.Next(Palette.PlayerColors.Length), "", "", "", "", ""), // Only random colors

            _ => new NetworkedPlayerInfo.PlayerOutfit().Set("", 15, "", "", "", "", ""), // defalt
        };
    }
}
public static class Camouflage
{
    public static bool IsCamouflage;

    static NetworkedPlayerInfo.PlayerOutfit CamouflageOutfit = new NetworkedPlayerInfo.PlayerOutfit().Set("", 15, "", "", "", "", ""); // Default

    public static List<byte> ResetSkinAfterDeathPlayers = [];
    public static Dictionary<byte, NetworkedPlayerInfo.PlayerOutfit> PlayerSkins = [];
    public static bool IsActive;

    public static void Init()
    {
        IsCamouflage = false;
        PlayerSkins.Clear();
        ResetSkinAfterDeathPlayers.Clear();

        IsActive = Options.CommsCamouflage.GetBool() && !(Options.DisableOnSomeMaps.GetBool() &&
            (
            (Options.DisableOnSkeld.GetBool() && GameStates.SkeldIsActive) ||
            (Options.DisableOnMira.GetBool() && GameStates.MiraHQIsActive) ||
            (Options.DisableOnPolus.GetBool() && GameStates.PolusIsActive) ||
            (Options.DisableOnDleks.GetBool() && GameStates.DleksIsActive) ||
            (Options.DisableOnAirship.GetBool() && GameStates.AirshipIsActive) ||
            (Options.DisableOnFungle.GetBool() && GameStates.FungleIsActive)
            ));

        switch (Options.KPDCamouflageMode.GetValue())
        {
            case 0: // Default
                CamouflageOutfit = new NetworkedPlayerInfo.PlayerOutfit()
                    .Set("", 15, "", "", "", "", "");
                break;

            case 1: // Host's outfit
                CamouflageOutfit = new NetworkedPlayerInfo.PlayerOutfit()
                    .Set("", DataManager.Player.Customization.Color, DataManager.Player.Customization.Hat, DataManager.Player.Customization.Skin, DataManager.Player.Customization.Visor, DataManager.Player.Customization.Pet, "");
                break;

            case 2: // Random outfit
            case 3: // Only random color
                CamouflageOutfit = PlayerOutfitExtension.GetRandomOutfit();
                break;

            case 4: // Karpe
                CamouflageOutfit = new NetworkedPlayerInfo.PlayerOutfit()
                    .Set("", 13, "hat_pk05_Plant", "", "visor_BubbleBumVisor", "", "");
                break;

            case 5: // Lauryn
                CamouflageOutfit = new NetworkedPlayerInfo.PlayerOutfit()
                    .Set("", 13, "hat_rabbitEars", "skin_Bananaskin", "visor_BubbleBumVisor", "pet_Pusheen", "");
                break;

            case 6: // Lime
                CamouflageOutfit = new NetworkedPlayerInfo.PlayerOutfit()
                    .Set("", 0, "hat_mira_headset_yellow", "skin_SuitB", "visor_lollipopCrew", "pet_EmptyPet", "");
                break;

            case 7: // Pyro
                CamouflageOutfit = new NetworkedPlayerInfo.PlayerOutfit()
                    .Set("", 17, "hat_pkHW01_Witch", "skin_greedygrampaskin", "visor_Plsno", "pet_Pusheen", "");
                break;

            case 8: // ryuk
                CamouflageOutfit = new NetworkedPlayerInfo.PlayerOutfit()
                    .Set("", 7, "hat_theohair", "skin_Theoskin", "visor_Carrot", "pet_Snow", "");
                break;

            case 9: // Gurge44
                CamouflageOutfit = new NetworkedPlayerInfo.PlayerOutfit()
                    .Set("", 7, "hat_pk04_Snowman", "", "", "", "");
                break;

            case 10: // TommyXL
                CamouflageOutfit = new NetworkedPlayerInfo.PlayerOutfit()
                    .Set("", 17, "hat_baseball_Black", "skin_Scientist-Darkskin", "visor_pusheenSmileVisor", "pet_Pip", "");
                break;
            case 11: // Sarha, Sponsor
                CamouflageOutfit = new NetworkedPlayerInfo.PlayerOutfit()
                    .Set("", 17, "hat_mira_flower", "skin_PusheenPurpleskin", "visor_hl_hmph", "pet_Charles", "");
                break;
        }
    }
    public static void CheckCamouflage()
    {
        if (!(AmongUsClient.Instance.AmHost && (Options.CommsCamouflage.GetBool() || Camouflager.HasEnabled))) return;

        var oldIsCamouflage = IsCamouflage;

        IsCamouflage = (Utils.IsActive(SystemTypes.Comms) && IsActive) || Camouflager.AbilityActivated;

        if (oldIsCamouflage != IsCamouflage)
        {
            foreach (var pc in Main.AllPlayerControls)
            {
                RpcSetSkin(pc);

                if (!IsCamouflage && !pc.IsAlive())
                {
                    pc.RpcRemovePet();
                }
            }
            if (Main.CurrentServerIsVanilla && Options.BypassRateLimitAC.GetBool())
            {
                Main.Instance.StartCoroutine(Utils.NotifyEveryoneAsync(speed: 5));
            }
            else
            {
                Utils.DoNotifyRoles();
            }
        }
    }
    public static void RpcSetSkin(PlayerControl target, bool ForceRevert = false, bool RevertToDefault = false, bool GameEnd = false)
    {
        if (!(AmongUsClient.Instance.AmHost && (Options.CommsCamouflage.GetBool() || Camouflager.HasEnabled))) return;
        if (target == null) return;

        var targetId = target.PlayerId;

        // if player dead, and Camouflage active, return
        if (IsCamouflage && Main.PlayerStates[targetId].IsDead)
        {
            return;
        }

        // Check which Outfit needs to be set
        var newOutfit = Options.KPDCamouflageMode.GetValue() is 2 or 3
            ? PlayerOutfitExtension.GetRandomOutfit()
            : CamouflageOutfit;


        // if is not Camouflage or need force revert skins
        if (!IsCamouflage || ForceRevert)
        {
            // if player are a shapeshifter, change to the id of your current Outfit
            if (Main.CheckShapeshift.GetValueOrDefault(targetId, false) && !RevertToDefault)
            {
                targetId = Main.ShapeshiftTarget[targetId];
            }


            bool Hasovveride = Main.OvverideOutfit.TryGetValue(targetId, out var RealOutfit);

            // if game not end and Something clone skins
            if (!GameEnd && Hasovveride)
            {
                Logger.Info($"{RealOutfit.outfit.SkinId}", "RealOutfit Check");
                newOutfit = RealOutfit.outfit;
            }
            else
            {
                // if game end, set normal name
                if (GameEnd && Hasovveride)
                {
                    targetId.GetPlayer()?.RpcSetName(RealOutfit.name);
                }

                // Set Outfit
                newOutfit = PlayerSkins[targetId];
            }
        }

        // if the current Outfit is the same, return it
        if (newOutfit.Compare(target.Data.DefaultOutfit)) return;

        Logger.Info($"playerId {target.PlayerId} newOutfit={newOutfit.GetString().RemoveHtmlTags()}", "RpcSetSkin");
        target.SetNewOutfit(newOutfit, setName: false, setNamePlate: false);
    }
}
