namespace SKAIChips_Verification_Tool.Core.AutoTask
{
    public sealed class AutoTaskProgress
    {
        #region Properties

        public AutoTaskState State { get; }
        public int CurrentStep { get; }
        public int TotalSteps { get; }
        public string Message { get; }

        #endregion

        #region Constructors

        public AutoTaskProgress(AutoTaskState state, int currentStep, int totalSteps, string message)
        {
            State = state;
            CurrentStep = currentStep;
            TotalSteps = totalSteps;
            Message = message;
        }

        #endregion

        #region Methods

        public override string ToString() =>
            $"{State} ({CurrentStep}/{TotalSteps}) - {Message}";

        #endregion
    }
}
