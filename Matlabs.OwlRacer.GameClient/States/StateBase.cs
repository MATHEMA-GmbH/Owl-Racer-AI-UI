using DocumentFormat.OpenXml.Drawing.Charts;
using Grpc.Core;
using Grpc.Net.Client;
using Matlabs.OwlRacer.Common.Model;
using Matlabs.OwlRacer.Common.Options;
using Matlabs.OwlRacer.GameClient.Services.Interface;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Matlabs.OwlRacer.GameClient.States
{
    public abstract class StateBase<TOptions> : IState<TOptions>
    {
        public ContentManager Content { get; private set; }
        public GraphicsDevice GraphicsDevice { get; private set; }
        public TOptions Options { get; private set; }

        protected ILogger<StateBase<TOptions>> Logger { get; }
        protected OwlRacerGame Game { get; }

        protected StateBase(OwlRacerGame game, ILogger<StateBase<TOptions>> logger)
        {
            Game = game;
            Logger = logger;
        }

        public virtual void Initialize(GraphicsDevice graphicsDevice, ContentManager content, TOptions options)
        {
            Options = options;
            GraphicsDevice = graphicsDevice;
            Content = content;
        }

        public virtual void PostUpdate(GameTime gameTime)
        {
        }

        public virtual void LoadContent(GameTime gameTime)
        {
        }
        
        public abstract void Draw(GameTime gameTime, SpriteBatch spriteBatch);
        public abstract void Update(GameTime gameTime);
    }
}