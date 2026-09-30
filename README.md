# Hazardicate: RADIOLIB

![tModLoader v1.4.4](https://img.shields.io/badge/tModLoader-v1.4.4-blue)
![C#](https://img.shields.io/badge/Language-C%23-green)
![Netcode](https://img.shields.io/badge/Multiplayer-MP--Safe-brightgreen)

Lightweight, data-driven framework for tModLoader designed to handle tactical radio communications, CRT-glitched portraits, and localized subtitles via JSON configuration files.

---

## Navigation / Навигация
- [English Guide](#english-guide)
- [Руководство на русском](#руководство-на-русском)

---

<a name="english-guide"></a>
# English Guide

## 1. Mod Setup
Add the library reference to your mod's `build.txt`:
```ini
modReferences = RadioLib
Import the namespace in your .cs files:

C#
using RadioLib.Systems;
2. JSON Configuration
Create a file in your mod directory: Assets/Radio/Dialogues.json.

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
Key Sections
Profiles: Defines transmission senders (Callsigns, Roles, Portrait asset paths, Accent Colors).

Transmissions: Individual entries (Text content, Sound asset path, Duration in ticks: 60 ticks = 1 second).

Pools: Groupings of transmission IDs. The engine automatically picks a random entry from the array.

3. Registering JSON Configs
Load your JSON during mod initialization inside any ModSystem.OnModLoad():

C#
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
The "DefaultAccentColor" and "AccentColor" properties accept both color keywords and RGB hex values:

"Red" / "Danger" — Critical Red

"Toxic" / "Lime" — Acid Green

"Yellow" / "Warning" — Warning Yellow

"Orange" / "Amber" — Tactical Orange

"Cyan" / "Blue" — Cyber Cyan

"Purple" — Purple

"White" — White

"#HEXCODE" (e.g., #FF0055) — Custom RGB Hex

[!IMPORTANT]
Multiplayer Rules: RadioEngine.Play("ID") broadcasts a packet to all clients by default. If invoked inside a local client loop (e.g., projectile or NPC AI() methods running on every machine simultaneously), pass syncNetwork: false:

C#
RadioEngine.PlayRandom("PilotQuotesPool", syncNetwork: false);
Руководство на русском
1. Подключение библиотеки
В файле build.txt вашего мода добавьте зависимость:

Ini, TOML
modReferences = RadioLib
В файлах кода .cs подключите пространство имён:

C#
using RadioLib.Systems;
2. Структура JSON-файла
В папке вашего мода создайте файл по пути: Assets/Radio/Dialogues.json.

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
Описание секций
Profiles: Настройки отправителей (позывной, роль, список портретов, базовый цвет интерфейса).

Transmissions: Конкретные сообщения (текст, путь к звуковому файлу, время отображения в тиках: 60 тиков = 1 секунда).

Pools: Группы сообщений. Движок автоматически выбирает случайную фразу из массива.

3. Регистрация JSON при загрузке мода
В любом классе ModSystem вашего мода вызовите загрузку конфига внутри метода OnModLoad():

C#
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
4. Воспроизведение из C# кода
Для одиночной конкретной фразы:
C#
RadioEngine.Play("BossAwake");
Для случайной фразы из пула:
C#
RadioEngine.PlayRandom("PilotQuotesPool");
Таблица цветов
В полях "DefaultAccentColor" или "AccentColor" поддерживаются текстовые имена и Hex-коды:

"Red" / "Danger" — Красный

"Toxic" / "Lime" — Кислотно-зелёный

"Yellow" / "Warning" — Жёлтый

"Orange" / "Amber" — Оранжевый

"Cyan" / "Blue" — Голубой

"Purple" — Фиолетовый

"White" — Белый

"#HEXCODE" (например #FF0055) — Кастомный цвет

[!IMPORTANT]
Мультиплеер и сетевая синхронизация: Метод RadioEngine.Play("ID") по умолчанию отправляет сетевой пакет всем игрокам. Если вызов происходит внутри метода AI() босса или снаряда, который уже исполняется на каждом клиенте локально, укажите syncNetwork: false:

C#
RadioEngine.PlayRandom("PilotQuotesPool", syncNetwork: false);
