﻿using System;
using System.Text;
using UnityEngine;
using static TOHE.Options;
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

    private float LastNowF = 0;
    private float countnowF = 0;

    private string LastCD;

    private static Color32 OrangeColor = new(255, 190, 92, 255); // The lest color
    private static Color32 GreenColor = new(0, 128, 0, 255); // The final color

    private static int Charges;

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
        ReduceVision = FloatOptionItem.Create(Id + 12, "ChronomancerVisionMassacre", new(0.25f, 1f, 0.25f), 0.5f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Chronomancer])
            .SetValueFormat(OptionFormat.Multiplier);
    }
    public override void Add(byte playerId)
    {
        FullCharge = KillCooldown.GetInt();
        Charges = (int)Math.Round(KillCooldown.GetInt() / 10.0);
        realcooldown = DefaultKillCooldown;

        LastCD = GetCharge();
    }
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        if (IsInMassacre)
        {
            opt.SetVision(false);
            opt.SetFloat(FloatOptionNames.ImpostorLightMod, ReduceVision.GetFloat());
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
        var sb = new StringBuilder(Utils.ColorString(percentcolor, $"{(int)Math.Round(((double)ChargedTime / FullCharge) * 100)}% "));
        var ChargeToColor = GetChargeToColor();
        var CHcol = IsInMassacre ? "#630303" : "#0cb339";

        sb.Append($"<size=75%>");
        for (int i = 0; i < Charges; i++)
        {
            string box = ChargeToColor > 0 ? $"<{CHcol}>█ </color>" : "<#666666>█ </color>";
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
    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        ChargedTime = 0;
        IsInMassacre = false;
        _Player?.MarkDirtySettings();
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
        return true;
    }
    public override void OnFixedUpdate(PlayerControl pc)
    {
        if (GameStates.IsMeeting) return;

        if (LastCD != GetCharge())
        {
            LastCD = GetCharge();
            Utils.NotifyRoles(SpecifySeer: pc, SpecifyTarget: pc);
        }

        if (ChargedTime != FullCharge
            && now + 1 <= Utils.GetTimeStamp() && !IsInMassacre)
        {
            now = Utils.GetTimeStamp();
            ChargedTime++;
        }
        else if (IsInMassacre && ChargedTime > 0 && countnowF >= LastNowF)
        {
            LastNowF = countnowF + Dtime.GetFloat();
            ChargedTime--;
        }

        if (IsInMassacre && ChargedTime < 1)
        {
            IsInMassacre = false;
            pc.MarkDirtySettings();
        }

        countnowF += Time.deltaTime;
    }
    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        bool ismeeting = GameStates.IsMeeting || isForMeeting;
        if (seer == seen && !ismeeting)
        {
            if (!isForHud && seer.IsModClient())
                return string.Empty;

            return GetCharge();
        }
        return "";
    }
}