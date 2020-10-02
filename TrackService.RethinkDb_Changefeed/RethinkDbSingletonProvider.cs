using Microsoft.Extensions.Options;
using System;
using TrackService.RethinkDb_Changefeed.Model;

namespace TrackService.RethinkDb_Changefeed
{
    internal class RethinkDbSingletonProvider : IRethinkDbSingletonProvider, IDisposable
    {
        private bool _disposed = false;

        public RethinkDb.Driver.RethinkDB RethinkDbSingleton { get; }

        public RethinkDb.Driver.Net.Connection RethinkDbConnection { get; }

        public RethinkDbSingletonProvider(IOptions<RethinkDbOptions> options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (String.IsNullOrWhiteSpace(options.Value.Host))
            {
                throw new ArgumentNullException(nameof(RethinkDbOptions.Host));
            }

            var rethinkDbSingleton = RethinkDb.Driver.RethinkDB.R;

            var rethinkDbConnection = rethinkDbSingleton.Connection().Hostname(options.Value.Host).Db(options.Value.Database);

            if (string.IsNullOrEmpty(Convert.ToString(options.Value.Port)))
            {
                rethinkDbConnection.Port(options.Value.Port);
            }

            if (string.IsNullOrEmpty(Convert.ToString(options.Value.Timeout)))
            {
                rethinkDbConnection.Timeout(options.Value.Timeout);
            }

            RethinkDbConnection = rethinkDbConnection.Connect();

            RethinkDbSingleton = rethinkDbSingleton;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                RethinkDbConnection.Dispose();

                GC.SuppressFinalize(this);

                _disposed = true;
            }
        }
    }
}
