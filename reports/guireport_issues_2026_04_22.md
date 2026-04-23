# GUI Report — IcdFyIt 2.0× Scale

All screenshots taken at the 2.0× UI scale.

---

## Summary of Issue Categories

| # | Category | Screens affected |
|---|----------|-----------------|
| A | Dialog window too small — content/buttons clipped | 07, 08, 09, 10, 11, 12 |
| B | Content wider than window — right-edge clipping | 01, 02, 03, 04, 06, 13, 17 |
| C | Close button layout overlap — renders mid-dialog | 15, 16, 18 |
| D | Missing UI elements | 01, 13, 14, 15 |

---

## Screen-by-Screen

### 01 — Main Window

- **[B]** Packet type name in the sidebar tree is clipped: "distribute on/off device c…" — the right edge of the panel cuts off the full name.
- **[D]** "Telemetries" tree group has no expand chevron, unlike the "Telecommands" group above it.

---

### 02 — Data Types Window

- **[B]** The "Kind" cell for `uint16` reads "UnsignedIntege…" — column is too narrow for the full "UnsignedInteger" value.
- **[B]** The numeric columns to the right (Min Value, Max Value, Scale/Offset) are partially visible and cut off at the right edge of the window.

---

### 03 — Parameters Window

- **[B]** Column headers are truncated: "Numeric I…" (should be "Numeric ID") and "Data Ty…" (should be "Data Type").
- **[B]** The "Data Type" cell for "OnOffDeviceAddress" reads "addres…" — clipped at the right edge.
- **[B]** The rightmost columns (Formula, Hex Value) are entirely or mostly hidden beyond the window edge.

---

### 04 — Header Types Window

- **[B]** Description cells are truncated: "PUS C Telecommand Head…", "PUS C Headerless Telemet…", and the sub-grid row "Message Typ…".
- **[B]** The "IDs" column header is partially clipped at the right edge.

---

### 05 — Options Window

No issues observed.

---

### 06 — Export Window

- **[B]** The output folder placeholder text is truncated: "Select or type an output folder…" is cut off mid-word within the text field.

---

### 07 — About Window

- **[A]** The OK button is not visible — the window height (220 px base) is too small to show it at this scale.

---

### 08 — Add Packet Type Dialog

- **[A]** Only the "Name" label and text field are visible. The OK and Cancel buttons at the bottom are not shown — the dialog height (160 px base) is too small.

---

### 09 — Add Data Type Dialog

- **[A]** Only the "Name" field is visible. The "Kind:" label is partially visible at the very bottom edge. The Kind dropdown, type-specific fields, and OK/Cancel buttons are all clipped.

---

### 10 — Add Parameter Dialog

- **[A]** Only the "Name" field is visible. Everything below (Kind, other fields, OK/Cancel) is clipped for the same reason as 08 and 09.

---

### 11 — Unsaved Changes Dialog

- **[A]** The dialog message is clipped on the left: shows "…aved changes. What would you…" — the beginning of the sentence ("You have uns…") is hidden. The dialog is too narrow (400 px base) for the full message at this scale.

---

### 12 — Validation Dialog

- **[A]** The third validation message, "Header type 'PUSC-HTM' has no ID fields", is cut off at the bottom — only "…fields" is partially visible.

---

### 13 — Enum Values Dialog

- **[B]** The second column header reads "Raw Values (comm…" — truncated; should be "Raw Values (comma-separated)".
- **[D]** No Add / Remove row buttons are visible. They may be positioned above the visible area or missing from the dialog layout.

---

### 14 — Struct Fields Dialog

- **[B]** The second row's Name cell reads "test field…" — the field name is truncated.
- **[D]** No Add / Remove row toolbar is visible (same issue as 13).

---

### 15 — Array Type Dialog

- **[C]** The "Close" button renders in the middle of the dialog, overlapping the "Size Field Bit Size" and "Array Min Length" rows. The button should be at the bottom of the dialog.
- **[D]** The "BE" label for the second radio button in the "Size Field Endianness" row is missing — only the radio circle is visible, with no label.
- **[A]** The bottom portion of the dialog (below "Array Min Length") is likely clipped.

---

### 16 — Parameter Attributes Dialog

- **[A]** Only two rows are visible: "Kind:" (with only the dropdown arrow visible, value clipped) and "Data Type:".
- **[C]** The "Close" button text appears directly on top of the "Data Type" field value, reading "CloseUint16" — confirming the same mid-dialog button overlap as screen 15.
- **[B]** The Kind dropdown value (selected item) is fully clipped at the right.

---

### 17 — Select Header Type Dialog

- **[B]** The "Header Type" value reads "PUSC-T…" — truncated; should show the full name "PUSC-TC" (or similar).

---

### 18 — Header ID Data Type Dialog

- **[C]** The "Close" button text overlaps the "Data Type" field value, rendering as "uint8 / Close" stacked in the same visual area — same layout overlap issue as screens 15 and 16.
- **[A]** The dialog base height (130 px) appears far too small; most content is hidden.

---

## Recommended Fixes

### Category A — Increase dialog base sizes in `ScreenshotRunner.cs`

The following base heights are too small for the actual dialog content at 2× scale:

| Key | Current H | Minimum suggested |
|-----|-----------|-------------------|
| `about` | 220 | 260 |
| `add_packet_type` | 160 | 220 |
| `add_data_type` | 160 | 340 |
| `add_parameter` | 160 | 220 |
| `unsaved_changes` | 180 | 220 |
| `array_type` | 340 | 420 |
| `param_attributes` | 220 | 420 |
| `header_id_datatype` | 130 | 200 |

### Category B — Increase window widths or minimum column widths

The `data_types`, `parameters`, `main`, and several dialog windows need wider base widths, or the columns need `MinWidth` constraints to prevent overflow.

### Category C — Close button z-order / layout overlap

The "Close" button in `array_type`, `param_attributes`, and `header_id_datatype` dialogs overlaps form fields. This is likely caused by a layout container that places the button at an absolute or unscaled position while the surrounding fields are scaled by `LayoutTransformControl`. Verify that the button is inside the same scaling context as the other controls, or use a `DockPanel`/`Grid` row that sizes its height relative to scaled content.

### Category D — Missing controls

- **Telemetries expand chevron** (screen 01): investigate why the `TreeViewItem` for Telemetries does not show an expand indicator when it has no children vs when it does.
- **Add/Remove row buttons** (screens 13, 14): confirm buttons are inside the scrollable area or above the captured viewport.
- **"BE" radio label** (screen 15): check that the label `TextBlock` next to the second radio button has a non-zero width and is not hidden.
