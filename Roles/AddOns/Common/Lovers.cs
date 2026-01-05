using Hazel;
using TOHE.Modules.Rpc;
using TOHE.Roles.Neutral;
using static TOHE.Options;

namespace TOHE.Roles.AddOns.Common;

public class Lovers : IAddon
{
    public CustomRoles Role => CustomRoles.Lovers;
    private const int Id = 23600;
    public AddonTypes Type => AddonTypes.Misc;
    private static readonly List<(byte, byte)> loverPairs = [];
    private static readonly Dictionary<(byte, byte), bool> hasHeartbreak = [];
    public static byte loverless = byte.MaxValue;
    public static bool IsEnable => loverPairs.Any();

    public static OptionItem LoverKnowRoles;
    public static OptionItem LoverSuicide;
    public void SetupCustomOption()
    {
        var spawnOption = StringOptionItem.Create(Id, "Lovers", EnumHelper.GetAllNames<RatesZeroOne>(), 0, TabGroup.Addons, false).SetColor(Utils.GetRoleColor(CustomRoles.Lovers))
            .SetHeader(true)
            .SetGameMode(CustomGameMode.Standard) as StringOptionItem;

        var countOption = IntegerOptionItem.Create(Id + 1, "NumberOfLovers", new(2, 16, 2), 2, TabGroup.Addons, false)
            .SetParent(spawnOption)
            .SetValueFormat(OptionFormat.Players)
            .SetGameMode(CustomGameMode.Standard);

        var spawnRateOption = IntegerOptionItem.Create(Id + 2, "LoverSpawnChances", new(0, 100, 5), 65, TabGroup.Addons, false)
            .SetParent(spawnOption)
            .SetValueFormat(OptionFormat.Percent)
            .SetGameMode(CustomGameMode.Standard) as IntegerOptionItem;

        LoverKnowRoles = BooleanOptionItem.Create(Id + 4, "LoverKnowRoles", true, TabGroup.Addons, false)
            .SetParent(spawnOption)
            .SetGameMode(CustomGameMode.Standard);

        LoverSuicide = BooleanOptionItem.Create(Id + 3, "LoverSuicide", true, TabGroup.Addons, false)
            .SetParent(spawnOption)
            .SetGameMode(CustomGameMode.Standard);

        var impOption = BooleanOptionItem.Create(Id + 5, "ImpCanBeInLove", true, TabGroup.Addons, false)
            .SetParent(spawnOption)
            .SetGameMode(CustomGameMode.Standard)
            .AddReplacement(("{role}", CustomRoles.Lovers.ToColoredString()));

        var neutralOption = BooleanOptionItem.Create(Id + 7, "NeutralCanBeInLove", true, TabGroup.Addons, false)
            .SetParent(spawnOption)
            .SetGameMode(CustomGameMode.Standard)
            .AddReplacement(("{role}", CustomRoles.Lovers.ToColoredString()));

        var crewOption = BooleanOptionItem.Create(Id + 6, "CrewCanBeInLove", true, TabGroup.Addons, false)
            .SetParent(spawnOption)
            .SetGameMode(CustomGameMode.Standard)
            .AddReplacement(("{role}", CustomRoles.Lovers.ToColoredString()));

        var covenOption = BooleanOptionItem.Create(Id + 8, "CovenCanBeInLove", true, TabGroup.Addons, false)
            .SetParent(spawnOption)
            .SetGameMode(CustomGameMode.Standard)
            .AddReplacement(("{role}", CustomRoles.Lovers.ToColoredString()));

        AddonCanBeSettings.Add(CustomRoles.Lovers, (impOption, neutralOption, crewOption, covenOption));


        CustomAdtRoleSpawnRate.Add(CustomRoles.Lovers, spawnRateOption);
        CustomRoleSpawnChances.Add(CustomRoles.Lovers, spawnOption);
        CustomRoleCounts.Add(CustomRoles.Lovers, countOption);
    }
    public void Init()
    {
        loverPairs.Clear();
        loverless = byte.MaxValue;
    }
    public void Add(byte playerId, bool gameIsLoading = true)
    {
        if (loverless == byte.MaxValue)
        {
            loverless = playerId;
            SendRPC();
            return;
        }

        loverPairs.Add((loverless, playerId));
        hasHeartbreak.Add((loverless, playerId), false);

        loverless = byte.MaxValue;

        SendRPC();
    }
    public void Remove(byte playerId)
    {
        if (loverless == playerId)
        {
            loverless = byte.MaxValue;
            return;
        }

        var loverId = GetLoverId(playerId);
        var pair = GetPair(playerId, loverId);

        var hadHeartbreak = hasHeartbreak[pair];

        loverPairs.Remove(pair);
        hasHeartbreak.Remove(pair);

        if (loverless == byte.MaxValue)
        {
            loverless = loverId;
            return;
        }

        // Two broken people will find each other, because lovers crave companionship
        loverPairs.Add((loverless, playerId));
        hasHeartbreak.Add((loverless, playerId), hadHeartbreak);

        loverless = byte.MaxValue;

        SendRPC();
    }
    public static byte GetLoverId(PlayerControl player) => GetLoverId(player.PlayerId);
    public static byte GetLoverId(byte playerId)
    {
        // if (loverless == playerId) return byte.MaxValue;

        foreach (var pair in loverPairs)
        {
            if (loverless == pair.Item1 || loverless == pair.Item2) Logger.Warn("Lover is also loverless?", "Lovers");
            if (pair.Item1 == playerId) return pair.Item2;
            if (pair.Item2 == playerId) return pair.Item1;
        }

        return byte.MaxValue;
    }
    public static (byte PlayerId1, byte PlayerId2) GetPair(byte player, byte target) => loverPairs.FirstOrDefault(x => x.Item1 == player && x.Item2 == target || x.Item2 == player && x.Item1 == target);
    public static bool AreLovers(PlayerControl player, PlayerControl target) => AreLovers(player.PlayerId, target.PlayerId);
    public static bool AreLovers(byte player, byte target) => loverPairs.Any(x => (x.Item1 == player && x.Item2 == target) || (x.Item2 == player && x.Item1 == target));
    public static bool LoverIsAlive(PlayerControl player) => LoverIsAlive(player.PlayerId);
    public static bool LoverIsAlive(byte player)
    {
        var loverId = GetLoverId(player);
        var pair = GetPair(player, loverId);

        if (hasHeartbreak.TryGetValue(pair, out bool heartbreak)) return !heartbreak;

        return false;
    }

