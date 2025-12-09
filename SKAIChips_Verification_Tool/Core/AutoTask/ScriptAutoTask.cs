using System;
using System.Threading;
using System.Threading.Tasks;

namespace SKAIChips_Verification_Tool.Core.AutoTask
{
    public sealed class ScriptAutoTask : IAutoTask
    {
        #region Properties

        public string Name { get; }
        public AutoTaskDefinition Definition { get; }

        #endregion

        #region Constructors

        public ScriptAutoTask(string name)
        {
            Name = string.IsNullOrWhiteSpace(name) ? "AutoTask" : name;
            Definition = new AutoTaskDefinition(Name);
        }

        public ScriptAutoTask(AutoTaskDefinition definition)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            Name = string.IsNullOrWhiteSpace(definition.Name) ? "AutoTask" : definition.Name;
        }

        #endregion

        #region Methods

        public Task ExecuteAsync(
            AutoTaskContext context,
            IProgress<AutoTaskProgress> progress,
            CancellationToken token) =>
            ExecuteInternalAsync(context, progress, token);

        private async Task ExecuteInternalAsync(
            AutoTaskContext context,
            IProgress<AutoTaskProgress> progress,
            CancellationToken token)
        {
            if (context == null)
                context = new AutoTaskContext();

            var blocks = Definition?.Blocks;
            var totalSteps = blocks?.Count ?? 0;

            progress?.Report(new AutoTaskProgress(
                AutoTaskState.Running,
                0,
                totalSteps,
                Name));

            if (blocks != null)
            {
                for (var i = 0; i < blocks.Count; i++)
                {
                    token.ThrowIfCancellationRequested();

                    var block = blocks[i];
                    var msg = block?.Title ?? $"Step {i + 1}";

                    try
                    {
                        if (block != null)
                            await block.ExecuteAsync(context, token);

                        progress?.Report(new AutoTaskProgress(
                            AutoTaskState.Running,
                            i + 1,
                            totalSteps,
                            msg));
                    }
                    catch (OperationCanceledException)
                    {
                        progress?.Report(new AutoTaskProgress(
                            AutoTaskState.Canceled,
                            i,
                            totalSteps,
                            "Canceled"));
                        throw;
                    }
                    catch (Exception ex)
                    {
                        progress?.Report(new AutoTaskProgress(
                            AutoTaskState.Failed,
                            i,
                            totalSteps,
                            ex.Message));
                        throw;
                    }
                }
            }

            progress?.Report(new AutoTaskProgress(
                AutoTaskState.Completed,
                totalSteps,
                totalSteps,
                "Completed"));
        }

        #endregion
    }
}
