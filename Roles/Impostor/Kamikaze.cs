using Hazel;
using TOHE.Roles.Double;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Impostor;

internal class Kamikaze : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 26900;

    private static readonly HashSet<byte> Playerids = [];
    public static bool HasEnabled => Playerids.Any();
    
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorSupport;
    //==================================================================\\

    private static OptionItem KillCooldown;
    private static OptionItem OptMaxMarked;

    private static readonly Dictionary<byte, HashSet<byte>> KamikazedList = [];

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Kamikaze);
        KillCooldown = FloatOptionItem.Create(Id + 10, "KillCooldown", new(0f, 180f, 2.5f), 25f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Kamikaze])
            .SetValueFormat(OptionFormat.Seconds);
        OptMaxMarked = IntegerOptionItem.Create(Id + 11, "KamikazeMaxMarked", new(1, 14, 1), 14, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Kamikaze])
           .SetValueFormat(OptionFormat.Times);

    }
    public override void Init()
    {
        KamikazedList.Clear();
    }
    public override void Add(byte playerId)
    {
        AbilityLimit = OptMaxMarked.GetInt();
        KamikazedList[playerId] = [];

        // Double Trigger
        var pc = Utils.GetPlayerById(playerId);
        pc.AddDoubleTrigger();

        Playerids.Add(playerId);
    }

    private void SendRPC(byte KamiId, byte targetId = byte.MaxValue, bool checkMurder = false)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncKami, SendOption.Reliable, -1);
        writer.Write(checkMurder);
        writer.Write(KamiId);

        if (checkMurder) 
        { 
            writer.Write(targetId);
            writer.Write(AbilityLimit);
        }
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        var checkMurder = reader.ReadBoolean();
        var kamiId = reader.ReadByte();
        if (checkMurder)
        {
            var targetId = reader.ReadByte();
            float Limit = reader.ReadSingle();
            if (!KamikazedList.ContainsKey(kamiId)) KamikazedList[kamiId] = [];
            KamikazedList[kamiId].Add(targetId);
            Main.PlayerStates[kamiId].RoleClass.AbilityLimit = Limit;
        }
        else KamikazedList.Remove(kamiId);
    }

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
    
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (target.Is(CustomRoles.NiceMini) && Mini.Age < 18)
        {
            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Kamikaze), GetString("KamikazeHostage"))); 
            return false;
        }

        return killer.CheckDoubleTrigger(target, () =>
        {

            if (AbilityLimit > 0) 
            {
                if (!KamikazedList.ContainsKey(killer.PlayerId)) KamikazedList[killer.PlayerId] = [];
                KamikazedList[killer.PlayerId].Add(target.PlayerId);
                killer.SetKillCooldown(KillCooldown.GetFloat());
                Utils.NotifyRoles(SpecifySeer: killer);
                AbilityLimit--;
                SendRPC(KamiId: killer.PlayerId, targetId: target.PlayerId, checkMurder: true);
            } 
            else
            {
                killer.RpcMurderPlayer(target);
            }
        });
        
    }
    public override void OnMurderPlayerAsTarget(PlayerControl killer, PlayerControl target, bool inMeeting, bool isSuicide)
    {
        if (!KamikazedList.ContainsKey(target.PlayerId) || !KamikazedList[target.PlayerId].Any()) return;
        foreach(var kamiTarget in KamikazedList[target.PlayerId])
        {
            PlayerControl pc = Utils.GetPlayerById(kamiTarget);
            if (pc == null || !pc.IsAlive()) continue;
            Main.PlayerStates[kamiTarget].deathReason = PlayerState.DeathReason.Targeted;
            pc.RpcMurderPlayer(pc);
            pc.SetRealKiller(target);
        }
        KamikazedList.Remove(target.PlayerId);
        SendRPC(KamiId: target.PlayerId, checkMurder: false);
    }

    //private void MurderKamikazedPlayers(PlayerControl kamikameha)
    //{
    //    if (!KamikazedList.ContainsKey(kamikameha.PlayerId)) return;

    //    if (!kamikameha.IsAlive())
    //    {
    //        KamikazedList.Remove(kamikameha.PlayerId);
    //        SendRPC(KamiId: byte.MaxValue, targetId: kamikameha.PlayerId, checkMurder: false); // to remove playerid
    //        return;
    //    }
    //    var kami = Utils.GetPlayerById(KamikazedList[kamikameha.PlayerId]);
    //    if (kami == null) return;
    //    if (!kami.IsAlive())
    //    {
    //        if (kamikameha.IsAlive())
    //        {
    //            Main.PlayerStates[kamikameha.PlayerId].deathReason = PlayerState.DeathReason.Targeted;
    //            kamikameha.SetRealKiller(kami);
    //            kamikameha.RpcMurderPlayer(kamikameha);
    //            // Logger.Info($"{alivePlayer.GetNameWithRole()} is the killer of {kamikameha.GetNameWithRole()}", "Kamikaze"); -- Works fine
    //        }

    //    }
    //}

    private bool CanMark(byte id) => AbilityLimit > 0;
    
    public override string GetProgressText(byte playerId, bool comms)
        => Utils.ColorString(CanMark(playerId)
            ? Utils.GetRoleColor(CustomRoles.Kamikaze).ShadeColor(0.25f) 
            : Color.gray, $"({AbilityLimit})");
}

