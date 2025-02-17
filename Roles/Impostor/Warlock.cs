using AmongUs.GameOptions;
using TOHE.Modules;
using TOHE.Roles.Crewmate;
using TOHE.Roles.Double;
using TOHE.Roles.Neutral;
using UnityEngine;

namespace TOHE.Roles.Impostor;

internal class Warlock : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Warlock;
    private const int Id = 5100;
    public override CustomRoles ThisRoleBase => CustomRoles.Shapeshifter;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.ImpostorConcealing;
    //==================================================================\\

    private static OptionItem WarlockCanKillAllies;
    private static OptionItem WarlockCanKillSelf;
    private static OptionItem WarlockShiftDuration;

    private static readonly Dictionary<byte, float> WarlockTimer = [];
    private static readonly Dictionary<byte, PlayerControl> CursedPlayers = [];
    private static readonly Dictionary<byte, bool> IsCurseAndKill = [];

    private static bool IsCursed = false;

    public override void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Warlock);
        WarlockCanKillAllies = BooleanOptionItem.Create(Id + 2, GeneralOption.CanKillImpostors, true, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Warlock]);
        WarlockCanKillSelf = BooleanOptionItem.Create(Id + 3, "Warlock_CanKillSelf", false, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Warlock]);
        WarlockShiftDuration = FloatOptionItem.Create(Id + 4, GeneralOption.ShapeshifterBase_ShapeshiftDuration, new(1, 180, 1), 1, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Warlock])
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void Init()
    {

        CursedPlayers.Clear();
        IsCurseAndKill.Clear();
        WarlockTimer.Clear();
        IsCursed = false;
    }
    public override void Add(byte playerId)
    {

        CursedPlayers.Add(playerId, null);
        IsCurseAndKill.Add(playerId, false);
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.ShapeshifterCooldown = IsCursed ? 1f : Options.DefaultKillCooldown;
        AURoleOptions.ShapeshifterDuration = WarlockShiftDuration.GetFloat();
    }

    public static bool CursedIsActive(PlayerControl player) => CursedPlayers.ContainsValue(player);

    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (!Main.CheckShapeshift[killer.PlayerId] && !IsCurseAndKill[killer.PlayerId])
        {
            if (target.Is(CustomRoles.LazyGuy) || target.Is(CustomRoles.Lazy) || target.Is(CustomRoles.NiceMini) && Mini.Age < 18) return false;

            IsCursed = true;
            killer.SetKillCooldown();
            killer.RPCPlayCustomSound("Line");
            CursedPlayers[killer.PlayerId] = target;
            WarlockTimer.Add(killer.PlayerId, 0f);
            IsCurseAndKill[killer.PlayerId] = true;
            return false;
        }

        if (Main.CheckShapeshift[killer.PlayerId])
        {
            killer.RpcCheckAndMurder(target);
            return false;
        }

        if (IsCurseAndKill[killer.PlayerId])
            killer.RpcGuardAndKill(target);

        return false;
    }

    public override void OnShapeshift(PlayerControl shapeshifter, PlayerControl target, bool IsAnimate, bool shapeshifting)
    {
        if (CursedPlayers[shapeshifter.PlayerId] != null)
        {
            if (shapeshifting && CursedPlayers[shapeshifter.PlayerId].IsAlive())
            {
                var cp = CursedPlayers[shapeshifter.PlayerId];
                Vector2 cppos = cp.transform.position;
                Dictionary<PlayerControl, float> cpdistance = [];
                float dis;

                foreach (PlayerControl p in Main.AllAlivePlayerControls)
                {
                    if (p.PlayerId == cp.PlayerId) continue;
                    if (!WarlockCanKillSelf.GetBool() && p.PlayerId == shapeshifter.PlayerId) continue;
                    if (!WarlockCanKillAllies.GetBool() && p.Is(Custom_Team.Impostor)) continue;
                    if (Pelican.IsEaten(p.PlayerId) || Medic.IsProtected(p.PlayerId)) continue;
                    if (p.Is(CustomRoles.Glitch) || p.Is(CustomRoles.Pestilence)) continue;

                    dis = Utils.GetDistance(cppos, p.transform.position);
                    cpdistance.Add(p, dis);
                    Logger.Info($"{p?.Data?.PlayerName} distance: {dis}", "Warlock");
                }
                if (cpdistance.Count >= 1)
                {
                    var min = cpdistance.OrderBy(c => c.Value).FirstOrDefault();
                    PlayerControl targetw = min.Key;
                    if (cp.RpcCheckAndMurder(targetw, true))
                    {
                        cp.RpcMurderPlayer(targetw);
                        targetw.SetRealKiller(shapeshifter);
                        shapeshifter.RpcGuardAndKill(shapeshifter);
                        Logger.Info($"{targetw.GetNameWithRole()} was killed", "Warlock");
                        shapeshifter.Notify(Translator.GetString("WarlockControlKill"));
                    }
                }
                else
                {
                    shapeshifter.Notify(Translator.GetString("WarlockNoTarget"));
                }
                IsCurseAndKill[shapeshifter.PlayerId] = false;
            }
            else if (!shapeshifting)
            {
                shapeshifter.Notify(Translator.GetString("WarlockTargetDead"));
            }
            CursedPlayers[shapeshifter.PlayerId] = null;
        }
        else if (!GameStates.IsMeeting)
        {
            shapeshifter.Notify(Translator.GetString("WarlockNoTargetYet"));
        }
    }

    public override void OnFixedUpdate(PlayerControl player, bool lowLoad, long nowTime, int timerLowLoad)
    {
        if (WarlockTimer.TryGetValue(player.PlayerId, out var warlockTimer))
        {
            var playerId = player.PlayerId;
            if (player.IsAlive())
            {
                if (warlockTimer >= 1f)
                {
                    player.SyncSettings();
                    player.RpcResetAbilityCooldown();
                    IsCursed = false;
                    WarlockTimer.Remove(playerId);
                }
                else
                {
                    warlockTimer += Time.fixedDeltaTime;
                    WarlockTimer[playerId] = warlockTimer;
                }
            }
            else
            {
                WarlockTimer.Remove(playerId);
            }
        }
    }

    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        foreach (var warlockId in _playerIdList)
        {
            CursedPlayers[warlockId] = null;
            IsCurseAndKill[warlockId] = false;
        }
        IsCursed = false;
    }

    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        bool shapeshifting = Main.CheckShapeshift.TryGetValue(playerId, out bool ss) && ss;
        bool curse = IsCurseAndKill.TryGetValue(playerId, out bool wcs) && wcs;

        if (!shapeshifting && !curse)
        {
            hud.KillButton?.OverrideText(Translator.GetString("WarlockCurseButtonText"));
            hud.AbilityButton?.OverrideText(Translator.GetString("WarlockShapeshiftButtonText"));
        }
        else
            hud.KillButton?.OverrideText(Translator.GetString("KillButtonText"));
    }

    public override Sprite GetKillButtonSprite(PlayerControl player, bool shapeshifting) => !shapeshifting ? CustomButton.Get("Curse") : null;
    public override Sprite GetAbilityButtonSprite(PlayerControl player, bool shapeshifting) => !shapeshifting && IsCurseAndKill.TryGetValue(player.PlayerId, out bool curse) && curse ? CustomButton.Get("CurseKill") : null;
}
