# Implementation Tasks

## Overview

This document breaks down the implementation of the shortcut customization feature into discrete, testable tasks. Tasks are ordered to minimize dependencies and allow for incremental testing.

## Task List

### Task 1: Add WiX Properties to Package.wxs

**Description:** Define the two properties that will store user shortcut selections.

**Files Modified:**
- `wheel_overlay/Package/Package.wxs`

**Changes:**
Add the following properties in the Package element, after the existing Property definitions:

```xml
<!-- Shortcut selection properties -->
<Property Id="INSTALLSTARTMENUSHORTCUT" Value="1" />
<Property Id="INSTALLDESKTOPSHORTCUT" Value="1" />
```

**Acceptance Criteria:**
- Properties are defined with default value "1"
- Properties are placed in appropriate location in Package.wxs
- File remains valid WiX XML

**Testing:**
- Build MSI successfully with `build_msi.ps1`
- No build errors or warnings

---

### Task 2: Add Conditional Logic to Shortcut Components

**Description:** Add Condition elements to both shortcut components so they are only installed when the corresponding property is set to "1".

**Files Modified:**
- `wheel_overlay/Package/Package.wxs`

**Changes:**

For the Start Menu shortcut component, add a Condition element:
```xml
<Component Id="ApplicationShortcut" Guid="e8369302-3932-491c-8f43-855f93976375">
  <Condition>INSTALLSTARTMENUSHORTCUT="1"</Condition>
  <!-- existing Shortcut, RemoveFolder, RegistryValue elements -->
</Component>
```

For the Desktop shortcut component, add a Condition element:
```xml
<Component Id="DesktopShortcut" Directory="DesktopFolder" Guid="f8369302-3932-491c-8f43-855f93976376">
  <Condition>INSTALLDESKTOPSHORTCUT="1"</Condition>
  <!-- existing Shortcut, RegistryValue elements -->
</Component>
```

**Acceptance Criteria:**
- Condition elements added to both components
- Condition syntax is correct
- Existing component content unchanged
- File remains valid WiX XML

**Testing:**
- Build MSI successfully
- Install MSI - both shortcuts should be created (properties default to "1")
- Verify Start Menu shortcut exists
- Verify Desktop shortcut exists
- Uninstall - both shortcuts should be removed

---

### Task 3: Create ShortcutOptionsDlg Dialog

**Description:** Add a new dialog to CustomUI.wxs that displays checkboxes for shortcut selection.

**Files Modified:**
- `wheel_overlay/Package/CustomUI.wxs`

**Changes:**

Add the following dialog definition in the Fragment element, after the InstallDirDlg and before VerifyReadyDlg:

```xml
<!-- Shortcut Options Dialog -->
<Dialog Id="ShortcutOptionsDlg" Width="370" Height="270" Title="[ProductName] Setup">
  <Control Id="Next" Type="PushButton" X="236" Y="243" Width="56" Height="17" Default="yes" Text="Next">
    <Publish Event="NewDialog" Value="VerifyReadyDlg" />
  </Control>
  <Control Id="Back" Type="PushButton" X="180" Y="243" Width="56" Height="17" Text="Back">
    <Publish Event="NewDialog" Value="InstallDirDlg" />
  </Control>
  <Control Id="Cancel" Type="PushButton" X="304" Y="243" Width="56" Height="17" Cancel="yes" Text="Cancel">
    <Publish Event="SpawnDialog" Value="CancelDlg" />
  </Control>
  <Control Id="Title" Type="Text" X="15" Y="6" Width="200" Height="15" Transparent="yes" NoPrefix="yes" Text="{\WixUI_Font_Title}Shortcut Options" />
  <Control Id="Description" Type="Text" X="25" Y="23" Width="280" Height="15" Transparent="yes" NoPrefix="yes" Text="Choose which shortcuts to create." />
  <Control Id="BannerLine" Type="Line" X="0" Y="44" Width="370" Height="0" />
  <Control Id="StartMenuCheckbox" Type="CheckBox" X="20" Y="70" Width="330" Height="17" Property="INSTALLSTARTMENUSHORTCUT" CheckBoxValue="1" Text="Create Start Menu shortcut" />
  <Control Id="DesktopCheckbox" Type="CheckBox" X="20" Y="95" Width="330" Height="17" Property="INSTALLDESKTOPSHORTCUT" CheckBoxValue="1" Text="Create Desktop shortcut" />
  <Control Id="WarningText" Type="Text" X="20" Y="130" Width="330" Height="40" Transparent="yes" NoPrefix="yes" Text="Note: It is recommended to create at least one shortcut for easy access to the application.">
    <Condition Action="show"><![CDATA[INSTALLSTARTMENUSHORTCUT="" AND INSTALLDESKTOPSHORTCUT=""]]></Condition>
    <Condition Action="hide"><![CDATA[INSTALLSTARTMENUSHORTCUT="1" OR INSTALLDESKTOPSHORTCUT="1"]]></Condition>
  </Control>
  <Control Id="BottomLine" Type="Line" X="0" Y="234" Width="370" Height="0" />
</Dialog>
```

