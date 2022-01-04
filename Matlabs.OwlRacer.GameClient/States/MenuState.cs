using System;
using System.Collections.Generic;
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
using System.Linq;
using System.IO;

namespace Matlabs.OwlRacer.GameClient.States
{
    public class MenuState : StateBase<MenuStateOptions>, IMenuState<MenuStateOptions>
    {
        // Constants
        private const float GameTimeSetting = 50f;

        // Services and States
        private readonly IGameService _gameService;
        private readonly ISessionService _clientService;
        private readonly IResourceService _resourceService;
        private readonly IStateFactory _stateFactory;

        // Components and Buttons
        private List<Component> _components;
        private List<SessionButton> _sessionButtons = new();
        private Button _newGameButton;
        private Button _quitGameButton;
        private Button _playerButton;
        private Button _sessionNameButton;
        private Button _playerModeButton;
        private Button _spectatorModeButton;
        private Button _track0Button;
        private Button _track1Button;
        private Button _track2Button;
        private List<Button> _buttonList;

        private GuidListData _availableSessions;
        private GuidListData _oldSessions;
        private string _selectedSession;
        private bool _drawRanking;
        private int _trackNum;
        private List<RaceCar> _carList = new();

        // Fonts and Textures
        private SpriteFont _font;
        private SpriteFont _fontSmall;
        private SpriteFont _buttonFont;

        private Texture2D _logo;
        private Texture2D _logoMathema;
        private Texture2D _circle;
        private Texture2D _street;
        private Texture2D _line;
        private Texture2D _buttonTexture;
        private Texture2D _buttonTextureRed;
        private Texture2D _buttonTextureX;
        private Texture2D _qrMenuTexture;
        private Texture2D _qrBusinessTexture;
        private Texture2D _qrBlogTexture;
        private Texture2D _background;
        private Texture2D _raceCarTexture;
        private Texture2D _track0Texture;
        private Texture2D _track1Texture;
        private Texture2D _track2Texture;

        //Corporate Colors

        private Color _corporateGray40 = new Color(187, 188, 188);
        private Color _corporateGray60 = new Color(136, 139, 141);
        private Color _corporateGreen = new Color(44, 154, 117);

        // Positions and Limits
        private int _borderLeftRight;
        private int _borderTop;
        private int pos_y;
        private int pos_y_2;
        private int pos_x;
        private int pos_x_2;
        private int _yLimit;

        private int _startPosCarX;
        private int _startPosCarY;
        private int _rotation;

        private double _oldGameTime;
        private double _newGameTime;

        private bool _releasedKey = true;
        private Keys[] _oldPressedKey;


        public MenuState(OwlRacerGame game,
            ILogger<MenuState> logger,
            ISessionService clientService,
            IGameService gameService,
            IResourceService resourceService,
            IStateFactory stateFactory)
            : base(game, logger)
        {
            _clientService = clientService;
            _stateFactory = stateFactory;
            _gameService = gameService;
            _resourceService = resourceService ?? throw new ArgumentNullException(nameof(resourceService));
        }

