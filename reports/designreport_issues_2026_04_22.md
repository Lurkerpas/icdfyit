# Design Review Report

**Project:** icdfyit  
**Review date:** 2026-04-23  
**Sources reviewed:** `design/design.md`, `design/requirements.md`, all source files under `src/IcdFyIt.Core/` and `src/IcdFyIt.App/`

---

## Summary

The overall architecture follows the design closely: MVVM layering, decomposed service layer, GUID-based entity references, annotation-driven XML serialization, and Python.NET/Mako template rendering are all in place. However, a number of concrete non-conformances were found across infrastructure, undo/redo correctness, model validation coverage, data integrity, and the UI. Each finding is classified by the requirement(s) it violates and the file(s) involved.

---

## 1. Infrastructure / Logging

### NC-01 — `LogManager.Initialise()` is never called
**Violates:** ICD-IF-200, ICD-IF-201  
**Files:** `src/IcdFyIt.App/Program.cs`, `src/IcdFyIt.App/App.axaml.cs`, `src/IcdFyIt.Core/Infrastructure/LogManager.cs`

`LogManager.Initialise(string logDirectory)` is defined but has no call site anywhere in the application entry-points. The Serilog pipeline is therefore never configured. All logging is completely inactive at runtime — no log file is ever written.

### NC-02 — Log file naming does not match the requirement
**Violates:** ICD-IF-200  
**File:** `src/IcdFyIt.Core/Infrastructure/LogManager.cs`

ICD-IF-200 requires the log file to be named `log{date-time}.txt`. The `LogManager` implementation configures a Serilog rolling-file sink with the path template `icdfyit-.log`, producing names such as `icdfyit-20260423.log`. Even if the logger were initialised, the file would not conform to the required name format.

### NC-03 — `settings.xml` is stored in OS AppData, not in the working directory
**Violates:** ICD-FUN-101  
**File:** `src/IcdFyIt.Core/Infrastructure/OptionsManager.cs`

ICD-FUN-101 states: *"Options shall be stored in a settings.xml file located in the working directory."* The `OptionsManager` implementation derives the settings path from `Environment.SpecialFolder.ApplicationData`, resolving to `~/.config/icdfyit/settings.xml` on Linux and `%APPDATA%\icdfyit\settings.xml` on Windows. This is an OS-specific roaming profile location, not the working directory of the running process.

---

## 2. Undo / Redo

### NC-04 — `UndoDepth` option is persisted but never applied to `UndoRedoManager`
**Violates:** ICD-IF-170  
**Files:** `src/IcdFyIt.Core/Infrastructure/AppOptions.cs`, `src/IcdFyIt.App/App.axaml.cs`, `src/IcdFyIt.Core/Services/UndoRedoManager.cs`

ICD-IF-170 requires a configurable undo/redo stack depth (default 64). `AppOptions.UndoDepth` is loaded, displayed, and persisted via the Options window. However, no code ever reads this setting and assigns it to `UndoRedoManager.MaxDepth`. The `UndoRedoManager` is instantiated with its hardcoded default of 64 in `App.axaml.cs`, and that value is never updated. Users cannot effectively change the undo depth.

### NC-05 — Undo of `RemoveParameter` does not restore packet-field cross-references
**Violates:** ICD-FUN-53  
**File:** `src/IcdFyIt.Core/Services/DataModelManager.cs` (`RemoveParameter`)

`RemoveParameter` correctly nullifies `PacketField.Parameter` references before removing the parameter, but it then pushes a generic `AddEntityCommand<Parameter>` with `IsRemove = true` onto the undo stack. `AddEntityCommand<T>` only adds or removes from the model list; it captures no cross-references. When the removal is undone, the parameter is restored to the list, but all packet fields that previously referenced it still have `null` as their `Parameter`. Compare with `RemoveDataTypeCommand`, which correctly captures and restores `StructureField`, `ArrayType`, and `Parameter` references.

### NC-06 — Undo of `RemoveHeaderType` does not restore packet-type header references
**Violates:** ICD-FUN-53  
**File:** `src/IcdFyIt.Core/Services/DataModelManager.cs` (`RemoveHeaderType`)

