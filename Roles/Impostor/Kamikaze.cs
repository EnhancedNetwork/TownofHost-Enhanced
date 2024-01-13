using Hazel;
using System.Collections.Generic;
using TOHE.Roles.Double;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Impostor;

public static class Kamikaze
{
    private static readonly int Id = 26900;
    public static bool IsEnable = false;


    public static Dictionary<byte, byte> KamikazedList = new();
    private static OptionItem KillCooldown;
    private static OptionItem OptMaxMarked;
    public static Dictionary<byte, int> MarkedLim = new();

    //public static bool CheckKamiDeath = false;



    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Kamikaze);
        KillCooldown = FloatOptionItem.Create(Id + 10, "KillCooldown", new(0f, 180f, 2.5f), 25f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Kamikaze])
            .SetValueFormat(OptionFormat.Seconds);
        OptMaxMarked = IntegerOptionItem.Create(Id + 11, "KamikazeMaxMarked", new(1, 14, 1), 14, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Kamikaze])
           .SetValueFormat(OptionFormat.Times);

    }
    public static void Init()
    {
        MarkedLim = new();
        KamikazedList = new();
        IsEnable = false;
    }
    public static void Add(byte playerId)
    {
        IsEnable = true;
        MarkedLim.Add(playerId, OptMaxMarked.GetInt());

        // Double Trigger
        var pc = Utils.GetPlayerById(playerId);
        pc.AddDoubleTrigger();
    }

    private static void SendRPC(byte KamiId, byte targetId, bool checkMurder = false)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncKami, SendOption.Reliable, -1);
        writer.Write(checkMurder);
        writer.Write(targetId);
        
        if (checkMurder) 
        { 
            writer.Write(KamiId);
            writer.Write(MarkedLim[KamiId]);
        }
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        var checkMurder = reader.ReadBoolean();
        var targetId = reader.ReadByte();
        if (checkMurder)
        {
            var KamiId = reader.ReadByte();
            int Limit = reader.ReadInt32();

            KamikazedList[targetId] = KamiId;
            MarkedLim[KamiId] = Limit;
        }
        else KamikazedList.Remove(targetId);
    }
    public static void MurderKamikazedPlayers(PlayerControl kamikameha)
    {
        if (!KamikazedList.ContainsKey(kamikameha.PlayerId)) return;


        if (!kamikameha.IsAlive())
        {
            KamikazedList.Remove(kamikameha.PlayerId);
            SendRPC(KamiId: KamikazedList[kamikameha.PlayerId], targetId:kamikameha.PlayerId, checkMurder:false); // to remove playerid
            return;
        }
        var kami = Utils.GetPlayerById(KamikazedList[kamikameha.PlayerId]);
        if (kami == null) return;
        if (!kami.IsAlive())
        {
            if (kamikameha.IsAlive())
            {
                Main.PlayerStates[kamikameha.PlayerId].deathReason = PlayerState.DeathReason.Targeted;
                kamikameha.RpcMurderPlayerV3(kamikameha);
                // Logger.Info($"{alivePlayer.GetNameWithRole()} is the killer of {kamikameha.GetNameWithRole()}", "Kamikaze"); -- Works fine
            }

        }
    }
    public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
    public static bool OnCheckMurder(PlayerControl killer, PlayerControl target)
    {
        if (target.Is(CustomRoles.NiceMini) && Mini.Age < 18)
        {
            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Kamikaze), GetString("KamikazeHostage"))); 
            return false;
        }

        return killer.CheckDoubleTrigger(target, () =>
        {

            if (MarkedLim[killer.PlayerId] > 0) 
            { 
                KamikazedList[target.PlayerId] = killer.PlayerId;
                killer.SetKillCooldown(KillCooldown.GetFloat());
                Utils.NotifyRoles(SpecifySeer: killer);
                MarkedLim[killer.PlayerId]--;
                SendRPC(KamiId: killer.PlayerId, targetId: target.PlayerId, checkMurder: true) ;
            } 
            else
            {
                killer.RpcMurderPlayerV3(target);
            }

        });
        
    }

    public static bool CanMark(byte id) => MarkedLim.TryGetValue(id, out var x) && x > 0;
    public static string GetMarkedLimit(byte playerId) => Utils.ColorString(CanMark(playerId) ? Utils.GetRoleColor(CustomRoles.Kamikaze).ShadeColor(0.25f) : Color.gray, MarkedLim.TryGetValue(playerId, out var MarkedLimiter) ? $"({MarkedLimiter})" : "Invalid");


}

