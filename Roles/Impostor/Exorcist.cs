using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.Arm;
using System.Text;
using System.Threading.Tasks;
using TOHE.Roles.Core;
using UnityEngine;
using static TOHE.Translator;

namespace TOHE.Roles.Impostor;

internal class Exorcist : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 30200;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Exorcist);
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorKilling;
    //==================================================================\\
    private static OptionItem ExcorismActiveFor;
    private static OptionItem ExcorismPerGame;
    private static OptionItem ExcorismDelay;
    private static OptionItem ExcorismSacrificesToDispell;
    private static OptionItem ExcorismLimitMeeting;
    private static OptionItem ExcorismEndOnKill;
    private static OptionItem TryHideMsg;
  

    private int ExcorismLimitPerMeeting;
    private static bool IsExcorism;
    private static bool IsDelay;
    private static PlayerControl ExorcistPlayer;
    private int Sacrifices = 0;
    private bool Dispelled = false;

    public override void SetupCustomOption()
    {
        Options.SetupSingleRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Exorcist, 1, zeroOne: false);
        ExcorismActiveFor = FloatOptionItem.Create(Id + 2, "ExcorismActiveFor", new(1f, 10f, 1f), 3f, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Exorcist])
            .SetValueFormat(OptionFormat.Seconds);
        ExcorismPerGame = IntegerOptionItem.Create(Id + 3, "ExcorismPerGame", new(1, 10, 1), 3, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Exorcist]);
        ExcorismDelay = FloatOptionItem.Create(Id + 4, "ExcorismDelay", new(0f, 10f, 1f), 3f, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Exorcist])
            .SetValueFormat(OptionFormat.Seconds);
        ExcorismSacrificesToDispell = IntegerOptionItem.Create(Id + 5, "ExcorismSacrificesToDispell", new(1, 10, 1), 2, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Exorcist]);
        ExcorismLimitMeeting = IntegerOptionItem.Create(Id + 6, "ExcorismLimitMeeting", new(1, 5, 1), 1, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Exorcist]);
        ExcorismEndOnKill = BooleanOptionItem.Create(Id + 7, "ExcorismEndOnKill", true, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Exorcist]);
        TryHideMsg = BooleanOptionItem.Create(Id + 8, "ExorcistTryHideMsg", true, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Exorcist])
            .SetColor(Color.green);

    }
    public override void Add(byte playerId)
    {
        ExcorismLimitPerMeeting = ExcorismLimitMeeting.GetInt();
        AbilityLimit = ExcorismPerGame.GetInt();
    }
    public override void AfterMeetingTasks()
    {
        ExcorismLimitPerMeeting = ExcorismLimitMeeting.GetInt();
    }
    public bool CheckCommond(ref string msg, string command, PlayerControl player)
    {

        var comList = command.Split('|');
        for (int i = 0; i < comList.Length; i++)
        {
            if (msg == "/" + comList[i])
            {
                if (!GameStates.IsMeeting) return false;
                if (player.Data.IsDead) return false;
                if (AbilityLimit <= 0 || ExcorismLimitPerMeeting <= 0)
                {
                    if (TryHideMsg.GetBool() && !player.Data.IsHost())
                        GuessManager.TryHideMsg();
                    Utils.SendMessage(Translator.GetString("ExorcistOutOfUsages"), player.PlayerId);
                    return true;
                }
                if (Dispelled)
                {
                    if (TryHideMsg.GetBool() && !player.Data.IsHost())
                        GuessManager.TryHideMsg();
                    Utils.SendMessage(Translator.GetString("ExorcistDispelled"), player.PlayerId);
                    return true;
                }
                if (IsExcorism || IsDelay)
                {
                    if (TryHideMsg.GetBool() && !player.Data.IsHost())
                        GuessManager.TryHideMsg();
                    Utils.SendMessage(Translator.GetString("ExorcistActive"), player.PlayerId);
                    return true;
                }
                ActivateExorcism(player);
                return true;
            }
        }
        return false;
    }
    public static bool IsExorcismActive()
    {
        return IsExcorism;
    }
    public static void ExcersizePlayer(PlayerControl player)
    {
        if (ExcorismEndOnKill.GetBool() && IsExcorism && ExorcistPlayer == player)
        {
            IsExcorism = false;
            Utils.SendMessage(Translator.GetString("ExorcistEnd"));
        }
        player.SetDeathReason(PlayerState.DeathReason.Excersized);
        player.SetRealKiller(ExorcistPlayer);
        GuessManager.RpcGuesserMurderPlayer(player);
        Main.PlayersDiedInMeeting.Add(player.PlayerId);
        MurderPlayerPatch.AfterPlayerDeathTasks(player, PlayerControl.LocalPlayer, true);
        Utils.SendMessage(string.Format(Translator.GetString("ExorcistKill"),player.name.RemoveHtmlTags()));
        Exorcist exorcist = (Exorcist)ExorcistPlayer.GetRoleClass();
        exorcist.Sacrifice();
    }
    public void ActivateExorcism(PlayerControl player)
    {
        ExcorismLimitPerMeeting--;
        AbilityLimit--;
        if(TryHideMsg.GetBool())
            GuessManager.TryHideMsg();
        ExorcistPlayer = player;
        IsDelay = true;
        if (ExcorismDelay.GetFloat() > 0)
             Utils.SendMessage(string.Format(Translator.GetString("ExorcistNotify"), ExcorismDelay.GetFloat()));
        _ = new LateTask(() =>
        {
            IsExcorism = true;
            IsDelay = false;
            Utils.SendMessage(string.Format(Translator.GetString("ExorcistStart"), ExcorismActiveFor.GetFloat()));
            _ = new LateTask(() =>
            {
                if (IsExcorism)
                {
                    IsExcorism = false;
                    Utils.SendMessage(Translator.GetString("ExorcistEnd"));
                }   
            }, ExcorismActiveFor.GetFloat(), "ExorcistNotify");
            
        }, ExcorismDelay.GetFloat(), "ExorcistNotify");
       

    }
    public void Sacrifice()
    {
        Sacrifices++;
        if (Sacrifices >= ExcorismSacrificesToDispell.CurrentValue)
            Dispelled = true;
    }
    public override string GetProgressText(byte playerId, bool coooms) => Utils.ColorString(AbilityLimit <= 0 ? Color.gray : Utils.GetRoleColor(CustomRoles.Exorcist), $"({AbilityLimit})") ?? "Invalid";
}
