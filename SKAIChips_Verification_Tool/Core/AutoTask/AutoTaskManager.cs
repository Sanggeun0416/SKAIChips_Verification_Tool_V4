using System;
using System.Threading;
using System.Threading.Tasks;

namespace SKAIChips_Verification_Tool.Core.AutoTask
{
    public sealed class AutoTaskManager
    {
        private static readonly Lazy<AutoTaskManager> _instance =
            new(() => new AutoTaskManager());
        public static AutoTaskManager Instance => _instance.Value;

        private readonly object _lock = new();
        private CancellationTokenSource _cts;
        private Task _runningTask;

        public bool IsRunning => _runningTask != null && !_runningTask.IsCompleted;

        public event EventHandler<AutoTaskProgress> ProgressChanged;

        private AutoTaskManager()
        {
        }

        public bool TryStart(IAutoTask task, AutoTaskContext context)
        {
            lock (_lock)
            {
                if (IsRunning)
                    return false;

                _cts = new CancellationTokenSource();
                var token = _cts.Token;

                // 여기서 System.Progress<T> 를 명시적으로 사용
                IProgress<AutoTaskProgress> progress =
                    new System.Progress<AutoTaskProgress>(p =>
                    {
                        ProgressChanged?.Invoke(this, p);
                    });

                _runningTask = Task.Run(async () =>
                {
                    try
                    {
                        progress.Report(new AutoTaskProgress(
                            AutoTaskState.Running, 0, 0, task.Name + " 시작"));

                        await task.ExecuteAsync(context, progress, token);

                        if (token.IsCancellationRequested)
                        {
                            progress.Report(new AutoTaskProgress(
                                AutoTaskState.Canceled, 0, 0, task.Name + " 취소됨"));
                        }
                        else
                        {
                            progress.Report(new AutoTaskProgress(
                                AutoTaskState.Completed, 0, 0, task.Name + " 완료"));
                        }
                    }
                    catch (Exception ex)
                    {
                        progress.Report(new AutoTaskProgress(
                            AutoTaskState.Failed, 0, 0, ex.Message));
                    }
                    finally
                    {
                        lock (_lock)
                        {
                            _cts.Dispose();
                            _cts = null;
                            _runningTask = null;
                        }
                    }
                }, token);

                return true;
            }
        }

        public void Cancel()
        {
            lock (_lock)
            {
                _cts?.Cancel();
            }
        }
    }
}
