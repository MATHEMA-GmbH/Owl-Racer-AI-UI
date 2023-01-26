using Matlabs.OwlRacer.Common.Model;
using Matlabs.OwlRacer.GameClient.Services;
using Matlabs.OwlRacer.GameClient.Services.Interface;
using Matlabs.OwlRacer.GameClient.States.Layout;
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


        // UI scaling
        private double _scaleY;
        private double _scaleX;
        private float _scaleFactor;

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
            _scaleX = ((float)LayoutUtility.screenWidth / (float)1920);
            _scaleY = ((float)LayoutUtility.screenHeight/ (float)1200);
            _scaleFactor = (float)(Math.Min(_scaleX,_scaleY));

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

            int logoRectWidth = (int)((double)_logo.Width * 0.3 * _scaleX);
            int logoRectHeight = (int)((double)_logo.Height * 0.3 * _scaleY);

            int logoRectMathemaWidth = (int)((double)_logoMathema.Width * 0.1 * _scaleX);
            int logoRectMathemaHeight = (int)((double)_logoMathema.Height * 0.1 * _scaleY);

            int logoRectCircleWidth = LayoutUtility.circleWidth();
            int logoRectCircleHeight = logoRectCircleWidth;


            Rectangle logoRectMathema = new Rectangle(LayoutUtility.bottomRightXValue(), LayoutUtility.bottomRightYValue(2),
            logoRectMathemaWidth, logoRectMathemaHeight);
            
            Rectangle logoRect = new Rectangle(LayoutUtility.bottomRightXValue(), LayoutUtility.bottomRightYValue(0),
            logoRectWidth, logoRectHeight);
            
            Rectangle logoRectCircle = new Rectangle((int)(LayoutUtility.screenWidth -logoRectCircleWidth), LayoutUtility.screenHeight - logoRectCircleHeight, logoRectCircleWidth, logoRectCircleHeight);
            
            Rectangle logoRectStreet1 = new Rectangle(0, GraphicsDevice.Adapter.CurrentDisplayMode.Height - 8 * GraphicsDevice.Adapter.CurrentDisplayMode.Height / _street.Height,
                GraphicsDevice.Adapter.CurrentDisplayMode.Width, _street.Height * GraphicsDevice.Adapter.CurrentDisplayMode.Height / 8);

            spriteBatch.Draw(_street, logoRectStreet1, Color.White);
            spriteBatch.Draw(_circle, logoRectCircle, Color.White);
            spriteBatch.Draw(_logo, logoRect, Color.White);

            spriteBatch.DrawString(_fontSmall, "EIN PROJEKT DER", LayoutUtility.bottomRightVectorPosXY(1), _corporateGray60, (float)0.0, new Vector2(0, 0), _scaleFactor, SpriteEffects.None, (float)0.0);
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

            //General scoreboard parameters
            int columnindex = 0;
            int maxColumnIndex = 5;
            double scoreBorderTop = 0.01;
            double scoreBorderLeftRight = 0.01;

            //Pixel values for Scoreboard
            int scoreBoardBorderTopPx = (int)(scoreBorderTop * LayoutUtility.screenHeight);
            int scoreBoardBorderLeftRightPx = (int)(scoreBorderLeftRight* LayoutUtility.screenWidth);
            int scoreBoardColumnWidthPx = (int)((double)_font.MeasureString("01234567890123456789").X * _scaleFactor);
            int scoreBoardLineHeightPx = (int)((double)_font.MeasureString("A").Y * _scaleFactor);
            
            //Pixel Values for overall Scoreboard
            int scoreBoardWidth = maxColumnIndex * scoreBoardColumnWidthPx + 2 * scoreBoardBorderLeftRightPx;
            int scoreBoardHeight = (sortedList.Count + 1) * scoreBoardLineHeightPx + 2 * scoreBoardBorderTopPx;

            // Initial position on the x and y Axis
            // Values are set so that the scoreboard appears in the middle of the screen
            int xPos = GraphicsDevice.Adapter.CurrentDisplayMode.Width/2 - scoreBoardWidth/2;
            int yPos = GraphicsDevice.Adapter.CurrentDisplayMode.Height / 2 - scoreBoardHeight/2;
            
            
            //_startPos = new StartPosition(GraphicsDevice.Adapter.CurrentDisplayMode.Width / 2 - _trackWidth / 2, GraphicsDevice.Adapter.CurrentDisplayMode.Height / 2 - _trackHeight / 2);


            Rectangle scoreBoardBackground = new Rectangle(xPos - scoreBoardBorderLeftRightPx, yPos - scoreBoardBorderTopPx, scoreBoardWidth, scoreBoardHeight);
            spriteBatch.Draw(_background, scoreBoardBackground, null, _corporateGray40, 0, new Vector2(0, 0), SpriteEffects.None, 0);
            
            //Header above scoreboard            
            spriteBatch.DrawString(_font, "Exit Game (press Escape) ", new Vector2(xPos, yPos - (scoreBoardBorderTopPx + scoreBoardLineHeightPx)), Color.Black, (float)0.0, new Vector2(0, 0), _scaleFactor, SpriteEffects.None, (float)0.0);

            // Column Headers
            spriteBatch.DrawString(_font, "Rank", new Vector2(xPos + columnindex * scoreBoardColumnWidthPx , yPos), Color.Black, (float)0.0, new Vector2(0, 0), _scaleFactor, SpriteEffects.None, (float)0.0);
            columnindex++;
            spriteBatch.DrawString(_font, "Name", new Vector2(xPos + columnindex * scoreBoardColumnWidthPx, yPos), Color.Black, (float)0.0, new Vector2(0, 0), _scaleFactor, SpriteEffects.None, (float)0.0);
            columnindex++;
            spriteBatch.DrawString(_font, "Score", new Vector2(xPos + columnindex * scoreBoardColumnWidthPx, yPos), Color.Black, (float)0.0, new Vector2(0, 0), _scaleFactor, SpriteEffects.None, (float)0.0);
            columnindex++;
            spriteBatch.DrawString(_font, "Crashes", new Vector2(xPos + columnindex * scoreBoardColumnWidthPx, yPos), Color.Black, (float)0.0, new Vector2(0, 0), _scaleFactor, SpriteEffects.None, (float)0.0);
            columnindex++;
            spriteBatch.DrawString(_font, "Rounds", new Vector2(xPos + columnindex * scoreBoardColumnWidthPx, yPos), Color.Black, (float)0.0, new Vector2(0, 0), _scaleFactor, SpriteEffects.None, (float)0.0);
            columnindex=0;

            //Drawing actual Ranking
            var ranking = 1;
            yPos += scoreBoardLineHeightPx;

            foreach (var car in sortedList)
            {
                spriteBatch.DrawString(_font, ranking.ToString(), new Vector2(xPos, yPos), Color.Black, (float)0.0, new Vector2(0, 0), _scaleFactor, SpriteEffects.None, (float)0.0);
                columnindex++;
                spriteBatch.DrawString(_font, car.Key.Name , new Vector2(xPos + columnindex * scoreBoardColumnWidthPx, yPos), Color.Black, (float)0.0, new Vector2(0, 0), _scaleFactor, SpriteEffects.None, (float)0.0);
                columnindex++;
                spriteBatch.DrawString(_font, car.Value.ToString(), new Vector2(xPos + columnindex * scoreBoardColumnWidthPx, yPos ), Color.Black, (float)0.0, new Vector2(0, 0), _scaleFactor, SpriteEffects.None, (float)0.0);
                columnindex++;
                spriteBatch.DrawString(_font, car.Key.NumCrashes.ToString(), new Vector2(xPos + columnindex * scoreBoardColumnWidthPx, yPos), Color.Black, (float)0.0, new Vector2(0, 0), _scaleFactor, SpriteEffects.None, (float)0.0);
                columnindex++;
                spriteBatch.DrawString(_font, car.Key.NumRounds.ToString(), new Vector2(xPos + columnindex * scoreBoardColumnWidthPx, yPos), Color.Black, (float)0.0, new Vector2(0, 0), _scaleFactor, SpriteEffects.None, (float)0.0);
                columnindex=0;
                ranking += 1;
                yPos += scoreBoardLineHeightPx;
            }
        }
    }
}