        public override void Initialize(GraphicsDevice graphicsDevice, ContentManager content, MenuStateOptions options)
        {
            base.Initialize(graphicsDevice, content, options);
            Game.IsSpectator = false;
            _drawRanking = false;
            _startPosCarX = 0;
            _startPosCarY = 0;
            _oldGameTime = 0;
            _newGameTime = 0;
            _rotation = 0;
            _borderLeftRight = (int)(GraphicsDevice.Adapter.CurrentDisplayMode.Width * 0.12);
            _borderTop = (int)(GraphicsDevice.Adapter.CurrentDisplayMode.Height * 0.1);

            _buttonTexture = Content.Load<Texture2D>("Images/Button");
            _buttonTextureRed = Content.Load<Texture2D>("Images/ButtonRed");
            _buttonTextureX = Content.Load<Texture2D>("Images/ButtonX");
            _buttonFont = Content.Load<SpriteFont>("Inter-Regular");
            _fontSmall = Content.Load<SpriteFont>("Inter-Regular-small");
            _track0Texture = Content.Load<Texture2D>("Images/level0");
            _track1Texture = Content.Load<Texture2D>("Images/level1");
            _track2Texture = Content.Load<Texture2D>("Images/level2");

            pos_y = (int)(_borderTop * 3);
            pos_y_2 = (int)(_borderTop * 3);
            pos_x = (int)(_borderLeftRight);
            pos_x_2 = (int)(_borderLeftRight * 1.1 + _buttonTextureRed.Width);

            _yLimit = (int)(GraphicsDevice.Adapter.CurrentDisplayMode.Height);

            _newGameButton = new Button(_buttonTexture, _buttonFont)
            {
                Position = new Vector2((int)(_borderLeftRight * 2.3 * 2.6), (int)(_borderTop * 1.4)),
                Text = "New Game",
                HoverColor = _corporateGreen,
            };

            _newGameButton.Click += NewGameButton_Click;

            _quitGameButton = new Button(_buttonTexture, _buttonFont)
            {
                Position = new Vector2((int)(_borderLeftRight * 2.3 * 2.6), (int)(_borderTop * 1.5 + _buttonTexture.Height)),
                Text = "Quit Game",
                ButtonColor = _corporateGray40,
            };

            _quitGameButton.Click += QuitGameButton_Click;

            _playerButton = new Button(_buttonTexture, _buttonFont)
            {
                Position = new Vector2(_borderLeftRight, (int)(_borderTop*1.4)),
                Text = "",
                ButtonColor = Color.White,
                Width = 230,
            };

            _playerButton.Click += PlayerNameButton_Click;

            _sessionNameButton = new Button(_buttonTexture, _buttonFont)
            {
                Position = new Vector2((int)(_borderLeftRight * 2.3 * 2), (int)(_borderTop * 1.4)),
                Text = "",
                Width = 230,
            };

            _sessionNameButton.Click += SessionNameButton_Click;

            _playerModeButton = new Button(_buttonTexture, _buttonFont)
            {
                Position = new Vector2((int)(_borderLeftRight * 2.3), (int)(_borderTop*1.4)),
                Text = "Player",
                ButtonColor = Color.White,
            };
            _playerModeButton.Clicked = true;

            _playerModeButton.Click += PlayerModeButton_Click;

            _spectatorModeButton = new Button(_buttonTexture, _buttonFont)
            {
                Position = new Vector2((int)(_borderLeftRight * 2.3 + _playerModeButton.Width), (int)(_borderTop*1.4)),
                Text = "Spectator",
                ButtonColor = Color.White,
            };

            _spectatorModeButton.Click += SpectatorModeButton_Click;

            _track0Button = new Button(_track0Texture, _buttonFont)
            {
                Position = new Vector2((int)(_borderLeftRight * 1.4 + _buttonTextureRed.Width + 500), (int)(_borderTop * 3.2)), //(int)(_borderLeftRight * 1.2 + _buttonTextureRed.Width), (int)(_borderTop * 3.2)
                Text = "Level 1",
                ButtonColor = Color.White,
                Width = _track0Texture.Width / 5,
                Height = _track0Texture.Height / 5,
            };

            _track0Button.Click += Track0Button_Click;

            _track1Button = new Button(_track1Texture, _buttonFont)
            {
                Position = new Vector2((int)(_borderLeftRight * 1.4 + _buttonTextureRed.Width + 500), (int)(_borderTop * 3.4 + _track0Button.Height)),
                Text = "Level 2",
                ButtonColor = Color.White,
                Width = _track1Texture.Width / 5,
                Height = _track1Texture.Height / 5,
            };

            _track1Button.Click += Track1Button_Click;

            _track2Button = new Button(_track2Texture, _buttonFont)
            {
                Position = new Vector2((int)(_borderLeftRight * 1.4 + _buttonTextureRed.Width + 500), (int)(_borderTop * 3.6 + _track0Button.Height + _track1Button.Height)),
                Text = "Level 3",
                ButtonColor = Color.White,
                Width = _track2Texture.Width / 5,
                Height = _track2Texture.Height / 5,
            };

            _track2Button.Click += Track2Button_Click;
            _track2Button.Clicked = true;
            _trackNum = 2;

            _components = new List<Component>()
            {
                _newGameButton,
                _quitGameButton,
                _playerButton,
                _sessionNameButton,
                _playerModeButton,
                _spectatorModeButton,
                _track0Button,
                _track1Button,
                _track2Button
            };

            _buttonList = new List<Button>()
            {
                _newGameButton,
                _quitGameButton,
                _playerButton,
                _sessionNameButton
            };
        }

