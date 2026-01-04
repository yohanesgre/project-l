# Visual Novel UI System

## Summary

This UI system provides a complete visual novel dialogue interface built with Unity's UI Toolkit. It includes:

- **DialogueUIController** - Main dialogue box with typewriter effect and control buttons
- **ChoiceUIController** - Dynamic choice selection panel
- **HistoryUIController** - Scrollable dialogue history/log
- **TypewriterEffect** - Character-by-character text reveal
- **SettingsPanel** - Auto-play and text speed controls

All controllers integrate seamlessly with the `DialogueManager` singleton and use a dark modern theme.

---

## Setup

### 1. Scene Setup

Create a GameObject hierarchy in your scene:

```
DialogueSystem (Empty GameObject)
├── DialogueManager (Component)
├── DialogueEventProcessor (Component)
├── DialogueUIController (Component)
├── ChoiceUIController (Component)
├── HistoryUIController (Component)
└── TypewriterEffect (Component)
```

### 2. UI Documents

Create UI Document GameObjects for each panel:

```
UI (Empty GameObject)
├── DialogueUI (UIDocument) → Assign DialoguePanel.uxml
├── ChoiceUI (UIDocument) → Assign ChoicePanel.uxml
├── HistoryUI (UIDocument) → Assign HistoryPanel.uxml
└── SettingsUI (UIDocument) → Assign SettingsPanel.uxml
```

### 3. Component Configuration

**DialogueUIController:**
```
Dialogue Document: [DialogueUI UIDocument]
Choice Document: [ChoiceUI UIDocument]
History Document: [HistoryUI UIDocument]
Settings Document: [SettingsUI UIDocument]
Typewriter Effect: [TypewriterEffect Component]
```

**ChoiceUIController:**
```
Choice Document: [ChoiceUI UIDocument]
Show Title: false (optional)
Default Title: "Make a choice"
```

**HistoryUIController:**
```
History Document: [HistoryUI UIDocument]
Max History Entries: 100
Scroll To Bottom On Add: true
```

---

## Usage

### Starting Dialogue

```csharp
using Runtime;

public class GameController : MonoBehaviour
{
    [SerializeField] private DialogueDatabase dialogueDatabase;

    void Start()
    {
        // Start from beginning
        DialogueManager.Instance.StartDialogue(dialogueDatabase);
        
        // Or start from specific entry
        DialogueManager.Instance.StartDialogue(dialogueDatabase, "scene1_001");
        
        // Or start from specific scene
        DialogueManager.Instance.StartDialogueFromScene(dialogueDatabase, "Chapter1");
    }
}
```

### Manual Dialogue Control

```csharp
// Advance to next dialogue
DialogueManager.Instance.AdvanceDialogue();

// Jump to specific entry
DialogueManager.Instance.JumpToEntry("scene2_015");

// End dialogue
DialogueManager.Instance.EndDialogue();
```

### Handling Choices

Choices are handled automatically when `DialogueEntry.HasChoices` is true. To manually handle:

```csharp
// Subscribe to choice events
DialogueManager.Instance.OnChoicesPresented += (choices) =>
{
    foreach (var choice in choices)
    {
        Debug.Log($"Choice: {choice.DisplayText} -> {choice.TextID}");
    }
};

// Select a choice programmatically
DialogueManager.Instance.SelectChoice(0); // Select first choice
```

### Typewriter Effect Control

```csharp
// Get reference
var dialogueUI = FindObjectOfType<DialogueUIController>();

// Skip typewriter animation
dialogueUI.SkipTypewriter();

// Set text speed (0 = slow, 1 = fast)
dialogueUI.SetTextSpeed(0.7f);

// Toggle auto-play
dialogueUI.ToggleAuto();
```

### History Panel

```csharp
var historyUI = FindObjectOfType<HistoryUIController>();

// Show/hide history
historyUI.Show();
historyUI.Hide();
historyUI.Toggle();

// Add custom entry
historyUI.AddEntry("Character", "Custom dialogue text");

// Clear history
historyUI.ClearHistory();

// Get all entries
var entries = historyUI.GetHistory();
```

### Custom Choice Display

