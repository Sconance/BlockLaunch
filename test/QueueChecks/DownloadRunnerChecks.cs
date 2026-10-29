using BlockLaunch;

internal static class DownloadRunnerChecks
{
    public static async Task Run()
    {
        var directory = Path.Combine(Path.GetTempPath(), "MCUG-download-runner-" + Guid.NewGuid().ToString("N"));
        try
        {
            var operations = new OperationCoordinator(directory);
            var queue = new DownloadQueueService(operations);
            var allowed = false;
            var calls = 0;
            var logs = new List<string>();
            var first = queue.Enqueue("fixture", directory, "fixture", new());
            var runner = new DownloadQueueRunner(queue, (_, _) => { calls++; return Task.CompletedTask; }, () => allowed, logs.Add);
            await runner.Tick();
            Check(calls == 0, "runner respects host gate");
            allowed = true;
            queue.Change(first.Id, OperationState.Paused);
            await runner.Tick();
            Check(calls == 0, "runner respects global pause");
            queue.Change(first.Id, OperationState.Queued);
            await runner.Tick();
            Check(calls == 1 && !runner.IsRunning && operations.History().Single().State == OperationState.Completed, "runner executes shared queue");

            var active = queue.Enqueue("shutdown fixture", directory, "fixture", new());
            var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var blockingRunner = new DownloadQueueRunner(queue, async (_, context) =>
            {
                entered.TrySetResult();
                await Task.Delay(Timeout.Infinite, context.Token);
            }, () => true, logs.Add);
            var running = blockingRunner.Tick();
            await entered.Task;
            await blockingRunner.Tick();
            Check(blockingRunner.IsRunning, "reentrant tick does not start another task");
            await blockingRunner.Stop();
            await running;
            Check(operations.History().Single(item => item.Id == active.Id).State == OperationState.Queued && !queue.IsPaused,
                "shutdown requeues active task without manual pause");
            await blockingRunner.Tick();
            Check(!blockingRunner.IsRunning, "stopped runner never starts new work");

            var commitEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var committingRunner = new DownloadQueueRunner(queue, async (_, context) =>
            {
                using var scope = context.BeginCommit();
                commitEntered.TrySetResult();
                await release.Task;
            }, () => true, logs.Add);
            running = committingRunner.Tick();
            await commitEntered.Task;
            var stopping = committingRunner.Stop();
            Check(!stopping.IsCompleted && operations.Current!.Record.Committing, "shutdown waits for commit");
            release.TrySetResult();
            await stopping;
            await running;
            Check(operations.History().Single(item => item.Id == active.Id).State == OperationState.Completed, "commit completes during shutdown");

            queue.Enqueue("failure fixture", directory, "fixture", new());
            var failingRunner = new DownloadQueueRunner(queue, (_, _) => throw new IOException("fixture failure"), () => true, logs.Add);
            await failingRunner.Tick();
            Check(!failingRunner.IsRunning && logs.Any(line => line.Contains("fixture failure")), "runner reports failure and releases gate");
            Console.WriteLine("ALL_DOWNLOAD_RUNNER_CHECKS_PASSED");
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    static void Check(bool condition, string name)
    {
        if (!condition)
            throw new Exception(name);
        Console.WriteLine("PASS " + name);
    }
}
