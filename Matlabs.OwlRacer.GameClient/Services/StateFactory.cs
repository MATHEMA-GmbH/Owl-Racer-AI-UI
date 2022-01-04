using Matlabs.OwlRacer.GameClient.Services.Interface;
using Matlabs.OwlRacer.GameClient.States;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Drawing.Charts;
using SixLabors.ImageSharp;
using Matlabs.OwlRacer.Common.Model;

namespace Matlabs.OwlRacer.GameClient.Services
{
    public class StateFactory : IStateFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public StateFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public T CreateState<T, TOptions>(GraphicsDevice graphicsDevice, ContentManager contentManager)
            where T : IState<TOptions>
            where TOptions : new() =>
            CreateState<T, TOptions>(graphicsDevice, contentManager, _ => { });

        public T CreateState<T, TOptions>(GraphicsDevice graphicsDevice, ContentManager contentManager, Action<TOptions> config)
            where T : IState<TOptions>
            where TOptions : new()
        {
            var state = _serviceProvider.GetRequiredService<T>();

            var options = new TOptions();
            config(options);

            state.Initialize(graphicsDevice, contentManager, options);
            return state;
        }
    }
}
