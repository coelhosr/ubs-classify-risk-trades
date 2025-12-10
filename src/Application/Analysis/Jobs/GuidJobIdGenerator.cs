using Application.Analysis.Interfaces;

namespace Application.Analysis.Jobs;

public class GuidJobIdGenerator : IJobIdGenerator
{
    public string NewId() => Guid.NewGuid().ToString("N");
}
