#  Functional requirements
ICD-FUN-10: Software shall allow to define (add, delete, modify) Data Types. 
ICD-FUN-20: Software shall allow to define (add, delete, modify) Parameters.
ICD-FUN-30: Software shall allow to define (add, delete, modify) Packet Types.
ICD-FUN-40: Data Types, Parameters and Packet Types shall be a part of a single consistent, integrated Data Model.
ICD-FUN-50: Software shall allow to open, edit and save Data Models.
ICD-FUN-60: Software shall allow to define (add, delete, modify) Template Sets.
ICD-FUN-70: Software shall allow to export (render) Data Model via Template Sets for use in documents or code.
ICD-FUN-80: Software shall allow to select the output folder for exporting Data Model via a Template Set.
ICD-FUN-90: When exporting, Software shall render each template in the following manner:
- Data Model shall be injected into Mako templating engine.
- File Name shall be rendered using templating engine, from the Output Name Pattern.
- File Content shall be rendered using templating engine, from the template content.
- File Content shall be saved under the File Name located in the user-selected output folder.
ICD-FUN-100: Options shall be persisted upon closure of the Options window.

# Data requirements
ICD-DAT-10: Data Type shall have name.
ICD-DAT-20: Data Type shall have base type.
ICD-DAT-30: Signed Integer shall be a base type.
ICD-DAT-40: Unsigned Integer shall be a base type.
ICD-DAT-50: Float shall be a base type.
ICD-DAT-60: Enumerated shall be a base type.
ICD-DAT-61: Enumerated shall have a list of values, each with associated name and set of numeric raw values.
ICD-DAT-70: Boolean shall be a base type.
ICD-DAT-80: Structure shall be a base type. Structure is an ordered list of fields that have Name and Data Type.
ICD-DAT-90: Array shall be a base type. Array has size, which is Unsigned Integer, and element type, which is Data Type. 
ICD-DAT-91: Array size shall have endianness, bit size, and inclusive range.
ICD-DAT-100: Bit String shall be a base type.
ICD-DAT-110: Scalar Data Types shall have endianness.
ICD-DAT-120: Scalar Data Types shall have bit size.
ICD-DAT-130: Numeric Data Types shall have inclusive range. 
ICD-DAT-140: Numeric Data Types shall have a possibility to define unit (saved as string).
ICD-DAT-150: Numeric Data Types shall have a possibility to define a calibration curve (saved as string formula).
ICD-DAT-210: Parameter shall have name.
ICD-DAT-211: Parameter shall have optional short description.
ICD-DAT-212: Parameter shall have optional long description.
ICD-DAT-220: Parameter shall have Data Type.
ICD-DAT-230: Parameter shall have numeric ID.
ICD-DAT-240: Parameter shall have optional mnemonic.
ICD-DAT-250: Parameter shall have kind.
ICD-DAT-251: Parameter Kind can be software setting.
ICD-DAT-252: Parameter Kind can be software acquisition.
ICD-DAT-253: Parameter Kind can be hardware acquisition.
ICD-DAT-254: Parameter Kind can be synthetic value.
ICD-DAT-255: Parameter of synthetic value Kind shall have string formula.
ICD-DAT-256: Parameter Kind can be fixed value.
ICD-DAT-257: Parameter of fixed value Kind shall have its value defined using hexadecimal string.
ICD-DAT-258: Parameter Kind can be ID.
ICD-DAT-259: Parameter Kind can be placeholder. Placeholders are meant to be set and interpreted by custom code.
ICD-DAT-410: Packet Type can be either Telecommand or Telemetry.
ICD-DAT-420: Packet Type shall have an ordered list of Packet Fields.
ICD-DAT-430: Packet Field shall have name.
ICD-DAT-440: Packet Field shall have optional description.
ICD-DAT-450: Packet Field shall be associated with Parameter (more than one Packet Field can be associated with the same Parameter).
ICD-DAT-460: Packet Field can be set as Packet Type indicator.
ICD-DAT-461: Packet Field set as a Packet Type indicator shall be associated with Parameter of Kind ID.
ICD-DAT-462: Packet Field set as a Packet Type indicator shall have its value defined using hexadecimal string.
ICD-DAT-600: Template Set shall have a name.
ICD-DAT-601: Template Set shall have a description.
ICD-DAT-610: Template Set shall consist of a set of Templates.
ICD-DAT-620: Template shall be defined via path to a file, which can be relative or absolute.
ICD-DAT-621: Template file shall be a mako template to be rendered by Mako templating engine.
ICD-DAT-630: Template shall have a name.
ICD-DAT-640: Template shall have a description.
ICD-DAT-650: Template shall have Output Name Pattern.

