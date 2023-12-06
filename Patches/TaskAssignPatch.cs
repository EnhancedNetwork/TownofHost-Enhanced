using AmongUs.GameOptions;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using System.Collections.Generic;
using System.Linq;
using TOHE.Roles.AddOns.Crewmate;
using TOHE.Roles.Neutral;

namespace TOHE;

[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.AddTasksFromList))]
class AddTasksFromListPatch
{
    public static void Prefix(ShipStatus __instance,
        [HarmonyArgument(4)] Il2CppSystem.Collections.Generic.List<NormalPlayerTask> unusedTasks)
    {
        if (!AmongUsClient.Instance.AmHost) return;

        if (!Options.DisableShortTasks.GetBool() && !Options.DisableCommonTasks.GetBool() && !Options.DisableLongTasks.GetBool() && !Options.DisableOtherTasks.GetBool()) return;
        
        List<NormalPlayerTask> disabledTasks = new();

        for (var i = 0; i < unusedTasks.Count; i++)
        {
            var task = unusedTasks[i];
            if (task.TaskType == TaskTypes.SwipeCard && Options.DisableSwipeCard.GetBool()) disabledTasks.Add(task);//SwipeCard task
            if (task.TaskType == TaskTypes.SubmitScan && Options.DisableSubmitScan.GetBool()) disabledTasks.Add(task);//SubmitScan tast
            if (task.TaskType == TaskTypes.UnlockSafe && Options.DisableUnlockSafe.GetBool()) disabledTasks.Add(task);//UnlockSafe task
            if (task.TaskType == TaskTypes.UploadData && Options.DisableUploadData.GetBool()) disabledTasks.Add(task);//UploadData task
            if (task.TaskType == TaskTypes.StartReactor && Options.DisableStartReactor.GetBool()) disabledTasks.Add(task);//StartReactor task
            if (task.TaskType == TaskTypes.ResetBreakers && Options.DisableResetBreaker.GetBool()) disabledTasks.Add(task);//ResetBreakers task
            if (task.TaskType == TaskTypes.VentCleaning && Options.DisableCleanVent.GetBool()) disabledTasks.Add(task);//CleanVent task
            if (task.TaskType == TaskTypes.CalibrateDistributor && Options.DisableCalibrateDistributor.GetBool()) disabledTasks.Add(task);//CalibrateDistributor task
            if (task.TaskType == TaskTypes.ChartCourse && Options.DisableChartCourse.GetBool()) disabledTasks.Add(task);//ChartCourse task
            if (task.TaskType == TaskTypes.StabilizeSteering && Options.DisableStabilizeSteering.GetBool()) disabledTasks.Add(task);//StabilizeSteering task
            if (task.TaskType == TaskTypes.CleanO2Filter && Options.DisableCleanO2Filter.GetBool()) disabledTasks.Add(task);//CleanO2Filter task
            if (task.TaskType == TaskTypes.UnlockManifolds && Options.DisableUnlockManifolds.GetBool()) disabledTasks.Add(task);//UnlockManifolds task
            if (task.TaskType == TaskTypes.PrimeShields && Options.DisablePrimeShields.GetBool()) disabledTasks.Add(task);//PrimeShields task
            if (task.TaskType == TaskTypes.MeasureWeather && Options.DisableMeasureWeather.GetBool()) disabledTasks.Add(task);//MeasureWeather task
            if (task.TaskType == TaskTypes.BuyBeverage && Options.DisableBuyBeverage.GetBool()) disabledTasks.Add(task);//BuyBeverage task
            if (task.TaskType == TaskTypes.AssembleArtifact && Options.DisableAssembleArtifact.GetBool()) disabledTasks.Add(task);//AssembleArtifact task
            if (task.TaskType == TaskTypes.SortSamples && Options.DisableSortSamples.GetBool()) disabledTasks.Add(task);//SortSamples task
            if (task.TaskType == TaskTypes.ProcessData && Options.DisableProcessData.GetBool()) disabledTasks.Add(task);//ProcessData task
            if (task.TaskType == TaskTypes.RunDiagnostics && Options.DisableRunDiagnostics.GetBool()) disabledTasks.Add(task);//RunDiagnostics task
            if (task.TaskType == TaskTypes.RepairDrill && Options.DisableRepairDrill.GetBool()) disabledTasks.Add(task);//RepairDrill task
            if (task.TaskType == TaskTypes.AlignTelescope && Options.DisableAlignTelescope.GetBool()) disabledTasks.Add(task);//AlignTelescope task
            if (task.TaskType == TaskTypes.RecordTemperature && Options.DisableRecordTemperature.GetBool()) disabledTasks.Add(task);//RecordTemperature task
            if (task.TaskType == TaskTypes.FillCanisters && Options.DisableFillCanisters.GetBool()) disabledTasks.Add(task);//FillCanisters task
            if (task.TaskType == TaskTypes.MonitorOxygen && Options.DisableMonitorTree.GetBool()) disabledTasks.Add(task);//MonitorTree task
            if (task.TaskType == TaskTypes.StoreArtifacts && Options.DisableStoreArtifacts.GetBool()) disabledTasks.Add(task);//StoreArtifacts task
            if (task.TaskType == TaskTypes.PutAwayPistols && Options.DisablePutAwayPistols.GetBool()) disabledTasks.Add(task);//PutAwayPistols task
            if (task.TaskType == TaskTypes.PutAwayRifles && Options.DisablePutAwayRifles.GetBool()) disabledTasks.Add(task);//PutAwayRifles task
            if (task.TaskType == TaskTypes.MakeBurger && Options.DisableMakeBurger.GetBool()) disabledTasks.Add(task);//MakeBurger task
            if (task.TaskType == TaskTypes.CleanToilet && Options.DisableCleanToilet.GetBool()) disabledTasks.Add(task);//CleanToilet task
            if (task.TaskType == TaskTypes.Decontaminate && Options.DisableDecontaminate.GetBool()) disabledTasks.Add(task);//Decontaminate task
            if (task.TaskType == TaskTypes.SortRecords && Options.DisableSortRecords.GetBool()) disabledTasks.Add(task);//SortRecords task
            if (task.TaskType == TaskTypes.FixShower && Options.DisableFixShower.GetBool()) disabledTasks.Add(task);//FixShower task
            if (task.TaskType == TaskTypes.PickUpTowels && Options.DisablePickUpTowels.GetBool()) disabledTasks.Add(task);//PickUpTowels task
            if (task.TaskType == TaskTypes.PolishRuby && Options.DisablePolishRuby.GetBool()) disabledTasks.Add(task);//PolishRuby task
            if (task.TaskType == TaskTypes.DressMannequin && Options.DisableDressMannequin.GetBool()) disabledTasks.Add(task);//DressMannequin task
            if (task.TaskType == TaskTypes.AlignEngineOutput && Options.DisableAlignEngineOutput.GetBool()) disabledTasks.Add(task);//AlignEngineOutput task
            if (task.TaskType == TaskTypes.InspectSample && Options.DisableInspectSample.GetBool()) disabledTasks.Add(task);//InspectSample task
            if (task.TaskType == TaskTypes.EmptyChute && Options.DisableEmptyChute.GetBool()) disabledTasks.Add(task);//EmptyChute task
            if (task.TaskType == TaskTypes.ClearAsteroids && Options.DisableClearAsteroids.GetBool()) disabledTasks.Add(task);//ClearAsteroids task
            if (task.TaskType == TaskTypes.WaterPlants && Options.DisableWaterPlants.GetBool()) disabledTasks.Add(task);//WaterPlants task
            if (task.TaskType == TaskTypes.OpenWaterways && Options.DisableOpenWaterways.GetBool()) disabledTasks.Add(task);//OpenWaterways task
            if (task.TaskType == TaskTypes.ReplaceWaterJug && Options.DisableReplaceWaterJug.GetBool()) disabledTasks.Add(task);//ReplaceWaterJug task
            if (task.TaskType == TaskTypes.RebootWifi && Options.DisableRebootWifi.GetBool()) disabledTasks.Add(task);//RebootWifi task
            if (task.TaskType == TaskTypes.DevelopPhotos && Options.DisableDevelopPhotos.GetBool()) disabledTasks.Add(task);//DevelopPhotos task
            if (task.TaskType == TaskTypes.RewindTapes && Options.DisableRewindTapes.GetBool()) disabledTasks.Add(task);//RewindTapes task
            if (task.TaskType == TaskTypes.StartFans && Options.DisableStartFans.GetBool()) disabledTasks.Add(task);//StartFans task
            if (task.TaskType == TaskTypes.FixWiring && Options.DisableFixWiring.GetBool()) disabledTasks.Add(task);//FixWiring task (nightmare)
            if (task.TaskType == TaskTypes.EnterIdCode && Options.DisableEnterIdCode.GetBool()) disabledTasks.Add(task);//EnterIdCode task
            if (task.TaskType == TaskTypes.InsertKeys && Options.DisableInsertKeys.GetBool()) disabledTasks.Add(task);//InsertKeys task
            if (task.TaskType == TaskTypes.ScanBoardingPass && Options.DisableScanBoardingPass.GetBool()) disabledTasks.Add(task);//ScanBoardingPass task
            if (task.TaskType == TaskTypes.EmptyGarbage && Options.DisableEmptyGarbage.GetBool()) disabledTasks.Add(task);//EmptyGarbage task
            if (task.TaskType == TaskTypes.FuelEngines && Options.DisableFuelEngines.GetBool()) disabledTasks.Add(task);//FuelEngines task
            if (task.TaskType == TaskTypes.DivertPower && Options.DisableDivertPower.GetBool()) disabledTasks.Add(task);//DivertPower task v1.0a
            if (task.TaskType == TaskTypes.FixWeatherNode && Options.DisableActivateWeatherNodes.GetBool()) disabledTasks.Add(task);//ActivateWeatherNodes task
            if (task.TaskType == TaskTypes.RoastMarshmallow && Options.DisableRoastMarshmallow.GetBool()) disabledTasks.Add(task);//Roast Marshmallow
            if (task.TaskType == TaskTypes.CollectSamples && Options.DisableCollectSamples.GetBool()) disabledTasks.Add(task);//Collect Samples
            if (task.TaskType == TaskTypes.ReplaceParts && Options.DisableReplaceParts.GetBool()) disabledTasks.Add(task);//Replace Parts
            if (task.TaskType == TaskTypes.CollectVegetables && Options.DisableCollectVegetables.GetBool()) disabledTasks.Add(task);//Collect Vegetables
            if (task.TaskType == TaskTypes.MineOres && Options.DisableMineOres.GetBool()) disabledTasks.Add(task);//Mine Ores
            if (task.TaskType == TaskTypes.ExtractFuel && Options.DisableExtractFuel.GetBool()) disabledTasks.Add(task);//Extract Fuel
            if (task.TaskType == TaskTypes.CatchFish && Options.DisableCatchFish.GetBool()) disabledTasks.Add(task);//Catch Fish
            if (task.TaskType == TaskTypes.PolishGem && Options.DisablePolishGem.GetBool()) disabledTasks.Add(task);//Polish Gem
            if (task.TaskType == TaskTypes.HelpCritter && Options.DisableHelpCritter.GetBool()) disabledTasks.Add(task);//Help Critter
            if (task.TaskType == TaskTypes.HoistSupplies && Options.DisableHoistSupplies.GetBool()) disabledTasks.Add(task);//Hoist Supplies
            if (task.TaskType == TaskTypes.FixAntenna && Options.DisableFixAntenna.GetBool()) disabledTasks.Add(task);//Fix Antenna
            if (task.TaskType == TaskTypes.BuildSandcastle && Options.DisableBuildSandcastle.GetBool()) disabledTasks.Add(task);//Build Sandcastle
            if (task.TaskType == TaskTypes.CrankGenerator && Options.DisableCrankGenerator.GetBool()) disabledTasks.Add(task);//Crank Generator
            if (task.TaskType == TaskTypes.MonitorMushroom && Options.DisableMonitorMushroom.GetBool()) disabledTasks.Add(task);//Monitor Mushroom
            if (task.TaskType == TaskTypes.PlayVideogame && Options.DisablePlayVideoGame.GetBool()) disabledTasks.Add(task);//Play Video Game
            if (task.TaskType == TaskTypes.TuneRadio && Options.DisableFindSignal.GetBool()) disabledTasks.Add(task);//Find Signal
            if (task.TaskType == TaskTypes.TestFrisbee && Options.DisableThrowFisbee.GetBool()) disabledTasks.Add(task);//Throw Fisbee
            if (task.TaskType == TaskTypes.LiftWeights && Options.DisableLiftWeights.GetBool()) disabledTasks.Add(task);//Lift Weights
            if (task.TaskType == TaskTypes.CollectShells && Options.DisableCollectShells.GetBool()) disabledTasks.Add(task);//Collect Shells
        }
        foreach (var task in disabledTasks.ToArray())
        {
            Logger.Msg("Deletion: " + task.TaskType.ToString(), "Disable Tasks");
            unusedTasks.Remove(task);
        }
    }
}

