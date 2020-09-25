using Sargon.Assets;

using System;
using System.Collections.Generic;

using Ur;

/// <summary> 
/// Sarraqum - Akkadian for Thief, Rogue.
/// A small engine for rendering ascii console-like interfaces based on Sargon 2.0 
/// It completely wraps a Sargon Game instance - if you use Sarraqum, there is no need to interact with Sargon classes.
/// 
/// </summary>
namespace Sarraqum {
    public class Console {
        internal  ConsoleConfiguration Configuration { get; }
        public Sargon.Game SargonGameInstance { get; private set; }

        public Console(ConsoleConfiguration config) {
            this.Configuration = config;
        }

        public int Width => Configuration.Dimensions.X;
        public int Height => Configuration.Dimensions.Y;

        public event Action Loaded;
        public event Action Tick;
        public event Action<double> Frame;

        public IEnumerable<Sargon.Input.Keys> GetPressedKeys() => SargonGameInstance.Context.Input.CurrentlyPressedKeys();

        public void Create() {
            SargonGameInstance = new Sargon.Game();
            SargonGameInstance.Title = Configuration.Title;
            SargonGameInstance.Context.Timer.ScreenFramerateLimit = Configuration.TargetFPS;
            SargonGameInstance.TicksPerSecond = Configuration.TicksPerSecond;
            SargonGameInstance.Context.Timer.ScreenVSync = Configuration.VSync;

            SargonGameInstance.SetResolution(
                (Configuration.CharacterSize.X * Configuration.Dimensions.X * Configuration.DisplayScale).Round(),
                (Configuration.CharacterSize.Y * Configuration.Dimensions.Y * Configuration.DisplayScale).Round(),
                Sargon.Game.WindowStyle.Windowed
            );

            // new Sargon.Graphics.PipelineSteps.BackgroundColorSetter(Configuration.BackgroundColor);
            var canvas = new Sargon.Graphics.Canvas();
            var loader = new Sargon.Utils.BasicLoader("Resources");
            loader.Complete += OnLoadComplete;
            loader.ScanAndExecute(); // async, fire and forget

            SargonGameInstance.AddCallback(Sargon.Hooks.Frame, () => {
                Frame?.Invoke(SargonGameInstance.FrameTime);
                this.MainSurface?.Aggregator?.Flush();
            });
            SargonGameInstance.AddCallback(Sargon.Hooks.Tick, () => Tick?.Invoke());
        }

        private void GenerateEverything() {
            FixMainTextureBlacksToAlpha();
            MainTextureGridDefinition = CreateASCIIGridDefinition();
            MainConsoleRenderer = new ConsoleRenderer(this, Configuration.Dimensions.X, Configuration.Dimensions.Y);
            mainSurfaceInstance = Surface.CreateMainSurface(MainConsoleRenderer);
            mainSurfaceInstance.BackgroundColor = Configuration.BackgroundColor;

            MainSurface.Fill(' ' , Color.White, Configuration.BackgroundColor);
        }

        private void FixMainTextureBlacksToAlpha() {
            var mainTex = SargonGameInstance.Context.Assets.Find(Configuration.Texture);
            var massager = new TextureMassager();
            massager.AddAlphaChannel(mainTex.Texture);
        }

        public Surface MainSurface => mainSurfaceInstance;

        static internal Surface mainSurfaceInstance;

        private GridDefinition CreateASCIIGridDefinition() {
            // initialize items to ascii characters
            var tex = SargonGameInstance.Context.Assets.Find(Configuration.Texture);
            if (tex == null) {
                throw new ArgumentException($"Configured texture by name of {Configuration.Texture} was not found");
            }

            var glyphx = tex.Texture.Width / Configuration.CharacterSize.X;
            var glyphy = tex.Texture.Height / Configuration.CharacterSize.Y;

            var griddef = new GridDefinition($"{Configuration.Texture}.grid", tex.Texture, (glyphx, glyphy));

            for (var y = 0; y < griddef.GridDimension.Y; y++)
            for (var x = 0; x < griddef.GridDimension.X; x++) {
                var index = y * griddef.GridDimension.X + x;
                griddef.SetGlyphID(y, x, $"{(char)index}");
            }
            return griddef;
        }

        private void OnLoadComplete() {
            GenerateEverything();
            Loaded?.Invoke();
        }

        public void Run() {
            if (SargonGameInstance == null) throw new InvalidOperationException("Cannot run game - did you forget to call Console.Create() first?");
            SargonGameInstance.Run();
        }
         
        internal ConsoleRenderer MainConsoleRenderer { get; private set; }

        internal GridDefinition MainTextureGridDefinition { get; private set; }

        public void DefineCharacter(int row, int column, string glyphID) {
            MainTextureGridDefinition.SetGlyphID(row, column, glyphID);
        }
    }
}