        private void RevertComponentsToInit()
        {
            _oldSessions = _availableSessions;
            _components.Clear();
            _components.Add(_newGameButton);
            _components.Add(_quitGameButton);
            _components.Add(_playerButton);
            _components.Add(_sessionNameButton);
            _components.Add(_playerModeButton);
            _components.Add(_spectatorModeButton);
            _components.Add(_track0Button);
            _components.Add(_track1Button);
            _components.Add(_track2Button);

            pos_y = (int)(_borderTop * 3);
            pos_y_2 = (int)(_borderTop * 3);
            pos_x = (int)(_borderLeftRight);
            pos_x_2 = (int)(_borderLeftRight * 1.1 + _buttonTextureRed.Width);
        }

        private void UpdateSessions()
        {
            _availableSessions = _clientService.GetSessionIds();

            if (_oldSessions == null)
            {
                RevertComponentsToInit();
            }
            else if(_availableSessions.Guids.Equals(_oldSessions.Guids) == false)
            {
                RevertComponentsToInit();
            }

            foreach (var entry in _availableSessions.Guids)
            {
                var sessionGuid = new Guid(entry.GuidString);
                var mySession = _clientService.GetSession(sessionGuid);
                var buttonExist = false;

                foreach (var button in _components.OfType<SessionButton>())
                {
                    if (button.SessionId == mySession.Id.ToString())
                    {
                        buttonExist = true;
                        continue;
                    }
                }

                if (!buttonExist)
                {   
                    if (pos_y + _qrMenuTexture.Height/3 < _yLimit)
                    {
                        DrawSingleSessionButton(mySession, pos_x, pos_y, _buttonTextureRed);
                        DrawSingleRemoveSessionButton(mySession, (int)(pos_x - _buttonTextureX.Width * 1.1), pos_y, _buttonTextureX);
                        pos_y = (int)(pos_y + _borderTop * 0.4);
                    }
                    else
                    {
                        DrawSingleSessionButton(mySession, pos_x_2, pos_y_2, _buttonTextureRed);
                        DrawSingleRemoveSessionButton(mySession, (int)(pos_x_2 - _buttonTextureX.Width * 1.1), pos_y_2, _buttonTextureX);
                        pos_y_2 = (int)(pos_y_2 + _borderTop * 0.4);
                    }
                }
            }
        }


        public override void LoadContent(GameTime gameTime)
        {
            _font = Content.Load<SpriteFont>("Inter-SemiBold");
            _logo = Content.Load<Texture2D>(@"Images/owlracer-logo-solo");
            _logoMathema = Content.Load<Texture2D>(@"Images/mathema-logo");
            _circle = Content.Load<Texture2D>(@"Images/Circle_down");
            _street = Content.Load<Texture2D>(@"Images/Street");

            _background = new Texture2D(GraphicsDevice, 1, 1, false, SurfaceFormat.Color);
            _background.SetData(new[] { Color.White });

            _line = new Texture2D(GraphicsDevice, 1, 1, false, SurfaceFormat.Color);
            _line.SetData(new[] { Color.Black });

            var rawImageData = _resourceService.GetBaseImageDataAsync().Result;
            var _raceCarImageData = rawImageData.Car.ToByteArray();
            using var raceCarMem = new MemoryStream(_raceCarImageData);
            _raceCarTexture = Texture2D.FromStream(GraphicsDevice, raceCarMem);

            _startPosCarX = _raceCarTexture.Width;
            _startPosCarY = _borderTop/2;

            _qrMenuTexture = Content.Load<Texture2D>("Images/QR_Startseite");
            _qrBusinessTexture = Content.Load<Texture2D>("Images/QR_Karriere");
            _qrBlogTexture = Content.Load<Texture2D>("Images/QR_Blog");
        }

        public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            spriteBatch.Begin();

