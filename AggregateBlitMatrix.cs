
using System;
using System.Collections.Generic;
using System.Linq;

using Ur.Grid;

namespace Sarraqum {
    class AggregateBlitMatrix {
        readonly int w, h;
        ConsoleRenderer Renderer { get; }

        bool[] dirtyGlyphs;
        byte[] surfaceIndices;
        bool allDirty;

        List<int> dirtyGlyphIndices;

        internal AggregateBlitMatrix(ConsoleRenderer renderer) {
            Renderer = renderer;
            this.w = Renderer.W; this.h = Renderer.H;
            dirtyGlyphs = new bool[w * h];
            surfaceIndices = new byte[w * h];
            allDirty = true;
            dirtyGlyphIndices = new List<int>(w * h + 2);
        }

        internal void Flush() {
            if (generationsUpset) ResortSurfaces();
            if (allDirty) FeedAllIndices();
            else UpdateFromList();
        }

        private void FeedAllIndices() {
            for (var i = 0; i < w * h; i++) {
                var s = surfaces[surfaceIndices[i]];
                (var x, var y) = UnpackIndex(i);
                var g = s.GetGlyphAtScreen(x, y);
                Renderer.Set(x, y, g);
            }
            allDirty = false;
            dirtyGlyphIndices.Clear();
            dirtyGlyphs = new bool[dirtyGlyphs.Length];
        }

        private void UpdateFromList() {
            foreach (var i in dirtyGlyphIndices) {
                var s = surfaces[surfaceIndices[i]];
                (var x, var y) = UnpackIndex(i);
                var g = s.GetGlyphAtScreen(x, y);
                Renderer.Set(x, y, g);
                dirtyGlyphs[i] = false;
            }
            allDirty = false;
            dirtyGlyphIndices.Clear();
        }

        List<Surface> surfaces = new List<Surface>();
        bool generationsUpset  = false;
         
        internal void RegisterSurface(Surface s) {
            surfaces.Add(s);
            generationsUpset = true;
        }

        internal void UnregisterSurface(Surface s) {
            surfaces.Remove(s);
            generationsUpset = true;
        }

        // if surfaces are resorted, for now we naively just resolve the ownership of every 
        // single matrix cell. Surfaces are supposed to be rarely resorted.
        private void ResortSurfaces() {
            
            var generationTuples = surfaces
                .Select(s => (s, this.GetSurfaceGeneration(s)))
                .OrderBy(tuple => tuple.Item2)
                .ThenBy(tuple => tuple.s.index);

            foreach (var t in generationTuples) t.s.generation = t.Item2;
                
            surfaces = generationTuples.Select(tuple => tuple.s).ToList();

            generationsUpset = false;
            foreach (var surface in surfaces) surface.calculatedScreenRect = CalculateScreenRectOf(surface);

            surfaceIndices = new byte[w * h];
            for (byte i = 1; i < surfaces.Count; i++) {
                var surf = surfaces[i];
                var offset = surf.Rect.BoundsLow;
                foreach (var crd in surf.Rect.Enumerate())
                    surfaceIndices[PackIndex(crd.X, crd.Y)] = i;
            }
            allDirty = true;
        }

        Rect CalculateScreenRectOf(Surface s) {
            if (s.IsRoot) return s.Rect;
            if (s.AttachedTo == null) throw new InvalidOperationException("Surface not attached!");
            // non-recursive, implies we are doing this in order:
            return new Rect(s.AttachedTo.calculatedScreenRect.BoundsLow + s.Rect.BoundsLow, s.Rect.Dimension);
            // recursive, works in any order, but o(n^2)
            // return new Rect(ScreenRect(s.AttachedTo).BoundsLow + s.Rect.BoundsLow, s.Rect.Dimension);
        }

        internal void MarkAllDirty() {
            allDirty = true;
        }

        internal void MarkGlyphDirty(int v) {
            if (dirtyGlyphs[v]) return;
            dirtyGlyphIndices.Add(v);
        }

        private int GetSurfaceGeneration(Surface s) {
            if (s.IsRoot) return 0;
            return s.AttachedTo.generation + 1;
        }

        public int PackIndex(int x, int y) => y * w + x;

        (int x, int y) UnpackIndex(int i) {
            var y = i / w;
            return (i - y * w, y);
        }

    }
}
