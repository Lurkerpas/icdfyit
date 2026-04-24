# icdfyit — Design Document

## 1. Overview

icdfyit is a cross-platform desktop application for authoring Interface Control Documents. It manages a unified Data Model of Data Types, Parameters, and Packet Types, and renders output artifacts (code, documentation) through user-defined Mako template sets. The application targets .NET/C# with an Avalonia UI and embeds Python via Python.NET for template rendering.

## 2. Architecture

```
┌───────────────────────────────────────────────────────────────┐
│                       Avalonia UI Layer                       │
│  MainWindow · DataTypesWindow · ParametersWindow             │
│  HeaderTypesWindow · ExportWindow · OptionsWindow            │
│  ValidationDialog                                            │
├───────────────────────────────────────────────────────────────┤
│                       ViewModel Layer                         │
│  Per-window ViewModels                                       │
├───────────────────────────────────────────────────────────────┤
│                       Service Layer                           │
│  DataModelManager · UndoRedoManager · ModelValidator         │
│  ChangeNotifier · DirtyTracker                               │
├───────────────────────────────────────────────────────────────┤
│                       Domain / Model Layer                    │
│  DataType · Parameter · PacketType · HeaderType (all GUIDs)  │
├──────────────┬──────────────┬─────────────────────────────────┤
│  Persistence │ Export Engine │ Infrastructure                  │
│  XmlPersist  │ MakoRenderer │ OptionsManager · LogManager    │
│  (GUID refs) │ (Python.NET) │                                │
└──────────────┴──────────────┴─────────────────────────────────┘
```

The application follows the **MVVM** pattern. All windows share a common service layer that holds the in-memory Data Model and exposes change notifications so updates propagate reactively across all controls and open windows (ICD-IF-140).

## 3. Data Model

Every entity (Data Type, Parameter, Packet Type, Header Type) carries a **GUID**, automatically assigned on creation, used for internal identification and reference serialization (ICD-FUN-41).

### 3.0 Hexadecimal Notation (ICD-FUN-130)

All numeric input fields (sizes, offsets, IDs, and raw values) accept both decimal and hexadecimal notation. Hexadecimal values must be prefixed with `0x` (e.g., `0x400`, `0xFF`). The original notation is preserved through save/reload: if the user enters `0x400`, the field continues to display `0x400` after the model is saved and reopened.

**Implementation:** Each numeric model field uses a dual property pattern:
- An `[XmlIgnore] int Foo` property holds the parsed integer, used by internal logic (duplicate checks, auto-increment).
- A companion `[XmlAttribute("Foo")] string FooStr` (or `[XmlElement("Foo")] string FooStr` for XML-element fields) is what XmlSerializer reads/writes; it stores the user's original notation verbatim.
- When code sets `Foo` programmatically (e.g., auto-generated IDs), `FooStr` falls back to decimal display.

Affected fields: `Memory.NumericId`, `Memory.Size`, `Memory.Alignment`; `Parameter.NumericId`, `Parameter.MemoryOffset`; `PacketType.NumericId`; `ScalarProperties.BitSize`; `EnumeratedType.BitSize`; `ArraySizeDescriptor.BitSize`; `EnumeratedValue.RawValues` (via `RawValuesDisplay`).

### 3.1 Data Types

Each Data Type has a **name** (unique within the Data Model) and a **base type** discriminator. Properties that apply depend on the base type:

| Base Type | Additional Properties |
|---|---|
| Signed Integer | endianness, bit size, range, unit, calibration |
| Unsigned Integer | endianness, bit size, range, unit, calibration |
| Float | endianness, bit size, range, unit, calibration |
| Boolean | endianness, bit size |
| Bit String | endianness, bit size |
| Enumerated | list of (name, raw values) pairs |
| Structure | ordered list of fields (name, Data Type ref) |
| Array | element Data Type ref, size descriptor (endianness, bit size, range) |

*Scalar* types are: Signed Integer, Unsigned Integer, Float, Boolean, and Bit String (ICD-DAT-101). They carry endianness and bit size. *Numeric* types are a subset of scalar: Signed Integer, Unsigned Integer, and Float (ICD-DAT-102). They additionally carry range, optional unit string, and optional calibration formula string.

