# Windows Tetris

A native Windows desktop Tetris built with **.NET / WPF** and a modern Fluent
(dark) UI. It re-implements the feature set of the original Unity
[Tetris](https://github.com/madmerger/Tetris) project as a lightweight,
dependency-free desktop application.

## Features

Mirrors the gameplay of the original Unity project:

- **Stage & Infinite modes** — Infinite is classic endless play; Stage mode loads
  20 hand-crafted layouts where you must clear every **gem** to advance.
- **Scoring** — 40 / 100 / 300 / 1200 points for 1–4 line clears, multiplied by
  the current level.
- **Next-piece preview** — see the upcoming tetromino.
- **Ghost piece** — shows where the current piece will land.
- **Hard drop & soft drop** — Space drops instantly; Down accelerates the fall.
- **Levels** — the fall speed increases as you clear more lines.
- **Background music & sound effects** — the classic BGM plus lock / clear / menu
  effects (the original audio assets, with a mute toggle).
- **Pause / resume, restart and a start menu.**

Implementation extras for a modern feel: a 7-bag randomiser, simple rotation
wall-kicks, and an on-screen play timer.

## Controls

| Key | Action |
| --- | --- |
| ◀ / ▶ | Move left / right |
| ▲ (or X) | Rotate clockwise |
| ▼ | Soft drop |
| Space | Hard drop |
| P | Pause / resume |
| R | Restart |
| M | Mute / unmute |

## Requirements

- Windows 10 / 11
- [.NET 10 SDK](https://dotnet.microsoft.com/download) (the project targets
  `net10.0-windows`)

## Build & run

```powershell
# from the repository root
dotnet run --project src/WindowsTetris -c Release
```

To produce a standalone build:

```powershell
dotnet build -c Release
# output: src/WindowsTetris/bin/Release/net10.0-windows/WindowsTetris.exe
```

## Tests

The game logic (`GameEngine`, tetromino rotations, stage data) is covered by unit
tests that run without a display:

```powershell
dotnet test
```

## Project layout

```
src/WindowsTetris/
  Game/            # UI-independent game logic
    GameEngine.cs  # board, gravity, scoring, line/gem clearing
    Tetromino.cs   # the seven pieces and their rotations
    Stages.cs      # the 20 Stage-mode layouts (ported from the Unity project)
    GameMode.cs
  Audio/
    SoundManager.cs
  Assets/Audio/    # BGM and sound effects (from the original project)
  MainWindow.xaml  # Fluent dark UI + rendering
tests/WindowsTetris.Tests/
```

## Credits

Gameplay and audio assets are based on the original Unity
[Tetris](https://github.com/madmerger/Tetris) project.
