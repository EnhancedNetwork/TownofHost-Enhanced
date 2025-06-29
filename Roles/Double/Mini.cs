using Hazel;
using TOHE.Modules.Rpc;
using TOHE.Roles.Core;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.Double;

internal class Mini : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Mini;
    private const int Id = 7000;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.EvilMini) || CustomRoleManager.HasEnabled(CustomRoles.NiceMini);
    public override CustomRoles ThisRoleBase => IsEvilMini ? CustomRoles.Impostor : CustomRoles.Crewmate;
    public override Custom_RoleType ThisRoleType => IsEvilMini ? Custom_RoleType.ImpostorKilling : Custom_RoleType.CrewmateBasic;
    //==================================================================\\

    private static OptionItem GrowUpDuration;
    private static OptionItem EveryoneCanKnowMini;
    private static OptionItem CountMeetingTime;
    private static OptionItem EvilMiniSpawnChances;
    private static OptionItem CanBeEvil;
    public static OptionItem CanGuessEvil;
    private static OptionItem UpDateAge;
    private static OptionItem MinorCD;
    private static OptionItem MajorCD;


    public static int Age = new();
    private static bool IsEvilMini = false;
    private static int GrowUpTime = new();
    //private static int GrowUp = new();
    private static long LastFixedUpdate = new();
    private static bool misguessed = false;

    public override void SetupCustomOption()
    {
        Options.SetupSingleRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Mini, 1, zeroOne: false);
        GrowUpDuration = IntegerOptionItem.Create(Id + 100, "GrowUpDuration", new(200, 800, 25), 400, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Mini])
            .SetValueFormat(OptionFormat.Seconds);
        EveryoneCanKnowMini = BooleanOptionItem.Create(Id + 102, "EveryoneCanKnowMini", true, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Mini]);
        CanBeEvil = BooleanOptionItem.Create(Id + 106, "CanBeEvil", true, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Mini]);
        EvilMiniSpawnChances = IntegerOptionItem.Create(Id + 108, "EvilMiniSpawnChances", new(0, 100, 5), 50, TabGroup.CrewmateRoles, false).SetParent(CanBeEvil)
            .SetValueFormat(OptionFormat.Percent);
        CanGuessEvil = BooleanOptionItem.Create(Id + 104, "EvilMiniCanBeGuessed", true, TabGroup.CrewmateRoles, false).SetParent(CanBeEvil);
        MinorCD = FloatOptionItem.Create(Id + 110, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 45f, TabGroup.CrewmateRoles, false).SetParent(CanBeEvil)
            .SetValueFormat(OptionFormat.Seconds);
        MajorCD = FloatOptionItem.Create(Id + 112, "MajorCooldown", new(0f, 180f, 2.5f), 15f, TabGroup.CrewmateRoles, false).SetParent(CanBeEvil)
            .SetValueFormat(OptionFormat.Seconds);
        UpDateAge = BooleanOptionItem.Create(Id + 114, "UpDateAge", true, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Mini]);
        CountMeetingTime = BooleanOptionItem.Create(Id + 116, "CountMeetingTime", true, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Mini]);
    }
    public override void Init()
    {
        GrowUpTime = 0;
        //GrowUp = GrowUpDuration.GetInt() / 18;
        Age = 0;
        misguessed = false;

        IsEvilMini = false;
        if (AmongUsClient.Instance.AmHost && CustomRoles.Mini.IsEnable())
        {
            var rand = IRandom.Instance;
            IsEvilMini = CanBeEvil.GetBool() && (rand.Next(0, 100) < EvilMiniSpawnChances.GetInt());
        }
    }
    public override void Add(byte playerId)
    {
        if (AmongUsClient.Instance.AmHost)
        {
            SendRPC();
        }
    }
    private void SendRPC()
    {
        var writer = MessageWriter.Get(SendOption.Reliable);
        writer.Write(Age);
        writer.Write(IsEvilMini);
        RpcUtils.LateBroadcastReliableMessage(new RpcSyncRoleSkill(PlayerControl.LocalPlayer.NetId, _Player.NetId, writer));
    }
    public override void ReceiveRPC(MessageReader reader, PlayerControl NaN)
    {
        Age = reader.ReadInt32();
        IsEvilMini = reader.ReadBoolean();
    }

    public static bool CheckSpawnEvilMini() => IsEvilMini;

    public override bool OnCheckMurderAsTarget(PlayerControl killer, PlayerControl target)
    {
        if (Age < 18)
        {
            killer.Notify(ColorString(GetRoleColor(CustomRoles.Mini), GetString("Cantkillkid")));
            return false;
        }
        return true;
    }
    public void OnFixedUpdates(PlayerControl player, long nowTime)
    {
        if (Age >= 18) return;

        //Check if nice mini is dead
        if (player.Is(CustomRoles.NiceMini) && !player.IsAlive())
        {
            if (CustomWinnerHolder.WinnerTeam == CustomWinner.Default && !CustomWinnerHolder.CheckForConvertedWinner(player.PlayerId))
            {
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.NiceMini);
                CustomWinnerHolder.WinnerIds.Add(player.PlayerId);
            }
            // â†‘ This code will show the mini winning player on the checkout screen, Tommy you shouldn't comment it out!
        } //Is there any need to check this 30 times a second?

        if (GameStates.IsMeeting && !CountMeetingTime.GetBool()) return;

        if (LastFixedUpdate == nowTime) return;
        LastFixedUpdate = nowTime;
        GrowUpTime++;

        if (GrowUpTime >= GrowUpDuration.GetInt() / 18)
        {
            Age += 1;
            GrowUpTime = 0;
            Logger.Info("Mini grow up by 1", "Mini");
            if (player.Is(CustomRoles.EvilMini))
            {
                player.ResetKillCooldown();
                player.SyncSettings();
                //Only sync settings so evil mini's age updates upon next kill attempt or after meeting
            }

            if (player.Is(CustomRoles.NiceMini))
                player.RpcGuardAndKill();

            /*Dont show guard animation for evil mini,
            this would simply stop them from murdering.
            Imagine reseting kill cool down every 20 seconds
            and evil mini can never kill before age 18*/

            if (UpDateAge.GetBool())
            {
                SendRPC();
                player.Notify(GetString("MiniUp"));
                NotifyRoles(SpecifyTarget: player);
            }
        }
    }

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = GetKillCoolDown();
    private static float GetKillCoolDown()
    {
        if (MinorCD.GetFloat() <= MajorCD.GetFloat())
            return MinorCD.GetFloat();

        if (Age == 0) return MinorCD.GetFloat();
        if (Age == 18) return MajorCD.GetFloat();

        return MinorCD.GetFloat() + (MajorCD.GetFloat() - MinorCD.GetFloat()) / 18 * Age;
    }
    public override string GetProgressText(byte playerId, bool comms) => ColorString(GetRoleColor(CustomRoles.Mini), Age != 18 ? $"({Age})" : "");
    public override bool GuessCheck(bool isUI, PlayerControl guesser, PlayerControl target, CustomRoles role, ref bool guesserSuicide)
    {
        if (guesser.Is(CustomRoles.NiceMini) && Age < 18 && misguessed)
        {
            guesser.ShowInfoMessage(isUI, GetString("MiniGuessMax"));
            return true;
        }
        return false;
    }
    public override bool OnRoleGuess(bool isUI, PlayerControl target, PlayerControl guesser, CustomRoles role, ref bool guesserSuicide)
    {
        if (role is not CustomRoles.NiceMini or CustomRoles.EvilMini) return false;
        if (Age < 18 && (target.Is(CustomRoles.NiceMini) || !CanGuessEvil.GetBool() && target.Is(CustomRoles.EvilMini)))
        {
            guesser.ShowInfoMessage(isUI, GetString("GuessMini"));
            return true;
        }
        return false;
    }
    public override bool CheckMisGuessed(bool isUI, PlayerControl guesser, PlayerControl target, CustomRoles role, ref bool guesserSuicide)
    {
        if (Age < 18 && guesser.PlayerId == target.PlayerId)
        {
            misguessed = true;
            _ = new LateTask(() => { SendMessage(GetString("MiniMisGuessed"), target.PlayerId, ColorString(GetRoleColor(CustomRoles.NiceMini), GetString("GuessKillTitle")), true); }, 0.6f, "Mini MisGuess Msg");
            return true;
        }

        return false;
    }

    public override void CheckExile(NetworkedPlayerInfo exiled, ref bool DecidedWinner, bool isMeetingHud, ref string name)
    {
        var mini = GetPlayerById(exiled.PlayerId);
        if (mini != null && mini.Is(CustomRoles.NiceMini) && Age < 18)
        {
            if (!CustomWinnerHolder.CheckForConvertedWinner(exiled.PlayerId))
            {
                if (isMeetingHud)
                {
                    name = string.Format(GetString("ExiledNiceMini"), Main.LastVotedPlayer, GetDisplayRoleAndSubName(exiled.PlayerId, exiled.PlayerId, false, true));
                }
                else
                {
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.NiceMini);
                    CustomWinnerHolder.WinnerIds.Add(exiled.PlayerId);
                }
                DecidedWinner = true;
            }
        }
    }

    //public override bool OthersKnowTargetRoleColor(PlayerControl seer, PlayerControl target)
    //    => (target.Is(CustomRoles.NiceMini) || target.Is(CustomRoles.EvilMini)) && EveryoneCanKnowMini.GetBool();

    //public override string PlayerKnowTargetColor(PlayerControl seer, PlayerControl target)
    //    => (target.Is(CustomRoles.NiceMini) || target.Is(CustomRoles.EvilMini)) && EveryoneCanKnowMini.GetBool() ? Main.roleColors[CustomRoles.Mini] : string.Empty;

    public override string GetMarkOthers(PlayerControl seer, PlayerControl target = null, bool isForMeeting = false)
        => EveryoneCanKnowMini.GetBool() && (target.Is(CustomRoles.NiceMini) || target.Is(CustomRoles.EvilMini))
            ? CustomRoles.Mini.GetColoredTextByRole(Age != 18 && UpDateAge.GetBool() ? Age.ToString() : string.Empty)
            : string.Empty;
}
