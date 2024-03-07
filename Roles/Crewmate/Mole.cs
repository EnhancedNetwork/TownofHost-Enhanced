using AmongUs.GameOptions;
using System.Collections.Generic;
using System.Linq;
using TOHE.Roles.Core;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate
{
    internal class Mole : RoleBase
    {
        //===========================SETUP================================\\
        private const int Id = 26000;
        public static bool On = false;
        public override bool IsEnable => On;
        public static bool HasEnabled => CustomRoles.Mole.IsClassEnable();
        public override CustomRoles ThisRoleBase => CustomRoles.Engineer;

        //==================================================================\\

        public static OptionItem VentCooldown;

        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Mole);
            VentCooldown = FloatOptionItem.Create(Id + 11, "MoleVentCooldown", new(5f, 180f, 1f), 20f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Mole])
                .SetValueFormat(OptionFormat.Seconds);
        }
        public override void Init()
        {
            //playerIdList = [];
            On = false;
        }
        public override void Add(byte playerId)
        {
            //playerIdList.Add(playerId);
            On = true;
        }

        public override void OnExitVent(PlayerControl pc, Vent vivent)
        {
            if (!pc.Is(CustomRoles.Mole)) return;

            _ = new LateTask(() =>
            {
                var vents = Object.FindObjectsOfType<Vent>().Where(x => x.Id != vivent.Id).ToArray();
                var rand = IRandom.Instance;
                var vent = vents[rand.Next(0, vents.Length)];

                Logger.Info($" {vent.transform.position}", "Mole vent teleport");
                pc.RpcTeleport(new Vector2(vent.transform.position.x, vent.transform.position.y + 0.3636f));
            }, 0.1f, "Mole On Exit Vent");
        }
        public override void SetAbilityButtonText(HudManager hud, byte playerId)
        {
            hud.AbilityButton.OverrideText(GetString("MoleVentButtonText"));
        }
        public override void ApplyGameOptions(IGameOptions opt, byte playerId)
        {
            AURoleOptions.EngineerCooldown = Mole.VentCooldown.GetFloat();
            AURoleOptions.EngineerInVentMaxTime = 1;
        }
    }
}