Identical pattern to NC-05. `RemoveHeaderType` nullifies `PacketType.HeaderType` references before pushing a generic `AddEntityCommand<HeaderType>` with `IsRemove = true`. Undoing the removal restores the header type to the model list but leaves all formerly-associated packet types with a `null` header type, discarding their header ID values as well.

### NC-07 — Add/remove of Header Type ID entries is not undoable
**Violates:** ICD-IF-170  
**File:** `src/IcdFyIt.App/ViewModels/HeaderTypesWindowViewModel.cs` (`AddId`, `RemoveId`)

The `AddId` and `RemoveId` commands in `HeaderTypesWindowViewModel` mutate `HeaderType.Ids` and `IdRows` directly, with no involvement of `UndoRedoManager`. A comment in the code acknowledges this: *"// ── ID entry CRUD (direct model mutation, no undo) ────────────────────────"*. These operations cannot be undone.

### NC-08 — Add/remove/reorder of Packet Fields is not undoable
**Violates:** ICD-IF-170  
**File:** `src/IcdFyIt.App/ViewModels/PacketTypeNodeViewModel.cs` (`AddField`, `RemoveField`, `MoveField`)

Packet field mutations in `PacketTypeNodeViewModel` directly update `_packetType.Fields` and the `Fields` observable collection without routing through `UndoRedoManager`. Add, remove, and drag-to-reorder of packet fields cannot be undone.

---

## 3. Model Validation

### NC-09 — Validator does not check that Header Type ID entries reference a Data Type of kind ID
**Violates:** ICD-FUN-52, design §4.2.5, ICD-DAT-730  
**File:** `src/IcdFyIt.Core/Services/ModelValidator.cs`

The design document and §4.2.5 explicitly list the following as a required validation check: *"Header Type ID entries referencing a Data Type that is not of kind ID."* The implementation's `CheckHeaderTypeIdNullDataTypes` only checks whether the DataType reference is `null`; it does not verify that the referenced DataType's kind is `ID`. A Header Type ID entry bound to, for example, a `SignedIntegerType` would pass validation silently.

### NC-10 — Validator does not check for missing Header ID values on Packet Types
**Violates:** ICD-FUN-52, ICD-DAT-414  
**File:** `src/IcdFyIt.Core/Services/ModelValidator.cs`

ICD-DAT-414 states: *"Packet Type shall define fixed values for all IDs defined in the associated Header Type."* The validator does not check whether `PacketType.HeaderIdValues` contains an entry for every `HeaderTypeId` in the associated header type. A packet type with a partially or completely empty `HeaderIdValues` list would pass validation without error.

### NC-11 — Validator does not check that type-indicator fields have an `IndicatorValue` set
**Violates:** ICD-FUN-52, ICD-DAT-462  
**File:** `src/IcdFyIt.Core/Services/ModelValidator.cs`

ICD-DAT-462 states: *"Packet Field set as a Packet Type indicator shall have its value defined using hexadecimal string."* The validator's `CheckTypeIndicatorKinds` only checks that the associated parameter is of kind ID; it does not check that `PacketField.IndicatorValue` is non-null and non-empty when `IsTypeIndicator` is true.

---

## 4. Data / Model

### NC-12 — `DuplicateParameter` copies `NumericId`, immediately creating a duplicate
**Violates:** ICD-DAT-230  
**File:** `src/IcdFyIt.Core/Services/DataModelManager.cs` (`DuplicateParameter`)

ICD-DAT-230 requires parameter IDs to be unique within the Data Model. `DuplicateParameter` copies `NumericId` verbatim from the source parameter. The resulting copy always starts life with the same numeric ID as the original, producing an immediate violation detectable by the validator.

### NC-13 — `DuplicatePacketType` does not copy `HeaderType` or `HeaderIdValues`
**Violates:** ICD-DAT-413, ICD-DAT-414  
**File:** `src/IcdFyIt.Core/Services/DataModelManager.cs` (`DuplicatePacketType`)

The `DuplicatePacketType` implementation copies `Name`, `Kind`, `Description`, and `Fields`, but does not copy `HeaderType` or `HeaderIdValues`. The resulting packet type has no header type association and no header ID values, violating both ICD-DAT-413 and ICD-DAT-414 from the moment it is created.

