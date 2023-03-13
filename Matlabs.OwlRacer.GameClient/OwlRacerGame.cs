using Grpc.Core;
using Matlabs.OwlRacer.Common.Model;
using Matlabs.OwlRacer.GameClient.Services.Interface;
using Matlabs.OwlRacer.GameClient.States;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Matlabs.OwlRacer.Common.Options;
using Matlabs.OwlRacer.GameClient.States.Options;

namespace Matlabs.OwlRacer.GameClient
{
    public class OwlRacerGame : Game
    {
        private readonly ILogger<OwlRacerGame> _logger;
        private readonly Channel _grpcChannel;

        public Boolean IsSpectator { get; set; }
        public Boolean IsAdmin { get; set; }
        public Session Session { get; set; }
        public VectorOptions _startPosition;

        private readonly GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        
        // Game states
        private IState _nextState;
        private IState _currentState;

        private readonly IStateFactory _stateFactory;

        //Corporate colors
        private Color _corporateRed = new Color(197, 0, 62);
        private Color _corporateGray20 = new Color(217, 217, 214);
        private Color _corporateGray40 = new Color(187, 188, 188);
        private Color _corporateGray60 = new Color(136, 139, 141);
        private Color _corporateGray80 = new Color(83, 86, 90);


        public OwlRacerGame(
            ILogger<OwlRacerGame> logger,
            Channel grpcChannel,
            IStateFactory stateFactory)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _grpcChannel = grpcChannel ?? throw new ArgumentNullException(nameof(grpcChannel));
            _stateFactory = stateFactory;

            _graphics = new GraphicsDeviceManager(this);
            _startPosition = new(100, 100);
            

            Content.RootDirectory = "Content";

            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            _logger.LogInformation("Initializing MonoGame Window");

            _graphics.PreferredBackBufferWidth = _graphics.GraphicsDevice.Adapter.CurrentDisplayMode.Width;
            _graphics.PreferredBackBufferHeight = _graphics.GraphicsDevice.Adapter.CurrentDisplayMode.Height;
            _graphics.IsFullScreen = false;
            _graphics.ApplyChanges();

            

            base.Initialize();
        }

        public void ChangeState(IState state)
        {
            _nextState = state;
        }
        
        protected override void LoadContent()
        {
            _logger.LogInformation("Loading MonoGame Base Content");
            _spriteBatch = new SpriteBatch(GraphicsDevice);
        }

        protected override void Update(GameTime gameTime)
        {
            if (_nextState != null)
            {
                _logger.LogInformation($"Switching state to {_nextState.GetType().Name}");

                _currentState = _nextState;
                _nextState = null;
                _currentState.LoadContent(gameTime);
            }
            else if(_currentState == null)
            {
                // Create the main window if we have just started the application (_currentState
                // and _nextState are null).
                _logger.LogInformation("No state was set. Setting menu state");
                _currentState ??= _stateFactory.CreateState<IMenuState<MenuStateOptions>, MenuStateOptions>(_graphics.GraphicsDevice, Content);

                _currentState.LoadContent(gameTime);
            }

            _currentState.Update(gameTime);
            _currentState.PostUpdate(gameTime);

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(_corporateGray60);
            
            _currentState.Draw(gameTime, _spriteBatch);
            base.Draw(gameTime);
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                _grpcChannel.ShutdownAsync().Wait();
            }
            finally
            {
                base.Dispose(disposing);
            }
        }
    }
}
