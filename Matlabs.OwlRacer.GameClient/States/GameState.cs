using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.InkML;
using DocumentFormat.OpenXml.Math;
using DocumentFormat.OpenXml.Presentation;
using DocumentFormat.OpenXml.Spreadsheet;
using Grpc.Core;
using Grpc.Net.Client;
using Matlabs.OwlRacer.Common.Model;
using Matlabs.OwlRacer.Common.Options;
using Matlabs.OwlRacer.GameClient.Controls;
using Matlabs.OwlRacer.GameClient.Services;
using Matlabs.OwlRacer.GameClient.Services.Interface;
using Matlabs.OwlRacer.GameClient.States.Layout;
using Matlabs.OwlRacer.GameClient.States.Options;
using Matlabs.OwlRacer.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using static System.Net.Mime.MediaTypeNames;
using Color = Microsoft.Xna.Framework.Color;

namespace Matlabs.OwlRacer.GameClient.States
{
    public class GameState : StateBase<GameStateOptions>, IGameState<GameStateOptions>
    {
        // Constants
        private const float GameTimeSetting = 50f;

        // Services
        private readonly ISessionService _sessionService;
        private readonly IGameService _gameService;
        private readonly IResourceService _resourceService;
        private readonly IStateFactory _stateFactory;

        // Options
        private readonly PythonOptions _pythonOptions;
        private readonly MLNetOptions _mlNetOptions;
        private readonly GenericOptions _genericOptions;

        // Race Car
        private List<RaceCar> _raceCarList = new();
        private RaceCar _raceCar;
        //private List<RaceCar> _driverList = new();
        

        // Race Track
        private Texture2D _raceTrackTexture;
        private Texture2D _raceCarTexture;
        private Texture2D _startLineTexture;
        private byte[] _raceTrackImageData;
        private byte[] _raceCarImageData;
        private byte[] _startLineImageData;
        private int _trackWidth;
        private int _trackHeight;
        private VectorOptions _startPos;
        private StartLineOptions _startLinePos;
        private VectorOptions _posInfo;
        
        private SpriteFont _buttonFont;

        private Texture2D _buttonTextureRed;
        private Texture2D _buttonTexture;
        private Texture2D _buttonTextureX;
        private Texture2D _background;
        private Texture2D _logo;
        private Texture2D _logoMathema;
        private Texture2D _circle;
        private Texture2D _street;
        private Texture2D _finishFlag;
        private Dictionary<int, Texture2D> _startPhaseTextures = new();

        private List<Component> _components;
        private List<RaceCarButton> _raceCarButtons = new();
        private List<ModelButton> _modelButtons = new();
        private List<RaceCarButton> _raceCarRemoveButtons = new();
        private int _numPlayers = 0;
        private bool _adminOldStatus = false;

        // Game Specific Data
        private bool _isSpectator;
        private StreamWriter _dataWriter;
        private bool _capture;
        private bool _gamePadVibration;
        private bool _gamePadConnected;
        private bool _debugState = true;
        private GamePadState _previousGamePadState;

        private string _raceCarInfo = "";

        private Color _corporateGray60 = new Color(136, 139, 141);

        private SpriteFont _font;
        private SpriteFont _fontSmall;
        private SpriteFont _raceCarNameFont;

        private readonly ILogger<GameState> _logger;

        private string _logFilePath;


        //UI scaling
        private double _scaleY;
        private double _scaleX;
        private float _scaleFactor;
        
        //Fixed pixel lengths for elements
        private int _buttonHeight;
        private int _buttonWidth;
        private int _textLineHeight;

        //DarkMode
        private bool _darkMode;

        //Logging
        private string _oldFilePath;

        //Admin Feature
        private bool _raceIsFinished;
        private bool _isAdmin;
        private List<RaceCar> _raceCarFinalPositions;

        private int _modelButtonWidth;
        private Button _modelStartButton;
        private Button _raceFinishedButton;
        private Button _finishRaceButton;

