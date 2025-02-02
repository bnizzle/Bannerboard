﻿using Ed.Bannerboard.Models.Widgets;
using SuperSocket.WebSocket;
using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace Ed.Bannerboard.Logic.Widgets
{
    /// <summary>
    /// A widget for displaying kingdom lord charts on the dashboard.
    /// </summary>
    public class KingdomLordsWidget : WidgetBase
    {
        /// <summary>
        /// A widget for displaying kingdom lord charts on the dashboard.
        /// </summary>
        /// <param name="server">WebSocket server to send data to.</param>
        /// <param name="version">Mod version.</param>
        public KingdomLordsWidget(WebSocketServer server, Version version)
            : base(server, version)
        {
        }

        /// <summary>
        /// Registers widget events.
        /// </summary>
        public override void RegisterEvents()
        {
            // Update all sessions about kingdom lord count every "hour"
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, new Action(() =>
            {
                foreach (var session in Server.GetAllSessions())
                {
                    SendUpdate(session);
                }
            }));
        }

        /// <summary>
        /// Initializes a widget.
        /// </summary>
        /// <param name="session">The session to initialize the widget for.</param>
        public override void Init(WebSocketSession session)
        {
            // Send the first update
            SendUpdate(session);
        }

        public override bool CanHandleMessage(string message)
        {
            return false;
        }

        public override void HandleMessage(WebSocketSession session, string message)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Sends a dashboard update to a WebSocket session.
        /// </summary>
        /// <param name="session">The session to send the update to.</param>
        private void SendUpdate(WebSocketSession session)
        {
            var model = new KingdomLordsModel
            {
                Kingdoms = Campaign.Current.Kingdoms
                    .Select(k => new KingdomLordsItem
                    {
                        Name = k.Name.ToString(),
                        Lords = k.Lords.Count(),
                        PrimaryColor = Color.FromUint(k.Color).ToString(),
                        SecondaryColor = Color.FromUint(k.Color2).ToString(),
                    })
                    .ToList(),
                Version = Version,
            };
            session.Send(model.ToJsonArraySegment());
        }
    }
}
