using Hazel;
using System;
using System.Text;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Utils;
using static TOHE.Translator;
using AmongUs.GameOptions;

namespace TOHE.Roles.Impostor;

internal class Chronomancer : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 900;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorKilling;
    //==================================================================\\

    private int ChargedTime = 0;
    long now = Utils.GetTimeStamp();
    private int FullCharge = 0;
    private bool IsInMassacre;

    public float realcooldown; 

    float LastNowF = 0;
    float countnowF = 0;

    private static Color32 OrangeColor = new (143, 126, 96, 255); // The lest color
    private static Color32 GreenColor = new (0, 128, 0, 255); // The final color

    static int Charges;

    private static OptionItem KillCooldown;
    private static OptionItem Dtime;
    private static OptionItem ReduceVision;

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Chronomancer);
        KillCooldown = IntegerOptionItem.Create(Id + 10, "ChronomancerKillCooldown", new(1, 180, 1), 60, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Chronomancer])
            .SetValueFormat(OptionFormat.Seconds);
        Dtime = FloatOptionItem.Create(Id + 11, "ChronomancerDecreaseTime", new(0.05f, 1f, 0.05f), 0.15f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Chronomancer])
            .SetValueFormat(OptionFormat.Seconds);
        ReduceVision = FloatOptionItem.Create(Id + 12, "ChronomancerVisionMassacre", new(0.25f, 1f, 0.25f), 0.75f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Chronomancer])
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void Add(byte playerId)
    {
        FullCharge = KillCooldown.GetInt();
        Charges = (int)Math.Round(KillCooldown.GetInt() / 10.0);
        realcooldown = DefaultKillCooldown;
    }
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        if (IsInMassacre)
        {
            opt.SetVision(false);
            opt.SetFloat(FloatOptionNames.ImpostorLightMod, 0.3f);
        }
        else
        {
            opt.SetVision(true);
            opt.SetFloat(FloatOptionNames.ImpostorLightMod, Main.DefaultImpostorVision);
        }
    }
    private Color32 GetPercentColor(int val)
    {
        float chargeRatio = Mathf.Clamp01((float)val / FullCharge);
        return Color32.Lerp(OrangeColor, GreenColor, chargeRatio);
    
    }
    private int GetChargeToColor()
    { 
        int percent = (int)Math.Round(((double)ChargedTime / FullCharge) * 100);
        int ToColor = (Charges * percent) / 100;

        return ToColor;
    }
    private string GetCharge()
    {
        Color32 percentcolor = GetPercentColor(ChargedTime);
        var sb = new StringBuilder(Utils.ColorString(percentcolor, $"<br>{(int)Math.Round(((double)ChargedTime / FullCharge) * 100)}% "));
        var ChargeToColor = GetChargeToColor();

        sb.Append($"<size=75%>");
        for (int i = 0; i < Charges; i++)
        {
            string box = ChargeToColor > 0 ? $"<color=#0cb339>█ </color>" : "<color=#666666>█ </color>";
            ChargeToColor--;
            sb.Append(box);
        }
        sb.Append($"</size>");

        return sb.ToString();
    }
    public void SetCooldown()
    {
        if (IsInMassacre)
        {
            Main.AllPlayerKillCooldown[_state.PlayerId] = 0.1f;
        }
        else
        {
            Main.AllPlayerKillCooldown[_state.PlayerId] = realcooldown;
        }
        _Player.SyncSettings();
    }
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (ChargedTime >= FullCharge)
        {
            LastNowF = countnowF + Dtime.GetFloat();
            killer.Notify(GetString("ChronomancerStartMassacre"));
            IsInMassacre = true;
        }
        killer.SetKillCooldown();
        SetCooldown();
        return true;
    }
    public override void OnFixedUpdate(PlayerControl pc)
    {
        if (ChargedTime != FullCharge 
            && now + 1 <= Utils.GetTimeStamp() && !IsInMassacre)
        {
            now = Utils.GetTimeStamp();
            ChargedTime++;
        }
        else if(IsInMassacre && ChargedTime > 0 && countnowF >= LastNowF)
        {
            LastNowF = countnowF + Dtime.GetFloat();
            ChargedTime--;
        }

        if(IsInMassacre && ChargedTime < 1)
        {
            IsInMassacre = false;
            pc.MarkDirtySettings();
        }

        countnowF += Time.deltaTime;
    }
    public override string GetSuffix(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
    {

        if(seer == seen) return GetCharge();

        return "";
    }
}