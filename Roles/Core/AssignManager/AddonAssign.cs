using System;
using TOHE.Roles.AddOns.Impostor;

namespace TOHE.Roles.Core.AssignManager;

public static class AddonAssign
{
    private static readonly HashSet<CustomRoles> AddonRolesList = [];

    private static bool NotAssignAddOnInGameStarted(CustomRoles role)
    {
        switch (role)
        {
            case CustomRoles.Lovers:
            case CustomRoles.Workhorse:
            case CustomRoles.LastImpostor:
                return true;
            case CustomRoles.Autopsy when Options.EveryoneCanSeeDeathReason.GetBool():
            case CustomRoles.Madmate when Madmate.MadmateSpawnMode.GetInt() != 0:
            case CustomRoles.Glow or CustomRoles.Mare when GameStates.FungleIsActive:
                return true;
        }

        /*else if (Options.IsActiveDleks) // Dleks
        {
            if (role is CustomRoles.Nimble or CustomRoles.Burst or CustomRoles.Circumvent) continue;
        }*/

        return false;
    }

    public static void StartSelect()
    {
        switch (Options.CurrentGameMode)
        {
            case CustomGameMode.FFA:
            case CustomGameMode.CandR:
            case CustomGameMode.UltimateTeam:
            case CustomGameMode.TrickorTreat:
                return;
        }
        AddonRolesList.Clear();
        foreach (var cr in CustomRolesHelper.AllRoles)
        {
            CustomRoles role = (CustomRoles)Enum.Parse(typeof(CustomRoles), cr.ToString());
            if (!role.IsAdditionRole()) continue;

            if (NotAssignAddOnInGameStarted(role)) continue;

            AddonRolesList.Add(role);
        }
    }
    public static void StartSortAndAssign()
    {
           if (Options.CurrentGameMode == CustomGameMode.FFA || Options.CurrentGameMode == CustomGameMode.CandR || Options.CurrentGameMode == CustomGameMode.UltimateTeam || Options.CurrentGameMode == CustomGameMode.TrickorTreat) return;

        var rd = IRandom.Instance;
        List<CustomRoles> addonsList = [];
        List<CustomRoles> addonsIsEnableList = [];

        // Sort Add-ons by spawn rate
        var sortAddOns = Options.CustomAdtRoleSpawnRate.OrderByDescending(role => role.Value.GetFloat());
        var dictionarSortAddOns = sortAddOns.ToDictionary(x => x.Key, x => x.Value);

        // Add only enabled add-ons
        foreach (var addonKVP in dictionarSortAddOns.Where(a => a.Key.IsEnable()).ToArray())
        {
            if (!NotAssignAddOnInGameStarted(addonKVP.Key))
            {
                addonsIsEnableList.Add(addonKVP.Key);
            }
        }

        Logger.Info($"Number enabled of add-ons (before priority): {addonsIsEnableList.Count}", "Check Add-ons Count");

        // Add addons which have a percentage greater than 90
        foreach (var addonKVP in dictionarSortAddOns.Where(a => a.Key.IsEnable() && a.Value.GetFloat() >= 90).ToArray())
        {
            var addon = addonKVP.Key;

            if (AddonRolesList.Contains(addon))
            {
                addonsList.Add(addon);
                addonsIsEnableList.Remove(addon);
            }
        }

        if (addonsList.Count > 2)
            addonsList = addonsList.Shuffle(rd).ToList();

        Logger.Info($"Number enabled of add-ons (after priority): {addonsIsEnableList.Count}", "Check Add-ons Count");

        // Add addons randomly
        while (addonsIsEnableList.Any())
        {
            var randomAddOn = addonsIsEnableList.RandomElement();

            if (!addonsList.Contains(randomAddOn) && AddonRolesList.Contains(randomAddOn))
            {
                addonsList.Add(randomAddOn);
            }

            // Even if an add-on cannot be added, it must be removed from the "addonsIsEnableList"
            // To prevent the game from freezing
            addonsIsEnableList.Remove(randomAddOn);
        }

        Logger.Info($" Is Started", "Assign Add-ons");

        // Assign add-ons
        foreach (var addOn in addonsList.ToArray())
        {
            if (rd.Next(1, 101) <= (Options.CustomAdtRoleSpawnRate.TryGetValue(addOn, out var sc) ? sc.GetFloat() : 0))
            {
                AssignSubRoles(addOn);
            }
        }
    }
    public static void AssignSubRoles(CustomRoles role, int RawCount = -1)
    {
        try
        {
            var checkAllPlayers = Main.AllAlivePlayerControls.Where(x => CustomRolesHelper.CheckAddonConfilct(role, x));
            var allPlayers = checkAllPlayers.ToList();
            if (!allPlayers.Any()) return;
            var count = Math.Clamp(RawCount, 0, allPlayers.Count);
            if (RawCount == -1) count = Math.Clamp(role.GetCount(), 0, allPlayers.Count);
            if (count <= 0) return;
            for (var i = 0; i < count; i++)
            {
                // if the number of all players is 0
                if (!allPlayers.Any()) return;

                // Select player
                var player = allPlayers.RandomElement();
                allPlayers.Remove(player);

                // Set Add-on
                Main.PlayerStates[player.PlayerId].SetSubRole(role);
                Logger.Info($"Registered Add-on: {player?.Data?.PlayerName} = {player.GetCustomRole()} + {role}", $"Assign {role}");
            }
        }
        catch (Exception error)
        {
            Logger.Warn($"Add-On {role} get error after check addon confilct for: {error}", "AssignSubRoles");
        }
    }

