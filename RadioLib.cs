using System.IO;
using Microsoft.Xna.Framework;
using RadioLib.Systems;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace RadioLib
{
    public enum RadioMessageType : byte
    {
        SyncTransmission
    }

    public class RadioLib : Mod
    {
        public static RadioLib Instance => ModContent.GetInstance<RadioLib>();

        public override void HandlePacket(BinaryReader reader, int whoAmI)
        {
            RadioMessageType msgType = (RadioMessageType)reader.ReadByte();

            switch (msgType)
            {
                case RadioMessageType.SyncTransmission:
                    ReceiveTransmissionPacket(reader, whoAmI);
                    break;
            }
        }

        private static void ReceiveTransmissionPacket(BinaryReader reader, int whoAmI)
        {
            string text = reader.ReadString();
            string callSign = reader.ReadString();
            string rankOrRole = reader.ReadString();
            int duration = reader.ReadInt32();
            
            bool hasColor = reader.ReadBoolean();
            Color? accentColor = hasColor ? new Color(reader.ReadByte(), reader.ReadByte(), reader.ReadByte(), reader.ReadByte()) : null;

            string portraitPath = reader.ReadString();
            string soundPath = reader.ReadString();
            float soundVolume = reader.ReadSingle();

            if (Main.netMode == NetmodeID.Server)
            {
                ModPacket packet = Instance.GetPacket();
                packet.Write((byte)RadioMessageType.SyncTransmission);
                packet.Write(text);
                packet.Write(callSign);
                packet.Write(rankOrRole);
                packet.Write(duration);
                packet.Write(hasColor);
                if (hasColor)
                {
                    packet.Write(accentColor.Value.R);
                    packet.Write(accentColor.Value.G);
                    packet.Write(accentColor.Value.B);
                    packet.Write(accentColor.Value.A);
                }
                packet.Write(portraitPath);
                packet.Write(soundPath);
                packet.Write(soundVolume);
                packet.Send(-1, whoAmI);
                return;
            }

            RadioTransmission transmission = new RadioTransmission
            {
                Text = text,
                CallSign = string.IsNullOrEmpty(callSign) ? null : callSign,
                RankOrRole = string.IsNullOrEmpty(rankOrRole) ? null : rankOrRole,
                Duration = duration,
                AccentColor = accentColor
            };

            if (!string.IsNullOrEmpty(portraitPath) && ModContent.HasAsset(portraitPath))
            {
                transmission.Portrait = ModContent.Request<Microsoft.Xna.Framework.Graphics.Texture2D>(portraitPath);
            }

            if (!string.IsNullOrEmpty(soundPath))
            {
                transmission.TransmitSound = new SoundStyle(soundPath) { Volume = soundVolume };
            }

            RadioSystem.EnqueueLocal(transmission);
        }
    }
}