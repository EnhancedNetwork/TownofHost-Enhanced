using UnityEngine;
using System.Text;
using AmongUs.GameOptions;
using static TOHE.Options;
using static TOHE.Utils;
using static TOHE.Translator;

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
        PlayerIds.Clear();
    }
    public override void Add(byte playerId)
    {
        PlayerIds.Add(playerId);
        playerId.SetAbilityUseLimit(0);
    }
    public override string GetProgressText(byte playerId, bool comms)
    {
        var ProgressText = new StringBuilder();
        var TextColor = GetRoleColor(CustomRoles.Vector).ShadeColor(0.25f);

        ProgressText.Append(GetTaskCount(playerId, comms));
        ProgressText.Append(ColorString(TextColor, ColorString(Color.white, " - ") + $"({playerId.GetAbilityUseLimit()}/{VectorVentNumWin.GetInt()})"));
        return ProgressText.ToString();
    }
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.EngineerCooldown = VectorVentCD.GetFloat();
        AURoleOptions.EngineerInVentMaxTime = 1;
    }
    public override void OnEnterVent(PlayerControl pc, Vent vent)
    {
        pc.RpcIncreaseAbilityUseLimitBy(1);
        NotifyRoles(SpecifySeer: pc, ForceLoop: false);

        var count = pc.GetAbilityUseLimit();
        Logger.Info($"Vent count {count}", "Vector");
        
        if (count >= VectorVentNumWin.GetInt())
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
        hud.AbilityButton.SetUsesRemaining(VectorVentNumWin.GetInt() - (int)playerId.GetAbilityUseLimit());
    }
}
