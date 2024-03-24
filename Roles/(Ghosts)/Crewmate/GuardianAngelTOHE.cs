using AmongUs.GameOptions;
using System;
using System.Collections.Generic;
using System.Linq;
using TOHE.Roles.Core;
using static TOHE.Options;

namespace TOHE.Roles._Ghosts_.Crewmate
{
    internal class GuardianAngelTOHE : RoleBase
    {
        //===========================SETUP================================\\
        private const int Id = 20900;
        private static readonly HashSet<byte> PlayerIds = [];
        public static bool HasEnabled => PlayerIds.Any();
        public override bool IsEnable => HasEnabled;
        public override CustomRoles ThisRoleBase => CustomRoles.GuardianAngel;
        //==================================================================\\

        private static OptionItem AbilityCooldown;
        private static OptionItem ProtectDur;
        private static OptionItem ImpVis;
        public static readonly Dictionary<byte, long> PlayerShield = [];
        public static void SetupCustomOptions()
        {
            SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.GuardianAngelTOHE);
            AbilityCooldown = FloatOptionItem.Create(Id + 10, "ProtectCooldown", new(2.5f, 120f, 2.5f), 35f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.GuardianAngelTOHE])
                .SetValueFormat(OptionFormat.Seconds);
            ProtectDur = IntegerOptionItem.Create(Id + 11, "ProtectDur", new(1, 120, 1), 10, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.GuardianAngelTOHE])
                .SetValueFormat(OptionFormat.Seconds);
            ImpVis = BooleanOptionItem.Create(Id + 12, "ProtectVisToImp", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.GuardianAngelTOHE]);
        }
        public override void Init()
        {
            PlayerShield.Clear();
            PlayerIds.Clear();
        }
        public override void Add(byte playerId)
        {
            CustomRoleManager.CheckDeadBodyOthers.Add(OthersAfterPlayerDeathTask);
            CustomRoleManager.OnFixedUpdateOthers.Add(OnOthersFixUpdate);
            PlayerIds.Add(playerId);
        }
        public override void ApplyGameOptions(IGameOptions opt, byte playerId)
        {
            AURoleOptions.GuardianAngelCooldown = AbilityCooldown.GetFloat();
            AURoleOptions.ProtectionDurationSeconds = ProtectDur.GetFloat();
            AURoleOptions.ImpostorsCanSeeProtect = ImpVis.GetBool();
        }
        public override bool OnCheckProtect(PlayerControl angel, PlayerControl target)
        {
            if (!PlayerShield.ContainsKey(target.PlayerId))
            {
                PlayerShield.Add(target.PlayerId, Utils.GetTimeStamp());
            }
            else
            {
                PlayerShield[target.PlayerId] = Utils.GetTimeStamp();
            }
            return true;
        }
        private static void OthersAfterPlayerDeathTask(PlayerControl killer, PlayerControl target, bool inMeeting)
        {
            if (PlayerShield.ContainsKey(target.PlayerId))
                PlayerShield.Remove(target.PlayerId);
        }
        public override bool CheckMurderOnOthersTarget(PlayerControl killer, PlayerControl target)
            => !PlayerShield.ContainsKey(target.PlayerId);
        
        private static void OnOthersFixUpdate(PlayerControl player)
        {
            if (PlayerShield.ContainsKey(player.PlayerId) && PlayerShield[player.PlayerId] + ProtectDur.GetInt() <= Utils.GetTimeStamp())
            {
                PlayerShield.Remove(player.PlayerId);
            }
        }
    }
}
