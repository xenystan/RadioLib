using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.Audio;

namespace RadioLib.Systems
{
    public class RadioTransmission
    {
        public Asset<Texture2D> Portrait { get; set; }
        public string PortraitPath { get; set; }
        public string CallSign { get; set; }
        public string RankOrRole { get; set; }
        public string Text { get; set; }
        public int Duration { get; set; }
        public SoundStyle? TransmitSound { get; set; }
        public string SoundPath { get; set; }
        public float SoundVolume { get; set; } = 1f;
        public Color? AccentColor { get; set; }
    }
}