```csharp
var choiceUI = FindObjectOfType<ChoiceUIController>();

// Display choices manually
choiceUI.DisplayChoices(new string[] {
    "Go to the forest",
    "Return to town",
    "Wait here"
});

// Subscribe to selection
choiceUI.OnChoiceSelected += (index) =>
{
    Debug.Log($"Selected choice: {index}");
};
```

---

## Events

### DialogueManager Events

| Event | Parameters | Description |
|-------|------------|-------------|
| `OnDialogueStarted` | `DialogueEntry` | Fired when dialogue begins |
| `OnDialogueChanged` | `DialogueEntry` | Fired for each new dialogue entry |
| `OnChoicesPresented` | `List<ChoiceOption>` | Fired when choices are available |
| `OnDialogueEnded` | - | Fired when dialogue ends |
| `OnSpeakerChanged` | `string` | Fired when speaker changes |

### DialogueUIController Events

| Event | Description |
|-------|-------------|
| `OnAutoToggled` | Auto-play mode toggled |
| `OnSkipRequested` | Skip button pressed |
| `OnLogRequested` | Log/History button pressed |
| `OnSettingsRequested` | Settings button pressed |
| `OnDialogueClicked` | Dialogue box clicked |

### TypewriterEffect Events

| Event | Parameters | Description |
|-------|------------|-------------|
| `OnTypewriterStarted` | - | Text animation started |
| `OnTypewriterCompleted` | - | Text animation finished |
| `OnCharacterRevealed` | `char` | Each character revealed |

---

## Keyboard Shortcuts

| Key | Action |
|-----|--------|
| `Click` / `Space` | Advance dialogue / Skip typewriter |
| `1-9` | Select choice (when choices visible) |
| `L` | Toggle history panel |
| `Escape` | Close history panel |

---

## Customization

### Styling

Edit `Assets/Runtime/UI/Styles/VisualNovelTheme.uss` to customize:

```css
:root {
    --color-accent-cyan: rgb(79, 195, 247);    /* Speaker name color */
    --color-bg-panel: rgba(20, 20, 20, 0.95);  /* Panel background */
    --font-size-normal: 18px;                   /* Dialogue text size */
}
```

### Speaker Positioning

```csharp
// Position speech bubble pointer
dialogueUI.SetPointerPosition(isLeft: true);  // Left side
dialogueUI.SetPointerPosition(isLeft: false); // Right side
```

### Typewriter Speed

```csharp
// Direct delay setting (seconds per character)
typewriterEffect.CharacterDelay = 0.03f;

// Normalized speed (0-1)
typewriterEffect.SetSpeedNormalized(0.8f);
```

---

## File Structure

```
Assets/Runtime/
├── Scripts/
│   ├── Core/
│   │   └── DialogueManager.cs
│   ├── UI/
│   │   ├── Choice/
│   │   │   └── ChoiceUIController.cs
│   │   ├── Components/
│   │   │   └── TypewriterEffect.cs
│   │   ├── Dialogue/
│   │   │   └── DialogueUIController.cs
│   │   └── History/
│   │       └── HistoryUIController.cs
│   └── Data/Models/
│       ├── DialogueEntry.cs
│       └── DialogueDatabase.cs
└── UI/
    ├── Layouts/
    │   ├── DialoguePanel.uxml
    │   ├── ChoicePanel.uxml
    │   ├── HistoryPanel.uxml
    │   └── SettingsPanel.uxml
    └── Styles/
        └── VisualNovelTheme.uss
```

---

## Quick Start Example

```csharp
using UnityEngine;
using Runtime;

public class QuickStartExample : MonoBehaviour
{
    [SerializeField] private DialogueDatabase database;

    void Start()
    {
        // Subscribe to events
        var dm = DialogueManager.Instance;
        dm.OnDialogueEnded += () => Debug.Log("Dialogue finished!");
        
        // Start dialogue
        dm.StartDialogue(database);
    }

    void Update()
    {
        // Manual advance with spacebar
        if (Input.GetKeyDown(KeyCode.Space))
        {
            DialogueManager.Instance.AdvanceDialogue();
        }
    }
}
```
