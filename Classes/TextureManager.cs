using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using Microsoft.Xna.Framework.Graphics;
using Blish_HUD;
using Blish_HUD.Controls;
using Microsoft.Xna.Framework;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using Color = Microsoft.Xna.Framework.Color;
using Gw2Sharp.ChatLinks;
namespace Kenedia.Modules.ItemDestructor
{
    public enum _Controls
    {
        GlidingBar,
        GlidingFill,
        GlidingFill_Gray,
        TabActive,
        SpecHighlight,
        Line,
        Land,
        Water,
        EliteFrame,
        PlaceHolder_Traitline,
        SpecFrame,
        SpecSideSelector,
        SpecSideSelector_Hovered,
        SkillSelector,
        SkillSelector_Hovered,
        NoWaterTexture,
        TabBorderLeft,
        TabBorderRight,
        TabBar_FadeIn,
        TabBar_Line,
        Selector,
        AddButton,
        ResetButton,
        ResetButton_Hovered,
        Template_Border,
        Delete,
        Delete_Hovered,
        Clear,
        Add,
        Add_Hovered,
        Copy,
        Copy_Hovered,
        Import,
        Import_Hovered,
    }
    public enum _Icons
    {
        Bug,
        Refresh,
        Template,
        Template_White,
        Helmet,
        Helmet_White,
        Cog,
        Cog_White,
        Undo,
        Undo_White,
        Checkmark_White,
        Checkmark_Color,
        Checkmark_Highlight,
        Stop_White,
        Stop_Color,
        Stop_Highlight,
        Search,
        Search_Highlight,
        Edit_Feather,
        Edit_Feather_Highlight,
        Edit_Feather_Pressed,
        Mouse,
        Lock_Locked,
        SingleSpinner,
    }
    public enum _Emblems
    {
        SwordAndShield,
        QuestionMark,
    }
    public enum _Backgrounds
    {
        MainWindow,
        BlueishMainWindow,
        Tooltip,
    }

    public class TextureManager : IDisposable
    {
        public List<Texture2D> _Backgrounds = new List<Texture2D>();
        public List<Texture2D> _Icons = new List<Texture2D>();
        public List<Texture2D> _Emblems = new List<Texture2D>();
        public List<Texture2D> _Controls = new List<Texture2D>();

        private bool disposed = false;
        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;

                _Backgrounds?.DisposeAll();
                _Icons?.DisposeAll();
                _Emblems?.DisposeAll();
                _Controls?.DisposeAll();
            }
        }


        public TextureManager()
        {
            var ContentsManager = ItemDestructor.ModuleInstance.ContentsManager;

            var values = Enum.GetValues(typeof(_Backgrounds));
            _Backgrounds = new List<Texture2D>(new Texture2D[values.Cast<int>().Max() + 1]);
            foreach (_Backgrounds num in values)
            {
                var texture = ContentsManager.GetTexture(@"textures\backgrounds\" + (int)num + ".png");
                _Backgrounds.Insert((int)num, texture);
            }

            values = Enum.GetValues(typeof(_Icons));
            _Icons = new List<Texture2D>(new Texture2D[values.Cast<int>().Max() + 1]);
            foreach (_Icons num in values)
            {
                var texture = ContentsManager.GetTexture(@"textures\icons\" + (int)num + ".png");
                _Icons.Insert((int)num, texture);
            }

            values = Enum.GetValues(typeof(_Emblems));
            _Emblems = new List<Texture2D>(new Texture2D[values.Cast<int>().Max() + 1]);
            foreach (_Emblems num in values)
            {
                var texture = ContentsManager.GetTexture(@"textures\emblems\" + (int)num + ".png");
                _Emblems.Insert((int)num, texture);
            }

            values = Enum.GetValues(typeof(_Controls));
            _Controls = new List<Texture2D>(new Texture2D[values.Cast<int>().Max() + 1]);
            foreach (_Controls num in values)
            {
                var texture = ContentsManager.GetTexture(@"textures\controls\" + (int)num + ".png");
                _Controls.Insert((int)num, texture);
            }

        }

        public Texture2D getBackground(_Backgrounds background)
        {
            var index = (int)background;

            if (index < _Backgrounds.Count && _Backgrounds[index] != null) return _Backgrounds[index];
            return _Icons[0];
        }

        public Texture2D getIcon(_Icons icon)
        {
            var index = (int)icon;

            if (index < _Icons.Count && _Icons[index] != null) return _Icons[index];
            return _Icons[0];
        }

        public Texture2D getEmblem(_Emblems emblem)
        {
            var index = (int)emblem;
            if (index < _Emblems.Count && _Emblems[index] != null) return _Emblems[index];
            return _Icons[0];
        }

        public Texture2D getControlTexture(_Controls control)
        {
            var index = (int)control;
            if (index < _Controls.Count && _Controls[index] != null) return _Controls[index];
            return _Icons[0];
        }
    }
}
