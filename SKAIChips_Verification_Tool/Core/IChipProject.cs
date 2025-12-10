using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SKAIChips_Verification_Tool.Core
{
    public interface IChipProject
    {
        string Name { get; }

        IEnumerable<ProtocolType> SupportedProtocols { get; }

        IRegisterChip CreateChip(IBus bus, ProtocolSettings settings);
    }

    public interface IChipProjectWithTests : IChipProject
    {
        IChipTestSuite CreateTestSuite(IRegisterChip chip);
    }

    public interface IChipTestSuite
    {
        IReadOnlyList<ChipTestInfo> Tests { get; }

        Task RunTestAsync(
            string testId,
            Func<string, string, Task> log,
            CancellationToken cancellationToken);
    }

    public sealed class ChipTestInfo
    {
        public string Id { get; }

        public string Name { get; }

        public string Description { get; }

        public ChipTestInfo(string id, string name, string description)
        {
            Id = id;
            Name = name;
            Description = description;
        }
    }
}