Enumerated values map a symbolic name to a *set* of integer raw values (e.g., name "low" → raw values 1, 2, 3).

Circular references between Data Types (e.g., Structure A → Structure B → Structure A) are forbidden (ICD-FUN-42). This is enforced by validation.

### 3.2 Parameters

| Field | Type | Notes |
|---|---|---|
| Name | string | required, unique within Data Model |
| Short Description | string | optional |
| Long Description | string | optional |
| Data Type | Data Type ref | required; nullable if referent is deleted |
| ID | integer | required, unique within Data Model |
| Mnemonic | string | optional |
| Kind | enum | see below |
| Memory | Memory ref | optional; the memory region this parameter resides in |
| Memory Offset | integer | byte offset within Memory; meaningful only when Memory is set |
| Validity Parameter | Parameter ref | optional; a boolean parameter that indicates whether this parameter is valid |
| Alarm Low | double | optional; low alarm threshold; only meaningful for numeric data types |
| Alarm High | double | optional; high alarm threshold; only meaningful for numeric data types |

**Parameter Kind** values and kind-specific data:

- **Software Setting** — no extra data.
- **Software Acquisition** — no extra data.
- **Hardware Acquisition** — no extra data.
- **Synthetic Value** — carries a formula string.
- **Fixed Value** — carries a hexadecimal value string.
- **ID** — no extra data.
- **Placeholder** — no extra data; set/interpreted by custom code.

### 3.3 Packet Types

A Packet Type is either **Telecommand** or **Telemetry**. It has a **name** (unique within the Data Model), a **numeric ID** (unique within the Data Model), an optional **mnemonic**, an optional **description**, and is associated with a **Header Type** (nullable if the referent is deleted). For each ID defined in the associated Header Type the Packet Type stores a **fixed hex value** (ICD-DAT-413, ICD-DAT-414).

It contains an ordered list of Packet Fields:

| Field | Type | Notes |
|---|---|---|
| Name | string | required, unique within Data Model |
| Description | string | optional |
| ID | integer | required, unique within Data Model |
| Mnemonic | string | optional |
| Header Type | Header Type ref | required; nullable if referent is deleted |
| Header ID Values | list of (ID name → hex string) | one entry per Header Type ID |
| Fields | ordered list of Packet Fields | see below |

| Field | Type | Notes |
|---|---|---|
| Name | string | required |
| Description | string | optional |
| Parameter | Parameter ref | required (many-to-one); nullable if referent is deleted |
| Is Type Indicator | bool | if true, Parameter must be of Kind ID and the field carries a hex value |
| Indicator Value | hex string | present when Is Type Indicator is true |

### 3.4 Header Types

A Header Type describes the common header structure shared by Packet Types. It has a **name** (unique within the Data Model) and a **description**, and carries an ordered list of IDs:

| Field | Type | Notes |
|---|---|---|
| Name | string | required, unique within Data Model |
| Description | string | required |
| IDs | ordered list | see below |

Each **ID entry** in the list has:

| Field | Type | Notes |
|---|---|---|
| Name | string | required |
| Description | string | optional |
| Data Type | Data Type ref | required; must be of kind ID; nullable if referent deleted |

When a Header Type is deleted, all Packet Type references to it are set to null (ICD-FUN-51). The associated per-ID fixed values on the Packet Type are discarded at that point.

### 3.5 Template Sets

| Field | Type | Notes |
|---|---|---|
| Name | string | required |
| Description | string | required |
| Templates | list of Template | one or more |

Each **Template**:

| Field | Type | Notes |
|---|---|---|
| Name | string | required |
| Description | string | required |
| File Path | string | absolute or relative path to a Mako template file |
| Output Name Pattern | string | Mako expression rendered to produce the output file name |

Template Sets are stored in `settings.xml`, separate from the Data Model XML (ICD-DES-81). The settings file is located in a system-managed per-user application data directory.

### 3.6 Memories

A Memory represents a named hardware or software memory region. It carries:

