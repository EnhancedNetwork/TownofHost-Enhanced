using AmongUs.GameOptions;
using Hazel;
using System;
using System.Collections.Generic;
using System.Linq;
using TOHE.Roles.Core;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;
using UnityEngine;

namespace TOHE.Roles._Ghosts_.Crewmate
{
    internal class Ghastly : RoleBase
    {

        //===========================SETUP================================\\
        private const int Id = 22004;
        private readonly static HashSet<byte> PlayerIds = [];
        public static bool HasEnabled => PlayerIds.Any();
        public override bool IsEnable => HasEnabled;
        public override CustomRoles ThisRoleBase => CustomRoles.GuardianAngel;
        //==================================================================\\

        private static OptionItem PossessCooldown;
        private static OptionItem MaxPossesions;
        private static OptionItem PossessDur;
        private static OptionItem GhastlySpeed;

        private int PossessLimit = MaxPossesions != null ? MaxPossesions.GetInt() : byte.MaxValue;
        private (byte, byte) killertarget = (byte.MaxValue, byte.MaxValue);
        private static readonly Dictionary<byte, long> LastTime = [];
        private static bool KillerIsChosen = false;

        public static void SetupCustomOptions()
        {
            SetupSingleRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Ghastly);
            PossessCooldown = FloatOptionItem.Create(Id + 10, "GhastlyPossessCD", new(2.5f, 120f, 2.5f), 35f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Ghastly])
                .SetValueFormat(OptionFormat.Seconds);
            MaxPossesions = IntegerOptionItem.Create(Id + 11, "GhastlyMaxPossessions", new(1, 99, 1), 10, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Ghastly])
                .SetValueFormat(OptionFormat.Players);
            PossessDur = FloatOptionItem.Create(Id + 12, "GhastlyPossessionDuration", new(2.5f, 120f, 2.5f), 40f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Ghastly])
                .SetValueFormat(OptionFormat.Seconds);
            GhastlySpeed = FloatOptionItem.Create(Id + 13, "GhastlySpeed", new(1.5f, 5f, 0.5f), 2f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Ghastly])
                .SetValueFormat(OptionFormat.Multiplier);
        }
        public override void Init()
        {
            PlayerIds.Clear();
            LastTime.Clear();
            KillerIsChosen = false;
        }
        public override void Add(byte playerId)
        {
            PlayerIds.Add(playerId);

            CustomRoleManager.LowerOthers.Add(OthersNameText);
            CustomRoleManager.OnFixedUpdateOthers.Add(OnFixUpdateOthers);
        }

        private static void SendRPC(int limit)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, -1);
            writer.WritePacked((int)CustomRoles.Ghastly);
            writer.Write(limit);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public override void ReceiveRPC(MessageReader reader, PlayerControl NaN)
        {
            int Limit = reader.ReadInt32();
            PossessLimit = Limit;
        }
        public override void ApplyGameOptions(IGameOptions opt, byte playerId)
        {
            AURoleOptions.GuardianAngelCooldown = PossessCooldown.GetFloat();
            AURoleOptions.ProtectionDurationSeconds = 0f;
        }
        public override bool OnCheckProtect(PlayerControl angel, PlayerControl target)
        {
            if (PossessLimit <= 0)
            {
                angel.Notify(GetString("GhastlyNoMorePossess"));
                return false;
            }

            var killer = killertarget.Item1;
            var Target = killertarget.Item2;

            if (!KillerIsChosen && target.PlayerId != killer)
            {
                TargetArrow.Remove(killer, Target);
                LastTime.Remove(killer);
                killer = target.PlayerId;
                Target = byte.MaxValue;
                KillerIsChosen = true;

                angel.Notify(GetString("GhastlyChooseTarget"));
            }
            else if (KillerIsChosen && Target == byte.MaxValue && target.PlayerId != killer)
            {
                Target = target.PlayerId;
                PossessLimit--;
                SendRPC(PossessLimit);
                LastTime.Add(killer, GetTimeStamp());

                KillerIsChosen = false;
                GetPlayerById(killer).Notify(GetString("GhastlyYouvePosses"));

                TargetArrow.Add(killer, Target);
                angel.RpcGuardAndKill(target);
                angel.RpcResetAbilityCooldown();

                Logger.Info($" chosen {target.GetRealName()} for : {GetPlayerById(killer).GetRealName()}", "GhastlyTarget");
            }
            else if (target.PlayerId == killer)
            {
                angel.Notify(GetString("GhastlyCannotPossessTarget"));
            }

            killertarget = (killer, Target);

            return false;
        }
        public override void OnFixedUpdate(PlayerControl pc)
        {
            var speed = Main.AllPlayerSpeed[pc.PlayerId];
            if (speed != GhastlySpeed.GetFloat())
            {
                Main.AllPlayerSpeed[pc.PlayerId] = GhastlySpeed.GetFloat();
                pc.MarkDirtySettings();
            }
        }
        public void OnFixUpdateOthers(PlayerControl player)
        {
            if (killertarget.Item1 == player.PlayerId 
                && LastTime.TryGetValue(player.PlayerId, out var now) && now + PossessDur.GetFloat() <= GetTimeStamp())
            {
                TargetArrow.Remove(killertarget.Item1, killertarget.Item2);
                LastTime.Remove(player.PlayerId);
                KillerIsChosen = false;
                killertarget = (byte.MaxValue, byte.MaxValue);
            }

        }
        public override bool CheckMurderOnOthersTarget(PlayerControl killer, PlayerControl target)
        {
            var tuple = killertarget;
            if (tuple.Item1 == killer.PlayerId && tuple.Item2 != byte.MaxValue)
            {
                if (tuple.Item2 != target.PlayerId)
                {
                    killer.Notify(GetString("GhastlyNotUrTarget"));
                    return true;
                }
            }
            return false;
        }
        private string OthersNameText(PlayerControl seer, PlayerControl player, bool IsForMeeting, bool isforhud = false)
        {
            var IsMeeting = GameStates.IsMeeting || IsForMeeting;
            if (IsMeeting || seer != player) return "";

            var killer = killertarget.Item1;
            var target = killertarget.Item2;

            if (killer == seer.PlayerId && target != byte.MaxValue)
            {
                var arrows = TargetArrow.GetArrows(GetPlayerById(killer), target);
                var tar = GetPlayerById(target).GetRealName();
                var colorstring = "<alpha=#88>" + ColorString(GetRoleColor(CustomRoles.Ghastly), tar + arrows) + "</alpha>";

                return colorstring;
            }


            return "";
        }
        public override string GetProgressText(byte playerId, bool cooms) => ColorString(PossessLimit > 0 ? GetRoleColor(CustomRoles.Ghastly).ShadeColor(0.25f) : Color.gray, $"({PossessLimit})");
        
    }
}
