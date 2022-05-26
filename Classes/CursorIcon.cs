using System;
using Blish_HUD;
using Blish_HUD.Controls;
using Microsoft.Xna.Framework;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using Color = Microsoft.Xna.Framework.Color;
using Microsoft.Xna.Framework.Graphics;

namespace Kenedia.Modules.ItemDestructor
{
    public class CursorIcon : Control
    {
        public Texture2D Texture;
        public CursorIcon()
        {
            ClipsBounds = false;

        }
        protected override void Paint(SpriteBatch spriteBatch, Rectangle bounds)
        {
            if (Texture != null && Parent != null)
            {
                spriteBatch.DrawOnCtrl(Parent,
                                        Texture,
                                        new Rectangle(Input.Mouse.Position.X + Size.Y, Input.Mouse.Position.Y + Size.Y, Size.X, Size.Y),
                                        Texture.Bounds,
                                        Color.White,
                                        0f,
                                        default);

            }
        }

        protected override void DisposeControl()
        {
            base.DisposeControl();
            Texture?.Dispose();
        }
    }

    public class CursorSpinner : Container
    {
        LoadingSpinner LoadingSpinner;
        public Texture2D Background;
        public CursorSpinner()
        {
            //ClipsBounds = false;
            LoadingSpinner = new LoadingSpinner()
            {
                Size = new Point(40, 40),
                Parent = this,
                Location = new Point(5, 5)
            };
        }

        public override void UpdateContainer(GameTime gameTime)
        {
            base.UpdateContainer(gameTime);

            Location = Input.Mouse.Position.Add(new Point(15, 15));
        }

        public override void PaintBeforeChildren(SpriteBatch spriteBatch, Rectangle bounds)
        {
            base.PaintBeforeChildren(spriteBatch, bounds);

            if(Background != null)
            {

                spriteBatch.DrawOnCtrl(this,
                                        Background,
                                        bounds,
                                        bounds,
                                       Color.White,
                                        0f,
                                        default);

                var color = Color.Black;
                //Top
                spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, new Rectangle(bounds.Left, bounds.Top, bounds.Width, 2), Rectangle.Empty, color * 0.5f);
                spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, new Rectangle(bounds.Left, bounds.Top, bounds.Width, 1), Rectangle.Empty, color * 0.6f);

                //Bottom
                spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, new Rectangle(bounds.Left, bounds.Bottom - 2, bounds.Width, 2), Rectangle.Empty, color * 0.5f);
                spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, new Rectangle(bounds.Left, bounds.Bottom - 1, bounds.Width, 1), Rectangle.Empty, color * 0.6f);

                //Left
                spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, new Rectangle(bounds.Left, bounds.Top, 2, bounds.Height), Rectangle.Empty, color * 0.5f);
                spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, new Rectangle(bounds.Left, bounds.Top, 1, bounds.Height), Rectangle.Empty, color * 0.6f);

                //Right
                spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, new Rectangle(bounds.Right - 2, bounds.Top, 2, bounds.Height), Rectangle.Empty, color * 0.5f);
                spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, new Rectangle(bounds.Right - 1, bounds.Top, 1, bounds.Height), Rectangle.Empty, color * 0.6f);

                var Font = GameService.Content.DefaultFont14;

                spriteBatch.DrawStringOnCtrl(this,
                                       $"{ItemDestructor.ModuleInstance.Name}",
                                       GameService.Content.DefaultFont14,
                                       new Rectangle(50, 5, bounds.Width - 55, bounds.Height - 10 - Font.LineHeight),
                                       Color.Orange,
                                       false,
                                       HorizontalAlignment.Left,
                                       VerticalAlignment.Middle
                                       );

                spriteBatch.DrawStringOnCtrl(this,
                                       ItemDestructor.ModuleInstance.Instruction,
                                       GameService.Content.DefaultFont14,
                                       new Rectangle(50, 5 + Font.LineHeight, bounds.Width - 55, bounds.Height - 10 - Font.LineHeight),
                                       Color.White,
                                       false,
                                       HorizontalAlignment.Left,
                                       VerticalAlignment.Middle
                                       );
            }

        }

        protected override void DisposeControl()
        {
            base.DisposeControl();

            LoadingSpinner?.Dispose();
            Background?.Dispose();
        }
    }

    public class DeleteIndicator : Control
    {
        public Texture2D Texture;
        public DeleteIndicator()
        {
            ClipsBounds = false;

        }
        protected override void Paint(SpriteBatch spriteBatch, Rectangle bounds)
        {
            if (Texture != null && Parent != null)
            {
                BackgroundColor = Color.Magenta;
                spriteBatch.DrawOnCtrl(Parent,
                                        Texture,
                                        new Rectangle(Location, Size),
                                        Texture.Bounds,
                                        Color.White,
                                        0f,
                                        default);

            }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            Location = Input.Mouse.Position.Add(new Point(5,5));
        }

        protected override void DisposeControl()
        {
            base.DisposeControl();
            Texture?.Dispose();
        }
    }
}
