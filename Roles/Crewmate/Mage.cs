using UnityEngine;
using static TOHE.Options;
using static TOHE.Utils;
using static TOHE.Translator;
using TOHE.Roles.Core;
using AmongUs.GameOptions;
using System;
using System.Text;
using Rewired.ControllerExtensions;
using TOHE.Modules;

namespace TOHE.Roles.Crewmate;

internal class Mage : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 29520;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Mage);
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Shapeshifter;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmatePower;
    //==================================================================\\

    public enum Spell
    {
        Invincible,
        Disguise,
        Dash,
        Crush,
        Grasp,
        Warp,
        Sweep,
    }
    public static OptionItem InvincibilityDur;
    public static OptionItem DisguiseDur;
    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Mage);
        InvincibilityDur = IntegerOptionItem.Create(Id + 10, "MageInvincibilitydur", new(1, 80, 1), 20, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Mage])
            .SetValueFormat(OptionFormat.Seconds);
        DisguiseDur = IntegerOptionItem.Create(Id + 11, "MageDisguisedur", new(1, 80, 1), 30, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Mage])
            .SetValueFormat(OptionFormat.Seconds); 
    }

    private float cd => CurrentSpell switch
    {
        Spell.Warp => 20f,
        Spell.Grasp or Spell.Invincible => 10f,
        Spell.Crush => 40f,
        Spell.Sweep => 40f,
        _ => 0.1f,
    };
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.ShapeshifterCooldown = cd;
    }

    private Spell CurrentSpell;
    private Direction direction;
    private readonly List<byte> GraspedPlayers = [];

    //
    Coroutine InvincibilityCoroutine;
    bool Isinvincible;
    Vector2? guwienko; // temporary to test, cuz I weant this to be a CNO
    Coroutine DisguiseCoroutine;

    //
    private Action Spellaction => CurrentSpell switch
    {
        Spell.Invincible => () => {
            if (Mana < 30)
            {
                _Player.Notify(string.Format(GetString("MageNotEnoughMana"), 30));
                return;
            }
            Isinvincible = true;
            Mana -= 30;
            _Player.SyncSettings();
            _Player.RpcResetAbilityCooldown();
            _Player.RpcGuardAndKill();

            Main.Instance.StopCoroutine(InvincibilityCoroutine);
            InvincibilityCoroutine = Main.Instance.StartCoroutine(EndInvincibility(GetTimeStamp(), this));

            static System.Collections.IEnumerator EndInvincibility(long Timestamp, Mage thiz)
            {
                while (Timestamp + InvincibilityDur.GetInt() > GetTimeStamp())
                {
                    yield return null;
                }
                thiz.Isinvincible = false;
            }
        },
        Spell.Disguise => () =>
        {
            if (Main.AllAlivePlayerControls.Length < 2)
            {
                return;
            }
            if (Mana < 10)
            {
                _Player.Notify(string.Format(GetString("MageNotEnoughMana"), 10));
                return;
            }
            Mana -= 10;

            var RandPC = Main.AllAlivePlayerControls.Where(x => x != _Player).ToArray().RandomElement();

            _Player.ResetPlayerOutfit(Main.PlayerStates[RandPC.PlayerId].NormalOutfit);

            Main.Instance.StopCoroutine(DisguiseCoroutine);
            DisguiseCoroutine = Main.Instance.StartCoroutine(EndDisguise(GetTimeStamp(), this));

            static System.Collections.IEnumerator EndDisguise(long Timestamp, Mage thiz)
            {
                while ((Timestamp + DisguiseDur.GetInt() + 1 > GetTimeStamp()) && !Main.MeetingIsStarted)
                {
                    if (Timestamp + DisguiseDur.GetInt() - GetTimeStamp() == 5)
                    {
                        Timestamp--;
                        thiz._Player.Notify(GetString("MageAboutRunOut"));
                    }
                    yield return null;
                }
                thiz._Player.ResetPlayerOutfit();
            }
        }
        ,
        Spell.Dash => () => {
            
            if (Mana < 5)
            {
                _Player.Notify(string.Format(GetString("MageNotEnoughMana"), 5));
                return;
            }
            Mana -= 5;

            var addVector = direction switch
            {
                Direction.Left => new(-2f, 0),
                Direction.UpLeft => new(-2f, 2f),
                Direction.Up => new(0, 2f),
                Direction.UpRight => new(2f, 2f),
                Direction.Right => new(2f, 0),
                Direction.DownRight => new(2f, -2f),
                Direction.Down => new(0, -2f),
                Direction.DownLeft => new(-2f, -2f),
                _ => Vector2.zero
            };

            _Player.RpcTeleport(_Player.GetCustomPosition() + addVector);

        },
        Spell.Warp => () => {
            if (Mana < 30 && guwienko == null)
            {
                _Player.Notify(string.Format(GetString("MageNotEnoughMana"), 30));
                return;
            }

            if (guwienko != null)
            {
                _Player.RpcTeleport(guwienko.Value);
                _Player.RpcGuardAndKill();
                guwienko = null;
                return;
            }

            _Player.SyncSettings();
            _Player.RpcResetAbilityCooldown();

            Mana -= 30;
            guwienko = _Player.GetCustomPosition();

        },
        Spell.Sweep => () =>
        {
            if (Mana < 40)
            {
                _Player.Notify(string.Format(GetString("MageNotEnoughMana"), 40));
                return;
            }
            var Players = Main.AllAlivePlayerControls.Without(_Player).Where(x => Utils.GetDistance(_Player.GetCustomPosition(), x.GetCustomPosition()) < 2);

            if (!Players.Any())
            {
                _Player.Notify(GetString("MageTrySweepGhosts"));
                return;
            }
            _Player.SyncSettings();
            _Player.RpcResetAbilityCooldown();
            Mana -= 40;

            RandomSpawn.SpawnMap map = Utils.GetActiveMapId() switch
            {
                0 => new RandomSpawn.SkeldSpawnMap(),
                1 => new RandomSpawn.MiraHQSpawnMap(),
                2 => new RandomSpawn.PolusSpawnMap(),
                3 => new RandomSpawn.DleksSpawnMap(),
                5 => new RandomSpawn.FungleSpawnMap(),
                _ => null,
            };
            if (map != null) Players.Do(map.RandomTeleport);
        },
        _ => () => { }
    };
    private Action SwitchSpell => () => {
        if (Enum.GetValues<Spell>().Length - 1 >= (int)CurrentSpell + 1)
        {
            CurrentSpell++;
        }
        else
        {
            CurrentSpell = Spell.Invincible;
        }
        Utils.NotifyRoles(SpecifySeer: _Player, SpecifyTarget: _Player);
    };

    Vector2 LastPosition = Vector2.zeroVector;
    private int Mana = 0;
    private const int FullCharge = 100;
    private static int Charges => (int)Math.Round(FullCharge / 10.0);

    private float LastNowF = 0;
    private float countnowF = 0;

    private readonly DoubleShapeShift Doubletrigger = new();
    public override string GetMark(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
    {
        return seer == seen && Isinvincible ? ColorString(new(89, 85, 125, 255), "❖") : string.Empty;
    }
    public override string GetSuffix(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
    {
        return GraspedPlayers.Contains(seen.PlayerId) ? ColorString(new(90, 145, 142, 255), "⊙") : string.Empty;
    }
    public override void UnShapeShiftButton(PlayerControl shapeshifter)
    {
        _Player.SyncSettings();
        _Player.RpcResetAbilityCooldown();
        Doubletrigger.CheckDoubleTrigger(Spellaction, SwitchSpell);
    }

    public override bool OnCheckMurderAsTarget(PlayerControl killer, PlayerControl target) => !Isinvincible;
    
    private int GetChargeToColor()
    {
        int percent = (int)Math.Round(((double)Mana / FullCharge) * 100);
        int ToColor = (Charges * percent) / 100;

        return ToColor;
    }
    private string GetCharge()
    {
        var sb = new StringBuilder(Utils.ColorString(new(127, 22, 166, 255) , $"{Mana}/{FullCharge} "));
        var ChargeToColor = GetChargeToColor();
        var CHcol = "#7f16a6";

        sb.Append($"<size=75%>");
        for (int i = 0; i < Charges; i++)
        {
            string box = ChargeToColor > 0 ? $"<{CHcol}>█ </color>" : "<#666666>█ </color>";
            ChargeToColor--;
            sb.Append(box);
        }
        sb.Append($"</size>");

        return sb.ToString();
    }
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = cd;
    public override bool CanUseKillButton(PlayerControl pc) => true;
    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {

        switch (CurrentSpell)
        {
            case Spell.Crush:
                if (Mana < 70)
                {
                    _Player.Notify(string.Format(GetString("MageNotEnoughMana"), 70));
                    break;
                }
                _Player.SyncSettings();
                _Player.SetKillCooldown(cd);
                _Player.RpcResetAbilityCooldown();

                Mana -= 70;
                return true;

            case Spell.Grasp:
                if (GraspedPlayers.Contains(target.PlayerId))
                    break;

                if (Mana < 50)
                {
                    _Player.Notify(string.Format(GetString("MageNotEnoughMana"), 50));
                    break;
                }
                _Player.SyncSettings();
                _Player.SetKillCooldown(cd);
                _Player.RpcResetAbilityCooldown();

                Mana -= 50;
                GraspedPlayers.Add(target.PlayerId);
                break;

        }
        return false;
    }
    public override bool CheckMurderOnOthersTarget(PlayerControl killer, PlayerControl target)
    {
        if (GraspedPlayers.Remove(target.PlayerId))
        {
            Main.PlayerStates[killer.PlayerId].IsBlackOut = true;
            killer.MarkDirtySettings();
            _ = new LateTask(() => {
                Main.PlayerStates[killer.PlayerId].IsBlackOut = false;
                killer.MarkDirtySettings();

            }, 2f);

            target.RpcTeleport(_Player.GetCustomPosition());
            return true;
        }
        return base.CheckMurderOnOthersTarget(killer, target);
    }
    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        bool ismeeting = GameStates.IsMeeting || isForMeeting;
        if (seer == seen && !ismeeting)
        {
            if (!isForHud && seer.IsModClient())
                return string.Empty;
            string spelltext = ColorString(GetRoleColor(CustomRoles.Mage), string.Format($"{GetString("MageSpell")}", GetString($"Spell.{CurrentSpell}")));

            return $"{spelltext}\n" + GetCharge();
        }
        return string.Empty;
    }


    public override void OnFixedUpdate(PlayerControl pc)
    {
        Doubletrigger.FixedUpdate();

        if (countnowF >= LastNowF && Mana < FullCharge)
        {
            LastNowF = countnowF + 1f;
            Mana++;
            DoNotifyRoles(SpecifySeer: pc, SpecifyTarget: pc);
        }
        countnowF += Time.deltaTime;

        ChangeDir(pc);
    }

    private void ChangeDir(PlayerControl pc)
    {
        var pos = pc.GetCustomPosition();
        if (GetDistance(pos, LastPosition) < 0.1f) return;

        direction = pos.x < LastPosition.x
            ? pos.y < LastPosition.y
                ? Direction.DownLeft
                : pos.y > LastPosition.y
                    ? Direction.UpLeft
                    : Direction.Left
            : pos.x > LastPosition.x
                ? pos.y < LastPosition.y
                    ? Direction.DownRight
                    : pos.y > LastPosition.y
                        ? Direction.UpRight
                        : Direction.Right
                : pos.y < LastPosition.y
                    ? Direction.Down
                    : pos.y > LastPosition.y
                        ? Direction.Up
                        : Direction.Left;

        LastPosition = pos;
    }

    enum Direction
    {
        Left,
        UpLeft,
        Up,
        UpRight,
        Right,
        DownRight,
        Down,
        DownLeft
    }


    private class DoubleShapeShift
    {
        public float TimeSpan = 0;
        public float count = 0;
        public Action Firstaction;
        public void CheckDoubleTrigger(System.Action firstaction, System.Action secondAction)
        {
            if (Firstaction != null)
            {
                Firstaction = null;
                secondAction.Invoke();
            }
            else
            {
                TimeSpan = count + 1.4f;
                Firstaction = firstaction;
            }
        }
        public void FixedUpdate()
        {
            count += Time.deltaTime;
            if (Firstaction == null) return;

            if (count >= TimeSpan)
            {
                Firstaction.Invoke();
                Firstaction = null;
            }
        }

    }
}
