﻿using Ed.Bannerboard.Models;
using Ed.Bannerboard.Models.Widgets;
using Ed.Bannerboard.UI.Models;
using Ed.Bannerboard.UI.Widgets;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.WebSockets;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Ed.Bannerboard.UI.Pages
{
    public partial class Index : ComponentBase
    {
        private readonly CancellationTokenSource _disposalTokenSource = new();
        private readonly ClientWebSocket _webSocket = new();

        // TODO: Dynamically render and update the widgets from a list when Blazor supports it
        private KingdomStrength kingdomStrength;
        private KingdomLords kingdomLords;
        private KingdomWars kingdomWars;
        private Stats stats;
        private StatsModel statsModel;
        private DashboardSettings settings;

        protected override async Task OnInitializedAsync()
        {
            settings = _configuration.GetSection("DashboardSettings").Get<DashboardSettings>();
            statsModel = new StatsModel
            {
                DashboardVersion = new Version(settings.Version)
            };

            try
            {
                // TODO: Change the port or use an URL?
                await _webSocket.ConnectAsync(new Uri("ws://localhost:2020"), _disposalTokenSource.Token);
                _ = ReceiveLoop();
            }
            catch
            {
                // TODO: Retry failed connections
            }
        }

        private async Task ReceiveLoop()
        {
            var buffer = new ArraySegment<byte>(new byte[2048]);

            do
            {
                var message = string.Empty;

                using (var stream = new MemoryStream())
                {
                    WebSocketReceiveResult result;

                    do
                    {
                        // Wait for a message from the server
                        // Read it into the stream via the buffer when one is received
                        Debug.WriteLine("Receiving...");
                        result = await _webSocket.ReceiveAsync(buffer, _disposalTokenSource.Token);
                        stream.Write(buffer.Array, buffer.Offset, result.Count);
                        Debug.WriteLine("Received, deserializing...");
                    } while (!result.EndOfMessage);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        _webSocket.Abort();
                        StateHasChanged();
                        break;
                    }

                    statsModel.LastMessageBytes = stream.Length;
                    statsModel.ReceivedMessageCount++;

                    // Read the stream as a string
                    // Expecting JSON messages only
                    stream.Seek(0, SeekOrigin.Begin);
                    using (var reader = new StreamReader(stream))
                    {
                        message = await reader.ReadToEndAsync();
                        Debug.WriteLine("Message: " + message);
                    }
                }

                if (Regex.IsMatch(message, "\"Type\":.*\"HandshakeModel\""))
                {
                    var model = JsonConvert.DeserializeObject<HandshakeModel>(message, new VersionConverter());
                    statsModel.ModVersion = model.Version;
                }

                await stats.Update(statsModel);
                await UpdateWidgets(message);
            } while (!_disposalTokenSource.IsCancellationRequested);
        }

        private async Task UpdateWidgets(string message)
        {
            // Send model to widgets
            // TODO: Send to the widget that cares, need an array and dynamic rendering to do this

            if (Regex.IsMatch(message, "\"Type\":.*\"KingdomStrengthModel\""))
            {
                var model = JsonConvert.DeserializeObject<KingdomStrengthModel>(message, new VersionConverter());
                if (kingdomStrength.CanUpdate(model, statsModel.ModVersion))
                {
                    await kingdomStrength.Update(model);
                    return;
                }
            }

            if (Regex.IsMatch(message, "\"Type\":.*\"KingdomLordsModel\""))
            {
                var model = JsonConvert.DeserializeObject<KingdomLordsModel>(message, new VersionConverter());
                if (kingdomLords.CanUpdate(model, statsModel.ModVersion))
                {
                    await kingdomLords.Update(model);
                    return;
                }
            }

            if (Regex.IsMatch(message, "\"Type\":.*\"KingdomWarsModel\""))
            {
                var model = JsonConvert.DeserializeObject<KingdomWarsModel>(message, new VersionConverter());
                if (kingdomWars.CanUpdate(model, statsModel.ModVersion))
                {
                    await kingdomWars.Update(model);
                    return;
                }
            }
        }

        public void Dispose()
        {
            _disposalTokenSource.Cancel();
            _ = _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Bannerboard client stopped", CancellationToken.None);
        }
    }
}
