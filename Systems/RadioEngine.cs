using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace RadioLib.Systems
{
    // json data contract for speaker profiles
    public class JsonRadioProfile
    {
        public string CallSign { get; set; }
        public string RankOrRole { get; set; }
        public List<string> SquadNames { get; set; } = new();
        public List<string> Roles { get; set; } = new();
        public List<string> Portraits { get; set; } = new();
        public string DefaultAccentColor { get; set; } = "#39FF14";
    }

    // json data contract for individual radio transmissions
    public class JsonTransmission
    {
        public string Profile { get; set; }
        public string Text { get; set; }
        public string TextKey { get; set; }
        public string Sound { get; set; }
        public float Volume { get; set; } = 1f;
        public int Duration { get; set; } = 180;
        public string AccentColor { get; set; }
        public bool Priority { get; set; } = false;
    }

    // wrapper package for deserializing dialogue json files
    public class RadioJsonPackage
    {
        public Dictionary<string, JsonRadioProfile> Profiles { get; set; } = new();
        public Dictionary<string, JsonTransmission> Transmissions { get; set; } = new();
        public Dictionary<string, List<string>> Pools { get; set; } = new();
    }

    // core engine managing radio transmission registrations, json parsing and triggering
    public static class RadioEngine
    {
        private static readonly Dictionary<string, JsonTransmission> registeredTransmissions = new();
        private static readonly Dictionary<string, JsonRadioProfile> registeredProfiles = new();
        private static readonly Dictionary<string, List<string>> registeredPools = new();

        // clears all registered json data, profiles and pools
        public static void Clear()
        {
            registeredTransmissions.Clear();
            registeredProfiles.Clear();
            registeredPools.Clear();
        }

        // loads and registers dialogue json files from specified mod path
        public static void LoadJson(Mod mod, string relativePath)
        {
            if (!mod.FileExists(relativePath)) return;

            string jsonContent = System.Text.Encoding.UTF8.GetString(mod.GetFileBytes(relativePath));
            RadioJsonPackage package = JsonConvert.DeserializeObject<RadioJsonPackage>(jsonContent);

            if (package == null) return;

            foreach (var (key, profile) in package.Profiles)
                registeredProfiles[key] = profile;

            foreach (var (key, transmission) in package.Transmissions)
                registeredTransmissions[key] = transmission;

            foreach (var (key, pool) in package.Pools)
                registeredPools[key] = pool;
        }

        // triggers a registered transmission by id and handles multiplayer sync
        public static void Play(string transmissionId, bool syncNetwork = true)
        {
            if (!registeredTransmissions.TryGetValue(transmissionId, out var jsonTrans))
                return;

            string portrait = "";
            string callSign = "";
            string role = "";
            Color accentColor = Color.LimeGreen;

            if (!string.IsNullOrEmpty(jsonTrans.Profile) && registeredProfiles.TryGetValue(jsonTrans.Profile, out var profile))
            {
                if (profile.Portraits.Count > 0)
                    portrait = profile.Portraits[Main.rand.Next(profile.Portraits.Count)];

                callSign = !string.IsNullOrEmpty(profile.CallSign) 
                    ? profile.CallSign 
                    : (profile.SquadNames.Count > 0 ? GetRandomSquadCallsign(profile.SquadNames) : "");

                role = !string.IsNullOrEmpty(profile.RankOrRole) 
                    ? profile.RankOrRole 
                    : (profile.Roles.Count > 0 ? profile.Roles[Main.rand.Next(profile.Roles.Count)] : "");

                accentColor = ParseColor(profile.DefaultAccentColor);
            }

            if (!string.IsNullOrEmpty(jsonTrans.AccentColor))
            {
                accentColor = ParseColor(jsonTrans.AccentColor);
            }

            string displayText = jsonTrans.Text ?? "";
            if (!string.IsNullOrEmpty(jsonTrans.TextKey))
            {
                displayText = Language.GetTextValue(jsonTrans.TextKey);
            }

            RadioSystem.SendTransmission(new RadioTransmission
            {
                PortraitPath = portrait,
                CallSign = callSign,
                RankOrRole = role,
                Text = displayText,
                Duration = jsonTrans.Duration,
                AccentColor = accentColor,
                SoundPath = jsonTrans.Sound ?? "",
                SoundVolume = jsonTrans.Volume
            }, priority: jsonTrans.Priority, syncNetwork: syncNetwork);
        }

        // selects a random transmission id from a pool and triggers it
        public static void PlayRandom(string poolId, bool syncNetwork = true)
        {
            if (registeredPools.TryGetValue(poolId, out var pool) && pool.Count > 0)
            {
                string selectedId = pool[Main.rand.Next(pool.Count)];
                Play(selectedId, syncNetwork);
            }
        }

        // generates dynamic squad callsign with numerical designation
        private static string GetRandomSquadCallsign(List<string> squads)
        {
            string squad = squads[Main.rand.Next(squads.Count)];
            if (squad.Contains("-") && squad.Length > 6) return squad;

            return $"{squad} {Main.rand.Next(1, 6)}-{Main.rand.Next(1, 4)}";
        }

        // parses preset color names or hex codes into xna color
        private static Color ParseColor(string colorInput)
        {
            if (string.IsNullOrWhiteSpace(colorInput)) 
                return Color.LimeGreen;

            string cleanInput = colorInput.Trim().ToLowerInvariant();

            // predefined color aliases
            switch (cleanInput)
            {
                case "red":
                case "danger":
                case "critical":
                    return new Color(255, 35, 35);

                case "toxic":
                case "lime":
                case "green":
                    return new Color(57, 255, 20);

                case "yellow":
                case "warning":
                    return new Color(255, 215, 0);

                case "orange":
                case "amber":
                    return new Color(255, 120, 0);

                case "cyan":
                case "blue":
                    return new Color(0, 220, 255);

                case "white":
                    return Color.White;

                case "purple":
                    return new Color(180, 70, 255);
            }

            // fallback hex parser
            string hex = cleanInput.Replace("#", "");
            if (hex.Length == 6 && uint.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out uint hexVal))
            {
                byte r = (byte)((hexVal >> 16) & 255);
                byte g = (byte)((hexVal >> 8) & 255);
                byte b = (byte)(hexVal & 255);
                return new Color(r, g, b);
            }

            return Color.LimeGreen; // default fallback color
        }
    }
}
