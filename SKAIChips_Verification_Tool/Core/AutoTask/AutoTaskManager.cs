using System;
using System.Threading;
using System.Threading.Tasks;

namespace SKAIChips_Verification_Tool.Core.AutoTask
{
    public class AutoTaskManager
    {
        private static readonly AutoTaskManager _instance = new AutoTaskManager();
        public static AutoTaskManager Instance => _instance;

        private Task _runningTask;
        private CancellationTokenSource _cts;

        public bool IsRunning => _runningTask != null && !_runningTask.IsCompleted;
        public string CurrentTaskName { get; private set; }

        public event EventHandler<AutoTaskProgress> ProgressChanged;

        private AutoTaskManager()
        {
        }

        public bool TryStart(IAutoTask task, AutoTaskContext context)
        {
            if (task == null) throw new ArgumentNullException(nameof(task));

            if (IsRunning)
                return false; // 이미 실행 중

            _cts = new CancellationTokenSource();
            CurrentTaskName = task.Name;

            var progress = new Progress<AutoTaskProgress>(p =>
            {
                ProgressChanged?.Invoke(this, p);
            });

            _runningTask = Task.Run(async () =>
            {
                try
                {
                    ProgressChanged?.Invoke(this, new AutoTaskProgress
                    {
                        State = AutoTaskState.Running,
                        Message = $"Start: {task.Name}"
                    });

                    // context에 토큰이 필요하면 여기서 새로 만들어서 넘겨도 됨
                    await task.RunAsync(context, progress, _cts.Token);

                    ProgressChanged?.Invoke(this, new AutoTaskProgress
                    {
                        State = AutoTaskState.Completed,
                        Message = $"Completed: {task.Name}"
                    });
                }
                catch (OperationCanceledException)
                {
                    ProgressChanged?.Invoke(this, new AutoTaskProgress
                    {
                        State = AutoTaskState.Canceled,
                        Message = $"Canceled: {task.Name}"
                    });
                }
                catch (Exception ex)
                {
                    ProgressChanged?.Invoke(this, new AutoTaskProgress
                    {
                        State = AutoTaskState.Failed,
                        Error = ex,
                        Message = $"Failed: {task.Name}"
                    });
                }
                finally
                {
                    CurrentTaskName = null;
                    _cts.Dispose();
                    _cts = null;
                }
            });

            return true;
        }

        public void Cancel()
        {
            if (!IsRunning || _cts == null)
                return;

            _cts.Cancel();
        }
    }
}
