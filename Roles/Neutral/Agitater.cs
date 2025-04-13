using AmongUs.GameOptions;
using Hazel;
using InnerNet;
using TOHE.Roles.Core;
using UnityEngine;
using static TOHE.Translator;

namespace TOHE.Roles.Neutral;
internal class Agitater : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Agitater;
    private const int Id = 15800;
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralKilling;
    //==================================================================\\

    private static OptionItem BombExplodeCooldown;
    private static OptionItem PassCooldown;
    private static OptionItem AgitaterCanGetBombed;
    private static OptionItem AgiTaterBombCooldown;
    private static OptionItem AgitaterAutoReportBait;
    private static OptionItem HasImpostorVision;

    public static byte CurrentBombedPlayer = byte.MaxValue;
    public static byte LastBombedPlayer = byte.MaxValue;
    public static bool AgitaterHasBombed = false;
    public static long? CurrentBombedPlayerTime = new();
    public static long? AgitaterBombedTime = new();


    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Agitater);
        AgiTaterBombCooldown = FloatOptionItem.Create(Id + 10, "AgitaterBombCooldown", new(10f, 180f, 2.5f), 20f, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Agitater])
            .SetValueFormat(OptionFormat.Seconds);
        PassCooldown = FloatOptionItem.Create(Id + 11, "AgitaterPassCooldown", new(0f, 5f, 0.25f), 1f, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Agitater])
            .SetValueFormat(OptionFormat.Seconds);
        BombExplodeCooldown = FloatOptionItem.Create(Id + 12, "BombExplodeCooldown", new(1f, 10f, 1f), 10f, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Agitater])
            .SetValueFormat(OptionFormat.Seconds);
        AgitaterCanGetBombed = BooleanOptionItem.Create(Id + 13, "AgitaterCanGetBombed", false, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Agitater]);
        AgitaterAutoReportBait = BooleanOptionItem.Create(Id + 14, "AgitaterAutoReportBait", false, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Agitater]);
        HasImpostorVision = BooleanOptionItem.Create(Id + 15, GeneralOption.ImpostorVision, true, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Agitater]);
    }
    public override void Init()
    {
        CurrentBombedPlayer = byte.MaxValue;
        LastBombedPlayer = byte.MaxValue;
        AgitaterHasBombed = false;
        CurrentBombedPlayerTime = new();
    }

    public override void Add(byte playerId)
    {
        CustomRoleManager.OnFixedUpdateOthers.Add(OnFixedUpdateOthers);
    }

    public static void ResetBomb()
    {
        CurrentBombedPlayer = 254;
        CurrentBombedPlayerTime = new();
        LastBombedPlayer = byte.MaxValue;
        AgitaterHasBombed = false;
    }
    public override void OnFixedUpdate(PlayerControl player, bool lowLoad, long nowTime, int timerLowLoad)
    {
        if (!lowLoad && CurrentBombedPlayer == 254)
        {
            SendRPC(CurrentBombedPlayer, LastBombedPlayer);
            CurrentBombedPlayer = byte.MaxValue;
        }
    }
    public override bool CanUseKillButton(PlayerControl pc) => true;
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = AgiTaterBombCooldown.GetFloat();
    public override void ApplyGameOptions(IGameOptions opt, byte id) => opt.SetVision(HasImpostorVision.GetBool());

    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (AgitaterAutoReportBait.GetBool() && target.Is(CustomRoles.Bait)) return true;
        if (target.Is(CustomRoles.Pestilence))
        {
            target.RpcMurderPlayer(killer);
            ResetBomb();
            return false;
        }

        CurrentBombedPlayer = target.PlayerId;
        LastBombedPlayer = killer.PlayerId;
        CurrentBombedPlayerTime = Utils.GetTimeStamp();
        killer.RpcGuardAndKill(killer);
        killer.Notify(GetString("AgitaterPassNotify"));
        target.Notify(GetString("AgitaterTargetNotify"));
        AgitaterHasBombed = true;
        killer.ResetKillCooldown();
        killer.SetKillCooldown();

        _ = new LateTask(() =>
        {
            if (CurrentBombedPlayer != byte.MaxValue && GameStates.IsInTask)
            {
                var pc = Utils.GetPlayerById(CurrentBombedPlayer);
                if (pc != null && pc.IsAlive() && killer != null && !pc.IsTransformedNeutralApocalypse())
                {
                    CurrentBombedPlayer.SetDeathReason(PlayerState.DeathReason.Bombed);
                    pc.RpcMurderPlayer(pc);
                    pc.SetRealKiller(killer);
                    Logger.Info($"{killer.GetNameWithRole()} bombed {pc.GetNameWithRole()} - bomb cd complete", "Agitater");
                    ResetBomb();
                }

            }
        }, BombExplodeCooldown.GetFloat(), "Agitater Bomb Kill");
        return false;
    }

    public override void OnReportDeadBody(PlayerControl reported, NetworkedPlayerInfo agitatergoatedrole)
    {
        if (CurrentBombedPlayer == byte.MaxValue) return;
        var target = Utils.GetPlayerById(CurrentBombedPlayer);
        var killer = _Player;
        if (target == null || killer == null) return;

        CurrentBombedPlayer.SetDeathReason(PlayerState.DeathReason.Bombed);
        Main.PlayerStates[CurrentBombedPlayer].SetDead();
        target.RpcExileV2();
        target.SetRealKiller(killer);
        MurderPlayerPatch.AfterPlayerDeathTasks(killer, target, true);
        ResetBomb();
        Logger.Info($"{killer.GetRealName()} bombed {target.GetRealName()} on report", "Agitater");
    }
    private void OnFixedUpdateOthers(PlayerControl player, bool lowLoad, long nowTime)
    {
        if (lowLoad || !AgitaterHasBombed || CurrentBombedPlayer != player.PlayerId) return;

        if (!player.IsAlive())
        {
            ResetBomb();
        }
        else
        {
            var playerPos = player.GetCustomPosition();
            Dictionary<byte, float> targetDistance = [];
            float dis;

            foreach (var target in Main.AllAlivePlayerControls)
            {
                if (target.PlayerId != player.PlayerId && target.PlayerId != LastBombedPlayer)
                {
                    dis = Utils.GetDistance(playerPos, target.transform.position);
                    targetDistance.Add(target.PlayerId, dis);
                }
            }

            if (targetDistance.Any())
            {
                var min = targetDistance.OrderBy(c => c.Value).FirstOrDefault();
                var target = min.Key.GetPlayer();
                var KillRange = ExtendedPlayerControl.GetKillDistances();
                if (min.Value <= KillRange && !player.inVent && !player.inMovingPlat && !target.inVent && !target.inMovingPlat && player.RpcCheckAndMurder(target, true))
                {
                    PassBomb(player, target);
                }
            }
        }
    }
    private void PassBomb(PlayerControl player, PlayerControl target)
    {
        if (!AgitaterHasBombed) return;
        if (!target.IsAlive()) return;

        var now = Utils.GetTimeStamp();
        if (now - CurrentBombedPlayerTime < PassCooldown.GetFloat()) return;
        if (target.PlayerId == LastBombedPlayer) return;
        if (!AgitaterCanGetBombed.GetBool() && target.Is(CustomRoles.Agitater)) return;


        if (target.Is(CustomRoles.Pestilence))
        {
            target.RpcMurderPlayer(player);
            ResetBomb();
            return;
        }
        LastBombedPlayer = CurrentBombedPlayer;
        CurrentBombedPlayer = target.PlayerId;
        CurrentBombedPlayerTime = now;
        Utils.MarkEveryoneDirtySettings();


        player.Notify(GetString("AgitaterPassNotify"));
        target.Notify(GetString("AgitaterTargetNotify"));

        SendRPC(CurrentBombedPlayer, LastBombedPlayer);
        Logger.Msg($"{player.GetNameWithRole()} passed bomb to {target.GetNameWithRole()}", "Agitater Pass");
    }

    public void SendRPC(byte newbomb, byte oldbomb)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, -1);
        writer.WriteNetObject(_Player);
        writer.Write(newbomb);
        writer.Write(oldbomb);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }

    public override void ReceiveRPC(MessageReader reader, PlayerControl NaN)
    {
        CurrentBombedPlayer = reader.ReadByte();
        LastBombedPlayer = reader.ReadByte();
    }
    public override void SetAbilityButtonText(HudManager hud, byte playerId)
        => hud.KillButton.OverrideText(GetString("AgitaterKillButtonText"));
    public override Sprite GetKillButtonSprite(PlayerControl player, bool shapeshifting) => CustomButton.Get("bombshell");
}
