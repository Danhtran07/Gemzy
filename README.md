# Gemzy

Gemzy is a lightweight pixel-art match-3 game built with Unity. Swap adjacent jewels, create chains of 3 or more, trigger combos, and reach the target score before running out of moves.

## Media

Add your gameplay screenshot here:

```md
![Gemzy gameplay screenshot](docs/media/gemzy-screenshot.png)
```

Add your gameplay demo video here:

```md
[![Gemzy gameplay demo](docs/media/gemzy-video-cover.png)](https://github.com/user-attachments/assets/your-video-id)
```

## Gameplay

- Match 3 or more jewels in a row or column.
- Each valid swap consumes 1 move.
- Matched jewels pop with a sparkle effect, then new jewels drop in.
- Combo chains multiply the score.
- Win by reaching `2500` points within `30` moves.
- If no moves are available, the board shuffles automatically.

## Features

- Classic 8x8 match-3 board.
- Animated pixel gem sprites.
- Spark effect on matched jewels.
- Mobile-friendly portrait layout with safe-area support.
- Touch and mouse input support through Unity Input System.
- Pixel UI font from the included Thaleah font asset.
- Editor setup tool to build the scene hierarchy once, instead of generating the full UI every time Play starts.
- Build tools for Windows x64 and Android APK.

## Tech Stack

- Unity `6000.4.6f1`
- Universal Render Pipeline
- Unity Input System
- Unity UI
- Pixel Art Gem Pack assets
- Thaleah Pixel Font

## Project Structure

```text
Assets/
  Editor/
    GemzySetupWindow.cs
    GemzyBuildTool.cs
    GemzyAssetImporter.cs
  Resources/
    GemAnimations/
    Effects/Spark/
    Gems/
  Scenes/
    Gemzy.unity
  Scripts/
    GemzyMatchGame.cs
    GemzyMatchGame.Board.cs
    GemzyMatchGame.UI.cs
    GemzyMatchGame.Tile.cs
    GemzyMatchSpriteAnimation.cs
  Thaleah_PixelFont/
```

## Getting Started

1. Open the project in Unity `6000.4.6f1` or newer Unity 6 version.
2. Open the scene:

```text
Assets/Scenes/Gemzy.unity
```

3. Run the setup tool from the Unity menu:

```text
Gemzy > Setup Window > Build Game Objects Into Hierarchy
```

4. Press Play.

## Build

Use the Unity menu:

```text
Gemzy > Build > Windows x64
Gemzy > Build > Android APK
```

Default build outputs:

```text
Builds/Windows/Gemzy.exe
Builds/Android/Gemzy.apk
```

## Controls

- Mouse: click a jewel, then click an adjacent jewel to swap.
- Touch: tap a jewel, then tap an adjacent jewel to swap.
- Restart: use the in-game Restart button.

## Notes

The game scene is designed to be prepared through the editor setup tool. Runtime code reuses the scene hierarchy and only resets gameplay state when Play starts.
