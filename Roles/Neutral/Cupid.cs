using AmongUs.GameOptions;
using Hazel;
using TOHE.Modules;
using TOHE.Modules.Rpc;
using TOHE.Roles.Core;
using TOHE.Roles.AddOns.Impostor;
using static TOHE.Options;
using static TOHE.Translator;
using TOHE.Roles.Coven;
using TOHE.Roles.Crewmate;
using TOHE.Roles.AddOns.Common;
using UnityEngine;

namespace TOHE.Roles.Neutral;

internal class Cupid : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Cupid;
    private const int Id = 32400;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Cupid);
    public override bool IsExperimental => true;
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Shapeshifter;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralBenign;
    //==================================================================\\
    private static readonly Dictionary<byte, (byte, byte)?> cupidArrows = [];
    private static readonly Dictionary<byte, byte?> firstArrow = [];
    private static readonly Dictionary<byte, bool> isProtecting = [];

    // Your arrow couldn’t charm the target
    private static OptionItem CharmCooldown;
    public static OptionItem LoversKnowCupid;
    private static OptionItem ProtectCooldown;
    private static OptionItem ProtectDuration;

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Cupid);
        CharmCooldown = FloatOptionItem.Create(Id + 10, "CupidSettings.CharmCooldown", new(0f, 180f, 2.5f), 30f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Cupid])
            .SetValueFormat(OptionFormat.Seconds);
        LoversKnowCupid = BooleanOptionItem.Create(Id + 11, "CupidSettings.LoversKnowCupid", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Cupid]);
        ProtectCooldown = FloatOptionItem.Create(Id + 12, "CupidSettings.ProtectCooldown", new(0f, 180f, 2.5f), 30f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Cupid])
            .SetValueFormat(OptionFormat.Seconds);
        ProtectDuration = FloatOptionItem.Create(Id + 13, "CupidSettings.ProtectDuration", new(0f, 180f, 2.5f), 30f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Cupid])
            .SetValueFormat(OptionFormat.Seconds);
    }

    public override void Init()
    {
        cupidArrows.Clear();
    }

    public override void Add(byte playerId)
    {
        // playerId.SetAbilityUseLimit(2);
        cupidArrows[playerId] = null;
        firstArrow[playerId] = null;
        isProtecting[playerId] = false;
    }

    public override void Remove(byte playerId)
    {
        cupidArrows.Remove(playerId);
        firstArrow.Remove(playerId);
        isProtecting.Remove(playerId);
    }

    private void SendRPC()
    {
        var writer = MessageWriter.Get(SendOption.Reliable);

        writer.Write(cupidArrows.Count);

        foreach (var arrow in cupidArrows)
        {
            writer.Write(arrow.Key);
            var val = arrow.Value ?? (0xff, 0xff);
            writer.Write(val.Item1);
            writer.Write(val.Item2);
        }

        RpcUtils.LateBroadcastReliableMessage(new RpcSyncRoleSkill(PlayerControl.LocalPlayer.NetId, _Player.NetId, writer));
    }
    public override void ReceiveRPC(MessageReader reader, PlayerControl NaN)
    {
        cupidArrows.Clear();
        var count = reader.ReadInt16();

        for (int i = 0; i < count; i++)
        {
            var key = reader.ReadByte();

            var val1 = reader.ReadByte();
            var val2 = reader.ReadByte();

            if (val1 == 0xff && val2 == 0xff)
            {
                cupidArrows[key] = null;
                continue;
            }

            cupidArrows[key] = (val1, val2);
        }
    }

    public override bool CheckMurderOnOthersTarget(PlayerControl killer, PlayerControl target)
    {
        if (!HasEnabled) return false;

        // returns 0xff is target is not one of cupid's lovers
        var cupid = GetCupidId(target.PlayerId);

        if (cupid == 0xff) return false;

        if (isProtecting.TryGetValue(cupid, out bool protect) && protect) return true;

        return false;
    }

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = ProtectCooldown.GetFloat();
    public override bool CanUseKillButton(PlayerControl pc) => cupidArrows.TryGetValue(pc.PlayerId, out var pair) && pair != null;
    public override bool ForcedCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (killer.PlayerId == target.PlayerId) return true;

        if (!(isProtecting.TryGetValue(killer.PlayerId, out bool protect) && protect))
        {
            killer.ResetKillCooldown();
            killer.SetKillCooldown();
            killer.RPCPlayCustomSound("Shield");
            killer.Notify(GetString("Cupid.ProtectLovers"));

            _ = new LateTask(() =>
                {
                    if (!GameStates.IsInTask) return;
                    isProtecting[killer.PlayerId] = false;
                    killer.Notify(GetString("Cupid.ProtectingOver"));
                    killer.SetKillCooldown();
                }, ProtectDuration.GetFloat(), "Cupid Protecting Is Over");
        }

        return false;
    }
    public override bool OnCheckShapeshift(PlayerControl shapeshifter, PlayerControl target, ref bool resetCooldown, ref bool shouldAnimate)
    {
        shouldAnimate = false;
        if (cupidArrows.TryGetValue(shapeshifter.PlayerId, out var pair) && pair != null)
        {
            shapeshifter.Notify(GetString("Cupid.AlreadyHasPair"));
            return false;
        }
        if (shapeshifter == null || target == null) return false;

        AddTarget(shapeshifter, target);
        
        return false;
    }

    private void AddTarget(PlayerControl cupid, PlayerControl target)
    {
        if (!CustomRolesHelper.CheckAddonConfilct(CustomRoles.Lovers, target, checkLimitAddons: false))
        {
            cupid.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Cultist), GetString("Cupid.CantCharm")));
            return;
        }
        if (!(firstArrow.TryGetValue(cupid.PlayerId, out byte? first) && first != null))
        {
            firstArrow[cupid.PlayerId] = target.PlayerId;
            Logger.Info($"{target.GetRealName()} is Lover1", "Cupid");

            if (Lovers.loverless != byte.MaxValue)
            {
                var newTarget = Lovers.loverless;
                Lovers.loverless = byte.MaxValue;
                AddTarget(cupid, newTarget.GetPlayer());
            }
        }
        else
        {
            cupidArrows[cupid.PlayerId] = (first ?? byte.MaxValue, target.PlayerId);

            SendRPC();

            PlayerControl p = first.GetValueOrDefault(0xff).GetPlayer();

            p.RpcSetCustomRole(CustomRoles.Lovers, false, true);
            target.RpcSetCustomRole(CustomRoles.Lovers, false, true);

            Utils.NotifyRoles(SpecifySeer: cupid, SpecifyTarget: target);
            Utils.NotifyRoles(SpecifySeer: target, SpecifyTarget: cupid);
            Logger.Info($"{target.GetRealName()} is Lover2", "Cupid");
        }
        
        cupid.Notify(string.Format(GetString("Cupid.PlayerAdded"), target.GetRealName()));
    }

    public static bool IsCupidLover(PlayerControl cupid, PlayerControl lover) => IsCupidLover(cupid.PlayerId, lover.PlayerId);
    public static bool IsCupidLover(byte cupidId, byte loverId)
    {
        if (!cupidArrows.TryGetValue(cupidId, out var loverPair)) return false;

        var pair = loverPair ?? (byte.MaxValue, byte.MaxValue);

        return pair.Item1 == loverId || pair.Item2 == loverId;
    }

    public static bool IsCupidLoverPair(PlayerControl player1, PlayerControl player2) => IsCupidLoverPair(player1.PlayerId, player2.PlayerId);
    public static bool IsCupidLoverPair(byte p1, byte p2) => cupidArrows.Where(x => x.Value != null).Any(y => (y.Value.Value.Item1 == p1 && y.Value.Value.Item2 == p2) || (y.Value.Value.Item1 == p2 && y.Value.Value.Item2 == p1));

    public static byte GetCupidId(byte lover) => cupidArrows.Where(x => x.Value != null && (x.Value.Value.Item1 == lover || x.Value.Value.Item2 == lover)).FirstOrDefault(defaultValue: new(0xff, null)).Key;

    public static void CheckAdditionalWin()
    {
        var loverWinners = CustomWinnerHolder.WinnerIds.Where(p => p.GetPlayer().Is(CustomRoles.Lovers));

        foreach (var lover in loverWinners)
        {
            var cupid = GetCupidId(lover);

            if (cupid != 0xff && !CustomWinnerHolder.WinnerIds.Contains(cupid))
            {
                CustomWinnerHolder.WinnerIds.Add(cupid);
                CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.Cupid);
            }
        }
    }

    public static bool IsPolycule(PlayerControl[] players)
    {
        if (players.Length != 3) return false;

        (PlayerControl cupid, PlayerControl p1, PlayerControl p2) poly = (players[0], players[1], players[2]);

        if (poly.cupid.Is(CustomRoles.Cupid))
        {

        }
        else if (poly.p1.Is(CustomRoles.Cupid))
        {
            poly = (poly.p1, poly.cupid, poly.p2);
        }
        else if (poly.p2.Is(CustomRoles.Cupid))
        {
            poly = (poly.p2, poly.p1, poly.cupid);
        }
        else return false;

        return cupidArrows.TryGetValue(poly.cupid.PlayerId, out var pair) && pair != null &&
            ((pair.Value.Item1 == poly.p1.PlayerId && pair.Value.Item2 == poly.p2.PlayerId)
            || (pair.Value.Item1 == poly.p2.PlayerId && pair.Value.Item2 == poly.p1.PlayerId));
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.ShapeshifterCooldown = CharmCooldown.GetFloat();
    }
    public override void SetAbilityButtonText(HudManager hud, byte id)
    {
        hud.KillButton.OverrideText(GetString("Cupid.Protect"));
        hud.AbilityButton.OverrideText(GetString("Cupid.Charm"));
    }
    public override Sprite GetAbilityButtonSprite(PlayerControl player, bool shapeshifting) => CustomButton.Get("RomanticProtect");
}