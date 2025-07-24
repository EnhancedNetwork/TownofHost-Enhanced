using AmongUs.GameOptions;
using System.Text;
using TOHE.Modules;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.Neutral;

internal class Vector : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Vector;
    private const int Id = 15500;
    public override CustomRoles ThisRoleBase => CustomRoles.Engineer;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralChaos;
    public override bool BlockMoveInVent(PlayerControl pc) => VectorInVentMaxTime.GetFloat() <= 1f;
    //==================================================================\\

    private static OptionItem VectorVentNumWin;
    private static OptionItem VectorVentCD;
    private static OptionItem VectorInVentMaxTime;

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Vector);
        VectorVentNumWin = IntegerOptionItem.Create(Id + 10, "VectorVentNumWin", new(5, 500, 5), 40, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Vector])
            .SetValueFormat(OptionFormat.Times);
        VectorVentCD = FloatOptionItem.Create(Id + 11, GeneralOption.EngineerBase_VentCooldown, new(0f, 180f, 1f), 15f, TabGroup.NeutralRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Vector])
            .SetValueFormat(OptionFormat.Seconds);
        VectorInVentMaxTime = FloatOptionItem.Create(Id + 12, GeneralOption.EngineerBase_InVentMaxTime, new(0f, 180f, 1f), 1f, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Vector])
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void Add(byte playerId)
    {
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
        AURoleOptions.EngineerInVentMaxTime = VectorInVentMaxTime.GetFloat();
    }
    public override void OnEnterVent(PlayerControl pc, Vent vent)
    {
        pc.RpcIncreaseAbilityUseLimitBy(1);
        NotifyRoles(SpecifySeer: pc, ForceLoop: false);
        pc.RPCPlayCustomSound("MarioJump");

        var count = pc.GetAbilityUseLimit();
        Logger.Info($"Vent count {count}", "Vector");

        if (count >= VectorVentNumWin.GetInt())
        {
            if (!CustomWinnerHolder.CheckForConvertedWinner(pc.PlayerId))
            {
                pc.RPCPlayCustomSound("MarioCoin");
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Vector);
                CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
            }
        }
    }
    public override Sprite GetAbilityButtonSprite(PlayerControl player, bool shapeshifting) => CustomButton.Get("Happy");
    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.AbilityButton.OverrideText(GetString("VectorVentButtonText"));
        hud.AbilityButton.SetUsesRemaining(VectorVentNumWin.GetInt() - (int)playerId.GetAbilityUseLimit());
    }
}
