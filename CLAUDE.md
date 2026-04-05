# stride-hytale-admin

Stride Engine (C#/.NET 10) desktop admin tool for a Hytale game server. Replaces the `../hytale-map/` Hugo static site with a native 2D UI editor.

## Build & Run

```bash
dotnet build
dotnet run
```

Target: `linux-x64`, OpenGL. Uses `Stride.CommunityToolkit.Linux` and `Stride.CommunityToolkit.Bepu`.

## Architecture

### Entry Point

`Program.cs` — creates `Game`, calls `AddGraphicsCompositor().AddCleanUIStage()` (UI-only, no 3D scene), initializes `UIHelper`, then loads `EditorScene` via `AppManager` → `SceneManager`.

### Core

- **`AppManager`** — creates all services (`ServiceContainer`), transitions to `EditorScene`
- **`SceneManager`** — manages scene load/unload with optional fade transitions
- **`ServiceContainer`** — DI container: `ApiClient`, `MapData`, `EntityData`, `Selection`, `Config`, `Game`

### Services

- **`HytaleApiClient`** — HTTP client to the Hytale plugin REST API (default `localhost:8080`). Emits `StatusChanged` events.
- **`MapDataService`** — caches surface blocks in `Dictionary<(int x, int z), BlockCell>`. Fires `MapUpdated` on merge. Accumulated across multiple loads.
- **`EntityDataService`** — polls players, entities, sound zones on a timer. Uses `_pendingUpdate` flag to defer scene mutations to main thread via `FlushOnMainThread()`.
- **`SelectionService`** — tracks `SelectedAsset`, `SelectedEntity`, `SelectedPlayer`, `SelectedZone`, `HoveredBlock`. Fires `SelectionChanged`.

### Rendering (Pure 2D UI)

All rendering is done via Stride UI elements — no 3D scene, camera, or mesh primitives.

- **`MapRenderer`** — generates `Texture.New2D` from block data with height-shaded colors, displayed via `ImageElement` + `SpriteFromTexture`. Owns pan/zoom state and a root `Canvas`. Provides `ScreenToWorld()`/`WorldToScreen()` for 2D coordinate conversion. Exposes an `OverlayCanvas` for entity markers and selection shapes.
- **`EntityRenderer`** — colored `Grid` dots (red=players, blue=NPCs) and semi-transparent rectangles (teal=sound zones) positioned on the map's `OverlayCanvas`. `TextBlock` labels above each marker. Uses `MapRenderer.WorldToScreen()` for positioning.
- **`SelectionRenderer`** — hover highlight (red `Grid` snapped to block grid) and area selection (teal `Grid` for shift+drag), positioned via `MapRenderer.WorldToScreen()`.

### UI

All UI lives in a single full-screen `UIComponent` entity. Layout is a `Grid` with:
- Row 0: `HeaderBar` (config inputs, load button, filter/refresh controls, status)
- Row 1: 3-column grid — left `AssetBrowserPanel` (280px), center `MapRenderer.RootCanvas` (map viewer with `ClipToBounds`), right `InspectorPanel` (280px)

- **`UIHelper`** — static factory for TextBlock, Button, EditText, StatBar, TabButton, ScrollViewer, Panel. Manages m5x7.ttf pixel font.
- **`EditorUI`** — master layout builder, wires panels and `MapRenderer` into layout, exposes `SetStatus()`, `UpdateHoverInfo()`.
- **`AssetBrowserPanel`** — 5 tabs (Blocks/Items/Prefabs/NPCs/Sounds), search filter, collapsible group tree, selection highlighting.
- **`InspectorPanel`** — shows selected entity/player/zone details with stat bars (health=red, stamina=yellow, mana=blue, oxygen=teal).
- **`HeaderBar`** — EditText for WorldId/X/Z/Radius, Load Map button, entity filter toggle buttons, refresh rate buttons, status + hover text.
- **`FadeOverlay`** — scene transition fade (currently unused for editor).

### Input

- `InputMap` — key bindings: Enter=LoadMap, Escape=Cancel/Deselect, Shift=modifier for area selection
- Left-click: pan (drag) or place asset / select entity (click)
- Shift+left-drag: area selection for ambient sound zones
- Scroll wheel: zoom

### Models

**API DTOs** (`Models/Api/`): `SurfaceResponse`, `PlayerDto`, `EntityDto` (with `EntityStatsDto`/`StatValue`), `SoundZoneDto`, `PlaceRequest`/`PlaceResponse`, `SoundPlayRequest`/`SoundAmbientRequest`/`SoundResponse`.

**Domain** (`Models/Domain/`): `BlockCell` (x/z/y/block/rgb), `EditorConfig` (api url, world, center, radius, refresh, filter), `SelectedAsset` (category, id).

## Key Patterns

### Thread Safety
`EntityDataService` polls on a `System.Threading.Timer` (thread pool). Scene mutations must happen on the main thread. The service sets `_pendingUpdate = true`, and `FlushOnMainThread()` is called from `EditorScene.Update()` to fire `DataUpdated` safely.

### Stride UI Quirks
- No built-in ComboBox/Dropdown or TabControl — composed from Button rows with toggled `BackgroundColor`.
- `Grid` has no `Padding` property — use `Margin` on children instead.
- `Thickness` constructor requires 4 args `(left, top, right, bottom)`, not 1.
- Transparent overlays need both `BackgroundColor = Color.Transparent` and `CanBeHitByUser = false` for click-through.
- Multiple `UIComponent` entities with `IsFullScreen = true` stack; only one should capture input for a given area.

### REST API Endpoints (consumed from Hytale plugin at `../hytale-plugin/`)

| Method | Endpoint | Purpose |
|--------|----------|---------|
| GET | `/api/health` | Health check |
| GET | `/api/server/info` | Server info |
| GET | `/api/players` | Connected players (paginated) |
| GET | `/api/players/{uuid}` | Single player detail |
| GET/POST | `/api/players/{uuid}/stats` | Player stats get/set |
| POST | `/api/players/{uuid}/teleport` | Teleport player |
| POST | `/api/players/{uuid}/message` | Send message |
| POST | `/api/players/{uuid}/kick` | Kick player |
| GET | `/api/entities?world=&type=` | NPCs/entities (paginated) |
| GET | `/api/entities/types?world=` | Entity type list |
| GET | `/api/entities/{uuid}` | Single entity detail |
| DELETE | `/api/entities/{uuid}` | Remove entity |
| POST | `/api/entities/spawn` | Spawn NPC |
| GET/POST | `/api/entities/{uuid}/stats` | Entity stats get/set |
| GET | `/api/entities/{uuid}/role` | Entity role config |
| POST | `/api/entities/{uuid}/teleport` | Teleport entity |
| GET | `/api/worlds` | List worlds |
| GET | `/api/worlds/{id}` | World info |
| GET | `/api/worlds/{id}/surface?x=&z=&radius=` | Terrain blocks |
| GET/POST | `/api/worlds/{id}/blocks` | Block get/set |
| GET | `/api/worlds/{id}/chunks` | Chunk info |
| POST | `/api/worlds/{id}/prefabs` | Place prefab |
| GET | `/api/assets` | Full asset catalog |
| GET | `/api/assets/blocks/{id}` | Block detail |
| GET | `/api/assets/items/{id}` | Item detail |
| GET | `/api/assets/npcs/{name}` | NPC role detail |
| GET | `/api/assets/models` | Model listing |
| GET | `/api/assets/search?q=` | Asset search |
| GET | `/api/sound/list` | Sound catalog |
| GET | `/api/sound/categories` | Sound categories |
| GET | `/api/sound/zones?world=` | Active sound zones |
| POST | `/api/sound/play` | Play one-shot sound |
| POST | `/api/sound/ambient` | Create ambient zone |
| POST | `/api/sound/ambient/stop` | Stop ambient zone(s) |
| POST | `/api/place` | Place asset (legacy) |
| GET | `/api/commands/list` | List commands |
| POST | `/api/commands/execute` | Execute command |

## Generic Reusable Components

These components are built with SOLID principles and should be reused/extended rather than duplicated:

### Node Editor (`UI/NodeEditor/`)
- `NodeEditor<TNode>` — generic graph canvas with pan/zoom, bezier links, drag-to-connect
- `INode` — minimal interface: Id, NodeType, Title, Subtitle, Position, Ports
- `IGraphBuilderStrategy` — builds graph definitions from plugin schemas dynamically
- `SchemaGraphBuilder` — default strategy reading `graphHints` from schema
- `IContextMenuProvider<TNode>` — externally defined right-click menus
- `PortTypeMap` — type-safe connection rules between port types
- `NodeStyle` — per-node-type visual config
- `GraphLayoutState` — persistence of node positions + pan/zoom to `~/.hytale-admin/`

### Tree View (`UI/Components/`)
- `TreeView<TItem>` — generic tree with search, selection, expand/collapse, context menu
- `ITreeDataProvider<TItem>` — strategy for mapping any data to tree hierarchy
- `ITreeContextMenu<TItem>` — right-click menu for tree items

### Map Picker (`UI/Components/`)
- `MapPickerDialog` — coordinate/entity picker with own pan/zoom state
- Uses `MapRenderer` texture data without interfering with main map state

### Rendering
- `IPluginMapPresenter` — strategy for rendering plugin entities on map
- `EntityRenderer` — delegates plugin entity rendering to presenters

## SOLID Architecture Notes

### What Works Well
- **Generic components** (NodeEditor, TreeView, MapPicker) use interfaces + strategy pattern
- **ServiceRegistry** (hytale-plugin) enables dependency inversion between plugins
- **Schema-driven rendering** — graph nodes, forms, and actions are all data-driven from server schemas
- **Event-driven updates** — EntityDataService, SelectionService use events for decoupling

### Known Technical Debt — Address When Modifying These Areas

**Form field rendering duplicated in 3 places:**
- `PluginView.RenderWidget()`, `PluginPanel.DrawField()`, `AdventureView.DrawActionFormField()` / `DrawDetailField()`
- Extract to `UI/Framework/FieldRenderer.cs` with static methods per field type

**Color constants scattered across 15+ files:**
- Every UI class declares its own `DimColor`, `AccentColor`, `SaveColor`, etc.
- Extract to `UI/Framework/UIColors.cs` as single source of truth

**HytaleApiClient is a monolith (40+ methods):**
- Should be split into domain interfaces: `IMapApi`, `IEntityApi`, `IPlayerApi`, `ISoundApi`, `IPluginApi`
- `HytaleApiClient` implements all interfaces for backward compatibility

**Error handling inconsistent:**
- API methods silently return null on failure — no error context
- Consider `Result<T>` pattern: `record Result<T>(bool Success, T? Value, string? Error)`

**EditorUI hard-codes all panels:**
- ViewMode enum with switch-case dispatching
- Should use `IPanel` interface with data-driven mode definitions

### Hexa.NET.ImGui Quirks
- `io.MouseDown[1]` (right-click) is NOT forwarded to ImGui by Stride's integration — use `Stride.Input.InputManager.IsMouseButtonPressed(MouseButton.Right)` instead
- `ImGui.IsMouseClicked()`, `ImGui.IsMouseDoubleClicked()`, `ImGui.GetKeyData()` do NOT exist — use `io.MouseDoubleClicked[0]` or manual edge detection
- `ImGui.BeginMenu()` only works inside popup/menubar contexts — use `ImGui.Selectable()` inside regular `Begin()` windows for custom menus
- ImGui popups (`OpenPopup`/`BeginPopup`) flicker with right-click — use manual floating `Begin()` windows with frame-age dismiss guard instead

### Cross-Plugin Architecture (hytale-plugin core)
- `ServiceRegistry` — generic `Register<T>/Get<T>` for cross-plugin service discovery
- `QuestGiverProvider` — interface in core, implemented by hytale-adventure, consumed by HyCitizens
- `NpcRoleManager` — interface for role regeneration, implemented by HyCitizens
- No direct dependencies between adventure and citizens plugins — both depend only on core interfaces

## Reference Implementation

The `../hytale-map/` Hugo site is the feature reference. Key files:
- `static/js/assets-panel.js` — asset browser behavior
- `static/js/map-renderer.js` — rendering, interaction, tooltips
- `static/js/block-colors.js` — block color lookup
- `static/css/style.css` — dark theme colors and layout
