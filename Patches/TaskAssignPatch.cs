using System.Collections.Generic;
using AmongUs.GameOptions;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

using TOHE.Roles.AddOns.Crewmate;

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
        }
        foreach (var task in disabledTasks)
        {
            Logger.Msg("削除: " + task.TaskType.ToString(), "AddTask");
            unusedTasks.Remove(task);
        }
    }
}

[HarmonyPatch(typeof(GameData), nameof(GameData.RpcSetTasks))]
class RpcSetTasksPatch
{
    //タスクを割り当ててRPCを送る処理が行われる直前にタスクを上書きするPatch
    //バニラのタスク割り当て処理自体には干渉しない
    public static void Prefix(GameData __instance,
    [HarmonyArgument(0)] byte playerId,
    [HarmonyArgument(1)] ref Il2CppStructArray<byte> taskTypeIds)
    {
        //null対策
        if (Main.RealOptionsData == null)
        {
            Logger.Warn("警告:RealOptionsDataがnullです。", "RpcSetTasksPatch");
            return;
        }

        var pc = Utils.GetPlayerById(playerId);
        CustomRoles? RoleNullable = pc?.GetCustomRole();
        if (RoleNullable == null) return;
        CustomRoles role = RoleNullable.Value;

        //デフォルトのタスク数
        bool hasCommonTasks = true;
        int NumLongTasks = Main.NormalOptions.NumLongTasks;
        int NumShortTasks = Main.NormalOptions.NumShortTasks;

        if (Options.OverrideTasksData.AllData.TryGetValue(role, out var data) && data.doOverride.GetBool())
        {
            hasCommonTasks = data.assignCommonTasks.GetBool(); // コモンタスク(通常タスク)を割り当てるかどうか
                                                               // 割り当てる場合でも再割り当てはされず、他のクルーと同じコモンタスクが割り当てられる。
            NumLongTasks = data.numLongTasks.GetInt(); // 割り当てるロングタスクの数
            NumShortTasks = data.numShortTasks.GetInt(); // 割り当てるショートタスクの数
                                                         // ロングとショートは常時再割り当てが行われる。
        }

        //背叛告密的任务覆盖
        if (pc.Is(CustomRoles.Snitch) && pc.Is(CustomRoles.Madmate))
        {
            hasCommonTasks = false;
            NumLongTasks = 0;
            NumShortTasks = Options.MadSnitchTasks.GetInt();
        }

        //管理员和摆烂人没有任务
        if (pc.Is(CustomRoles.GM) || pc.Is(CustomRoles.Needy) || pc.Is(CustomRoles.Lazy))
        {
            hasCommonTasks = false;
            NumShortTasks = 0;
            NumLongTasks = 0;
        }

        //加班狂加班咯~
        if (pc.Is(CustomRoles.Workhorse))
            (hasCommonTasks, NumLongTasks, NumShortTasks) = Workhorse.TaskData;

        //资本主义要祸害人咯~
        if (Main.CapitalismAssignTask.ContainsKey(playerId))
        {
            NumShortTasks += Main.CapitalismAssignTask[playerId];
            Main.CapitalismAssignTask.Remove(playerId);
        }

        if (taskTypeIds.Count == 0) hasCommonTasks = false; //タスク再配布時はコモンを0に
        if (!hasCommonTasks && NumLongTasks == 0 && NumShortTasks == 0) NumShortTasks = 1; //タスク0対策
        if (hasCommonTasks && NumLongTasks == Main.NormalOptions.NumLongTasks && NumShortTasks == Main.NormalOptions.NumShortTasks) return; //変更点がない場合

        //割り当て可能なタスクのIDが入ったリスト
        //本来のRpcSetTasksの第二引数のクローン
        Il2CppSystem.Collections.Generic.List<byte> TasksList = new();
        foreach (var num in taskTypeIds)
            TasksList.Add(num);

        //参考:ShipStatus.Begin
        //不要な割り当て済みのタスクを削除する処理
        //コモンタスクを割り当てる設定ならコモンタスク以外を削除
        //コモンタスクを割り当てない設定ならリストを空にする
        int defaultCommonTasksNum = Main.RealOptionsData.GetInt(Int32OptionNames.NumCommonTasks);
        if (hasCommonTasks) TasksList.RemoveRange(defaultCommonTasksNum, TasksList.Count - defaultCommonTasksNum);
        else TasksList.Clear();

        //割り当て済みのタスクが入れられるHashSet
        //同じタスクが複数割り当てられるのを防ぐ
        Il2CppSystem.Collections.Generic.HashSet<TaskTypes> usedTaskTypes = new();
        int start2 = 0;
        int start3 = 0;

        //割り当て可能なロングタスクのリスト
        Il2CppSystem.Collections.Generic.List<NormalPlayerTask> LongTasks = new();
        foreach (var task in ShipStatus.Instance.LongTasks)
            LongTasks.Add(task);
        Shuffle<NormalPlayerTask>(LongTasks);

        //割り当て可能なショートタスクのリスト
        Il2CppSystem.Collections.Generic.List<NormalPlayerTask> ShortTasks = new();
        foreach (var task in ShipStatus.Instance.ShortTasks)
            ShortTasks.Add(task);
        Shuffle<NormalPlayerTask>(ShortTasks);

        //実際にAmong Us側で使われているタスクを割り当てる関数を使う。
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

        //タスクのリストを配列(Il2CppStructArray)に変換する
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