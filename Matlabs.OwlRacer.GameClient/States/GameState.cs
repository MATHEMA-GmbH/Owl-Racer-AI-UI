using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Spreadsheet;
using Grpc.Core;
using Grpc.Net.Client;
using Matlabs.OwlRacer.Common.Model;
using Matlabs.OwlRacer.Common.Options;
using Matlabs.OwlRacer.GameClient.Controls;
using Matlabs.OwlRacer.GameClient.Services;
using Matlabs.OwlRacer.GameClient.Services.Interface;
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
        private Texture2D _background;
        private Texture2D _logo;
        private Texture2D _logoMathema;
        private Texture2D _circle;
        private Texture2D _street;
        private Dictionary<int, Texture2D> _startPhaseTextures = new();

        private List<Component> _components;
        private List<RaceCarButton> _raceCarButtons = new();
        private int _numPlayers = 0;

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

            _isSpectator = Game.IsSpectator;

            _components = new List<Component>(){};

            _numPlayers = 0;

            var path = Path.Join(Directory.GetCurrentDirectory(), "capture");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            if (!_isSpectator)
            {
                Logger.LogInformation("Creating new RaceCar.");
                _raceCar = _gameService.CreateRaceCarAsync(Game.Session, 0.5f, 0.05f, Options.RaceCarName, string.Empty).Result;
                _raceCarList.Add(_raceCar);
                Logger.LogInformation($"---> RaceCar successfully created (ID={_raceCar.Id}, name={_raceCar.Name}, color={_raceCar.Color}");
                
                _dataWriter = File.AppendText(Path.Join(path, $"{_raceCar.Id}.txt"));
                _dataWriter.WriteLine("Id;IsCrashed;MaxVelocity;Position.X;Position.Y;PreviousCheckpoint;Rotation;Score;ScoreOverall;Ticks;Velocity;Distance.Front;Distance.FrontLeft;Distance.FrontRight;Distance.Left;Distance.Right;stepCommand");
            }
        }

        public override void LoadContent(GameTime gameTime)
        {
            _buttonTextureRed = Content.Load<Texture2D>("Images/ButtonRedMini");
            _buttonTexture = Content.Load<Texture2D>("Images/Button");
            _buttonFont = Content.Load<SpriteFont>("Inter-Regular");
            _font = Content.Load<SpriteFont>("Inter-SemiBold");
            _fontSmall = Content.Load<SpriteFont>("Inter-Regular-small");
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

            _logo = Content.Load<Texture2D>(@"Images/owlracer-logo-solo");
            _logoMathema = Content.Load<Texture2D>(@"Images/mathema-logo");
            _circle = Content.Load<Texture2D>(@"Images/Circle");
            _street = Content.Load<Texture2D>(@"Images/Street");
            _startPos = new VectorOptions((int)(GraphicsDevice.Adapter.CurrentDisplayMode.Width*0.01), (int)((GraphicsDevice.Adapter.CurrentDisplayMode.Height - _trackHeight - _logo.Height/3)*0.5));
            _posInfo = new VectorOptions((int)((GraphicsDevice.Adapter.CurrentDisplayMode.Width-_trackWidth)*2.05), 20 );
            _startLinePos = Game.Session.RaceTrack.StartLine;
        }

        public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            spriteBatch.Begin();

            spriteBatch.Draw(_background, new Rectangle(_startPos.X, _startPos.Y, _trackWidth, _trackHeight), null, _corporateGray60, 0, new Vector2(0, 0), SpriteEffects.None, 0);

            DrawStartLine(spriteBatch);
            
            spriteBatch.Draw(
                _raceTrackTexture,
                new Rectangle(_startPos.X , _startPos.Y, _trackWidth, _trackHeight),
                Color.White
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

            foreach (var component in _components)
            {
                component.Draw(gameTime, spriteBatch);
            }

            //Rectangle logoRect = new Rectangle(GraphicsDevice.Adapter.CurrentDisplayMode.Width - (_logo.Width/3), GraphicsDevice.Adapter.CurrentDisplayMode.Height - (_logo.Height/3),
            //    _logo.Width / 3, _logo.Height / 3);
            Rectangle logoRect = new Rectangle((int) (GraphicsDevice.Adapter.CurrentDisplayMode.Width - _logo.Width*0.56), (int)(GraphicsDevice.Adapter.CurrentDisplayMode.Height -  _circle.Height/3), (int)( _logo.Width*0.53), (int)(_logo.Height*0.53));
            Rectangle logoRectCircle = new Rectangle(GraphicsDevice.Adapter.CurrentDisplayMode.Width - _circle.Width/3*2, (GraphicsDevice.Adapter.CurrentDisplayMode.Height - _circle.Height/2),
                _circle.Width/3*2, _circle.Height/2);

            Rectangle logoRectMathema = new Rectangle((int)(GraphicsDevice.Adapter.CurrentDisplayMode.Width - (_logoMathema.Width * 0.2)), (int)(GraphicsDevice.Adapter.CurrentDisplayMode.Height - _circle.Height / 3 + logoRect.Height * 1.9),
               (int)(_logoMathema.Width * 0.15), (int)(_logoMathema.Height * 0.15));

            Rectangle logoRectStreet1 = new Rectangle(0, GraphicsDevice.Adapter.CurrentDisplayMode.Height - 8 * GraphicsDevice.Adapter.CurrentDisplayMode.Height / _street.Height,
                GraphicsDevice.Adapter.CurrentDisplayMode.Width, _street.Height * GraphicsDevice.Adapter.CurrentDisplayMode.Height / 8);

            spriteBatch.Draw(_street, logoRectStreet1, Color.White);
            spriteBatch.Draw(_circle, logoRectCircle, Color.White);
            spriteBatch.Draw(_logo, logoRect, Color.White);

            spriteBatch.DrawString(_fontSmall, "EIN PROJEKT DER", new Vector2((int)(GraphicsDevice.Adapter.CurrentDisplayMode.Width - (logoRectCircle.Width / 2)), (int)(GraphicsDevice.Adapter.CurrentDisplayMode.Height - _circle.Height / 3 + logoRect.Height*1.3)), _corporateGray60);
            spriteBatch.Draw(_logoMathema, logoRectMathema, Color.White);

            spriteBatch.DrawString(_font, $"Session: {Game.Session.Name}", new Vector2(_trackWidth / 2 - 40, 20), Color.Black);

            //DrawRankingText(spriteBatch, 10, 300, _raceCarList);

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
            spriteBatch.DrawString(_font, $"{(Game.Session.GameTime.Ticks < 0 ? "-": "")} {Math.Abs(Game.Session.GameTime.Minutes)}:{Math.Abs(Game.Session.GameTime.Seconds)}.{Math.Abs(Game.Session.GameTime.Milliseconds)}", new Vector2(10, 10), Color.Black);

            var phase = Game.Session.HasRaceStarted ? 0 : Math.Clamp(Math.Abs(Game.Session.GameTime.Seconds - 1), 0, 3);

            spriteBatch.Draw(
                _startPhaseTextures[phase],
                new Rectangle(150, 10, 120, 46),
                Color.White);
        }

        public override void Update(GameTime gameTime)
        {
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

                // mit logger in csv schreiben...
                if (_capture)
                {
                    _dataWriter.WriteLine(GetMessage(_raceCar, stepCommand));
                    _dataWriter.Flush();
                }
            }
            List<RaceCar> removeCarList = new List<RaceCar>();

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

            UpdatePlayers();

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
            
            if (OwlKeyboard.HasBeenPressed(Keys.L))
            {
                _capture = !_capture;
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
            // Python
            if (_pythonOptions.ScriptMappings != null && _pythonOptions.BinPath != null)
            {

                foreach (var mapping in _pythonOptions.ScriptMappings)
                {
                    if (Enum.TryParse(typeof(Keys), mapping.Key, true, out var value))
                    {
                        var key = (Keys) value;

                        if (OwlKeyboard.HasBeenPressed(key))
                        {
                            Logger.LogInformation($"Executing python script {mapping.File}");
                            var args = $"{mapping.File} --session={Game.Session.Id}";
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
                            var args = $"--model={mapping.File} --session={Game.Session.Id} --carName={mapping.CarName} --carColor={mapping.CarColar}";
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
            if (OwlKeyboard.HasBeenPressed(Keys.D) && raceCar.Id.ToString() == _raceCar.Id.ToString())
            {
                _debugState = !_debugState;
            }

            int yPos = _posInfo.Y;
            int xPos = _posInfo.X;

            if (_raceCar != null)
            {
                
                if (!_isSpectator && _debugState && raceCar == _raceCar)
                { 
                    xPos = _posInfo.X;
                    //spriteBatch.DrawString(_font, $"You are driving Racecar: {raceCar.Name} (ID={_raceCar.Id})", new Vector2(xPos, yPos), Color.Black);
                    spriteBatch.DrawString(_font, $"You are driving Racecar: {raceCar.Name}", new Vector2(xPos, yPos), Color.Black);
                    DrawCarStatistics(spriteBatch, raceCar, xPos, yPos);
                }   
                else if (raceCar != _raceCar)
                {
                    xPos = _posInfo.X;
                    //spriteBatch.DrawString(_font, $"Racecar: {raceCar.Name} (ID={_raceCar.Id})", new Vector2(xPos, yPos), Color.Black);
                    spriteBatch.DrawString(_font, $"Racecar: {raceCar.Name}", new Vector2(xPos, yPos+140), Color.Black);
                    DrawCarStatistics(spriteBatch, raceCar, xPos, yPos+140);
                }
            }
            else
            {
                xPos = _posInfo.X;
                spriteBatch.DrawString(_font, $"Racecar: {raceCar.Name}", new Vector2(xPos, yPos+140), Color.Black);
                DrawCarStatistics(spriteBatch, raceCar, xPos, yPos+140);
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
            spriteBatch.DrawString(_font, "Score:      " + raceCar.ScoreOverall.ToString(), new Vector2(xPos, yPos + 20), Color.Black);
            spriteBatch.DrawString(_font, "Checkpoint (Position on Track): " + raceCar.Checkpoint.ToString(), new Vector2(xPos, yPos + 40), Color.Black);
            spriteBatch.DrawString(_font, "Num Rounds:  " + raceCar.NumRounds, new Vector2(xPos, yPos + 60), Color.Black);
            spriteBatch.DrawString(_font, "Num Crashes:  " + raceCar.NumCrashes, new Vector2(xPos, yPos + 80), Color.Black);
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

            if (_raceCarList.Count == 0)
            {
                _components.Clear();
            }

            if (_raceCarList.Count != _numPlayers)
            {
                _numPlayers = _raceCarList.Count;
                _components.Clear();
                var pos_y = _posInfo.Y + 260;
                var pos_x = _posInfo.X;
                var pos_y_2 = _posInfo.Y + 260;
                var pos_x_2 = _posInfo.X + _buttonTextureRed.Width + 5;

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
                                pos_y += 50;
                            }
                            else
                            {
                                DrawSingleRaceCarButton(pos_x_2, pos_y_2, car);
                                pos_y_2 += 50;
                            }  
                        }
                    }
                    else
                    {
                        if (pos_y + 350 < GraphicsDevice.Adapter.CurrentDisplayMode.Height - _street.Height)
                        {
                            DrawSingleRaceCarButton(pos_x, pos_y, car);
                            pos_y += 50;
                        }
                        else
                        {
                            DrawSingleRaceCarButton(pos_x_2, pos_y_2, car);
                            pos_y_2 += 50;
                        }
                    }
                }

                if (_raceCarList.Count > 1 || _isSpectator == true)
                {
                    var raceCarClearButton = new Button(_buttonTexture, _buttonFont)
                    {
                        Position = new Vector2(pos_x + 45, pos_y + 10),
                        Text = "CLEAR",
                        ButtonColor = Color.White
                    };
                    raceCarClearButton.Click += RaceCarClearButton_Click;
                    _components.Add(raceCarClearButton);
                }
            }
        }

        private void DrawSingleRaceCarButton(int pos_x, int pos_y, RaceCar car)
        {
            var raceCarButton = new RaceCarButton(_buttonTextureRed, _buttonFont)
            {
                Position = new Vector2(pos_x, pos_y),
                Text = car.Name,
                RaceCarId = car.Id.ToString()
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
    }
}