**Acceptance Criteria:**
- Dialog definition is syntactically correct
- All controls are properly positioned
- Checkboxes are bound to correct properties
- Warning text has proper show/hide conditions
- Navigation buttons are configured correctly
- Dialog follows UI consistency guidelines

**Testing:**
- Build MSI successfully
- Dialog is not yet in navigation flow, so won't appear during install
- No build errors or warnings

---

### Task 4: Update InstallDirDlg Navigation

**Description:** Modify the InstallDirDlg Next button to navigate to ShortcutOptionsDlg instead of VerifyReadyDlg.

**Files Modified:**
- `wheel_overlay/Package/CustomUI.wxs`

**Changes:**

In the InstallDirDlg dialog definition, find the Next button control and change its Publish event:

**Before:**
```xml
<Control Id="Next" Type="PushButton" X="236" Y="243" Width="56" Height="17" Default="yes" Text="Next">
  <Publish Event="SetTargetPath" Value="[WIXUI_INSTALLDIR]" />
  <Publish Event="NewDialog" Value="VerifyReadyDlg" />
</Control>
```

**After:**
```xml
<Control Id="Next" Type="PushButton" X="236" Y="243" Width="56" Height="17" Default="yes" Text="Next">
  <Publish Event="SetTargetPath" Value="[WIXUI_INSTALLDIR]" />
  <Publish Event="NewDialog" Value="ShortcutOptionsDlg" />
</Control>
```

**Acceptance Criteria:**
- Next button navigates to ShortcutOptionsDlg
- SetTargetPath event is preserved
- No other changes to InstallDirDlg

**Testing:**
- Build MSI successfully
- Install MSI and verify dialog flow: InstallDir → ShortcutOptions
- Verify both checkboxes are checked by default
- Click Back to return to InstallDir dialog

---

### Task 5: Update VerifyReadyDlg Navigation

**Description:** Modify the VerifyReadyDlg Back button to navigate to ShortcutOptionsDlg instead of InstallDirDlg.

**Files Modified:**
- `wheel_overlay/Package/CustomUI.wxs`

**Changes:**

In the VerifyReadyDlg dialog definition, find the Back button control and change its Publish event:

**Before:**
```xml
<Control Id="Back" Type="PushButton" X="180" Y="243" Width="56" Height="17" Text="Back">
  <Publish Event="NewDialog" Value="InstallDirDlg" />
</Control>
```

**After:**
```xml
<Control Id="Back" Type="PushButton" X="180" Y="243" Width="56" Height="17" Text="Back">
  <Publish Event="NewDialog" Value="ShortcutOptionsDlg" />
</Control>
```

**Acceptance Criteria:**
- Back button navigates to ShortcutOptionsDlg
- No other changes to VerifyReadyDlg

**Testing:**
- Build MSI successfully
- Install MSI and verify complete dialog flow:
  - Welcome → License → InstallDir → ShortcutOptions → VerifyReady
