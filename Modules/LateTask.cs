using System;

namespace TOHE;

class LateTask
{
    public string name;
    public float timer;
    public bool shouldLog;
    public Action action;
    public bool Cancelled { get; private set; }
    public static List<LateTask> Tasks = [];

    public bool Run(float deltaTime)
    {
        if (Cancelled)
        {
            Tasks.Remove(this);
            return true;
        }

        timer -= deltaTime;
        if (timer <= 0)
        {
            if (!Cancelled)
            {
                action();
            }
            return true;
        }
        return false;
    }
    public LateTask(Action action, float time, string name = "No Name Task", bool shoudLog = true)
    {
        this.action = action;
        this.timer = time;
        this.name = name;
        this.shouldLog = shoudLog;
        this.Cancelled = false;

        Tasks.Add(this);
        if (name != "")
            if (shoudLog)
                Logger.Info("\"" + name + "\" is created", "LateTask");
    }
    public void Cancel()
    {
        Cancelled = true;
        if (name != "" && shouldLog)
            Logger.Info($"\"{name}\" is cancelled", "LateTask");
    }

    public static void Update(float deltaTime)
    {
        foreach (var task in Tasks.ToArray())
        {
            try
            {
                if (task.Run(deltaTime))
                {
                    if (task.name is not "" && task.shouldLog)
                        Logger.Info($"\"{task.name}\" is finished", "LateTask");

                    Tasks.Remove(task);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"{ex.GetType()}: {ex.Message}  in \"{task.name}\"\n{ex.StackTrace}", "LateTask.Error", false);
                Tasks.Remove(task);
            }
        }
    }
}
