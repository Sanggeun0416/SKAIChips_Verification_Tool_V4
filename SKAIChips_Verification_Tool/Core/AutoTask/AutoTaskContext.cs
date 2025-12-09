using System;
using System.Collections.Generic;

namespace SKAIChips_Verification_Tool.Core.AutoTask
{
    public sealed class AutoTaskContext
    {
        #region Properties

        public IDictionary<string, object> Variables { get; }

        public Action<string, string, string, string> LogCallback { get; set; }

        public Action<uint, uint> RegisterUpdatedCallback { get; set; }

        #endregion

        #region Constructors

        public AutoTaskContext()
        {
            Variables = new Dictionary<string, object>();
        }

        #endregion

        #region Methods

        public T Get<T>(string key) where T : class
        {
            if (Variables.TryGetValue(key, out var obj))
                return obj as T;

            return null;
        }

        #endregion
    }
}
