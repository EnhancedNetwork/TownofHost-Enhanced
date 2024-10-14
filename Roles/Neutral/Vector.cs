﻿using static TOHE.Options;
using static TOHE.Utils;
using static TOHE.Translator;
using UnityEngine;
using AmongUs.GameOptions;
using Hazel;
using InnerNet;

namespace TOHE.Roles.Neutral;

internal class Vector : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 15500;
    private static readonly HashSet<byte> PlayerIds = [];
    public static bool HasEnabled => PlayerIds.Any();
    
    public override CustomRoles ThisRoleBase => CustomRoles.Engineer;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralChaos;
    //==================================================================\\

    private static OptionItem VectorVentNumWin;
    private static OptionItem VectorVentCD;

    private static readonly Dictionary<byte, int> VectorVentCount = [];

    public override void SetupCustomOption()
    {
        SetupRoleOptions(15500, TabGroup.NeutralRoles, CustomRoles.Vector);
        VectorVentNumWin = IntegerOptionItem.Create(15502, "VectorVentNumWin", new(5, 500, 5), 40, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Vector])
            .SetValueFormat(OptionFormat.Times);
        VectorVentCD = FloatOptionItem.Create(15503, GeneralOption.EngineerBase_VentCooldown, new(0f, 180f, 1f), 15f, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Vector])
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void Init()
    {
        VectorVentCount.Clear();
        PlayerIds.Clear();
    }
    public override void Add(byte playerId)
    {
        VectorVentCount[playerId] = 0;
        PlayerIds.Add(playerId);
    }
    private void SendRPC()
    {
        if (_Player.IsNonHostModdedClient()) return;
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, _Player.GetClientId());
        writer.WriteNetObject(_Player);
        writer.WritePacked(VectorVentCount[_Player.PlayerId]); 
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public override void ReceiveRPC(MessageReader reader, PlayerControl pc)
    {
        int count = reader.ReadPackedInt32();
        VectorVentCount[_Player.PlayerId] = count;
    }
    public override string GetProgressText(byte playerId, bool comms)
    {
        return ColorString(GetRoleColor(CustomRoles.Vector).ShadeColor(0.25f), $"({(VectorVentCount.TryGetValue(playerId, out var count) ? count : 0)}/{VectorVentNumWin.GetInt()})");
    }
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.EngineerCooldown = VectorVentCD.GetFloat();
        AURoleOptions.EngineerInVentMaxTime = 1;
    }
    public override void OnEnterVent(PlayerControl pc, Vent vent)
    {
        VectorVentCount[pc.PlayerId]++;
        SendRPC();
        NotifyRoles(SpecifySeer: pc);
        
        Logger.Info($"Vent count {VectorVentCount[pc.PlayerId]}", "Vector");
        
        if (VectorVentCount[pc.PlayerId] >= VectorVentNumWin.GetInt())
        {
            if (!CustomWinnerHolder.CheckForConvertedWinner(pc.PlayerId))
            {
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Vector);
                CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
            }
        }
    }
    //public override void OnFixedUpdateLowLoad(PlayerControl player)
    //{
    //    if (VectorVentCount[player.PlayerId] >= VectorVentNumWin.GetInt())
    //    {
    //        VectorVentCount[player.PlayerId] = VectorVentNumWin.GetInt();
    //        if (!CustomWinnerHolder.CheckForConvertedWinner(player.PlayerId))
    //        {
    //            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Vector);
    //            CustomWinnerHolder.WinnerIds.Add(player.PlayerId);
    //        }
    //    }
    //}
    public override Sprite GetAbilityButtonSprite(PlayerControl player, bool shapeshifting) => CustomButton.Get("Happy");
    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.AbilityButton.OverrideText(GetString("VectorVentButtonText"));
        hud.AbilityButton.SetUsesRemaining(VectorVentNumWin.GetInt() - (VectorVentCount.TryGetValue(PlayerControl.LocalPlayer.PlayerId, out var mx) ? mx : 0));
    }
}
