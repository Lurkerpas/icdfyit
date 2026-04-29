#  Functional requirements
ICD-FUN-10: Software shall allow to define (add, delete, modify) Data Types. 
ICD-FUN-20: Software shall allow to define (add, delete, modify) Parameters.
ICD-FUN-30: Software shall allow to define (add, delete, modify) Packet Types.
ICD-FUN-40: Data Types, Parameters and Packet Types shall be a part of a single consistent, integrated Data Model.
ICD-FUN-41: All entities (Data Types, Parameters, Packet Types, Header Types) shall have a GUID, automatically assigned on creation, used for internal identification and reference serialization.
ICD-FUN-42: Circular references between Data Types (e.g., Structure A referencing Structure B which references Structure A) shall be forbidden. Validation shall detect and report circular references.
ICD-FUN-50: Software shall allow to create new, open, edit and save Data Models.
ICD-FUN-51: When a referenced entity (e.g., Data Type, Parameter or Header Type) is deleted, references to it shall be set to null. The application shall not crash due to null references, and the user shall be able to select a different reference later.
ICD-FUN-53: Undoing a deletion shall restore the deleted entity and all references that were set to null as a result of the deletion.
ICD-FUN-52: Software shall provide a menu action to validate the Data Model for correctness (e.g., detecting null references, duplicate names, circular references, or other constraint violations).
ICD-FUN-60: Software shall allow to define (add, delete, modify) Template Sets.
ICD-FUN-70: Software shall allow to export (render) Data Model via Template Sets for use in documents or code.
ICD-FUN-80: Software shall allow to select the output folder for exporting Data Model via a Template Set.
ICD-FUN-90: When exporting, Software shall render each template in the following manner:
- Data Model shall be injected into Mako templating engine.
- File Name shall be rendered using templating engine, from the Output Name Pattern.
- File Content shall be rendered using templating engine, from the template content.
- File Content shall be saved under the File Name located in the user-selected output folder.
ICD-FUN-100: Options Window shall have Save and Cancel buttons. Save persists options; Cancel discards changes made since the window was opened.
ICD-FUN-140: Software shall allow the user to import a Template Set definition from a Template Set XML file. Importing shall add the Template Set, with all its Templates, to the current list of Template Sets in options.
ICD-FUN-141: When importing a Template Set from an XML file, all template file paths that are relative shall be resolved to absolute paths relative to the directory containing the imported XML file, and stored as absolute paths.
ICD-FUN-142: Template file paths in options shall support environment variable expansion using ${VAR_NAME} syntax (e.g., ${HOME}/templates/mytemplate.mako). Environment variables shall be expanded at the time a template file is accessed (e.g., during export), not at the time of import or save.
ICD-FUN-101: Options shall be stored in a settings.xml file located in a system-managed per-user, per-application settings directory.
ICD-FUN-102: If settings.xml is corrupted or cannot be deserialized, Software shall log the error and continue with default options.
ICD-FUN-110: Software shall allow to define (add, delete, modify) Header Types.
ICD-FUN-120: Software shall allow to define (add, delete, modify) Memory entities.
ICD-FUN-121: Software shall allow to define and edit ICD metadata.
ICD-FUN-130: All numeric input fields (sizes, offsets, IDs, raw values) shall accept both decimal and hexadecimal notation. Hexadecimal values shall be prefixed with "0x" (e.g., "0x400", "0xFF"). If a value is entered in hexadecimal, it shall be displayed in hexadecimal notation after saving and reloading the Data Model.
ICD-FUN-150: Software shall allow to export a Data Model to YAML format.
ICD-FUN-151: Software shall allow to import a Data Model from YAML format.
ICD-FUN-152: The YAML Data Model format shall support splitting the model across multiple files via an include directive, so that different parts of the model (e.g., data types, parameters, packet types) may reside in separate files and be composed at import time.
ICD-FUN-153: YAML import shall resolve include directives relative to the directory of the file that contains them, supporting nested includes and multiple levels of inclusion.
ICD-FUN-154: In YAML format, all inter-entity references shall be expressed by entity name, not by GUID or any other internal identifier.
ICD-FUN-155: In YAML format, for fields where the Data Model holds both a string representation and a numeric representation (e.g., hexadecimal IDs, bit sizes), only the string representation shall be present in the YAML. The numeric value shall be derived from the string on import.
ICD-FUN-156: YAML export shall produce output that is human-readable and directly editable in a plain-text editor without knowledge of internal model structure.
ICD-FUN-157: The GUI shall allow to export the current Data Model to a YAML file chosen by the user.
ICD-FUN-158: The GUI shall allow to import a Data Model from a YAML file chosen by the user, replacing the current Data Model (with the same unsaved-changes guard as for XML open).
ICD-FUN-160: The CLI shall support exporting a Data Model from XML to YAML.
ICD-FUN-161: The CLI shall support importing a Data Model from YAML and saving it as XML.
ICD-FUN-162: The CLI shall support running a template-set export (render) directly from the command line, given a Data Model file (XML or YAML) and a template set definition XML file.

