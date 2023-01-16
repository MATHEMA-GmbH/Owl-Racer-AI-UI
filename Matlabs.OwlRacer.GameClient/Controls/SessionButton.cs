using DocumentFormat.OpenXml.Office.CustomUI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace Matlabs.OwlRacer.GameClient.Controls
{
    public class SessionButton : Component
    {
        private MouseState _currentMouse;

        private SpriteFont _font;

        private bool _isHovering;

        private MouseState _previousMouse;

        private Texture2D _texture;

        public event EventHandler Click;

        public bool Clicked { get; set; }

        public Color PenColour { get; set; }

        public Vector2 Position { get; set; }

        public int NumClicked { get; set; }

        public int Width { get; set; }
        public int Height { get; set; }

        //Corporate Colors
        //Primary
        private Color _corporateRed = new Color(197, 0, 62);

        //Gray
        private Color _corporateGray20 = new Color(217, 217, 214);
        private Color _corporateGray40 = new Color(187, 188, 188);
        private Color _corporateGray60 = new Color(136, 139, 141);
        private Color _corporateGray80 = new Color(83, 86, 90);

        //Secondary
        private Color _corporateYellow = new Color(245, 212, 16);
        private Color _corporateBlue = new Color(55, 114, 182);
        private Color _corporateGreen = new Color(44, 154, 117);
        private Color _corporateMagenta = new Color(163, 0, 105);

        public Rectangle Rectangle
        {
            get
            {
                return new Rectangle((int)Position.X, (int)Position.Y, Width, Height);
            }
        }

        public string Text { get; set; }
        public string SessionId { get; set; }

        public SessionButton(Texture2D texture, SpriteFont font)
        {
            _texture = texture;

            _font = font;

            PenColour = Color.Black;

            NumClicked = 0;
        }

        public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            var colour = _corporateGray20;

            if (Clicked && !_isHovering)
                colour = _corporateBlue;

            else if (_isHovering && !Clicked)
                colour = _corporateGray80;

            else if (Clicked && _isHovering)
                colour = _corporateGreen;


            spriteBatch.Draw(_texture, Rectangle, colour);

            if (!string.IsNullOrEmpty(Text))
            {
                var x = (Rectangle.X + (Rectangle.Width / 2)) - (_font.MeasureString(Text).X / 2);
                var y = (Rectangle.Y + (Rectangle.Height / 2)) - (_font.MeasureString(Text).Y / 2);

                spriteBatch.DrawString(_font, Text, new Vector2(x, y), PenColour);
            }
        }

        public override void Update(GameTime gameTime)
        {
            _previousMouse = _currentMouse;
            _currentMouse = Mouse.GetState();

            var mouseRectangle = new Rectangle(_currentMouse.X, _currentMouse.Y, 1, 1);

            _isHovering = false;

            if (mouseRectangle.Intersects(Rectangle))
            {
                _isHovering = true;

                if (_currentMouse.LeftButton == ButtonState.Released && _previousMouse.LeftButton == ButtonState.Pressed)
                {
                    Click?.Invoke(this, new EventArgs());
                }
            }
        }
    }
}