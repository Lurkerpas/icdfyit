# Code Review Report - 2026-04-25

Scope: entire project (App, Core, CLI, GuiReporter, tests), with emphasis on correctness, readability, and maintainability.

Method: static review of source + targeted pattern scan + test execution (9 passed).

## Findings

### Critical

1. Close flow can discard unsaved work even when user chooses Save
- Severity: Critical
- Evidence:
  - src/IcdFyIt.App/Views/MainWindow.axaml.cs:113
  - src/IcdFyIt.App/Views/MainWindow.axaml.cs:116
  - src/IcdFyIt.App/Views/MainWindow.axaml.cs:117
  - src/IcdFyIt.App/ViewModels/MainWindowViewModel.cs:218
  - src/IcdFyIt.App/ViewModels/MainWindowViewModel.cs:222
- Why this is a problem:
  - In the save branch, close is forced immediately after awaiting SaveDocumentCommand.
  - SaveDocumentCore returns early when no path is selected (for example, user cancels Save As), and does not indicate failure/cancel to the caller.
  - The close handler still sets allow-close and closes the window.
- Impact:
  - User can select Save, cancel file picker, and still lose unsaved changes.

### Major

2. PacketType node view models are subscribed to manager events without lifecycle unsubscription
- Severity: Major
- Evidence:
  - src/IcdFyIt.App/ViewModels/PacketTypeNodeViewModel.cs:32
  - src/IcdFyIt.App/ViewModels/MainWindowViewModel.cs:438
  - src/IcdFyIt.App/ViewModels/MainWindowViewModel.cs:446
- Why this is a problem:
  - Each PacketTypeNodeViewModel subscribes to DataModelManager.PacketFieldsChanged.
  - Node removal path removes from collections but does not detach event handlers.
- Impact:
  - Removed nodes can remain rooted by event subscriptions, causing memory growth and stale handler invocation over time.

3. Undo depth is loaded from settings without validation; invalid values can break undo infrastructure
- Severity: Major
- Evidence:
  - src/IcdFyIt.App/App.axaml.cs:39
  - src/IcdFyIt.Core/Services/UndoRedoManager.cs:14
  - src/IcdFyIt.Core/Services/UndoRedoManager.cs:25
- Why this is a problem:
  - MaxDepth is directly assigned from persisted settings.
  - Push loop assumes sane MaxDepth; negative values make loop condition always true until RemoveFirst is called on an empty list.
- Impact:
  - Corrupted or hand-edited settings can cause runtime exceptions during normal edit operations.

4. Settings load errors are silently swallowed
- Severity: Major
- Evidence:
  - src/IcdFyIt.Core/Infrastructure/OptionsManager.cs:29
- Why this is a problem:
  - Deserialization failures return default AppOptions without diagnostics.
- Impact:
  - Persistent user settings can appear to "randomly reset" with no actionable error signal.

5. Python detection suppresses root-cause failures
- Severity: Major
- Evidence:
  - src/IcdFyIt.Core/Export/ExportEngine.cs:135
- Why this is a problem:
  - CanRunPython catches all exceptions and returns false, discarding process launch diagnostics.
- Impact:
  - Export troubleshooting is harder; distinct failure classes are collapsed into generic "not found" behavior.

6. Async-void close helper has unobserved-exception risk in a critical workflow
- Severity: Major
- Evidence:
  - src/IcdFyIt.App/Views/MainWindow.axaml.cs:108
- Why this is a problem:
  - Exceptions from awaited operations in async void cannot be awaited by caller and are difficult to control/recover.
- Impact:
  - Close workflow failures can become unstable or crash-prone under I/O or dialog failures.

7. Inconsistent undo model for list reordering vs documented manager contract
- Severity: Major
- Evidence:
  - src/IcdFyIt.Core/Services/DataModelManager.cs:10
  - src/IcdFyIt.Core/Services/DataModelManager.cs:124
  - src/IcdFyIt.Core/Services/DataModelManager.cs:259
- Why this is a problem:
  - Class-level contract states every mutating operation is wrapped as undoable command.
  - MoveParameter and MoveMemory bypass undo command stack and mark dirty directly.
- Impact:
  - User-visible inconsistency: some edits are undoable while reorder operations are not.
  - Documentation/implementation drift increases maintenance cost.

8. Test suite does not cover UI/application-layer behavior and newly added layout persistence
- Severity: Major
- Evidence:
  - tests/IcdFyIt.Core.Tests/IcdFyIt.Core.Tests.csproj:24
  - tests/IcdFyIt.Core.Tests/Services/ModelValidatorTests.cs:1
  - tests/IcdFyIt.Core.Tests/Persistence/XmlPersistenceTests.cs:1
  - tests/IcdFyIt.Core.Tests/Model/DataModelTests.cs:1
  - src/IcdFyIt.App/Services/LayoutPersistenceManager.cs:1
- Why this is a problem:
  - Existing automated tests target Core only.
  - No automated checks for MainWindow closing flow, DraggableGrid layout persistence, or reset-to-default behavior.
- Impact:
  - Regressions in critical UX workflows can ship undetected.

### Minor

9. Unused/unsafe command implementation left in About window view model
- Severity: Minor
- Evidence:
  - src/IcdFyIt.App/ViewModels/AboutWindowViewModel.cs:19
  - src/IcdFyIt.App/Views/AboutWindow.axaml:24
- Why this is a problem:
  - Close command throws NotImplementedException while the view currently uses click handler instead.
  - This is dead/unsafe code that can become an accidental crash if command binding is introduced later.
- Impact:
  - Maintainer trap and latent crash risk.

10. Settings location tied to process working directory
- Severity: Minor
- Evidence:
  - src/IcdFyIt.Core/Infrastructure/OptionsManager.cs:12
- Why this is a problem:
  - Runtime behavior depends on launch directory, not a stable user config location.
- Impact:
  - Users may unintentionally create multiple independent settings.xml files across launch contexts.

11. Clipboard copy handler uses async void
- Severity: Minor
- Evidence:
  - src/IcdFyIt.App/Views/ValidationDialog.axaml.cs:22
- Why this is a problem:
  - async-void handlers are hard to test and can hide failure behavior.
- Impact:
  - Occasional clipboard errors are hard to surface and diagnose.

## Open Questions / Needs Confirmation

1. Should choosing Save during close be treated as "close only if save actually committed"? Current behavior appears to close regardless of save cancellation path.
2. Is non-undoable reorder for parameters/memories intentional product behavior, or a gap against the DataModelManager contract comments?
3. Is working-directory-based settings location an explicit product requirement, or should it migrate to a stable per-user app data path?

## Secondary Summary

- Automated tests currently pass (9/9), but they do not exercise the main risks above.
- Highest priority risk is unsaved-change data loss path in MainWindow close flow.
- Next priority risks are event-subscription lifecycle leaks and unchecked persisted undo-depth values.