- Click Back from VerifyReady to return to ShortcutOptions
- Click Back from ShortcutOptions to return to InstallDir

---

### Task 6: Comprehensive Testing

**Description:** Test all checkbox combinations and verify correct behavior for installation and uninstallation.

**Files Modified:**
- None (testing only)

**Test Cases:**

**Test Case 6.1: Both Shortcuts Selected (Default)**
1. Run installer
2. Navigate to ShortcutOptions dialog
3. Verify both checkboxes are checked
4. Verify warning text is NOT visible
5. Complete installation
6. Verify Start Menu shortcut exists: `%APPDATA%\Microsoft\Windows\Start Menu\Programs\Wheel Overlay\Wheel Overlay.lnk`
7. Verify Desktop shortcut exists: `%USERPROFILE%\Desktop\Wheel Overlay.lnk`
8. Run uninstaller
9. Verify both shortcuts are removed

**Test Case 6.2: Only Start Menu Shortcut**
1. Run installer
2. Navigate to ShortcutOptions dialog
3. Uncheck Desktop checkbox
4. Verify warning text is NOT visible
5. Complete installation
6. Verify Start Menu shortcut exists
7. Verify Desktop shortcut does NOT exist
8. Run uninstaller
9. Verify Start Menu shortcut is removed
10. Verify no errors about missing Desktop shortcut

**Test Case 6.3: Only Desktop Shortcut**
1. Run installer
2. Navigate to ShortcutOptions dialog
3. Uncheck Start Menu checkbox
4. Verify warning text is NOT visible
5. Complete installation
6. Verify Desktop shortcut exists
7. Verify Start Menu shortcut does NOT exist
8. Run uninstaller
9. Verify Desktop shortcut is removed
10. Verify no errors about missing Start Menu shortcut

**Test Case 6.4: No Shortcuts**
1. Run installer
2. Navigate to ShortcutOptions dialog
3. Uncheck both checkboxes
4. Verify warning text IS visible
5. Verify Next button is still enabled (non-blocking)
6. Complete installation
7. Verify no shortcuts exist
8. Verify application is installed and can be launched from install directory
9. Run uninstaller
10. Verify uninstall completes without errors

**Test Case 6.5: Navigation Testing**
1. Run installer
2. Navigate forward through all dialogs
3. Use Back button to navigate backward through all dialogs
4. Verify dialog flow is correct in both directions
5. Verify checkbox states are preserved when navigating back and forth

**Test Case 6.6: Cancel Testing**
1. Run installer
2. Navigate to ShortcutOptions dialog
3. Change checkbox states
4. Click Cancel
5. Verify cancellation dialog appears
6. Verify installation is cancelled

**Acceptance Criteria:**
- All test cases pass
- No installation errors
- No uninstallation errors
- Shortcuts are created/removed as expected
- Warning message appears/disappears correctly
- Navigation works correctly in all directions

---

## Task Dependencies

```
Task 1 (Properties)
    ↓
Task 2 (Conditions) ← Can test basic conditional installation
    ↓
Task 3 (Dialog) ← Dialog created but not in flow
    ↓
Task 4 (InstallDir Nav) ← Dialog now accessible
    ↓
Task 5 (VerifyReady Nav) ← Complete dialog flow
    ↓
Task 6 (Testing) ← Full feature testing
```

## Implementation Order

Tasks should be implemented in numerical order (1-6) as each task builds on the previous one. After each task, build the MSI to verify no syntax errors were introduced.

## Rollback Plan

If issues are discovered during implementation:

1. **After Task 1-2:** Revert property and condition changes, installer will work as before
2. **After Task 3:** Dialog exists but not in flow, no impact on installation
3. **After Task 4-5:** Revert navigation changes to restore original flow
4. **During Task 6:** If critical issues found, revert all changes and reassess design

## Success Criteria

The implementation is complete when:

1. All tasks are implemented
2. All test cases pass
3. Build script runs without errors
4. MSI installs and uninstalls correctly in all scenarios
5. UI is consistent with existing dialogs
6. No regression in existing functionality
