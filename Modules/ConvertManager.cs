using Hazel;
using LibCpp2IL.Elf;
using System.Collections.Generic;
using System.Linq;
using TOHE.Roles.AddOns.Crewmate;
using TOHE.Roles.Crewmate;
using TOHE.Roles.Double;
using TOHE.Roles.Neutral;
using static TOHE.Translator;

namespace TOHE.Modules
{
    /*
     * Niko cant understand the conditions of virus and infectious
     * So currently these 2 roles are not supported by convert Manager
     * 
     * Used by Gangster, Admirer, Jackal
     */
    public static class ConvertManager
    {
        public static Dictionary<byte, byte> AlreadyConverted = new(); //converted target, converter
        public static CustomRoles GetConvertSubRole(PlayerControl killer, CustomRoles original)
        {
            if (killer.Is(CustomRoles.Madmate)) return CustomRoles.Madmate;
            if (killer.Is(CustomRoles.Recruit)) return CustomRoles.Recruit;
            if (killer.Is(CustomRoles.Charmed)) return CustomRoles.Charmed;
            if (killer.Is(CustomRoles.Infected)) return CustomRoles.Infected;
            if (killer.Is(CustomRoles.Admired)) return CustomRoles.Admired;
            if (killer.Is(CustomRoles.Contagious)) return CustomRoles.Contagious;
            //Overall check if killer is converted

            return original;
            //Above check fails? Return original sub role here
            //Following code is not used
            /*var mainRole = killer.GetCustomRole();
            if (mainRole.IsImpostorTeam()) return CustomRoles.Madmate;
            switch (mainRole)
            {
                case CustomRoles.Gangster: 
                    return CustomRoles.Madmate;
                case CustomRoles.Jackal:
                    return CustomRoles.Recruit;
                case CustomRoles.Succubus:
                    return CustomRoles.Charmed;
                case CustomRoles.Virus:
                    return CustomRoles.Contagious;
                case CustomRoles.Infectious:
                    return CustomRoles.Infected;
                case CustomRoles.Admirer:
                    return CustomRoles.Admired;
            }*/
        }

        public static bool CanBeConvertSubRole(PlayerControl target, CustomRoles subRole, PlayerControl killer = null)
        {
            if (target == null && target.Data.IsDead) return false;

            if (target.Is(subRole)) return false;

            if (target.Is(CustomRoles.Loyal) || target.Is(CustomRoles.Soulless)) return false;
            if ((target.Is(CustomRoles.NiceMini) || target.Is(CustomRoles.EvilMini)) && Mini.Age < 18) return false;
            if (target.Is(CustomRoles.Hurried) && !Hurried.CanBeConverted.GetBool()) return false;

            if (target.Is(CustomRoles.Succubus) || target.Is(CustomRoles.Virus) || target.Is(CustomRoles.Infectious)) return false;
            //Ban these three role until i fix it

            if (killer != null)
            {
                if (AlreadyConverted.ContainsKey(target.PlayerId))
                {
                    if (AlreadyConverted[target.PlayerId] == killer.PlayerId)
                        return false;
                }
            }

            switch (subRole)
            {
                case CustomRoles.Admired:
                    break; //重写
                case CustomRoles.Madmate:
                    if (Utils.CanBeMadmate(target, inGame: true))
                        goto Succeed;
                    break;
                case CustomRoles.Recruit:
                    if (!target.Is(CustomRoles.Sidekick) && !target.Is(CustomRoles.Recruit))
                        goto Succeed;
                    break; //Jackal is superb
                case CustomRoles.Charmed:
                    if (Succubus.CanBeCharmed(target)) goto Succeed;
                    break;
                //Following is Not used by main virus and infectious
                case CustomRoles.Infected:
                    if (Infectious.CanBeBitten(target)) goto Succeed;
                    break;
                case CustomRoles.Contagious:
                    if (Virus.CanBeInfected(target)) goto Succeed;
                    break;
            }

            return false;

        Succeed:
            return true;
            //Niko wanted to do something here, but then they forgot
        }

        public static void SetPlayerConverted(PlayerControl killer, PlayerControl target, CustomRoles targetSubRole = CustomRoles.NotAssigned)
        {
            if (!AlreadyConverted.ContainsKey(target.PlayerId))
            {
                AlreadyConverted.Add(target.PlayerId, killer.PlayerId);
            }
            else AlreadyConverted[target.PlayerId] = killer.PlayerId;

            ResetPlayerConverted(target, targetSubRole);

            if (AmongUsClient.Instance.AmHost)
                SendRPC(killer.PlayerId, target.PlayerId, targetSubRole);
        }
        public static void ResetPlayerConverted(PlayerControl target, CustomRoles targetSubRole)
        {
            if (!Options.UseSuperbConvertSystem.GetBool())
            {
                foreach (var item in AlreadyConverted)
                {
                    if (item.Value != target.PlayerId) continue;
                    AlreadyConverted.Remove(item.Key);
                }
                return;
            }
            
            if (AmongUsClient.Instance.AmHost)
            {
                if (AlreadyConverted.Count > 0)
                {
                    foreach (var item in AlreadyConverted)
                    {
                        if (item.Value != target.PlayerId) continue;

                        var pc = Utils.GetPlayerById(item.Key);
                        if (pc != null)
                        {
                            if (pc.IsAlive())
                            {
                                Logger.Info($"Superb change Alive {pc.GetNameWithRole()} to {targetSubRole.ToString()} by {target.GetNameWithRole()}", "Superb Assign");
                                pc.RpcSetCustomRole(targetSubRole);
                                pc.ResetKillCooldown();
                                pc.SetKillCooldown(forceAnime: true);
                            }
                            else if (Options.SuperbConvertDeadPlayer.GetBool())
                            {
                                Logger.Info($"Superb change DEAD {pc.GetNameWithRole()} to {targetSubRole.ToString()} by {target.GetNameWithRole()}", "Superb Assign");
                                pc.RpcSetCustomRole(targetSubRole);
                            }
                        }
                    }
                }
                Utils.NotifyRoles();
            }
            
        }

        public static void SendRPC(byte killer, byte target, CustomRoles customRoles)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetPlayerConvert, SendOption.Reliable, -1);
            writer.Write(killer);
            writer.Write(target);
            writer.WritePacked((int)customRoles);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }

        public static void ReceiveRPC(MessageReader reader)
        {
            var killer = Utils.GetPlayerById(reader.ReadByte());
            var target = Utils.GetPlayerById(reader.ReadByte());
            CustomRoles role = (CustomRoles)reader.ReadPackedInt32();

            if (killer != null && target != null)
                SetPlayerConverted(killer, target, role);
        }

        public static bool KnowRole(PlayerControl seer, PlayerControl target, bool ConvertedSeeEachOther = false)
        {
            if (ConvertedSeeEachOther)
            {
                if (AlreadyConverted.ContainsKey(seer.PlayerId) && AlreadyConverted.ContainsKey(target.PlayerId))
                    if (AlreadyConverted[seer.PlayerId] == AlreadyConverted[target.PlayerId]) return true;
                //This is for converted players by the same converter to know each other
            }

            if (AlreadyConverted.ContainsKey(seer.PlayerId))
                if (AlreadyConverted[seer.PlayerId] == target.PlayerId) return true;
            //Converted player know who converted him

            foreach (var item in AlreadyConverted)
            {
                if (item.Value != seer.PlayerId) continue;
                if (item.Key == target.PlayerId) return true;
            }
            //Converter know converted

            return false;
        }
        
    }
}
