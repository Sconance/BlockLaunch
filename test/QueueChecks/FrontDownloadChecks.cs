using BlockLaunch;
using BlockLaunch.Contracts;
using BlockLaunch.SharedUI;

internal static class FrontDownloadChecks
{
    public static async Task Run()
    {
        var directory = Path.Combine(Path.GetTempPath(), "MCUG-front-download-" + Guid.NewGuid().ToString("N"));
        try
        {
            var operations = new OperationCoordinator(directory);
            var queue = new DownloadQueueService(operations);
            var backend = new DownloadFrontendService(queue);
            using var model = new DownloadTasksViewModel(backend);
            model.Refresh();
            Check(model.Tasks.Count == 0, "empty shared task list");
            var task = queue.Enqueue("shared", directory, "target", new());
            model.Refresh();
            var snapshot = model.Tasks.Single();
            Check(snapshot.Id == task.Id && snapshot.CanPause && !snapshot.CanResume, "queued snapshot capabilities");
            model.Change(task.Id, FrontDownloadAction.Pause);
            Check(queue.IsPaused && model.IsPaused && model.Tasks.Single().CanResume, "view model pauses shared backend");
            Check(snapshot.CanPause && !snapshot.CanResume, "previous snapshot remains unchanged");
            Check(backend.Change(task.Id, FrontDownloadAction.Resume).Changed && !queue.IsPaused, "second caller resumes same queue");
            try
            {
                await queue.Execute(queue.NextQueued()!, _ => throw new IOException("test failure"));
            }
            catch (IOException)
            {
            }
            model.Refresh();
            Check(model.Tasks.Single().ResumeLabel == "重试", "failed task exposes retry");
            model.Change(task.Id, FrontDownloadAction.Resume);
            await queue.Execute(queue.NextQueued()!, context =>
            {
                context.Report("latest progress", 25);
                Check(backend.Read().Tasks.Single().Percent == 25, "snapshot reads active progress");
                using var commit = context.BeginCommit();
                var current = backend.Read().Tasks.Single();
                Check(!current.CanPause && !current.CanResume && !current.CanCancel, "commit disables all actions");
                Check(!backend.Change(task.Id, FrontDownloadAction.Cancel).Changed, "backend rejects commit cancellation");
                return Task.CompletedTask;
            });
            model.Refresh();
            Check(!model.Tasks.Single().CanResume, "completed task cannot retry");
            model.Change(task.Id, FrontDownloadAction.Resume);
            Check(model.Status.Contains("任务状态已变化"), "rejected action has visible status");
            await operations.Run("account action", directory, "account", _ => Task.CompletedTask);
            Check(backend.Read().Tasks.Count == 1, "non-download operations are excluded");

            var fake = new FailingDownloads();
            using var failingModel = new DownloadTasksViewModel(fake);
            failingModel.Refresh();
            Check(failingModel.Status.Contains("读取下载任务失败"), "read failure is visible");
            fake.FailRead = false;
            failingModel.Refresh();
            Check(failingModel.Status == "任务状态已读取。", "refresh recovers from read error");
            fake.FailRead = true;
            failingModel.Change("test", FrontDownloadAction.Pause);
            Check(failingModel.Status.Contains("保存警告") && failingModel.Status.Contains("刷新失败"), "accepted action retains warning when refresh fails");
            failingModel.Dispose();
            failingModel.Change("test", FrontDownloadAction.Cancel);
            failingModel.Refresh();
            Check(fake.Changes == 1, "disposed model ignores commands");
            Console.WriteLine("ALL_FRONT_DOWNLOAD_CHECKS_PASSED");
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    static void Check(bool condition, string message)
    {
        if (!condition)
            throw new Exception(message);
        Console.WriteLine("PASS " + message);
    }

    sealed class FailingDownloads : IFrontDownloadTasks
    {
        public bool FailRead = true;
        public int Changes;
        public FrontDownloadQueue Read() => FailRead ? throw new IOException("read error") : new(false, []);
        public FrontDownloadResult Change(string id, FrontDownloadAction action)
        {
            Changes++;
            return new(true, "保存警告");
        }
    }
}
