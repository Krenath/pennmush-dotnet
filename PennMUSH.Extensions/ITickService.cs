namespace PennMUSH.Extensions;

using System;
using System.Threading;
using System.Threading.Tasks;

public interface ITickService
{
    string Name { get; }
    TimeSpan Interval { get; }
    Task ExecuteTickAsync(CancellationToken ct);
}
