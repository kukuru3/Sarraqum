namespace Sarraqum {

    class ConsoleRenderer {
        public int W { get; }
        public int H { get; }
        public Console Window { get; }

        Sargon.Graphics.Canvas canvas;
        Sargon.Graphics.QuadGrid foregroundQuads;
        Sargon.Graphics.QuadGrid backgroundQuads;

        public ConsoleRenderer(Console window, int w, int h) {
            Window = window;
            W = w; H = h;
            CreateVisualElements();
        }

        private void CreateVisualElements() {
            
            canvas = new Sargon.Graphics.Canvas();

            foregroundQuads = new Sargon.Graphics.QuadGrid() {
                DefaultColor = Ur.Colors.White,
                QuadDimensions = Window.Configuration.CharacterSize * Window.Configuration.DisplayScale,
                NumQuads = (W, H),
                Position = (0f, 0f),
                Zed = 0f,
                Visible = true,
                SourceGrid = Window.MainTextureGridDefinition,
            };

            backgroundQuads = new Sargon.Graphics.QuadGrid() {
                DefaultColor = Window.Configuration.BackgroundColor,
                QuadDimensions = Window.Configuration.CharacterSize * Window.Configuration.DisplayScale,
                NumQuads = (W, H),
                Position = (0f, 0f),
                Zed = 0f,
                Visible = true,
                SourceGrid = null,// Source.MainTextureGridDefinition,
            };

            canvas.SortRenderables = false;
            canvas.Add(backgroundQuads);
            canvas.Add(foregroundQuads);
        }

        internal void Set(int x, int y, Glyph g) {
            foregroundQuads.SetGlyph(x, y, g.glyphid);
            foregroundQuads.SetColor(x, y, g.foreColor);
            backgroundQuads.SetColor(x, y, g.backColor);
        }
    }
}
