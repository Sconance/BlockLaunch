using BlockLaunch;

internal static class DownloadControlChecks
{
    public static async Task Run(string directory)
    {
        var settings = new SettingsStore(Path.Combine(directory, "settings"));
        var prefs = new Preferences { Root = Path.Combine(directory, "game"), Player = "OriginalPlayer" };
        settings.Save(prefs);
        var tasks = Path.Combine(directory, "tasks");
        var operations = new OperationCoordinator(tasks);
        var service = new DownloadQueueService(operations, new DownloadPauseStore(settings, () => prefs));
        var first = service.Enqueue("first", prefs.Root, "first", new() { Java = "frozen-java" });
        var second = service.Enqueue("second", prefs.Root, "second", new());
        var newer = settings.Load();
        newer.Player = "NewerPlayer";
        newer.Ram = 8192;
        settings.Save(newer);
        Check(service.Change(first.Id, OperationState.Paused).Changed && prefs.DownloadsPaused && prefs.PausedDownloadId == first.Id,
            "unified pause updates live queue preferences");
        var persisted = settings.Load();
        Check(persisted.DownloadsPaused && persisted.PausedDownloadId == first.Id && persisted.Player == "NewerPlayer" && persisted.Ram == 8192,
            "pause persists only queue preferences without overwriting newer settings");
        var restartedPrefs = settings.Load();
        var restarted = new DownloadQueueService(new OperationCoordinator(tasks), new DownloadPauseStore(settings, () => restartedPrefs));
        Check(restarted.IsPaused && restarted.NextQueued() == null, "restart preserves global queue pause");
        await Fails<InvalidOperationException>(() => restarted.Execute(second, _ => Task.CompletedTask));
        Check(restarted.Toggle(first.Id).Changed && !restartedPrefs.DownloadsPaused && restartedPrefs.PausedDownloadId == "",
            "toggle resumes saved paused task and clears global pause");
        Check(!settings.Load().DownloadsPaused, "resume persists across restart");
        prefs = settings.Load();
        Check(service.Change(first.Id, OperationState.Paused).Changed, "task-card pause uses the same command");
        Check(service.Change(first.Id, OperationState.Cancelled).Changed && service.IsPaused && service.PauseState.TaskId == "",
            "cancelling the pause anchor clears its id without implicitly restarting other tasks");
        Check(!service.Change(first.Id, OperationState.Queued).Changed, "cancelled tasks cannot accidentally retry");
        Check(service.Toggle(second.Id).Changed && !service.IsPaused && service.NextQueued()?.Id == second.Id,
            "another waiting task can explicitly release global pause");
        var bytes = File.ReadAllBytes(Path.Combine(directory, "settings", "settings.json"));
        Check(!service.Change("missing", OperationState.Paused).Changed && !service.Change(second.Id, OperationState.Completed).Changed,
            "unknown task and unsupported transition are rejected");
        Check(File.ReadAllBytes(Path.Combine(directory, "settings", "settings.json")).SequenceEqual(bytes), "rejected commands do not write preferences");
        await Fails<IOException>(() => service.Execute(second, _ => throw new IOException("fixture download failed")));
        Check(service.Toggle(second.Id).Changed && operations.History().Single(record => record.Id == second.Id).State == OperationState.Queued,
            "failed task toggle retries the original persisted task");
        var entered = new TaskCompletionSource();
        var active = service.Execute(service.NextQueued()!, async context =>
        {
            entered.SetResult();
            await Task.Delay(Timeout.Infinite, context.Token);
        });
        await entered.Task;
        Check(!service.Change(second.Id, OperationState.Queued).Changed, "running task cannot be resumed or retried");
        Check(service.Toggle(second.Id).Changed, "mini toggle pauses running task through coordinator cancellation");
        Check((await active).State == OperationState.Paused && prefs.DownloadsPaused, "active pause waits for cleanup and holds queue");
        Check(service.Change(second.Id, OperationState.Queued).Changed, "task list resumes the same paused task");
        entered = new TaskCompletionSource();
        var release = new TaskCompletionSource();
        active = service.Execute(service.NextQueued()!, async context =>
        {
            using var commit = context.BeginCommit();
            entered.SetResult();
            await release.Task;
        });
        await entered.Task;
        try
        {
            Check(!service.Change(second.Id, OperationState.Paused).Changed
                && !service.Change(second.Id, OperationState.Cancelled).Changed && !service.Toggle(second.Id).Changed
                && !service.SuspendForShutdown(), "every download command respects commit protection");
        }
        finally
        {
            release.SetResult();
            await active;
        }
        Check(!service.Toggle(second.Id).Changed, "completed task cannot be toggled or reinstalled");
        var shutdown = service.Enqueue("shutdown", prefs.Root, "shutdown", new());
        entered = new TaskCompletionSource();
        active = service.Execute(shutdown, async context =>
        {
            entered.SetResult();
            await Task.Delay(Timeout.Infinite, context.Token);
        });
        await entered.Task;
        Check(service.SuspendForShutdown(), "shutdown suspends active download through unified service");
        Check((await active).State == OperationState.Queued && !prefs.DownloadsPaused,
            "shutdown requeues for next startup without persisting a manual pause");
        var later = service.Enqueue("later", prefs.Root, "later", new());
        Check(service.Move(later.Id, -1) && service.NextQueued()?.Id == later.Id, "queue reorder uses unified download module");
        await CheckSaveFailure(Path.Combine(directory, "failure"));
    }

    static async Task CheckSaveFailure(string directory)
    {
        var settings = new SettingsStore(Path.Combine(directory, "settings"));
        var prefs = new Preferences { Root = Path.Combine(directory, "game") };
        settings.Save(prefs);
        var operations = new OperationCoordinator(Path.Combine(directory, "tasks"));
        var service = new DownloadQueueService(operations, new DownloadPauseStore(settings, () => prefs));
        var task = service.Enqueue("save failure", prefs.Root, "target", new());
        var incompatible = settings.Load();
        incompatible.SchemaVersion = 99;
        var file = Path.Combine(directory, "settings", "settings.json");
        AtomicFile.WriteJson(file, incompatible);
        var before = File.ReadAllBytes(file);
        var result = service.Change(task.Id, OperationState.Paused);
        Check(result.Changed && result.Warning.Length > 0 && prefs.DownloadsPaused && service.NextQueued() == null,
            "preference save failure reports warning while retaining actual task state and live pause");
        Check(operations.History().Single().State == OperationState.Paused && File.ReadAllBytes(file).SequenceEqual(before),
            "preference failure does not corrupt newer settings or undo durable queue transition");
        Check(service.Change(task.Id, OperationState.Cancelled).Changed, "task can still be cancelled after preference persistence failure");
        var unrelated = await operations.Run("not a download", prefs.Root, "target", _ => Task.CompletedTask);
        Check(!service.Change(unrelated.Id, OperationState.Paused).Changed, "download commands reject non-download tasks");
    }

    static async Task Fails<T>(Func<Task> action) where T : Exception
    {
        try
        {
            await action();
        }
        catch (T)
        {
            return;
        }
        throw new Exception("Expected " + typeof(T).Name);
    }

    static void Check(bool condition, string name)
    {
        if (!condition)
            throw new Exception(name);
        Console.WriteLine("PASS " + name);
    }
}