        public GameState(
            OwlRacerGame game,
            ILogger<GameState> logger,
            ISessionService sessionService,
            IGameService gameService,
            IResourceService resourceService,
            IStateFactory stateFactory,
            IOptions<PythonOptions> pythonOptions,
            IOptions<MLNetOptions> mlNetOptions,
            IOptions<GenericOptions> genericOptions)
            : base(game, logger)
        {
            _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
            _gameService = gameService ?? throw new ArgumentNullException(nameof(gameService));
            _resourceService = resourceService ?? throw new ArgumentNullException(nameof(resourceService));
            _stateFactory = stateFactory ?? throw new ArgumentNullException(nameof(stateFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _pythonOptions = pythonOptions.Value;
            _mlNetOptions = mlNetOptions.Value;
            _genericOptions = genericOptions.Value;
        }

        public override void Initialize(GraphicsDevice graphicsDevice, ContentManager content, GameStateOptions options)
        {
            base.Initialize(graphicsDevice, content, options);

            _font = Content.Load<SpriteFont>("Inter-SemiBold");
            _fontSmall = Content.Load<SpriteFont>("Inter-Regular-small");
            _buttonFont = Content.Load<SpriteFont>("Inter-Regular");

            _isSpectator = Game.IsSpectator;
            _isAdmin = Game.IsAdmin;

            _components = new List<Component>(){};

            _numPlayers = 0;

            _dataWriter = null;

            _darkMode = false;

            _raceCarFinalPositions = null;

            //Calculating parameters for UI-scaling
            _scaleX = ((float)(GraphicsDevice.Adapter.CurrentDisplayMode.Width) / (float)1920);
            _scaleY = ((float)(GraphicsDevice.Adapter.CurrentDisplayMode.Height) / (float)1200);
            _scaleFactor = (float)Math.Min(_scaleX,_scaleY);

            //Fixed size Values for buttons and text lines
            _buttonHeight = LayoutUtility.heightPx(1);
            _buttonWidth = LayoutUtility.widthPx(1);
            _textLineHeight = (int)((float)_font.MeasureString("A").Y * _scaleFactor);

            // Logging information 
            _logFilePath = Path.Join(Directory.GetCurrentDirectory(), "capture");
            _oldFilePath = "";
            if (!Directory.Exists(_logFilePath))
            {
                Directory.CreateDirectory(_logFilePath);
            }

            _raceIsFinished = _sessionService.RaceIsFinished(new GuidData { GuidString = Game.Session.Id.ToString() });
            if (!_isSpectator && _raceIsFinished == false)
            {
                Logger.LogInformation("Creating new RaceCar.");
                _raceCar = _gameService.CreateRaceCarAsync(Game.Session, 0.5f, 0.05f, Options.RaceCarName, string.Empty).Result;
                _raceCarList.Add(_raceCar);
                Logger.LogInformation($"---> RaceCar successfully created (ID={_raceCar.Id}, name={_raceCar.Name}, color={_raceCar.Color}");
                
            }
        }

        public override void LoadContent(GameTime gameTime)
        {
            _buttonTextureRed = Content.Load<Texture2D>("Images/ButtonRedMini");
            _buttonTexture = Content.Load<Texture2D>("Images/Button");
            _buttonTextureX = Content.Load<Texture2D>("Images/ButtonX");
            _raceCarNameFont = Content.Load<SpriteFont>("Inter-Regular-small");

            var rawBaseImageData = _resourceService.GetBaseImageDataAsync().Result;
            var rawTrackImageData = _resourceService.GetTrackImageDataAsync(Game.Session.RaceTrack.TrackNumber).Result;
            _raceTrackImageData = rawTrackImageData.RaceTrack.ToByteArray();
            _raceCarImageData = rawBaseImageData.Car.ToByteArray();
            _startLineImageData = rawBaseImageData.StartLine.ToByteArray();

            using var raceTrackMem = new MemoryStream(_raceTrackImageData);
            _raceTrackTexture = Texture2D.FromStream(GraphicsDevice, raceTrackMem);

            using var raceCarMem = new MemoryStream(_raceCarImageData);
            _raceCarTexture = Texture2D.FromStream(GraphicsDevice, raceCarMem);

            using var startLineMem = new MemoryStream(_startLineImageData);
            _startLineTexture = Texture2D.FromStream(GraphicsDevice, startLineMem);

            using var raceTrackImage = SixLabors.ImageSharp.Image.Load(_raceTrackImageData);
            _trackWidth = raceTrackImage.Width;
            _trackHeight = raceTrackImage.Height;

            _startPhaseTextures = _resourceService.GetStartPhaseImageData().Result.ToDictionary(
                x => x.Key,
                y =>
                {
                    using var texMem = new MemoryStream(y.Value);
                    return Texture2D.FromStream(GraphicsDevice, texMem);

                });

            _background = new Texture2D(GraphicsDevice, 1, 1, false, SurfaceFormat.Color);
            _background.SetData(new[] { Color.White });

            _finishFlag = Content.Load<Texture2D>(@"Images/Finish-Flag");
            _logo = Content.Load<Texture2D>(@"Images/owlracer-logo-solo");
            _logoMathema = Content.Load<Texture2D>(@"Images/mathema-logo");
            _circle = Content.Load<Texture2D>(@"Images/Circle");
            _street = Content.Load<Texture2D>(@"Images/Street");
            _startPos = new VectorOptions((int)(GraphicsDevice.Adapter.CurrentDisplayMode.Width*0.01), (int)((GraphicsDevice.Adapter.CurrentDisplayMode.Height - _trackHeight - _logo.Height/3)*0.5));
            _posInfo = new VectorOptions((int)(_trackWidth + LayoutUtility.widthPx(0.1)), 20);
            _startLinePos = Game.Session.RaceTrack.StartLine;

            // Width of model buttons is calculated to fill the space from the keybindings information to the end of the racetrack in steps of 4
            _modelButtonWidth = (_raceTrackTexture.Width - (int)((_font.MeasureString("D: Show statistics   ").X) * _scaleFactor)) / 4;
        }

        public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            spriteBatch.Begin();

            //Color values used for normal mode
            Color backgroundColor = _corporateGray60;
            Color raceTrackFilter = Color.White;
            
            //If clause to enable darkMode options
            if(_darkMode)
            {
                backgroundColor = Color.Black;
                raceTrackFilter = Color.Black;
            }


            spriteBatch.Draw(_background, new Rectangle(_startPos.X, _startPos.Y, _trackWidth, _trackHeight), null, backgroundColor, 0, new Vector2(0, 0), SpriteEffects.None, 0);


            DrawStartLine(spriteBatch);

            // Text for adding models in right next to the keybindings info text and 3 buttonheights above the track
            if(Game.IsAdmin)
            {
                spriteBatch.DrawString(_font, "Choose model to add:", new Vector2(_startPos.X + (int)((_font.MeasureString("D: Show statistics   ").X) * _scaleFactor), (int)(_startPos.Y - 3 * _buttonHeight)), Color.Black, (float)(0.0), new Vector2(0, 0), _scaleFactor, SpriteEffects.None, (float)0.0);
            }

            //Displaytext for Controls abvoe Racetrack

            // Positioning of texts starts 4 lines of text above the start position of the track and goes down from there
            spriteBatch.DrawString(_font, "Esc: Quit game", new Vector2(_startPos.X, _startPos.Y - 4 * _font.MeasureString("A").Y * _scaleFactor), Color.Black, (float)(0.0), new Vector2(0, 0), _scaleFactor, SpriteEffects.None, (float)0.0);
            spriteBatch.DrawString(_font, "D: Show statistics", new Vector2(_startPos.X, _startPos.Y - 3 * _font.MeasureString("A").Y * _scaleFactor), Color.Black, (float)(0.0), new Vector2(0, 0), _scaleFactor, SpriteEffects.None, (float)0.0);
            spriteBatch.DrawString(_font, "K: Dark-Mode", new Vector2(_startPos.X, _startPos.Y - 2 * _font.MeasureString("A").Y * _scaleFactor), Color.Black, (float)(0.0), new Vector2(0, 0), _scaleFactor, SpriteEffects.None, (float)0.0);
            spriteBatch.DrawString(_font, "L: Log game", new Vector2(_startPos.X, _startPos.Y - _font.MeasureString("A").Y * _scaleFactor), Color.Black, (float)(0.0), new Vector2(0, 0), _scaleFactor, SpriteEffects.None, (float)0.0);

            spriteBatch.Draw(
                _raceTrackTexture,
                new Rectangle(_startPos.X , _startPos.Y, _trackWidth, _trackHeight),
                raceTrackFilter
                );

            DrawStartPhase(spriteBatch);

            if (!_isSpectator)
            {
                DrawDebugText(spriteBatch, _raceCar);

                if (_capture)
                {
                    DrawRecordingSquare(spriteBatch);
                }
                //if (_gamePadConnected) //_gamePadConnected
                //{
                //    DrawVibrationText(spriteBatch);
                //}  
            }
            else if (_isSpectator && _capture && _raceCarInfo != "")
            {
                DrawRecordingSquare(spriteBatch);
            }



            var myRaceCarId = 0;
            foreach (var raceCar in _raceCarList)
            {
                var color = Color.Black;

                if (raceCar.Color.Length == 0)
                {
                    color = GetRaceCarColor(myRaceCarId);
                }
                else
                {
                    color = GetRaceCarColorFromString(raceCar);
                }
                
                DrawSingleCar(raceCar, color, spriteBatch);
                DrawDinstanceLines(raceCar, color, spriteBatch);
                myRaceCarId += 1;
                
                if (_raceCarInfo == raceCar.Id.ToString())
                {
                    DrawDebugText(spriteBatch, raceCar);
                }
            }


            // Draws Cars in last position, when race is finished
            _raceIsFinished = _sessionService.RaceIsFinished(new GuidData { GuidString = Game.Session.Id.ToString() });
            if (_raceIsFinished && _raceCarFinalPositions != null)
            {
                DrawFinishPositionRaceCarWithCars(_raceCarFinalPositions, spriteBatch);
            }

            foreach (var component in _components)
            {
                component.Draw(gameTime, spriteBatch);
            }

            int logoRectWidth = (int)((double)_logo.Width * 0.3 * _scaleX);
            int logoRectHeight = (int)((double)_logo.Height * 0.3 * _scaleY);

            int logoRectMathemaWidth = (int)((double)_logoMathema.Width * 0.1 * _scaleX);
            int logoRectMathemaHeight = (int)((double)_logoMathema.Height * 0.1 * _scaleY);

            int logoRectCircleWidth = LayoutUtility.circleWidth();
            int logoRectCircleHeight = logoRectCircleWidth;


            Rectangle logoRectMathema = new Rectangle(LayoutUtility.bottomRightXValue(), LayoutUtility.bottomRightYValue(2), logoRectMathemaWidth, logoRectMathemaHeight);
            Rectangle logoRect = new Rectangle(LayoutUtility.bottomRightXValue(), LayoutUtility.bottomRightYValue(0), logoRectWidth, logoRectHeight);
            Rectangle logoRectCircle = new Rectangle(LayoutUtility.screenWidth - logoRectCircleWidth, LayoutUtility.screenHeight - logoRectCircleHeight, logoRectCircleWidth, logoRectCircleHeight);

            Rectangle logoRectStreet1 = new Rectangle(0, GraphicsDevice.Adapter.CurrentDisplayMode.Height - 8 * GraphicsDevice.Adapter.CurrentDisplayMode.Height / _street.Height,
                GraphicsDevice.Adapter.CurrentDisplayMode.Width, _street.Height * GraphicsDevice.Adapter.CurrentDisplayMode.Height / 8);

            spriteBatch.Draw(_street, logoRectStreet1, Color.White);
            spriteBatch.Draw(_circle, logoRectCircle, Color.White);
            spriteBatch.Draw(_logo, logoRect, Color.White);

            spriteBatch.DrawString(_fontSmall, "EIN PROJEKT DER", LayoutUtility.bottomRightVectorPosXY(1), _corporateGray60, (float)0.0, new Vector2(0, 0), _scaleFactor, SpriteEffects.None, (float)0.0);
            spriteBatch.Draw(_logoMathema, logoRectMathema, Color.White);

            string typePlayer = _isSpectator ? "Spectator" : "Player";
            typePlayer += _isAdmin ? "&Admin": "";

            spriteBatch.DrawString(_font, $"Session: {Game.Session.Name}", new Vector2(_trackWidth / 2 - 40, 20), Color.Black,(float)0.0,new Vector2(0,0), _scaleFactor, SpriteEffects.None, (float)0.0);
            spriteBatch.DrawString(_font, $"Logged in as: {typePlayer}", new Vector2(_trackWidth / 2 - 40, 20 + _font.MeasureString("A").Y), Color.Black, (float)0.0, new Vector2(0, 0), _scaleFactor, SpriteEffects.None, (float)0.0);
            //DrawRankingText(spriteBatch, 10, 300, _raceCarList);

            Rectangle finishFlagRect = new Rectangle(60 * (int)_font.MeasureString("A").X + LayoutUtility.widthPx(0.25) + _modelButtonWidth - _buttonHeight, 10, _buttonHeight, _buttonHeight);

            if(Game.IsAdmin == true)
            {
                spriteBatch.Draw(_finishFlag, finishFlagRect, Color.White);
            }
            spriteBatch.End();
        }

        private void DrawStartLine(SpriteBatch spriteBatch)
        {
            var remainingStartLine = (_startLinePos.End.Y - _startLinePos.Start.Y);
            var tiles = 0;
            for (; remainingStartLine > 16; remainingStartLine -= 16, tiles++)
            {
                spriteBatch.Draw(
                    _startLineTexture,
                    new Rectangle(
                        _startPos.X + _startLinePos.Start.X,
                        _startPos.Y + _startLinePos.Start.Y + (tiles * 16),
                        16,
                        16),
                    Color.White);
            }

            if (remainingStartLine > 0)
            {
                spriteBatch.Draw(
                    _startLineTexture,
                    new Rectangle(
                        _startPos.X + _startLinePos.Start.X,
                        _startPos.Y + _startLinePos.Start.Y + (tiles * 16),
                        16,
                        remainingStartLine),
                    Color.White);
            }
        }

        private void DrawStartPhase(SpriteBatch spriteBatch)
        {
            int fontWidth = (int)(_font.MeasureString("A").X*_scaleFactor);
            spriteBatch.DrawString(_font, $"{(Game.Session.GameTime.Ticks < 0 ? "-": "")} {Math.Abs(Game.Session.GameTime.Minutes)}:{Math.Abs(Game.Session.GameTime.Seconds)}.{Math.Abs(Game.Session.GameTime.Milliseconds)}", new Vector2(10, 10), Color.Black,(float)0.0,new Vector2(0,0), _scaleFactor,SpriteEffects.None, (float)0.0);

            var phase = Game.Session.HasRaceStarted ? 0 : Math.Clamp(Math.Abs(Game.Session.GameTime.Seconds - 1), 0, 5);

            spriteBatch.Draw(
                _startPhaseTextures[phase],
                new Rectangle(67*fontWidth, 10, LayoutUtility.widthPx(0.25), LayoutUtility.heightPx(1)),
                Color.White);
        }

        public override void Update(GameTime gameTime)
        {

            // Update 

            _raceIsFinished = _sessionService.RaceIsFinished(new GuidData { GuidString = Game.Session.Id.ToString() });
            if (_raceIsFinished == true)
            {
                // change needed to prevent update bug of players own race car
                _isSpectator = true;
            }
            else
            {
                // Updates the cars final positions as long as race isn't finished
                // With a sufficient delay between finishing the race and deleting the cars
                // This should allow clients to keep a pretty accurate final position of cars,
                // when the race finishes
                _raceCarFinalPositions = FinishPositionListRaceCarWithCars();
            }
            OwlKeyboard.GetState();
            var keyState = OwlKeyboard.currentKeyState;
            var currentState = GamePad.GetState(PlayerIndex.One);
            CheckExitGame();
            CheckExecuteScript();

            Game.Session = _sessionService.GetSession(Game.Session.Id);
            //if (gameTime.TotalGameTime.Seconds % 2 == 0)
            if (true)
            {
                var carIds = _gameService.GetRaceCarIdsAsync(Game.Session.Id).Result;

                if (!carIds.Any())
                {
                    _raceCarList.Clear();
                }

                // if a car is not in the carIds list, we will remove it and not track it anymore
                var carsToRemove = _raceCarList.Where(car => carIds.Any(id => car.Id != id)).ToList();
                foreach (var raceCar in carsToRemove)
                {
                    _raceCarList.Remove(raceCar);
                }

                // track a car when a it appears ind the carIds list
                // limit to 10 because we only have 10 colors
                var newCars = carIds
                    //.Take(10)
                    .Where(id => _raceCarList.All(car => car.Id != id))
                    .Select(newId => new RaceCar(new Guid(), newId, string.Empty, string.Empty));

                foreach (var newCar in newCars)
                {

                    if (!_isSpectator)
                    {
                        if (newCar.Id == _raceCar.Id)
                        {
                            newCar.Color = _raceCar.Color;
                            newCar.Name = _raceCar.Name;
                            _raceCarList.Add(newCar);
                        }
                        else
                        {
                            try
                            {
                                var carData = _gameService.GetRaceCarDataAsync(newCar).Result;
                                newCar.Name = carData.Name;
                                newCar.Color = carData.Color;
                                _raceCarList.Add(newCar);
                            }

                            catch
                            {
                                Logger.LogInformation($"car {newCar.Id} has been already deleted");
                            }
                        }
                    }
                    else
                    {
                        
                        try
                        {
                            var carData = _gameService.GetRaceCarDataAsync(newCar).Result;
                            var name = carData.Name;
                            var color = carData.Color;

                            newCar.Name = name;
                            newCar.Color = color;
                            _raceCarList.Add(newCar);
                        }
                        catch
                        {
                            Logger.LogInformation($"car {newCar.Id} has been already deleted");
                        }
                    }
                }
            }

            if (!_isSpectator)
            {
                try
                {
                    _gameService.UpdateRaceCarDataAsync(_raceCar).Wait(3000);
                }
                catch
                {
                    Logger.LogError("Could not update car after 3 seconds");
                } 

                if (keyState.IsKeyDown(Keys.Escape))
                {
                    if(_dataWriter!= null)
                    {
                        _dataWriter.Close();
                    }
                    _raceCarList.Remove(_raceCar);
                    _gameService.DestroyRaceCarAsync(_raceCar);   
                }

                var stepCommand = StepData.Types.StepCommand.Idle;

                _gamePadConnected = currentState.IsConnected;
                if (currentState.IsConnected)
                {
                    stepCommand = GetGamePadStepCommand(currentState, stepCommand);
                    _previousGamePadState = currentState;
                }



                stepCommand = GetKeyboardStepCommand(keyState, stepCommand);
              
            }

            if (OwlKeyboard.HasBeenPressed(Keys.L))
            {
                _capture = !_capture;
            }
            
            Parallel.ForEach(_raceCarList, raceCar =>
            {
                try
                {
                    _gameService.UpdateRaceCarDataAsync(raceCar).Wait(3000);
                }
                catch
                {
                    Logger.LogError("Could not update car after 3 seconds");
                }
            });

            if (_capture && _raceCarInfo != "" && _isAdmin)
            {
                int index = _raceCarList.FindIndex(x => x.Id.ToString() == _raceCarInfo);
                if (index >= 0)
                {
                    logRacecar(_raceCarList.ElementAt(index));
                }
            }
            else if (_capture && _raceCarInfo == "" && !_isSpectator&& _raceCarList.Contains(_raceCar))
            { 
                logRacecar(_raceCar);
            }

            UpdatePlayers();

            if (!_components.Contains(_modelStartButton) && Game.IsAdmin == true)
            {
                DrawModelCarButtons();
            }

            if (!_components.Contains(_finishRaceButton) && Game.IsAdmin == true)
            {
                DrawFinishRaceButton();
            }

            _raceIsFinished = _sessionService.RaceIsFinished(new GuidData { GuidString = Game.Session.Id.ToString() });
            if (!(_components.Contains(_raceFinishedButton)) && _raceIsFinished == true)
            {
                DrawRaceFinishedButton();
            }


            foreach (var component in _components)
            {
                component.Update(gameTime);
            }
        }

        private Color GetRaceCarColor(int raceCarid)
        {
            //limited to 10 cars
            raceCarid = raceCarid % 10;
            Color[] colorArray = new Color[] { Color.White, Color.Pink, Color.Orange, Color.Red, Color.Yellow, Color.Violet, Color.Green, Color.AliceBlue, Color.Coral, Color.Brown };
            Color color = colorArray[raceCarid];
            return color;
        }

        private Color GetRaceCarColorFromString(RaceCar raceCar)
        {

            var hexString = raceCar.Color;

            //replace # occurences
            if (hexString.IndexOf('#') != -1)
                hexString = hexString.Replace("#", "");

            int r, g, b = 0;

            r = int.Parse(hexString.Substring(0, 2), NumberStyles.AllowHexSpecifier);
            g = int.Parse(hexString.Substring(2, 2), NumberStyles.AllowHexSpecifier);
            b = int.Parse(hexString.Substring(4, 2), NumberStyles.AllowHexSpecifier);

            Color color = new Color(r, g, b);

            return color;
        }

        private void DrawSingleCar(RaceCar raceCar, Color color, SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(
                    _raceCarTexture,
                    new Rectangle((int)(raceCar).Position.X + _startPos.X, (int)raceCar.Position.Y + _startPos.Y, _raceCarTexture.Width, _raceCarTexture.Height),
                    new Rectangle(0, 0, _raceCarTexture.Width, _raceCarTexture.Height),
                    color,
                    raceCar.Rotation,
                    new Vector2((int)(_raceCarTexture.Width / 2), (int)(_raceCarTexture.Height / 2)),
                    SpriteEffects.None,
                    1.0f
                );
            ;
            if (raceCar.Name.Length == 0)
            {
                raceCar.Name = "ToDo";
            }
            spriteBatch.DrawString(_raceCarNameFont, raceCar.Name, new Vector2((int)(raceCar).Position.X + _startPos.X, (int)raceCar.Position.Y + _startPos.Y - 40), color);
 
        }

        private void DrawDinstanceLines(RaceCar raceCar, Color color, SpriteBatch _spriteBatch)
        {
            var widthOffsetFront = (int)_raceCarTexture.Width / 2;
            var widthOffsetSide = (int)_raceCarTexture.Height / 2;
            var diagOffset = (int)Math.Sqrt(Math.Pow(widthOffsetFront, 2) + Math.Pow(widthOffsetSide, 2));

            Texture2D line = new Texture2D(_spriteBatch.GraphicsDevice, 1, 1, false, SurfaceFormat.Color);
            line.SetData(new[] { color });

            _spriteBatch.Draw(line, new Rectangle((int)raceCar.Position.X + _startPos.X, (int)raceCar.Position.Y + _startPos.Y, raceCar.Distance.Front + widthOffsetFront, 1), null, color, raceCar.Rotation, new Vector2(0, 0), SpriteEffects.None, 0);
            _spriteBatch.Draw(line, new Rectangle((int)raceCar.Position.X + _startPos.X, (int)raceCar.Position.Y + _startPos.Y, raceCar.Distance.Right + widthOffsetSide, 1), null, color, MathHelper.ToRadians(90) + raceCar.Rotation, new Vector2(0, 0), SpriteEffects.None, 0);
            _spriteBatch.Draw(line, new Rectangle((int)raceCar.Position.X + _startPos.X, (int)raceCar.Position.Y + _startPos.Y, raceCar.Distance.Left + widthOffsetSide, 1), null, color, MathHelper.ToRadians(270) + raceCar.Rotation, new Vector2(0, 0), SpriteEffects.None, 0);
            _spriteBatch.Draw(line, new Rectangle((int)raceCar.Position.X + _startPos.X, (int)raceCar.Position.Y + _startPos.Y, raceCar.Distance.FrontRight + diagOffset, 1), null, color, MathHelper.ToRadians(55) + raceCar.Rotation, new Vector2(0, 0), SpriteEffects.None, 0);
            _spriteBatch.Draw(line, new Rectangle((int)raceCar.Position.X + _startPos.X, (int)raceCar.Position.Y + _startPos.Y, raceCar.Distance.FrontLeft + diagOffset, 1), null, color, MathHelper.ToRadians(305) + raceCar.Rotation, new Vector2(0, 0), SpriteEffects.None, 0);
        }

        private StepData.Types.StepCommand GetKeyboardStepCommand(KeyboardState keyState, StepData.Types.StepCommand stepCommand)
        {
            if (keyState.IsKeyDown(Keys.Left))
            {
                stepCommand = keyState.IsKeyDown(Keys.Up)
                    ? StepData.Types.StepCommand.AccelerateLeft
                    : StepData.Types.StepCommand.TurnLeft;
            }

            else if (keyState.IsKeyDown(Keys.Right))
            {
                stepCommand = keyState.IsKeyDown(Keys.Up)
                    ? StepData.Types.StepCommand.AccelerateRight
                    : StepData.Types.StepCommand.TurnRight;
            }

            else if (keyState.IsKeyDown(Keys.Down))
            {
                stepCommand = StepData.Types.StepCommand.Decelerate;
            }
            else if (keyState.IsKeyDown(Keys.Up))
            {
                stepCommand = StepData.Types.StepCommand.Accelerate;
            }
            try
            {
                _gameService.StepAsync(_raceCar, stepCommand).Wait();
                if (keyState.IsKeyDown(Keys.Space))
                {
                    _gameService.ResetCarAsync(_raceCar);
                }
            }
            catch(Exception ex)
            {
                _logger.LogWarning($"Failed to step race car with id {_raceCar.Id}; message: {ex}");
            }

            if (OwlKeyboard.HasBeenPressed(Keys.K))
            {
                _darkMode= !_darkMode;
            }
            if(OwlKeyboard.HasBeenPressed(Keys.D))
            {
                _debugState= !_debugState;
            }

            return stepCommand;
        }

        private void CheckExitGame()
        {
            if (OwlKeyboard.HasBeenPressed(Keys.Escape))
            {
                Logger.LogInformation("RankingState");
                var rankingState = _stateFactory.CreateState<IRankingState<RankingStateOptions>, RankingStateOptions> (GraphicsDevice, Content);

                Game.ChangeState(rankingState);
            }
        }

        private void CheckExecuteScript()
        {
            _raceIsFinished = _sessionService.RaceIsFinished(new GuidData { GuidString = Game.Session.Id.ToString() });
            if (_raceIsFinished == false)
            {
                // Python
                if (_pythonOptions.ScriptMappings != null && _pythonOptions.BinPath != null)
                {

                    foreach (var mapping in _pythonOptions.ScriptMappings)
                    {
                        if (Enum.TryParse(typeof(Keys), mapping.Key, true, out var value))
                        {
                            var key = (Keys)value;

                            if (OwlKeyboard.HasBeenPressed(key))
                            {
                                Logger.LogInformation($"Executing python script {mapping.File}");
                                var args = $"{mapping.File} --session={Game.Session.Id} --carName={mapping.CarName} --carColor={mapping.CarColor} --model={mapping.Model}";
                                ExecuteScript(_pythonOptions.BinPath, args);
                            }
                        }
                    }
                }

                // ML.NET
                if (_mlNetOptions.ScriptMappings != null && _mlNetOptions.BinPath != null)
                {
                    foreach (var mapping in _mlNetOptions.ScriptMappings)
                    {
                        if (Enum.TryParse(typeof(Keys), mapping.Key, true, out var value))
                        {
                            var key = (Keys)value;

                            if (OwlKeyboard.HasBeenPressed(key))
                            {
                                var path = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), _mlNetOptions.BinPath));
                                Logger.LogInformation($"Executing ML.NET {mapping.File}");
                                var args = $"--model={mapping.File} --session={Game.Session.Id} --carName={mapping.CarName} --carColor={mapping.CarColor} --version={mapping.Version}";
                                ExecuteScript(path, args); //> venv?
                            }
                        }
                    }
                }

