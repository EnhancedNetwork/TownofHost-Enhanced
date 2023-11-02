using System.Collections.Generic;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate;

public static class CopyCat
{
    private static readonly int Id = 31000;
    public static List<byte> playerIdList = new();
    public static bool IsEnable = false;

    public static Dictionary<byte, float> CurrentKillCooldown = new();
    public static Dictionary<byte, int> MiscopyLimit = new();

    public static OptionItem KillCooldown;
    public static OptionItem CopyCrewVar;
    public static OptionItem CanKill;
    public static OptionItem MiscopyLimitOpt;

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.CopyCat);
        KillCooldown = FloatOptionItem.Create(Id + 10, "CopyCatCopyCooldown", new(0f, 180f, 1f), 15f, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.CopyCat])
            .SetValueFormat(OptionFormat.Seconds);
        //    CanKill = BooleanOptionItem.Create(Id + 11, "CopyCatCanKill", false, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.CopyCat]);
        CopyCrewVar = BooleanOptionItem.Create(Id + 13, "CopyCrewVar", true, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.CopyCat]);
        /*  MiscopyLimitOpt = IntegerOptionItem.Create(Id + 12, "CopyCatMiscopyLimit", new(0, 14, 1), 2, TabGroup.CrewmateRoles, false).SetParent(CanKill)
              .SetValueFormat(OptionFormat.Times); */
    }

    public static void Init()
    {
        playerIdList = new();
        CurrentKillCooldown = new();
        MiscopyLimit = new();
        IsEnable = false;
    }

    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        CurrentKillCooldown.Add(playerId, KillCooldown.GetFloat());
        IsEnable = true;

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }


    /*  private static void SendRPC(byte playerId)
      {
          MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetCopyCatMiscopyLimit, SendOption.Reliable, -1);
          writer.Write(playerId);
          writer.Write(MiscopyLimit[playerId]);
          AmongUsClient.Instance.FinishRpcImmediately(writer);
      }
      public static void ReceiveRPC(MessageReader reader)
      {
          byte CopyCatId = reader.ReadByte();
          int Limit = reader.ReadInt32();
          if (MiscopyLimit.ContainsKey(CopyCatId))
              MiscopyLimit[CopyCatId] = Limit;
          else
              MiscopyLimit.Add(CopyCatId, MiscopyLimitOpt.GetInt());
      } */
    public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = Utils.GetPlayerById(id).IsAlive() ? CurrentKillCooldown[id] : 300f;

    public static void AfterMeetingTasks()
    {
        if (!IsEnable) return;

        foreach (var player in playerIdList)
        {
            var pc = Utils.GetPlayerById(player);
            if (pc == null) continue;
            var role = pc.GetCustomRole();
            ////////////           /*remove the settings for current role*/             /////////////////////
            switch (role)
            {
                //case CustomRoles.Addict:
                //    Addict.SuicideTimer.Remove(player);
                //    Addict.ImmortalTimer.Remove(player);
                //    break;
                //case CustomRoles.Bloodhound:
                //    Bloodhound.BloodhoundTargets.Remove(player);
                //    break;
                case CustomRoles.Cleanser:
                    Cleanser.CleanserTarget.Remove(pc.PlayerId);
                    Cleanser.CleanserUses.Remove(pc.PlayerId);
                    Cleanser.DidVote.Remove(pc.PlayerId);
                    break;
                case CustomRoles.Jailer:
                    Jailer.JailerExeLimit.Remove(pc.PlayerId);
                    Jailer.JailerTarget.Remove(pc.PlayerId);
                    Jailer.JailerHasExe.Remove(pc.PlayerId);
                    Jailer.JailerDidVote.Remove(pc.PlayerId);
                    break;
                case CustomRoles.ParityCop:
                    ParityCop.MaxCheckLimit.Remove(player);
                    ParityCop.RoundCheckLimit.Remove(player);
                    break;
                case CustomRoles.Medic:
                    Medic.ProtectLimit.Remove(player);
                    break;
                case CustomRoles.Mediumshiper:
                    Mediumshiper.ContactLimit.Remove(player);
                    break;
                case CustomRoles.Merchant:
                    Merchant.addonsSold.Remove(player);
                    Merchant.bribedKiller.Remove(player);
                    break;
                case CustomRoles.Oracle:
                    Oracle.CheckLimit.Remove(player);
                    break;
                //case CustomRoles.DovesOfNeace:
                //    Main.DovesOfNeaceNumOfUsed.Remove(player);
                //    break;
                case CustomRoles.Paranoia:
                    Main.ParaUsedButtonCount.Remove(player);
                    break;
                case CustomRoles.Snitch:
                    Snitch.IsExposed.Remove(player);
                    Snitch.IsComplete.Remove(player);
                    break;
                //case CustomRoles.Spiritualist:
                //    Spiritualist.LastGhostArrowShowTime.Remove(player);
                //    Spiritualist.ShowGhostArrowUntil.Remove(player);
                //    break;
                //case CustomRoles.Tracker:
                //    Tracker.TrackLimit.Remove(player);
                //    Tracker.TrackerTarget.Remove(player);
                //    break;
                case CustomRoles.Counterfeiter:
                    Counterfeiter.SeelLimit.Remove(player);
                    break;
                //case CustomRoles.SwordsMan:
                //    if (!AmongUsClient.Instance.AmHost) break;
                //    if (!Main.ResetCamPlayerList.Contains(player))
                //        Main.ResetCamPlayerList.Add(player);
                //    break;
                case CustomRoles.Sheriff:
                    Sheriff.CurrentKillCooldown.Remove(player);
                    Sheriff.ShotLimit.Remove(player);
                    break;
                case CustomRoles.Crusader:
                    Crusader.CurrentKillCooldown.Remove(player);
                    Crusader.CrusaderLimit.Remove(player);
                    break;
                case CustomRoles.Veteran:
                    Main.VeteranNumOfUsed.Remove(player);
                    break;
                case CustomRoles.Grenadier:
                    Main.GrenadierNumOfUsed.Remove(player);
                    break;
                case CustomRoles.Lighter:
                    Main.LighterNumOfUsed.Remove(player);
                    break;
                //case CustomRoles.TimeMaster:
                //    Main.TimeMasterNumOfUsed.Remove(player);
                //    break;
                case CustomRoles.Judge:
                    Judge.TrialLimit.Remove(player);
                    break;
                case CustomRoles.Mayor:
                    Main.MayorUsedButtonCount.Remove(player);
                    break;
                case CustomRoles.Divinator:
                    Divinator.CheckLimit.Remove(pc.PlayerId);
                    break;
                case CustomRoles.Reverie:
                    Reverie.NowCooldown.Remove(pc.PlayerId);
                    break;
                case CustomRoles.President:
                    President.CheckPresidentReveal.Remove(pc.PlayerId);
                    President.EndLimit.Remove(pc.PlayerId);
                    President.RevealLimit.Remove(pc.PlayerId);
                    break;
                case CustomRoles.Spy:
                    Spy.UseLimit.Remove(pc.PlayerId);
                    break;
                case CustomRoles.SabotageMaster:
                    SabotageMaster.UsedSkillCount.Remove(pc.PlayerId);
                    break;
                case CustomRoles.Admirer:
                    Admirer.AdmirerLimit.Remove(pc.PlayerId);
                    break;
            }
            pc.RpcSetCustomRole(CustomRoles.CopyCat);
            SetKillCooldown(player);
        }
    }

    public static bool BlacklList(this CustomRoles role)
    {
        return role is CustomRoles.CopyCat or
            //bcoz of vent cd
            CustomRoles.Grenadier or
            CustomRoles.Lighter or
            CustomRoles.DovesOfNeace or
            CustomRoles.Veteran or
            CustomRoles.Bastion or
            CustomRoles.Addict or
            CustomRoles.Chameleon or
            CustomRoles.Alchemist or
            CustomRoles.TimeMaster or
            //bcoz of arrows
            CustomRoles.Mortician or
            CustomRoles.Bloodhound or
            CustomRoles.Tracefinder or
            CustomRoles.Spiritualist or
            CustomRoles.Tracker or
            //bcoz of single role
            // Other
            CustomRoles.Investigator;
    }

    public static bool OnCheckMurder(PlayerControl pc, PlayerControl tpc)
    {
        CustomRoles role = tpc.GetCustomRole();
        if (role.BlacklList())
        {
            pc.Notify(GetString("CopyCatCanNotCopy"));
            SetKillCooldown(pc.PlayerId);
            return false;
        }
        if (CopyCrewVar.GetBool())
        {
            switch (role)
            {
                case CustomRoles.Eraser:
                    role = CustomRoles.Cleanser;
                    break;
                case CustomRoles.Mafia:
                    role = CustomRoles.Retributionist;
                    break;
                case CustomRoles.Visionary:
                    role = CustomRoles.Oracle;
                    break;
                case CustomRoles.Workaholic:
                    role = CustomRoles.Snitch;
                    break;
                case CustomRoles.Sunnyboy:
                    role = CustomRoles.Doctor;
                    break;
                case CustomRoles.Vindicator:
                case CustomRoles.Pickpocket:
                    role = CustomRoles.Mayor;
                    break;
                case CustomRoles.Councillor:
                    role = CustomRoles.Judge;
                    break;
                case CustomRoles.Sans:
                case CustomRoles.Juggernaut:
                    role = CustomRoles.Reverie;
                    break;
                case CustomRoles.EvilGuesser:
                case CustomRoles.Doomsayer:
                    role = CustomRoles.NiceGuesser;
                    break;
            }
            //if (role == CustomRoles.Eraser) role = CustomRoles.Cleanser;
            //if (role == CustomRoles.Mafia) role = CustomRoles.Retributionist;
            //if (role == CustomRoles.Visionary) role = CustomRoles.Oracle;
            //if (role == CustomRoles.Workaholic) role = CustomRoles.Snitch;
            //if (role == CustomRoles.Sunnyboy) role = CustomRoles.Doctor;
            //if (role == CustomRoles.Vindicator || role == CustomRoles.Pickpocket) role = CustomRoles.Mayor;
            //else if (role == CustomRoles.Councillor) role = CustomRoles.Judge;
            //else if (role == CustomRoles.Sans || role == CustomRoles.Juggernaut) role = CustomRoles.Reverie;
            //else if (role == CustomRoles.EvilGuesser || role == CustomRoles.Doomsayer) role = CustomRoles.NiceGuesser;
        }
        if (role.IsCrewmate()/* && (!tpc.GetCustomSubRoles().Any(x => x == CustomRoles.Rascal))*/)
        {
            ////////////           /*add the settings for new role*/            ////////////
            /* anything that is assigned in onGameStartedPatch.cs comes here */
            switch (role)
            {
                //case CustomRoles.Addict:
                //    Addict.SuicideTimer[pc.PlayerId] = -10f;
                //    Addict.ImmortalTimer[pc.PlayerId] = 420f;
                //    break;
                //case CustomRoles.Bloodhound:
                //    Bloodhound.BloodhoundTargets.Add(pc.PlayerId, new List<byte>());
                //    break;
                case CustomRoles.Cleanser:
                    Cleanser.CleanserTarget.Add(pc.PlayerId, byte.MaxValue);
                    Cleanser.CleanserUses.Add(pc.PlayerId, 0);
                    Cleanser.DidVote.Add(pc.PlayerId, false);
                    break;
                case CustomRoles.Jailer:
                    Jailer.JailerExeLimit.Add(pc.PlayerId, Jailer.MaxExecution.GetInt());
                    Jailer.JailerTarget.Add(pc.PlayerId, byte.MaxValue);
                    Jailer.JailerHasExe.Add(pc.PlayerId, false);
                    Jailer.JailerDidVote.Add(pc.PlayerId, false);

                    if (!AmongUsClient.Instance.AmHost) break;
                    if (!Main.ResetCamPlayerList.Contains(pc.PlayerId))
                        Main.ResetCamPlayerList.Add(pc.PlayerId);
                    break;
                case CustomRoles.Deputy:
                    Deputy.SetKillCooldown(pc.PlayerId);
                    break;
                case CustomRoles.Witness:
                    if (!AmongUsClient.Instance.AmHost) break;
                    if (!Main.ResetCamPlayerList.Contains(pc.PlayerId))
                        Main.ResetCamPlayerList.Add(pc.PlayerId);
                    break;
                case CustomRoles.ParityCop:
                    ParityCop.MaxCheckLimit.Add(pc.PlayerId, ParityCop.ParityCheckLimitMax.GetInt());
                    ParityCop.RoundCheckLimit.Add(pc.PlayerId, ParityCop.ParityCheckLimitPerMeeting.GetInt());
                    break;
                case CustomRoles.Medic:
                    Medic.ProtectLimit.TryAdd(pc.PlayerId, Medic.SkillLimit);
                    break;
                case CustomRoles.Mediumshiper:
                    Mediumshiper.ContactLimit.Add(pc.PlayerId, Mediumshiper.ContactLimitOpt.GetInt());
                    break;
                case CustomRoles.Merchant:
                    Merchant.addonsSold.Add(pc.PlayerId, 0);
                    Merchant.bribedKiller.Add(pc.PlayerId, new List<byte>());
                    break;
                case CustomRoles.Oracle:
                    Oracle.CheckLimit.TryAdd(pc.PlayerId, Oracle.CheckLimitOpt.GetInt());
                    break;
                //case CustomRoles.DovesOfNeace:
                //    Main.DovesOfNeaceNumOfUsed.Add(pc.PlayerId, Options.DovesOfNeaceMaxOfUseage.GetInt());
                //    break;
                case CustomRoles.Paranoia:
                    Main.ParaUsedButtonCount[pc.PlayerId] = 0;
                    break;
                case CustomRoles.Snitch:
                    Snitch.IsExposed[pc.PlayerId] = false;
                    Snitch.IsComplete[pc.PlayerId] = false;
                    break;
                //case CustomRoles.Spiritualist:
                //    Spiritualist.LastGhostArrowShowTime.Add(pc.PlayerId, 0);
                //    Spiritualist.ShowGhostArrowUntil.Add(pc.PlayerId, 0);
                //    break;
                //case CustomRoles.Tracker:
                //    Tracker.TrackLimit.TryAdd(pc.PlayerId, Tracker.TrackLimitOpt.GetInt());
                //    Tracker.TrackerTarget.Add(pc.PlayerId, byte.MaxValue);
                //    break;
                case CustomRoles.Counterfeiter:
                    Counterfeiter.SeelLimit.Add(pc.PlayerId, Counterfeiter.CounterfeiterSkillLimitTimes.GetInt());
                    if (!AmongUsClient.Instance.AmHost) break;
                    if (!Main.ResetCamPlayerList.Contains(pc.PlayerId))
                        Main.ResetCamPlayerList.Add(pc.PlayerId);
                    break;
                case CustomRoles.SwordsMan:
                    if (!AmongUsClient.Instance.AmHost) break;
                    if (!Main.ResetCamPlayerList.Contains(pc.PlayerId))
                        Main.ResetCamPlayerList.Add(pc.PlayerId);
                    break;
                case CustomRoles.Sheriff:
                    Sheriff.CurrentKillCooldown.Add(pc.PlayerId, KillCooldown.GetFloat());
                    Sheriff.ShotLimit.TryAdd(pc.PlayerId, Sheriff.ShotLimitOpt.GetInt());
                    Logger.Info($"{Utils.GetPlayerById(pc.PlayerId)?.GetNameWithRole()} : 残り{Sheriff.ShotLimit[pc.PlayerId]}発", "Sheriff");

                    if (!AmongUsClient.Instance.AmHost) break;
                    if (!Main.ResetCamPlayerList.Contains(pc.PlayerId))
                        Main.ResetCamPlayerList.Add(pc.PlayerId);
                    break;
                case CustomRoles.Crusader:
                    Crusader.CurrentKillCooldown.Add(pc.PlayerId, KillCooldown.GetFloat());
                    Crusader.CrusaderLimit.TryAdd(pc.PlayerId, Sheriff.ShotLimitOpt.GetInt());
                    Logger.Info($"{Utils.GetPlayerById(pc.PlayerId)?.GetNameWithRole()} : 残り{Crusader.CrusaderLimit[pc.PlayerId]}発", "Crusader");

                    if (!AmongUsClient.Instance.AmHost) break;
                    if (!Main.ResetCamPlayerList.Contains(pc.PlayerId))
                        Main.ResetCamPlayerList.Add(pc.PlayerId);
                    break;
                //case CustomRoles.Veteran:
                //    Main.VeteranNumOfUsed.Add(pc.PlayerId, Options.VeteranSkillMaxOfUseage.GetInt());
                //    break;
                case CustomRoles.Judge:
                    Judge.TrialLimit.Add(pc.PlayerId, Judge.TrialLimitPerMeeting.GetInt());
                    break;
                case CustomRoles.Mayor:
                    Main.MayorUsedButtonCount[pc.PlayerId] = 0;
                    break;
                case CustomRoles.Divinator:
                    Divinator.CheckLimit.TryAdd(pc.PlayerId, 5);
                    break;
                case CustomRoles.Reverie:
                    Reverie.NowCooldown.TryAdd(pc.PlayerId, Reverie.DefaultKillCooldown.GetFloat());
                    break;
                case CustomRoles.President:
                    President.CheckPresidentReveal.Add(pc.PlayerId, false);
                    President.EndLimit.Add(pc.PlayerId, President.PresidentAbilityUses.GetInt());
                    President.RevealLimit.Add(pc.PlayerId, 1);
                    break;
                case CustomRoles.Spy:
                    Spy.UseLimit.Add(pc.PlayerId, Spy.UseLimitOpt.GetInt());
                    break;
                case CustomRoles.SabotageMaster:
                    SabotageMaster.UsedSkillCount.Add(pc.PlayerId, 0);
                    break;
                case CustomRoles.Admirer:
                    Admirer.AdmirerLimit.Add(pc.PlayerId, Admirer.SkillLimit.GetInt());
                    break;
            }

            pc.RpcSetCustomRole(role);
            if (tpc.Is(CustomRoles.Madmate) || tpc.Is(CustomRoles.Rascal)) pc.RpcSetCustomRole(CustomRoles.Madmate);

            pc.RpcGuardAndKill(pc);
            pc.Notify(string.Format(GetString("CopyCatRoleChange"), Utils.GetRoleName(role)));
            return false;
        }
        //if (CanKill.GetBool())
        //{
        //    if (MiscopyLimit[pc.PlayerId] >= 1)
        //    {
        //        MiscopyLimit[pc.PlayerId]--;
        //        SetKillCooldown(pc.PlayerId);
        //        SendRPC(pc.PlayerId);
        //        return true;
        //    }
        //    Main.PlayerStates[pc.PlayerId].deathReason = PlayerState.DeathReason.Suicide;
        //    pc.RpcMurderPlayerV3(pc);
        //    return false;
        //}
        pc.Notify(GetString("CopyCatCanNotCopy"));
        SetKillCooldown(pc.PlayerId);
        return false;
    }


}