            _newGameTime = gameTime.TotalGameTime.TotalMilliseconds;

            if (_oldGameTime + 30 < _newGameTime)
            {
                _oldGameTime = _newGameTime;

                if (_startPosCarX + 5 < GraphicsDevice.Adapter.CurrentDisplayMode.Width - _circle.Width)
                {
                    _startPosCarX += 5;
                    //_startPosCarY = GraphicsDevice.Adapter.CurrentDisplayMode.Height - 4 * GraphicsDevice.Adapter.CurrentDisplayMode.Height / _street.Height - _raceCarTexture.Height;
                    _rotation = 0;
                }

                else
                {
                    _startPosCarX = _raceCarTexture.Width;
                    //_startPosCarY = GraphicsDevice.Adapter.CurrentDisplayMode.Height - 4 * GraphicsDevice.Adapter.CurrentDisplayMode.Height / _street.Height - _raceCarTexture.Height;
                    _rotation = 0;
                }
            }

            spriteBatch.DrawString(_font, "Available Sessions:", new Vector2((int)(_borderLeftRight), (int)(_borderTop * 2.5)), Color.Black);

            spriteBatch.DrawString(_font, "Enter Player Name: ", new Vector2(_borderLeftRight, _borderTop), Color.White);

            spriteBatch.DrawString(_font, "Choose Mode: ", new Vector2((int) (_borderLeftRight * 2.3), _borderTop), Color.White);

            spriteBatch.DrawString(_font, "Enter Session Name: ", new Vector2((int)(_borderLeftRight * 2.3 * 2), (int)(_borderTop)), Color.Black);

            spriteBatch.DrawString(_font, "Choose Track: ", new Vector2((int)(GraphicsDevice.Adapter.CurrentDisplayMode.Width - _track0Texture.Width / 5), _borderTop), Color.Black);

            Rectangle logoRect = new Rectangle((int)(GraphicsDevice.Adapter.CurrentDisplayMode.Width - (_logo.Width*0.8)), (int)(_circle.Height * 0.75 * 0.25- _logo.Height*0.75), 
               (int)( _logo.Width*0.75), (int)(_logo.Height*0.75));
            Rectangle logoRectMathema = new Rectangle((int)(GraphicsDevice.Adapter.CurrentDisplayMode.Width - (_logoMathema.Width * 0.21)), (int)(_circle.Height * 0.75 * 0.7 - _logo.Height),
               (int)(_logoMathema.Width*0.18), (int)(_logoMathema.Height*0.18));
            Rectangle logoRectCircle = new Rectangle((int)(GraphicsDevice.Adapter.CurrentDisplayMode.Width - _circle.Width*0.85), 0,
                (int)(_circle.Width*0.85), (int)(_circle.Height*0.75));

            Rectangle logoRectStreet1 = new Rectangle(0, 0,
                GraphicsDevice.Adapter.CurrentDisplayMode.Width, (int)(_borderTop*0.8));


            Rectangle lineRect = new Rectangle((int)(_borderLeftRight * 2.3 * 1.9), (int) (_borderTop*0.97), 2, 100);


            spriteBatch.Draw(_street, logoRectStreet1, Color.White);
            spriteBatch.Draw(_circle, logoRectCircle, Color.White);
            spriteBatch.Draw(_logo, logoRect, Color.White);
            spriteBatch.DrawString(_font, "EIN PROJEKT DER", new Vector2((int)(GraphicsDevice.Adapter.CurrentDisplayMode.Width - (logoRectCircle.Width/2)), (int)(_circle.Height * 0.75 * 0.5 - _logo.Height * 0.75)), _corporateGray60);
            spriteBatch.Draw(_logoMathema, logoRectMathema, Color.White);
            spriteBatch.Draw(_line, lineRect, Color.White);

            spriteBatch.Draw(
                    _raceCarTexture,
                    new Rectangle(_startPosCarX, _startPosCarY,
                    _raceCarTexture.Width, _raceCarTexture.Height),
                    new Rectangle(0, 0, _raceCarTexture.Width, _raceCarTexture.Height),
                    Color.White,
                    _rotation,
                    new Vector2((int)(_raceCarTexture.Width / 2), (int)(_raceCarTexture.Height / 2)),
                    SpriteEffects.None,
                    1.0f
                );

