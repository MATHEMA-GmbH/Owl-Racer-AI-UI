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
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Bibliography;
using System.Xml;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using Matlabs.OwlRacer.GameClient.States.Layout;

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
        private Button _pageButton;
        private Button _previousPageButton;
        private Button _nextPageButton;
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
        private Texture2D _logoMathemaDarkOwl;
        private Texture2D _buttonTextureLeft;
        private Texture2D _buttonTextureRight;

        //Corporate Colors

        private Color _corporateGray40 = new Color(187, 188, 188);
        private Color _corporateGray60 = new Color(136, 139, 141);
        private Color _corporateGreen = new Color(44, 154, 117);

        // Positions and Limits
        private int _borderLeftRight;
        private int _borderTop;
        private int _startPosCarX;
        private int _startPosCarY;
        private int _rotation;

        private double _oldGameTime;
        private double _newGameTime;

        private bool _releasedKey = true;
        private Keys[] _oldPressedKey;

        //Layout & Resizing of UI
        private float _scaleFactor;
        private float _scaleX;
        private float _scaleY;
        private int _buttonHeight;
        private int _buttonWidth;

        //Page functionality
        private int _pageSize;
        private int _currentPageNumber;
        private int _totalPageNumber;
        private int _oldSessionCount;
        private int _newSessionCount;
        private int pos_y;
        private int pos_x;

        //List of Keys for Keyboard input Whitelist
        private List<string> _legitKeys;

        //Admin Utility
        private Button _adminModeButton;
        private bool _adminModeOldStatus;
        private List<Component> _removeSessionButtons = new();



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
            Game.IsAdmin = false;
            _drawRanking = false;
            _startPosCarX = 0;
            _startPosCarY = 0;
            _oldGameTime = 0;
            _newGameTime = 0;
            _rotation = 0;
            _borderLeftRight = (int)(GraphicsDevice.Adapter.CurrentDisplayMode.Width * 0.12);
            _borderTop = (int)(GraphicsDevice.Adapter.CurrentDisplayMode.Height * 0.1);

            // Initializing variables for resizing of UI
            LayoutUtility.screenHeight = (int)(GraphicsDevice.Adapter.CurrentDisplayMode.Height);
            LayoutUtility.screenWidth = (int)(GraphicsDevice.Adapter.CurrentDisplayMode.Width);
            _scaleX = ((float)LayoutUtility.screenWidth / (float)1920);
            _scaleY = ((float)LayoutUtility.screenHeight / (float)1200);
            _scaleFactor = Math.Min(_scaleX, _scaleY);
            LayoutUtility.scaleFactor = _scaleFactor;
            _buttonHeight = LayoutUtility.heightPx(1);
            _buttonWidth = LayoutUtility.widthPx(1);
            

            //Initialize variables for Page handling
            _currentPageNumber = 1;
            _totalPageNumber = 1;
            _oldSessionCount = 0;
            _pageSize = 4;


            _buttonTexture = Content.Load<Texture2D>("Images/Button");
            _buttonTextureRed = Content.Load<Texture2D>("Images/ButtonRed");
            _buttonTextureX = Content.Load<Texture2D>("Images/ButtonX");
            _buttonFont = Content.Load<SpriteFont>("Inter-Regular");
            _fontSmall = Content.Load<SpriteFont>("Inter-Regular-small");
            _track0Texture = Content.Load<Texture2D>("Images/level0");
            _track1Texture = Content.Load<Texture2D>("Images/level1");
            _track2Texture = Content.Load<Texture2D>("Images/level2");
            _buttonTextureLeft = Content.Load<Texture2D>("Images/ButtonLeft");
            _buttonTextureRight = Content.Load<Texture2D>("Images/ButtonRight");


            //Starting values for page functionality
            pos_y = LayoutUtility.YValue(6);
            pos_x = LayoutUtility.XValue(0);

            //initialize variables for Admin Functionality
            _adminModeOldStatus = false;

            //Creating list of legit Keys
            _legitKeys = createLegitKeyList();



            _newGameButton = new Button(_buttonTexture, _buttonFont, _scaleFactor)
            {
                Position = LayoutUtility.VectorPosXY(1,3),
                Text = "New Game",
                HoverColor = _corporateGreen,
                Width = _buttonWidth/2,
                Height = _buttonHeight,
            };

            
            _newGameButton.Click += NewGameButton_Click;

            _quitGameButton = new Button(_buttonTexture, _buttonFont, _scaleFactor)
            {
                Position = LayoutUtility.VectorPosXY(1.5, 3),
                Text = "Quit Game",
                ButtonColor = _corporateGray40,
                Width = _buttonWidth/2,
                Height = _buttonHeight,
            };

            _quitGameButton.Click += QuitGameButton_Click;

            _playerButton = new Button(_buttonTexture, _buttonFont, _scaleFactor)
            {
                Position = LayoutUtility.VectorPosXY(0,1),
                Text = "",
                ButtonColor = Color.White,
                Width = _buttonWidth,
                Height = _buttonHeight,
            };

            _playerButton.Click += PlayerNameButton_Click;

            _sessionNameButton = new Button(_buttonTexture, _buttonFont, _scaleFactor)
            {
                Position = LayoutUtility.VectorPosXY(1, 1),
                Text = "",
                Width = _buttonWidth,
                Height = _buttonHeight
            };

            _sessionNameButton.Click += SessionNameButton_Click;

            _playerModeButton = new Button(_buttonTexture, _buttonFont, _scaleFactor)
            {
                Position = LayoutUtility.VectorPosXY(0, 3),
                Text = "Player",
                ButtonColor = Color.White,
                Width = _buttonWidth/2,
                Height = _buttonHeight
            };
            _playerModeButton.Clicked = true;

            _playerModeButton.Click += PlayerModeButton_Click;

            _spectatorModeButton = new Button(_buttonTexture, _buttonFont, _scaleFactor)
            {
                Position = LayoutUtility.VectorPosXY(0.5, 3),
                Text = "Spectator",
                ButtonColor = Color.White,
                Width = _buttonWidth/2,
                Height = _buttonHeight,
            };

            _spectatorModeButton.Click += SpectatorModeButton_Click;

            _track0Button = new Button(_track0Texture, _buttonFont, _scaleFactor)
            {
                Position = LayoutUtility.VectorPosXY(0,12), //(int)(_borderLeftRight * 1.2 + _buttonTextureRed.Width), (int)(_borderTop * 3.2)
                Text = "Level 1",
                ButtonColor = Color.White,
                Width = _buttonWidth,
                Height = (int)(_buttonWidth * (float)_track0Texture.Height/ (float)_track0Texture.Width),
            };
            _track0Button.Click += Track0Button_Click;

            _track1Button = new Button(_track1Texture, _buttonFont, _scaleFactor)
            {
                Position = LayoutUtility.VectorPosXY(1, 12),
                Text = "Level 2",
                ButtonColor = Color.White,
                Width = _buttonWidth,
                Height = (int)(_buttonWidth * (float)_track1Texture.Height / (float)_track1Texture.Width),
            };
            _track1Button.Click += Track1Button_Click;

            _track2Button = new Button(_track2Texture, _buttonFont, _scaleFactor)
            {
                Position = LayoutUtility.VectorPosXY(2, 12),
                Text = "Level 3",
                ButtonColor = Color.White,
                Width = _buttonWidth,
                Height = (int)(_buttonWidth * (float)_track2Texture.Height / (float)_track2Texture.Width),
            };
            _track2Button.Click += Track2Button_Click;


            _pageButton = new Button(_buttonTextureRed, _buttonFont, _scaleFactor)
            {
                Position = LayoutUtility.VectorPosXY(0, 10),
                Width = _buttonWidth,
                Height = _buttonHeight,
            };


            _previousPageButton = new Button(_buttonTextureLeft, _buttonFont, _scaleFactor)
            {
                Position = LayoutUtility.VectorPosXY(0, 10),
                Height = _buttonHeight,
                Width = _buttonHeight,
            };
            _previousPageButton.Click += previousPageButton_Click;



            _nextPageButton = new Button(_buttonTextureRight, _buttonFont, _scaleFactor)
            {
                Position = new Vector2((int)((double)LayoutUtility.screenWidth*(0.1+0.25)-(double)_buttonHeight), LayoutUtility.YValue(10)),
                Height = _buttonHeight,
                Width = _buttonHeight,
            };
            _nextPageButton.Click += nextPageButton_Click;

            _adminModeButton = new Button(_buttonTexture, _buttonFont, _scaleFactor)
            {
                Position = LayoutUtility.VectorPosXY(0.5, 4),
                Height = _buttonHeight,
                Width = _buttonWidth / 2,
                ButtonColor = Color.White,
                Text ="Off",
            };
            _adminModeButton.Click += adminModeButton_Click;

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
                _track2Button,
                _adminModeButton,
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
            _components.Add(_adminModeButton);
            //Resetting values for page Feature
            pos_y = LayoutUtility.YValue(6);
            pos_x = LayoutUtility.XValue(0);
        }

        private void UpdateSessions()
        {
            _availableSessions = _clientService.GetSessionIds();
            

            // Recalculating Page Number
            if (_oldSessionCount != _newSessionCount)
            {
                _totalPageNumber = (int)Math.Ceiling((double)(_availableSessions.Guids.Count) / (double)(_pageSize));
                _pageButton.Text = "Page: " + _currentPageNumber + "/" + _totalPageNumber;
                if(_currentPageNumber > _totalPageNumber)
                {
                    _currentPageNumber = _totalPageNumber;
                }
            }


            if (_oldSessions == null)
            {
                RevertComponentsToInit();
            }
            else if (_availableSessions.Guids.Equals(_oldSessions.Guids) == false || Game.IsAdmin != _adminModeOldStatus)
            {
                RevertComponentsToInit();
            }

            //Drawing Session Button Pages
            if (_availableSessions.Guids.Count != 0)
            {
                // Adding Page utility when needed
                if(_availableSessions.Guids.Count > _pageSize)
                {
                    if (!_components.Contains(_pageButton))
                    {
                        _components.Add(_pageButton);
                    }
                    if(!_components.Contains(_nextPageButton))
                    {
                        _components.Add(_nextPageButton);
                    }
                    if(!_components.Contains(_previousPageButton))
                    {
                        _components.Add(_previousPageButton);
                    }
                }
                
                // List of sessions to order by sessionname
                // and create session buttons
                List<Session> openSessions = new List<Session>();
                foreach (var entry in _availableSessions.Guids)
                {
                    var sessionGuid = new Guid(entry.GuidString);
                    openSessions.Add(_clientService.GetSession(sessionGuid));
                }
                openSessions = openSessions.OrderBy(x => x.Name).ToList();

                //Drawing of session buttons based on current page
                for (int i = (_currentPageNumber - 1) * _pageSize; i < Math.Min(openSessions.Count, _currentPageNumber * _pageSize); i++)
                {
                    var mySession = openSessions.ElementAt(i);
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
                        DrawSingleSessionButton(mySession, pos_x, pos_y, _buttonTextureRed);
                        if (Game.IsAdmin)
                        {
                            DrawSingleRemoveSessionButton(mySession, pos_x, pos_y, _buttonTextureX);
                        }
                        int currentIndexOfFirstPageEntry = ((_currentPageNumber - 1) * _pageSize);
                        pos_y = LayoutUtility.YValue(i - currentIndexOfFirstPageEntry + 7);
                    }
                }
                _newSessionCount = _availableSessions.Guids.Count;
                pos_y = LayoutUtility.YValue(6);
                _adminModeOldStatus = Game.IsAdmin;
            }                            
        }


        public override void LoadContent(GameTime gameTime)
        {
            _font = Content.Load<SpriteFont>("Inter-SemiBold"); 
            _logo = Content.Load<Texture2D>(@"Images/owlracer-logo-solo");
            _logoMathema = Content.Load<Texture2D>(@"Images/mathema-logo");
            _circle = Content.Load<Texture2D>(@"Images/Circle_down");
            _street = Content.Load<Texture2D>(@"Images/Street");
            _logoMathemaDarkOwl = Content.Load<Texture2D>(@"Images/mat-pictogram-rgb-dark");

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

            //Writing Text elements in GUI

            spriteBatch.DrawString(_font, "Admin Mode: ", LayoutUtility.VectorPosXY(0, 4), Color.White, (float)0.0, new Vector2(0, 0), _scaleFactor, SpriteEffects.None, (float)0.0);
            spriteBatch.DrawString(_font, "Available Sessions: ", LayoutUtility.VectorPosXY(0, 5), Color.White, (float)0.0, new Vector2(0, 0), _scaleFactor, SpriteEffects.None, (float)0.0);
            spriteBatch.DrawString(_font, "Enter Player Name: ", LayoutUtility.VectorPosXY(0,0), Color.White, (float)0.0, new Vector2(0, 0), _scaleFactor, SpriteEffects.None, (float)0.0);
            spriteBatch.DrawString(_font, "Enter Session Name: ", LayoutUtility.VectorPosXY(1,0), Color.White, (float)0.0, new Vector2(0,0), _scaleFactor, SpriteEffects.None, (float) 0.0);
            spriteBatch.DrawString(_font, "Choose Mode: ", LayoutUtility.VectorPosXY(0,2), Color.White, (float)0.0, new Vector2(0, 0), _scaleFactor, SpriteEffects.None, (float)0.0);
            spriteBatch.DrawString(_font, "Choose Track: ", LayoutUtility.VectorPosXY(0,11), Color.White, (float)0.0, new Vector2(0, 0), _scaleFactor, SpriteEffects.None, (float)0.0);

            // Declaring and Initializing variables for Mathema Logo-Unit
            
            int logoRectWidth = (int)((double)_logo.Width * 0.3 * _scaleX);
            int logoRectHeight = (int)((double)_logo.Height * 0.3 * _scaleY);

            int logoRectMathemaWidth = (int)((double)_logoMathema.Width * 0.1 * _scaleX);
            int logoRectMathemaHeight = (int)((double)_logoMathema.Height * 0.1 * _scaleY);

            int logoRectCircleWidth = LayoutUtility.circleWidth();
            int logoRectCircleHeight = logoRectCircleWidth;



            // Creating Rectangles for Drawing of Logo-Unit
            Rectangle logoRect = new Rectangle(LayoutUtility.topRightXValue(), LayoutUtility.topRightYValue(0), logoRectWidth, logoRectHeight);
            Rectangle logoRectMathema = new Rectangle(LayoutUtility.topRightXValue(), LayoutUtility.topRightYValue(2), logoRectMathemaWidth, logoRectMathemaHeight);
            Rectangle logoRectCircle = new Rectangle(LayoutUtility.screenWidth - logoRectCircleWidth, 0, logoRectCircleWidth, logoRectCircleHeight);



            Rectangle logoRectStreet1 = new Rectangle(0, 0,GraphicsDevice.Adapter.CurrentDisplayMode.Width, (int)(_borderTop*0.8));

            Rectangle logoRectDarkOwl = new Rectangle(LayoutUtility.XValue(2),LayoutUtility.YValue(0), LayoutUtility.widthPx(1), LayoutUtility.heightPx(14));


            //Drawing Logo-Unit
            spriteBatch.Draw(_street, logoRectStreet1, null, Color.White, (float)0.0, new Vector2(0, 0), SpriteEffects.None, (float)0.0);
            spriteBatch.Draw(_circle, logoRectCircle, null, Color.White, (float)0.0, new Vector2(0, 0), SpriteEffects.None, (float)0.0);
            spriteBatch.Draw(_logo, logoRect, null, Color.White,(float)0.0, new Vector2(0,0), SpriteEffects.None, (float)0.0);
            spriteBatch.DrawString(_fontSmall, "EIN PROJEKT DER", LayoutUtility.topRightVectorPosXY(1), _corporateGray60, (float)0.0, new Vector2(0, 0), _scaleFactor, SpriteEffects.None, (float)0.0);
            spriteBatch.Draw(_logoMathema, logoRectMathema, null, Color.White, (float)0.0, new Vector2(0, 0), SpriteEffects.None, (float)0.0);
            spriteBatch.Draw(_logoMathemaDarkOwl, logoRectDarkOwl, null, Color.White, (float)0.0, new Vector2(0, 0), SpriteEffects.None, (float)0.0);

            spriteBatch.Draw(
                    _raceCarTexture,
                    new Rectangle(_startPosCarX, _startPosCarY,
                    (int)(_raceCarTexture.Width*_scaleX), (int)(_raceCarTexture.Height*_scaleY)),
                    new Rectangle(0, 0, _raceCarTexture.Width, _raceCarTexture.Height),
                    Color.White,
                    _rotation,
                    new Vector2((int)(_raceCarTexture.Width / 2), (int)(_raceCarTexture.Height / 2)),
                    SpriteEffects.None,
                    1.0f
                );

            if (_drawRanking) 
            {
                DrawRankingText(spriteBatch, LayoutUtility.XValue(1), LayoutUtility.YValue(5), _carList);
            }

            foreach (var component in _components)
            {
                component.Draw(gameTime, spriteBatch);
            }

            spriteBatch.End();
        }

        private void DrawSingleSessionButton(Session mySession, int pos_x, int pos_y, Texture2D buttonTexture)
        {
            var sessionButton = new SessionButton(buttonTexture, _buttonFont, _scaleFactor)
            {
                Position = new Vector2(pos_x, pos_y),
                Text = mySession.Name,
                SessionId = mySession.Id.ToString(),
                Width = _buttonWidth,
                Height = _buttonHeight,
            };
            sessionButton.Click += Session_Click;
            _components.Add(sessionButton);
            _sessionButtons.Add(sessionButton);
        }

        private void DrawSingleRemoveSessionButton(Session mySession, int pos_x, int pos_y, Texture2D buttonTexture)
        {
            var sessionRemoveButton = new SessionButton(buttonTexture, _buttonFont, _scaleFactor)
            {
                Position = new Vector2(pos_x, pos_y),
                SessionId = mySession.Id.ToString(),
                Height = _buttonHeight,
                Width = _buttonHeight,
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
                sessionName = "Game";
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
            if (_nextPageButton.Clicked == true)
            {
                RevertComponentsToInit();
            }
            _nextPageButton.Clicked = false;

            if (_previousPageButton.Clicked == true)
            {
                RevertComponentsToInit();
            }
            _previousPageButton.Clicked = false;




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
            if (pressed.Length > 0 && _legitKeys.Contains(pressed[0].ToString()))
            {
                if (pressed[0].ToString() == "Back" && button.Text.Length > 0)
                {
                    button.Text = button.Text.Remove(button.Text.Length - 1);
                } 
                else if(pressed[0].ToString() == "Space" && button.Text.Length > 0)
                {
                    button.Text = button.Text + " ";
                }
                
                //filter condition for keyboard numbers 0-9
                //Number 0 is stored as D0 etc.
                else if (pressed[0].ToString().Contains('D') && pressed[0].ToString() != "D")
                {
                    string pressedNumber = pressed[0].ToString();
                    pressedNumber = pressedNumber.Remove(0,1);
                    button.Text = button.Text + pressedNumber;
                }
                else if (pressed[0].ToString() != "Back" && pressed[0].ToString() != "Space")
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
                raceCarName = "You";
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

        private void adminModeButton_Click(object sender, EventArgs e)
        {
            Game.IsAdmin = !Game.IsAdmin;
            if(Game.IsAdmin == true)
            {
                _adminModeButton.Text = "On";
                _adminModeButton.Clicked = true;
            }
            else
            {
                _adminModeButton.Text = "Off";
                _adminModeButton.Clicked = false;
            }
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

        private void previousPageButton_Click(object sender, EventArgs e)
        {
            _currentPageNumber = Math.Max(1, _currentPageNumber - 1);
            _pageButton.Text = "Page: " + _currentPageNumber + "/" + _totalPageNumber;
            _previousPageButton.Clicked = true;
        }
        private void nextPageButton_Click(object sender, EventArgs e)
        {
            _currentPageNumber = Math.Min(_currentPageNumber + 1, _totalPageNumber); 
            _pageButton.Text = "Page: " + _currentPageNumber + "/" + _totalPageNumber;
            _nextPageButton.Clicked = true;

        }

        //Method creates a list of key Inputs that is used for a whitelist
        private List<string> createLegitKeyList()
        {
            List<String> legitKeys = new List<String>();

            //Creating list of letters
            //Monogame apparently only knows capital letters
            char[] alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();
            foreach (char x in alphabet)
            {
                legitKeys.Add(x.ToString());
            }

            //Adding Keyboard Inputs 0-9 which are stored as D0-D9 
            for(int i = 0; i < 10; i++)
            {
                string interimResult = "D" + i;
                legitKeys.Add(interimResult);
            }

            // Adding exception for back button
            legitKeys.Add("Back");
            legitKeys.Add("Space");

            return legitKeys;
        }

        private void DrawRankingText(SpriteBatch spriteBatch, int xPos, int yPos, List<RaceCar> raceCarList)
        {
            var numPlayers = 0;
            int maxNumPlayers = 5;

            if (Game.Session.Scores.Count() > 0)
            {
                //List<RaceCar> SortedList = raceCarList.OrderByDescending(o => o.ScoreOverall).ToList();
                var SortedList = Game.Session.Scores.OrderByDescending(o => o.Value).ToList();
                numPlayers = Game.Session.Scores.Count();
            }
            
            int _textLineHeight = (int)((double)_font.MeasureString("A").Y * _scaleFactor);

            int numberOfRows = (int)(Math.Min((Math.Max(numPlayers + 3, 4)), maxNumPlayers +3));

            spriteBatch.Draw(_background, new Rectangle(xPos, yPos, LayoutUtility.widthPx(1), (int)((float)numberOfRows * _textLineHeight)), null, _corporateGray40, 0, new Vector2(0, 0), SpriteEffects.None, 0);
            spriteBatch.DrawString(_font, "Session: " + Game.Session.Name, new Vector2(xPos+ LayoutUtility.widthPx(0.01), yPos), Color.Black, (float)0.0, new Vector2(0, 0), _scaleFactor, SpriteEffects.None, (float)0.0);
            yPos += _textLineHeight;
            spriteBatch.DrawString(_font, "Ranking List ", new Vector2(xPos + LayoutUtility.widthPx(0.01), yPos), Color.Black, (float)0.0, new Vector2(0, 0), _scaleFactor, SpriteEffects.None, (float)0.0);
            yPos += _textLineHeight;
            spriteBatch.DrawString(_font, "(active Players)", new Vector2(xPos + LayoutUtility.widthPx(0.01), yPos), Color.DarkGreen, (float)0.0, new Vector2(0, 0), _scaleFactor, SpriteEffects.None, (float)0.0);
            yPos += _textLineHeight;

            if (Game.Session.Scores.Count() > 0)
            {
                //List<RaceCar> SortedList = raceCarList.OrderByDescending(o => o.ScoreOverall).ToList();
                var SortedList = Game.Session.Scores.OrderByDescending(o => o.Value).ToList();
                var ranking = 1;
                var _color = Color.Black;

                for (int i = 0; i < Math.Min(SortedList.Count, maxNumPlayers); i++)
                {
                    var car = SortedList.ElementAt(i);
                    foreach (var item in raceCarList)
                    {
                        if (item.Id.ToString() == car.Key.Id.ToString())
                        {
                            _color = Color.DarkGreen;
                        }
                    }

                    spriteBatch.DrawString(_font, ranking.ToString(), new Vector2(xPos + LayoutUtility.widthPx(0.01), yPos), _color, (float)0.0, new Vector2(0, 0), _scaleFactor, SpriteEffects.None, (float)0.0);
                    spriteBatch.DrawString(_font, car.Key.Name, new Vector2(xPos + LayoutUtility.widthPx(0.15), yPos), _color, (float)0.0, new Vector2(0, 0), _scaleFactor, SpriteEffects.None, (float)0.0);
                    spriteBatch.DrawString(_font, car.Value.ToString(), new Vector2(xPos + LayoutUtility.widthPx(0.7), yPos), _color, (float)0.0, new Vector2(0, 0), _scaleFactor, SpriteEffects.None, (float)0.0);
                    ranking += 1;
                    yPos += _textLineHeight;
                    _color = Color.Black;
                }
            }
            else
            {
                spriteBatch.DrawString(_font, "no players", new Vector2(xPos + LayoutUtility.widthPx(0.01), yPos), Color.Black, (float)0.0, new Vector2(0, 0), _scaleFactor, SpriteEffects.None, (float)0.0);
            }
        }
    }
}