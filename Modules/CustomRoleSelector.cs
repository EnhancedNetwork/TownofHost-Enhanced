using AmongUs.GameOptions;
using System;
using System.Collections.Generic;
using System.Linq;
using TOHE.Roles.Double;
using TOHE.Roles.Impostor;
using TOHE.Roles.Neutral;
using static TOHE.Translator;

namespace TOHE.Modules;

internal class CustomRoleSelector
{
    public static Dictionary<PlayerControl, CustomRoles> RoleResult;
    public static IReadOnlyList<CustomRoles> AllRoles => RoleResult.Values.ToList();

    public static void SelectCustomRoles()
    {
        // 开始职业抽取
        RoleResult = new();
        var rd = IRandom.Instance;
        int playerCount = Main.AllAlivePlayerControls.Count();
        int optImpNum = Main.RealOptionsData.GetInt(Int32OptionNames.NumImpostors);
        int optNonNeutralKillingNum = 0;
        int optNeutralKillingNum = 0;

        if (Options.NonNeutralKillingRolesMaxPlayer.GetInt() > 0 && Options.NonNeutralKillingRolesMaxPlayer.GetInt() >= Options.NonNeutralKillingRolesMinPlayer.GetInt())
        {
            optNonNeutralKillingNum = rd.Next(Options.NonNeutralKillingRolesMinPlayer.GetInt(), Options.NonNeutralKillingRolesMaxPlayer.GetInt() + 1);
        }
        if (Options.NeutralKillingRolesMaxPlayer.GetInt() > 0 && Options.NeutralKillingRolesMaxPlayer.GetInt() >= Options.NeutralKillingRolesMinPlayer.GetInt())
        {
            optNeutralKillingNum = rd.Next(Options.NeutralKillingRolesMinPlayer.GetInt(), Options.NeutralKillingRolesMaxPlayer.GetInt() + 1);
        }

        int readyRoleNum = 0;
        int readyNonNeutralKillingNum = 0;
        int readyNeutralKillingNum = 0;

        List<CustomRoles> rolesToAssign = new();
        List<CustomRoles> roleList = new();
        List<CustomRoles> roleOnList = new();

        List<CustomRoles> ImpOnList = new();
        List<CustomRoles> MiniOnList = new();
        List<CustomRoles> ImpRateList = new();
        List<CustomRoles> MiniRateList = new();

        List<CustomRoles> NonNeutralKillingOnList = new();
        List<CustomRoles> NonNeutralKillingRateList = new();

        List<CustomRoles> NeutralKillingOnList = new();
        List<CustomRoles> NeutralKillingRateList = new();

        List<CustomRoles> roleRateList = new();

        foreach (var cr in CustomRolesHelper.AllRoles)
        {
            var role = (CustomRoles)Enum.Parse(typeof(CustomRoles), cr.ToString());
            if (role.IsVanilla() || role.IsAdditionRole()) continue;
            if (role is CustomRoles.GM or CustomRoles.NotAssigned) continue;
            for (int i = 0; i < role.GetCount(); i++)
                roleList.Add(role);
        }

        // 职业设置为：优先
        foreach (var role in roleList) if (role.GetMode() == 2)
        {
            if (role.IsImpostor()) ImpOnList.Add(role);
            else if (role.IsMini()) MiniOnList.Add(role);
            else if (role.IsNonNK()) NonNeutralKillingOnList.Add(role);
            else if (role.IsNK()) NeutralKillingOnList.Add(role);
            else roleOnList.Add(role);
        }
        // 职业设置为：启用
        foreach (var role in roleList) if (role.GetMode() == 1)
        {
            if (role.IsImpostor()) ImpRateList.Add(role);
            else if (role.IsMini()) MiniRateList.Add(role);
            else if (role.IsNonNK()) NonNeutralKillingRateList.Add(role);
            else if (role.IsNK()) NeutralKillingRateList.Add(role);
            else roleRateList.Add(role);
        }

        while (MiniOnList.Count == 1)
        {
            var select = MiniOnList[rd.Next(0, MiniOnList.Count)];
            MiniOnList.Remove(select);
            Mini.SetMiniTeam(Mini.EvilMiniSpawnChances.GetFloat());
            if (!Mini.IsEvilMini)
            {
                roleOnList.Add(CustomRoles.NiceMini);
            }
            if (Mini.IsEvilMini)
            {
                ImpOnList.Add(CustomRoles.EvilMini);
            }
        }
        while (MiniRateList.Count == 1)
        {
            var select = MiniRateList[rd.Next(0, MiniRateList.Count)];
            MiniRateList.Remove(select);
            Mini.SetMiniTeam(Mini.EvilMiniSpawnChances.GetFloat());
            if (!Mini.IsEvilMini)
            {
                roleRateList.Add(CustomRoles.NiceMini);
            }
            if (Mini.IsEvilMini)
            {
                ImpRateList.Add(CustomRoles.EvilMini);
            }
        }

        // 抽取优先职业（内鬼）
        while (ImpOnList.Any())
        {
            var select = ImpOnList[rd.Next(0, ImpOnList.Count)];
            ImpOnList.Remove(select);
            rolesToAssign.Add(select);
            readyRoleNum++;
            Logger.Info(select.ToString() + " Add to Impostor waiting list (priority)", "CustomRoleSelector");
            if (readyRoleNum >= playerCount) goto EndOfAssign;
            if (readyRoleNum >= optImpNum) break;
        }
        // 优先职业不足以分配，开始分配启用的职业（内鬼）
        if (readyRoleNum < playerCount && readyRoleNum < optImpNum)
        {
            while (ImpRateList.Any())
            {
                var select = ImpRateList[rd.Next(0, ImpRateList.Count)];
                ImpRateList.Remove(select);
                rolesToAssign.Add(select);
                readyRoleNum++;
                Logger.Info(select.ToString() + " Add to Impostor waiting list", "CustomRoleSelector");
                if (readyRoleNum >= playerCount) goto EndOfAssign;
                if (readyRoleNum >= optImpNum) break;
            }
        }

        // Select NonNeutralKilling "Always"
        while (NonNeutralKillingOnList.Any() && optNonNeutralKillingNum > 0)
        {
            var select = NonNeutralKillingOnList[rd.Next(0, NonNeutralKillingOnList.Count)];
            NonNeutralKillingOnList.Remove(select);
            rolesToAssign.Add(select);
            readyRoleNum++;
            readyNonNeutralKillingNum += select.GetCount();
            Logger.Info(select.ToString() + " Add to Non NeutralKilling waiting list (priority)", "CustomRoleSelector");
            if (readyRoleNum >= playerCount) goto EndOfAssign;
            if (readyNonNeutralKillingNum >= optNonNeutralKillingNum) break;
        }

        // Select NonNeutralKilling "Random"
        if (readyRoleNum < playerCount && readyNonNeutralKillingNum < optNonNeutralKillingNum)
        {
            while (NonNeutralKillingRateList.Any() && optNonNeutralKillingNum > 0)
            {
                var select = NonNeutralKillingRateList[rd.Next(0, NonNeutralKillingRateList.Count)];
                NonNeutralKillingRateList.Remove(select);
                rolesToAssign.Add(select);
                readyRoleNum++;
                readyNonNeutralKillingNum += select.GetCount();
                Logger.Info(select.ToString() + "Add to Non Neutral Killing waiting list", "CustomRoleSelector");
                if (readyRoleNum >= playerCount) goto EndOfAssign;
                if (readyNonNeutralKillingNum >= optNonNeutralKillingNum) break;
            }
        }

        // Select NeutralKilling "Always"
        while (NeutralKillingOnList.Any() && optNeutralKillingNum > 0)
        {
            var select = NeutralKillingOnList[rd.Next(0, NeutralKillingOnList.Count)];
            NeutralKillingOnList.Remove(select);
            rolesToAssign.Add(select);
            readyRoleNum++;
            readyNeutralKillingNum += select.GetCount();
            Logger.Info(select.ToString() + " Add to NeutralKilling waiting list (priority)", "CustomRoleSelector");
            if (readyRoleNum >= playerCount) goto EndOfAssign;
            if (readyNeutralKillingNum >= optNeutralKillingNum) break;
        }

        // Select NeutralKilling "Random"
        if (readyRoleNum < playerCount && readyNeutralKillingNum < optNeutralKillingNum)
        {
            while (NeutralKillingRateList.Any() && optNeutralKillingNum > 0)
            {
                var select = NeutralKillingRateList[rd.Next(0, NeutralKillingRateList.Count)];
                NeutralKillingRateList.Remove(select);
                rolesToAssign.Add(select);
                readyRoleNum++;
                readyNeutralKillingNum += select.GetCount();
                Logger.Info(select.ToString() + " Add to NeutralKilling waiting list", "CustomRoleSelector");
                if (readyRoleNum >= playerCount) goto EndOfAssign;
                if (readyNeutralKillingNum >= optNeutralKillingNum) break;
            }
        }

        // 抽取优先职业
        while (roleOnList.Any())
        {
            var select = roleOnList[rd.Next(0, roleOnList.Count)];
            roleOnList.Remove(select);
            rolesToAssign.Add(select);
            readyRoleNum++;
            Logger.Info(select.ToString() + " Add to Crewmate waiting list (preferred)", "CustomRoleSelector");
            if (readyRoleNum >= playerCount) goto EndOfAssign;
        }
        // 优先职业不足以分配，开始分配启用的职业
        if (readyRoleNum < playerCount)
        {
            while (roleRateList.Any())
            {
                var select = roleRateList[rd.Next(0, roleRateList.Count)];
                roleRateList.Remove(select);
                rolesToAssign.Add(select);
                readyRoleNum++;
                Logger.Info(select.ToString() + " Add to Crewmate waiting list", "CustomRoleSelector");
                if (readyRoleNum >= playerCount) goto EndOfAssign;
            }
        }

    // 职业抽取结束
    EndOfAssign:

        // 隐藏职业
        {
            if (rd.Next(0, 100) < Options.SunnyboyChance.GetInt() && rolesToAssign.Remove(CustomRoles.Jester)) rolesToAssign.Add(CustomRoles.Sunnyboy);
        }
        {
            if (rd.Next(0, 100) < Sans.BardChance.GetInt() && rolesToAssign.Remove(CustomRoles.Sans)) rolesToAssign.Add(CustomRoles.Bard);
        }
        {
            if (rd.Next(0, 100) < Vampire.VampiressChance.GetInt() && rolesToAssign.Remove(CustomRoles.Vampire)) rolesToAssign.Add(CustomRoles.Vampiress);
        }
        {
            if (rd.Next(0, 100) < Options.NukerChance.GetInt() && rolesToAssign.Remove(CustomRoles.Bomber)) rolesToAssign.Add(CustomRoles.Nuker);
        }
        if (NSerialKiller.HasSerialKillerBuddy.GetBool() && rolesToAssign.Contains(CustomRoles.NSerialKiller))
        {
            if (rd.Next(0, 100) < NSerialKiller.ChanceToSpawn.GetInt()) rolesToAssign.Add(CustomRoles.NSerialKiller);
            //if (rd.Next(0, 100) < NSerialKiller.ChanceToSpawnAnother.GetInt()) rolesToAssign.Add(CustomRoles.NSerialKiller);
        }

        if (Options.NeutralKillingRolesMaxPlayer.GetInt() > 1 && !Options.TemporaryAntiBlackoutFix.GetBool())
        {
            _ = new LateTask(() =>
                {
                    Logger.SendInGame(GetString("NeutralKillingBlackoutWarning"));
                }, 4f, "Neutral Killing Blackout Warning");

        }

        if (Romantic.IsEnable)
        {
            if (rolesToAssign.Contains(CustomRoles.Romantic))
            {
                if (rolesToAssign.Contains(CustomRoles.Lovers))
                    rolesToAssign.Remove(CustomRoles.Lovers);
                if (rolesToAssign.Contains(CustomRoles.Ntr))
                    rolesToAssign.Remove(CustomRoles.Ntr);
            }
        }

        /*  if (!rolesToAssign.Contains(CustomRoles.Lovers) && rolesToAssign.Contains(CustomRoles.FFF) || !rolesToAssign.Contains(CustomRoles.Ntr) && rolesToAssign.Contains(CustomRoles.FFF))
              rolesToAssign.Remove(CustomRoles.FFF); 
              rolesToAssign.Add(CustomRoles.Jester); */

        /*   if (!Options.DisableSaboteur.GetBool()) // no longer hidden
           {
               if (rd.Next(0, 100) < 25 && rolesToAssign.Remove(CustomRoles.Inhibitor)) rolesToAssign.Add(CustomRoles.Saboteur);
           } */

        // EAC封禁名单玩家开房将被分配为小丑
        if (BanManager.CheckEACList(PlayerControl.LocalPlayer.FriendCode, PlayerControl.LocalPlayer.GetClient().GetHashedPuid()))
        {
            if (!rolesToAssign.Contains(CustomRoles.Jester))
                rolesToAssign.Add(CustomRoles.Jester);
            Main.DevRole.Remove(PlayerControl.LocalPlayer.PlayerId);
            Main.DevRole.Add(PlayerControl.LocalPlayer.PlayerId, CustomRoles.Jester);
        }

        // Dev Roles List Edit
        foreach (var dr in Main.DevRole)
        {
            if (dr.Key == PlayerControl.LocalPlayer.PlayerId && Main.EnableGM.Value) continue;
            if (rolesToAssign.Contains(dr.Value))
            {
                rolesToAssign.Remove(dr.Value);
                rolesToAssign.Insert(0, dr.Value);
                Logger.Info("Role list improved priority：" + dr.Value, "Dev Role");
                continue;
            }
            for (int i = 0; i < rolesToAssign.Count; i++)
            {
                var role = rolesToAssign[i];
                if (dr.Value.GetMode() != role.GetMode()) continue;
                if (
                    (dr.Value.IsMini() && role.IsMini()) ||
                    (dr.Value.IsImpostor() && role.IsImpostor()) ||
                    (dr.Value.IsNonNK() && role.IsNonNK()) ||
                    (dr.Value.IsNK() && role.IsNK()) ||
                    (dr.Value.IsCrewmate() & role.IsCrewmate())
                    )
                {
                    rolesToAssign.RemoveAt(i);
                    rolesToAssign.Insert(0, dr.Value);
                    Logger.Info("Coverage role list：" + i + " " + role.ToString() + " => " + dr.Value, "Dev Role");
                    break;
                }
            }
        }

        var AllPlayer = Main.AllAlivePlayerControls.ToList();

        while (AllPlayer.Any() && rolesToAssign.Any())
        {
            PlayerControl delPc = null;
            foreach (var pc in AllPlayer)
                foreach (var dr in Main.DevRole.Where(x => pc.PlayerId == x.Key))
                {
                    if (dr.Key == PlayerControl.LocalPlayer.PlayerId && Main.EnableGM.Value) continue;
                    var id = rolesToAssign.IndexOf(dr.Value);
                    if (id == -1) continue;
                    RoleResult.Add(pc, rolesToAssign[id]);
                    Logger.Info($"Role Priority Assignment：{AllPlayer[0].GetRealName()} => {rolesToAssign[id]}", "CustomRoleSelector");
                    delPc = pc;
                    rolesToAssign.RemoveAt(id);
                    goto EndOfWhile;
                }

            var roleId = rd.Next(0, rolesToAssign.Count);
            RoleResult.Add(AllPlayer[0], rolesToAssign[roleId]);
            Logger.Info($"Role grouping：{AllPlayer[0].GetRealName()} => {rolesToAssign[roleId]}", "CustomRoleSelector");
            AllPlayer.RemoveAt(0);
            rolesToAssign.RemoveAt(roleId);

        EndOfWhile:;

            if (delPc != null)
            {
                AllPlayer.Remove(delPc);
                Main.DevRole.Remove(delPc.PlayerId);
            }
        }

        if (AllPlayer.Any())
            Logger.Error("Role assignment error: There are players who have not been assigned role", "CustomRoleSelector");
        if (rolesToAssign.Any())
            Logger.Error("Role assignment error: There is an unassigned role", "CustomRoleSelector");

    }

