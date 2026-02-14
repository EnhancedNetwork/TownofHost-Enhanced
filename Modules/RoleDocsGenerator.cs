
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using TOHE.Roles.Core;
using UnityEngine;

namespace TOHE;

public static class RoleDocsGenerator
{
    static readonly string Desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
    [Obfuscation(Exclude = true)]
    private static readonly DirectoryInfo SaveDataDirectoryInfo = new(Desktop);
    private static FileInfo DocsFileInfo(string fileName) => new($"{SaveDataDirectoryInfo.FullName}/{(fileName.EndsWith(".md") ? fileName : fileName + ".md")}");
    private static readonly string template =
    """
    ---
    lang: {lang}
    title: {role}
    prev: {prev}
    next: {next}
    ---

    # <font color="{color}">{emoji} <b>{role}</b></font> <Badge text="{sub_alignment}" type="tip" vertical="middle"/>
    ---

    {roleInfoLong}

    {settings}

    > From: {author}

    <details>
    <summary><b><font color=gray>Unofficial Lore</font></b></summary>

    Placeholder: This role is a ROLE OH EM GOSH
    > Submitted by: Member
    </details>
    """;

    public static void GenerateDocs(this CustomRoles customRole)
    {
        if (customRole.IsAdditionRole())
        {
            customRole.GenerateAddonDocs();
            return;
        }
        RoleBase roleClass = customRole.GetStaticRoleClass();
        string infoLong = customRole.GetInfoLong();
        Color color = Utils.GetRoleColor(customRole);
        string colorCode = "#" + ColorUtility.ToHtmlStringRGB(color);

        Custom_RoleType roleType = roleClass.ThisRoleType;
        string alignment = roleType.GetAlignment();
        string sub_alignment = roleType.GetSubAlignment();
        string settings = string.Join("", GetSettings(customRole));

        Dictionary<string, string> replaceDict = new()
        {
            ["{lang}"] = "en_us",
            ["{role}"] = customRole.GetActualRoleName(),
            ["{color}"] = colorCode,
            ["{sub_alignment}"] = sub_alignment,
            ["{roleInfoLong}"] = infoLong,
            ["{settings}"] = settings,
            [$"({alignment}):\n"] = "",
        };

        string docs = template;

        foreach (var rplc in replaceDict)
        {
            docs = docs.Replace(rplc.Key, rplc.Value);
        }

        docs = docs.FormatColors();

        SaveDocs(docs, $"{replaceDict["{role}"]}");
    }

    private static void GenerateAddonDocs(this CustomRoles role)
    {
        var addon = CustomRoleManager.AddonClasses[role];
        string sub_alignment = addon.Type.ToString();
        string infoLong = role.GetInfoLong();
        Color color = Utils.GetRoleColor(role);
        string colorCode = $"#{color.r:x2}{color.g:x2}{color.b:x2}{color.a:x2}";

        string settings = string.Join("", GetSettings(role));

        Dictionary<string, string> replaceDict = new()
        {
            ["{lang}"] = "en_us",
            ["{role}"] = role.GetActualRoleName(),
            ["{color}"] = colorCode,
            ["{sub_alignment}"] = sub_alignment,
            ["{roleInfoLong}"] = infoLong,
            ["{settings}"] = settings,
            [$"(Add-ons):\n"] = "",
        };

        string docs = template;

        foreach (var rplc in replaceDict)
        {
            docs = docs.Replace(rplc.Key, rplc.Value);
        }

        docs = docs.FormatColors();

        SaveDocs(docs, $"{replaceDict["{role}"]}");
    }

    static void SaveDocs(string docs, string fileName)
    {
        if (!DocsFileInfo(fileName).Exists)
        {
            DocsFileInfo(fileName).Create().Dispose();
        }

        try
        {
            File.WriteAllText(DocsFileInfo(fileName).FullName, docs);

            ProcessStartInfo psi = new("Explorer.exe") { Arguments = "/e,/select," + DocsFileInfo(fileName).FullName.Replace("/", "\\") };
            Process.Start(psi);
        }
        catch (Exception error)
        {
            Logger.Error($"Error: {error}", "OptionCopier.Save");
        }
    }

