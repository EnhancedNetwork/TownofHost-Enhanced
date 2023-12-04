using HarmonyLib;
using System;

namespace TOHE;

[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.FixedUpdate))]
public static class Ship_SabotagePostfix
{
    //Postfix patch of ShipStatus.FixedUpdate to sabotage different systems
    public static void Postfix(ShipStatus __instance)
    {
        byte currentMapID = Utils.getCurrentMapID();

        if (CheatSettings.reactorSab)
        { //Reactor sabotages

            if (currentMapID == 2)
            { //Polus uses has SystemTypes.Laboratory instead of SystemTypes.Reactor

                var labSystem = __instance.Systems[SystemTypes.Laboratory].Cast<ReactorSystemType>();

                if (labSystem.IsActive)
                {

                    __instance.RpcUpdateSystem(SystemTypes.Laboratory, 16); //Repair reactor

                }
                else
                {

                    __instance.RpcUpdateSystem(SystemTypes.Laboratory, 128); //Sabotage reactor

                }

            }
            else if (currentMapID == 4)
            { //Airship uses HeliSabotageSystem to sabotage reactor

                var reactSystem = __instance.Systems[SystemTypes.HeliSabotage].Cast<HeliSabotageSystem>();

                if (reactSystem.IsActive)
                {
                    //Repair reactor
                    __instance.RpcUpdateSystem(SystemTypes.HeliSabotage, 16 | 0);
                    __instance.RpcUpdateSystem(SystemTypes.HeliSabotage, 16 | 1);

                }
                else
                {
                    //Sabotage reactor
                    __instance.RpcUpdateSystem(SystemTypes.HeliSabotage, 128 | 0);
                    __instance.RpcUpdateSystem(SystemTypes.HeliSabotage, 128 | 1);

                }
            }
            else
            { //Skeld & MiraHQ & Fungle behave normally 
                var reactSystem = __instance.Systems[SystemTypes.Reactor].Cast<ReactorSystemType>();
                if (reactSystem.IsActive)
                {
                    __instance.RpcUpdateSystem(SystemTypes.Reactor, 16);
                }
                else
                {
                    __instance.RpcUpdateSystem(SystemTypes.Reactor, 128);
                }
            }

            CheatSettings.reactorSab = false; //Button behaviour

        }
        else if (CheatSettings.oxygenSab)
        { //Oxygen sabotages

            if (currentMapID != 4 && currentMapID != 2 && currentMapID != 5)
            {

                var oxygenSystem = __instance.Systems[SystemTypes.LifeSupp].Cast<LifeSuppSystemType>();

                if (oxygenSystem.IsActive)
                {
                    __instance.RpcUpdateSystem(SystemTypes.LifeSupp, 16); //Repair oxygen
                }
                else
                {
                    __instance.RpcUpdateSystem(SystemTypes.LifeSupp, 128); //Sabotage oxygen
                }

            }
            else
            {
                HudManager.Instance.Notifier.AddItem("Oxygen system not present on this map"); //Polus & Airship have NO oxygen system
            }

            CheatSettings.oxygenSab = false; //Button behaviour

        }
        else if (CheatSettings.commsSab)
        { //Communications sabotages

            if (currentMapID == 1 || currentMapID == 5)
            { //MiraHQ uses HqHudSystemType to sabotage communications
                var hqcommsSystem = __instance.Systems[SystemTypes.Comms].Cast<HqHudSystemType>();
                if (hqcommsSystem.IsActive)
                {
                    __instance.RpcUpdateSystem(SystemTypes.Comms, 16); //Repair communications
                    __instance.RpcUpdateSystem(SystemTypes.Comms, 17);
                }
                else
                {
                    __instance.RpcUpdateSystem(SystemTypes.Comms, 128); //Sabotage communications
                }
            }
            else
            {//Polus, Skeld and Airship Fungle have normal behaviour
                var commsSystem = __instance.Systems[SystemTypes.Comms].Cast<HudOverrideSystemType>();
                if (commsSystem.IsActive)
                {
                    __instance.RpcUpdateSystem(SystemTypes.Comms, 16); //Repair communications
                }
                else
                {
                    __instance.RpcUpdateSystem(SystemTypes.Comms, 128); //Sabotage communications
                }
            }

            CheatSettings.commsSab = false; //Button behaviour

        }
        else if (CheatSettings.elecSab)
        { //Eletrical sabotage

            if (currentMapID != 5)
            {
                var elecSystem = __instance.Systems[SystemTypes.Electrical].Cast<SwitchSystem>();
                if (elecSystem.ActualSwitches != elecSystem.ExpectedSwitches)
                {

                    for (var i = 0; i < 5; i++)
                    {
                        var switchMask = 1 << (i & 0x1F);

                        if ((elecSystem.ActualSwitches & switchMask) != (elecSystem.ExpectedSwitches & switchMask))
                        {
                            __instance.RpcUpdateSystem(SystemTypes.Electrical, (byte)i); //Repair electrical
                        }
                    }

                }
                else
                {

                    byte b = 4;
                    for (int i = 0; i < 5; i++)
                    {
                        if (BoolRange.Next(0.5f))
                        {
                            b |= (byte)(1 << i);
                        }
                    }

                    __instance.RpcUpdateSystem(SystemTypes.Electrical, (byte)(b | 128)); //Sabotage electrical

                }
            }
            else
            {
                HudManager.Instance.Notifier.AddItem("Elec not present on Fungle");
            }

            CheatSettings.elecSab = false; //Button behaviour
        }
        else if (CheatSettings.MushRoomMixUp)
        {
            if (currentMapID == 5)
            {
                var mushroomSystem = __instance.Systems[SystemTypes.MushroomMixupSabotage].Cast<MushroomMixupSabotageSystem>();
                if (mushroomSystem.IsActive)
                {
                    HudManager.Instance.Notifier.AddItem("Mushroom sabotage Already active!");
                }
                else
                {
                    __instance.RpcUpdateSystem(SystemTypes.MushroomMixupSabotage, 1);
                }
            }
            else
            {
                HudManager.Instance.Notifier.AddItem("Mushroom sabotage not present on this map");
            }

            CheatSettings.MushRoomMixUp = false; //Button behaviour
        }
    }
}

[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.FixedUpdate))]
public static class Ship_CloseAllDoorsPostfix
{

    //Postfix patch of ShipStatus.FixedUpdate to lock all doors on a ship
    public static void Postfix(ShipStatus __instance)
    {
        if (CheatSettings.fullLockdown)
        {

            //Loop through all rooms and close their doors
            foreach (SystemTypes room in (SystemTypes[])Enum.GetValues(typeof(SystemTypes)))
            {
                try { __instance.RpcCloseDoorsOfType(room); } catch { } //try-catch for rooms with no doors
            }

            CheatSettings.fullLockdown = false;

        }
    }
}

[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.FixedUpdate))]
public static class Ship_BlackoutPostfix
{

    //Postfix patch of ShipStatus.FixedUpdate to disable lights completly
    public static void Postfix(ShipStatus __instance)
    {
        if (CheatSettings.blackOut)
        {
            //Apparently most values you put for amount in RpcUpdateSystem will break lights completly
            //They are unfixable through regular means (toggling switches)
            //They can only be repaired by repeating RpcUpdateSystem with the same amount
            //wow thats cool

            __instance.RpcUpdateSystem(SystemTypes.Electrical, 69);
            CheatSettings.blackOut = false;
        }
    }
}