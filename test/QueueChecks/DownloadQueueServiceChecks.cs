using BlockLaunch;

internal static class DownloadQueueServiceChecks
{
    public static async Task Run()
    {
        var directory = Path.Combine(Path.GetTempPath(), "MCUG-queue-service-" + Guid.NewGuid().ToString("N"));
        try
        {
            var operations = new OperationCoordinator(directory);
            var service = new DownloadQueueService(operations);
            var root = Path.Combine(directory, "game-original");
            var first = service.Enqueue("first", root, "target", new() { Java = "original-java" });
            var second = service.Enqueue("second", root, "target", new());
            Check(service.NextQueued()?.Id == first.Id, "queue service preserves enqueue order");
            Check(service.ChangeState(first.Id, OperationState.Paused), "queue service pauses pending task");
            Check(service.NextQueued() == null, "pausing a task holds the entire download queue");
            Check(service.ChangeState(second.Id, OperationState.Queued), "resuming a waiting task releases the global pause");
            Check(service.NextQueued()?.Id == second.Id, "queue service skips individually paused tasks after global resume");
            Check(!service.ChangeState(second.Id, OperationState.Completed), "queue service rejects invalid state transition");
            await service.Execute(second, context =>
            {
                Check(context.Record.Root == root, "queue service executes saved game root");
                using var committing = context.BeginCommit();
                Check(!service.ChangeState(second.Id, OperationState.Cancelled), "queue service protects committing task from cancellation");
                return Task.CompletedTask;
            });
            Check(service.ChangeState(first.Id, OperationState.Queued), "queue service resumes paused task");
            var queued = service.NextQueued()!;
            Check(queued.Download!.Java == "original-java", "queue service preserves saved Java selection");
            var entered = new TaskCompletionSource();
            var running = service.Execute(queued, async context =>
            {
                entered.SetResult();
                await Task.Delay(Timeout.Infinite, context.Token);
            });
            await entered.Task;
            Check(service.ChangeState(queued.Id, OperationState.Paused), "queue service pauses active task through coordinator");
            Check((await running).State == OperationState.Paused, "queue service waits for active task cleanup");
            await DownloadControlChecks.Run(Path.Combine(directory, "controls"));
            Console.WriteLine("ALL_DOWNLOAD_QUEUE_SERVICE_CHECKS_PASSED");
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    static void Check(bool value, string name)
    {
        if (!value)
            throw new Exception(name);
        Console.WriteLine("PASS " + name);
    }
}
