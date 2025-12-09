using System;
using System.Collections.Generic;
using System.Threading;

namespace SKAIChips_Verification_Tool.Core.AutoTask
{
    public sealed class AutoTaskContext
    {
        public IDictionary<string, object> Variables { get; }

        // ★ UI 쪽에서 주입할 콜백
        // type: "READ"/"WRITE" 등, addr/data/result는 문자열 그대로 AddLog에 쓸 형태
        public Action<string, string, string, string> LogCallback { get; set; }

        // addr: 레지스터 주소, value: 읽거나 쓴 값
        public Action<uint, uint> RegisterUpdatedCallback { get; set; }

        public AutoTaskContext()
        {
            Variables = new Dictionary<string, object>();
        }

        public T Get<T>(string key) where T : class
        {
            if (Variables.TryGetValue(key, out var obj))
                return obj as T;
            return null;
        }
    }
}
