using System;
using DocumentFormat.OpenXml.Drawing.Charts;
using Matlabs.OwlRacer.Common.Model;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Matlabs.OwlRacer.GameClient.States
{
    public interface IState
    {
        ContentManager Content { get; }
        GraphicsDevice GraphicsDevice { get; }
        void Draw(GameTime gameTime, SpriteBatch spriteBatch);
        void Update(GameTime gameTime);
        void PostUpdate(GameTime gameTime);
        void LoadContent(GameTime gameTime);
    }

    public interface IState<TOptions> : IState
    {
        TOptions Options { get; }
        void Initialize(GraphicsDevice graphicsDevice, ContentManager content, TOptions config);
    }
}