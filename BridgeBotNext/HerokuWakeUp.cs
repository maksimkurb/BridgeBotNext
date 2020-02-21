using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace BridgeBotNext
{
    public class HerokuWakeUp
    {
        private readonly Uri _uri;
        private readonly TimeSpan _interval;
        private readonly CancellationToken _cancellationToken;
        private ILogger<HerokuWakeUp> _logger;
        
        public HerokuWakeUp(
            ILogger<HerokuWakeUp> logger,
            Uri uri,
            TimeSpan interval,
            CancellationToken cancellationToken)
        {
            _logger = logger;
            _uri = uri;
            _interval = interval;
            _cancellationToken = cancellationToken;
        }

        private void Ping()
        {
            _logger.LogInformation("Calling uri {}", _uri);
            var webClient = new WebClient();
            webClient.DownloadData(_uri);
        }
        
        public async Task PeriodicPing()
        {
            while (true)
            {
                Ping();
                await Task.Delay(_interval, _cancellationToken);
                if (_cancellationToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }
    }
}