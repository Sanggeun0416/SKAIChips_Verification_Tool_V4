namespace SKAIChips_Verification_Tool.Core.AutoTask
{
    public class AutoTaskProgress
    {
        public AutoTaskState State { get; }
        public int CurrentStep { get; }
        public int TotalSteps { get; }
        public string Message { get; }

        public AutoTaskProgress(AutoTaskState state, int currentStep, int totalSteps, string message)
        {
            State = state;
            CurrentStep = currentStep;
            TotalSteps = totalSteps;
            Message = message;
        }
    }
}