| Field | Type | Notes |
|---|---|---|
| Name | string | required, unique within Data Model |
| Numeric ID | integer | required, unique within Data Model |
| Mnemonic | string | optional |
| Size | integer | required; unit is implementation-defined (e.g. bytes) |
| Address | string | optional; typically a hex address string |
| Description | string | optional |
| Alignment | integer | required; default 1 |
| Is Writable | boolean | whether the region is writable |
| Is Readable | boolean | whether the region is readable |

Memories are independent entities with no cross-references to other Data Model entities. They are presented in the Memories window (ICD-IF-250).

## 4. Component Design

### 4.1 `DataModel`

Central domain object aggregating all Data Types, Parameters, Packet Types, Header Types, and Memories. Passed to the serialization and export subsystems as a single unit. Exposed to Mako templates at render time as a variable named `model`.

References between entities (e.g., Parameter → Data Type, Packet Field → Parameter, Packet Type → Header Type, Header Type ID → Data Type) are stored as GUID-based nullable references. When a referenced entity is deleted, all references to it are set to null (ICD-FUN-51). The UI tolerates null references gracefully (displaying a placeholder or blank), allowing the user to select a replacement later.

### 4.2 Service Layer

The former monolithic `DataModelService` is decomposed into focused, single-responsibility classes:

#### 4.2.1 `DataModelManager`

Singleton owning the current `DataModel` instance. Orchestrates high-level workflows:

- New / Open / Save (delegates serialization to `XmlPersistence`).
- "New" creates an empty Data Model (ICD-FUN-50).
- CRUD operations on Data Types, Parameters, Packet Types, and Header Types — each operation is routed through `UndoRedoManager` and `ChangeNotifier`.

#### 4.2.2 `ChangeNotifier`

Centralizes change-notification logic. Exposes `INotifyPropertyChanged` and `ObservableCollection<T>` events consumed by all ViewModels, ensuring reactive propagation across controls and windows (ICD-IF-140).

#### 4.2.3 `DirtyTracker`

Tracks whether the Data Model has been modified since the last save. Set on any mutation, cleared on save. Consumed by the UI to guard against closing with unsaved changes (ICD-IF-180).

#### 4.2.4 `UndoRedoManager`

Implements the **Command** pattern. Every mutating operation (add, delete, modify) is wrapped in a reversible command object pushed onto a global undo stack. Redo is supported via a complementary stack. The maximum depth is configurable (default: 64) and stored in `settings.xml` (ICD-IF-170).

Delete commands capture the list of references that were nulled so that **undo restores both the entity and all its former references** (ICD-FUN-53).

#### 4.2.5 `ModelValidator`

Scans the Data Model for constraint violations (ICD-FUN-52):

- Null references (missing Data Type on Parameter, missing Parameter on Packet Field, missing Data Type on Header Type ID entry).
- Duplicate names or IDs (Data Types, Parameters, Packet Types).
- Circular Data Type references (ICD-FUN-42).
- Type indicator fields not associated with Kind ID parameters.
- Header Type ID entries referencing a Data Type that is not of kind ID.

Returns a list of human-readable issue descriptions, presented in a validation dialog (ICD-IF-191).

### 4.3 XML Persistence (`XmlPersistence`)

Serialization and deserialization of `DataModel` to/from XML using annotation-driven mapping (ICD-DES-100, ICD-DES-110). Model classes are decorated with `[XmlElement]`, `[XmlAttribute]`, etc., so no hand-written glue code is required. The standard `System.Xml.Serialization` namespace is used.

**Reference serialization**: Inter-entity references are serialized as the target entity's GUID string (ICD-DES-92). On deserialization, GUIDs are resolved back to in-memory object references via a lookup dictionary populated during load.

**Format versioning** (ICD-DES-91): The XML root element carries a `version` attribute.

- If the file version is **older** than the current application version, the loader applies sequential migration functions (version N → N+1 → … → current) to transform the XML before deserializing.
- If the file version is **newer** than the current application version, the application presents an error and refuses to load.

### 4.4 Export Engine (`ExportEngine`)

Orchestrates template rendering (ICD-FUN-90). The Python.NET runtime (pythonnet NuGet package) is initialized once on first export and kept alive for the application lifetime to avoid repeated startup cost.

