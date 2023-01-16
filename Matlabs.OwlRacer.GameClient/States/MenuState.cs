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

        // trial and error

        private int _fullSizeTop;
        private int _fullSizeLeftRight;
        private float _scaleX;
        private float _scaleY;
        private int _buttonHeight;
        private int _buttonWidth;
        private double _borderFactor;
        private double _columnSizeFactor;
        private double _columnBorder;
        private double _rowSizeFactor;
        private double _rowBorder;
        private Texture2D _logoMathemaDarkOwl;
        private Texture2D _buttonTextureLeft;
        private Texture2D _buttonTextureRight;
        private double _scoreLineSize;
        private int _currentPageNumber;
        private int _totalPageNumber;
        private Button _pageButton;
        private Button _previousPageButton;
        private Button _nextPageButton;
        private int _oldSessionCount;
        private int _newSessionCount;
        private int pos_y_initial;
        private List<string> _legitKeys;



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

            //trial and error
            _fullSizeTop = (int)(GraphicsDevice.Adapter.CurrentDisplayMode.Height);
            _fullSizeLeftRight = (int)(GraphicsDevice.Adapter.CurrentDisplayMode.Width);
            _scaleX = ((float)_fullSizeLeftRight / (float)1920);
            _scaleY = ((float)_fullSizeTop / (float)1080);
            _borderFactor = 0.1;
            _columnSizeFactor = 0.25;
            _columnBorder = 0.025;
            _rowSizeFactor = 0.04;
            _rowBorder = 0.01;
            _buttonHeight = (int)(_rowSizeFactor*_fullSizeTop);
            _buttonWidth = (int)(_fullSizeLeftRight*_columnSizeFactor);
            _scoreLineSize = 0.015;
            _currentPageNumber = 1;
            _totalPageNumber = 2;
            _oldSessionCount = 0;

            //Creating list of legit Keys
            _legitKeys = createLegitKeyList();









            _buttonTexture = Content.Load<Texture2D>("Images/Button");
            _buttonTextureRed = Content.Load<Texture2D>("Images/ButtonRed");
            _buttonTextureX = Content.Load<Texture2D>("Images/ButtonX");
            //_buttonFont = Content.Load<SpriteFont>("Inter-Regular");
            _fontSmall = Content.Load<SpriteFont>("Inter-Regular-small");
            _track0Texture = Content.Load<Texture2D>("Images/level0");
            _track1Texture = Content.Load<Texture2D>("Images/level1");
            _track2Texture = Content.Load<Texture2D>("Images/level2");
            _buttonTextureLeft = Content.Load<Texture2D>("Images/ButtonLeft");
            _buttonTextureRight = Content.Load<Texture2D>("Images/ButtonRight");

            // Skalierung der Schrift über Bildschirmauflösung

            if (_fullSizeLeftRight * _fullSizeTop > 2560 * 2048)
            {
                _buttonFont = Content.Load<SpriteFont>("Inter-Regular-big");
            }
            else if (_fullSizeLeftRight * _fullSizeTop >= 1920*1200)
            {
                _buttonFont = Content.Load<SpriteFont>("Inter-Regular");
            }
            else if (_fullSizeLeftRight * _fullSizeTop > 1280 * 720)
            {
                _buttonFont = Content.Load<SpriteFont>("Inter-Regular-small");
            }
            else
            {
                _buttonFont = Content.Load<SpriteFont>("Inter-Regular-very-small");
            }

            pos_y_initial = (int)(_fullSizeTop * (_borderFactor + 4 * _rowSizeFactor + 4 * _rowBorder));
            pos_y = pos_y_initial;
            pos_y_2 = (int)(_fullSizeTop * (_borderFactor + 4 * _rowSizeFactor + 4 * _rowBorder));

            pos_x = (int)(_fullSizeLeftRight*_borderFactor);
            pos_x_2 = (int)(_fullSizeLeftRight*(_borderFactor + _columnSizeFactor*0.5));

            _yLimit = (int)(_fullSizeTop * (_borderFactor + 10 * _rowSizeFactor + 4 * _rowBorder));

            _newGameButton = new Button(_buttonTexture, _buttonFont)
            {
                Position = new Vector2((int)(_fullSizeLeftRight * (_borderFactor + _columnSizeFactor + _columnBorder)), (int)(_fullSizeTop * (_borderFactor + 3 * _rowSizeFactor + _rowBorder))),
                Text = "New Game",
                HoverColor = _corporateGreen,
                Width = _buttonWidth/2,
                Height = _buttonHeight,
            };

            
            _newGameButton.Click += NewGameButton_Click;

            _quitGameButton = new Button(_buttonTexture, _buttonFont)
            {
                Position = new Vector2((int)(_fullSizeLeftRight * (_borderFactor + _columnSizeFactor + _columnBorder) + _newGameButton.Width), (int)(_fullSizeTop * (_borderFactor + 3 * _rowSizeFactor + _rowBorder))),
                Text = "Quit Game",
                ButtonColor = _corporateGray40,
                Width = _buttonWidth/2,
                Height = _buttonHeight,
            };

            _quitGameButton.Click += QuitGameButton_Click;

            _playerButton = new Button(_buttonTexture, _buttonFont)
            {
                Position = new Vector2((int)(_fullSizeLeftRight * _borderFactor), (int)(_fullSizeTop * (_borderFactor + _rowSizeFactor))),
                Text = "",
                ButtonColor = Color.White,
                Width = _buttonWidth,
                Height = _buttonHeight
                //Width = 230,
            };

            _playerButton.Click += PlayerNameButton_Click;

            _sessionNameButton = new Button(_buttonTexture, _buttonFont)
            {
                Position = new Vector2((int)(_fullSizeLeftRight *(_borderFactor + _columnSizeFactor + _columnBorder)), (int)(_fullSizeTop * (_borderFactor + _rowSizeFactor))),
                Text = "",
                Width = _buttonWidth,
                Height = _buttonHeight
                //Width = 230,
            };

            _sessionNameButton.Click += SessionNameButton_Click;

            _playerModeButton = new Button(_buttonTexture, _buttonFont)
            {
                Position = new Vector2((int)(_fullSizeLeftRight * _borderFactor), (int)(_fullSizeTop * (_borderFactor + 3 * _rowSizeFactor + _rowBorder))),
                Text = "Player",
                ButtonColor = Color.White,
                Width = _buttonWidth/2,
                Height = _buttonHeight
            };
            _playerModeButton.Clicked = true;

            _playerModeButton.Click += PlayerModeButton_Click;

            _spectatorModeButton = new Button(_buttonTexture, _buttonFont)
            {
                Position = new Vector2((int)(_fullSizeLeftRight * _borderFactor + _playerModeButton.Width), (int)(_fullSizeTop * (_borderFactor + 3 *  _rowSizeFactor + _rowBorder))),
                Text = "Spectator",
                ButtonColor = Color.White,
                Width = _buttonWidth/2,
                Height = _buttonHeight
            };

            _spectatorModeButton.Click += SpectatorModeButton_Click;

            _track0Button = new Button(_track0Texture, _buttonFont)
            {
                Position = new Vector2((int)(_fullSizeLeftRight * _borderFactor), (int)(_fullSizeTop*(_borderFactor + 12 * _rowSizeFactor + 6 * _rowBorder))), //(int)(_borderLeftRight * 1.2 + _buttonTextureRed.Width), (int)(_borderTop * 3.2)
                Text = "Level 1",
                ButtonColor = Color.White,
                Width = (int)(_fullSizeLeftRight*_columnSizeFactor),
                Height = (int)(_fullSizeLeftRight * _columnSizeFactor*((float)_track0Texture.Height/ (float)_track0Texture.Width)),
            };

            //(int)(_borderLeftRight * 1.4 + _buttonTextureRed.Width + 500)

            _track0Button.Click += Track0Button_Click;

            _track1Button = new Button(_track1Texture, _buttonFont)
            {
                Position = new Vector2((int)(_fullSizeLeftRight * (_borderFactor + 1 * _columnSizeFactor +  1 * _columnBorder)), (int)(_fullSizeTop * (_borderFactor + 12 * _rowSizeFactor + 6 * _rowBorder))),
                Text = "Level 2",
                ButtonColor = Color.White,
                Width = (int)(_fullSizeLeftRight * _columnSizeFactor),
                Height = (int)(_fullSizeLeftRight * _columnSizeFactor * ((float)_track1Texture.Height / (float)_track1Texture.Width)),
            };
            //(int)(_borderLeftRight * 1.4 + _buttonTextureRed.Width + 500)

            _track1Button.Click += Track1Button_Click;

            _track2Button = new Button(_track2Texture, _buttonFont)
            {
                Position = new Vector2((int)(_fullSizeLeftRight * (_borderFactor + 2 * _columnSizeFactor + 2 * _columnBorder)), (int)(_fullSizeTop * (_borderFactor + 12 * _rowSizeFactor + 6 * _rowBorder))),
                Text = "Level 3",
                ButtonColor = Color.White,
                Width = (int)(_fullSizeLeftRight * _columnSizeFactor),
                Height = (int)(_fullSizeLeftRight * _columnSizeFactor * ((float)_track2Texture.Height / (float)_track2Texture.Width)),
            };
            //(int)(_borderLeftRight * 1.4 + _buttonTextureRed.Width + 500)


            _pageButton = new Button(_buttonTextureRed, _buttonFont)
            {
                Position = new Vector2(pos_x, (int)(_fullSizeTop * (_borderFactor + 11 * _rowSizeFactor + 4 * _rowBorder))),
                Width = (int)((float)(_columnSizeFactor) * (float)(_fullSizeLeftRight * 0.47)),
                Height = (int)((float)(_columnSizeFactor) * (float)(_fullSizeLeftRight * 0.47 * (float)_buttonTextureRed.Height / (float)_buttonTextureRed.Width)),
            };


            _previousPageButton = new Button(_buttonTextureLeft, _buttonFont)
            {
                Position = new Vector2(pos_x - (int)((float)(_rowBorder) * (float)(_fullSizeLeftRight * 0.5)), (int)(_fullSizeTop * (_borderFactor + 11 * _rowSizeFactor + 4 * _rowBorder + 0.0011))),
                Height = (int)((float)(_rowBorder) * (float)(_fullSizeLeftRight)),
                Width = (int)((float)(_rowBorder) * (float)(_fullSizeLeftRight)),
            };
            _previousPageButton.Click += previousPageButton_Click;



            _nextPageButton = new Button(_buttonTextureRight, _buttonFont)
            {
                Position = new Vector2((int)(pos_x + (int)((float)(_columnSizeFactor) * (float)(_fullSizeLeftRight * 0.47)) - (int)((float)(_rowBorder) * (float)(_fullSizeLeftRight * 0.5))), (int)(_fullSizeTop * (_borderFactor + 11 * _rowSizeFactor + 4 * _rowBorder + 0.0011))),
                Height = (int)((float)(_rowBorder) * (float)(_fullSizeLeftRight)),
                Width = (int)((float)(_rowBorder) * (float)(_fullSizeLeftRight)),
            };
            _nextPageButton.Click += nextPageButton_Click;





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
                _track2Button,
                _pageButton,
                _previousPageButton,
                _nextPageButton

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
            _components.Add(_pageButton);
            _components.Add(_previousPageButton);
            _components.Add(_nextPageButton);


            pos_y = (int)(_fullSizeTop * (_borderFactor + 4 * _rowSizeFactor + 4 * _rowBorder));
            pos_y_2 = (int)(_fullSizeTop * (_borderFactor + 4 * _rowSizeFactor + 4 * _rowBorder));
            pos_x = (int)(_fullSizeLeftRight * _borderFactor);
            pos_x_2 = (int)(_fullSizeLeftRight * (_borderFactor + _columnSizeFactor*0.5));
        }

        private void UpdateSessions()
        {
            _availableSessions = _clientService.GetSessionIds();


            if (_oldSessionCount != _newSessionCount)
            {
                _totalPageNumber = (int)Math.Ceiling((double)(_availableSessions.Guids.Count) / (double)(7.0));
                _pageButton.Text = _currentPageNumber + "/" + _totalPageNumber;
            }


            if (_oldSessions == null)
            {
                RevertComponentsToInit();
            }
            else if (_availableSessions.Guids.Equals(_oldSessions.Guids) == false)
            {
                RevertComponentsToInit();
            }

            if (_availableSessions.Guids.Count != 0)
            {
                for (int i = (_currentPageNumber - 1) * 7; i < Math.Min(_availableSessions.Guids.Count, _currentPageNumber * 7); i++)
                {
                    var entry = _availableSessions.Guids.ElementAt(i);
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
                        DrawSingleSessionButton(mySession, pos_x, pos_y, _buttonTextureRed);
                        DrawSingleRemoveSessionButton(mySession, (int)(pos_x - (int)((float)(_rowBorder) * (float)(_fullSizeLeftRight * 0.5))), pos_y, _buttonTextureX);
                        pos_y = (int)(pos_y + _rowSizeFactor * _fullSizeTop);
                    }

                }
                _newSessionCount = _availableSessions.Guids.Count;
                pos_y = pos_y_initial;
            }

            //foreach (var entry in _availableSessions.Guids)
            //{
            //    var sessionGuid = new Guid(entry.GuidString);
            //    var mySession = _clientService.GetSession(sessionGuid);
            //    var buttonExist = false;

            //    foreach (var button in _components.OfType<SessionButton>())
            //    {
            //        if (button.SessionId == mySession.Id.ToString())
            //        {
            //            buttonExist = true;
            //            continue;
            //        }
            //    }

            //    if (!buttonExist)
            //    {
            //        if (pos_y < _yLimit)
            //        {
            //            DrawSingleSessionButton(mySession, pos_x, pos_y, _buttonTextureRed);
            //            DrawSingleRemoveSessionButton(mySession, (int)(pos_x - (int)((float)(_rowBorder) * (float)(_fullSizeLeftRight * 0.5))), pos_y, _buttonTextureX);
            //            pos_y = (int)(pos_y + _rowSizeFactor * _fullSizeTop);
            //        }
            //        else
            //        {
            //            DrawSingleSessionButton(mySession, pos_x_2, pos_y_2, _buttonTextureRed);
            //            DrawSingleRemoveSessionButton(mySession, (int)(pos_x_2 - (int)((float)(_rowBorder) * (float)(_fullSizeLeftRight * 0.5))), pos_y_2, _buttonTextureX);
            //            pos_y_2 = (int)(pos_y_2 + _rowSizeFactor * _fullSizeTop);
            //        }
            //    }
            //}

                      
  
        }


        public override void LoadContent(GameTime gameTime)
        {
            //_font = Content.Load<SpriteFont>("Inter-SemiBold"); 
            _logo = Content.Load<Texture2D>(@"Images/owlracer-logo-solo");
            _logoMathema = Content.Load<Texture2D>(@"Images/mathema-logo");
            _circle = Content.Load<Texture2D>(@"Images/Circle_down");
            _street = Content.Load<Texture2D>(@"Images/Street");
            _logoMathemaDarkOwl = Content.Load<Texture2D>(@"Images/mat-pictogram-rgb-dark");

            _background = new Texture2D(GraphicsDevice, 1, 1, false, SurfaceFormat.Color);
            _background.SetData(new[] { Color.White });

            _line = new Texture2D(GraphicsDevice, 1, 1, false, SurfaceFormat.Color);
            _line.SetData(new[] { Color.Black });

            //Trial and Error

            //if (_fullSizeLeftRight <= 2048)
            //{
            //    _font = Content.Load<SpriteFont>("Inter-SemiBold");
            //}
            //else
            //{
            //    _font = Content.Load<SpriteFont>("Inter-SemiBold-big");
            //}

            if (_fullSizeLeftRight * _fullSizeTop >= 2560 * 2048)
            {
                _font = Content.Load<SpriteFont>("Inter-SemiBold-big");
            }
            else if (_fullSizeLeftRight * _fullSizeTop >= 1920*1200)
            {
               _font = Content.Load<SpriteFont>("Inter-SemiBold");
            }
            else if (_fullSizeLeftRight * _fullSizeTop > 1280*720)
            {
                _font = Content.Load<SpriteFont>("Inter-SemiBold-small");
            }
            else
            {
                _font = Content.Load<SpriteFont>("Inter-SemiBold-very-small");
            }


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

            spriteBatch.DrawString(_font, "Available Sessions: ", new Vector2((int)(_fullSizeLeftRight * _borderFactor), (int)(_fullSizeTop * (_borderFactor + 4 * _rowSizeFactor + 2*_rowBorder))), Color.White);

            spriteBatch.DrawString(_font, "Enter Player Name: ", new Vector2((int)(_fullSizeLeftRight * _borderFactor), (int)(_fullSizeTop* _borderFactor)), Color.White);

            spriteBatch.DrawString(_font, "Enter Session Name: ", new Vector2((int)(_fullSizeLeftRight * (_borderFactor + _columnSizeFactor + _columnBorder)), (int)(_fullSizeTop * _borderFactor)), Color.White);

            spriteBatch.DrawString(_font, "Choose Mode: ", new Vector2((int)(_fullSizeLeftRight * _borderFactor), (int)(_fullSizeTop*(_borderFactor + 2 * _rowSizeFactor + _rowBorder))), Color.White);

            
            spriteBatch.DrawString(_font, "Choose Track: ", new Vector2((int)(_fullSizeLeftRight * _borderFactor ), (int)(_fullSizeTop * (_borderFactor + 12 * _rowSizeFactor + 4 * _rowBorder))), Color.White);

            //spriteBatch.DrawString(_font, "Available Sessions:", new Vector2((int)(_borderLeftRight), (int)(_borderTop * 2.5)), Color.Black);


            //spriteBatch.DrawString(_font, "Enter Player Name: ", new Vector2(_borderLeftRight, _borderTop), Color.White);

            //spriteBatch.DrawString(_font, "Choose Mode: ", new Vector2((int)(_borderLeftRight * 2.3), _borderTop), Color.White);

            //spriteBatch.DrawString(_font, "Enter Session Name: ", new Vector2((int)(_borderLeftRight * 2.3 * 2), (int)(_borderTop)), Color.Black);

            //spriteBatch.DrawString(_font, "Choose Track: ", new Vector2((int)(GraphicsDevice.Adapter.CurrentDisplayMode.Width - _track0Texture.Width / 5), _borderTop), Color.Black);



            Rectangle logoRect = new Rectangle((int)(_fullSizeLeftRight * (1-_borderFactor*0.9)), (int)(_fullSizeTop *  _borderFactor* 0.3), 
               (int)(_logo.Width*0.3*_scaleX), (int)(_logo.Height*_scaleY*0.3));
            Rectangle logoRectMathema = new Rectangle((int)(_fullSizeLeftRight * (1 - _borderFactor * 0.9)), (int)(_fullSizeTop * _borderFactor * 0.90),
               (int)(_logoMathema.Width*0.1*_scaleX), (int)(_logoMathema.Height*0.1*_scaleY));
            Rectangle logoRectCircle = new Rectangle((int)(_fullSizeLeftRight*(1-_borderFactor*1.25)), 0, (int)(_fullSizeLeftRight*_borderFactor*1.25), (int)(_fullSizeLeftRight*_borderFactor*1.25));

            Rectangle logoRectStreet1 = new Rectangle(0, 0,
                GraphicsDevice.Adapter.CurrentDisplayMode.Width, (int)(_borderTop*0.8));

            Rectangle logoRectDarkOwl = new Rectangle((int)(_fullSizeLeftRight * (_borderFactor + 2 * _columnSizeFactor + 2 * _columnBorder)), (int)(_fullSizeTop * _borderFactor), (int)(_fullSizeLeftRight * _columnSizeFactor), (int)(_fullSizeTop * (11 * _rowSizeFactor + 5 * _rowBorder)));


            //Rectangle lineRect = new Rectangle((int)(_borderLeftRight * 2.3 * 1.9), (int)(_borderTop * 0.97), 2, 100);


            spriteBatch.Draw(_street, logoRectStreet1, null, Color.White, (float)0.0, new Vector2(0, 0), SpriteEffects.None, (float)0.0);
            spriteBatch.Draw(_circle, logoRectCircle, null, Color.White, (float)0.0, new Vector2(0, 0), SpriteEffects.None, (float)0.0);
            spriteBatch.Draw(_logo, logoRect, null, Color.White,(float)0.0, new Vector2(0,0), SpriteEffects.None, (float)0.0);
            spriteBatch.DrawString(_font, "EIN PROJEKT DER", new Vector2((int)(_fullSizeLeftRight * (1 - _borderFactor * 1.05)), (int)(_fullSizeTop * _borderFactor * 0.60)), _corporateGray60);
            spriteBatch.Draw(_logoMathema, logoRectMathema, null, Color.White, (float)0.0, new Vector2(0, 0), SpriteEffects.None, (float)0.0);
            spriteBatch.Draw(_logoMathemaDarkOwl, logoRectDarkOwl, null, Color.White, (float)0.0, new Vector2(0, 0), SpriteEffects.None, (float)0.0);
            //spriteBatch.Draw(_line, lineRect, null, Color.White, (float)0.0, new Vector2(0, 0), SpriteEffects.None, (float)0.0);

            //spriteBatch.Draw(_street, logoRectStreet1, Color.White);
            //spriteBatch.Draw(_circle, logoRectCircle, Color.White);
            //spriteBatch.Draw(_logo, logoRect, Color.White);
            //spriteBatch.DrawString(_font, "EIN PROJEKT DER", new Vector2((int)(GraphicsDevice.Adapter.CurrentDisplayMode.Width - (logoRectCircle.Width / 2)), (int)(_circle.Height * 0.75 * 0.5 - _logo.Height * 0.75)), _corporateGray60);
            //spriteBatch.Draw(_logoMathema, logoRectMathema, Color.White);
            //spriteBatch.Draw(_line, lineRect, Color.White);

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

             _yLimit = (int)(_fullSizeTop * (_borderFactor + 10 * _rowSizeFactor + 4 * _rowBorder));
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
                DrawRankingText(spriteBatch, (int)((float)_fullSizeLeftRight * (_borderFactor + 0.5 * _columnSizeFactor)), (int)(_fullSizeTop * (_borderFactor + 4 * _rowSizeFactor + 4 * _rowBorder)), _carList);
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
                SessionId = mySession.Id.ToString(),
                Width = (int)((float)(_columnSizeFactor)*(float)(_fullSizeLeftRight*0.47)),
                Height = (int)((float)(_columnSizeFactor) * (float)(_fullSizeLeftRight * 0.47 * (float)buttonTexture.Height/(float)buttonTexture.Width)),
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
                SessionId = mySession.Id.ToString(),
                Height = (int)((float)(_rowBorder) * (float)(_fullSizeLeftRight)),
                Width = (int)((float)(_rowBorder) * (float)(_fullSizeLeftRight)),
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
            if (pressed.Length > 0 && _legitKeys.BinarySearch(pressed[0].ToString()) >=0)
            {
                if (pressed[0].ToString() == "Back" && button.Text.Length > 0)
                {
                    button.Text = button.Text.Remove(button.Text.Length - 1);
                } 
                else if (pressed[0].ToString().Contains('D') && pressed[0].ToString() != "D")
                {
                    string interimNumber = pressed[0].ToString();
                    interimNumber = interimNumber.Substring(1, interimNumber.Length - 1);
                    button.Text = button.Text + interimNumber;
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
            _pageButton.Text = _currentPageNumber + "/" + _totalPageNumber;
            _previousPageButton.Clicked = true;
        }
        private void nextPageButton_Click(object sender, EventArgs e)
        {
            _currentPageNumber = Math.Min(_currentPageNumber + 1, _totalPageNumber); 
            _pageButton.Text = _currentPageNumber + "/" + _totalPageNumber;
            _nextPageButton.Clicked = true;

        }

        private List<string> createLegitKeyList()
        {
            List<String> legitKeys = new List<String>();
            char[] alphabet = "AaBbCcDdEeFfGgHhIiJjKkLlMmNnOoPpQqRrSsTtUuVvWwXxYyZz".ToCharArray();
            foreach (char x in alphabet)
            {
                legitKeys.Add(x.ToString());
            }
            for(int i = 0; i < 10; i++)
            {
                string interimResult = "D" + i;
                legitKeys.Add(interimResult);
            }
            legitKeys.Add("Back");
            legitKeys.Add("Divide");
            legitKeys.Sort();
            return legitKeys;
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
            if (pos_y >= _yLimit)
            {
                xPos = pos_x_2 + (int)((_columnSizeFactor *0.5+ _columnBorder) * _fullSizeLeftRight);
            }
            
            spriteBatch.Draw(_background, new Rectangle(xPos, yPos, (int)((float)_fullSizeLeftRight*_columnSizeFactor*0.475), (int)((float)(Math.Max(numPlayers+3,4)) * _scoreLineSize*1.25 * (float)_fullSizeTop)), null, _corporateGray40, 0, new Vector2(0, 0), SpriteEffects.None, 0);
            spriteBatch.DrawString(_font, "Session: " + Game.Session.Name, new Vector2(xPos+ (int)(0.003 * (float)_fullSizeLeftRight ), yPos), Color.Black);
            yPos += (int)(_scoreLineSize * 1.25 * (float)_fullSizeTop);
            spriteBatch.DrawString(_font, "Ranking List ", new Vector2(xPos + (int)(0.003 * (float)_fullSizeLeftRight), yPos), Color.Black);
            yPos += (int)(_scoreLineSize * 1.25 * (float)_fullSizeTop);
            spriteBatch.DrawString(_font, "(active Players)", new Vector2(xPos + (int)(0.003 * (float)_fullSizeLeftRight), yPos), Color.DarkGreen);
            yPos += (int)(_scoreLineSize * 1.25 * (float)_fullSizeTop);

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

                    spriteBatch.DrawString(_font, ranking.ToString(), new Vector2(xPos + (int)(0.003 * (float)_fullSizeLeftRight), yPos), _color);
                    spriteBatch.DrawString(_font, car.Key.Name, new Vector2(xPos + (int)(0.01 * (float)_fullSizeLeftRight), yPos), _color);
                    spriteBatch.DrawString(_font, car.Value.ToString(), new Vector2(xPos + (int)(0.1 * (float)_fullSizeLeftRight), yPos), _color);
                    ranking += 1;
                    yPos += (int)(_scoreLineSize * 1.25 * (float)_fullSizeTop);
                    _color = Color.Black;
                }
            }
            else
            {
                spriteBatch.DrawString(_font, "no players", new Vector2(xPos + (int)(0.003 * (float)_fullSizeLeftRight), yPos), Color.Black);
            }
        }
    }
}