    public static int addScientistNum = 0;
    public static int addEngineerNum = 0;
    public static int addShapeshifterNum = 0;
    public static void CalculateVanillaRoleCount()
    {
        // 计算原版特殊职业数量
        addEngineerNum = 0;
        addScientistNum = 0;
        addShapeshifterNum = 0;
        foreach (var role in AllRoles)
        {
            switch (CustomRolesHelper.GetVNRole(role))
            {
                case CustomRoles.Scientist: addScientistNum++; break;
                case CustomRoles.Engineer: addEngineerNum++; break;
                case CustomRoles.Shapeshifter: addShapeshifterNum++; break;
            }
        }
    }

    public static List<CustomRoles> AddonRolesList = new();
    public static void SelectAddonRoles()
    {
        AddonRolesList = new();
        foreach (var cr in CustomRolesHelper.AllRoles)
        {
            CustomRoles role = (CustomRoles)Enum.Parse(typeof(CustomRoles), cr.ToString());
            if (!role.IsAdditionRole()) continue;
            if (role is CustomRoles.Madmate && Options.MadmateSpawnMode.GetInt() != 0) continue;
            if (role is CustomRoles.Lovers or CustomRoles.LastImpostor or CustomRoles.Workhorse) continue;
            AddonRolesList.Add(role);
        }
    }
}