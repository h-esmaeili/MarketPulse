﻿using MarketPulse.Api.Configs;
using MarketPulse.Api.Middleware;
using MarketPulse.Api.Models;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Net.WebSockets;
using System.Text;

namespace MarketPulse.Api.ServiceWorker
{
    public class MarketUpdateService : BackgroundService
    {
        private readonly WebSocketConnectionManager _connectionManager;
        private readonly TiingoSettings _settings;
        public MarketUpdateService(WebSocketConnectionManager connectionManager, IOptions<TiingoSettings> settings)
        {
            _connectionManager = connectionManager;
            _settings = settings.Value;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var clientWebSocket = new ClientWebSocket();
            clientWebSocket.Options.SetRequestHeader("Authorization", _settings.ApiKey);
            var tiingoUri = new Uri(_settings.WebSockets.Uri);
            await clientWebSocket.ConnectAsync(tiingoUri, stoppingToken);

            // Send subscription message to Tiingo
            await SubscribeToTickerAsync(clientWebSocket, _settings.WebSockets.Ticker);

            while (!stoppingToken.IsCancellationRequested)
            {
                // Receive data from Tiingo and broadcast
                await ReceiveAndBroadcast(clientWebSocket, stoppingToken);
            }
        }

        private async Task ReceiveAndBroadcast(ClientWebSocket clientWebSocket, CancellationToken stoppingToken)
        {
            var buffer = new byte[1024 * 4];
            var result = await clientWebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), stoppingToken);
            var data = Encoding.UTF8.GetString(buffer, 0, result.Count);

            // Broadcast to all connected clients
            var message = JsonConvert.DeserializeObject<MarketData>(data);
            if (message != null && message.messageType == "A")
                await _connectionManager.BroadcastToClientsAsync(data);
        }
        private async Task SubscribeToTickerAsync(ClientWebSocket clientWebSocket, string ticker)
        {
            // Subscription message
            var subscribeMessage = new
            {
                eventName = "subscribe",
                authorization = _settings.ApiKey,
                ticker = _settings.WebSockets.Ticker,
                eventData = new { thresholdLevel = 2 }
            };

            var message = JsonConvert.SerializeObject(subscribeMessage);
            var messageBytes = Encoding.UTF8.GetBytes(message);

            // Send the subscription message
            await clientWebSocket.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, CancellationToken.None);
            Console.WriteLine($"Subscribed to {ticker} on Tiingo WebSocket");
        }
    }
}