# Interface requirements
ICD-IF-10: Software shall be multiwindow, with the main window, and auxiliary windows.
ICD-IF-11: Window content shall be reactive, with inputs propagating across controls.
ICD-IF-20: Auxiliary windows shall be non-modal, enabling concurrent work across all windows.
ICD-IF-30: Windows shall be styled in dark theme.
ICD-IF-40: Windows shall have their menus merged with the title bar.
ICD-IF-41: Window title bars shall begin with an Icon.
ICD-IF-42: Windows shall have consistent layout patterns, typically with a tree view on the left, and details on the right, within resizeable panels, and with scrollbars if necessary.
ICD-IF-50: Main Window shall have menu for: creating, loading and saving model, exiting application, showing options, showing other windows, showing help and about. 
ICD-IF-60: Main Window shall, as its main content, present Packet Types: Telecommands and Telemetries.
ICD-IF-70: Option Window shall contain all options, divided across tabs.
ICD-IF-71: It shall be possible to enter filesystem paths both using a text edit and by launching a picker, using a nearby button labelled "...".
ICD-IF-72: All options shall have a tooltip with short description of the option and the default value.
ICD-IF-80: Export Window shall contain list of export templates and commands to export the model.
ICD-IF-90: Parameters Window shall present the parameters with their metadata.
ICD-IF-91: Parameters Window shall provide the means to filter parameters by various metadata.
ICD-IF-92: Parameters in the Parameters Window shall be presented on a grid for mass editing, like a spreadsheet.
ICD-IF-93: Parameters Window shall allow to view, edit, create and delete parameters.
ICD-IF-100: Data Types Window shall present the types with their metadata.
ICD-IF-110: Data Types Window shall provide the means to filter types by various metadata.
ICD-IF-120: Data Types in the Data Types Window shall be presented on a grid for mass editing, like a spreadsheet.
ICD-IF-130: Data Types Window shall allow to view, edit, create and delete data types.
ICD-IF-140: Data shall propagate across windows without the user needing to explicitly refresh anything (e.g., when new Data Type is defined in the Data Types window, it should be immediately available in the Parameters Window for use in Parameters).
ICD-IF-150: If data is presented in a grid/spreadsheet form, it shall be possible to hide selected columns.
ICD-IF-160: If data is presented in a grid/spreadsheet form, it shall be possible to import/export it from/to CSV.
ICD-IF-170: It shall be possible to undo/redo add, delete, modify operations on the Data Model entities.
ICD-IF-180: Application shall notify the user if it is about to be closed, but there are unsaved changes.

# Design requirements
ICD-DES-10: Software shall be written in portable .NET C#.
ICD-DES-20: Software shall use Avalonia for GUI.
ICD-DES-30: Software shall use Python .NET for hosting embedded Python code.
ICD-DES-40: Software shall use Mako as the templating engine.
ICD-DES-50: Software shall be compatible with Linux systems, with Ubuntu 24 as the baseline.
ICD-DES-60: Software shall be compatible with Windows systems, with Windows 10 as the baseline.
ICD-DES-70: Software shall be designed in a modular, extendable manner.
ICD-DES-80: Software shall use templates for producing output artifacts.
ICD-DES-90: Software shall use XML for storage of Data Models.
ICD-DES-100: Data Model serialization shall be automatic, based on annotations.
ICD-DES-110: Glue code shall be avoided whenever possible, prioritizing automation via annotations and reflection.
ICD-DES-120: Code shall be human readable, with small functions, readable names and low cyclomatic complexity.
ICD-DES-130: Comments shall be avoided unless required to explain the motivation, source or non-obvious logic.
ICD-DES-140: Comments shall be provided in a Doxygen compatible format.
ICD-DES-150: Composition shall be preferred over inheritance.