1. Ensure the Python.NET runtime is initialized (one-time: locate Python via system PATH, import Mako, log an error if not found).
2. User selects a Template Set and an output folder.
3. For each Template in the set:
   a. Inject the `DataModel` as the variable `model` (and any helper utilities) into the Mako template context.
   b. Render the Output Name Pattern to produce the file name.
   c. Render the template file content to produce the file body.
   d. Write the result to `<output folder>/<rendered file name>`.

### 4.5 Options Manager

Persists user options (e.g., default paths, UI preferences, undo depth) and Template Set definitions (ICD-DES-81) to `settings.xml` in a system-managed per-user application data directory (ICD-FUN-101). Options are loaded at startup. If `settings.xml` is corrupted or cannot be deserialized, the failure is logged and default options are used for that run (ICD-FUN-102). The Options Window provides explicit **Save** and **Cancel** buttons (ICD-FUN-100): Save persists all changes; Cancel discards them. Each option carries a default value and tooltip description.

### 4.6 Error Handling & Logging (`LogManager`)

All unhandled exceptions and operational errors (e.g., failed template rendering, corrupt XML on load, unwritable output folder) are caught by a global error handler that:

1. Presents a modal dialog with a human-readable error message and, when available, a stack trace (ICD-IF-190).
2. Writes the error to the session log file.

The application maintains a log file named `log{date-time}.txt` in the working directory (ICD-IF-200). The following significant actions are logged:

- Application startup and shutdown.
- Data Model new / open / save (with file path).
- Entity add / delete / modify (with entity type and name).
- Undo / redo operations.
- Export start and finish (with template set name and output folder).
- Option changes (with option name and old/new values).
- Validation runs and their results.
- Errors and exceptions (with stack traces).

On startup, log files older than one day are automatically deleted (ICD-IF-201).

## 5. UI Design

All windows use the Avalonia dark theme with title-bar-merged menus and a leading icon (ICD-IF-30, ICD-IF-40, ICD-IF-41).

### 5.1 Main Window

- **Menu bar**: New / Open / Save, Validate Model, Exit, Options, Windows (Data Types, Parameters, Header Types, Export), Help / About.
- **Content area**: tree-on-left listing Telecommand and Telemetry packet types, detail panel on-right showing the selected packet's field list and metadata. Panels are resizable with splitters.
- **Packet Type CRUD**: toolbar or context menu to add, delete, and modify Packet Types and their Packet Fields (ICD-IF-61).
- **Close guard**: When the user attempts to close the application (or start a new/open operation) while unsaved changes exist, a confirmation dialog offers Save, Discard, or Cancel (ICD-IF-180). If Save is selected, close continues only when save has been successfully committed.
- **Help**: opens the project's GitHub page (README) in the default web browser (ICD-IF-52).
- **About**: presents a modal window with application information; content to be decided later (ICD-IF-51).

### 5.2 Data Types Window

- Grid/spreadsheet view of all Data Types for mass editing (ICD-IF-120).
- Column visibility toggle and filter controls (by base type, name, etc.).
- CSV import/export of the grid.
- Toolbar or context menu for add/delete.

### 5.3 Parameters Window

- Grid/spreadsheet view analogous to Data Types (ICD-IF-92).
- Filter by kind, data type, name, mnemonic, etc.
- Column visibility toggle, CSV import/export.
- Toolbar or context menu for add/delete.

### 5.4 Header Types Window

- Grid/spreadsheet view of all Header Types (ICD-IF-220).
- Each row shows the Header Type name and description.
- A detail panel (or expandable row) lists the ordered ID entries for the selected Header Type, showing each entry's name, description, and Data Type reference (ICD-IF-210).
- Toolbar or context menu for add/delete of Header Types and their ID entries (ICD-IF-210).
- Column visibility toggle and CSV import/export per ICD-IF-150 and ICD-IF-160.

### 5.5 Export Window

- Template Set selector (drop-down populated from the sets defined in Options).
- Output folder selector (text field + "..." picker button).
- Export button triggers the Export Engine for the selected set.

### 5.6 Options Window