# Data requirements
ICD-DAT-10: Data Type shall have name. Data Type names shall be unique within the Data Model.
ICD-DAT-20: Data Type shall have base type.
ICD-DAT-30: Signed Integer shall be a base type.
ICD-DAT-40: Unsigned Integer shall be a base type.
ICD-DAT-50: Float shall be a base type.
ICD-DAT-60: Enumerated shall be a base type.
ICD-DAT-61: Enumerated shall have a list of values, each with associated name and a set of integer raw values (e.g., name "low" can map to raw values 1, 2 and 3).
ICD-DAT-70: Boolean shall be a base type.
ICD-DAT-80: Structure shall be a base type. Structure is an ordered list of fields that have Name and Data Type.
ICD-DAT-90: Array shall be a base type. Array has size, which is Unsigned Integer, and element type, which is Data Type. 
ICD-DAT-91: Array size shall have endianness, bit size, and inclusive range.
ICD-DAT-100: Bit String shall be a base type.
ICD-DAT-101: Scalar Data Types are: Signed Integer, Unsigned Integer, Float, Boolean, and Bit String.
ICD-DAT-102: Numeric Data Types are: Signed Integer, Unsigned Integer, and Float.
ICD-DAT-110: Scalar Data Types shall have endianness.
ICD-DAT-120: Scalar Data Types shall have bit size.
ICD-DAT-130: Numeric Data Types shall have inclusive range. 
ICD-DAT-140: Numeric Data Types shall have a possibility to define unit (saved as string).
ICD-DAT-150: Numeric Data Types shall have a possibility to define a calibration curve (saved as string formula).
ICD-DAT-210: Parameter shall have name. Parameter names shall be unique within the Data Model.
ICD-DAT-211: Parameter shall have optional short description.
ICD-DAT-212: Parameter shall have optional long description.
ICD-DAT-220: Parameter shall have Data Type.
ICD-DAT-230: Parameter shall have numeric ID. Parameter IDs shall be unique within the Data Model.
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
ICD-DAT-270: Parameter may optionally be associated with a Memory entity.
ICD-DAT-271: If a Parameter is associated with a Memory, it shall have a byte offset within that Memory.
ICD-DAT-280: Parameter may optionally reference a validity Parameter. The validity Parameter shall have a Boolean Data Type.
ICD-DAT-290: Numeric Parameters (Signed Integer, Unsigned Integer, Float) may have a low alarm threshold.
ICD-DAT-291: Numeric Parameters (Signed Integer, Unsigned Integer, Float) may have a high alarm threshold.
ICD-DAT-410: Packet Type can be either Telecommand or Telemetry.
ICD-DAT-411: Packet Type shall have name. Packet Type names shall be unique within the Data Model.
ICD-DAT-412: Packet Type shall have optional description.
ICD-DAT-413: Packet Type shall be associated with a Header Type.
ICD-DAT-414: Packet Type shall define fixed values for all IDs defined in the associated Header Type.
ICD-DAT-415: Packet Type shall have a numeric ID. Packet Type numeric IDs shall be unique within the Data Model.
ICD-DAT-416: Packet Type shall have an optional mnemonic.
ICD-DAT-420: Packet Type shall have an ordered list of Packet Fields.
ICD-DAT-430: Packet Field shall have name.
ICD-DAT-440: Packet Field shall have optional description.
ICD-DAT-450: Packet Field shall be associated with Parameter (more than one Packet Field can be associated with the same Parameter).
ICD-DAT-460: Packet Field can be set as Packet Type indicator.
ICD-DAT-461: Packet Field set as a Packet Type indicator shall be associated with a Parameter whose Data Type base type is Signed Integer, Unsigned Integer, or Enumerated.
ICD-DAT-462: Packet Field set as a Packet Type indicator shall have its value defined using hexadecimal string.
ICD-DAT-600: Template Set shall have a name.
ICD-DAT-601: Template Set shall have a description.
ICD-DAT-610: Template Set shall consist of a set of Templates.
ICD-DAT-620: Template shall be defined via path to a file, which can be relative or absolute, and may contain environment variable references in ${VAR_NAME} syntax.
ICD-DAT-621: Template file shall be a mako template to be rendered by Mako templating engine.
ICD-DAT-630: Template shall have a name.
ICD-DAT-640: Template shall have a description.
ICD-DAT-650: Template shall have Output Name Pattern.
ICD-DAT-660: A Template Set definition file shall be an XML file accompanying a set of template files and fully describing a Template Set (name, description, and all Templates with their metadata and relative or absolute paths).
ICD-DAT-661: The Template Set definition XML file shall include a format version number to allow future migrations.
ICD-DAT-662: Template file paths within a Template Set definition XML file may be relative (resolved against the XML file's directory) or absolute, and may contain environment variable references in ${VAR_NAME} syntax.
ICD-DAT-710: Header Type shall have a name.
ICD-DAT-715: Header Type shall have a mnemonic.
ICD-DAT-720: Header Type shall have a description.
ICD-DAT-730: Header Type shall have an ordered list of IDs, each with associated name, description and Data Type whose base type is Signed Integer, Unsigned Integer, or Enumerated.

# Metadata requirements
ICD-DAT-810: Data Model shall contain document-level ICD metadata.
ICD-DAT-811: ICD metadata shall contain Name (string).
ICD-DAT-812: ICD metadata shall contain Version (string).
ICD-DAT-813: ICD metadata shall contain Date (string).
ICD-DAT-814: ICD metadata shall contain Status (string).
ICD-DAT-815: ICD metadata shall contain Description (string).
ICD-DAT-816: ICD metadata shall contain a set of user-defined fields represented as Name:Value string pairs.

# Memory requirements
ICD-DAT-510: Memory shall have a name. Memory names shall be unique within the Data Model.
ICD-DAT-511: Memory shall have a numeric ID. Memory numeric IDs shall be unique within the Data Model.
ICD-DAT-512: Memory shall have an optional mnemonic.
ICD-DAT-513: Memory shall have a size (integer).
ICD-DAT-514: Memory shall have an optional address (string, typically hexadecimal).
ICD-DAT-515: Memory shall have an optional description.
ICD-DAT-516: Memory shall have an alignment (integer, default 1).
ICD-DAT-517: Memory shall have an IsWritable flag (boolean).
ICD-DAT-518: Memory shall have an IsReadable flag (boolean).

# Interface requirements
ICD-IF-10: Software shall be multiwindow, with the main window, and auxiliary windows.
ICD-IF-20: Auxiliary windows shall be non-modal, enabling concurrent work across all windows.
ICD-IF-30: Windows shall be styled in dark theme.
ICD-IF-40: Windows shall have their menus merged with the title bar.
ICD-IF-41: Window title bars shall begin with an Icon.
ICD-IF-42: Unless stated otherwise, windows shall have consistent layout patterns with a tree view on the left, and details on the right, within resizeable panels, and with scrollbars if necessary.
ICD-IF-50: Main Window shall have menu for: creating, loading and saving model, validating model, exiting application, showing options, showing other windows, showing help and about.
ICD-IF-51: About shall present a modal window with application information (content to be decided later).
ICD-IF-52: Help shall open the project's GitHub page (README) in the default web browser.
ICD-IF-60: Main Window shall, as its main content, present Packet Types: Telecommands and Telemetries.
ICD-IF-61: Main Window shall allow to view, edit, create and delete Packet Types and their Packet Fields.
ICD-IF-70: Option Window shall contain all options, divided across tabs.
ICD-IF-71: It shall be possible to enter filesystem paths both using a text edit and by launching a picker, using a nearby button labelled "...".
ICD-IF-72: All options shall have a tooltip with short description of the option and the default value.
ICD-IF-73: Options Window shall have a dedicated tab for defining (add, delete, modify) Template Sets and their Templates.
ICD-IF-74: The Template Sets tab in Options shall provide an "Import Template Set" action that opens a file picker for selecting a Template Set definition XML file and adds the imported Template Set to the list.
ICD-IF-75: Template file path fields in Options shall accept and display environment variable references in ${VAR_NAME} syntax without expanding them, so the user can see and edit the unexpanded form.
ICD-IF-80: Export Window shall allow to select a Template Set, select the output folder, and export the model.
ICD-IF-90: Parameters Window shall present the parameters with their metadata.
ICD-IF-91: Parameters Window shall provide the means to filter parameters by various metadata.
ICD-IF-92: Parameters in the Parameters Window shall be presented on a grid for mass editing, like a spreadsheet.
ICD-IF-93: Parameters Window shall allow to view, edit, create and delete parameters.
ICD-IF-100: Data Types Window shall present the types with their metadata.
ICD-IF-110: Data Types Window shall provide the means to filter types by various metadata.
ICD-IF-120: Data Types in the Data Types Window shall be presented on a grid for mass editing, like a spreadsheet.
ICD-IF-130: Data Types Window shall allow to view, edit, create and delete data types.
ICD-IF-140: Data shall propagate reactively across controls and windows without the user needing to explicitly refresh anything (e.g., when new Data Type is defined in the Data Types window, it should be immediately available in the Parameters Window for use in Parameters).
ICD-IF-150: If data is presented in a grid/spreadsheet form, it shall be possible to hide selected columns.
ICD-IF-160: If data is presented in a grid/spreadsheet form, it shall be possible to import/export it from/to CSV.
ICD-IF-170: It shall be possible to undo/redo add, delete, modify and reorder operations on the Data Model entities. The undo/redo stack shall be global (shared across all entity types), with a configurable maximum depth (default: 64).
ICD-IF-180: Application shall notify the user if it is about to be closed, but there are unsaved changes. The user shall be able to save, discard, or cancel the close operation. If Save is selected, close shall proceed only after the save operation is successfully committed.
ICD-IF-190: Errors shall be presented to the user in a modal window with a human-readable message and, if available, a stack trace.
ICD-IF-191: Validation results shall be presented as a list of issues in a dialog. The list shall be selectable and copyable to the clipboard.
ICD-IF-200: Application shall maintain a log file named log{date-time}.txt in the working directory, recording all actions and errors for post-mortem troubleshooting.
ICD-IF-201: Log files older than one day shall be automatically deleted on application startup.
ICD-IF-210: Header Types Window shall allow to view, edit, create and delete Header Types.
ICD-IF-220: Header Types Window shall present Header Types with their metadata.
ICD-IF-250: Memories Window shall allow to view, edit, create and delete Memory entities.
ICD-IF-251: Memories Window shall present Memory entities in a spreadsheet-style grid with filtering and column visibility toggles.
ICD-IF-252: Memories Window shall support CSV import and export.
ICD-IF-260: Metadata Window shall allow to view and edit ICD metadata.
ICD-IF-261: Metadata in the Metadata Window shall be presented as a grid.
ICD-IF-262: Metadata Window shall support add, delete, duplicate and reorder operations for user-defined metadata fields.
ICD-IF-263: Metadata Window shall support CSV import and export.
ICD-IF-310: The CLI application shall support a command to convert a Data Model from XML to YAML: given an input XML file path and an output YAML file path, it shall write the equivalent YAML representation (ICD-FUN-160).
ICD-IF-320: The CLI application shall support a command to convert a Data Model from YAML to XML: given a root YAML file path and an output XML file path, it shall resolve includes, merge, and write the XML (ICD-FUN-161).
ICD-IF-330: The CLI application shall support a command to run a template-set export: given a Data Model file (XML or YAML), a Template Set definition XML file, an output folder, and optionally a Python interpreter path, it shall render all templates in the set to the output folder (ICD-FUN-162).
ICD-IF-340: The CLI shall report errors to stderr and exit with a non-zero exit code on failure.
ICD-IF-350: The CLI shall print a usage summary when invoked with no arguments or with --help.
ICD-IF-410: The YAML Data Model format shall have a version field at the root level. If the file version is older than the current application version, the importer shall migrate the model. If the file version is newer than the current application version, the importer shall present an error and refuse to load.
ICD-IF-420: The YAML root document shall optionally contain an `includes` list of file paths (relative to the including file's directory). The importer shall merge included files' content into the current model before processing the rest of the root document.
ICD-IF-421: An include path may itself refer to a file that contains further includes. Circular includes shall be detected and reported as an error.
ICD-IF-430: The YAML format shall organize model entities in top-level sections: `metadata`, `data_types`, `parameters`, `packet_types`, `header_types`, and `memories`.
ICD-IF-431: Each section is optional; absent sections are treated as empty.
ICD-IF-440: Data Type entries in YAML shall be identified by their name. The name acts as the key in the data_types mapping.
ICD-IF-441: References from Parameters or Packet Fields to Data Types in YAML shall use the Data Type name string.
ICD-IF-442: References from Parameters to Memories in YAML shall use the Memory name string.
ICD-IF-443: References from Parameters to validity Parameters in YAML shall use the Parameter name string.
ICD-IF-444: References from Packet Types to Header Types in YAML shall use the Header Type name string.
ICD-IF-445: References from Header Type IDs to Data Types in YAML shall use the Data Type name string.
ICD-IF-450: Fields that have both a string and a numeric representation in the Data Model (e.g., numeric IDs, bit sizes, raw enum values, memory offsets) shall appear in YAML using only their string form (e.g., "0x1A", "32"). The importer shall parse the string to derive the numeric value (ICD-FUN-155).
ICD-IF-460: GUIDs shall not appear in YAML. On import, new GUIDs shall be generated for all entities.


# Design requirements
ICD-DES-10: Software shall be written in portable .NET C#.
ICD-DES-20: Software shall use Avalonia for GUI.
ICD-DES-30: Software shall use Python .NET for hosting embedded Python code.
ICD-DES-40: Software shall use Mako as the templating engine.
ICD-DES-50: Software shall be compatible with Linux systems, with Ubuntu 24 as the baseline.
ICD-DES-60: Software shall be compatible with Windows systems, with Windows 10 as the baseline.
ICD-DES-70: Software shall be designed in a modular, extendable manner.
ICD-DES-80: Software shall use templates for producing output artifacts.
ICD-DES-81: Template Sets shall be stored in the settings.xml file, separate from the Data Model XML.
ICD-DES-90: Software shall use XML for storage of Data Models.
ICD-DES-91: Data Model XML format shall include a version number. If the file version is older than the current application version, the application shall migrate the model on load. If the file version is newer than the current application version, the application shall present an error and refuse to load.
ICD-DES-92: Inter-entity references in the Data Model XML shall be serialized using the entity's GUID.
ICD-DES-100: Data Model serialization shall be automatic, based on annotations.
ICD-DES-110: Glue code shall be avoided whenever possible, prioritizing automation via annotations and reflection.
ICD-DES-120: Code shall be human readable, with small functions, readable names and low cyclomatic complexity.
ICD-DES-130: Comments shall be avoided unless required to explain the motivation, source or non-obvious logic.
ICD-DES-140: Comments shall be provided in a Doxygen compatible format.
ICD-DES-150: Composition shall be preferred over inheritance.
ICD-DES-160: All third-party libraries used by the software shall be licensed under terms compatible with AGPL-3.0.
