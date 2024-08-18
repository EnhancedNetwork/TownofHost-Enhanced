using UnityEngine;
using static TOHE.Options;
using static TOHE.Utils;
using static TOHE.Translator;
using TOHE.Roles.Core;
using AmongUs.GameOptions;
using System;
using System.Text;

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


    //
    bool Isinvincible;

    //
    private Action Spellaction => CurrentSpell switch
    {
        Spell.Invincible => () => {
            if (Mana < 30)
            {
                _Player.Notify(string.Format(GetString("MageNotEnoughMana"), 30));
            }
            Isinvincible = true;

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



    };
    Vector2 LastPosition = Vector2.zeroVector;
    private int Mana = 0;
    private const int FullCharge = 100;
    private int Charges => (int)Math.Round(FullCharge / 10.0);

    private float LastNowF = 0;
    private float countnowF = 0;

    private readonly DoubleShapeShift Doubletrigger = new();


    public override void UnShapeShiftButton(PlayerControl shapeshifter)
    {
        base.UnShapeShiftButton(shapeshifter);
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

        if (countnowF >= LastNowF && Mana < FullCharge)
        {
            Mana++;
        }
        countnowF += Time.deltaTime;
    }

    private void ChangeDir(PlayerControl pc)
    {
        var pos = pc.GetCustomPosition();
        if (Vector2.Distance(pos, LastPosition) < 0.1f) return;

        var direction = pos.x < LastPosition.x
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