[HarmonyPatch(typeof(GameData), nameof(GameData.RpcSetTasks))]
class RpcSetTasksPatch
{
    // Patch to overwrite the task just before the process of allocating the task and sending the RPC is performed
    // Does not interfere with the vanilla task allocation process itself
    public static void Prefix(GameData __instance,
    [HarmonyArgument(0)] byte playerId,
    [HarmonyArgument(1)] ref Il2CppStructArray<byte> taskTypeIds)
    {
        // null measure
        if (Main.RealOptionsData == null)
        {
            Logger.Warn("Warning: RealOptionsData is null", "RpcSetTasksPatch");
            return;
        }

        var pc = Utils.GetPlayerById(playerId);
        CustomRoles? RoleNullable = pc?.GetCustomRole();
        if (RoleNullable == null) return;
        CustomRoles role = RoleNullable.Value;

        // Default number of tasks
        bool hasCommonTasks = true;
        int NumLongTasks = Main.NormalOptions.NumLongTasks;
        int NumShortTasks = Main.NormalOptions.NumShortTasks;

        if (Options.OverrideTasksData.AllData.TryGetValue(role, out var data) && data.doOverride.GetBool())
        {
            // whether to assign a common task (normal task) or not.
            // If assigned, it will not be reassigned and the same common task will be assigned as the other crew members.
            hasCommonTasks = data.assignCommonTasks.GetBool();

            NumLongTasks = data.numLongTasks.GetInt(); // Number of long tasks to assign
            NumShortTasks = data.numShortTasks.GetInt(); // Number of short tasks to allocate

            // Long and short tasks are always reallocated.
        }

        // Betrayal of whistleblower mission coverage
        if (pc.Is(CustomRoles.Snitch) && pc.Is(CustomRoles.Madmate))
        {
            hasCommonTasks = false;
            NumLongTasks = 0;
            NumShortTasks = Options.MadSnitchTasks.GetInt();
        }

        // GM - no have tasks, Lazy Gay and Lazy have 1 task, FFA all are killers so need to assign any tasks
        if (pc.Is(CustomRoles.GM) || pc.Is(CustomRoles.Needy) || pc.Is(CustomRoles.Lazy) || Options.CurrentGameMode == CustomGameMode.FFA)
        {
            hasCommonTasks = false;
            NumShortTasks = 0;
            NumLongTasks = 0;
        }

        if (pc.Is(CustomRoles.Workhorse))
        {
            (hasCommonTasks, NumLongTasks, NumShortTasks) = Workhorse.TaskData;
        }

        if (pc.Is(CustomRoles.Solsticer))
        {
            Solsticer.SetShortTasksToAdd();
            NumShortTasks += Solsticer.AddShortTasks;
            var taskState = pc.GetPlayerTaskState();
            taskState.AllTasksCount = NumShortTasks + NumLongTasks;
            hasCommonTasks = false;
        }

        // Capitalism is going to wreak havoc on people
        if (Main.CapitalismAssignTask.ContainsKey(playerId))
        {
            NumShortTasks += Main.CapitalismAssignTask[playerId];
            Main.CapitalismAssignTask.Remove(playerId);
        }

        if (taskTypeIds.Count == 0) hasCommonTasks = false; //Common to 0 when redistributing tasks
        if (!hasCommonTasks && NumLongTasks == 0 && NumShortTasks == 0) NumShortTasks = 1; //Task 0 Measures
        if (hasCommonTasks && NumLongTasks == Main.NormalOptions.NumLongTasks && NumShortTasks == Main.NormalOptions.NumShortTasks) return; //If there are no changes

        // A list containing the IDs of tasks that can be assigned
        // Clone of the second argument of the original RpcSetTasks
        Il2CppSystem.Collections.Generic.List<byte> TasksList = new();
        foreach (var num in taskTypeIds)
            TasksList.Add(num);

        // Reference:ShipStatus.Begin
        // Deleting unnecessary allocated tasks
        // Deleting tasks other than common tasks if common tasks are assigned
        // Empty the list if common tasks are not allocated
        int defaultCommonTasksNum = Main.RealOptionsData.GetInt(Int32OptionNames.NumCommonTasks);
        if (hasCommonTasks) TasksList.RemoveRange(defaultCommonTasksNum, TasksList.Count - defaultCommonTasksNum);
        else TasksList.Clear();

        // A HashSet into which allocated tasks can be placed
        // Prevents multiple assignments of the same task
        Il2CppSystem.Collections.Generic.HashSet<TaskTypes> usedTaskTypes = new();
        int start2 = 0;
        int start3 = 0;

        // List of long tasks that can be assigned
        Il2CppSystem.Collections.Generic.List<NormalPlayerTask> LongTasks = new();
        foreach (var task in ShipStatus.Instance.LongTasks.ToArray())
            LongTasks.Add(task);
        Shuffle<NormalPlayerTask>(LongTasks);

        // List of short tasks that can be assigned
        Il2CppSystem.Collections.Generic.List<NormalPlayerTask> ShortTasks = new();
        foreach (var task in ShipStatus.Instance.ShortTasks.ToArray())
            ShortTasks.Add(task);
        Shuffle<NormalPlayerTask>(ShortTasks);

        // Use the function to assign tasks that are actually used on the Among Us side
        ShipStatus.Instance.AddTasksFromList(
            ref start2,
            NumLongTasks,
            TasksList,
            usedTaskTypes,
            LongTasks
        );
        ShipStatus.Instance.AddTasksFromList(
            ref start3,
            NumShortTasks,
            TasksList,
            usedTaskTypes,
            ShortTasks
        );

        // Converts a list of tasks into an array (Il2CppStructArray)
        taskTypeIds = new Il2CppStructArray<byte>(TasksList.Count);
        for (int i = 0; i < TasksList.Count; i++)
        {
            taskTypeIds[i] = TasksList[i];
        }

    }
    public static void Shuffle<T>(Il2CppSystem.Collections.Generic.List<T> list)
    {
        for (int i = 0; i < list.Count - 1; i++)
        {
            T obj = list[i];
            int rand = UnityEngine.Random.Range(i, list.Count);
            list[i] = list[rand];
            list[rand] = obj;
        }
    }
}