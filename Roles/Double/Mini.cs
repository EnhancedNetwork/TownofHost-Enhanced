using HarmonyLib;
using Hazel;
using MS.Internal.Xml.XPath;
using Sentry;
using System.Collections.Generic;
using System.Linq;
using TOHE.Roles.Neutral;
using UnityEngine;
using static TOHE.RandomSpawn;
using static UnityEngine.GraphicsBuffer;

namespace TOHE.Roles.Double;
public class Mini
{
    private static readonly int Id = 7565376;
    public static bool IsEvilMini = false;
    public static void SetMiniTeam(float EvilMiniRate)
    {
        EvilMiniRate = EvilMiniSpawnChances.GetFloat();
        IsEvilMini = Random.Range(1, 100) < EvilMiniRate;
    }
    private static List<byte> playerIdList = new();
    public static int GrowUpTime = new();
    public static int GrowUp = new();
    //public static int EvilKillCDmin = new();
    public static int Age = new();
    public static OptionItem GrowUpDuration;
    public static OptionItem EveryoneCanKnowMini;
    //public static OptionItem OnMeetingStopCountdown;
    public static bool IsEnable = false;
    public static OptionItem EvilMiniSpawnChances;
    public static OptionItem CanBeEvil;
    public static OptionItem UpDateAge;
    public static OptionItem MinorCD;
    public static OptionItem MajorCD;
    public static void SetupCustomOption()
    {
        Options.SetupSingleRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Mini, 1, zeroOne: false);
        GrowUpDuration = IntegerOptionItem.Create(Id + 100, "GrowUpDuration", new(200, 800, 25), 400, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Mini])
            .SetValueFormat(OptionFormat.Seconds);
        EveryoneCanKnowMini = BooleanOptionItem.Create(Id + 102, "EveryoneCanKnowMini", true, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Mini]);
        CanBeEvil = BooleanOptionItem.Create(Id + 106, "CanBeEvil", true, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Mini]);
        EvilMiniSpawnChances = IntegerOptionItem.Create(Id + 108, "EvilMiniSpawnChances", new(0, 100, 5), 50, TabGroup.CrewmateRoles, false).SetParent(CanBeEvil)
            .SetValueFormat(OptionFormat.Percent);
        MinorCD = FloatOptionItem.Create(Id + 110, "KillCooldown", new(0f, 180f, 2.5f), 45f, TabGroup.CrewmateRoles, false).SetParent(CanBeEvil)
            .SetValueFormat(OptionFormat.Seconds);
        MajorCD = FloatOptionItem.Create(Id + 112, "MajorCooldown", new(0f, 180f, 2.5f), 15f, TabGroup.CrewmateRoles, false).SetParent(CanBeEvil)
           .SetValueFormat(OptionFormat.Seconds);
        UpDateAge = BooleanOptionItem.Create(Id + 114, "UpDateAge", true, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Mini]);
    }
    public static void Init()
    {
        GrowUpTime = 0;
        playerIdList = new();
        GrowUp = GrowUpDuration.GetInt() / 18;
        IsEnable = false;
        Age = 0;
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        IsEnable = true;

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }
    public static void SendRPC()
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncMiniAge, SendOption.Reliable, -1);
        writer.Write(Age);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        Age = reader.ReadInt32();
    }
    public static string GetAge(byte playerId) => Utils.ColorString(Utils.GetRoleColor(CustomRoles.Mini), Age != 18 ? $"({Age})" : "");
}