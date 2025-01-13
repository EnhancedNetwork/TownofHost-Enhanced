using AmongUs.GameOptions;
using static TOHE.Options;
using static TOHE.Translator;
using static UnityEngine.GraphicsBuffer;

namespace TOHE.Roles.Impostor;

internal class Diviner : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 31300;
    private static readonly HashSet<byte> playerIdList = [];
    public static bool HasEnabled => playerIdList.Any();

    public override CustomRoles Role => CustomRoles.Diviner;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorKilling;
    //==================================================================\\

    private static OptionItem KillCooldown;
    private static OptionItem RevengeTime;
    private static OptionItem CanSabotage;
    private static OptionItem CanVent;
    private static OptionItem CanKill;

    public static PlayerControl Killer = null;
    private static bool IsRevenge = false;
    private static int Timer = 0;
    private static bool Success = false;
    private static float tempKillTimer = 0;

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Diviner);
        KillCooldown = FloatOptionItem.Create(Id + 10, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 20f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Diviner])
            .SetValueFormat(OptionFormat.Seconds);
        RevengeTime = IntegerOptionItem.Create(Id + 11, "DivinerRevengeTime", new(0, 60, 1), 30, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Diviner])
            .SetValueFormat(OptionFormat.Seconds);
        CanSabotage = BooleanOptionItem.Create(Id + 15, GeneralOption.CanUseSabotage, true, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Diviner]);
        CanVent = BooleanOptionItem.Create(Id + 12, GeneralOption.CanVent, true, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Diviner]);
        CanKill = BooleanOptionItem.Create(Id + 13, GeneralOption.CanKill, false, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Diviner]);
    }
    public override void Init()
    {
        playerIdList.Clear();
        IsRevenge = false;
        Success = false;
        Killer = null;
        tempKillTimer = 0;
    }
    public override void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        Timer = RevengeTime.GetInt();

        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
    public override bool CanUseKillButton(PlayerControl pc) => IsRevenge | CanKill.GetBool() && pc.IsAlive();
    public override bool CanUseImpostorVentButton(PlayerControl pc) => CanVent.GetBool();
    public override bool CanUseSabotage(PlayerControl pc) => CanSabotage.GetBool();


    public override bool OnCheckMurderAsTarget(PlayerControl killer, PlayerControl target)
    {
        if (killer.PlayerId == target.PlayerId) return true;
        if (killer == null) return false;
        if (target == null) return false;
        if (IsRevenge) return true;

        _ = new LateTask(() => { target.RpcRandomVentTeleport(); }, 0.01f, "Random Vent Teleport - Diviner");

        Timer = RevengeTime.GetInt();
        Countdown(Timer, target);
        IsRevenge = true;
        killer.SetKillCooldown();
        killer.Notify(GetString("DivinerHide"), RevengeTime.GetFloat());
        tempKillTimer = target.killTimer;
        target.SetKillCooldown(time: 1f);
        Killer = killer;

        return false;
    }
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (killer == null || target == null || !killer.IsAlive()) return false;

        if (!IsRevenge) return true;
        else if (target == Killer)
        {
            Success = true;
            killer.Notify(GetString("NecromancerSuccess"));
            killer.SetKillCooldown(KillCooldown.GetFloat() + tempKillTimer);
            IsRevenge = false;
            return true;
        }
        else
        {
            killer.RpcMurderPlayer(killer);
            return false;
        }
    }
    private static void Countdown(int seconds, PlayerControl player)
    {
        var killer = Killer;
        if (Success || !player.IsAlive())
        {
            Timer = RevengeTime.GetInt();
            Success = false;
            Killer = null;
            return;
        }
        if (GameStates.IsMeeting && player.IsAlive())
        {
            player.SetDeathReason(PlayerState.DeathReason.Kill);
            player.RpcExileV2();
            player.Data.IsDead = true;
            player.Data.MarkDirty();
            Main.PlayerStates[player.PlayerId].SetDead();
            player.SetRealKiller(killer);
            Killer = null;
            return;
        }
        if (seconds <= 0)
        {
            player.RpcMurderPlayer(player);
            player.SetRealKiller(killer);
            Killer = null;
            return;
        }
        player.Notify(string.Format(GetString("DivinerRevenge"), seconds, Killer.GetRealName()), 1.1f);
        Timer = seconds;

        _ = new LateTask(() => { Countdown(seconds - 1, player); }, 1.01f, "Diviner Countdown");
    }
}