             _yLimit = (int)((GraphicsDevice.Adapter.CurrentDisplayMode.Height - 8 * GraphicsDevice.Adapter.CurrentDisplayMode.Height / _street.Height) - (_qrMenuTexture.Height / 3 * 1.5));
            var xPosQR = (int)(GraphicsDevice.Adapter.CurrentDisplayMode.Width - _qrMenuTexture.Width / 2 - _borderTop * 0.2);
            var yPosQR = (int)(_circle.Height * 0.75 + _borderTop * 0.1);

            Rectangle qrMenu = new Rectangle(xPosQR, yPosQR, (int) (_qrMenuTexture.Width/2), _qrMenuTexture.Height/2);
            Rectangle qrBusiness = new Rectangle(xPosQR, (int)(yPosQR + qrMenu.Height * 1.1 + _borderTop *0.4), _qrBusinessTexture.Width/2, _qrBusinessTexture.Height/2);
            Rectangle qrBlog = new Rectangle(xPosQR, (int)(yPosQR + qrMenu.Height * 1.1 + _borderTop * 0.8 + qrBusiness.Height * 1.1), _qrBlogTexture.Width / 2, _qrBlogTexture.Height / 2);

            //spriteBatch.Draw(_qrMenuTexture, qrMenu, Color.White);
            //spriteBatch.DrawString(_fontSmall, "Homepage", new Vector2(xPosQR, (int)(yPosQR + qrMenu.Height*1.1)), Color.Black);

            //spriteBatch.Draw(_qrBusinessTexture, qrBusiness, Color.White);
            //spriteBatch.DrawString(_fontSmall, "Career", new Vector2(xPosQR, (int)(yPosQR + qrMenu.Height * 1.1 + _borderTop * 0.4 + qrBusiness.Height * 1.1)), Color.Black);

            //spriteBatch.Draw(_qrBlogTexture, qrBlog, Color.White);
            //spriteBatch.DrawString(_fontSmall, "Blog", new Vector2(xPosQR, (int)(yPosQR + qrMenu.Height * 1.1 + _borderTop * 0.8 + qrBusiness.Height * 1.1 + qrBlog.Height * 1.1)), Color.Black);

            if (_drawRanking) 
            {
                DrawRankingText(spriteBatch, (int)(_borderLeftRight * 1.2 + _buttonTextureRed.Width), (int)(_borderTop * 3.2), _carList);
            }

            foreach (var component in _components)
            {
                component.Draw(gameTime, spriteBatch);
            }
            spriteBatch.End();
        }

        private void DrawSingleSessionButton(Session mySession, int pos_x, int pos_y, Texture2D buttonTexture)
        {
            var sessionButton = new SessionButton(buttonTexture, _buttonFont)
            {
                Position = new Vector2(pos_x, pos_y),
                Text = mySession.Name,
                SessionId = mySession.Id.ToString()
            };
            sessionButton.Click += Session_Click;
            _components.Add(sessionButton);
            _sessionButtons.Add(sessionButton);
        }

        private void DrawSingleRemoveSessionButton(Session mySession, int pos_x, int pos_y, Texture2D buttonTexture)
        {
            var sessionRemoveButton = new SessionButton(buttonTexture, _buttonFont)
            {
                Position = new Vector2(pos_x, pos_y),
                SessionId = mySession.Id.ToString()
            };
            sessionRemoveButton.Click += SessionRemove_Click;
            _components.Add(sessionRemoveButton);
            _sessionButtons.Add(sessionRemoveButton);
        }

        private void NewGameButton_Click(object sender, EventArgs e)
        {
            Game.Session = null;

            var sessionName = _sessionNameButton.Text;
            Random rnd = new Random();

            if (sessionName.Length == 0)
            {
                sessionName = "MyGame" + rnd.NextDouble().ToString();
            }
            
            Game.Session = _clientService.CreateSession(GameTimeSetting, _trackNum, sessionName);

            var gameState = _stateFactory.CreateState<IGameState<GameStateOptions>, GameStateOptions>(GraphicsDevice, Content,
                x =>
                {
                    x.RaceCarName = GetPlayerName();
                });
            _carList.Clear();
            Game.ChangeState(gameState);
        }

