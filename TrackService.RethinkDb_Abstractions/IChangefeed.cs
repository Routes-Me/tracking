using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TrackService.RethinkDb_Abstractions
{
    public interface IChangefeed<out T>
    {
        IAsyncEnumerable<T> FetchFeed(CancellationToken cancellationToken = default);
    }
}
