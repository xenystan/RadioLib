using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.Audio;

namespace RadioLib.Systems
{
    // mp-safe data container for an active or queued radio transmission
    public class RadioTransmission
    {
        // loaded portrait texture asset
        public Asset<Texture2D> Portrait { get; set; }

        // path to the portrait texture asset in content
        public string PortraitPath { get; set; }

        // callsign displayed in the transmission header
        public string CallSign { get; set; }

        // military rank or operational role
        public string RankOrRole { get; set; }

        // subtitle message text
        public string Text { get; set; }

        // display lifetime in ticks (60 ticks = 1 second)
        public int Duration { get; set; }

        // resolved sound style for incoming voice or beep
        public SoundStyle? TransmitSound { get; set; }

        // asset path to the transmission audio file
        public string SoundPath { get; set; }

        // playback volume multiplier for audio
        public float SoundVolume { get; set; } = 1f;

        // accent color for ui borders and callsign highlighting
        public Color? AccentColor { get; set; }
    }
}
