using AmongUs.GameOptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TOHE.Roles.Core;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate
{
    internal class Witness : RoleBase
    {
        private const int Id = 10100;
        private static bool On = false;
        public override bool IsEnable => On;
        public static bool HasEnabled => CustomRoles.Witness.IsClassEnable();
        public override CustomRoles ThisRoleBase => CustomRoles.Impostor;

        public static OptionItem WitnessCD;
        public static OptionItem WitnessTime;
        public static void SetupCustomOptions()
        {
            SetupSingleRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Witness, 1);
            WitnessCD = FloatOptionItem.Create(Id + 10, "AbilityCD", new(0f, 60f, 2.5f), 15f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Witness])
                .SetValueFormat(OptionFormat.Seconds);
            WitnessTime = IntegerOptionItem.Create(Id + 11, "WitnessTime", new(1, 30, 1), 10, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Witness])
                .SetValueFormat(OptionFormat.Seconds);
        }
        public override void Init()
        {
            On = false;
        }
        public override void Add(byte playerId)
        {
            On = true;
        }
        public override void SetAbilityButtonText(HudManager hud, byte id)
        {
            hud.KillButton.OverrideText(GetString("WitnessButtonText"));
        }
        public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
        {
            killer.SetKillCooldown();
            if (Main.AllKillers.ContainsKey(target.PlayerId))
                killer.Notify(GetString("WitnessFoundKiller"));
            else
                killer.Notify(GetString("WitnessFoundInnocent"));
            return false;
        }
        public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = Witness.WitnessCD.GetFloat();
        public override void ApplyGameOptions(IGameOptions opt, byte playerId) => opt.SetVision(false);
    }
}
