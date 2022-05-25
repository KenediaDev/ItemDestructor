using Blish_HUD;
using Blish_HUD.Content;
using Blish_HUD.Controls;
using Blish_HUD.Controls.Extern;
using Blish_HUD.Modules;
using Blish_HUD.Modules.Managers;
using Blish_HUD.Settings;
using Gw2Sharp.WebApi.V2.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Point = Microsoft.Xna.Framework.Point;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

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

        [DllImport("user32")]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
        public const uint WM_COMMAND = 0x0111;
        public const uint WM_PASTE = 0x0302;
        [DllImport("user32")]
        public static extern bool PostMessage(IntPtr hWnd, uint Msg, int wParam, int lParam);

        public SettingEntry<Blish_HUD.Input.KeyBinding> CancelKey;
        public SettingEntry<Blish_HUD.Input.KeyBinding> ToggleModule;
        public SettingEntry<Blish_HUD.Input.KeyBinding> ReloadKey;
        public SettingEntry<bool> ShowCornerIcon;

        public string CultureString;
        public TextureManager TextureManager;
        public Ticks Ticks = new Ticks();

        public WindowBase2 MainWindow;
        private CursorSpinner CursorIcon;
        private DeleteIndicator DeleteIndicator;
        private CornerIcon cornerIcon;
        private InputService Input;

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
        private ButtonState HotKeyState;

        public string Instruction = "Shift + Click on an Item!";
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

            ToggleModule = settings.DefineSetting(nameof(ToggleModule),
                                                      new Blish_HUD.Input.KeyBinding(ModifierKeys.Ctrl, Keys.Delete),
                                                      () => "Toggle Module",
                                                      () => "Enables/Disables the modules destruction mode.");

            ShowCornerIcon = settings.DefineSetting(nameof(ShowCornerIcon),
                                                      true,
                                                      () => "Show Corner Icon",
                                                      () => "Show / Hide the Corner Icon of this module.");

            var internal_settings = settings.AddSubCollection("Internal Settings", false);

            ReloadKey = internal_settings.DefineSetting(nameof(ReloadKey),
                                                      new Blish_HUD.Input.KeyBinding(Keys.RightControl),
                                                      () => "Reload Button",
                                                      () => "");

            CancelKey = internal_settings.DefineSetting(nameof(CancelKey),
                                                      new Blish_HUD.Input.KeyBinding(Keys.Escape),
                                                      () => "Cancel Button",
                                                      () => "");
        }

        protected override void Initialize()
        {
            CultureString = ItemDestructor.getCultureString();
            Logger.Info("Starting Builds Manager v." + Version.BaseVersion());

            ReloadKey.Value.Enabled = true;
            ReloadKey.Value.Activated += ReloadKey_Activated;

            CancelKey.Value.Enabled = true;
            CancelKey.Value.Activated += CancelKey_Activated;

            Input = new InputService();
            DataLoaded = false;

            ToggleModule.Value.Enabled = true;
            ToggleModule.Value.Activated += ToggleModule_Activated;

            ModKeyMapping = new VirtualKeyShort[5];
            ModKeyMapping[(int)ModifierKeys.Ctrl] = VirtualKeyShort.CONTROL;
            ModKeyMapping[(int)ModifierKeys.Alt] = VirtualKeyShort.MENU;
            ModKeyMapping[(int)ModifierKeys.Shift] = VirtualKeyShort.LSHIFT;

            ShowCornerIcon.SettingChanged += ShowCornerIcon_SettingChanged;
        }

        private void ToggleModule_Activated(object sender, EventArgs e)
        {
            ScreenNotification.ShowNotification(string.Format("Item Destruction Mode: {0}", ModuleActive ? "Disabled" : "Enabled"), ScreenNotification.NotificationType.Warning);
            ModuleActive = !ModuleActive;
            CursorIcon.Visible = ModuleActive;
        }

        private void ShowCornerIcon_SettingChanged(object sender, ValueChangedEventArgs<bool> e)
        {
            if (cornerIcon != null) cornerIcon.Visible = e.NewValue;
        }

        private void CancelKey_Activated(object sender, EventArgs e)
        {
            DeletePrepared = false;
        }

        private void ReloadKey_Activated(object sender, EventArgs e)
        {
            ScreenNotification.ShowNotification("Rebuilding the UI", ScreenNotification.NotificationType.Warning);
            MainWindow?.Dispose();
            CreateUI();
            MainWindow?.ToggleWindow();
        }

        protected override async Task LoadAsync()
        {
        }

        protected override void OnModuleLoaded(EventArgs e)
        {
            TextureManager = new TextureManager();

            cornerIcon = new CornerIcon()
            {
                Icon = TextureManager.getIcon(_Icons.Stop_White),
                HoverIcon = TextureManager.getIcon(_Icons.Stop_Highlight),
                BasicTooltipText = $"{Name}",
                Parent = GameService.Graphics.SpriteScreen,
                Visible = ShowCornerIcon.Value,
            };

            CursorIcon = new CursorSpinner()
            {
                Parent = GameService.Graphics.SpriteScreen,
                Background = TextureManager.getBackground(_Backgrounds.Tooltip),
                Size = new Point(240, 50),
                Visible = false,
            };

            DeleteIndicator = new DeleteIndicator()
            {
                Parent = GameService.Graphics.SpriteScreen,
                Size = new Point(32, 32),
                Visible = false,
                Texture = TextureManager.getControlTexture(_Controls.Delete),
                ClipsBounds = false,
            };

            cornerIcon.Click += CornerIcon_Click;
            DataLoaded_Event += ItemDestructor_DataLoaded_Event;

            // Base handler must be called
            base.OnModuleLoaded(e);

            LoadData();
        }

        private void ItemDestructor_DataLoaded_Event(object sender, EventArgs e)
        {
            CreateUI();
        }

        private void CornerIcon_Click(object sender, Blish_HUD.Input.MouseEventArgs e)
        {
            if (MainWindow != null) MainWindow.ToggleWindow();
        }

        protected override void Update(GameTime gameTime)
        {
            Ticks.global += gameTime.ElapsedGameTime.TotalMilliseconds;

            if (Ticks.global > 25)
            {
                Ticks.global = 0;

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
                            Instruction = "Throw the marked item out!";
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
                            Instruction = "Shift + Click on an Item!";
                        }
                    }

                    MouseState = mouse.LeftButton;
                }
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

            cornerIcon?.Dispose();
            cornerIcon.Click -= CornerIcon_Click;

            TextureManager?.Dispose();
            TextureManager = null;

            ReloadKey.Value.Enabled = false;
            ReloadKey.Value.Activated -= ReloadKey_Activated;

            DataLoaded_Event -= ItemDestructor_DataLoaded_Event;
            OverlayService.Overlay.UserLocale.SettingChanged -= UserLocale_SettingChanged;

            DataLoaded = false;
            ModuleInstance = null;
        }

        public static string getCultureString()
        {
            var culture = "en-EN";
            switch (OverlayService.Overlay.UserLocale.Value)
            {
                case Gw2Sharp.WebApi.Locale.French:
                    culture = "fr-FR";
                    break;

                case Gw2Sharp.WebApi.Locale.Spanish:
                    culture = "es-ES";
                    break;

                case Gw2Sharp.WebApi.Locale.German:
                    culture = "de-DE";
                    break;

                default:
                    culture = "en-EN";
                    break;
            }
            return culture;
        }

        public async Task Fetch_APIData(bool force = false)
        {
        }

        async Task LoadData()
        {
            CultureString = ItemDestructor.getCultureString();
            await Fetch_APIData();

            OverlayService.Overlay.UserLocale.SettingChanged += UserLocale_SettingChanged;
        }

        private async void UserLocale_SettingChanged(object sender, ValueChangedEventArgs<Gw2Sharp.WebApi.Locale> e)
        {
            OverlayService.Overlay.UserLocale.SettingChanged -= UserLocale_SettingChanged;
            await LoadData();

            OnLanguageChanged(null, null);
        }

        private void CreateUI()
        {
            //MainWindow.ToggleWindow();
        }
    }
}