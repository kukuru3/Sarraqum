using System;

using Ur;
using Ur.Grid;

namespace Sarraqum {

    struct Glyph {
        public char glyphid;
        public uint backColor;
        public uint foreColor;
    }

    public class Surface {
        public uint BackgroundColor { get; set; }

        internal bool IsRoot { get; private set; }
        internal int index { get; private set; }

        internal int generation; // used for internal calculations
        private static int indexCounter;

        internal event Action<Surface> RectUpdated;
        internal AggregateBlitMatrix Aggregator { get; private set; }

        private Rect rect;
        internal Rect calculatedScreenRect; 

        public Surface AttachedTo { get; private set; }

        public void AttachTo(Surface parent) {
            if (this.IsRoot) throw new ArgumentException("Main surface cannot be attached");
            if (AttachedTo != null) throw new ArgumentException("Surfaces cannot be re-attached.");
            if (parent.Aggregator == null) throw new ArgumentException($"Parent surface {parent} does not have an aggregator - this should never happen");

            Aggregator = parent.Aggregator;
            AttachedTo = parent;
            Aggregator.RegisterSurface(this);

            index = ++Surface.indexCounter;
        }

        public void Destroy() {
            if (this.IsRoot) throw new ArgumentException("Main surface cannot be detached");
            Aggregator.UnregisterSurface(this);
        }

        internal static Surface CreateMainSurface(ConsoleRenderer renderer) {
            var s = new Surface {
                Rect = new Rect(0, 0, renderer.W, renderer.H),
                IsRoot = true,
                Aggregator = new AggregateBlitMatrix(renderer),
            };
            s.RegenerateGlyphArray();
            s.Aggregator.RegisterSurface(s);
            s.Clear();
            return s;
        }

        public static Surface Create(Surface parent, Rect rect) {
            var s = new Surface();
            s.Rect = rect;
            s.RegenerateGlyphArray();
            s.AttachTo(parent);
            s.Clear();
            return s;
        }

        public static Surface Create(Rect rect) => Create(Console.mainSurfaceInstance, rect);

        public Rect Rect { 
            get => rect;
            set {
                rect = value;
                RectUpdated?.Invoke(this);
                RegenerateGlyphArray();
            }
        }

        Glyph[] localGlyphs;

        internal int GetPackedScreenIndexOfLocalCoords(int localX, int localY) {
            var b = this.calculatedScreenRect.BoundsLow;
            return this.Aggregator.PackIndex(b.X + localX, b.Y + localY);
        }
        internal Glyph GetGlyphAtScreen(int screenX, int screenY) {
            var b = this.calculatedScreenRect.BoundsLow;
            var x = screenX - rect.BoundsLow.Y;
            var y = screenY - rect.BoundsLow.Y;
            if (!AreCoordinatesLegal(x, y)) return default;
            return localGlyphs[PackIndex(x,y)];
        }

        public void RegenerateGlyphArray() {
            localGlyphs = new Glyph[Rect.Width * Rect.Height];
        }
        
        // Print and SetFoo contain a lot of repetitive code and could be greatly architecturally improved
        // by even something as small as optional / nullable arguments. 
        // However since they are so frequently called and critical for performance, I use overloads, 
        // with a lot of duplicated code. Such is life.
        public void Print(int x, int y, string str) {
            for (var z = 0; z < str.Length; z++) {
                var finalX = x + z;
                if (!AreCoordinatesLegal(finalX, y)) return;
                var i = PackIndex(finalX, y);
                var g = localGlyphs[i];
                g.glyphid = str[z];
                localGlyphs[i] = g;
                NotifyAggregator(finalX, y);
            }
        }

        public void Print(int x, int y, string str, uint color) {
            for (var z = 0; z < str.Length; z++) {
                var finalX = x + z;
                if (!AreCoordinatesLegal(finalX, y)) return;
                var i = PackIndex(finalX, y);
                var g = localGlyphs[i];
                g.glyphid = str[z];
                g.foreColor = color;
                localGlyphs[i] = g;
                NotifyAggregator(finalX, y);
            }
        }

        public void Print(int x, int y, string str, uint color, uint background) {
            for (var z = 0; z < str.Length; z++) {
                var finalX = x + z;
                if (!AreCoordinatesLegal(finalX, y)) return;
                var i = PackIndex(finalX, y);
                localGlyphs[i] = new Glyph() {  glyphid = str[z], foreColor = color, backColor = background };
                NotifyAggregator(finalX, y);
            }
        }

        public void Clear() {
            Fill(' ', Color.White, BackgroundColor);
        }

        public int PackIndex(int x, int y) => y * rect.Width + x;

        public void SetGlyph(int x, int y, char c) {
            if (!AreCoordinatesLegal(x, y)) return;
            var i = PackIndex(x, y);

            // no idea if this actually results in a more performant code
            unsafe {
                fixed (Glyph* buffer = localGlyphs) {
                    buffer[i].glyphid = c;
                }
            }
            
            //var g = localGlyphs[i];
            //g.glyphid = c;
            //localGlyphs[i] = g;
            NotifyAggregator(x, y);
        }

        void NotifyAggregator(int x, int y) => Aggregator.MarkGlyphDirty(GetPackedScreenIndexOfLocalCoords(x, y));

        public void SetCell(int x, int y, char glyph, uint color, uint backColor) {
            if (!AreCoordinatesLegal(x, y)) return;
            var i = PackIndex(x, y);
            unsafe {
                 fixed (Glyph* buffer = localGlyphs) {
                    buffer[i].glyphid = glyph;
                    buffer[i].foreColor = color;
                    buffer[i].backColor = backColor;
                }
            }
            //var g = localGlyphs[i];
            //g.glyphid = glyph;
            //g.foreColor = color;
            //g.backColor = backColor;
            //localGlyphs[i] = g;
            NotifyAggregator(x, y);
        }

        public void SetForeColor(int x, int y, Color color) {
            if (!AreCoordinatesLegal(x, y)) return;
            var i = PackIndex(x, y);
            var g = localGlyphs[i];
            g.foreColor = color;
            localGlyphs[i] = g;
            NotifyAggregator(x, y);
        }

        public void SetBackColor(int x, int y, Color backColor) {
            if (!AreCoordinatesLegal(x, y)) return;
            var i = PackIndex(x, y);
            var g = localGlyphs[i];
            g.backColor = backColor;
            localGlyphs[i] = g;
            NotifyAggregator(x, y);
        }

        bool AreCoordinatesLegal(int x, int y) => x >= 0 && x < rect.Width && y >= 0 && y < rect.Height;

        internal void Fill(char id, Color fore, Color back) {
            var n = rect.Surface;
            var g = new Glyph() { glyphid = id, backColor = back, foreColor = fore };
            
            for (var i = 0; i < n; i++) { 
                localGlyphs[i] = g;
                Aggregator.MarkGlyphDirty(i);
            }
        }
    }
}
