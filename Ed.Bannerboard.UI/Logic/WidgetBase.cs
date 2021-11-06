﻿using Blazorise.Charts;
using Microsoft.AspNetCore.Components;
using System;
using System.Threading.Tasks;

namespace Ed.Bannerboard.UI.Logic
{
    public class WidgetBase : ComponentBase
    {
        /// <summary>
        /// Default bar chart options.
        /// </summary>
        protected object BaseBarChartOptions = new
        {
            Animation = new Animation
            {
                Duration = 0,
            },
            Legend = new Legend
            {
                Display = false,
            },
            Scales = new
            {
                YAxes = new[]
                {
                    new
                    {
                        Ticks = new
                        {
                            BeginAtZero = true,
                            Min = 0
                        }
                    }
                }
            }
        };

        /// <summary>
        /// Updates the widget.
        /// </summary>
        /// <param name="model">Model received from the server.</param>
        public virtual Task Update(object model)
        {
            // Should be overridden
            return Task.CompletedTask;
        }

        /// <summary>
        /// Determines whether the model can be used by the widget for an update.
        /// </summary>
        /// <param name="model">Model received from the server.</param>
        /// <param name="version">Mod version the model was received from.</param>
        public virtual bool CanUpdate(object model, Version version)
        {
            return false;
        }

        /// <summary>
        /// Determines whether the mod version is compatible with the minimum supported version by the vidget.
        /// </summary>
        /// <param name="version">The mod version.</param>
        /// <param name="minimumSupportedVersion">Minimum supported version by the widget.</param>
        public bool IsCompatible(Version version, Version minimumSupportedVersion)
        {
            // If we don't know the current mod version yet, don't process the update
            if (version == null)
            {
                return false;
            }

            // Current mod version should be the same or bigger than the minimum supported version
            return version.CompareTo(minimumSupportedVersion) >= 0;
        }
    }
}