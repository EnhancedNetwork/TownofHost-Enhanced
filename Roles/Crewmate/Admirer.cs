using Hazel;
using TOHE.Roles.AddOns.Crewmate;
using TOHE.Roles.AddOns.Impostor;
using TOHE.Roles.Core;
using TOHE.Roles.Coven;
using TOHE.Roles.Double;
using TOHE.Roles.Neutral;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate;

internal class Admirer : RoleBase
{
    //===========================SETUP================================\\
    public override CustomRoles Role => CustomRoles.Admirer;
    private const int Id = 24800;
    public static bool HasEnabled => CustomRoleManager.HasEnabled(CustomRoles.Admired);
    public override bool IsDesyncRole => true;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    public override Custom_RoleType ThisRoleType => Custom_RoleType.CrewmatePower;
    //==================================================================\\

    private static OptionItem AdmireCooldown;
    private static OptionItem KnowTargetRole;
    private static OptionItem SkillLimit;

    public static readonly Dictionary<byte, HashSet<byte>> AdmiredList = [];

    public override void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Admirer);
        AdmireCooldown = FloatOptionItem.Create(Id + 10, "AdmireCooldown", new(1f, 180f, 1f), 5f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Admirer])
            .SetValueFormat(OptionFormat.Seconds);
        KnowTargetRole = BooleanOptionItem.Create(Id + 11, "AdmirerKnowTargetRole", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Admirer]);
        SkillLimit = IntegerOptionItem.Create(Id + 12, "AdmirerSkillLimit", new(0, 100, 1), 1, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Admirer])
            .SetValueFormat(OptionFormat.Times);
    }
    public override void Init()
    {
        AdmiredList.Clear();
    }
    public override void Add(byte playerId)
    {
        AbilityLimit = SkillLimit.GetInt();
        AdmiredList[playerId] = [];
    }
    public override void Remove(byte playerId)
    {
        AdmiredList.Remove(playerId);
    }

    private void SendRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncAdmiredAbility, SendOption.Reliable, -1);
        writer.Write(playerId);
        writer.Write(AbilityLimit);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void SendRPC(byte playerId, byte targetId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncAdmiredList, SendOption.Reliable, -1);
        writer.Write(playerId);
        writer.Write(targetId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader, bool isList)
    {
        byte playerId = reader.ReadByte();
        byte targetId;
        float Limit;
        if (!isList)
        {
            Limit = reader.ReadSingle();
            Main.PlayerStates[playerId].RoleClass.AbilityLimit = Limit;
        }
        else
        {
            targetId = reader.ReadByte();
            if (!AdmiredList.ContainsKey(playerId))
                AdmiredList.Add(playerId, []);
            else AdmiredList[playerId].Add(targetId);
        }
    }

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = AbilityLimit >= 1 ? AdmireCooldown.GetFloat() : 300f;
    public override bool CanUseKillButton(PlayerControl player) => AbilityLimit >= 1;

    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (AbilityLimit < 1) return false;
        if (Mini.Age < 18 && (target.Is(CustomRoles.NiceMini) || target.Is(CustomRoles.EvilMini)))
        {
            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Cultist), GetString("CantRecruit")));
            return false;
        }

        if (!AdmiredList.ContainsKey(killer.PlayerId))
            AdmiredList.Add(killer.PlayerId, []);

        if (AbilityLimit < 1) return false;
        if (CanBeAdmired(target, killer))
        {
            if (KnowTargetRole.GetBool())
            {
                AdmiredList[killer.PlayerId].Add(target.PlayerId);
                SendRPC(killer.PlayerId, target.PlayerId); //Sync playerId list
            }
            

            if (!killer.GetCustomSubRoles().Find(x => x.IsBetrayalAddonV2() && x is not CustomRoles.Soulless, out var convertedAddon))
            {
                convertedAddon = CustomRoles.Admired;
            }
            else if (killer.IsAnySubRole(x => x.IsBetrayalAddonV2() && x is not CustomRoles.Soulless))
            {
                foreach (var subRole in killer.GetCustomSubRoles().Where(x => x.IsBetrayalAddonV2() && x is not CustomRoles.Soulless))
                {
                    switch (subRole)
                    {
                        case CustomRoles.Madmate when target.CanBeMadmate(forAdmirer: true):
                            convertedAddon = CustomRoles.Madmate;
                            break;
                        case CustomRoles.Enchanted when Ritualist.CanBeConverted(target):
                            convertedAddon = CustomRoles.Enchanted;
                            break;
                        case CustomRoles.Recruit when Jackal.CanBeSidekick(target):
                            convertedAddon = CustomRoles.Recruit;
                            break;
                        case CustomRoles.Charmed when Cultist.CanBeCharmed(target):
                            convertedAddon = CustomRoles.Charmed;
                            break;
                        case CustomRoles.Infected when target.CanBeInfected():
                            convertedAddon = CustomRoles.Infected;
                            break;
                        case CustomRoles.Contagious when target.CanBeInfected():
                            convertedAddon = CustomRoles.Contagious;
                            break;
                    }
                }
                Logger.Info("Set converted: " + target.GetNameWithRole().RemoveHtmlTags() + " to " + convertedAddon.ToString(), "Admirer Assign");
                target.RpcSetCustomRole(convertedAddon, false);
                killer.Notify(Utils.ColorString(Utils.GetRoleColor(convertedAddon), GetString("AdmiredPlayer")));
                target.Notify(Utils.ColorString(Utils.GetRoleColor(convertedAddon), GetString("AdmirerAdmired")));
            }
            else goto AdmirerFailed;

            AbilityLimit--;
            SendRPC(killer.PlayerId); //Sync skill

            killer.ResetKillCooldown();
            killer.SetKillCooldown();
            if (!DisableShieldAnimations.GetBool())
                killer.RpcGuardAndKill(target);

            target.RpcGuardAndKill(killer);
            target.ResetKillCooldown();
            target.SetKillCooldown(forceAnime: true);

            Logger.Info("设置职业:" + target?.Data?.PlayerName + " = " + target.GetCustomRole().ToString() + " + " + CustomRoles.Admirer.ToString(), "Assign " + CustomRoles.Admirer.ToString());

            return false;
        }

    AdmirerFailed:
        SendRPC(killer.PlayerId); //Sync skill

        killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Admirer), GetString("AdmirerInvalidTarget")));

        return false;
    }

    public override bool KnowRoleTarget(PlayerControl seer, PlayerControl target) => CheckKnowRoleTarget(seer, target);

    public static bool CheckKnowRoleTarget(PlayerControl seer, PlayerControl target)
    {
        if (!KnowTargetRole.GetBool()) return false;
        if (AdmiredList.ContainsKey(seer.PlayerId))
        {
            if (AdmiredList[seer.PlayerId].Contains(target.PlayerId)) return true;
            return false;
        }
        else if (AdmiredList.ContainsKey(target.PlayerId))
        {
            if (AdmiredList[target.PlayerId].Contains(seer.PlayerId)) return true;
            return false;
        }
        else return false;
    }

    public override string GetProgressText(byte playerId, bool comms)
        => Utils.ColorString(AbilityLimit >= 1 ? Utils.GetRoleColor(CustomRoles.Admirer).ShadeColor(0.25f) : Color.gray, $"({AbilityLimit})");

    public static bool CanBeAdmired(PlayerControl pc, PlayerControl admirer)
    {
        if (AdmiredList.ContainsKey(admirer.PlayerId))
        {
            if (AdmiredList[admirer.PlayerId].Contains(pc.PlayerId))
                return false;
        }
        else AdmiredList.Add(admirer.PlayerId, []);

        return pc != null && (pc.GetCustomRole().IsCrewmate() || pc.GetCustomRole().IsImpostor() || pc.GetCustomRole().IsNeutral() || pc.GetCustomRole().IsCoven())
            && !pc.Is(CustomRoles.Soulless) && !pc.Is(CustomRoles.Lovers) && !pc.Is(CustomRoles.Loyal)
            && !((pc.Is(CustomRoles.NiceMini) || pc.Is(CustomRoles.EvilMini)) && Mini.Age < 18)
            && !(pc.GetCustomSubRoles().Contains(CustomRoles.Hurried) && !Hurried.CanBeConverted.GetBool());
    }

    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.KillButton?.OverrideText(GetString("AdmireButtonText"));
    }
}
