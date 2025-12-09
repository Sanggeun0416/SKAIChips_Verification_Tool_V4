using System;
using System.Collections.Generic;

namespace SKAIChips_Verification_Tool.Core.AutoTask
{
    public sealed class AutoTaskDefinition
    {
        #region Properties

        public string Name { get; set; }

        public List<AutoBlockBase> Blocks { get; } = new List<AutoBlockBase>();

        #endregion

        #region Constructors

        public AutoTaskDefinition(string name)
        {
            Name = string.IsNullOrWhiteSpace(name) ? "AutoTask" : name;
        }

        #endregion

        #region Methods

        public int GetTotalSteps() => Blocks.Count;

        public override string ToString() => Name;

        #endregion
    }
}
