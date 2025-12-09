using System.Collections.Generic;

namespace SKAIChips_Verification_Tool.Core.AutoTask
{
    public sealed class AutoTaskDefinition
    {
        public string Name { get; set; }
        public List<AutoBlockBase> Blocks { get; } = new List<AutoBlockBase>();

        public AutoTaskDefinition(string name)
        {
            Name = string.IsNullOrWhiteSpace(name) ? "AutoTask" : name;
        }

        public int GetTotalSteps()
        {
            return Blocks.Count;
        }
    }
}
