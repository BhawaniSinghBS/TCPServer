using Microsoft.Extensions.Hosting;
using Serilog;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;

namespace TCPServer
{
    public class TcpServerService : BackgroundService
    {
        private TcpListener _tcpListener;
        public readonly ConcurrentDictionary<string, ClientInfo> _clients = new();


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _tcpListener = new TcpListener(TCPServerSettings.TCPServerIP, TCPServerSettings.TCPServerListningPort);
            _tcpListener.Start();
            Log.Information("TCP server started and listening on port 8085.");

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    var client = await _tcpListener.AcceptTcpClientAsync();
                    var endPoint = client.Client.RemoteEndPoint as IPEndPoint;
                    var clientId = $"{endPoint?.Address}:{endPoint?.Port}";

                    Log.Information($"New client connected with ID {clientId}.");

                    var clientInfo = new ClientInfo
                    {
                        Client = client,
                        ClientId = clientId,
                        ConnectedAt = DateTime.Now,
                        IPAddress = endPoint?.Address ?? default,
                        Port = endPoint?.Port ?? default
                    };

                    _clients.TryAdd(clientId, clientInfo);
                    _ = HandleClientConnectedAsync(clientInfo, stoppingToken);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Log.Error(ex, "TCP server stopped unexpectedly.");

                string className = MethodBase.GetCurrentMethod()?.DeclaringType?.Name ?? "UnknownClass";
                string methodName = MethodBase.GetCurrentMethod()?.Name ?? "UnknownMethod";
                string excetpionLocation = $"Class name : {className}  -- Function Name : {methodName}----------";
                Log.Information(ex, excetpionLocation);
            }
            finally
            {
                //_tcpListener.Stop();
                await Task.WhenAll(_clients.Values.Select(c => HandleClientDisconnectionAsync(c.ClientId)));

            }
        }

        private async Task HandleClientConnectedAsync(ClientInfo clientInfo, CancellationToken stoppingToken)
        {
            var client = clientInfo.Client;
            var stream = client.GetStream();
            var buffer = new byte[1024];

            try
            {
                while (!stoppingToken.IsCancellationRequested && client.Connected)
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, stoppingToken);
                    if (bytesRead == 0) break; // Client disconnected

                    var receivedText = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Log.Information($"Received from client {clientInfo.ClientId}: {receivedText}");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Error handling communication with client {clientInfo.ClientId}.");
            }
            finally
            {
                await HandleClientDisconnectionAsync(clientInfo.ClientId);
            }
        }

        private async Task HandleClientDisconnectionAsync(string clientId)
        {
            if (_clients.TryRemove(clientId, out var clientInfo))
            {
                clientInfo.Client.Close();
                Log.Information($"Client {clientId} disconnected and removed from tracking.");
            }
        }


        // Method to send a message to a specific client
        public async Task SendMessageToClient(string clientId, string message)
        {
            if (_clients.TryGetValue(clientId, out var clientInfo))
            {
                var messageBytes = Encoding.UTF8.GetBytes(message);
                var stream = clientInfo.Client.GetStream();

                try
                {
                    await stream.WriteAsync(messageBytes, 0, messageBytes.Length);
                    Log.Information($"Sent message to client {clientId}: {message}");
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"Failed to send message to client {clientId}. Attempting to remove client due to potential disconnection.");
                    await HandleClientDisconnectionAsync(clientId); // Remove the client if there's an error
                }
            }
            else
            {
                Log.Warning($"Client {clientId} not found.");
            }
        }

        // Method to broadcast a message to all connected clients
        public async Task BroadcastMessageToAllClients(string message)
        {
            var messageBytes = Encoding.UTF8.GetBytes(message);

            foreach (var clientId in _clients.Keys)
            {
                if (_clients.TryGetValue(clientId, out var clientInfo))
                {
                    var stream = clientInfo.Client.GetStream();
                    try
                    {
                        await stream.WriteAsync(messageBytes, 0, messageBytes.Length);
                        Log.Information($"Broadcast message to client {clientId}: {message}");
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, $"Failed to send broadcast message to client {clientId}. Removing client due to disconnection.");
                        await HandleClientDisconnectionAsync(clientId); // Remove disconnected clients
                    }
                }
            }
        }

    }
}