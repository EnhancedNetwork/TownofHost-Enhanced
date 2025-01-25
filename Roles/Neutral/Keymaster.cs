using Hazel;
using InnerNet;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.Neutral;

internal class Keymaster : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 32300;
    private static readonly HashSet<byte> playerIdList = [];
    public static bool HasEnabled = playerIdList.Any();

    public override CustomRoles Role => CustomRoles.Keymaster;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralChaos;
    //==================================================================\\

    private static readonly Dictionary<byte, HashSet<byte>> KeyedList = [];
    public static Dictionary<byte, string> KeymasterNotify = [];
    private static OptionItem GiveKeyCooldown;
    public static OptionItem StealsWin;
    private static OptionItem Stage2Difficulty;
    private static OptionItem ColoredKeyNotiDuration;
    private static byte KeyID = 10;
    private static byte ColID = 10;
    private static byte kmColor = 10;
    private static int KeyColor = 10;
    private static bool HasKey = false;
    public static bool HasWon = false;
    private static bool CanGetColoredKey = false;
    private static bool LimboState = false;
    int Count;
    public static long LastColorChange;

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Keymaster);
        GiveKeyCooldown = FloatOptionItem.Create(Id + 10, "KeymasterGiveKeyCooldown", new(0f, 60f, 2.5f), 5f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Keymaster])
            .SetValueFormat(OptionFormat.Seconds);
        ColoredKeyNotiDuration = IntegerOptionItem.Create(Id + 11, "KeymasterColoredKeyNotiDuration", new(1, 30, 1), 3, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Keymaster])
            .SetValueFormat(OptionFormat.Seconds);
        Stage2Difficulty = IntegerOptionItem.Create(Id + 12, "KeymasterStage2Difficulty", new(1, 3, 1), 3, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Keymaster]);
        StealsWin = BooleanOptionItem.Create(Id + 13, "KeymasterStealsWin", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Keymaster]);
    }
    public override void Init()
    {
        playerIdList.Clear();
        LastColorChange = GetTimeStamp();
        KeyColor = 10;
        KeyID = 10;
        ColID = 10;
        kmColor = 10;
        LimboState = false;
        HasKey = false;
        HasWon = false;
        CanGetColoredKey = false;
        KeyedList.Clear();
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        KeyedList[playerId] = [];

        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }
    public override void Remove(byte playerId)
    {
        playerIdList.Remove(playerId);
        KeyedList.Remove(playerId);
    }
    public override void SetAbilityButtonText(HudManager hud, byte id) => hud.AbilityButton.OverrideText(GetString("KeymasterButtonText"));
    public override Sprite GetAbilityButtonSprite(PlayerControl player, bool shapeshifting) => CustomButton.Get("Mark");

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = GiveKeyCooldown.GetFloat();
    public override bool CanUseKillButton(PlayerControl pc) => true;
    public override bool CanUseImpostorVentButton(PlayerControl pc) => true;
    private static bool IsKeyed(byte pc, byte target) => KeyedList.TryGetValue(pc, out var Targets) && Targets.Contains(target);

    public static void SendRPC(PlayerControl player, PlayerControl target)
    {
        MessageWriter writer;
        writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, -1);
        writer.WriteNetObject(GetPlayerById(playerIdList.First())); // setKeyedPlayer
        writer.Write(player.PlayerId);
        writer.Write(target.PlayerId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public override void ReceiveRPC(MessageReader reader, PlayerControl NaN)
    {
        byte KeymasterId = reader.ReadByte();
        byte KeyedId = reader.ReadByte();
        KeyedList[KeymasterId].Add(KeyedId);
    }

    public override void OnEnterVent(PlayerControl pc, Vent vent)
    {
        if (IsKeyedAll(pc))
        {
            HasKey = false;
        }
        if (KeyColor == 1 && kmColor == 1 || KeyColor == 2 && kmColor == 2 || KeyColor == 3 && kmColor == 3 || KeyColor == 4 && kmColor == 4 || KeyColor == 5 && kmColor == 5 || KeyColor == 6 && kmColor == 6 || KeyColor == 7 && kmColor == 7 || KeyColor == 8 && kmColor == 8)
        {
            pc.Notify(GetString("KeymasterGG"), 10f);
            KeyColor = 69;
            HasWon = true;
            foreach (var player in Main.AllAlivePlayerControls)
                if (pc.PlayerId != player.PlayerId && StealsWin.GetBool() == true)
                {
                    Main.PlayerStates[player.PlayerId].deathReason = player.PlayerId == pc.PlayerId ?
                        PlayerState.DeathReason.Overtired : PlayerState.DeathReason.Ashamed;

                    pc.RpcMurderPlayer(player);
                    player.SetRealKiller(pc);
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Keymaster); //Keymaster solo win
                    CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);

                }

        }
        if (KeyColor == 1 && kmColor != 1 || KeyColor == 2 && kmColor != 2 || KeyColor == 3 && kmColor != 3 || KeyColor == 4 && kmColor != 4 || KeyColor == 5 && kmColor != 5 || KeyColor == 6 && kmColor != 6 || KeyColor == 7 && kmColor != 7 || KeyColor == 8 && kmColor != 8)
        {
            pc.Notify(GetString("KeymasterL"), 10f);
            pc.RpcMurderPlayer(pc);
        }
        if (LimboState == false)
        {
            HasKey = true;
            pc.Notify(GetString("KeymasterGainedKey"), 5f);
        }
    }
    public override bool ForcedCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (IsKeyed(killer.PlayerId, target.PlayerId))
        {
            return false;
        }
        if (HasKey == true)
        {
            KeyedList[killer.PlayerId].Add(target.PlayerId);
            SendRPC(killer, target);
            NotifyRoles(SpecifySeer: killer);
            killer.Notify(GetString("KeymasterMarkedPlayer"), 5f);

            CheckKeyedAllPlayers();

            killer.ResetKillCooldown();
            killer.SetKillCooldown();
            HasKey = false;
        }
        if (CanGetColoredKey == true)
        {
            var rand = IRandom.Instance;
            KeyID = (byte)rand.Next(1, 9);

            switch (KeyID)
            {
                case 1:
                    killer.Notify(GetString("KeymasterRed"), ColoredKeyNotiDuration.GetInt());
                    KeyColor = 1;
                    break;
                case 2:
                    killer.Notify(GetString("KeymasterBlue"), ColoredKeyNotiDuration.GetInt());
                    KeyColor = 2;
                    break;
                case 3:
                    killer.Notify(GetString("KeymasterGreen"), ColoredKeyNotiDuration.GetInt());
                    KeyColor = 3;
                    break;
                case 4:
                    killer.Notify(GetString("KeymasterPink"), ColoredKeyNotiDuration.GetInt());
                    KeyColor = 4;
                    break;
                case 5:
                    killer.Notify(GetString("KeymasterOrange"), ColoredKeyNotiDuration.GetInt());
                    KeyColor = 5;
                    break;
                case 6:
                    killer.Notify(GetString("KeymasterYellow"), ColoredKeyNotiDuration.GetInt());
                    KeyColor = 6;
                    break;
                case 7:
                    killer.Notify(GetString("KeymasterBlack"), ColoredKeyNotiDuration.GetInt());
                    KeyColor = 7;
                    break;
                case 8:
                    killer.Notify(GetString("KeymasterWhite"), ColoredKeyNotiDuration.GetInt());
                    KeyColor = 8;
                    break;
                default:
                    break;
            }
            LimboState = true;
            CanGetColoredKey = false;
            return false;
        }
        return false;
    }
    private static bool IsKeyedAll(PlayerControl player)
    {
        if (!player.Is(CustomRoles.Keymaster)) return false;

        var (keyed, all) = KeyedPlayerCount(player.PlayerId);
        return keyed >= all;
    }
    private static (int, int) KeyedPlayerCount(byte playerId)
    {
        int all = Main.AllAlivePlayerControls.Count(pc => pc.PlayerId != playerId);
        int keyed = Main.AllAlivePlayerControls.Count(pc => pc.PlayerId != playerId && IsKeyed(playerId, pc.PlayerId));

        return (keyed, all);
    }

    public static void CheckKeyedAllPlayers()
    {
        foreach (var KeyedId in KeyedList.Keys)
        {
            var Keymaster = GetPlayerById(KeyedId);
            if (Keymaster == null) continue;

            if (IsKeyedAll(Keymaster))
            {
                Keymaster.RpcGuardAndKill(Keymaster);
                Keymaster.ResetKillCooldown();
                CanGetColoredKey = true;
                NotifyRoles(SpecifySeer: Keymaster);
                Keymaster.MarkDirtySettings();
            }
        }
    }
    public override string GetMark(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
        => IsKeyed(seer.PlayerId, seen.PlayerId) ? ColorString(GetRoleColor(CustomRoles.Keymaster), "♀") : string.Empty;

    public override string GetProgressText(byte playerId, bool comms)
    {
        var (keyed, all) = KeyedPlayerCount(playerId);
        return ColorString(GetRoleColor(CustomRoles.Keymaster).ShadeColor(0.25f), $"({keyed}/{all})");
    }
    public override void OnFixedUpdateLowLoad(PlayerControl pc)
    {
        Count++;

        if (Stage2Difficulty.GetInt() == 3 && Count < 20) return;
        if (Stage2Difficulty.GetInt() == 2 && Count < 30) return;
        if (Stage2Difficulty.GetInt() == 1 && Count < 40) return;
        Count = 0;
        if (Count % 3 == 0 && LimboState == true)
        {
            Count = 0;
            var rand = IRandom.Instance;
            ColID = (byte)rand.Next(1, 9);
            var KeymasterOutfit = Camouflage.PlayerSkins[pc.PlayerId];

            switch (ColID)
            {
                case 1:
                    if (kmColor != 1)
                    {
                        pc.RpcSetColor(0);
                        kmColor = 1;
                    }
                    else
                    {
                        pc.RpcSetColor(1);
                        kmColor = 2;
                    }
                    break;
                case 2:
                    if (kmColor != 2)
                    {
                        pc.RpcSetColor(1);
                        kmColor = 2;
                    }
                    else
                    {
                        pc.RpcSetColor(2);
                        kmColor = 3;
                    }
                    break;
                case 3:
                    if (kmColor != 3)
                    {
                        pc.RpcSetColor(2);
                        kmColor = 3;
                    }
                    else
                    {
                        pc.RpcSetColor(3);
                        kmColor = 4;
                    }
                    break;
                case 4:
                    if (kmColor != 4)
                    {
                        pc.RpcSetColor(3);
                        kmColor = 4;
                    }
                    else
                    {
                        pc.RpcSetColor(4);
                        kmColor = 5;
                    }
                    break;
                case 5:
                    if (kmColor != 5)
                    {
                        pc.RpcSetColor(4);
                        kmColor = 5;
                    }
                    else
                    {
                        pc.RpcSetColor(5);
                        kmColor = 6;
                    }
                    break;
                case 6:
                    if (kmColor != 6)
                    {
                        pc.RpcSetColor(5);
                        kmColor = 6;
                    }
                    else
                    {
                        pc.RpcSetColor(6);
                        kmColor = 7;
                    }
                    break;
                case 7:
                    if (kmColor != 7)
                    {
                        pc.RpcSetColor(6);
                        kmColor = 7;
                    }
                    else
                    {
                        pc.RpcSetColor(7);
                        kmColor = 8;
                    }
                    break;
                case 8:
                    if (kmColor != 8)
                    {
                        pc.RpcSetColor(7);
                        kmColor = 8;
                    }
                    else
                    {
                        pc.RpcSetColor(0);
                        kmColor = 1;
                    }
                    break;
                default:
                    break;

            }
        }
        Count = 0;
    }
}