### NC-14 — `Template` and `TemplateSet` in `IcdFyIt.Core.Model` are dead code
**Violates:** ICD-DES-70 (modular, extendable — no dead code)  
**Files:** `src/IcdFyIt.Core/Model/Template.cs`, `src/IcdFyIt.Core/Model/TemplateSet.cs`

Two classes, `Template` and `TemplateSet`, exist in the `IcdFyIt.Core.Model` namespace but are never referenced by `DataModel`, any service, or any serialization path. Template Set configuration is handled exclusively by `TemplateSetConfig` / `TemplateConfig` in `IcdFyIt.Core.Infrastructure`. The unused parallel hierarchy is a source of confusion.

### NC-15 — `HeaderType.Description` is nullable; the requirement marks it as required
**Violates:** ICD-DAT-720  
**Files:** `src/IcdFyIt.Core/Model/HeaderType.cs`

ICD-DAT-720 states: *"Header Type shall have a description."* The design reference table also marks Description as required. The model class declares `public string? Description { get; set; }`, making it optional. No validation check enforces its presence, so a Header Type can be saved with a null or empty description.

### NC-16 — Schema migration pipeline is absent
**Violates:** ICD-DES-91  
**File:** `src/IcdFyIt.Core/Persistence/XmlPersistence.cs`

ICD-DES-91 requires that when a file's schema version is older than the current version, the loader applies sequential migration functions. `XmlPersistence.Load` correctly refuses files with a schema version newer than `CurrentSchemaVersion`, but for older versions it performs no migration — it calls `ResolveReferences` and continues unchanged. There is no framework (no migration function registry, no chaining mechanism) for applying version N → N+1 transformations. While the current schema version is 1 so this has no immediate impact, the design requirement is unimplemented.

---

## 5. User Interface

### NC-17 — Window title bar icons are absent from all windows
**Violates:** ICD-IF-41  
**Files:** All `.axaml` files under `src/IcdFyIt.App/Views/`

ICD-IF-41 states: *"Window title bars shall begin with an Icon."* None of the application windows (`MainWindow`, `DataTypesWindow`, `ParametersWindow`, `HeaderTypesWindow`, `OptionsWindow`, `ExportWindow`, et al.) set an `Icon` property on the `Window` element, nor is a default `Application.WindowIcon` configured in `App.axaml`. All windows appear without an icon in the title bar.

### NC-18 — Menus are not merged with the title bar
**Violates:** ICD-IF-40  
**Files:** `src/IcdFyIt.App/Views/MainWindow.axaml` and other window AXAML files

ICD-IF-40 states: *"Windows shall have their menus merged with the title bar."* The `MainWindow` uses a standard `<Menu>` element docked at the top of a `DockPanel`, which places the menu bar in a separate horizontal band below the OS title bar. Avalonia supports title-bar-integrated menus via `ExtendClientAreaToDecorations`, `NativeMenu`, or `ExtendsContentIntoTitleBar`, none of which is used. The title bar and menu bar remain visually and structurally separate.

### NC-19 — No `Exit` item in the main window File menu
**Violates:** ICD-IF-50  
**File:** `src/IcdFyIt.App/Views/MainWindow.axaml`

ICD-IF-50 states the main window menu shall include options for *"exiting application."* The `File` menu contains `New`, `Open`, `Reopen`, `Save`, and `Save As` — but no `Exit` or `Quit` item. The application can only be closed via the OS window-close button.

### NC-20 — Clipboard copy in ValidationDialog is not implemented
**Violates:** ICD-IF-191  
**Files:** `src/IcdFyIt.App/Views/ValidationDialog.axaml`, `src/IcdFyIt.App/ViewModels/ValidationDialogViewModel.cs`

ICD-IF-191 states: *"The list shall be selectable and copyable to the clipboard."* The AXAML comment inside `ValidationDialog.axaml` reads: *"Copy to clipboard: not yet implemented (Avalonia 12 clipboard API TBD)."* The `ValidationDialogViewModel` also leaves a note: *"CopyToClipboard is handled in ValidationDialog code-behind (requires IClipboard from TopLevel)."* No button for clipboard copy is present in the view.

### NC-21 — Header Types window lacks CSV import/export and column visibility toggle
**Violates:** ICD-IF-150, ICD-IF-160  
**Files:** `src/IcdFyIt.App/Views/HeaderTypesWindow.axaml`, `src/IcdFyIt.App/ViewModels/HeaderTypesWindowViewModel.cs`

