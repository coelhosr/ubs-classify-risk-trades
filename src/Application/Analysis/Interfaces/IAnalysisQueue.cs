using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Analysis.Interfaces;

public interface IAnalysisQueue
{
    Task EnqueueAsync(AnalysisQueueItem item, CancellationToken ct);
    Task EnqueueManyAsync(string jobId, IEnumerable<Trade> trades, CancellationToken ct);

}
