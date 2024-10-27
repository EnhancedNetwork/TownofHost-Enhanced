using AmongUs.GameOptions;
using Hazel;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using TOHE.Roles.AddOns.Crewmate;
using TOHE.Roles.AddOns.Impostor;
using TOHE.Roles.Neutral;

namespace TOHE;

[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.AddTasksFromList))]
class AddTasksFromListPatch
{
    public static void Prefix(ShipStatus __instance,
        [HarmonyArgument(4)] Il2CppSystem.Collections.Generic.List<NormalPlayerTask> unusedTasks)
    {
        if (!AmongUsClient.Instance.AmHost || __instance == null) return;

        if (!Options.DisableShortTasks.GetBool() && !Options.DisableCommonTasks.GetBool() && !Options.DisableLongTasks.GetBool() && !Options.DisableOtherTasks.GetBool()) return;
        
        List<NormalPlayerTask> disabledTasks = [];

        foreach (var task in unusedTasks.GetFastEnumerator())
        {
            switch (task.TaskType)
            {
                case TaskTypes.SwipeCard when Options.DisableSwipeCard.GetBool():
                case TaskTypes.SubmitScan when Options.DisableSubmitScan.GetBool():
                case TaskTypes.UnlockSafe when Options.DisableUnlockSafe.GetBool():
                case TaskTypes.UploadData when Options.DisableUploadData.GetBool():
                case TaskTypes.StartReactor when Options.DisableStartReactor.GetBool():
                case TaskTypes.ResetBreakers when Options.DisableResetBreaker.GetBool():
                case TaskTypes.VentCleaning when Options.DisableCleanVent.GetBool():
                case TaskTypes.CalibrateDistributor when Options.DisableCalibrateDistributor.GetBool():
                case TaskTypes.ChartCourse when Options.DisableChartCourse.GetBool():
                case TaskTypes.StabilizeSteering when Options.DisableStabilizeSteering.GetBool():
                case TaskTypes.CleanO2Filter when Options.DisableCleanO2Filter.GetBool():
                case TaskTypes.UnlockManifolds when Options.DisableUnlockManifolds.GetBool():
                case TaskTypes.PrimeShields when Options.DisablePrimeShields.GetBool():
                case TaskTypes.MeasureWeather when Options.DisableMeasureWeather.GetBool():
                case TaskTypes.BuyBeverage when Options.DisableBuyBeverage.GetBool():
                case TaskTypes.AssembleArtifact when Options.DisableAssembleArtifact.GetBool():
                case TaskTypes.ProcessData when Options.DisableProcessData.GetBool():
                case TaskTypes.RunDiagnostics when Options.DisableRunDiagnostics.GetBool():
                case TaskTypes.RepairDrill when Options.DisableRepairDrill.GetBool():
                case TaskTypes.AlignTelescope when Options.DisableAlignTelescope.GetBool():
                case TaskTypes.RecordTemperature when Options.DisableRecordTemperature.GetBool():
                case TaskTypes.FillCanisters when Options.DisableFillCanisters.GetBool():
                case TaskTypes.MonitorOxygen when Options.DisableMonitorTree.GetBool():
                case TaskTypes.StoreArtifacts when Options.DisableStoreArtifacts.GetBool():
                case TaskTypes.PutAwayPistols when Options.DisablePutAwayPistols.GetBool():
                case TaskTypes.PutAwayRifles when Options.DisablePutAwayRifles.GetBool():
                case TaskTypes.MakeBurger when Options.DisableMakeBurger.GetBool():
                case TaskTypes.CleanToilet when Options.DisableCleanToilet.GetBool():
                case TaskTypes.Decontaminate when Options.DisableDecontaminate.GetBool():
                case TaskTypes.SortRecords when Options.DisableSortRecords.GetBool():
                case TaskTypes.FixShower when Options.DisableFixShower.GetBool():
                case TaskTypes.PickUpTowels when Options.DisablePickUpTowels.GetBool():
                case TaskTypes.PolishRuby when Options.DisablePolishRuby.GetBool():
                case TaskTypes.DressMannequin when Options.DisableDressMannequin.GetBool():
                case TaskTypes.AlignEngineOutput when Options.DisableAlignEngineOutput.GetBool():
                case TaskTypes.InspectSample when Options.DisableInspectSample.GetBool():
                case TaskTypes.EmptyChute when Options.DisableEmptyChute.GetBool():
                case TaskTypes.ClearAsteroids when Options.DisableClearAsteroids.GetBool():
                case TaskTypes.WaterPlants when Options.DisableWaterPlants.GetBool():
                case TaskTypes.OpenWaterways when Options.DisableOpenWaterways.GetBool():
                case TaskTypes.ReplaceWaterJug when Options.DisableReplaceWaterJug.GetBool():
                case TaskTypes.RebootWifi when Options.DisableRebootWifi.GetBool():
                case TaskTypes.DevelopPhotos when Options.DisableDevelopPhotos.GetBool():
                case TaskTypes.RewindTapes when Options.DisableRewindTapes.GetBool():
                case TaskTypes.StartFans when Options.DisableStartFans.GetBool():
                case TaskTypes.FixWiring when Options.DisableFixWiring.GetBool():
                case TaskTypes.EnterIdCode when Options.DisableEnterIdCode.GetBool():
                case TaskTypes.InsertKeys when Options.DisableInsertKeys.GetBool():
                case TaskTypes.ScanBoardingPass when Options.DisableScanBoardingPass.GetBool():
                case TaskTypes.EmptyGarbage when Options.DisableEmptyGarbage.GetBool():
                case TaskTypes.FuelEngines when Options.DisableFuelEngines.GetBool():
                case TaskTypes.DivertPower when Options.DisableDivertPower.GetBool():
                case TaskTypes.FixWeatherNode when Options.DisableActivateWeatherNodes.GetBool(): // Activate Weather Nodes
                case TaskTypes.RoastMarshmallow when Options.DisableRoastMarshmallow.GetBool():
                case TaskTypes.CollectSamples when Options.DisableCollectSamples.GetBool():
                case TaskTypes.ReplaceParts when Options.DisableReplaceParts.GetBool():
                case TaskTypes.CollectVegetables when Options.DisableCollectVegetables.GetBool():
                case TaskTypes.MineOres when Options.DisableMineOres.GetBool():
                case TaskTypes.ExtractFuel when Options.DisableExtractFuel.GetBool():
                case TaskTypes.CatchFish when Options.DisableCatchFish.GetBool():
                case TaskTypes.PolishGem when Options.DisablePolishGem.GetBool():
                case TaskTypes.HelpCritter when Options.DisableHelpCritter.GetBool():
                case TaskTypes.HoistSupplies when Options.DisableHoistSupplies.GetBool():
                case TaskTypes.FixAntenna when Options.DisableFixAntenna.GetBool():
                case TaskTypes.BuildSandcastle when Options.DisableBuildSandcastle.GetBool():
                case TaskTypes.CrankGenerator when Options.DisableCrankGenerator.GetBool():
                case TaskTypes.MonitorMushroom when Options.DisableMonitorMushroom.GetBool():
                case TaskTypes.PlayVideogame when Options.DisablePlayVideoGame.GetBool():
                case TaskTypes.TuneRadio when Options.DisableFindSignal.GetBool(): // Find Signal
                case TaskTypes.TestFrisbee when Options.DisableThrowFisbee.GetBool(): // Throw Fisbee
                case TaskTypes.LiftWeights when Options.DisableLiftWeights.GetBool():
                case TaskTypes.CollectShells when Options.DisableCollectShells.GetBool():
                    disabledTasks.Add(task);
                    break;
            }
        }
        foreach (var task in disabledTasks.ToArray())
        {
            Logger.Msg($"Deletion: {task.TaskType}", "Disable Tasks");
            unusedTasks.Remove(task);
        }
    }
}

