using AmongUs.GameOptions;
using Hazel;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TOHE.Modules;
using TOHE.Roles.Neutral;

namespace TOHE;

public static class AntiBlackout
{
    ///<summary>
    ///Whether to override the expulsion process due to one Impostor and Neutral killers
    ///</summary>
    public static bool ImpostorOverrideExiledPlayer => IsRequired && (IsSingleImpostor || Diff_CrewImp == 1);
    ///<summary>
    ///Whether to override the expulsion process due to Neutral Killers
    ///</summary>
    public static bool NeutralOverrideExiledPlayer => Options.TemporaryAntiBlackoutFix.GetBool() && CountNeutralKiller > 1 && !(IsSingleImpostor || Diff_CrewImp == 1);
    ///<summary>
    ///Whether there is only one impostors present in the setting
    ///</summary>
    public static bool IsSingleImpostor => Main.RealOptionsData != null ? Main.RealOptionsData.GetInt(Int32OptionNames.NumImpostors) <= 1 : Main.NormalOptions.NumImpostors <= 1;
    ///<summary>
    ///Whether processing within AntiBlackout is required
    ///</summary>
    public static bool IsRequired => Options.NoGameEnd.GetBool()
        // Neutrals
        || Jackal.IsEnable || BloodKnight.IsEnable
        || Glitch.IsEnable || Infectious.IsEnable
        || Juggernaut.IsEnable || Pelican.IsEnable
        || Pickpocket.IsEnable || NSerialKiller.IsEnable
        || Shroud.IsEnable || Traitor.IsEnable
        || Virus.IsEnable || Werewolf.IsEnable
        || Gamer.IsEnable || Succubus.IsEnable
        || NWitch.IsEnable || Maverick.IsEnable
        || RuthlessRomantic.IsEnable || Bandit.IsEnable
        || Spiritcaller.IsEnable //|| Occultist.IsEnable
        || Pyromaniac.IsEnable || Huntsman.IsEnable
        || PlagueBearer.IsEnable || CustomRoles.Pestilence.RoleExist(true)
        || HexMaster.IsEnable || Jinx.IsEnable
        || Medusa.IsEnable || Poisoner.IsEnable
        || PotionMaster.IsEnable || Wraith.IsEnable
        || Necromancer.IsEnable || Doppelganger.IsEnable
        || CustomRoles.Sidekick.RoleExist(true) || (CustomRoles.Arsonist.RoleExist(true) && Options.ArsonistCanIgniteAnytime.GetBool());
    ///<summary>
    ///Difference between the number of non-impostors and the number of imposters
    ///</summary>
    public static int Diff_CrewImp
    {
        get
        {
            int numCrewmates = 0;
            int numImpostors = 0;

            foreach (var pc in Main.AllPlayerControls)
            {
                if (pc.Data.Role.IsImpostor) numImpostors++;
                else numCrewmates++;
            }

            Logger.Info($" {numCrewmates}", "AntiBlackout Num Crewmates");
            Logger.Info($" {numImpostors}", "AntiBlackout Num Impostors");
            return numCrewmates - numImpostors;
        }
    }
    public static int CountNeutralKiller
    {
        get
        {
            int numNeutrals = 0;

            foreach (var pc in Main.AllPlayerControls)
            {
                if ((pc.GetCustomRole().IsNK() && !pc.Is(CustomRoles.Arsonist))) numNeutrals++;
                else if (pc.Is(CustomRoles.Arsonist) && Options.ArsonistCanIgniteAnytime.GetBool()) numNeutrals++;
                else if (pc.Is(CustomRoles.Succubus)) numNeutrals++;
            }

            Logger.Info($" {numNeutrals}", "AntiBlackout Num Neutrals");
            return numNeutrals;
        }
    }
    public static bool IsCached { get; private set; } = false;
    private static Dictionary<byte, (bool isDead, bool Disconnected)> isDeadCache = new();
    private readonly static LogHandler logger = Logger.Handler("AntiBlackout");

    public static void SetIsDead(bool doSend = true, [CallerMemberName] string callerMethodName = "")
    {
        logger.Info($"SetIsDead is called from {callerMethodName}");
        if (IsCached)
        {
            logger.Info("Please run RestoreIsDead before running SetIsDead again.");
            return;
        }
        isDeadCache.Clear();
        foreach (var info in GameData.Instance.AllPlayers.ToArray())
        {
            if (info == null) continue;
            isDeadCache[info.PlayerId] = (info.IsDead, info.Disconnected);
            info.IsDead = false;
            info.Disconnected = false;
        }
        IsCached = true;
        if (doSend) SendGameData();
    }
    public static void RestoreIsDead(bool doSend = true, [CallerMemberName] string callerMethodName = "")
    {
        logger.Info($"RestoreIsDead is called from {callerMethodName}");
        foreach (var info in GameData.Instance.AllPlayers.ToArray())
        {
            if (info == null) continue;
            if (isDeadCache.TryGetValue(info.PlayerId, out var val))
            {
                info.IsDead = val.isDead;
                info.Disconnected = val.Disconnected;
            }
        }
        isDeadCache.Clear();
        IsCached = false;
        if (doSend) SendGameData();
    }

    public static void SendGameData([CallerMemberName] string callerMethodName = "")
    {
        logger.Info($"SendGameData is called from {callerMethodName}");
        MessageWriter writer = MessageWriter.Get(SendOption.Reliable);
        // The writing {} is for readability.
        writer.StartMessage(5); //0x05 GameData
        {
            writer.Write(AmongUsClient.Instance.GameId);
            writer.StartMessage(1); //0x01 Data
            {
                writer.WritePacked(GameData.Instance.NetId);
                GameData.Instance.Serialize(writer, true);

            }
            writer.EndMessage();
        }
        writer.EndMessage();

        AmongUsClient.Instance.SendOrDisconnect(writer);
        writer.Recycle();
    }
    public static void OnDisconnect(GameData.PlayerInfo player)
    {
        // Execution conditions: Client is the host, IsDead is overridden, player is already disconnected
        if (!AmongUsClient.Instance.AmHost || !IsCached || !player.Disconnected) return;
        isDeadCache[player.PlayerId] = (true, true);
        player.IsDead = player.Disconnected = false;
        SendGameData();
    }

    ///<summary>
    ///Execute the code with IsDead temporarily set back to what it should be
    ///<param name="action">Execution details</param>
    ///</summary>
    public static void TempRestore(Action action)
    {
        logger.Info("==Temp Restore==");
        // Whether TempRestore was executed with IsDead overwritten
        bool before_IsCached = IsCached;
        try
        {
            if (before_IsCached) RestoreIsDead(doSend: false);
            action();
        }
        catch (Exception ex)
        {
            logger.Warn("An exception occurred within AntiBlackout.TempRestore");
            logger.Exception(ex);
        }
        finally
        {
            if (before_IsCached) SetIsDead(doSend: false);
            logger.Info("==/Temp Restore==");
        }
    }

    public static void Reset()
    {
        logger.Info("==Reset==");
        if (isDeadCache == null) isDeadCache = new();
        isDeadCache.Clear();
        IsCached = false;
        ShowExiledInfo = false;
        StoreExiledMessage = "";
    }

    public static bool ShowExiledInfo = false;
    public static string StoreExiledMessage = "";
}