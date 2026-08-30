# ACNHPokerCore for Linux

A from-scratch Avalonia UI port of [ACNHPokerCore](https://github.com/MyShiLingStar/ACNHPokerCore)
(also mirrored at [LeVonikke/ACNHPokerCore](https://github.com/LeVonikke/ACNHPokerCore)) — a
companion tool for *Animal Crossing: New Horizons* that talks to a Nintendo Switch running
[sys-botbase](https://github.com/olliz0r/sys-botbase) over a local TCP socket to read/write
game RAM (spawn items, edit terrain, control turnip prices, manage villagers, etc).

The original is WinForms + GDI+, Windows-only. This repo replaces the UI layer with
[Avalonia](https://avaloniaui.net/) so it runs natively on Linux, while porting the
~52,000 lines of non-UI logic (sys-botbase protocol, item/save-data parsing, RAM address
math) into a UI-agnostic class library largely unchanged.

**This is a multi-session project.** The original app has ~20 windows/dialogs and ~75,000
combined lines of logic + WinForms Designer code; one session ports the foundation and the
first screen. See [TODO](#todo--prioritized-for-future-sessions) below for what's left.

## Status at a glance

| Piece | Status |
|---|---|
| Solution structure (Core + Avalonia UI + tests) | Done |
| sys-botbase TCP protocol layer (`Utilities.cs`) | Ported |
| USB-botbase transport (`USBBot.cs`) | Ported, not wired into the UI |
| Item classification tables (`ItemAttr.cs`) | Ported |
| Custom design pattern decode (GDI+ → SkiaSharp) | Ported |
| Main window shell + IP connect/disconnect | **Working** |
| Main window feature tabs/tools (19 buttons) | Stubbed (click → log message, no behavior) |
| Map, RoadRoller, Bulldozer, Freezer, MapRegenerator, Dodo, Friendship, Chat, Variation, Setting, and 9 smaller dialogs | Not started |
| Emulator RAM-reading mode (kernel32.dll P/Invoke) | Left in Core, throws on Linux — Windows-only, out of scope |
| License/HWID-check dialog (`MyCheck`) | Not ported — out of scope by design (see below) |

## Architecture

```
ACNHPokerCore.Linux.sln
├── src/ACNHPokerCore.Core/        Framework-agnostic class library (net9.0)
│   ├── Protocol/                  sys-botbase TCP + USB transport, error-reporting shims
│   ├── Inventory/                 Item classification + custom design pattern decoding
│   └── Data/csv/                  Item/recipe/flower/variation databases (shipped data)
├── src/ACNHPokerCore.Avalonia/    Avalonia UI app (net9.0, MVVM via CommunityToolkit.Mvvm)
│   ├── Views/                     MainWindow.axaml (+ future screens)
│   └── ViewModels/
└── tests/ACNHPokerCore.Core.Tests/  xUnit tests for the pure parts of Core
```

**Core must never reference `System.Windows.Forms` or `System.Drawing.Common`.** That
boundary is what makes it portable and unit-testable; it's enforced by convention (no
build-time guard yet — a future session could add one, e.g. a Roslyn analyzer rule or a
simple `grep` in CI) rather than by a hard framework restriction, since the library still
targets plain `net9.0` (not `net9.0-windows`).

### Key decisions made this session

- **SkiaSharp instead of System.Drawing.Common.** `DesignPattern.cs` (decodes a custom
  clothing/flag pattern from save data into a bitmap) was the one piece of "pure" logic
  that used GDI+ (`Bitmap`, `Graphics`, `ImageAttributes`). Rewritten against SkiaSharp,
  which Avalonia already depends on for its own rendering — no extra native dependency on
  Linux. This is the template to follow for the Map/mini-map rendering code in a future
  session (`Map/miniMap.cs`, `Map/map.cs` — currently untouched GDI+).
- **MessageBox/MyMessageBox shims instead of a UI framework dependency.** The original
  `Utilities.cs` and `USBBot.cs` report protocol errors via ~100
  `System.Windows.Forms.MessageBox.Show(...)` / `MyMessageBox.Show(...)` calls. Rather than
  rewrite every call site, `Protocol/MessageBoxShim.cs` defines drop-in replacement types
  with the same call surface, in the `ACNHPokerCore.Core` namespace, that raise a C# event
  instead of drawing a dialog. `MainWindowViewModel` subscribes and appends to the on-screen
  log. A future session porting a themed error dialog (the original's
  `Custom/MyMessageBox.cs`, ~795 lines of GDI+) can just add another subscriber.
- **Avalonia pinned to 12.0.0, not the newest 12.1.1.** 12.1.1's Roslyn source-generator
  analyzer requires a newer C# compiler (4.14) than the currently-installed .NET 9 SDK
  ships (4.12), which silently breaks the `InitializeComponent()` codegen for `.axaml`
  files (no build error — the method just doesn't exist). Re-check this once a .NET 9 SDK
  servicing update lands, or when the project eventually moves to .NET 10.
- **Avalonia.Diagnostics (dev-time F12 inspector) left out.** No 12.x build exists on
  NuGet yet (latest is 11.3.20, incompatible with the 12.0.0 core packages). Commented out
  in `ACNHPokerCore.Avalonia.csproj` with instructions to re-add once available.
- **MVVM (CommunityToolkit.Mvvm) instead of code-behind.** The original is pure WinForms
  event handlers. This is a deliberate addition — it keeps `MainWindowViewModel` unit
  -testable independent of any Avalonia control tree, which matters a lot once the Map
  screen's drag-and-drop grid logic gets ported.
- **Windows-only emulator RAM reading left in Core but inert on Linux.** Five
  `kernel32.dll` P/Invoke calls (`OpenProcess`/`ReadProcessMemory`/`WriteProcessMemory`/
  `VirtualQueryEx`/`GetSystemInfo`) back an optional "read an emulator's process memory
  directly" mode, gated by `Utilities.isEmulator` (always `false` unless something sets
  it). They compile fine on Linux and now throw a clear `PlatformNotSupportedException`
  instead of a `DllNotFoundException` if ever reached. The Avalonia UI has no control that
  sets `isEmulator = true`, so this is unreachable in practice — exactly what the task
  brief asked for (stub it out rather than reimplement it).
- **Backslash path literals fixed.** `Utilities.cs` hardcoded `csv\`, `save\`, `img\`,
  `villager\`, `BridgeImage\` as folder prefixes. Forward slashes work as a path separator
  on both Linux and Windows in .NET, so they were changed rather than wrapped in
  `Path.Combine` everywhere those constants get concatenated — smaller diff, same result.
- **IP validation uses `IPAddress.TryParse`, not the original's regex.** Simpler, and
  correctly accepts IPv6 too (sys-botbase itself is IPv4-only in practice, but there's no
  reason to reject a valid address string on that basis).

### What was *not* ported, on purpose

- **`Custom/MyCheck.cs`** — a HWID/answer-key licensing-check dialog. Out of scope per this
  project's instructions: not removed, not replicated, not touched.
- **VillagerDatabase/** (~39MB of `.nhv2`/`.nhvh2` binary villager portrait/dialogue data)
  was not copied into this repo. It's only needed once the Villager/Friendship screen is
  ported — copy it from the upstream repo's `ACNHPokerCore/VillagerDatabase/` at that point.
- **DiscordWebhook and Twitch sub-projects** (optional integrations in the original
  solution) — not evaluated yet. Low priority; see TODO.

## Building and running on Linux

Requires the .NET 9 SDK (`dotnet --version` should show `9.0.1xx`).

```bash
git clone git@github.com:LeVonikke/ACNHPokerCore-Linux.git
cd ACNHPokerCore-Linux
dotnet build                                          # builds Core + Avalonia UI + tests
dotnet test tests/ACNHPokerCore.Core.Tests            # 14/14 passing as of this commit
dotnet run --project src/ACNHPokerCore.Avalonia       # launches the app
```

No native dependencies beyond what NuGet restores (SkiaSharp ships Linux-x64 native
binaries; LibUsbDotNet needs `libusb-1.0` on the system if you actually use the USB
transport — untested on Linux so far, see TODO).

To connect: enter your Switch's IP address (Switch: **System Settings → Internet →
Connection Status**) and press **Connect**. This requires a Switch running Atmosphère/CFW
with sys-botbase installed. Everything else in the window is a labeled stub.

## TODO — prioritized for future sessions

Ordered roughly by (screen importance × how much of it is GDI+-drawing-heavy vs.
straightforward data entry). Line counts are from the original WinForms `.cs` files
(excluding `.Designer.cs`) and are a rough proxy for effort.

1. **Map / mini-map (`Map/map.cs` 8,083 + `Map/miniMap.cs` 2,014 + `Map/TerrainUnit.cs`
   4,711 lines).** The centerpiece feature — hand-drawn GDI+ minimap, drag-and-drop terrain
   editing, item placement grid. Port the drawing to Avalonia's `DrawingContext` or
   SkiaSharp (see `DesignPattern.cs` in this repo for the pattern). This is the single
   biggest remaining chunk of work and probably deserves its own session.
2. **Main window's remaining tabs** (Inventory/Villager/Critter/Other — currently just
   button stubs in `MainWindow.axaml`). Inventory in particular needs the drag-and-drop
   item grid (`Inventory/inventorySlot.cs`, 514 lines) and depends on `Data/csv/items.csv`
   (already copied into Core).
3. **RoadRoller (`Map/RoadRoller.cs`, 3,898 lines)** and **Bulldozer
   (`Map/bulldozer.cs`, 2,157 lines)** — terrain-editing tools, GDI+-heavy, similar
   patterns to the mini-map.
4. **MapRegenerator (`Map/MapRegenerator.cs`, 1,945 lines)** and **Freezer
   (`Map/Freezer.cs`, 1,070 lines)**.
5. **Dodo Helper (`Dodo/dodo.cs` 2,330 + `Dodo/controller.cs` 795 + `Dodo/teleport.cs`
   405 lines)** — Dodo code / island-hopping helper.
6. **Friendship (`Villager/Friendship.cs`, 255 lines)**, **Chat (`Custom/Chat.cs`,
   183 lines)**, **Variation (`Custom/variation.cs`, 414 lines)** — smaller, good
   "next" screens once the Map work establishes the drawing pattern.
7. **Setting (`Setting/Setting.cs`, 435 lines)** — app settings dialog; note the original
   uses `System.Configuration`/`App.config`, which will need an Avalonia-friendly
   settings store (e.g. plain JSON file) since WinForms `ConfigurationManager` conventions
   don't carry over cleanly.
8. **Remaining small dialogs**: `Map/bulkSpawn.cs` (741), `Map/bulkList.cs` (501),
   `Map/variationSpawn.cs` (591), `Map/Filter.cs` (50), `Custom/MyWarning.cs` (103),
   `Custom/OrderDisplay.cs` (35), `Custom/ImgRetriever.cs` (158).
9. **USBBot wiring** — `Protocol/USBBot.cs` is ported and compiles, but nothing in the
   Avalonia UI creates or uses a `USBBot` instance yet (only the TCP path is wired up in
   `MainWindowViewModel`). Needs a UI toggle plus testing against real USB-botbase
   hardware/`libusb` on Linux.
10. **DiscordWebhook / Twitch sub-projects** — not yet looked at; evaluate whether they
    have WinForms/System.Drawing dependencies before porting.
11. **NAudio sound effects** — the original plays sound cues via NAudio (Windows-focused
    audio library, though it does have some cross-platform support via MediaFoundation
    alternatives). Evaluate a cross-platform replacement (e.g. an Avalonia-compatible
    audio library) when a screen that needs it comes up.
12. Build-time guard so `ACNHPokerCore.Core` can never accidentally gain a
    `System.Windows.Forms`/`System.Drawing.Common` reference again (currently just a
    convention, noted in the Architecture section above).

## License

BSD 2-Clause, carried forward unmodified from the upstream project — see [LICENSE](LICENSE).
Copyright (c) 2022, MyShiLingStar. This repository is a derivative work (a UI port) of
[MyShiLingStar/ACNHPokerCore](https://github.com/MyShiLingStar/ACNHPokerCore); all original
attribution is preserved.
