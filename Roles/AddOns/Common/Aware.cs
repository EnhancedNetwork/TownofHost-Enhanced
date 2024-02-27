using System.Collections.Generic;
using System.Linq;
using static TOHE.Translator;
using static TOHE.Options;

namespace TOHE.Roles.AddOns.Common;

public static class Aware
{
    private static readonly int Id = 21600;
    public static bool IsEnable = false;

    public static OptionItem ImpCanBeAware;
    public static OptionItem CrewCanBeAware;
    public static OptionItem NeutralCanBeAware;
    private static OptionItem AwareknowRole;

    public static Dictionary<byte, List<string>> AwareInteracted = [];

    public static void SetupCustomOptions()
    {
        SetupAdtRoleOptions(21600, CustomRoles.Aware, canSetNum: true);
        ImpCanBeAware = BooleanOptionItem.Create(Id + 10, "ImpCanBeAware", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Aware]);
        CrewCanBeAware = BooleanOptionItem.Create(Id + 11, "CrewCanBeAware", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Aware]);
        NeutralCanBeAware = BooleanOptionItem.Create(Id + 12, "NeutralCanBeAware", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Aware]);
        AwareknowRole = BooleanOptionItem.Create(Id + 13, "AwareKnowRole", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Aware]);
    }

    public static void Init()
    {
        AwareInteracted = [];
        IsEnable = false;
    }
    public static void Add(byte playerId)
    {
        AwareInteracted[playerId] = [];
        IsEnable = true;
    }

    public static void OnCheckMurder(CustomRoles killerRole, PlayerControl target)
    {
        switch (killerRole)
        {
            case CustomRoles.Consigliere:
            case CustomRoles.Overseer:
                if (!AwareInteracted.ContainsKey(target.PlayerId))
                {
                    AwareInteracted.Add(target.PlayerId, []);
                }
                if (!AwareInteracted[target.PlayerId].Contains(Utils.GetRoleName(killerRole)))
                {
                    AwareInteracted[target.PlayerId].Add(Utils.GetRoleName(killerRole));
                }
                break;
        }
    }

    public static void OnReportDeadBody() 
    {
        foreach (var pid in AwareInteracted.Keys.ToArray())
        {
            var Awarepc = Utils.GetPlayerById(pid);
            if (AwareInteracted[pid].Count > 0 && Awarepc.IsAlive())
            {
                string rolelist = "Someone";
                _ = new LateTask(() =>
                {
                    if (AwareknowRole.GetBool())
                        rolelist = string.Join(", ", AwareInteracted[pid]);

                    Utils.SendMessage(string.Format(GetString("AwareInteracted"), rolelist), pid, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Aware), GetString("AwareTitle")));
                    AwareInteracted[pid] = [];
                }, 0.5f, "Aware Check Msg");
            }
        }

    }
    public static void OnVoted(PlayerControl pc, PlayerVoteArea pva)
    {
        switch (pc.GetCustomRole())
        {
            case CustomRoles.Divinator:
            case CustomRoles.Oracle:
                if (!AwareInteracted.ContainsKey(pva.VotedFor)) AwareInteracted[pva.VotedFor] = [];
                if (!AwareInteracted[pva.VotedFor].Contains(Utils.GetRoleName(pc.GetCustomRole())))
                    AwareInteracted[pva.VotedFor].Add(Utils.GetRoleName(pc.GetCustomRole()));
                break;
        }
    }
}