        public override void Update(GameTime gameTime)
        {
            if (gameTime.ElapsedGameTime.Seconds % 2 == 0)
            {
                UpdateSessions();
            }

            foreach (var component in _components)
            {
                component.Update(gameTime);
            }
            
            if (_playerButton.Clicked || _sessionNameButton.Clicked)
            {
                OwlKeyboard.GetState();
                var keyState = OwlKeyboard.currentKeyState;
                var pressed = keyState.GetPressedKeys();


                if (_playerButton.Clicked && _releasedKey)
                {
                    PlayerInput(_playerButton, pressed);
                    _oldPressedKey = pressed;
                }

                else if (_sessionNameButton.Clicked && _releasedKey)
                {
                    PlayerInput(_sessionNameButton, pressed);
                    _oldPressedKey = pressed;
                }

                if (_oldPressedKey.Length > 0)
                {
                    if (keyState.IsKeyDown(_oldPressedKey[0]))
                    {
                        _releasedKey = false;
                    }
                    else if (keyState.IsKeyUp(_oldPressedKey[0]))
                    {
                        _releasedKey = true;
                    }
                }
            }
        }


        private void PlayerInput(Button button, Keys[] pressed)
        {
            if (pressed.Length > 0)
            {
                if (pressed[0].ToString() == "Back" && button.Text.Length > 0)
                {
                    button.Text = button.Text.Remove(button.Text.Length - 1);
                } 
                else if (pressed[0].ToString() != "Back")
                {
                    button.Text = button.Text + pressed[0].ToString();
                }
            }
        }

        private void UnclickAll(Button selectedButton)
        {
            foreach(var button in _buttonList)
            {
                if (button != selectedButton)
                {
                    button.Clicked = false;
                }
                else
                {
                    button.Clicked = !button.Clicked;
                }
            }
        }
        private string GetPlayerName()
        {
            string raceCarName;

            if (_playerButton.Text.Length == 0)
            {
                Random rnd = new Random();
                raceCarName = "car_" + rnd.NextDouble().ToString();
            }
            else
            {
                raceCarName = _playerButton.Text;
            }

            return raceCarName;
        }

        private void PlayerNameButton_Click (object sender, EventArgs e)
        {
            UnclickAll(_playerButton);
        }

        private void SessionNameButton_Click(object sender, EventArgs e)
        {
            UnclickAll(_sessionNameButton);
        }

        private void PlayerModeButton_Click(object sender, EventArgs e)
        {
            _playerModeButton.Clicked = true;
            _spectatorModeButton.Clicked = false;
            Game.IsSpectator = false;           
        }

        private void SpectatorModeButton_Click(object sender, EventArgs e)
        {
            _playerModeButton.Clicked = false;
            _spectatorModeButton.Clicked = true;
            Game.IsSpectator = true;
        }


        private void QuitGameButton_Click(object sender, EventArgs e)
        {
            try
            {
                Game.Exit();
            }
            catch (Exception ex)
            {
                Logger.LogError($"{ex} Exception caught.");
            }            
        }

        private void Session_Click(object sender, EventArgs e)
        {
            _drawRanking = false;
            _carList.Clear();
            foreach (var sessionButton in _components.OfType<SessionButton>())
            {
                if (sender == sessionButton)
                {
                    sessionButton.Clicked = true;
                    sessionButton.NumClicked += 1;
                    _selectedSession = sessionButton.SessionId;

                    if (sessionButton.NumClicked == 1)
                    {
                        Logger.LogInformation($"show ranking for session {_selectedSession}]");
                        _drawRanking = true;
                        var sessionGuid = new Guid(_selectedSession);
                        Game.Session = _clientService.GetSession(sessionGuid);

                        var carIds = _gameService.GetRaceCarIdsAsync(Game.Session.Id).Result;
                        var newCars = carIds
                           //.Take(10)
                           .Where(id => _carList.All(car => car.Id != id))
                           .Select(newId => new RaceCar(new Guid(), newId, string.Empty, string.Empty));

                        foreach (var newCar in newCars)
                        {
                            var carData = _gameService.GetRaceCarDataAsync(newCar);

                            newCar.Name = carData.Result.Name;
                            newCar.ScoreOverall = carData.Result.ScoreOverall;
                            _carList.Add(newCar);
                        }
                    }
                    if(sessionButton.NumClicked == 2)
                    {
                        Logger.LogInformation($"Join Game {_selectedSession}]");

                        var gameState = _stateFactory.CreateState<IGameState<GameStateOptions>, GameStateOptions>(GraphicsDevice, Content, x =>
                        {
                            x.RaceCarName = GetPlayerName();
                        });

                        Game.ChangeState(gameState);
                        _drawRanking = false;
                    }
                }
                else
                {
                    sessionButton.Clicked = false;
                    sessionButton.NumClicked = 0;
                }
            }
        }

