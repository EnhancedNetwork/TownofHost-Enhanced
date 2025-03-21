using AmongUs.GameOptions;
using Hazel;
using InnerNet;
using System.Text;
using TOHE.Modules;
using TOHE.Roles.Core;
using TOHE.Roles.Crewmate;
using TOHE.Roles.Double;
using TOHE.Roles.Impostor;
using UnityEngine;
using static TOHE.Translator;
using static TOHE.Utils;

namespace TOHE.Roles.Neutral;

internal class Pelican : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Pelican;
    private const int Id = 17300;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Pelican);
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralKilling;
    //==================================================================\\

    private static OptionItem KillCooldown;
    private static OptionItem HasImpostorVision;
    private static OptionItem CanVent;

    private static readonly Dictionary<byte, HashSet<byte>> eatenList = [];
    private static readonly Dictionary<byte, float> originalSpeed = [];
    public static Dictionary<byte, Vector2> PelicanLastPosition = [];

    private static int Count = 0;

    public override void SetupCustomOption()
    {
        Options.SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Pelican, 1, zeroOne: false);
        KillCooldown = FloatOptionItem.Create(Id + 10, "PelicanKillCooldown", new(0f, 180f, 2.5f), 30f, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Pelican])
            .SetValueFormat(OptionFormat.Seconds);
        CanVent = BooleanOptionItem.Create(Id + 11, GeneralOption.CanVent, true, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Pelican]);
        HasImpostorVision = BooleanOptionItem.Create(Id + 12, GeneralOption.ImpostorVision, true, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Pelican]);
    }
    public override void Init()
    {
        eatenList.Clear();
        originalSpeed.Clear();
        PelicanLastPosition.Clear();

        Count = 0;
    }
    public override void Add(byte playerId)
    {
        eatenList[playerId] = [];
    }
    public override void Remove(byte playerId)
    {
        ReturnEatenPlayerBack(playerId.GetPlayer());
    }
    private void SyncEatenList()
    {
        foreach (var el in eatenList)
            SendRPC(el.Key);
    }
    private void SendRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, -1);
        writer.WriteNetObject(_Player); // SetPelicanEatenNum
        writer.Write(playerId);
        if (playerId != byte.MaxValue)
        {
            writer.Write(eatenList[playerId].Count);
            foreach (var el in eatenList[playerId])
                writer.Write(el);
        }
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public override void ReceiveRPC(MessageReader reader, PlayerControl NaN)
    {
        byte playerId = reader.ReadByte();

        eatenList[playerId].Clear();

        int eatenNum = reader.ReadInt32();
        HashSet<byte> list = [];
        for (int i = 0; i < eatenNum; i++)
            list.Add(reader.ReadByte());

        eatenList[playerId] = list;
    }
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
    public override bool CanUseKillButton(PlayerControl pc) => true;
    public override bool CanUseImpostorVentButton(PlayerControl pc) => CanVent.GetBool();

    private static bool IsEaten(PlayerControl pc, byte id) => eatenList.TryGetValue(pc.PlayerId, out var list) && list.Contains(id);
    public static bool IsEaten(byte id)
    {
        foreach (var el in eatenList)
            if (el.Value.Contains(id))
                return true;
        return false;
    }
    public static bool CanEat(PlayerControl pc, byte id)
    {
        if (!pc.Is(CustomRoles.Pelican) || GameStates.IsMeeting) return false;

        var target = id.GetPlayer();

        var penguins = GetRoleBasesByType<Penguin>()?.ToList();
        if (penguins != null)
        {
            if (penguins.Any(pg => target.PlayerId == pg.AbductVictim?.PlayerId))
            {
                return false;
            }
        }

        return target != null && target.CanBeTeleported() && !target.IsTransformedNeutralApocalypse() && !Medic.IsProtected(target.PlayerId) && !target.Is(CustomRoles.GM) && !IsEaten(pc, id);
    }
    public static Vector2 GetBlackRoomPSForPelican()
    {
        return GetActiveMapId() switch
        {
            0 => new Vector2(-27f, 3.3f), // The Skeld
            1 => new Vector2(-11.4f, 8.2f), // MIRA HQ
            2 => new Vector2(42.6f, -19.9f), // Polus
            3 => new Vector2(27f, 3.3f), // dlekS ehT
            4 => new Vector2(-16.8f, -6.2f), // Airship
            5 => new Vector2(9.6f, 23.2f), // The Fungle
            _ => throw new System.NotImplementedException(),
        };
    }
    public override void ApplyGameOptions(IGameOptions opt, byte id) => opt.SetVision(HasImpostorVision.GetBool());

    public override string GetProgressText(byte playerId, bool coooms)
    {
        var eatenNum = 0;
        var ProgressText = new StringBuilder();

        if (eatenList.TryGetValue(playerId, out var list))
            eatenNum = list.Count;

        ProgressText.Append(ColorString(eatenNum < 1 ? Color.gray : GetRoleColor(CustomRoles.Pelican), $"({eatenNum})"));
        return ProgressText.ToString();
    }
    private void EatPlayer(PlayerControl pc, PlayerControl target)
    {
        if (pc == null || target == null || !target.CanBeTeleported()) return;
        if (Mini.Age < 18 && (target.Is(CustomRoles.NiceMini) || target.Is(CustomRoles.EvilMini)))
        {
            pc.Notify(ColorString(GetRoleColor(CustomRoles.NiceMini), GetString("CantEat")));
            return;
        }

        eatenList[pc.PlayerId].Add(target.PlayerId);
        SyncEatenList();

        originalSpeed.Remove(target.PlayerId);
        originalSpeed.Add(target.PlayerId, Main.AllPlayerSpeed[target.PlayerId]);

        target.RpcTeleport(GetBlackRoomPSForPelican());
        Main.AllPlayerSpeed[target.PlayerId] = 0.5f;
        ReportDeadBodyPatch.CanReport[target.PlayerId] = false;
        target.MarkDirtySettings();

        NotifyRoles(SpecifySeer: pc);
        NotifyRoles(SpecifySeer: target);

        Logger.Info($"{pc.GetRealName()} eat player => {target.GetRealName()}", "Pelican");
    }

    public override void OnReportDeadBody(PlayerControl Nah_Id, NetworkedPlayerInfo win)
    {
        foreach (var pc in eatenList)
        {
            foreach (var tar in pc.Value)
            {
                var target = tar.GetPlayer();
                var killer = pc.Key.GetPlayer();
                if (killer == null || target == null) continue;
                Main.AllPlayerSpeed[tar] = Main.AllPlayerSpeed[tar] - 0.5f + originalSpeed[tar];
                ReportDeadBodyPatch.CanReport[tar] = true;
                if (target.IsTransformedNeutralApocalypse()) continue;
                target.RpcExileV2();
                target.SetRealKiller(killer);
                tar.SetDeathReason(PlayerState.DeathReason.Eaten);
                Main.PlayerStates[target.PlayerId].SetDead();
                MurderPlayerPatch.AfterPlayerDeathTasks(killer, target, true);
                Logger.Info($"{killer.GetRealName()} 消化了 {target.GetRealName()}", "Pelican");
            }
            eatenList[pc.Key].Clear();
        }
        SyncEatenList();
    }
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (CanEat(killer, target.PlayerId))
        {
            EatPlayer(killer, target);
            if (!Options.DisableShieldAnimations.GetBool()) killer.RpcGuardAndKill(killer);
            killer.SetKillCooldown();
            killer.RPCPlayCustomSound("Eat");
            target.RPCPlayCustomSound("Eat");
        }
        else
        {
            killer.SetKillCooldown();
            killer.Notify(GetString("Pelican.TargetCannotBeEaten"));
        }
        return false;
    }
    public override bool OnCheckMurderAsTarget(PlayerControl killer, PlayerControl target)
    {
        if (killer.Is(CustomRoles.Scavenger) || killer.Is(CustomRoles.Pelican))
        {
            PelicanLastPosition[target.PlayerId] = target.GetCustomPosition();
        }
        return true;
    }
    public override void OnMurderPlayerAsTarget(PlayerControl SLAT, PlayerControl pelican, bool inMeeting, bool isSuicide)
    {
        if (inMeeting) return;

        ReturnEatenPlayerBack(pelican);
    }
    private void ReturnEatenPlayerBack(PlayerControl pelican)
    {
        var pelicanId = pelican.PlayerId;
        if (!eatenList.TryGetValue(pelicanId, out var list)) return;

        GameEndCheckerForNormal.ShouldNotCheck = true;

        try
        {
            Vector2 teleportPosition;
            if (Scavenger.KilledPlayersId.Contains(pelicanId) && PelicanLastPosition.TryGetValue(pelicanId, out var lastPosition))
                teleportPosition = lastPosition;
            else
                teleportPosition = pelican.GetCustomPosition();

            foreach (var tar in list)
            {
                var target = tar.GetPlayer();
                var player = pelicanId.GetPlayer();
                if (player == null || target == null) continue;

                target.RpcTeleport(teleportPosition);

                Main.AllPlayerSpeed[tar] = Main.AllPlayerSpeed[tar] - 0.5f + originalSpeed[tar];
                ReportDeadBodyPatch.CanReport[tar] = true;

                target.SyncSettings();

                RPC.PlaySoundRPC(tar, Sounds.TaskComplete);
                Utils.NotifyRoles(SpecifySeer: target);

                Logger.Info($"{pelican?.Data?.PlayerName} dead, player return back: {target?.Data?.PlayerName} in {teleportPosition}", "Pelican");
            }
            eatenList[pelicanId].Clear();
            SyncEatenList();
        }
        catch (System.Exception error)
        {
            ThrowException(error);
        }

        GameEndCheckerForNormal.ShouldNotCheck = false;
    }

    public override void OnFixedUpdate(PlayerControl player, bool lowLoad, long nowTime, int timerLowLoad)
    {
        if (lowLoad) return;

        Count--;

        if (Count > 0) return;

        Count = 4;

        if (eatenList.TryGetValue(player.PlayerId, out var playerList))
        {
            foreach (var tar in playerList.ToArray())
            {
                var target = tar.GetPlayer();
                if (!target.IsAlive()) continue;

                var pos = GetBlackRoomPSForPelican();
                var dis = GetDistance(pos, target.GetCustomPosition());
                if (dis < 1f) continue;

                target.RpcTeleport(pos, sendInfoInLogs: false);
                //NotifyRoles(SpecifySeer: target, ForceLoop: false);
            }
        }
    }
    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.KillButton.OverrideText(GetString("PelicanButtonText"));
    }
    public override Sprite GetKillButtonSprite(PlayerControl player, bool shapeshifting) => CustomButton.Get("Vulture");
}
