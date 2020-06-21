
using Ur;
using Ur.Grid;

namespace Sarraqum {
    public class ConsoleConfiguration {
        /// <summary> The string identifier of a Sargon texture definition</summary>
        public string Texture { get; set; }
        /// <summary> Single character size in pixels </summary>
        public Coords CharacterSize { get; set; }
        /// <summary>Console dimensions</summary>
        public Coords Dimensions { get; set; }

        public float DisplayScale { get; set; } = 1f;

        public Color BackgroundColor { get; set; }

        public string Title { get; set; }

        public int TargetFPS { get; set; } = 60;

        public bool VSync { get; set; }
    }
}
