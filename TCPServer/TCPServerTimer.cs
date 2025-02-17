using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

using RoutingAgentCore.Models.AppSettings;
namespace TCPServer
{
    public class TCPServerTimer : BackgroundService
    {
        private readonly TcpServerService _tcpServerService;
        private IConfiguration _configuration;
        private CancellationTokenSource _cancellationTokenSource;

        // Constructor injection of TcpServerService and Logger
        public TCPServerTimer(TcpServerService tcpServerService,IConfiguration configuration)
        {

            _configuration = configuration;
            _tcpServerService = tcpServerService;
            _cancellationTokenSource = new CancellationTokenSource();
        }

        // Persistent timer loop that never stops unless the application is shut down
        private async Task RunPersistentTimerAsync(CancellationToken token)
        {
            var timerCallckInMS = _configuration.GetValue<int>(AppSettingsKeys.TimerCallckInMSKey);
            var broadcastIntervalInSeconds = _configuration.GetValue<int>(AppSettingsKeys.BroadcastIntervalInSecondsKey);
            var otherTaskIntervalInSeconds = _configuration.GetValue<int>(AppSettingsKeys.OtherTaskIntervalInSecondsKey);

            var lastBroadcastTime = DateTime.UtcNow;
            var lastOtherTaskTime = DateTime.UtcNow;

            while (!token.IsCancellationRequested)
            {
                try
                {

                    // Broadcast message task (always runs on time)
                    if ((DateTime.UtcNow - lastBroadcastTime).TotalSeconds >= broadcastIntervalInSeconds)
                    {
                        _ = BroadcastMessageToAllClientsAsync($"Periodic server broadcast message to all clients {DateTime.UtcNow}");
                        lastBroadcastTime = DateTime.UtcNow;
                    }

                    // Other task runs on a different interval
                    if ((DateTime.UtcNow - lastOtherTaskTime).TotalSeconds >= otherTaskIntervalInSeconds)
                    {
                        if (!string.IsNullOrEmpty(_tcpServerService._clients.FirstOrDefault().Key))
                        {
                            string clientToSend = _tcpServerService._clients.FirstOrDefault().Key;
                            _ = SendMessageToClient(_tcpServerService._clients.FirstOrDefault().Key, $"send message so specific client with {clientToSend} at {DateTime.UtcNow}");
                        }
                        lastOtherTaskTime = DateTime.UtcNow;
                    }

                    // Wait before the next iteration
                    await Task.Delay(TimeSpan.FromMilliseconds(timerCallckInMS), token);
                }
                catch (Exception ex) when (ex is not TaskCanceledException)
                {
                    Serilog.Log.Error(ex, "An error occurred in the persistent timer.");
                }
            }
        }

        private async Task BroadcastMessageToAllClientsAsync(string message)
        {
            // Simulate broadcasting a message to all clients
            await _tcpServerService.BroadcastMessageToAllClients(message);
            Serilog.Log.Information($"Broadcast message sent.{DateTime.UtcNow}");
        }

        // Method to broadcast a message to all clients
        public async Task SendMessageToClient(string clientId, string message)
        {
            await _tcpServerService.SendMessageToClient(clientId, message);
        }

        // Optional: Method to stop the timer when the application shuts down
        public void StopTimer()
        {
            _cancellationTokenSource.Cancel();
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _ = RunPersistentTimerAsync(_cancellationTokenSource.Token);
            return Task.CompletedTask;
        }
    }
}
