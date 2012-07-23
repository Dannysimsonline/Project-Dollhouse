using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using System.Drawing;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Win32;
using SimsLib.FAR3;
using TSOClient.ThreeD;
using TSOClient.LUI;
using LogThis;
using Un4seen.Bass;

namespace TSOClient
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        public ScreenManager ScreenMgr;
        public SceneManager SceneMgr;

        private Dictionary<int, string> m_TextDict = new Dictionary<int, string>();

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            RegistryKey softwareKey = Registry.LocalMachine.OpenSubKey("SOFTWARE");
            if (Array.Exists(softwareKey.GetSubKeyNames(), delegate(string s) { return s.CompareTo("Maxis") == 0; }))
            {
                RegistryKey maxisKey = softwareKey.OpenSubKey("Maxis");
                if (Array.Exists(maxisKey.GetSubKeyNames(), delegate(string s) { return s.CompareTo("The Sims Online") == 0; }))
                {
                    RegistryKey tsoKey = maxisKey.OpenSubKey("The Sims Online");
                    string installDir = (string)tsoKey.GetValue("InstallDir");
                    installDir += "TSOClient\\";
                    GlobalSettings.Default.StartupPath = installDir;
                }
                else
                    MessageBox.Show("Error TSO was not found on your system.");
            }
            else
                MessageBox.Show("Error: No Maxis products were found on your system.");

            BassNet.Registration("afr088@hotmail.com", "2X3163018312422");
            Bass.BASS_Init(-1, 8000, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero, System.Guid.Empty);

            //GraphicsDevice.VertexDeclaration = new VertexDeclaration(GraphicsDevice, VertexPositionNormalTexture.VertexElements);
            GraphicsDevice.RasterizerState = RasterizerState.CullNone;

            graphics.PreferredBackBufferWidth = 1024;
            graphics.PreferredBackBufferHeight = 768;
            graphics.PreferMultiSampling = true;

            //800 * 600 is the default resolution. Since all resolutions are powers of 2, just scale using
            //the width (because the height would end up with the same scalefactor).
            GlobalSettings.Default.ScaleFactor = (int)Math.Round((double)(GlobalSettings.Default.GraphicsWidth / 800));

            graphics.ApplyChanges();

            this.IsMouseVisible = true;

            //Might want to reconsider this...
            this.IsFixedTimeStep = false;
            graphics.SynchronizeWithVerticalRetrace = false;

            ScreenMgr = new ScreenManager(this, Content.Load<SpriteFont>("ComicSans"), 
                Content.Load<SpriteFont>("ComicSansSmall"));
            SceneMgr = new SceneManager(this);

            Log.UseSensibleDefaults();

            //Make the screenmanager, scenemanager and the startup path globally available to all Lua scripts.
            LuaInterfaceManager.ExportObject("ScreenManager", ScreenMgr);
            LuaInterfaceManager.ExportObject("ThreeDManager", SceneMgr);
            LuaInterfaceManager.ExportObject("StartupPath", GlobalSettings.Default.StartupPath);

            //Read settings...
            LuaFunctions.ReadSettings("gamedata\\settings\\settings.lua");

            LoadStrings();
            ScreenMgr.TextDict = m_TextDict;

            //ScreenMgr.LoadInitialScreen("gamedata\\luascripts\\personselectionedit.lua");
            ScreenMgr.AddScreen(new LUI.UIScreen(ScreenMgr), "gamedata\\luascripts\\personselectionedit.lua");

            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // TODO: use this.Content to load your game content here
            int Channel = Bass.BASS_StreamCreateFile("Sounds\\BUTTON.WAV", 0, 0, BASSFlag.BASS_DEFAULT);
            UISounds.AddSound(new UISound(0x01, Channel));
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        private float m_FPS = 0;

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            // Allows the game to exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == Microsoft.Xna.Framework.Input.ButtonState.Pressed)
                this.Exit();

            m_FPS = (float)(1 / gameTime.ElapsedGameTime.TotalSeconds);

            // TODO: Add your update logic here
            ScreenMgr.Update(gameTime);
            SceneMgr.Update(gameTime);

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Microsoft.Xna.Framework.Color.CornflowerBlue);

            // TODO: Add your drawing code here
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

            ScreenMgr.Draw(spriteBatch, m_FPS);
            SceneMgr.Draw(spriteBatch);

            spriteBatch.End();

            //SceneMgr.Draw();

            base.Draw(gameTime);
        }

        /// <summary>
        /// Loads the correct set of strings based on the current language.
        /// This method is a bit of a hack, but it works.
        /// </summary>
        private void LoadStrings()
        {
            string CurrentLang = GlobalSettings.Default.CurrentLang.ToLower();

            LuaInterfaceManager.RunFileInThread("gamedata\\uitext\\luatext\\" +
                CurrentLang + "\\" + CurrentLang + ".lua");

            m_TextDict.Add(1, (string)LuaInterfaceManager.LuaVM["LoginName"]);
            m_TextDict.Add(2, (string)LuaInterfaceManager.LuaVM["LoginPass"]);
            m_TextDict.Add(3, (string)LuaInterfaceManager.LuaVM["Login"]);
            m_TextDict.Add(4, (string)LuaInterfaceManager.LuaVM["Exit"]);
            m_TextDict.Add(5, (string)LuaInterfaceManager.LuaVM["OverallProgress"]);
            m_TextDict.Add(6, (string)LuaInterfaceManager.LuaVM["CurrentTask"]);
            m_TextDict.Add(7, (string)LuaInterfaceManager.LuaVM["InfoPopup1"]);
            m_TextDict.Add(8, (string)LuaInterfaceManager.LuaVM["PersonSelectionCaption"]);
            m_TextDict.Add(9, (string)LuaInterfaceManager.LuaVM["TimeStart"]);
            m_TextDict.Add(10, (string)LuaInterfaceManager.LuaVM["PersonSelectionEditCaption"]);
            m_TextDict.Add(11, (string)LuaInterfaceManager.LuaVM["CreateASim"]);
            m_TextDict.Add(12, (string)LuaInterfaceManager.LuaVM["RetireASim"]);

            //Loading strings
            m_TextDict.Add(13, (string)LuaInterfaceManager.LuaVM["LoadText1"]);
            m_TextDict.Add(14, (string)LuaInterfaceManager.LuaVM["LoadText2"]);
            m_TextDict.Add(15, (string)LuaInterfaceManager.LuaVM["LoadText3"]);
            m_TextDict.Add(16, (string)LuaInterfaceManager.LuaVM["LoadText4"]);
            m_TextDict.Add(17, (string)LuaInterfaceManager.LuaVM["LoadText5"]);
            m_TextDict.Add(18, (string)LuaInterfaceManager.LuaVM["LoadText6"]);
            m_TextDict.Add(19, (string)LuaInterfaceManager.LuaVM["LoadText7"]);
            m_TextDict.Add(20, (string)LuaInterfaceManager.LuaVM["LoadText8"]);
            m_TextDict.Add(21, (string)LuaInterfaceManager.LuaVM["LoadText9"]);
            m_TextDict.Add(22, (string)LuaInterfaceManager.LuaVM["LoadText10"]);
            m_TextDict.Add(23, (string)LuaInterfaceManager.LuaVM["LoadText11"]);
        }
    }
}
