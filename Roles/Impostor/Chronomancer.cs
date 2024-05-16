using Hazel;
using System;
using System.Text;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Utils;
using static TOHE.Translator;

namespace TOHE.Roles.Impostor;

internal class Chronomancer : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 900;
    public override bool IsEnable => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorKilling;
    //==================================================================\\

    private int ChargedTime;
    long now = Utils.GetTimeStamp();
    private int FullCharge;
    private bool IsInMassacre;

    float LastNowF = 0;
    float countnowF = 0;

    private static Color32 OrangeColor = new (143, 126, 96, 255); // The lest color
    private static Color32 GreenColor = new (0, 128, 0, 255); // The final color

    static int Charges;

    private static OptionItem KillCooldown;
    private static OptionItem MassacreTime;

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Chronomancer);
        KillCooldown = IntegerOptionItem.Create(Id + 10, "ChronomancerKillCooldown", new(1, 180, 1), 30, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Chronomancer])
            .SetValueFormat(OptionFormat.Seconds);
        MassacreTime = IntegerOptionItem.Create(Id + 10, "ChronomancerKillCooldown", new(1, 15, 1), 10, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Chronomancer])
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void Init()
    {
        //Init bruv
    }
    public override void Add(byte playerId)
    {
        FullCharge = KillCooldown.GetInt();
        Charges = (int)Math.Round(KillCooldown.GetInt() / 10.0);
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
        var sb = new StringBuilder(Utils.ColorString(percentcolor, $"<br>{ChargedTime}% "));
        var ChargeToColor = GetChargeToColor();

        sb.Append("<mspace=2.75em>");
        for (int i = 0; i < Charges; i++)
        {
            string box = ChargeToColor > 0 ? $"<Color=#0cb339>?</color>" : "<color=#666666>?</color>";
            sb.Append(box);
        }
        sb.Append("</mspace");

        return sb.ToString();
    }
    public void SetCooldown()
    {
        if (IsInMassacre)
        {

        }

    }
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (ChargedTime >= FullCharge)
        {
            killer.Notify(GetString("ChronomancerStartMassacre"));
            IsInMassacre = true;
        }
        return true;
    }
    public override void OnFixedUpdate(PlayerControl pc)
    {
        if (ChargedTime != FullCharge 
            && now + 1 <= Utils.GetTimeStamp() && !IsInMassacre)
        {
            ChargedTime++;
        }
        else if(IsInMassacre && ChargedTime > 0 && countnowF <= LastNowF)
        {
            LastNowF = countnowF + 0.2f;
            ChargedTime--;
        }

        if(IsInMassacre && ChargedTime < 1)
        {
            IsInMassacre = false;
        }

        countnowF += Time.deltaTime;
    }
    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        return GetCharge();
    }
}