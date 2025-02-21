using AmongUs.GameOptions;
using Hazel;
using InnerNet;
using System;
using System.Text;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Impostor;

internal class Chronomancer : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Chronomancer;
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
        Dtime = FloatOptionItem.Create(Id + 11, "ChronomancerDecreaseTime", new(0.10f, 1f, 0.05f), 0.15f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Chronomancer])
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
    public override void SetKillCooldown(byte id)
    {
        if (IsInMassacre)
        {
            Main.AllPlayerKillCooldown[id] = 0.1f;
        }
        else
        {
            Main.AllPlayerKillCooldown[id] = realcooldown;
        }
        _Player?.SyncSettings();
    }
    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        ChargedTime = 0;
        IsInMassacre = false;
        _Player?.ResetKillCooldown();
    }
    public override void AfterMeetingTasks()
    {
        ChargedTime = 0;
        IsInMassacre = false;
        _Player?.ResetKillCooldown();
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
    public override void OnFixedUpdate(PlayerControl player, bool lowLoad, long nowTime, int timerLowLoad)
    {
        if (!Main.IntroDestroyed) return;

        var oldChargedTime = ChargedTime;
        if (LastCD != GetCharge())
        {
            LastCD = GetCharge();
            Utils.NotifyRoles(SpecifySeer: player, ForceLoop: false);
        }

        if (ChargedTime != FullCharge
            && now + 1 <= nowTime && !IsInMassacre)
        {
            now = nowTime;
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
            player.MarkDirtySettings();
        }

        if (oldChargedTime != ChargedTime)
        {
            SendChargedTimeRPC();
        }

        countnowF += Time.deltaTime;
    }

    public void SendChargedTimeRPC()
    {
        // Cant directly write Ability Limit, create another method to send it
        // Only send to the target to prevent logging in other's
        var writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.None, _Player.OwnerId);
        writer.WriteNetObject(_Player);
        writer.Write(ChargedTime);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }

    public override void ReceiveRPC(MessageReader reader, PlayerControl pc)
    {
        ChargedTime = reader.ReadInt32();
    }

    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        bool ismeeting = GameStates.IsMeeting || isForMeeting;
        if (seer == seen && !ismeeting)
        {
            if (!isForHud && seer.IsModded())
                return string.Empty;

            return GetCharge();
        }
        return string.Empty;
    }
}
