# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a Unity 3D multiplayer implementation of the Coup board game. The project uses Unity 6000.0.53f1 and is configured with the Universal Render Pipeline (URP) template.

## Key Dependencies

- **Unity Multiplayer Center** (1.0.0) - Central multiplayer development tools
- **Unity Input System** (1.14.0) - Modern input handling
- **Universal Render Pipeline** (17.0.4) - Graphics rendering pipeline
- **AI Navigation** (2.0.8) - NavMesh system for potential AI players
- **Visual Scripting** (1.9.7) - Node-based scripting system

## Project Structure

The project follows Unity's standard structure with URP configuration:

- `Assets/Scenes/` - Game scenes (currently contains SampleScene)
- `Assets/Settings/` - URP render pipeline assets and volume profiles
- `Assets/Scripts/` - C# scripts (currently minimal with tutorial scripts)
- `Assets/InputSystem_Actions.inputactions` - Input action mappings
- `ProjectSettings/` - Unity project configuration
- `Packages/manifest.json` - Package dependencies

## Development Architecture

### Multiplayer Implementation Strategy
This project is designed to implement Coup as a turn-based multiplayer game with the following considerations:

- **Turn-based networking** - Actions must be validated server-side
- **Hidden information** - Each player's cards must remain private
- **Real-time communication** - Challenge/block responses need immediate handling
- **Game state synchronization** - All players must see consistent game state

### Rendering Pipeline
The project uses URP with separate render pipeline assets for PC and Mobile platforms, allowing for platform-specific optimizations.

### Input System
Configured with Unity's Input System for modern input handling, suitable for both local and networked input management.

## Game-Specific Considerations

### Coup Game Rules Implementation
- 2-6 players with hidden role cards
- Turn-based actions (Income, Foreign Aid, Coup, Character abilities)
- Bluffing mechanics requiring challenge/block validation
- Win condition: Last player with influence cards

### Networking Challenges
- **Authority model** - Server authoritative for game logic
- **State validation** - All actions must be validated against current game state
- **Timing windows** - Challenge/block responses need time limits
- **Reconnection** - Handle player disconnections gracefully

## Development Commands

Since this is a Unity project, development is primarily done through the Unity Editor. However, for automated builds or CI/CD:

- Build the project using Unity's command line interface
- Use Unity Test Runner for automated testing
- Package builds can be automated through Unity Cloud Build or local scripts

## Architecture Notes

The current project structure is minimal (URP template), but for Coup implementation consider:

- **Game Manager** - Central authority for game state and rules
- **Network Manager** - Handle client-server communication
- **Card System** - Manage character cards and abilities
- **Player Controller** - Handle local and remote player actions
- **UI System** - Game interface optimized for multiplayer interaction
- **Audio Manager** - Sound effects and music management

The multiplayer architecture should use a client-server model where the server maintains the authoritative game state and validates all player actions, especially critical for a bluffing game like Coup where hidden information and rule enforcement are essential.