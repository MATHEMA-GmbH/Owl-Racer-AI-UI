using Matlabs.OwlRacer.Common.Model;
using Matlabs.OwlRacer.GameClient.States;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matlabs.OwlRacer.GameClient.Services.Interface
{
    public interface IStateFactory
    {
        public T CreateState<T, TOptions>(GraphicsDevice graphicsDevice, ContentManager contentManager)
            where T : IState<TOptions>
            where TOptions : new();

        public T CreateState<T, TOptions>(GraphicsDevice graphicsDevice, ContentManager contentManager, Action<TOptions> config)
            where T : IState<TOptions>
            where TOptions : new();
    }
}
