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
    private const int Id = 29600;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Mage);
    public override CustomRoles ThisRoleBase => CustomRoles.Shapeshifter;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmatePower;
    //==================================================================\\

    public enum Spell
    {
        Invincible,
        Dash,
        Disguise, 
        Crush,
        Grasp,
        Warp,
        Sweep,
    }
    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Mage);
    }

    private float cd => !SpellUsed ? 0.1f : CurrentSpell switch
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

    private bool SpellUsed;
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
            SpellUsed = true;
            _Player.SyncSettings();
            _Player.RpcResetAbilityCooldown();
            SpellUsed = false;

            Main.Instance.StopCoroutine(InvincibilityCoroutine);
            InvincibilityCoroutine = Main.Instance.StartCoroutine(EndInvincibility(GetTimeStamp(), this));

            static System.Collections.IEnumerator EndInvincibility(long Timestamp, Mage thiz)
            {
                while (Timestamp + 20 > GetTimeStamp())
                {
                    yield return null;
                }
                thiz.Isinvincible = false;
            }
        },
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
                while ((Timestamp + 30 > GetTimeStamp()) && !Main.MeetingIsStarted)
                {
                    yield return null;
                }
                thiz._Player.ResetPlayerOutfit();
            }
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
                guwienko = null;
                return;
            }

            SpellUsed = true;
            _Player.SyncSettings();
            _Player.RpcResetAbilityCooldown();
            SpellUsed = false;

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
            SpellUsed = true;
            _Player.SyncSettings();
            _Player.RpcResetAbilityCooldown();
            SpellUsed = false;

            if (!Players.Any())
            {
                _Player.Notify(GetString("MageTrySweepGhosts"));
                return;
            }
            Mana -= 40;

            foreach (var pc in Players)
            {
                var vent = ShipStatus.Instance.AllVents.RandomElement();
                pc.RpcTeleport(vent.transform.localPosition);
            }
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
    private int Lastmana = 0;
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
                Main.AllPlayerKillCooldown[killer.PlayerId] = cd;
                SpellUsed = true;
                _Player.SyncSettings();
                _Player.RpcResetAbilityCooldown();
                SpellUsed = false;
                killer.SetKillCooldown();

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
                Main.AllPlayerKillCooldown[killer.PlayerId] = cd;
                SpellUsed = true;
                _Player.SyncSettings();
                _Player.RpcResetAbilityCooldown();
                SpellUsed = false;
                killer.SetKillCooldown();

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
            string spelltext = ColorString(new(57, 46, 99, 255), string.Format($"{GetString("MageSpell")}", GetString($"Spell.{CurrentSpell}")));

            return $"{spelltext}\n" + GetCharge();
        }
        return "";
    }


    public override void OnFixedUpdate(PlayerControl pc)
    {
        Doubletrigger.FixedUpdate();

        if (Lastmana != Mana)
        {
            Lastmana = Mana;
            DoNotifyRoles(SpecifySeer: pc, SpecifyTarget: pc);
        }

        if (countnowF >= LastNowF && Mana < FullCharge)
        {
            LastNowF = countnowF + 1f;
            Mana++;
        }
        countnowF += Time.deltaTime;

        ChangeDir(pc);
    }

    private void ChangeDir(PlayerControl pc)
    {
        var pos = pc.GetCustomPosition();
        if (Vector2.Distance(pos, LastPosition) < 0.1f) return;

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


    public class DoubleShapeShift
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
