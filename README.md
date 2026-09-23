# Mists of Krakovia

**Mists of Krakovia** is the official game client for **Krakovia**, an experimental MMORPG built around a custom client-server architecture. The client is developed in **Unity 6 (URP)** and interfaces with a custom authoritative C# / .NET backend.

[▶️ Watch Krakovia in action](https://youtu.be/10xy_52HHik?t=820)

This repository contains the Unity project responsible for game rendering, client-side prediction, entity interpolation, user interface, input handling, audio, visual effects, and network synchronization with the **Krakovia Server**.

> **Project status:** Experimental / Game Jam project
> **Development time:** 30 days
> **Engine:** Unity 6 (6000.6.2f1)
> **Render Pipeline:** Universal Render Pipeline (URP)
> **Server:** C# / .NET 9 — [Krakovia Server Repository](https://github.com/Nnayuta/KrakoviaServer)

---

## Overview

Mists of Krakovia was built to explore the challenges of creating a responsive, stylized 3D MMORPG client that communicates directly with a standalone, authoritative game server rather than using high-level Unity multiplayer frameworks (like Netcode for GameObjects or Mirror).

The client handles player input, smooth movement interpolation for remote entities, local feedback for combat abilities, UI state management, and real-time environment rendering while deferring world state authority, hit validation, inventory management, and quest progression entirely to the server.

---

## Architecture

```text
 ┌──────────────────────────────────────────────────────────┐
 │               Mists of Krakovia (Unity 6)                │
 ├────────────────────────────┬─────────────────────────────┤
 │         Rendering          │           Gameplay          │
 │   - URP Custom Shaders     │   - Player Controller       │
 │   - VFX & Particle Systems │   - Animation State Machine │
 │   - Post-Processing        │   - Target & Combat System  │
 ├────────────────────────────┼─────────────────────────────┤
 │        User Interface      │          Networking         │
 │   - Action Bars & Hotkeys  │   - TCP Client (Auth/Data)  │
 │   - Inventory & Equipment  │   - UDP Client (State Sync) │
 │   - Quest Log & Dialogue   │   - Entity Interpolator     │
 └─────────────┬──────────────┴──────────────┬──────────────┘
               │                             │
         TCP (Port 7778)               UDP (Port 7777)
         Reliable Stream              Unreliable Datagrams
               │                             │
               └──────────────┬──────────────┘
                              ▼
               ┌─────────────────────────────┐
               │       Krakovia Server       │
               │         C# / .NET 9         │
               └─────────────────────────────┘
```

---

## Features

### Networking & Synchronization
* **Dual-Protocol Communication**:
  * **TCP**: Handles authentication, character selection, inventory operations, equipment changes, quest tracking, vendor transactions, and chat messages.
  * **UDP**: Transmits high-frequency player movement, rotation, ability triggers, entity updates, and combat events.
* **Entity Interpolation**: Smooths position and rotation updates from the server for other players, monsters, and NPCs to eliminate jitter across varying network latencies.
* **Network Environment Profiles**: ScriptableObject-based network profiles (`Dev_NetworkConfig.asset` and `Prod_NetworkConfig.asset`) for seamless switching between local development (`127.0.0.1`) and remote production servers.

---

### Character & Combat Systems
* **Responsive Third-Person Controller**: Smooth character movement, strafing, jump physics, and rotation based on the new Unity Input System.
* **Targeting System**: Tab-targeting and mouse selection for hostile NPCs, friendly players, gatherable nodes, and interactive objects.
* **Action Bar & Ability Dispatcher**: Keybound hotbars supporting instant casts, cooldown indicators, cast bars, and resource consumption feedback.
* **Animation State Machine**: Multi-layer humanoid animation blending for locomotion, combat stances, spellcasting, taking damage, and death states.
* **Floating Combat Text & Unit Frames**: Dynamic overhead health bars, resource gauges, status effect indicators, and animated damage/healing numbers.

---

### UI & Inventory Management
* **Grid-Based Inventory**: Drag-and-drop item management, stacking, tooltips, and item usage.
* **Equipment System**: Visual paperdoll equipment slots with instant stat synchronization.
* **Interactive Dialogue & Quests**: NPC interaction windows, branching dialogues, quest acceptance, progress tracking, and reward redemption.
* **Merchant & Vendor Interface**: Buy and sell interface with real-time currency calculation.
* **Responsive HUD**: Minimap, world map, player status frames, target frames, and customizable settings menu.

---

### World & Visual Design
* **Stylized Low-Poly Aesthetics**: Harmonious color palettes, customized lighting bakes, stylized water shaders, and atmospheric fog.
* **Modular Environments**: Biomes including forests, ruins, deserts, towns, and dungeon areas.
* **Visual Effects (VFX)**: Particle systems and shader graph effects for spell impacts, weapon trails, status buffs, and gathering nodes.
* **Dynamic Audio Engine**: Positional 3D audio, surface-dependent footstep sound effects, ambient soundscapes, and orchestral fantasy soundtracks.

---

## Project Structure

```text
Mists of Krakovia/
├── Assets/
│   ├── Editor/               # Custom editor tools & world data exporters
│   ├── Prefabs/              # Character, monster, UI, and world prefabs
│   ├── Resources/            # Dynamically loaded runtime assets
│   ├── Scenes/               # Game scenes (Login, CharacterSelect, WorldMap, etc.)
│   ├── Scripts/
│   │   ├── Audio/            # Sound management and surface footstep controllers
│   │   ├── Data/             # Data models and serializable structures
│   │   ├── Input/            # Unity Input System integration
│   │   ├── Items/            # Client-side item data and inventory handlers
│   │   ├── Managers/         # Core singletons (GameManager, UIManager, SoundManager)
│   │   ├── Network/          # TCP/UDP network clients and packet handlers
│   │   ├── NPC/              # Client-side NPC renderers and nameplates
│   │   ├── Player/           # Movement, camera, targeting, and animation controllers
│   │   ├── Quests/           # Quest UI and quest progression tracking
│   │   ├── UI/               # Windows, HUD, hotbars, tooltips, and menus
│   │   └── World/            # Interactive objects, gatherables, and spawn helpers
│   ├── Settings/             # URP render pipeline settings and quality presets
│   └── Shaders/              # Custom URP shaders (outlines, stencil, terrain)
├── Packages/                 # Unity Package Manager manifest and dependencies
├── ProjectSettings/          # Unity project settings (Input, Tags, Physics, Graphics)
├── .gitattributes            # Git LFS tracking rules for binary assets
└── .gitignore                # Unity-optimized ignore file
```

---

## Prerequisites & Getting Started

### Prerequisites
1. **Git & Git LFS**: Ensure [Git LFS](https://git-lfs.com/) is installed before cloning (mandatory for textures, 3D models, and lighting assets).
2. **Unity Version**: **Unity 6 (6000.6.2f1)** installed via [Unity Hub](https://unity.com/download).
3. **Krakovia Server**: The server backend must be running for multiplayer and world features.

### Cloning the Repository
```bash
# 1. Initialize Git LFS on your system
git lfs install

# 2. Clone the repository
git clone https://github.com/your-username/mists-of-krakovia.git

# 3. Navigate into the project folder
cd mists-of-krakovia

# 4. Pull LFS files (if not downloaded automatically)
git lfs pull
```

### Opening in Unity
1. Open **Unity Hub**.
2. Click **Add** -> **Add project from disk**.
3. Select the `Mists of Krakovia` folder.
4. Select Unity Editor version **6000.6.2f1**.
5. Allow Unity to import packages and build the local `Library/` cache.
6. Open `Assets/Scenes/LoginScene.unity` or `Assets/Scenes/GameScene.unity`.
7. Configure network settings in `Assets/Scripts/Dev_NetworkConfig.asset` if connecting to a local server (`127.0.0.1`).

---

## Technology Stack

| Component | Technology |
|---|---|
| **Game Engine** | Unity 6 (6000.6.2f1) |
| **Render Pipeline** | Universal Render Pipeline (URP) |
| **Input System** | Unity New Input System (`com.unity.inputsystem`) |
| **Networking** | Custom C# Sockets (TCP & UDP) |
| **Serialization** | JSON (Newtonsoft.Json & Unity JsonUtility) |
| **Version Control** | Git + Git LFS (Large File Storage) |

---

## Development Context

This project was developed during a **30-day MMORPG challenge**. The goal was to build a fully functional, playable prototype demonstrating real-time client-server architecture, authoritative world simulation, and responsive MMORPG mechanics in a constrained timeframe.

Due to the rapid development cycle:
* Some systems prioritize gameplay delivery and prototyping speed over exhaustive unit test coverage.
* The repository serves as an **engineering portfolio prototype** and proof-of-concept for custom multiplayer network integration in Unity.

---

## License

This project is provided for portfolio and educational purposes. See the repository for current licensing terms.
