using Matlabs.OwlRacer.Common.Model;
using Matlabs.OwlRacer.GameClient.Services;
using Matlabs.OwlRacer.GameClient.Services.Interface;
using Matlabs.OwlRacer.GameClient.States.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matlabs.OwlRacer.GameClient.States
{
    public class RankingState : StateBase<RankingStateOptions>, IRankingState<RankingStateOptions>
    {
        // Race Track
        Texture2D _background;

        // Session
        private Session _session;

        // Services
        private readonly IGameService _gameService;
        private readonly ISessionService _sessionService;
        private readonly IStateFactory _stateFactory;
        //private object stateFactory;

        // Constants
        private SpriteFont _font;
        private SpriteFont _fontSmall;

        // UI
        private Texture2D _logo;
        private Texture2D _circle;
        private Texture2D _street;
        private Texture2D _logoMathema;

        private Color _corporateGray60 = new Color(136, 139, 141);
        private Color _corporateGray40 = new Color(187, 188, 188);

        public RankingState(
            OwlRacerGame game,
            ILogger<RankingState> logger,
            IGameService gameService,
            ISessionService sessionService,
            IStateFactory stateFactory)
            : base(game, logger)
        {
            _gameService = gameService ?? throw new ArgumentNullException(nameof(gameService));
            _stateFactory = stateFactory ?? throw new ArgumentNullException(nameof(stateFactory));
            _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        }
        public override void Initialize(GraphicsDevice graphicsDevice, ContentManager content, RankingStateOptions rankingStateOptions)
        {
            base.Initialize(graphicsDevice, content, rankingStateOptions);

            _font = Content.Load<SpriteFont>("Inter-SemiBold");
            _fontSmall = Content.Load<SpriteFont>("Inter-Regular-small");
            _session = _sessionService.GetSession(Game.Session.Id);
        }

        public override void LoadContent(GameTime gameTime)
        {
            _background = new Texture2D(GraphicsDevice, 1, 1, false, SurfaceFormat.Color);
            _background.SetData(new[] { Color.White });

            _logo = Content.Load<Texture2D>(@"Images/owlracer-logo-solo");
            _circle = Content.Load<Texture2D>(@"Images/Circle");
            _street = Content.Load<Texture2D>(@"Images/Street");
            _logoMathema = Content.Load<Texture2D>(@"Images/mathema-logo");
        }

        public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            spriteBatch.Begin();

            Rectangle logoRect = new Rectangle((int)(GraphicsDevice.Adapter.CurrentDisplayMode.Width - _logo.Width * 0.56), (int)(GraphicsDevice.Adapter.CurrentDisplayMode.Height - _circle.Height / 3), (int)(_logo.Width * 0.53), (int)(_logo.Height * 0.53));
            Rectangle logoRectCircle = new Rectangle(GraphicsDevice.Adapter.CurrentDisplayMode.Width - _circle.Width / 3 * 2, (GraphicsDevice.Adapter.CurrentDisplayMode.Height - _circle.Height / 2),
                _circle.Width / 3 * 2, _circle.Height / 2);

            Rectangle logoRectMathema = new Rectangle((int)(GraphicsDevice.Adapter.CurrentDisplayMode.Width - (_logoMathema.Width * 0.2)), (int)(GraphicsDevice.Adapter.CurrentDisplayMode.Height - _circle.Height / 3 + logoRect.Height * 1.9),
               (int)(_logoMathema.Width * 0.15), (int)(_logoMathema.Height * 0.15));

            Rectangle logoRectStreet1 = new Rectangle(0, GraphicsDevice.Adapter.CurrentDisplayMode.Height - 8 * GraphicsDevice.Adapter.CurrentDisplayMode.Height / _street.Height,
                GraphicsDevice.Adapter.CurrentDisplayMode.Width, _street.Height * GraphicsDevice.Adapter.CurrentDisplayMode.Height / 8);

            spriteBatch.Draw(_street, logoRectStreet1, Color.White);
            spriteBatch.Draw(_circle, logoRectCircle, Color.White);
            spriteBatch.Draw(_logo, logoRect, Color.White);

            spriteBatch.DrawString(_fontSmall, "EIN PROJEKT DER", new Vector2((int)(GraphicsDevice.Adapter.CurrentDisplayMode.Width - (logoRectCircle.Width / 2)), (int)(GraphicsDevice.Adapter.CurrentDisplayMode.Height - _circle.Height / 3 + logoRect.Height * 1.3)), _corporateGray60);
            spriteBatch.Draw(_logoMathema, logoRectMathema, Color.White);


            DrawRankingText(spriteBatch);

            spriteBatch.End();
        }

        public override void Update(GameTime gameTime)
        {
            CheckExitGame();
        }

        private void CheckExitGame()
        {
            OwlKeyboard.GetState();
            if (OwlKeyboard.HasBeenPressed(Keys.Escape))
            {
                Logger.LogInformation("Back to MenuState");
                var menuState = _stateFactory.CreateState<IMenuState<MenuStateOptions>, MenuStateOptions>(GraphicsDevice, Content);

                Game.ChangeState(menuState);
            }
        }

        private void DrawRankingText(SpriteBatch spriteBatch)
        {
            var sortedList = _session.Scores.OrderByDescending(o => o.Value).ToList();

            int xPos = GraphicsDevice.Adapter.CurrentDisplayMode.Width/2 - 600/2;
            int yPos = GraphicsDevice.Adapter.CurrentDisplayMode.Height / 2 - (sortedList.Count * 20 + 60)/2;
            //_startPos = new StartPosition(GraphicsDevice.Adapter.CurrentDisplayMode.Width / 2 - _trackWidth / 2, GraphicsDevice.Adapter.CurrentDisplayMode.Height / 2 - _trackHeight / 2);


            spriteBatch.DrawString(_font, "Exit Game (press Escape) ", new Vector2(xPos, yPos - 50), Color.Black);
            spriteBatch.Draw(_background, new Rectangle(xPos - 10, yPos - 10, 600, sortedList.Count * 20 + 40), null, _corporateGray40, 0, new Vector2(0, 0), SpriteEffects.None, 0);
            spriteBatch.DrawString(_font, "Ranking List ", new Vector2(xPos, yPos), Color.Black);
            var ranking = 1;

            foreach (var car in sortedList)
            {
                spriteBatch.DrawString(_font, ranking.ToString(), new Vector2(xPos, yPos + 20), Color.Black);
                spriteBatch.DrawString(_font, car.Key.Name , new Vector2(xPos + 60, yPos + 20), Color.Black);
                spriteBatch.DrawString(_font, car.Value.ToString(), new Vector2(xPos + 360, yPos + 20), Color.Black);
                ranking += 1;
                yPos += 20;
            }
        }
    }
}
