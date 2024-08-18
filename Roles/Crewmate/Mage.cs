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

    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.ShapeshifterCooldown = 0f;
    }


    private Spell CurrentSpell;
    private Direction direction;
    private readonly List<byte> GraspedPlayers = [];

    //
    bool Isinvincible;
    Vector2? guwienko;

    //
    private Action Spellaction => CurrentSpell switch
    {
        Spell.Invincible => () => {
            if (Mana < 30)
            {
                _Player.Notify(string.Format(GetString("MageNotEnoughMana"), 30));
            }
            Isinvincible = true;
            Mana -= 30;

            Main.Instance.StartCoroutine(EndInvincibility(GetTimeStamp(), this));

            static System.Collections.IEnumerator EndInvincibility(long Timestamp, Mage thiz)
            {

                while (Timestamp + 30 > GetTimeStamp())
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

            Main.Instance.StartCoroutine(EndDisguise(GetTimeStamp(), this));

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
            var Players = Main.AllAlivePlayerControls.Where(x => Utils.GetDistance(_Player.GetCustomPosition(), x.GetCustomPosition()) < 2);

            if (Players.Any())
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

        if (Enum.GetValues<Spell>().Length >= (int)CurrentSpell + 1)
        {
            CurrentSpell = CurrentSpell + 1;
        }
        else
        {
            CurrentSpell = default;
        }
        Utils.NotifyRoles(SpecifySeer: _Player, SpecifyTarget: _Player);
    };

    Vector2 LastPosition = Vector2.zeroVector;
    private int Lastmana = 0;
    private int Mana = 0;
    private const int FullCharge = 100;
    private int Charges => (int)Math.Round(FullCharge / 10.0);

    private float LastNowF = 0;
    private float countnowF = 0;

    private readonly DoubleShapeShift Doubletrigger = new();


    public override void UnShapeShiftButton(PlayerControl shapeshifter)
    {
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
                Mana -= 70;
                return true;

            case Spell.Grasp:
                if (GraspedPlayers.Contains(target.PlayerId))
                    break;

                if (Mana < 50)
                {
                    _Player.Notify(string.Format(GetString("MageNotEnoughMana"), 70));
                    break;
                }
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

            return GetCharge();
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
        public long TimeSpan = 0;
        public Action CurrentAction;


        public void CheckDoubleTrigger(System.Action firstaction, System.Action secondAction)
        {
            if (CurrentAction != null)
            {
                CurrentAction = null;
                secondAction.Invoke();
            }
            else
            {
                TimeSpan = Utils.GetTimeStamp() + 1;
                CurrentAction = firstaction;
            }
        }
        public void FixedUpdate()
        {
            if (CurrentAction == null) return;

            if (TimeSpan >= Utils.GetTimeStamp())
            {
                CurrentAction.Invoke();
                CurrentAction = null;
            }
        }

    }
}