    public static void OnCheckForEndVoting(PlayerState.DeathReason deathReason, params byte[] exileIds)
    {
        if (!IsEnable || deathReason == PlayerState.DeathReason.Vote) return;

        List<byte> toKill = [];
        foreach (var pair in loverPairs)
        {
            if (exileIds.Contains(pair.Item1) && !exileIds.Contains(pair.Item2)) toKill.Add(pair.Item2);
            if (exileIds.Contains(pair.Item2) && !exileIds.Contains(pair.Item1)) toKill.Add(pair.Item1);
        }

        foreach (var playerId in toKill)
        {
            if (LoverIsAlive(playerId))
                LoversSuicide(playerId, true);
        }
    }
    public static void OnFixedUpdate(PlayerControl player, bool lowLoad, long nowTime, int timerLowLoad)
    {
        LoversSuicide();
    }
    public static void LoversSuicide(byte deathId = 0x7f, bool isExiled = false)
    {
        if (!LoverSuicide.GetBool()) return;

        foreach (var pair in loverPairs)
        {
            PlayerControl p1 = pair.Item1.GetPlayer(), p2 = pair.Item2.GetPlayer();

            if (p1.IsAlive() && pair.Item1 != deathId && p2.IsAlive() && pair.Item2 != deathId) continue;

            if (Cupid.IsCupidLoverPair(p1, p2)) continue;

            if (hasHeartbreak[pair]) continue;

            hasHeartbreak[pair] = true;

            // Switch order so p1 is the dead one
            var playerPair = (p1, p2);
            if (p1.IsAlive() && pair.Item1 != deathId) playerPair = (p2, p1);
            (p1, p2) = playerPair;

            p2.SetDeathReason(PlayerState.DeathReason.FollowingSuicide);

            if (isExiled)
            {
                if (Main.PlayersDiedInMeeting.Contains(deathId))
                {
                    p2.Data.IsDead = true;
                    p2.RpcExileV2();
                    Main.PlayerStates[p2.PlayerId].SetDead();
                    if (MeetingHud.Instance?.state is MeetingHud.VoteStates.Discussion or MeetingHud.VoteStates.NotVoted or MeetingHud.VoteStates.Voted)
                    {
                        MeetingHud.Instance?.CheckForEndVoting();
                    }
                    MurderPlayerPatch.AfterPlayerDeathTasks(p2, p2, true);
                    _ = new LateTask(() => HudManager.Instance?.SetHudActive(false), 0.3f, "SetHudActive in LoversSuicide", shoudLog: false);
                }
                else
                {
                    CheckForEndVotingPatch.TryAddAfterMeetingDeathPlayers(PlayerState.DeathReason.FollowingSuicide, p2.PlayerId);
                }
            }
            else
            {
                p2.RpcMurderPlayer(p2);
            }
        }
    }

