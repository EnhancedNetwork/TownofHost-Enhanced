using AmongUs.GameOptions;
using AmongUs.InnerNet.GameDataMessages;
using Hazel;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using TOHE.Roles.AddOns.Crewmate;
using TOHE.Roles.AddOns.Impostor;
using TOHE.Roles.Neutral;
using TOHE.Modules;

namespace TOHE;

class DisableTasks
{
    public static void DoRemove(ref List<NormalPlayerTask> usedTasks)
    {
        if (!Options.DisableShortTasks.GetBool() && !Options.DisableCommonTasks.GetBool() && !Options.DisableLongTasks.GetBool() && !Options.DisableOtherTasks.GetBool()) return;

        List<NormalPlayerTask> disabledTasks = [];

        foreach (var task in usedTasks)
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
            usedTasks.Remove(task);
        }
    }
}

[HarmonyPatch(typeof(NetworkedPlayerInfo), nameof(NetworkedPlayerInfo.RpcSetTasks))]
class RpcSetTasksPatch
{
    // Patch to overwrite the task just before the process of allocating the task and sending the RPC is performed
    // Does not interfere with the vanilla task allocation process itself

    /* TO DO:
     * Try to make players get different tasks from each other
     * InnerSloth uses task pool to achieve this
     */

    public static List<byte> decidedCommonTasks = [];
    public static bool Prefix(NetworkedPlayerInfo __instance)
    {
        if (!AmongUsClient.Instance.AmHost) return false;
        if (GameStates.IsHideNSeek) return true;

        // Null measure
        if (Main.RealOptionsData == null)
        {
            Logger.Warn("Warning: RealOptionsData is null", "RpcSetTasksPatch");
            return true;
        }

        var pc = __instance.Object;
        CustomRoles? RoleNullable = pc?.GetCustomRole();
        if (RoleNullable == null) return true;
        CustomRoles role = RoleNullable.Value;

        // Default number of tasks
        bool hasCommonTasks = true;
        int NumLongTasks = Main.NormalOptions.NumLongTasks;
        int NumShortTasks = Main.NormalOptions.NumShortTasks;

        if (Options.OverrideTasksData.AllData.TryGetValue(role, out var data) && data.doOverride.GetBool())
        {
            // whether to assign a common task (normal task) or not
            // If assigned, it will not be reassigned and the same common task will be assigned as the other Crewmate members.
            hasCommonTasks = data.assignCommonTasks.GetBool();

            NumLongTasks = data.numLongTasks.GetInt(); // Number of long tasks to assign
            NumShortTasks = data.numShortTasks.GetInt(); // Number of short tasks to allocate

            // Long and short tasks are always reallocated
        }

        // Betrayal of whistleblower mission coverage
        if (pc.Is(CustomRoles.Snitch) && pc.Is(CustomRoles.Madmate))
        {
            hasCommonTasks = false;
            NumLongTasks = 0;
            NumShortTasks = Madmate.MadSnitchTasks.GetInt();
        }

        // GM - no have tasks, Lazy Gay and Lazy have 1 task, FFA all are killers so need to assign any tasks
        if (pc.Is(CustomRoles.GM) || pc.Is(CustomRoles.LazyGuy) || pc.Is(CustomRoles.Lazy) || Options.CurrentGameMode == CustomGameMode.FFA || Options.CurrentGameMode == CustomGameMode.UltimateTeam)
        {
            hasCommonTasks = false;
            NumShortTasks = 0;
            NumLongTasks = 0;
        }

        if (Options.CurrentGameMode == CustomGameMode.TrickorTreat)
        {
            hasCommonTasks = false;
            NumShortTasks = 6;
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

        // Above is override task number
        /* --------------------------*/
        //Below is assign tasks

        // We completely igonre the tasks decided by ShipStatus and assign our own
        List<NormalPlayerTask> commonTasks = ShipStatus.Instance.CommonTasks.Shuffle().ToList();
        List<NormalPlayerTask> shortTasks = ShipStatus.Instance.ShortTasks.Shuffle().ToList();
        List<NormalPlayerTask> longTasks = ShipStatus.Instance.LongTasks.Shuffle().ToList();

        if (!GameManager.Instance.LogicOptions.GetVisualTasks())
        {
            shortTasks.RemoveAll(x => x.TaskType == TaskTypes.SubmitScan);
            longTasks.RemoveAll(x => x.TaskType == TaskTypes.SubmitScan);
        }

        // Remove all disabled tasks
        DisableTasks.DoRemove(ref commonTasks);
        DisableTasks.DoRemove(ref shortTasks);
        DisableTasks.DoRemove(ref longTasks);
        
        List<TaskTypes> usedTaskTypes = [];

        int defaultcommoncount = Main.RealOptionsData.GetInt(Int32OptionNames.NumCommonTasks);
        int commonTasksNum = System.Math.Min(commonTasks.Count, defaultcommoncount);

        // Setting task number to 0 will make Role description disappear from task panel for Vanilla Players and mod Crewmates
        if (!hasCommonTasks && NumShortTasks + NumLongTasks < 1)
        {
            NumShortTasks = 1;
        }

        if (decidedCommonTasks.Count < 1)
        {
            for (int i = 0; i < commonTasksNum; i++)
            {
                decidedCommonTasks.Add((byte)commonTasks[i].Index);
            }
        }

        Il2CppSystem.Collections.Generic.List<byte> TasksList = new();

        if (hasCommonTasks)
        {
            if (__instance.Object != null)
            {
                if (__instance.Object.IsPlayerCrewmateTeam() || !Options.NonCrewRandomCommonTasks.GetBool())
                {
                    foreach (var id in decidedCommonTasks)
                        TasksList.Add(id);
                }
                else
                {
                    for (int i = 0; i < commonTasksNum; i++)
                    {
                        TasksList.Add((byte)commonTasks[i].Index);
                    }
                }
            }
            else
            {
                for (int i = 0; i < commonTasksNum; i++)
                {
                    TasksList.Add((byte)commonTasks[i].Index);
                }
            }
        }

        byte list = 0; byte assigned = 0;
        while (assigned < System.Math.Min(longTasks.Count, NumLongTasks))
        {
            if (!TasksList.Contains((byte)longTasks[list].Index))
            {
                if (usedTaskTypes.Contains(longTasks[list].TaskType))
                {
                    list++;
                }
                else
                {
                    usedTaskTypes.Add(longTasks[list].TaskType);
                    TasksList.Add((byte)longTasks[list].Index);
                    assigned++;
                    list++;
                }
            }
            else
            {
                list++;
            }

            if (list >= longTasks.Count - 1)
            {
                list = 0;
                longTasks = longTasks.Shuffle().ToList();
                usedTaskTypes.Clear();
            }
        }

        list = 0; assigned = 0;

        usedTaskTypes.Clear();
        foreach (var task in longTasks)
        {
            if (TasksList.Contains((byte)task.Index))
            {
                if (!usedTaskTypes.Contains(task.TaskType))
                    usedTaskTypes.Add(task.TaskType);
            }
        }

        while (assigned < System.Math.Min(shortTasks.Count, NumShortTasks))
        {
            if (!TasksList.Contains((byte)shortTasks[list].Index))
            {
                if (usedTaskTypes.Contains(shortTasks[list].TaskType))
                {
                    list++;
                }
                else
                {
                    usedTaskTypes.Add(shortTasks[list].TaskType);
                    TasksList.Add((byte)shortTasks[list].Index);
                    assigned++;
                    list++;
                }
            }
            else
            {
                list++;
            }

            if (list >= shortTasks.Count - 1)
            {
                list = 0;
                shortTasks = shortTasks.Shuffle().ToList();
                usedTaskTypes.Clear();
            }
        }

        if (AmongUsClient.Instance.AmClient)
        {
            __instance.SetTasks((Il2CppStructArray<byte>)TasksList.ToArray());
        }

        RpcUtils.LateBroadcastReliableMessage(new RpcSetTasksMessage(__instance.NetId, (Il2CppStructArray<byte>)TasksList.ToArray()));
        return false;
    }
}

[HarmonyPatch(typeof(NetworkedPlayerInfo), nameof(NetworkedPlayerInfo.HandleRpc))]
class HandleRpcPatch
{
    public static bool Prefix(NetworkedPlayerInfo __instance, [HarmonyArgument(0)] byte callId, [HarmonyArgument(1)] MessageReader reader)
    {
        if (AmongUsClient.Instance.AmHost)
        {
            Logger.Error($"Received Rpc {(RpcCalls)callId} for {__instance.Object.GetRealName()}({__instance.PlayerId}), which is impossible.", "NetworkedPlayerInfo");

            EAC.WarnHost();
            return false;
        }

        return true;
    }
}
