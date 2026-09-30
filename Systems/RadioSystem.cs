// MP-Safe
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using ReLogic.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace RadioLib.Systems
{
    public class RadioSystem : ModSystem
    {
        public static Asset<SpriteFont> FontCallsign { get; private set; }
        public static Asset<SpriteFont> FontRole { get; private set; }
        public static Asset<SpriteFont> FontSubtitleEn { get; private set; }
        public static Asset<SpriteFont> FontSubtitleRu { get; private set; }

        private static readonly Queue<RadioTransmission> transmissionQueue = new();
        private static RadioTransmission activeTransmission;
        private static int currentTimer;
        private static int maxTimer;

        private const int FadeTicks = 25;

        public override void Load()
        {
            if (!Main.dedServ)
            {
                if (ModContent.HasAsset("RadioLib/Assets/Fonts/IntraNet_Outline"))
                    FontCallsign = ModContent.Request<SpriteFont>("RadioLib/Assets/Fonts/IntraNet_Outline");

                if (ModContent.HasAsset("RadioLib/Assets/Fonts/IntraNet"))
                    FontRole = ModContent.Request<SpriteFont>("RadioLib/Assets/Fonts/IntraNet");

                if (ModContent.HasAsset("RadioLib/Assets/Fonts/Saira"))
                    FontSubtitleEn = ModContent.Request<SpriteFont>("RadioLib/Assets/Fonts/Saira");

                if (ModContent.HasAsset("RadioLib/Assets/Fonts/Oswald"))
                    FontSubtitleRu = ModContent.Request<SpriteFont>("RadioLib/Assets/Fonts/Oswald");
            }
        }

        public override void Unload()
        {
            FontCallsign = null;
            FontRole = null;
            FontSubtitleEn = null;
            FontSubtitleRu = null;
            RadioEngine.Clear();
            ClearQueue();
        }

        public static void ClearQueue()
        {
            transmissionQueue.Clear();
            activeTransmission = null;
            currentTimer = 0;
            maxTimer = 0;
        }

        public static void EnqueueLocal(RadioTransmission transmission)
        {
            transmissionQueue.Enqueue(transmission);
        }

        public static void SendTransmission(RadioTransmission transmission, bool priority = false, bool syncNetwork = true)
        {
            if (priority)
            {
                ClearQueue();
            }

            if (syncNetwork && Main.netMode != NetmodeID.SinglePlayer)
            {
                ModPacket packet = RadioLib.Instance.GetPacket();
                packet.Write((byte)RadioMessageType.SyncTransmission);
                packet.Write(transmission.Text ?? "");
                packet.Write(transmission.CallSign ?? "");
                packet.Write(transmission.RankOrRole ?? "");
                packet.Write(transmission.Duration);

                bool hasColor = transmission.AccentColor.HasValue;
                packet.Write(hasColor);
                if (hasColor)
                {
                    packet.Write(transmission.AccentColor.Value.R);
                    packet.Write(transmission.AccentColor.Value.G);
                    packet.Write(transmission.AccentColor.Value.B);
                    packet.Write(transmission.AccentColor.Value.A);
                }

                packet.Write(transmission.PortraitPath ?? "");
                packet.Write(transmission.SoundPath ?? "");
                packet.Write(transmission.SoundVolume);

                if (Main.netMode == NetmodeID.MultiplayerClient)
                    packet.Send();
                else if (Main.netMode == NetmodeID.Server)
                    packet.Send(-1, -1);
            }

            if (Main.netMode != NetmodeID.Server)
            {
                transmissionQueue.Enqueue(transmission);
            }
        }

        public override void UpdateUI(GameTime gameTime)
        {
            if (Main.netMode == NetmodeID.Server) return;

            if (activeTransmission == null && transmissionQueue.Count > 0)
            {
                activeTransmission = transmissionQueue.Dequeue();
                currentTimer = activeTransmission.Duration;
                maxTimer = activeTransmission.Duration;

                // Автоматическая резолюция портрета по пути
                if (activeTransmission.Portrait == null && !string.IsNullOrEmpty(activeTransmission.PortraitPath) && ModContent.HasAsset(activeTransmission.PortraitPath))
                {
                    activeTransmission.Portrait = ModContent.Request<Texture2D>(activeTransmission.PortraitPath);
                }

                // Автоматическая резолюция звука по пути
                if (!activeTransmission.TransmitSound.HasValue && !string.IsNullOrEmpty(activeTransmission.SoundPath))
                {
                    activeTransmission.TransmitSound = new SoundStyle(activeTransmission.SoundPath) { Volume = activeTransmission.SoundVolume };
                }

                if (activeTransmission.TransmitSound.HasValue)
                    SoundEngine.PlaySound(activeTransmission.TransmitSound.Value);
            }

            if (activeTransmission != null)
            {
                currentTimer--;
                if (currentTimer <= 0)
                    activeTransmission = null;
            }
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            if (Main.netMode == NetmodeID.Server) return;

            int infoAccIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Info Accessories"));
            int targetIndex = infoAccIndex != -1 ? infoAccIndex + 1 : layers.FindIndex(layer => layer.Name.Equals("Vanilla: Inventory")) + 1;

            if (targetIndex > 0)
            {
                layers.Insert(targetIndex, new LegacyGameInterfaceLayer(
                    "RadioLib: Tactical Radio HUD",
                    delegate
                    {
                        DrawHUD(Main.spriteBatch);
                        return true;
                    },
                    InterfaceScaleType.UI)
                );
            }
        }

        private static bool ContainsCyrillic(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            foreach (char c in text)
            {
                if ((c >= 'а' && c <= 'я') || (c >= 'А' && c <= 'Я') || c == 'ё' || c == 'Ё')
                    return true;
            }
            return false;
        }

        private static SpriteFont GetSubtitleFont(string text)
        {
            bool isRussian = Language.ActiveCulture.Name == "ru-RU" || ContainsCyrillic(text);

            if (isRussian && FontSubtitleRu != null && FontSubtitleRu.IsLoaded)
                return FontSubtitleRu.Value;

            if (!isRussian && FontSubtitleEn != null && FontSubtitleEn.IsLoaded)
                return FontSubtitleEn.Value;

            return null;
        }

        private static void DrawHUD(SpriteBatch spriteBatch)
        {
            if (activeTransmission == null) return;

            int elapsed = maxTimer - currentTimer;
            float fadeIn = MathHelper.Clamp(elapsed / (float)FadeTicks, 0f, 1f);
            float fadeOut = MathHelper.Clamp(currentTimer / (float)FadeTicks, 0f, 1f);
            float alpha = Math.Min(fadeIn, fadeOut);

            Texture2D fill = TextureAssets.MagicPixel.Value;

            bool hasHUDHeader = (activeTransmission.Portrait != null && activeTransmission.Portrait.IsLoaded) || !string.IsNullOrEmpty(activeTransmission.CallSign);

            if (hasHUDHeader)
            {
                int portraitDrawWidth = 320;
                int portraitDrawHeight = 160;

                if (activeTransmission.Portrait != null && activeTransmission.Portrait.IsLoaded)
                {
                    Texture2D tex = activeTransmission.Portrait.Value;
                    portraitDrawHeight = (int)(portraitDrawWidth * ((float)tex.Height / tex.Width));
                }

                int padding = 6;
                int frameWidth = portraitDrawWidth + (padding * 2);
                int infoHeight = 48;
                int totalBoxHeight = portraitDrawHeight + infoHeight + padding;

                Vector2 origin = new Vector2(Main.screenWidth - frameWidth - 20, 115);

                Rectangle bgRect = new Rectangle((int)origin.X, (int)origin.Y, frameWidth, totalBoxHeight);
                spriteBatch.Draw(fill, bgRect, Color.Black * (0.92f * alpha));

                if (activeTransmission.Portrait != null && activeTransmission.Portrait.IsLoaded)
                {
                    Rectangle portraitRect = new Rectangle((int)origin.X + padding, (int)origin.Y + padding, portraitDrawWidth, portraitDrawHeight);
                    DrawGlitchedPortrait(spriteBatch, activeTransmission.Portrait.Value, portraitRect, alpha);
                }

                Color accent = activeTransmission.AccentColor ?? Color.Red;

                Rectangle lineRect = new Rectangle((int)origin.X + padding, (int)origin.Y + padding + portraitDrawHeight + 3, portraitDrawWidth, 2);
                spriteBatch.Draw(fill, lineRect, accent * (0.85f * alpha));

                float callsignScale = 0.61f;
                float roleScale = 0.49f;

                Vector2 callsignPos = origin + new Vector2(padding + 6, portraitDrawHeight + padding + 6);

                if (!string.IsNullOrEmpty(activeTransmission.CallSign))
                {
                    if (FontCallsign != null && FontCallsign.IsLoaded)
                    {
                        SpriteFont font = FontCallsign.Value;
                        spriteBatch.DrawString(font, activeTransmission.CallSign, callsignPos, accent * alpha, 0f, Vector2.Zero, callsignScale, SpriteEffects.None, 0f);
                    }
                    else
                    {
                        DynamicSpriteFont font = FontAssets.MouseText.Value;
                        spriteBatch.DrawString(font, activeTransmission.CallSign, callsignPos + Vector2.One, Color.Black * alpha, 0f, Vector2.Zero, callsignScale, SpriteEffects.None, 0f);
                        spriteBatch.DrawString(font, activeTransmission.CallSign, callsignPos, accent * alpha, 0f, Vector2.Zero, callsignScale, SpriteEffects.None, 0f);
                    }
                }

                if (!string.IsNullOrEmpty(activeTransmission.RankOrRole))
                {
                    Vector2 rolePos = callsignPos + new Vector2(0, 20);

                    if (FontRole != null && FontRole.IsLoaded)
                    {
                        SpriteFont font = FontRole.Value;
                        spriteBatch.DrawString(font, activeTransmission.RankOrRole, rolePos + Vector2.One, Color.Black * alpha, 0f, Vector2.Zero, roleScale, SpriteEffects.None, 0f);
                        spriteBatch.DrawString(font, activeTransmission.RankOrRole, rolePos, Color.White * alpha, 0f, Vector2.Zero, roleScale, SpriteEffects.None, 0f);
                    }
                    else
                    {
                        DynamicSpriteFont font = FontAssets.MouseText.Value;
                        spriteBatch.DrawString(font, activeTransmission.RankOrRole, rolePos + Vector2.One, Color.Black * alpha, 0f, Vector2.Zero, roleScale, SpriteEffects.None, 0f);
                        spriteBatch.DrawString(font, activeTransmission.RankOrRole, rolePos, Color.White * alpha, 0f, Vector2.Zero, roleScale, SpriteEffects.None, 0f);
                    }
                }
            }

            if (!string.IsNullOrEmpty(activeTransmission.Text))
            {
                float textScale = 0.67f;
                SpriteFont subtitleFont = GetSubtitleFont(activeTransmission.Text);

                Vector2 textSize = subtitleFont != null 
                    ? subtitleFont.MeasureString(activeTransmission.Text) * textScale
                    : FontAssets.MouseText.Value.MeasureString(activeTransmission.Text) * textScale;

                Vector2 subCenter = new Vector2(Main.screenWidth / 2f, Main.screenHeight - 135f);
                Vector2 subPos = subCenter - (textSize / 2f);

                Rectangle subBg = new Rectangle(
                    (int)(subPos.X - 14),
                    (int)(subPos.Y - 3),
                    (int)(textSize.X + 28),
                    (int)(textSize.Y + 6)
                );

                spriteBatch.Draw(fill, subBg, Color.Black * (0.8f * alpha));

                Color sideColor = (activeTransmission.AccentColor ?? Color.LimeGreen) * alpha;
                spriteBatch.Draw(fill, new Rectangle(subBg.X, subBg.Y, 3, subBg.Height), sideColor);
                spriteBatch.Draw(fill, new Rectangle(subBg.Right - 3, subBg.Y, 3, subBg.Height), sideColor);

                if (subtitleFont != null)
                {
                    spriteBatch.DrawString(subtitleFont, activeTransmission.Text, subPos + Vector2.One, Color.Black * alpha, 0f, Vector2.Zero, textScale, SpriteEffects.None, 0f);
                    spriteBatch.DrawString(subtitleFont, activeTransmission.Text, subPos, Color.White * alpha, 0f, Vector2.Zero, textScale, SpriteEffects.None, 0f);
                }
                else
                {
                    DynamicSpriteFont font = FontAssets.MouseText.Value;
                    spriteBatch.DrawString(font, activeTransmission.Text, subPos + Vector2.One, Color.Black * alpha, 0f, Vector2.Zero, textScale, SpriteEffects.None, 0f);
                    spriteBatch.DrawString(font, activeTransmission.Text, subPos, Color.White * alpha, 0f, Vector2.Zero, textScale, SpriteEffects.None, 0f);
                }
            }
        }

        private static void DrawGlitchedPortrait(SpriteBatch spriteBatch, Texture2D texture, Rectangle destRect, float alpha)
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;

            if (Main.rand.NextBool(3))
            {
                int splitOffset = Main.rand.Next(-5, 6);
                spriteBatch.Draw(texture, new Rectangle(destRect.X + splitOffset, destRect.Y, destRect.Width, destRect.Height), Color.Red * (0.5f * alpha));
                spriteBatch.Draw(texture, new Rectangle(destRect.X - splitOffset, destRect.Y, destRect.Width, destRect.Height), Color.Cyan * (0.5f * alpha));
            }

            if (Main.rand.NextBool(4))
            {
                int sliceCount = Main.rand.Next(4, 10);
                int srcSliceHeight = texture.Height / sliceCount;
                float destSliceHeight = (float)destRect.Height / sliceCount;

                for (int i = 0; i < sliceCount; i++)
                {
                    Rectangle srcRect = new Rectangle(0, i * srcSliceHeight, texture.Width, srcSliceHeight);
                    int xOffset = Main.rand.NextBool(3) ? Main.rand.Next(-14, 15) : 0;

                    Rectangle sliceDest = new Rectangle(
                        destRect.X + xOffset, 
                        (int)(destRect.Y + i * destSliceHeight), 
                        destRect.Width, 
                        (int)destSliceHeight + 1
                    );

                    spriteBatch.Draw(texture, sliceDest, srcRect, Color.White * alpha);
                }
            }
            else
            {
                spriteBatch.Draw(texture, destRect, Color.White * alpha);
            }

            for (int y = destRect.Y; y < destRect.Y + destRect.Height; y += 4)
            {
                spriteBatch.Draw(pixel, new Rectangle(destRect.X, y, destRect.Width, 2), Color.Black * (0.4f * alpha));
            }

            if (Main.rand.NextBool(2))
            {
                int noiseY = Main.rand.Next(destRect.Y, destRect.Y + destRect.Height);
                int noiseHeight = Main.rand.Next(2, 8);
                Color noiseColor = Main.rand.NextBool() ? Color.LimeGreen : Color.White;

                spriteBatch.Draw(pixel, new Rectangle(destRect.X, noiseY, destRect.Width, noiseHeight), noiseColor * (0.3f * alpha));
            }
        }
    }
}