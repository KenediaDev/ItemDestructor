using Blish_HUD;
using Blish_HUD.Content;
using Blish_HUD.Controls;
using Blish_HUD.Controls.Extern;
using Blish_HUD.Modules;
using Blish_HUD.Modules.Managers;
using Blish_HUD.Settings;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.ComponentModel.Composition;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Point = Microsoft.Xna.Framework.Point;

namespace Kenedia.Modules.ItemDestructor
{
    [Export(typeof(Module))]
    public class ItemDestructor : Module
    {
        internal static ItemDestructor ModuleInstance;
        public static readonly Logger Logger = Logger.GetLogger<ItemDestructor>();

        #region Service Managers

        internal SettingsManager SettingsManager => this.ModuleParameters.SettingsManager;
        internal ContentsManager ContentsManager => this.ModuleParameters.ContentsManager;
        internal DirectoriesManager DirectoriesManager => this.ModuleParameters.DirectoriesManager;
        internal Gw2ApiManager Gw2ApiManager => this.ModuleParameters.Gw2ApiManager;

        #endregion

        [ImportingConstructor]
        public ItemDestructor([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(moduleParameters)
        {
            ModuleInstance = this;
        }

        public SettingEntry<Blish_HUD.Input.KeyBinding> Cancel_Key;
        public SettingEntry<Blish_HUD.Input.KeyBinding> ToggleModule_Key;
        public SettingEntry<bool> ShowCornerIcon;

        public string CultureString;
        public TextureManager TextureManager;
        public Ticks Ticks = new Ticks();

        public WindowBase2 MainWindow;
        private CursorSpinner CursorIcon;
        private DeleteIndicator DeleteIndicator;
        private CornerIcon cornerIcon;
        private LoadingSpinner LoadingSpinner;

        public static VirtualKeyShort[] ModKeyMapping;

        private bool _DataLoaded;
        public bool FetchingAPI;
        public bool DataLoaded
        {
            get => _DataLoaded;
            set
            {
                _DataLoaded = value;
                if (value) ModuleInstance.OnDataLoaded();
            }
        }

        private ButtonState MouseState;
        private Point MousePos = Point.Zero;
        private Point ItemPos = Point.Zero;

        public string Instruction = Strings.common.ClickItem;
        private bool ModuleActive;
        private bool DeleteRunning;
        private bool DeletePrepared;

        public event EventHandler DataLoaded_Event;
        void OnDataLoaded()
        {
            this.DataLoaded_Event?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler LanguageChanged;
        public void OnLanguageChanged(object sender, EventArgs e)
        {
            this.LanguageChanged?.Invoke(this, EventArgs.Empty);
        }

        protected override void DefineSettings(SettingCollection settings)
        {
            ToggleModule_Key = settings.DefineSetting(nameof(ToggleModule_Key),
                                                      new Blish_HUD.Input.KeyBinding(ModifierKeys.Ctrl, Keys.Delete),
                                                      () => string.Format(Strings.common.Toggle, Name));

            ShowCornerIcon = settings.DefineSetting(nameof(ShowCornerIcon),
                                                      true,
                                                      () => Strings.common.ShowCorner_Name,
                                                      () => string.Format(Strings.common.ShowCorner_Tooltip, Name));

            var internal_settings = settings.AddSubCollection("Internal Settings", false);
            Cancel_Key = internal_settings.DefineSetting(nameof(Cancel_Key), new Blish_HUD.Input.KeyBinding(Keys.Escape));
        }

        protected override void Initialize()
        {
            Logger.Info("Starting Builds Manager v." + Version.BaseVersion());

            Cancel_Key.Value.Enabled = true;
            Cancel_Key.Value.Activated += CancelKey_Activated;

            DataLoaded = false;

            ToggleModule_Key.Value.Enabled = true;
            ToggleModule_Key.Value.Activated += ToggleModule;

            ModKeyMapping = new VirtualKeyShort[5];
            ModKeyMapping[(int)ModifierKeys.Ctrl] = VirtualKeyShort.CONTROL;
            ModKeyMapping[(int)ModifierKeys.Alt] = VirtualKeyShort.MENU;
            ModKeyMapping[(int)ModifierKeys.Shift] = VirtualKeyShort.LSHIFT;

            ShowCornerIcon.SettingChanged += ShowCornerIcon_SettingChanged;
            OverlayService.Overlay.UserLocale.SettingChanged += UserLocale_SettingChanged;
        }

        private void ToggleModule(object sender, EventArgs e)
        {
            ScreenNotification.ShowNotification(string.Format(Strings.common.RunStateChange, Name, ModuleActive ? Strings.common.Deactivated : Strings.common.Activated), ScreenNotification.NotificationType.Warning);
            ModuleActive = !ModuleActive;
            CursorIcon.Visible = ModuleActive;
            LoadingSpinner.Visible = cornerIcon?.Visible == true && ModuleActive;
        }

        private void ShowCornerIcon_SettingChanged(object sender, ValueChangedEventArgs<bool> e)
        {
            if (cornerIcon != null) cornerIcon.Visible = e.NewValue;
            if (LoadingSpinner != null) LoadingSpinner.Visible = cornerIcon?.Visible == true && ModuleActive;
        }

        private void CancelKey_Activated(object sender, EventArgs e)
        {
            DeletePrepared = false;
        }

        protected override async Task LoadAsync()
        {
        }

        protected override void OnModuleLoaded(EventArgs e)
        {
            TextureManager = new TextureManager();

            cornerIcon = new CornerIcon()
            {
                Icon = TextureManager.getIcon(_Icons.Delete),
                HoverIcon = TextureManager.getIcon(_Icons.Delete_HoveredWhite),
                BasicTooltipText = string.Format(Strings.common.Toggle, $"{Name}"),
                Parent = GameService.Graphics.SpriteScreen,
                Visible = ShowCornerIcon.Value,
            };
            cornerIcon.Size = cornerIcon.Size.Scale(0.8);

            LoadingSpinner = new LoadingSpinner()
            {
                Parent = GameService.Graphics.SpriteScreen,
                Size = cornerIcon.Size,
                Visible = false,
                Location = new Point(cornerIcon.Location.X, cornerIcon.Location.Y + cornerIcon.Height + 5),
        };

            CursorIcon = new CursorSpinner()
            {
                Parent = GameService.Graphics.SpriteScreen,
                Background = TextureManager.getBackground(_Backgrounds.Tooltip),
                Visible = false,
            };

            string[] instructions = {
                Strings.common.ClickItem,
                Strings.common.ThrowItem
            };
            var Font = GameService.Content.DefaultFont14;
            int width = 0;
            foreach (string s in instructions)
            {
                width = Math.Max((int)Font.MeasureString(s).Width, width);
            }

            CursorIcon.Size = new Point(50 + width + 5, 50);

            DeleteIndicator = new DeleteIndicator()
            {
                Parent = GameService.Graphics.SpriteScreen,
                Size = new Point(32, 32),
                Visible = false,
                Texture = TextureManager.getControlTexture(_Controls.Delete),
                ClipsBounds = false,
            };

            cornerIcon.Click += ToggleModule;
            cornerIcon.Moved += CornerIcon_Moved;

            // Base handler must be called
            base.OnModuleLoaded(e);
        }

        private void CornerIcon_Moved(object sender, MovedEventArgs e)
        {
            LoadingSpinner.Location = new Point(cornerIcon.Location.X, cornerIcon.Location.Y + cornerIcon.Height + 5);
        }

        protected override void Update(GameTime gameTime)
        {
            Ticks.global += gameTime.ElapsedGameTime.TotalMilliseconds;

            if (Ticks.global > 5)
            {
                Ticks.global = 0;
            }

            if (GameIntegrationService.GameIntegration.Gw2Instance.Gw2HasFocus && ModuleActive && !DeleteRunning)
            {
                var mouse = Mouse.GetState();
                var keyboard = Keyboard.GetState();

                var Clicked = MouseState == ButtonState.Pressed && mouse.LeftButton == ButtonState.Released;
                if (mouse.LeftButton == ButtonState.Pressed && MouseState == ButtonState.Released)
                {
                    if (ItemPos.Distance2D(mouse.Position) < 50)
                    {
                        MousePos = MousePos == Point.Zero ? mouse.Position : MousePos;
                    }
                    else
                    {
                        DeletePrepared = false;
                    }
                }

                if (Clicked)
                {
                    DeleteIndicator.Visible = false;

                    if (keyboard.IsKeyDown(Keys.LeftShift))
                    {
                        ItemPos = mouse.Position;
                        DeleteIndicator.Visible = true;
                        Instruction = Strings.common.ThrowItem;
                        Copy();
                    }
                    else if (DeletePrepared)
                    {
                        if (MousePos.Distance2D(mouse.Position) > 100)
                        {
                            Paste();
                        }

                        DeletePrepared = false;
                        DeleteIndicator.Visible = false;
                    }
                    else
                    {
                        MousePos = Point.Zero;
                        Instruction = Strings.common.ClickItem;
                    }
                }

                MouseState = mouse.LeftButton;
            }
        }

        public async void Paste()
        {
            await Task.Run(() =>
             {
                 Blish_HUD.Controls.Intern.Keyboard.Press(VirtualKeyShort.LCONTROL, true);
                 Blish_HUD.Controls.Intern.Keyboard.Stroke(VirtualKeyShort.KEY_A, true);
                 Thread.Sleep(5);
                 Blish_HUD.Controls.Intern.Keyboard.Stroke(VirtualKeyShort.KEY_V, true);
                 Thread.Sleep(5);
                 Blish_HUD.Controls.Intern.Keyboard.Release(VirtualKeyShort.LCONTROL, true);
                 DeletePrepared = false;
             });
        }

        public async void Copy()
        {
            DeleteRunning = true;
            await Task.Run(() =>
            {
                Blish_HUD.Controls.Intern.Keyboard.Press(VirtualKeyShort.LCONTROL, true);
                Blish_HUD.Controls.Intern.Keyboard.Stroke(VirtualKeyShort.KEY_A, true);
                Thread.Sleep(5);
                Blish_HUD.Controls.Intern.Keyboard.Release(VirtualKeyShort.LCONTROL, true);


                Blish_HUD.Controls.Intern.Keyboard.Press(VirtualKeyShort.LCONTROL, true);
                Blish_HUD.Controls.Intern.Keyboard.Stroke(VirtualKeyShort.KEY_C, true);
                Thread.Sleep(5);
                Blish_HUD.Controls.Intern.Keyboard.Release(VirtualKeyShort.LCONTROL, true);

                Blish_HUD.Controls.Intern.Keyboard.Release(VirtualKeyShort.LSHIFT, true);
                Thread.Sleep(5);
                Blish_HUD.Controls.Intern.Keyboard.Stroke(VirtualKeyShort.BACK, true);
                Blish_HUD.Controls.Intern.Keyboard.Stroke(VirtualKeyShort.RETURN, true);
            });

            await Task.Run(() =>
            {
                var text = ClipboardUtil.WindowsClipboardService.GetTextAsync()?.Result;
                text = text.Length > 3 ? text.Substring(1, text.Length - 2) : "";

                if (text.Length > 0) ClipboardUtil.WindowsClipboardService.SetTextAsync(text);
                DeletePrepared = true;
            });

            DeleteRunning = false;
        }

        protected override void Unload()
        {
            MainWindow?.Dispose();

            CursorIcon?.Dispose();
            DeleteIndicator?.Dispose();

            LoadingSpinner?.Dispose();

            cornerIcon.Click -= ToggleModule;
            cornerIcon.Moved -= CornerIcon_Moved;
            cornerIcon?.Dispose();

            ToggleModule_Key.Value.Activated -= ToggleModule;

            TextureManager?.Dispose();
            TextureManager = null;

            OverlayService.Overlay.UserLocale.SettingChanged -= UserLocale_SettingChanged;

            DataLoaded = false;
            ModuleInstance = null;
        }

        private async void UserLocale_SettingChanged(object sender, ValueChangedEventArgs<Gw2Sharp.WebApi.Locale> e)
        {
            OnLanguageChanged(null, null);

            Instruction = Strings.common.ClickItem;
            cornerIcon.BasicTooltipText = string.Format(Strings.common.Toggle, $"{Name}");


            string[] instructions = {
                Strings.common.ClickItem,
                Strings.common.ThrowItem
            };

            var Font = GameService.Content.DefaultFont14;
            int width = 0;
            foreach (string s in instructions)
            {
                width = Math.Max((int)Font.MeasureString(s).Width, width);
            }

            CursorIcon.Size = new Point(50 + width + 5, 50);
        }
    }
}