    public static void InitAndStartAssignLovers()
    {
        var rd = IRandom.Instance;
        if (CustomRoles.Lovers.IsEnable() && (CustomRoles.Hater.IsEnable() ? -1 : rd.Next(1, 100)) <= Options.LoverSpawnChances.GetInt())
        {
            // Initialize Lovers
            Main.LoversPlayers.Clear();
            Main.isLoversDead = false;

            //Two randomly selected
            AssignLovers();
        }
    }
    private static void AssignLovers(int RawCount = -1)
    {
        var allPlayers = new List<PlayerControl>();
        foreach (var pc in Main.AllPlayerControls)
        {
            if (pc.Is(CustomRoles.GM)
                || (pc.HasSubRole() && pc.GetCustomSubRoles().Count >= Options.NoLimitAddonsNumMax.GetInt())
                || pc.Is(CustomRoles.Dictator)
                || pc.Is(CustomRoles.God)
                || pc.Is(CustomRoles.Hater)
                || pc.Is(CustomRoles.Sunnyboy)
                || pc.Is(CustomRoles.Bomber)
                || pc.Is(CustomRoles.Provocateur)
                || pc.Is(CustomRoles.RuthlessRomantic)
                || pc.Is(CustomRoles.Romantic)
                || pc.Is(CustomRoles.VengefulRomantic)
                || pc.Is(CustomRoles.Workaholic)
                || pc.Is(CustomRoles.Solsticer)
                || pc.Is(CustomRoles.Mini)
                || pc.Is(CustomRoles.NiceMini)
                || pc.Is(CustomRoles.EvilMini)
                || (pc.GetCustomRole().IsCrewmate() && !Options.CrewCanBeInLove.GetBool())
                || (pc.GetCustomRole().IsNeutral() && !Options.NeutralCanBeInLove.GetBool())
                || (pc.GetCustomRole().IsImpostor() && !Options.ImpCanBeInLove.GetBool()))
                continue;

            allPlayers.Add(pc);
        }
        var role = CustomRoles.Lovers;
        var count = Math.Clamp(RawCount, 0, allPlayers.Count);
        if (RawCount == -1) count = Math.Clamp(role.GetCount(), 0, allPlayers.Count);
        if (count <= 0 || allPlayers.Count <= 1) return;
        for (var i = 0; i < count; i++)
        {
            var player = allPlayers.RandomElement();
            Main.LoversPlayers.Add(player);
            allPlayers.Remove(player);
            Main.PlayerStates[player.PlayerId].SetSubRole(role);
            Logger.Info($"Registered Lovers: {player?.Data?.PlayerName} = {player.GetCustomRole()} + {role}", "Assign Lovers");
        }
        if (Main.LoversPlayers.Any())
            RPC.SyncLoversPlayers();
    }
}
