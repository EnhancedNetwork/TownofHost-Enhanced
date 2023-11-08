using Hazel;
using System.Collections.Generic;
using TOHE.Roles.Impostor;
using UnityEngine;
using static TOHE.Options;

namespace TOHE.Roles.Neutral;
public static class Doppelganger
{
    private static readonly int Id = 25000;
    public static List<byte> playerIdList = new();
    public static bool IsEnable = false;

    private static OptionItem KillCooldown;
    public static OptionItem MaxSteals;

    public static Dictionary<byte, string> DoppelVictim = new();
    public static Dictionary<byte, GameData.PlayerOutfit> DoppelPresentSkin = new();
    public static Dictionary<byte, int> TotalSteals = new();


    public static void SetupCustomOption()
    {
        SetupSingleRoleOptions(Id, TabGroup.OtherRoles, CustomRoles.Doppelganger, 1, zeroOne: false);
        MaxSteals = IntegerOptionItem.Create(Id + 10, "DoppelMaxSteals", new(1, 14, 1), 9, TabGroup.OtherRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Doppelganger]);
        KillCooldown = FloatOptionItem.Create(Id + 11, "KillCooldown", new(0f, 180f, 2.5f), 20f, TabGroup.OtherRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Doppelganger])
            .SetValueFormat(OptionFormat.Seconds);
    }

    public static void Init()
    {
        playerIdList = new();
        DoppelVictim = new();
        TotalSteals = new();
        DoppelPresentSkin = new();
        IsEnable = false;
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        IsEnable = true;
        TotalSteals.Add(playerId, 0);
        if (playerId == PlayerControl.LocalPlayer.PlayerId && Main.nickName.Length != 0) DoppelVictim[playerId] = Main.nickName;
        else DoppelVictim[playerId] = Utils.GetPlayerById(playerId).Data.PlayerName;

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }

    private static void SendRPC(byte playerId, bool isTargetList = false)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetDoppelgangerStealLimit, SendOption.Reliable, -1);
        writer.Write(playerId);
        writer.Write(TotalSteals[playerId]);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }

    public static void ReceiveRPC(MessageReader reader)
    {
        byte PlayerId = reader.ReadByte();
        int Limit = reader.ReadInt32();
        if (TotalSteals.ContainsKey(PlayerId))
            TotalSteals[PlayerId] = Limit;
        else
            TotalSteals.Add(PlayerId, 0);
    }

    public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();

    //overloading
    public static GameData.PlayerOutfit Set(this GameData.PlayerOutfit instance, string playerName, int colorId, string hatId, string skinId, string visorId, string petId, string nameplateId)
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

    public static void RpcChangeSkin(PlayerControl pc, GameData.PlayerOutfit newOutfit)
    {
        var sender = CustomRpcSender.Create(name: $"Doppelganger.RpcChangeSkin({pc.Data.PlayerName})");
        //if (pc.PlayerId == PlayerControl.LocalPlayer.PlayerId) Main.nickName = newOutfit.PlayerName;
        pc.SetName(newOutfit.PlayerName);
        sender.AutoStartRpc(pc.NetId, (byte)RpcCalls.SetName)
            .Write(newOutfit.PlayerName)
        .EndRpc();
        //pc.RpcSetName(newOutfit.PlayerName); 
        Main.AllPlayerNames[pc.PlayerId] = newOutfit.PlayerName;

        pc.SetColor(newOutfit.ColorId);
        sender.AutoStartRpc(pc.NetId, (byte)RpcCalls.SetColor)
            .Write(newOutfit.ColorId)
        .EndRpc();

        pc.SetHat(newOutfit.HatId, newOutfit.ColorId);
        sender.AutoStartRpc(pc.NetId, (byte)RpcCalls.SetHatStr)
            .Write(newOutfit.HatId)
        .EndRpc();

        pc.SetSkin(newOutfit.SkinId, newOutfit.ColorId);
        sender.AutoStartRpc(pc.NetId, (byte)RpcCalls.SetSkinStr)
            .Write(newOutfit.SkinId)
        .EndRpc();

        pc.SetVisor(newOutfit.VisorId, newOutfit.ColorId);
        sender.AutoStartRpc(pc.NetId, (byte)RpcCalls.SetVisorStr)
            .Write(newOutfit.VisorId)
        .EndRpc();

        pc.SetPet(newOutfit.PetId);
        sender.AutoStartRpc(pc.NetId, (byte)RpcCalls.SetPetStr)
            .Write(newOutfit.PetId)
            .EndRpc();

        pc.SetNamePlate(newOutfit.NamePlateId);
        sender.AutoStartRpc(pc.NetId, (byte)RpcCalls.SetNamePlateStr)
            .Write(newOutfit.NamePlateId)
            .EndRpc();

        sender.SendMessage();
        DoppelPresentSkin[pc.PlayerId] = newOutfit;
    }

    public static void OnCheckMurder(PlayerControl killer, PlayerControl target)
    {
        if (killer == null || target == null || !IsEnable || Camouflage.IsCamouflage || Camouflager.AbilityActivated) return;
        if (Main.CheckShapeshift.TryGetValue(target.PlayerId, out bool isShapeshifitng) && isShapeshifitng)
        {
            Logger.Info("Target was shapeshifting", "Doppelganger");
            return; 
        } 
        if (TotalSteals[killer.PlayerId] >= MaxSteals.GetInt())
        {
            TotalSteals[killer.PlayerId] = MaxSteals.GetInt();
            return;
        }

        TotalSteals[killer.PlayerId]++;

        string kname;
        if (killer.PlayerId == PlayerControl.LocalPlayer.PlayerId && Main.nickName.Length != 0) kname = Main.nickName;
        else kname = killer.Data.PlayerName;
        string tname;
        if (target.PlayerId == PlayerControl.LocalPlayer.PlayerId && Main.nickName.Length != 0) tname = Main.nickName;
        else tname = target.Data.PlayerName;

        var killerSkin = new GameData.PlayerOutfit()
            .Set(kname, killer.CurrentOutfit.ColorId, killer.CurrentOutfit.HatId, killer.CurrentOutfit.SkinId, killer.CurrentOutfit.VisorId, killer.CurrentOutfit.PetId, killer.CurrentOutfit.NamePlateId);

        var targetSkin = new GameData.PlayerOutfit()
            .Set(tname, target.CurrentOutfit.ColorId, target.CurrentOutfit.HatId, target.CurrentOutfit.SkinId, target.CurrentOutfit.VisorId, target.CurrentOutfit.PetId, target.CurrentOutfit.NamePlateId);

        DoppelVictim[target.PlayerId] = tname;
        

        RpcChangeSkin(target, killerSkin);
        Logger.Info("Changed target skin", "Doppelganger");
        RpcChangeSkin(killer, targetSkin);
        Logger.Info("Changed killer skin", "Doppelganger");

        SendRPC(killer.PlayerId);
        Utils.NotifyRoles();
        killer.ResetKillCooldown();
        killer.SetKillCooldown();
        return;
    }

    public static string GetStealLimit(byte playerId) => Utils.ColorString(TotalSteals[playerId] < MaxSteals.GetInt() ? Utils.GetRoleColor(CustomRoles.Doppelganger).ShadeColor(0.25f) : Color.gray, TotalSteals.TryGetValue(playerId, out var stealLimit) ? $"({MaxSteals.GetInt() - stealLimit})" : "Invalid");
}
