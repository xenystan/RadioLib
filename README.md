Hazardicate: RADIOLIB — Developer Guide
RadioLib is a lightweight data-driven framework for tModLoader designed to render tactical radio communications, glitched portraits, and localized subtitles. All content is managed via JSON configuration files without requiring C# recompilation.

1. Mod Setup
Add the library reference to your mod's build.txt:

Ini, TOML
modReferences = RadioLib
Import the namespace in your .cs files:

C#
using RadioLib.Systems;
2. JSON Structure
Create a file inside your mod directory: Assets/Radio/Dialogues.json.

Configuration layout:

JSON
{
  "Profiles": {
    "MyBoss": {
      "CallSign": "XT-017 SCORPION",
      "RankOrRole": "HEAVY GUNSHIP",
      "DefaultAccentColor": "Red"
    },
    "MyPilot": {
      "SquadNames": ["REAPER", "NOMAD", "GHOST"],
      "Roles": ["TACTICAL CAS", "PILOT"],
      "Portraits": [
        "MyMod/Portraits/Pilot1",
        "MyMod/Portraits/Pilot2"
      ],
      "DefaultAccentColor": "Toxic"
    }
  },

  "Transmissions": {
    "BossAwake": {
      "Profile": "MyBoss",
      "Text": "Target locked. System online.",
      "Sound": "MyMod/Sounds/BossAwake",
      "Duration": 180
    },
    "PilotAirstrike": {
      "Profile": "MyPilot",
      "Text": "Airstrike inbound!",
      "Sound": "MyMod/Sounds/AirstrikeQuote",
      "Duration": 240
    }
  },

  "Pools": {
    "PilotQuotesPool": [ "PilotAirstrike" ]
  }
}
Key Sections:
Profiles: Defines transmission senders (Callsigns, Roles, Portrait asset paths, Default Accent Colors).

Transmissions: Individual entries (Text content, Sound asset path, Duration in ticks: 60 ticks = 1 second).

Pools: Groupings of transmission IDs. The engine automatically picks a random transmission from the specified pool.

3. Registering JSON Configs
Load your JSON during mod initialization inside any ModSystem.OnModLoad():

C#
// MP-Safe
using Terraria.ModLoader;
using RadioLib.Systems;

namespace MyMod.Systems
{
    public class RadioInitSystem : ModSystem
    {
        public override void OnModLoad()
        {
            RadioEngine.LoadJson(Mod, "Assets/Radio/Dialogues.json");
        }
    }
}
4. Triggering Transmissions in Code
Play a specific transmission:
C#
RadioEngine.Play("BossAwake");
Play a random transmission from a pool:
C#
RadioEngine.PlayRandom("PilotQuotesPool");
Color Reference
The "DefaultAccentColor" and "AccentColor" properties accept both color keywords and hex values:

"Red" / "Danger" — Red

"Toxic" / "Lime" — Acid Green

"Yellow" / "Warning" — Yellow

"Orange" / "Amber" — Orange

"Cyan" / "Blue" — Cyan

"Purple" — Purple

"White" — White

"#HEXCODE" (e.g., #FF0055) — Custom RGB Hex

Multiplayer Syncing Rules
RadioEngine.Play("ID") automatically broadcasts a packet to all clients by default.

If invoked inside a local client loop (such as projectile or NPC AI() methods running independently on each machine), pass syncNetwork: false:

C#
RadioEngine.PlayRandom("PilotQuotesPool", syncNetwork: false);
