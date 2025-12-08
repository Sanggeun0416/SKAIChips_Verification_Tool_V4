using System;

namespace SKAIChips_Verification_Tool.Core.AutoTask
{
    public class AutoTaskProgress
    {
        public AutoTaskState State { get; set; } = AutoTaskState.Idle;
        public int CurrentStep { get; set; }
        public int TotalSteps { get; set; }
        public string Message { get; set; }
        public Exception Error { get; set; }
    }
}
