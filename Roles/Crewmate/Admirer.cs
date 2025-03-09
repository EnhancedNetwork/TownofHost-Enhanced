using Hazel;
using TOHE.Modules;
using TOHE.Roles.AddOns.Crewmate;
using TOHE.Roles.AddOns.Impostor;
using TOHE.Roles.Core;
using TOHE.Roles.Coven;
using TOHE.Roles.Double;
using TOHE.Roles.Neutral;
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
        playerId.SetAbilityUseLimit(SkillLimit.GetInt());
        AdmiredList[playerId] = [];
    }
    public override void Remove(byte playerId)
    {
        AdmiredList.Remove(playerId);
    }
    public static void SendRPC(byte playerId, byte targetId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncAdmiredList, SendOption.Reliable, -1);
        writer.Write(playerId);
        writer.Write(targetId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        byte playerId = reader.ReadByte();
        byte targetId = reader.ReadByte();

        if (!AdmiredList.ContainsKey(playerId))
            AdmiredList.Add(playerId, []);
        else AdmiredList[playerId].Add(targetId);
    }

    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = id.GetAbilityUseLimit() >= 1 ? AdmireCooldown.GetFloat() : 300f;
    public override bool CanUseKillButton(PlayerControl player) => player.GetAbilityUseLimit() >= 1;

    public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
    {
        if (killer.GetAbilityUseLimit() < 1) return false;
        if (Mini.Age < 18 && (target.Is(CustomRoles.NiceMini) || target.Is(CustomRoles.EvilMini)))
        {
            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Cultist), GetString("CantRecruit")));
            return false;
        }

        if (!AdmiredList.ContainsKey(killer.PlayerId))
            AdmiredList.Add(killer.PlayerId, []);

        if (CanBeAdmired(target, killer))
        {
            if (KnowTargetRole.GetBool())
            {
                AdmiredList[killer.PlayerId].Add(target.PlayerId);
                SendRPC(killer.PlayerId, target.PlayerId); //Sync playerId list
            }

            if (!killer.Is(CustomRoles.Madmate) && !killer.Is(CustomRoles.Recruit) && !killer.Is(CustomRoles.Charmed)
                && !killer.Is(CustomRoles.Infected) && !killer.Is(CustomRoles.Contagious) && !killer.Is(CustomRoles.Enchanted))
            {
                Logger.Info("Set converted: " + target.GetNameWithRole().RemoveHtmlTags() + " to " + CustomRoles.Admired.ToString(), "Admirer Assign");
                target.RpcSetCustomRole(CustomRoles.Admired);
                killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Admirer), GetString("AdmiredPlayer")));
                target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Admirer), GetString("AdmirerAdmired")));
            }
            else if (killer.Is(CustomRoles.Madmate) && target.CanBeMadmate(forAdmirer: true))
            {
                Logger.Info("Set converted: " + target.GetNameWithRole().RemoveHtmlTags() + " to " + CustomRoles.Madmate.ToString(), "Admirer Assign");
                target.RpcSetCustomRole(CustomRoles.Madmate);
                killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Madmate), GetString("AdmiredPlayer")));
                target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Madmate), GetString("AdmirerAdmired")));
            }
            else if (killer.Is(CustomRoles.Enchanted) && Ritualist.CanBeConverted(target))
            {
                Logger.Info("Set converted: " + target.GetNameWithRole().RemoveHtmlTags() + " to " + CustomRoles.Enchanted.ToString(), "Admirer Assign");
                target.RpcSetCustomRole(CustomRoles.Enchanted);
                killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Enchanted), GetString("AdmiredPlayer")));
                target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Enchanted), GetString("AdmirerAdmired")));
            }
            else if (killer.Is(CustomRoles.Recruit) && Jackal.CanBeSidekick(target))
            {
                Logger.Info("Set converted: " + target.GetNameWithRole().RemoveHtmlTags() + " to " + CustomRoles.Recruit.ToString(), "Admirer Assign");
                target.RpcSetCustomRole(CustomRoles.Recruit);
                killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Recruit), GetString("AdmiredPlayer")));
                target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Recruit), GetString("AdmirerAdmired")));
            }
            else if (killer.Is(CustomRoles.Charmed) && Cultist.CanBeCharmed(target))
            {
                Logger.Info("Set converted: " + target.GetNameWithRole().RemoveHtmlTags() + " to " + CustomRoles.Charmed.ToString(), "Admirer Assign");
                target.RpcSetCustomRole(CustomRoles.Charmed);
                killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Charmed), GetString("AdmiredPlayer")));
                target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Charmed), GetString("AdmirerAdmired")));
            }
            else if (killer.Is(CustomRoles.Infected) && target.CanBeInfected())
            {
                Logger.Info("Set converted: " + target.GetNameWithRole().RemoveHtmlTags() + " to " + CustomRoles.Infected.ToString(), "Admirer Assign");
                target.RpcSetCustomRole(CustomRoles.Infected);
                killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Infected), GetString("AdmiredPlayer")));
                target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Infected), GetString("AdmirerAdmired")));
            }
            else if (killer.Is(CustomRoles.Contagious) && target.CanBeInfected())
            {
                Logger.Info("Set converted: " + target.GetNameWithRole().RemoveHtmlTags() + " to " + CustomRoles.Contagious.ToString(), "Admirer Assign");
                target.RpcSetCustomRole(CustomRoles.Contagious);
                killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Contagious), GetString("AdmiredPlayer")));
                target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Contagious), GetString("AdmirerAdmired")));
            }
            else goto AdmirerFailed;

            killer.RpcRemoveAbilityUse();

            killer.ResetKillCooldown();
            killer.SetKillCooldown();
            if (!DisableShieldAnimations.GetBool())
                killer.RpcGuardAndKill(target);

            target.RpcGuardAndKill(killer);
            target.ResetKillCooldown();
            target.SetKillCooldown(forceAnime: true);

            Logger.Info(target?.Data?.PlayerName + " = " + target.GetCustomRole().ToString() + " + " + CustomRoles.Admirer.ToString(), "Assign " + CustomRoles.Admirer.ToString());

            return false;
        }

    AdmirerFailed:

        killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Admirer), GetString("AdmirerInvalidTarget")));
        return false;
    }

    public override bool KnowRoleTarget(PlayerControl seer, PlayerControl target) => CheckKnowRoleTarget(seer, target) && !Main.PlayerStates[seer.PlayerId].IsNecromancer;

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
            && !(pc.GetCustomSubRoles().Contains(CustomRoles.Hurried) && !Hurried.CanBeConverted.GetBool()) && !(CovenManager.HasNecronomicon(pc.PlayerId) && pc.Is(CustomRoles.CovenLeader));
    }

    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        hud.KillButton?.OverrideText(GetString("AdmireButtonText"));
    }
}