    static string GetAlignment(this Custom_RoleType roleType) => roleType switch
    {
        Custom_RoleType.ImpostorVanilla or Custom_RoleType.ImpostorKilling or Custom_RoleType.ImpostorSupport or
        Custom_RoleType.ImpostorConcealing or Custom_RoleType.ImpostorHindering or Custom_RoleType.ImpostorGhosts or
        Custom_RoleType.Madmate => "Impostors",
        Custom_RoleType.CrewmateVanilla or Custom_RoleType.CrewmateVanillaGhosts or Custom_RoleType.CrewmateBasic or
        Custom_RoleType.CrewmateSupport or Custom_RoleType.CrewmateKilling or Custom_RoleType.CrewmatePower or 
        Custom_RoleType.CrewmateInvestigative or
        Custom_RoleType.CrewmateGhosts => "Crewmates",
        Custom_RoleType.NeutralBenign or Custom_RoleType.NeutralEvil or Custom_RoleType.NeutralChaos or
        Custom_RoleType.NeutralKilling or Custom_RoleType.NeutralApocalypse or Custom_RoleType.NeutralPariah => "Neutrals",
        Custom_RoleType.CovenPower or Custom_RoleType.CovenKilling or Custom_RoleType.CovenTrickery or
        Custom_RoleType.CovenUtility => "Coven",
        _ => "None"
    };

    static string GetSubAlignment(this Custom_RoleType roleType) => roleType switch
    {
        Custom_RoleType.ImpostorVanilla or Custom_RoleType.CrewmateVanilla or Custom_RoleType.CrewmateVanillaGhosts
        => "Vanilla",
        Custom_RoleType.ImpostorKilling or Custom_RoleType.CrewmateKilling or Custom_RoleType.NeutralKilling or
        Custom_RoleType.CovenKilling => "Killing",
        Custom_RoleType.ImpostorSupport or Custom_RoleType.CrewmateSupport => "Support",
        Custom_RoleType.ImpostorConcealing => "Concealing",
        Custom_RoleType.ImpostorHindering => "Hindering",
        Custom_RoleType.ImpostorGhosts or Custom_RoleType.CrewmateGhosts => "Ghost",
        Custom_RoleType.Madmate => "Madmate",
        Custom_RoleType.CrewmateBasic => "Basic",
        Custom_RoleType.CrewmatePower or Custom_RoleType.CovenPower => "Power",
        Custom_RoleType.NeutralBenign => "Benign",
        Custom_RoleType.NeutralEvil => "Evil",
        Custom_RoleType.NeutralChaos => "Chaos",
        Custom_RoleType.NeutralApocalypse => "Apocalypse",
        Custom_RoleType.NeutralPariah => "Pariah",
        Custom_RoleType.CovenTrickery => "Trickey",
        Custom_RoleType.CovenUtility => "Utility",
        Custom_RoleType.CrewmateInvestigative => "Investigative",
        _ => "None"
    };

    static List<string> GetSettings(CustomRoles role)
    {
        List<OptionItem> options = [Options.CustomRoleSpawnChances[role]];

        foreach (var option in OptionItem.AllOptions)
        {
            var parent = option.Parent;
            if (options.Contains(parent))
                options.Add(option);
        }

        options.Remove(Options.CustomRoleSpawnChances[role]);

        return [.. options.Select(GetOptionSettings)];
    }

    static string GetOptionSettings(OptionItem option)
    {
        var name = option.GetName(disableColor: true);
        StringBuilder sb = new($"* {name}\n");

        if (option is BooleanOptionItem)
        {
            sb.Append("\t* <font color=green>ON</font>: {on placeholder}\n");
            sb.Append("\t* <font color=red>OFF</font>: {off placeholder}\n");
        }
        else if (option is IntegerOptionItem or FloatOptionItem)
        {
            sb.Append("\t* {number placeholder}\n");
        }
        else if (option is StringOptionItem s)
        {
            foreach (var sel in s.Selections)
            {
                sb.Append($"\t* {sel}: {{option placeholder}}\n");
            }
        }

        return sb.ToString();
    }

    static string FormatColors(this string input)
    {
        string pattern = "<color=#([A-Fa-f0-9]{6})([A-Fa-f0-9]{2})?>";
        string replace = @"<font color=""#$1"">";
        input = Regex.Replace(input, pattern, replace);
        input = input.Replace("</color>", "</font>");
        return input;
    }
}