        private void SessionRemove_Click(object sender, EventArgs e)
        {
            foreach (var sessionButton in _components.OfType<SessionButton>())
            {
                if (sender == sessionButton)
                {
                    var sessionGuid = new Guid(sessionButton.SessionId);
                    var destroySession = _clientService.GetSession(sessionGuid);
                    _clientService.DestroySession(destroySession);
                    continue;
                }
            }
            _drawRanking = false;
        }

        private void Track0Button_Click(object sender, EventArgs e)
        {
            _track0Button.Clicked = true;
            _track1Button.Clicked = false;
            _track2Button.Clicked = false;
            _trackNum = 0;
        }

        private void Track1Button_Click(object sender, EventArgs e)
        {
            _track0Button.Clicked = false;
            _track1Button.Clicked = true;
            _track2Button.Clicked = false;
            _trackNum = 1;
        }

        private void Track2Button_Click(object sender, EventArgs e)
        {
            _track0Button.Clicked = false;
            _track1Button.Clicked = false;
            _track2Button.Clicked = true;
            _trackNum = 2;
        }

        private void DrawRankingText(SpriteBatch spriteBatch, int xPos, int yPos, List<RaceCar> raceCarList)
        {
            var numPlayers = 0;

            if (Game.Session.Scores.Count() > 0)
            {
                //List<RaceCar> SortedList = raceCarList.OrderByDescending(o => o.ScoreOverall).ToList();
                var SortedList = Game.Session.Scores.OrderByDescending(o => o.Value).ToList();
                numPlayers = Game.Session.Scores.Count();
            }
            
            spriteBatch.Draw(_background, new Rectangle(xPos, yPos, 500, numPlayers * 20 + 60), null, _corporateGray40, 0, new Vector2(0, 0), SpriteEffects.None, 0);
            spriteBatch.DrawString(_font, "Session: " + Game.Session.Name, new Vector2(xPos+10, yPos+10), Color.Black);
            spriteBatch.DrawString(_font, "Ranking List ", new Vector2(xPos+10, yPos+30), Color.Black);
            spriteBatch.DrawString(_font, "(active Players)", new Vector2(xPos + 150, yPos + 30), Color.DarkGreen);

            if (Game.Session.Scores.Count() > 0)
            {
                //List<RaceCar> SortedList = raceCarList.OrderByDescending(o => o.ScoreOverall).ToList();
                var SortedList = Game.Session.Scores.OrderByDescending(o => o.Value).ToList();
                var ranking = 1;
                var _color = Color.Black;

                foreach (var car in SortedList)
                {
                    foreach (var item in raceCarList)
                    {
                        if (item.Id.ToString() == car.Key.Id.ToString())
                        {
                            _color = Color.DarkGreen;
                        }
                    }

                    spriteBatch.DrawString(_font, ranking.ToString(), new Vector2(xPos+10, yPos + 50), _color);
                    spriteBatch.DrawString(_font, car.Key.Name, new Vector2(xPos + 70, yPos + 50), _color);
                    spriteBatch.DrawString(_font, car.Value.ToString(), new Vector2(xPos + 370, yPos + 50), _color);
                    ranking += 1;
                    yPos += 20;
                    _color = Color.Black;
                }
            }
            else
            {
                spriteBatch.DrawString(_font, "no players", new Vector2(xPos+10, yPos + 70), Color.Black);
            }
        }
    }
}