# icdfyit — Design Document

## 1. Overview

icdfyit is a cross-platform desktop application for authoring Interface Control Documents. It manages a unified Data Model of Data Types, Parameters, and Packet Types, and renders output artifacts (code, documentation) through user-defined Mako template sets. The application targets .NET/C# with an Avalonia UI and embeds Python via Python.NET for template rendering.

## 2. Architecture

```
┌─────────────────────────────────────────────────────────┐
│                    Avalonia UI Layer                     │
│  MainWindow · DataTypesWindow · ParametersWindow        │
│  ExportWindow · OptionsWindow                           │
├─────────────────────────────────────────────────────────┤
│                    ViewModel Layer                       │
│  Per-window ViewModels · Undo/Redo Manager              │
│  Shared reactive DataModelService                       │
├─────────────────────────────────────────────────────────┤
│                    Domain / Model Layer                  │
│  DataType · Parameter · PacketType · TemplateSet        │
├──────────────────────┬──────────────────────────────────┤
│   Persistence        │       Export Engine               │
│   XML Serialization  │  Python.NET → Mako Renderer      │
└──────────────────────┴──────────────────────────────────┘
```

The application follows the **MVVM** pattern. All windows share a single `DataModelService` that holds the in-memory Data Model and exposes change notifications so updates propagate reactively across all open windows (ICD-IF-140).

## 3. Data Model

### 3.1 Data Types

Each Data Type has a **name** and a **base type** discriminator. Properties that apply depend on the base type:

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

*Scalar* types (integer, float, boolean, bit string) carry endianness and bit size. *Numeric* types (integer, float) additionally carry range, optional unit string, and optional calibration formula string.

### 3.2 Parameters

| Field | Type | Notes |
|---|---|---|
| Name | string | required |
| Short Description | string | optional |
| Long Description | string | optional |
| Data Type | Data Type ref | required |
| ID | integer | required, unique |
| Mnemonic | string | optional |
| Kind | enum | see below |

**Parameter Kind** values and kind-specific data:

- **Software Setting** — no extra data.
- **Software Acquisition** — no extra data.
- **Hardware Acquisition** — no extra data.
- **Synthetic Value** — carries a formula string.
- **Fixed Value** — carries a hexadecimal value string.
- **ID** — no extra data.
- **Placeholder** — no extra data; set/interpreted by custom code.

### 3.3 Packet Types

A Packet Type is either **Telecommand** or **Telemetry**. It contains an ordered list of Packet Fields:

| Field | Type | Notes |
|---|---|---|
| Name | string | required |
| Description | string | optional |
| Parameter | Parameter ref | required (many-to-one) |
| Is Type Indicator | bool | if true, Parameter must be of Kind ID and the field carries a hex value |
| Indicator Value | hex string | present when Is Type Indicator is true |

### 3.4 Template Sets

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

## 4. Component Design

### 4.1 `DataModel`

Central domain object aggregating all Data Types, Parameters, and Packet Types. Passed to the serialization and export subsystems as a single unit. Exposed to Mako templates at render time.

### 4.2 `DataModelService`

Singleton service owning the current `DataModel` instance. Responsibilities:

- CRUD operations on Data Types, Parameters, Packet Types.
- Change notification via `IObservable`/`INotifyPropertyChanged` so all bound ViewModels update reactively.
- Undo/Redo command stack (records add/delete/modify operations as reversible commands).
- New / Open / Save workflow (XML serialization).

### 4.3 XML Persistence

Serialization and deserialization of `DataModel` to/from XML using annotation-driven mapping (ICD-DES-100, ICD-DES-110). Model classes are decorated with `[XmlElement]`, `[XmlAttribute]`, etc., so no hand-written glue code is required. The standard `System.Xml.Serialization` namespace is used.

### 4.4 Export Engine

Orchestrates template rendering (ICD-FUN-90):

1. User selects a Template Set and an output folder.
2. For each Template in the set:
   a. Initialize Python.NET runtime, import Mako.
   b. Inject the `DataModel` (and any helper utilities) into the Mako template context.
   c. Render the Output Name Pattern to produce the file name.
   d. Render the template file content to produce the file body.
   e. Write the result to `<output folder>/<rendered file name>`.

Python.NET is initialized once and kept alive for the application lifetime to avoid repeated startup cost.

### 4.5 Undo/Redo Manager

Implements the **Command** pattern. Every mutating operation on the Data Model (add, delete, modify) is wrapped in a reversible command object pushed onto an undo stack. Redo is supported via a complementary stack. The manager is consumed by `DataModelService` and surfaced through menu/keyboard shortcuts.

### 4.6 Options Manager

Persists user options (e.g., default paths, UI preferences) to a settings file on disk. Options are loaded at startup and saved when the Options window is closed (ICD-FUN-100). Each option carries a default value and tooltip description.

## 5. UI Design

All windows use the Avalonia dark theme with title-bar-merged menus and a leading icon (ICD-IF-30, ICD-IF-40, ICD-IF-41).

### 5.1 Main Window

- **Menu bar**: New / Open / Save, Exit, Options, Windows (Data Types, Parameters, Export), Help / About.
- **Content area**: tree-on-left listing Telecommand and Telemetry packet types, detail panel on-right showing the selected packet's field list and metadata. Panels are resizable with splitters.

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

### 5.4 Export Window

- List of configured Template Sets.
- Output folder selector (text field + "..." picker button).
- Export button triggers the Export Engine for the selected set.

### 5.5 Options Window

- Tabbed layout. Each tab groups related options.
- Every option has a tooltip showing its description and default value.
- Path options use text field + "..." file/folder picker button.
- Saving occurs on window close.

### 5.6 Cross-Window Reactivity

All windows bind to the same `DataModelService`. Avalonia's data-binding and `INotifyPropertyChanged`/`ObservableCollection<T>` ensure that a Data Type created in the Data Types Window is immediately available in drop-downs in the Parameters Window and elsewhere, with no manual refresh (ICD-IF-140).

## 6. Key Design Decisions

| Decision | Rationale |
|---|---|
| MVVM with a shared service layer | Natural fit for Avalonia; decouples UI from logic; enables reactive cross-window updates. |
| Annotation-driven XML serialization | Eliminates hand-written mapping code (ICD-DES-100, ICD-DES-110). |
| Command pattern for undo/redo | Clean, extensible approach to reversible operations (ICD-IF-170). |
| Python.NET long-lived runtime | Avoids repeated interpreter startup; Mako is the mandated engine (ICD-DES-30, ICD-DES-40). |
| Composition over inheritance | Domain types use contained components (e.g., scalar properties, numeric properties) rather than deep class hierarchies (ICD-DES-150). |
| Grid controls with CSV I/O | Satisfies spreadsheet-like editing, column hiding, and CSV round-tripping (ICD-IF-92, ICD-IF-150, ICD-IF-160). |

## 7. Technology Summary

| Concern | Technology |
|---|---|
| Language | C# (.NET, portable) |
| GUI Framework | Avalonia (dark theme) |
| Template Engine | Mako (Python) via Python.NET |
| Data Persistence | XML (`System.Xml.Serialization`) |
| Target Platforms | Ubuntu 24+, Windows 10+ |
