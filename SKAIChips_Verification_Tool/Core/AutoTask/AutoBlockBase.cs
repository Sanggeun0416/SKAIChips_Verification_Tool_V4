using System.Threading;
using System.Threading.Tasks;

namespace SKAIChips_Verification_Tool.Core.AutoTask
{
    public abstract class AutoBlockBase
    {
        #region Properties

        public string Title { get; set; }

        #endregion

        #region Constructors

        protected AutoBlockBase(string title)
        {
            Title = string.IsNullOrWhiteSpace(title) ? GetType().Name : title;
        }

        #endregion

        #region Methods

        public abstract Task ExecuteAsync(AutoTaskContext context, CancellationToken token);

        public override string ToString() => Title;

        #endregion
    }
}
