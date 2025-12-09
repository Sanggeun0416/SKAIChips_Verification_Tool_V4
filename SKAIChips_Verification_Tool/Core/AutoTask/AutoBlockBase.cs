using System.Threading;
using System.Threading.Tasks;

namespace SKAIChips_Verification_Tool.Core.AutoTask
{
    public abstract class AutoBlockBase
    {
        public string Title { get; set; }

        protected AutoBlockBase(string title)
        {
            Title = string.IsNullOrWhiteSpace(title) ? GetType().Name : title;
        }

        public abstract Task ExecuteAsync(AutoTaskContext context, CancellationToken token);
    }
}