    public static string GetMarkOthers(PlayerControl seer, PlayerControl seen)
    {
        string colorCode = Utils.GetRoleColorCode(CustomRoles.Lovers);
        if (AreLovers(seer, seen) || (seer.Is(CustomRoles.Lovers) && seer.PlayerId == seen.PlayerId))
        {
            return $"<color={colorCode}>♥</color>";
        }
        else if ((!seer.IsAlive() || Cupid.IsCupidLover(seer, seen)) && seen.Is(CustomRoles.Lovers))
        {
            byte loverId = GetLoverId(seen);
            return $"<color={colorCode}>♥{loverId}</color>";
        }

        return "";
    }


    public static void SendRPC()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        var msg = new RpcSetLoverPairs(PlayerControl.LocalPlayer.NetId, loverPairs.Count, loverPairs, loverless);
        RpcUtils.LateBroadcastReliableMessage(msg);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        loverPairs.Clear();
        int count = reader.ReadInt32();
        Logger.Info($"Received {count} lover pairs.", "Lovers.ReceiveRPC");

        for (int i = 0; i < count; i++)
        {
            var pair = (reader.ReadByte(), reader.ReadByte());
            loverPairs.Add(pair);
            Logger.Info($"{pair.Item1.GetPlayer().GetRealName()} ♥ {pair.Item2.GetPlayer().GetRealName()}", "Lovers.ReceiveRPC");
        }

        loverless = reader.ReadByte();

        Logger.Info($"{loverless.GetPlayer().GetRealName()} has no lover, how sad.", "Lovers.ReceiveRPC");
    }

    public static void CheckWin()
    {
        var alivePairs = loverPairs.Where(p => !((!p.Item1.GetPlayer().IsAlive() || !p.Item2.GetPlayer().IsAlive()) && LoverSuicide.GetBool()));

        if (!alivePairs.Any()) return;
        // if not (some lovers dead and lovers suicide)
        if (CustomWinnerHolder.WinnerTeam is CustomWinner.Crewmate or CustomWinner.Impostor or CustomWinner.Jackal or CustomWinner.Pelican or CustomWinner.Coven)
        {
            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Lovers);
            foreach (var pair in alivePairs)
            {
                CustomWinnerHolder.WinnerIds.Add(pair.Item1);
                CustomWinnerHolder.WinnerIds.Add(pair.Item2);
                
            }
        }
    }
    public static void CheckAdditionalWin()
    {
        var loverWinners = CustomWinnerHolder.WinnerIds.Where(p => p.GetPlayer().Is(CustomRoles.Lovers));

        foreach (var lover in loverWinners)
        {
            var loverId = GetLoverId(lover);
            if (!CustomWinnerHolder.WinnerIds.Contains(loverId))
            {
                CustomWinnerHolder.WinnerIds.Add(loverId);
                CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.Lovers);
            }
        }
    }

    public static void OnPartnerLeft(byte playerId)
    {
        var loverId = GetLoverId(playerId);

        var pair = loverPairs.First(x => x.Item1 == playerId || x.Item2 == loverId);
        loverPairs.Remove(pair);

        if (loverless != byte.MaxValue)
        {
            loverPairs.Add((loverId, loverless));
            loverless = byte.MaxValue;
        }
        else
        {
            loverless = loverId;
        }
    }
}

public static class LoversUtils
{
    public static bool IsLoverWith(this PlayerControl player, PlayerControl target) => Lovers.AreLovers(player, target);
}