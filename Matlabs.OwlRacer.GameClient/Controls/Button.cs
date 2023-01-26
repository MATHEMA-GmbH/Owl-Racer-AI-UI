using DocumentFormat.OpenXml.Office.CustomUI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace Matlabs.OwlRacer.GameClient.Controls
{
    public class Button : Component
    {
        private MouseState _currentMouse;

        private SpriteFont _font;

        private bool _isHovering;

        private MouseState _previousMouse;

        private Texture2D _texture;

        private float scale;

        public event EventHandler Click;

        public bool Clicked { get; set; }

        public Color PenColour { get; set; }

        public Vector2 Position { get; set; }

        public int Width { get; set; }
        public int Height { get; set; }

        public Color HoverColor { get; set; }
        public Color ButtonColor { get; set; }

        //Corporate Colors
        //Primary
        private Color _corporateRed = new Color(197, 0, 62);

        //Grey
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

        public Button(Texture2D texture, SpriteFont font, float scale)
        {
            _texture = texture;

            _font = font;

            PenColour = Color.Black;
            Width = _texture.Width;
            Height = _texture.Height;
            this.scale = scale;
        }

        public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            var colour = Color.White;

            if (_isHovering && !Clicked)
            {
                if (HoverColor.R == 0 && HoverColor.G == 0 && HoverColor.B == 0)
                {
                    colour = _corporateGray80;
                }
                else
                {
                    colour = HoverColor;
                }
            }
                
            else if (Clicked)
                colour = _corporateBlue;

            else
            {
                if(ButtonColor.R == 0 && ButtonColor.G == 0 && ButtonColor.B == 0)
                {
                    colour = _corporateGray20;
                }
                else
                {
                    colour = ButtonColor;
                }
            }

            spriteBatch.Draw(_texture, Rectangle, colour);

            if (!string.IsNullOrEmpty(Text))
            {
                var x = (Rectangle.X + (Rectangle.Width / 2)) - (_font.MeasureString(Text).X*scale / 2);
                var y = (Rectangle.Y + (Rectangle.Height / 2)) - (_font.MeasureString(Text).Y*scale / 2);

                spriteBatch.DrawString(_font, Text, new Vector2(x, y), PenColour, (float)0.0, new Vector2(0, 0), scale, SpriteEffects.None, (float)0.0);
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