- Tabbed layout. Each tab groups related options.
- Every option has a tooltip showing its description and default value.
- Path options use text field + "..." file/folder picker button.
- Dedicated **Template Sets** tab for defining (add, delete, modify) Template Sets and their Templates (ICD-IF-73).
- Explicit **Save** and **Cancel** buttons (ICD-FUN-100).

### 5.7 Validation Dialog

- Modal dialog presenting validation results as a scrollable, selectable list of issue descriptions (ICD-IF-191).
- The list is copyable to the clipboard.

### 5.8 Cross-Window Reactivity

All windows bind to the service layer via `ChangeNotifier`. Avalonia's data-binding and `INotifyPropertyChanged`/`ObservableCollection<T>` ensure that changes propagate reactively across controls and windows — e.g., a Data Type created in the Data Types Window is immediately available in drop-downs in the Parameters Window and elsewhere, with no manual refresh (ICD-IF-140).

## 6. Project Structure

```
icdfyit.sln
├── src/
│   ├── IcdFyIt.Core/              (net8.0 class library)
│   │   ├── Model/                 DataType, Parameter, PacketType, HeaderType, DataModel
│   │   ├── Services/              DataModelManager, ChangeNotifier, DirtyTracker,
│   │   │                          UndoRedoManager, ModelValidator
│   │   ├── Persistence/           XmlPersistence, migration helpers
│   │   ├── Export/                ExportEngine (uses pythonnet NuGet package)
│   │   └── Infrastructure/        OptionsManager, LogManager
│   └── IcdFyIt.App/              (net8.0 Avalonia application)
│       ├── ViewModels/            Per-window ViewModels
│       ├── Views/                 AXAML views + code-behind
│       ├── Converters/            Value converters
│       └── App.axaml              Application entry, theme, DI setup
└── tests/
    └── IcdFyIt.Core.Tests/       (net8.0 xUnit test project)
        ├── Model/
        ├── Services/
        └── Persistence/
```

- **IcdFyIt.Core** contains all domain logic, services, and persistence with no UI dependency.
- **IcdFyIt.App** contains Avalonia views and ViewModels.
- **IcdFyIt.Core.Tests** contains unit tests for the core library.

Target framework: **net8.0** (LTS).

## 7. Key Design Decisions

| Decision | Rationale |
|---|---|
| MVVM with decomposed service layer | Single-responsibility classes are easier to test and maintain than a monolithic service (ICD-DES-70, ICD-DES-120). |
| GUID-based entity identity | Stable, unique identifiers decouple reference serialization from mutable names (ICD-FUN-41, ICD-DES-92). |
| Annotation-driven XML serialization | Eliminates hand-written mapping code (ICD-DES-100, ICD-DES-110). |
| Sequential version migration | Each migration function transforms version N to N+1, composable and independently testable (ICD-DES-91). |
| Command pattern for undo/redo | Clean, extensible; delete commands capture affected references for full restoration (ICD-FUN-53). |
| Python.NET lazy-initialized runtime | Initialized on first export inside ExportEngine; avoids startup cost if export is not used (ICD-DES-30, ICD-DES-40). |
| Composition over inheritance | Domain types use contained components rather than deep class hierarchies (ICD-DES-150). |
| Avalonia DataGrid with CSV I/O | Satisfies spreadsheet-like editing, column hiding, and CSV round-tripping (ICD-IF-92, ICD-IF-150, ICD-IF-160). |

## 8. Technology Summary

| Concern | Technology | License |
|---|---|---|
| Language / Runtime | C# / .NET 8.0 | MIT |
| GUI Framework | Avalonia 11.x | MIT |
| MVVM Toolkit | CommunityToolkit.Mvvm | MIT |
| DataGrid | Avalonia.Controls.DataGrid | MIT |
| Python Interop | pythonnet | MIT |
| Template Engine | Mako (Python package) | MIT |
| XML Serialization | System.Xml.Serialization (built-in) | MIT |
| Logging | Serilog | Apache-2.0 |
| Testing | xUnit + FluentAssertions | Apache-2.0 / Apache-2.0 |
| Target Platforms | Ubuntu 24+, Windows 10+ | — |

All listed libraries are compatible with AGPL-3.0 (ICD-DES-160).