                // Generic Scripts
                if (_genericOptions.ScriptMappings != null)
                {
                    foreach (var mapping in _genericOptions.ScriptMappings)
                    {
                        if (Enum.TryParse(typeof(Keys), mapping.Key, true, out var value))
                        {
                            var key = (Keys)value;

                            if (OwlKeyboard.HasBeenPressed(key))
                            {
                                var args = mapping.Args.Replace("[SESSIONID]", Game.Session.Id.ToString());
                                Logger.LogInformation($"Executing generic script {mapping.File} with args \"{args}\"");

                                ExecuteScript(mapping.File, args, mapping.ShellExecute);
                            }
                        }
                    }
                }
            }
            
        }

        private void ExecuteScript(string exe, string args, bool shellExecute = false)
        {
            try
            {
                using (Process mlProcess = new Process())
                {
                    mlProcess.StartInfo.UseShellExecute = shellExecute;
                    mlProcess.StartInfo.FileName = exe;
                    mlProcess.StartInfo.Arguments = args;
                    mlProcess.StartInfo.CreateNoWindow = true;
                    mlProcess.Start();
                    mlProcess.StartInfo.RedirectStandardOutput = false;
                   // mlProcess.OutputDataReceived += MlProcess_OutputDataReceived;
                }
            }
            catch (Exception e)
            {
                Logger.LogInformation("Could not start ML Algorithm");
                Logger.LogInformation(e.ToString());
            }
        }

        private void MlProcess_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            Console.WriteLine(e.Data);
        }

        private StepData.Types.StepCommand GetGamePadStepCommand(GamePadState currentState, StepData.Types.StepCommand stepCommand)
        {
            if (currentState.Buttons.A == ButtonState.Pressed)
            {
                stepCommand = StepData.Types.StepCommand.Accelerate;
            }

            if (currentState.Buttons.B == ButtonState.Pressed)
            {
                stepCommand = StepData.Types.StepCommand.Decelerate;
            }

            if (currentState.ThumbSticks.Left.X > 0)
            {
                stepCommand = StepData.Types.StepCommand.TurnRight;
            }
            else if (currentState.ThumbSticks.Left.X < 0)
            {
                stepCommand = StepData.Types.StepCommand.TurnLeft;
            }

            if (currentState.Buttons.Y == ButtonState.Pressed &&
                _previousGamePadState.Buttons.Y == ButtonState.Released)
            {
                _gameService.ResetCarAsync(_raceCar);
            }

            if (currentState.Buttons.X == ButtonState.Pressed &&
                _previousGamePadState.Buttons.X != ButtonState.Pressed)
            {
                _capture = !_capture;
            }

            if (currentState.Buttons.RightShoulder == ButtonState.Pressed &&
                _previousGamePadState.Buttons.RightShoulder != ButtonState.Pressed)
            {
                _gamePadVibration = !_gamePadVibration;
            }

            if (_raceCar.IsCrashed && _gamePadVibration)
            {
                GamePad.SetVibration(PlayerIndex.One, 0.1f, 0.1f);
            }
            else
            {
                GamePad.SetVibration(PlayerIndex.One, 0.0f, 0.0f);
            }

            return stepCommand;
        }

        private void DrawDebugText(SpriteBatch spriteBatch, RaceCar raceCar)
        {
            int yPos = _posInfo.Y;
            int xPos = _posInfo.X;

            if (_raceCar != null)
            {
                
                if (!_isSpectator && _debugState && raceCar == _raceCar)
                { 
                    xPos = _posInfo.X;
                    //spriteBatch.DrawString(_font, $"You are driving Racecar: {raceCar.Name} (ID={_raceCar.Id})", new Vector2(xPos, yPos), Color.Black);
                    spriteBatch.DrawString(_font, $"You are driving Racecar: {raceCar.Name}", new Vector2(xPos, yPos), Color.Black,(float)0.0,new Vector2(0,0), _scaleFactor, SpriteEffects.None, (float)0.0);
                    DrawCarStatistics(spriteBatch, raceCar, xPos, yPos);
                }   
                else if (raceCar != _raceCar)
                {
                    xPos = _posInfo.X;
                    //spriteBatch.DrawString(_font, $"Racecar: {raceCar.Name} (ID={_raceCar.Id})", new Vector2(xPos, yPos), Color.Black);
                    spriteBatch.DrawString(_font, $"Racecar: {raceCar.Name}", new Vector2(xPos, yPos+ 6 * _textLineHeight), Color.Black,(float)0.0, new Vector2(0,0), _scaleFactor, SpriteEffects.None, (float)0.0);
                    DrawCarStatistics(spriteBatch, raceCar, xPos, yPos+ 6 * _textLineHeight);
                }
            }
            else
            {
                xPos = _posInfo.X;
                spriteBatch.DrawString(_font, $"Racecar: {raceCar.Name}", new Vector2(xPos, yPos+ 6 * _textLineHeight), Color.Black, (float)0.0, new Vector2(0, 0), _scaleFactor, SpriteEffects.None, (float)0.0);
                DrawCarStatistics(spriteBatch, raceCar, xPos, yPos + 6 * _textLineHeight) ;
            }
        }

        private void DrawRankingText(SpriteBatch spriteBatch, int xPos, int yPos, List<RaceCar> raceCarList)
        {
            List<RaceCar> SortedList = raceCarList.OrderByDescending(o => o.ScoreOverall).ToList();

            spriteBatch.Draw(_background, new Rectangle(xPos - 10, yPos -10, 200, SortedList.Count * 20 + 40), null, _corporateGray60, 0, new Vector2(0, 0), SpriteEffects.None, 0);
            spriteBatch.DrawString(_font, "Ranking List ", new Vector2(xPos, yPos), Color.Black);
            var ranking = 1;

            foreach (var car in SortedList)
            {
                spriteBatch.DrawString(_font, ranking + "    " +  car.Name + "          " + car.ScoreOverall, new Vector2(xPos, yPos + 20), Color.Black);
                ranking += 1;
                yPos += 20;
            }
        }

        private void DrawCarDebugText(SpriteBatch spriteBatch, RaceCar raceCar, int xPos, int yPos)
        {
            spriteBatch.DrawString(_font, "Position X: " + raceCar.Position.X.ToString(), new Vector2(xPos, yPos + 20), Color.Black);
            spriteBatch.DrawString(_font, "Position Y: " + raceCar.Position.Y.ToString(), new Vector2(xPos, yPos + 40), Color.Black);
            spriteBatch.DrawString(_font, "Velocity:   " + raceCar.Velocity.ToString(), new Vector2(xPos, yPos + 60), Color.Black);
            spriteBatch.DrawString(_font, "Score:      " + raceCar.ScoreOverall.ToString(), new Vector2(xPos, yPos + 80), Color.Black);
            spriteBatch.DrawString(_font, "Dist Left:  " + raceCar.Distance.Left.ToString(), new Vector2(xPos, yPos + 100), Color.Black);
            spriteBatch.DrawString(_font, "Dist FLeft: " + raceCar.Distance.FrontLeft.ToString(), new Vector2(xPos, yPos + 120), Color.Black);
            spriteBatch.DrawString(_font, "Dist Front: " + raceCar.Distance.Front.ToString(), new Vector2(xPos, yPos + 140), Color.Black);
            spriteBatch.DrawString(_font, "Dist FRight:" + raceCar.Distance.FrontRight.ToString(), new Vector2(xPos, yPos + 160), Color.Black);
            spriteBatch.DrawString(_font, "Dist Right: " + raceCar.Distance.Right.ToString(), new Vector2(xPos, yPos + 180), Color.Black);
            spriteBatch.DrawString(_font, "Checkpoint: " + raceCar.Checkpoint.ToString(), new Vector2(xPos, yPos + 200), Color.Black);
            spriteBatch.DrawString(_font, "IsCrashed:  " + raceCar.IsCrashed.ToString(), new Vector2(xPos, yPos + 220), Color.Black);
            spriteBatch.DrawString(_font, "Num Rounds:  " + raceCar.NumRounds, new Vector2(xPos, yPos + 240), Color.Black);
            spriteBatch.DrawString(_font, "Num Crashes:  " + raceCar.NumCrashes, new Vector2(xPos, yPos + 260), Color.Black);
        }

        private void DrawCarStatistics(SpriteBatch spriteBatch, RaceCar raceCar, int xPos, int yPos)
        {
            spriteBatch.DrawString(_font, "Score:      " + raceCar.ScoreOverall.ToString(), new Vector2(xPos, yPos + _textLineHeight), Color.Black, (float)0.0, new Vector2(0, 0), _scaleFactor, SpriteEffects.None, (float)0.0);
            spriteBatch.DrawString(_font, "Checkpoint (Position on Track): " + raceCar.Checkpoint.ToString(), new Vector2(xPos, yPos + 2 * _textLineHeight), Color.Black, (float)0.0, new Vector2(0, 0), _scaleFactor, SpriteEffects.None, (float)0.0);
            spriteBatch.DrawString(_font, "Num Rounds:  " + raceCar.NumRounds, new Vector2(xPos, yPos + 3 * _textLineHeight), Color.Black, (float)0.0, new Vector2(0, 0), _scaleFactor, SpriteEffects.None, (float)0.0);
            spriteBatch.DrawString(_font, "Num Crashes:  " + raceCar.NumCrashes, new Vector2(xPos, yPos + 4 * _textLineHeight), Color.Black, (float)0.0, new Vector2(0, 0), _scaleFactor, SpriteEffects.None, (float)0.0);
        }


        private void DrawRecordingSquare(SpriteBatch spriteBatch)
        {
            Texture2D circle = new Texture2D(spriteBatch.GraphicsDevice, 1, 1, false, SurfaceFormat.Color);
            circle.SetData(new[] { Color.Red });
            spriteBatch.Draw(circle, new Rectangle(1200 + _startPos.X, 25 + _startPos.Y, 25, 25), null, Color.Red, 0, new Vector2(0, 0), SpriteEffects.None, 0);
        }

        private void DrawVibrationText(SpriteBatch spriteBatch)
        {
            var font = Content.Load<SpriteFont>(@"font");
            var onOff = _gamePadVibration ? "On" : "Off";
            spriteBatch.DrawString(font, "Controller vibration " + onOff, new Vector2(0, 220), Color.Black);
        }

        private string GetMessage(RaceCar raceCar, StepData.Types.StepCommand stepCommand)
        {
            string message = "No cars available";
            if (raceCar != null)
            {
                message =
                    $@"{raceCar.Id};{raceCar.IsCrashed};{raceCar.MaxVelocity};{raceCar.Position.X};{raceCar.Position.Y};{raceCar.PreviousCheckpoint};{raceCar.Rotation};{raceCar.ScoreStep};{raceCar.ScoreOverall};{raceCar.Ticks};{raceCar.Velocity};{raceCar.Distance.Front};{raceCar.Distance.FrontLeft};{raceCar.Distance.FrontRight};{raceCar.Distance.Left};{raceCar.Distance.Right};{(int)stepCommand}";
            }
            return message;
        }

        private void UpdatePlayers()
        {
            if (_raceCarList.Count != _numPlayers || _adminOldStatus != Game.IsAdmin)
            {
                _adminOldStatus = Game.IsAdmin;
                _numPlayers = _raceCarList.Count;
                _components.Clear();
                var pos_y = Math.Max(_startPos.Y, _posInfo.Y + _textLineHeight * 13);
                //var pos_y = _posInfo.Y + _textLineHeight * 13;
                var pos_x = _posInfo.X;
                var pos_y_2 = Math.Max(_startPos.Y, _posInfo.Y + _textLineHeight * 13);

                //var pos_y_2 = _posInfo.Y + 260;
                var pos_x_2 = _posInfo.X + _buttonWidth + _buttonWidth/10;

                foreach (var car in _raceCarList)
                {
                    if (_raceCar != null)
                    {
                        if (car.Id == _raceCar.Id && !_isSpectator)
                        {
                            continue;
                        }
                        else
                        {
                            if (pos_y + 350 < GraphicsDevice.Adapter.CurrentDisplayMode.Height - _street.Height)
                            {
                                DrawSingleRaceCarButton(pos_x, pos_y, car);
                                if (Game.IsAdmin == true)
                                {
                                    DrawSingleRemoveCarButton(car, pos_x, pos_y, _buttonTextureX);
                                }
                                pos_y += _buttonHeight + _buttonHeight / 5;
                            }
                            else
                            {
                                DrawSingleRaceCarButton(pos_x_2, pos_y_2, car);
                                if (Game.IsAdmin == true)
                                {
                                    DrawSingleRemoveCarButton(car, pos_x, pos_y, _buttonTextureX);
                                }
                                pos_y_2 += _buttonHeight + _buttonHeight / 5;
                            }  
                        }
                    }
                    else
                    {
                        if (pos_y + 350 < GraphicsDevice.Adapter.CurrentDisplayMode.Height - _street.Height)
                        {
                            DrawSingleRaceCarButton(pos_x, pos_y, car);
                            if (Game.IsAdmin == true )
                            {
                                DrawSingleRemoveCarButton(car, pos_x, pos_y, _buttonTextureX);
                            }
                            pos_y += _buttonHeight + _buttonHeight / 5;
                        }
                        else
                        {
                            DrawSingleRaceCarButton(pos_x_2, pos_y_2, car);
                            if (Game.IsAdmin == true)
                            {
                                DrawSingleRemoveCarButton(car, pos_x, pos_y, _buttonTextureX);
                            }
                            pos_y_2 += _buttonHeight + _buttonHeight / 5;
                        }
                    }
                }

                if (_raceCarList.Count > 1 || _isSpectator == true)
                {
                    var raceCarClearButton = new Button(_buttonTexture, _buttonFont, _scaleFactor)
                    {
                        Position = new Vector2(pos_x, pos_y),
                        Text = "CLEAR",
                        ButtonColor = Color.White,
                        Width = _buttonWidth,
                        Height = _buttonHeight,
                    };
                    raceCarClearButton.Click += RaceCarClearButton_Click;
                    _components.Add(raceCarClearButton);
                }
            }
        }

        private void DrawSingleRaceCarButton(int pos_x, int pos_y, RaceCar car)
        {
            var raceCarButton = new RaceCarButton(_buttonTextureRed, _buttonFont, _scaleFactor)
            {
                Position = new Vector2(pos_x, pos_y),
                Text = car.Name,
                RaceCarId = car.Id.ToString(),
                Height = _buttonHeight,
                Width = _buttonWidth,
            };

            raceCarButton.Click += RaceCarButton_Click;
            _components.Add(raceCarButton);
            _raceCarButtons.Add(raceCarButton);
        }

        private void RaceCarButton_Click(object sender, EventArgs e)
        {
            foreach (var raceCarButton in _raceCarButtons)
            {
                if (sender == raceCarButton)
                {
                    raceCarButton.Clicked = true;
                    var raceCarId = raceCarButton.RaceCarId;
           
                    foreach (var car in _raceCarList)
                    {
                        if (car.Id.ToString() == raceCarId)
                        {
                            _raceCarInfo = raceCarId;
                            break;
                        }
                    } 
                }
                else
                {
                    raceCarButton.Clicked = false;
                }
            }
        }

        private void RaceCarClearButton_Click(object sender, EventArgs e)
        {
            foreach (var raceCarButton in _raceCarButtons)
            {
                raceCarButton.Clicked = false;
            }
                _raceCarInfo = "";
        }
        private void logRacecar(RaceCar raceCar)
        {
            string filePath = null;
            if (_raceCarList.Contains(raceCar))
            {
                filePath = Path.Join(_logFilePath, $"{Game.Session.Id}", $"{raceCar.Id}.txt");

                // Closes Datawriter, if Generated Filepath changed
                // This happens, if another Car is chosen
                if (filePath != _oldFilePath && !(_dataWriter is null))
                {
                    _dataWriter.Close();
                    _dataWriter = null;

                }

                // Opens new Datawriter
                if (_dataWriter is null)
                {
                    // Creates new Folder, but only if it doesn't already exist
                    Directory.CreateDirectory(Path.Join(_logFilePath, $"{Game.Session.Id}"));
                    
                    // Initializes new file, if it doesn't already exist
                    // Adds Headers for data as well
                    if (!File.Exists(filePath))
                    {
                        _dataWriter = File.AppendText(Path.Join(_logFilePath, $"{Game.Session.Id}", $"{raceCar.Id}.txt"));
                        _dataWriter.WriteLine("Time; Id;IsCrashed;MaxVelocity;Position.X;Position.Y;PreviousCheckpoint;Rotation;Score;ScoreOverall;Ticks;Velocity;Distance.Front;Distance.FrontLeft;Distance.FrontRight;Distance.Left;Distance.Right");
                        _dataWriter.Flush();
                    }
                    // opens Streamwriter for existing files
                    else
                    {
                        _dataWriter = File.AppendText(Path.Join(_logFilePath, $"{Game.Session.Id}", $"{raceCar.Id}.txt"));
                        _dataWriter.Flush();
                    }
                }
                // Writes Data into file
                else if (_dataWriter != null)
                {
                    _dataWriter.WriteLine($@"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture)};{raceCar.Id};{raceCar.IsCrashed};{raceCar.MaxVelocity};{raceCar.Position.X};{raceCar.Position.Y};{raceCar.PreviousCheckpoint};{raceCar.Rotation};{raceCar.ScoreStep};{raceCar.ScoreOverall};{raceCar.Ticks};{raceCar.Velocity};{raceCar.Distance.Front};{raceCar.Distance.FrontLeft};{raceCar.Distance.FrontRight};{raceCar.Distance.Left};{raceCar.Distance.Right}");
                    _dataWriter.Flush();
                }
            }


           _oldFilePath = filePath;
        }
        private void DrawSingleRemoveCarButton(RaceCar car, int pos_x, int pos_y, Texture2D buttonTexture)
        {
            var raceCarRemoveButton = new RaceCarButton(buttonTexture, _buttonFont, _scaleFactor)
            {
                Position = new Vector2(pos_x, pos_y),
                RaceCarId = car.Id.ToString(),
                Height = _buttonHeight,
                Width = _buttonHeight,
            };
           
            raceCarRemoveButton.Click += RaceCarRemoveButton_Click;
            _components.Add(raceCarRemoveButton);
            _raceCarRemoveButtons.Add(raceCarRemoveButton);
        }

        private void RaceCarRemoveButton_Click(object sender, EventArgs e)
        {
            foreach (var raceCarRemoveButton in _raceCarRemoveButtons)
            {
                if (sender == raceCarRemoveButton)
                {
                    var raceCarId = raceCarRemoveButton.RaceCarId;
                    RaceCar raceCarToRemove = _raceCarList.Find(x => x.Id.ToString() ==raceCarId);
                    _raceCarList.Remove(raceCarToRemove);
                    _gameService.DestroyRaceCarAsync(raceCarToRemove);
                }
            }
        }

        private void DrawSingleModelCarButton(ScriptMappingOptions script,  int pos_x, int pos_y, string path)
        {
            var modelCarButton = new ModelButton(script, _buttonTexture, _buttonFont, _scaleFactor, path)
            {
                Position = new Vector2(pos_x, pos_y),
                Height = _buttonHeight,
                Width = _modelButtonWidth,
                ButtonColor = new Color(55, 114, 182),
                Clicked = true
            };

            modelCarButton.Click += ModelCarButton_Click;
            _components.Add(modelCarButton);
            _modelButtons.Add(modelCarButton);
        }

        private void ModelCarButton_Click(object sender, EventArgs e)
        {
            foreach (var modelbutton in _modelButtons)
            {
                if (sender == modelbutton)
                {
                    modelbutton.Clicked = !modelbutton.Clicked;
                }
            }
        }

        private void DrawModelCarButtons()
        {
            string mlNetPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), _mlNetOptions.BinPath));


            // Start Positions to create ModelButtons
            int startValueX = _startPos.X + (int)((_font.MeasureString("D: Show statistics   ").X) * _scaleFactor);
            int startValueY = (int)(_startPos.Y - 2 * _buttonHeight);

            // Declaring and initializing local variables for placing Model Buttons
            int xValue = startValueX;
            int yValue = startValueY;

            // Calculation of number of Scripts in current config
            int numPythonScripts = _pythonOptions.ScriptMappings.Count();
            int numMLNetScripts = _mlNetOptions.ScriptMappings.Count();
            int numGenericScripts = _genericOptions.ScriptMappings.Count();
            int numScriptsTotal = numPythonScripts + numMLNetScripts + numGenericScripts;

            // Capping the number of Buttons at 6
            int maxNumScripts = Math.Min(6, numScriptsTotal);

            //

            ScriptMappingOptions interimScript;
            int indexRebased;

            // For loop to create model buttons
            // The idea is to draw all the buttons for a category, such as Python, before
            // going to the next Categorie
            // It is important, that there are no placeholder dummies in the config files,
            // as they would also generate a model button.
            for (int i = 0; i < maxNumScripts; i++)
            {
                // First Branch of if statement is to create Python model buttons, which are currently drawn first
                // Method is only executed, if there is enough Data for Python 
                if (_pythonOptions.ScriptMappings != null && _pythonOptions.BinPath != null && i < numPythonScripts)
                {
                    interimScript = _pythonOptions.ScriptMappings.ElementAt(i);
                    DrawSingleModelCarButton( interimScript, xValue, yValue, _pythonOptions.BinPath);

                }
                // Second  Branch of if statement is to create ML.Net model buttons
                // Method is only executed, if there is enough Data for ML.Net 
                else if (i >= numPythonScripts && i < numMLNetScripts + numPythonScripts && _mlNetOptions.ScriptMappings != null && _mlNetOptions.BinPath != null)
                {
                    indexRebased = (i - numPythonScripts);
                    interimScript = _mlNetOptions.ScriptMappings.ElementAt(indexRebased);
                    DrawSingleModelCarButton(interimScript, xValue, yValue, mlNetPath);
                }
                // Third  Branch for Generic Models
                else if (_genericOptions.ScriptMappings != null && i >= numPythonScripts + numMLNetScripts)
                {
                    indexRebased = i - numPythonScripts - numMLNetScripts;
                    interimScript = _genericOptions.ScriptMappings.ElementAt(indexRebased);
                    string interimFile = _genericOptions.ScriptMappings.ElementAt(indexRebased).File;
                    DrawSingleModelCarButton(interimScript, xValue, yValue, interimFile);
                }
                xValue += _modelButtonWidth;
                
                // Switch to change to second Row of Buttons as soon as 3 Buttons are drawn
                if(i == 2)
                {
                    xValue = startValueX;
                    yValue = startValueY + _buttonHeight;
                }

            }

            // Finally adding a single Button to start the models
            _modelStartButton = new Button(_buttonTexture, _font, _scaleFactor)
            {
                Position = new Vector2(startValueX + 3 * _modelButtonWidth, startValueY + _buttonHeight),
                Height = _buttonHeight,
                Width = _modelButtonWidth,
                Text = "Start Model",
            };
            _modelStartButton.Click += StartModelCarButton_Click;
            _components.Add(_modelStartButton);



        }
        private void StartModelCarButton_Click(object sender, EventArgs e)
        {
            // Button only works, when Race isn't finished

            if (_sessionService.RaceIsFinished(new GuidData { GuidString = Game.Session.Id.ToString() }) == false)
            {
                string mlNetPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), _mlNetOptions.BinPath));
                foreach (var modelbutton in _modelButtons)
                {
                    // Executes script for the clicked Buttons
                    if (modelbutton.Clicked == true)
                    {
                        // For the following see also code in checkExecuteScript()
                        if (modelbutton.path == _pythonOptions.BinPath)
                        {
                            Logger.LogInformation($"Executing Python script {modelbutton.script.File}");
                            var args = $"{modelbutton.script.File} --session={Game.Session.Id} --carName={modelbutton.script.CarName} --carColor={modelbutton.script.CarColor} --model={modelbutton.script.Model}";
                            ExecuteScript(modelbutton.path, args);
                        }
                        else if (modelbutton.path == mlNetPath)
                        {
                            Logger.LogInformation($"Executing ML.NET {modelbutton.script.File}");
                            var args = $"--model={modelbutton.script.File} --session={Game.Session.Id} --carName={modelbutton.script.CarName} --carColor={modelbutton.script.CarColor} --version={modelbutton.script.Version}";
                            ExecuteScript(modelbutton.path, args);
                        }
                        else
                        {
                            var args = modelbutton.script.Args.Replace("[SESSIONID]", Game.Session.Id.ToString());
                            Logger.LogInformation($"Executing generic script {modelbutton.script.File} with args \"{args}\"");
                            ExecuteScript(modelbutton.script.File, args, modelbutton.script.ShellExecute);
                        }
                        modelbutton.Clicked = false;
                    }
                }
            }
            
        }

        
        private List<RaceCar> FinishPositionListRaceCarWithCars()
        {
            // this Method is intended to create a copy of the 
            // RaceCars in the current game. The copies
            // have fixed values and are used to create
            // the impression that the screen is frozen.


            List<RaceCar> data = new();

            foreach (RaceCar raceCar in _raceCarList)
            {
                // Transfering CarDistance data of RaceCar to new Object to copy values
                CarDistance InterimResult = new CarDistance();
                InterimResult.FrontLeft = raceCar.Distance.FrontLeft;
                InterimResult.FrontRight = raceCar.Distance.FrontRight;
                InterimResult.Right = raceCar.Distance.Right;
                InterimResult.Left = raceCar.Distance.Left;
                InterimResult.MaxViewDistance = raceCar.Distance.MaxViewDistance;
                InterimResult.Front = raceCar.Distance.Front;

                // Creating new RaceCar object with copied values
                data.Add(new RaceCar(Game.Session.Id,raceCar.Name, raceCar.Color)
                {
                    Name = new String(raceCar.Name),
                    Color = new String(raceCar.Color),
                    Position = raceCar.Position,
                    Distance = InterimResult,
                    Rotation = raceCar.Rotation,

                }
                );
            }

            return data;

        }

        private void DrawFinishPositionRaceCarWithCars(List<RaceCar> data, SpriteBatch spriteBatch)
        {
            var myRaceCarId = 0;
            foreach (RaceCar raceCar in data)
            {
                // Making sure that a correct colour is chosen for the RaceCar
                var color = Color.Black;
                if (raceCar.Color.Length == 0)
                {
                    color = GetRaceCarColor(myRaceCarId);
                }
                else
                {
                    color = GetRaceCarColorFromString(raceCar);
                }

                // Drawing the individual Cars & Distance lights
                DrawSingleCar(raceCar, color, spriteBatch);
                DrawDinstanceLines(raceCar, color, spriteBatch);

                // Counter to add additional colours, if needed.                
                myRaceCarId += 1;

            }

        }
        private void DrawRaceFinishedButton()
        {
            _raceFinishedButton = new Button(_buttonTextureRed, _font, _scaleFactor)
            {
                Position = new Vector2(LayoutUtility.screenWidth/2 - _buttonWidth/2, LayoutUtility.screenHeight/2 - _buttonHeight),
                Height = _buttonHeight*2,
                Width = _buttonWidth,
                Text = "Race finished! Press Button/Esc to exit Game!",
            };
            _raceFinishedButton.Click += RaceFinishedButton_Click;
            _components.Add(_raceFinishedButton);
        }
        private void RaceFinishedButton_Click(object sender, EventArgs e)
        {
            Logger.LogInformation("RankingState");
            var rankingState = _stateFactory.CreateState<IRankingState<RankingStateOptions>, RankingStateOptions>(GraphicsDevice, Content);
            Game.ChangeState(rankingState);
        }

        private void DrawFinishRaceButton()
        {
            _finishRaceButton = new Button(_buttonTexture, _font, _scaleFactor)
            {
                Position = new Vector2(60 * _font.MeasureString("A").X + LayoutUtility.widthPx(0.25), 10),
                Height = _buttonHeight,
                Width = _modelButtonWidth,
                Text = "Finish race",
            };
            _finishRaceButton.Click += FinishRaceButtonButton_Click;
            _components.Add(_finishRaceButton);
        }

        private void FinishRaceButtonButton_Click(object sender, EventArgs e)
        {
            // Setting .isFinished for session to true
            _sessionService.FinishRace(new GuidData { GuidString = Game.Session.Id.ToString() });
            //closing open streamwriter           
            if (_dataWriter is not null)
            {
                _dataWriter.Close();
            }
            // Waiting 100 MS for clients to request new state of .isFinished for Session
            Thread.Sleep(100);
            
            // Removing Player RaceCar
            _raceCarList.Remove(_raceCar);
            _gameService.DestroyRaceCarAsync(_raceCar);

            // Removing other RaceCars
            for (int i = _raceCarList.Count - 1; i >= 0; i--)
            {
                RaceCar raceCar = _raceCarList.ElementAt(i);
                _raceCarList.RemoveAt(i);
                _gameService.DestroyRaceCarAsync(raceCar);
            }
            _finishRaceButton.Clicked = false;
        }
    }
}