[HarmonyPatch(typeof(NetworkedPlayerInfo), nameof(NetworkedPlayerInfo.RpcSetTasks))]
class RpcSetTasksPatch
{
    // Patch to overwrite the task just before the process of allocating the task and sending the RPC is performed
    // Does not interfere with the vanilla task allocation process itself
    public static void Prefix(NetworkedPlayerInfo __instance, [HarmonyArgument(0)] ref Il2CppStructArray<byte> taskTypeIds)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (GameStates.IsHideNSeek) return;

        // null measure
        if (Main.RealOptionsData == null)
        {
            Logger.Warn("Warning: RealOptionsData is null", "RpcSetTasksPatch");
            return;
        }

        var pc = __instance.Object;
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
            NumShortTasks = Madmate.MadSnitchTasks.GetInt();
        }

        // GM - no have tasks, Lazy Gay and Lazy have 1 task, FFA all are killers so need to assign any tasks
        if (pc.Is(CustomRoles.GM) || pc.Is(CustomRoles.LazyGuy) || pc.Is(CustomRoles.Lazy) || Options.CurrentGameMode == CustomGameMode.FFA)
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
        TasksList = Shuffle(TasksList);

        // A HashSet into which allocated tasks can be placed
        // Prevents multiple assignments of the same task
        Il2CppSystem.Collections.Generic.HashSet<TaskTypes> usedTaskTypes = new();
        int start2 = 0;
        int start3 = 0;

        // List of long tasks that can be assigned
        Il2CppSystem.Collections.Generic.List<NormalPlayerTask> LongTasks = new();
        foreach (var task in ShipStatus.Instance.LongTasks)
            LongTasks.Add(task);
        LongTasks = Shuffle(LongTasks);

        // List of short tasks that can be assigned
        Il2CppSystem.Collections.Generic.List<NormalPlayerTask> ShortTasks = new();
        foreach (var task in ShipStatus.Instance.ShortTasks)
            ShortTasks.Add(task);
        ShortTasks = Shuffle(ShortTasks);

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
    public static Il2CppSystem.Collections.Generic.List<T> Shuffle<T>(Il2CppSystem.Collections.Generic.List<T> list)
    {
        int listCount = list.Count;
        while (listCount > 1)
        {
            listCount--;
            int k = IRandom.Instance.Next(listCount + 1);
            (list[listCount], list[k]) = (list[k], list[listCount]);
        }
        return list;
    }
}

[HarmonyPatch(typeof(NetworkedPlayerInfo), nameof(NetworkedPlayerInfo.HandleRpc))]
class HandleRpcPatch
{
    public static bool Prefix(NetworkedPlayerInfo __instance, [HarmonyArgument(0)] byte callId, [HarmonyArgument(1)] MessageReader reader)
    {
        //MessageReader sr = MessageReader.Get(reader);
        // var rpc = (RpcCalls)callId;

        if (AmongUsClient.Instance.AmHost)
        {
            Logger.Error($"Received Rpc {(RpcCalls)callId} for {__instance.Object.GetRealName()}({__instance.PlayerId}), which is impossible.", "TaskAssignPatch");

            EAC.WarnHost();
            return false;
        }

        return true;
    }
}