ICD-IF-150 and ICD-IF-160 require that any grid-based view supports hiding columns and CSV round-tripping. The Data Types and Parameters windows both implement these features via `SetColumnVisible` and `ImportCsv`/`ExportCsv` commands. The Header Types window presents data in a `DraggableGrid` but exposes no column visibility toggle and no CSV commands.

### NC-22 — `ParametersWindowViewModel.Add()` creates a parameter without prompting the user
**Violates:** ICD-IF-93  
**File:** `src/IcdFyIt.App/ViewModels/ParametersWindowViewModel.cs` (`Add`)

ICD-IF-93 requires the Parameters window to allow adding parameters. The `RequestAddParameter` delegate exists and is wired to an `AddParameterDialog` in `App.axaml.cs`, but `ParametersWindowViewModel.Add()` ignores it — it directly calls `_dataModelManager.AddParameter("NewParameter")` with a hardcoded default name. Users cannot specify the name of a new parameter at creation time, unlike the Data Types window which prompts for both name and kind.

### NC-23 — No global unhandled exception handler
**Violates:** ICD-IF-190  
**Files:** `src/IcdFyIt.App/Program.cs`, `src/IcdFyIt.App/App.axaml.cs`

ICD-IF-190 states: *"Errors shall be presented to the user in a modal window with a human-readable message and, if available, a stack trace."* No handler is registered for `TaskScheduler.UnobservedTaskException`, `AppDomain.CurrentDomain.UnhandledException`, or Avalonia's equivalent. Unhandled exceptions will crash the application without presenting any user-facing dialog or writing to the log.

---

## 6. Minor Code Discrepancies

### NC-24 — `DataModelManager.New()` docstring references the wrong requirement
**Violates:** ICD-DES-130 (comments shall explain motivation correctly)  
**File:** `src/IcdFyIt.Core/Services/DataModelManager.cs`

The XML doc comment above `New()` reads: *"Discards the current model and starts a new empty one (ICD-FUN-10)."* ICD-FUN-10 covers Data Type definition. The correct reference is ICD-FUN-50 (*"Software shall allow to create new, open, edit and save Data Models"*).

---

## Requirement Traceability — Non-Conformances per Requirement

| Requirement | Description (abbreviated) | Non-conformance(s) |
|---|---|---|
| ICD-FUN-52 | Model validation | NC-09, NC-10, NC-11 |
| ICD-FUN-53 | Undo restores deleted entity and all nullified references | NC-05, NC-06, NC-07, NC-08 |
| ICD-FUN-101 | settings.xml in working directory | NC-03 |
| ICD-DAT-230 | Parameter IDs unique | NC-12 |
| ICD-DAT-413 | Packet Type associated with Header Type | NC-13 |
| ICD-DAT-414 | Packet Type defines values for all Header IDs | NC-10, NC-13 |
| ICD-DAT-462 | Type-indicator field has hex value | NC-11 |
| ICD-DAT-720 | Header Type has required description | NC-15 |
| ICD-DAT-730 | Header Type ID Data Type must be of kind ID | NC-09 |
| ICD-IF-40 | Menus merged with title bar | NC-18 |
| ICD-IF-41 | Window title bars begin with an icon | NC-17 |
| ICD-IF-50 | Main menu includes Exit | NC-19 |
| ICD-IF-93 | Parameters window: add parameter with user input | NC-22 |
| ICD-IF-150 | Column visibility toggle in grid views | NC-21 |
| ICD-IF-160 | CSV import/export in grid views | NC-21 |
| ICD-IF-170 | Configurable undo depth | NC-04, NC-07, NC-08 |
| ICD-IF-190 | Errors shown in modal dialog | NC-23 |
| ICD-IF-191 | Validation list copyable to clipboard | NC-20 |
| ICD-IF-200 | Log file named `log{date-time}.txt` | NC-01, NC-02 |
| ICD-IF-201 | Old log files deleted on startup | NC-01 |
| ICD-DES-70 | Modular, no dead code | NC-14 |
| ICD-DES-91 | Sequential schema migration for older files | NC-16 |
