# AnimatedSceneController - Quick Start Guide

## Overview
The **AnimatedSceneController** manages smooth transitions between animated scenes in your game. It handles scene loading/unloading, fade transitions using **UI Toolkit**, and provides both manual and automatic scene progression.

## Quick Setup

### 1. Create UI Toolkit Overlay
1. In your scene, create: `GameObject > UI Toolkit > UI Document`
2. Name it "SceneTransitionOverlay"
3. In the Inspector:
   - **Source Asset**: Assign `Assets/_Project/Features/UI/SceneTransitionOverlay.uxml`
   - **Sort Order**: Set to a high value (e.g., 1000) to ensure it's on top
4. The UXML file already contains the `fade-overlay` element

### 2. Add AnimatedSceneController Component
1. Create an empty GameObject in your scene (e.g., "SceneManager")
2. Add the `AnimatedSceneController` component
3. Configure in Inspector:
   - **Fade UI Document**: Drag the "SceneTransitionOverlay" UI Document here
   - **Fade Overlay Name**: Leave as `fade-overlay` (matches the UXML element name)
   - **Fade Color**: Choose your fade color (default: black)

### 3. Configure Scenes
In the Inspector, click **"Detect Animated Scenes"** to automatically find all scenes in:
```
Assets/_Project/Scenes/Animated_Scenes/
```

Or manually add scene names to the `Animated Scenes` list.

### 4. Add to Build Settings
Click **"Add to Build Settings"** to ensure all animated scenes are included in the build.

## Configuration

### Scene Settings
- **Animated Scenes**: List of scene names in order
- **Loop Scenes**: Return to first scene after last scene
- **Auto Transition Delay**: Auto-advance after X seconds (0 = disabled)

### Transition Settings
- **Fade Out Duration**: Time to fade to color (default: 0.5s)
- **Fade In Duration**: Time to fade from color (default: 0.5s)
- **Fade Color**: Color to fade to/from (default: black)
- **Load Additive**: Keep previous scene loaded during transition
- **Unload Previous Scene**: Unload old scene after additive load

## Usage

### From Code
```csharp
// Get reference to controller
AnimatedSceneController controller = GetComponent<AnimatedSceneController>();

// Transition to next/previous scene
controller.TransitionToNextScene();
controller.TransitionToPreviousScene();

// Jump to specific scene
controller.TransitionToSceneByIndex(2);
controller.TransitionToSceneByName("Scene_Riding_Looping");

// Listen for events
controller.OnSceneTransitionStarted += (sceneName) => 
{
    Debug.Log($"Transitioning to {sceneName}");
};

controller.OnSceneTransitionCompleted += (sceneName) => 
{
    Debug.Log($"Arrived at {sceneName}");
};

controller.OnTransitionProgress += (progress) =>
{
    // Update loading bar: progress is 0-1
};

// Control auto-transitions
controller.SetAutoTransitionDelay(3.0f); // Auto-advance every 3 seconds
controller.StopAutoTransition(); // Stop auto-advancing
```

### From Unity Events
The controller exposes UnityEvents in the Inspector for:
- `OnSceneTransitionStarted(string sceneName)`
- `OnSceneLoaded(string sceneName)`
- `OnSceneTransitionCompleted(string sceneName)`
- `OnTransitionProgress(float progress)`

Connect these to other game systems without code.

### From UI Buttons
Create UI buttons and use `OnClick()` to call:
- `TransitionToNextScene()`
- `TransitionToPreviousScene()`

## Scene Loading Modes

### Single Scene Loading (Default)
- `Load Additive`: OFF
- Unloads current scene and loads new scene
- Best for simple scene switching
- Memory efficient

### Additive Loading
- `Load Additive`: ON
- `Unload Previous Scene`: ON
- Keeps old scene while loading new one (smoother transitions)
- Unloads old scene after transition completes
- Better visual experience (no flicker)

## Play Mode Controls
When playing in the Editor, the Inspector shows:
- **Runtime Status**: Current scene, index, transition state
- **◄ Previous Scene / Next Scene ►**: Quick navigation buttons
- **Jump to Scene dropdown**: Select any scene directly

## Your Current Animated Scenes
- Scene_Riding_Looping
- Scene_Riding_Stop

## Customizing the Fade Overlay

### Change Fade Color
In the `AnimatedSceneController` Inspector, change the **Fade Color** property.

### Edit the Overlay Style
1. Open `Assets/_Project/Features/UI/SceneTransitionOverlay.uss`
2. Modify the `#fade-overlay` style:
```css
#fade-overlay {
    background-color: rgb(0, 0, 0); /* Change this */
    /* Add custom styles here */
}
```

### Add Custom Elements
1. Open `Assets/_Project/Features/UI/SceneTransitionOverlay.uxml`
2. Add loading text, progress bar, etc:
```xml
<ui:UXML>
    <ui:VisualElement name="fade-overlay">
        <ui:Label text="Loading..." name="loading-text" />
    </ui:VisualElement>
</ui:UXML>
```

## Advanced: Custom Transitions
Implement `ISceneTransition` for custom transition effects:

```csharp
using MyGame.Core;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class WipeTransition : MonoBehaviour, ISceneTransition
{
    private VisualElement wipeElement;
    
    public void Initialize() 
    {
        // Setup your custom transition
    }
    
    public IEnumerator TransitionOut(float duration)
    {
        // Your custom "out" effect (e.g., wipe from left to right)
        yield return new WaitForSeconds(duration);
    }
    
    public IEnumerator TransitionIn(float duration)
    {
        // Your custom "in" effect
        yield return new WaitForSeconds(duration);
    }
    
    public void Cleanup() 
    {
        // Clean up resources
    }
}
```

## Tips & Best Practices

### For Cutscene Sequences
- Set `Auto Transition Delay` > 0 for automatic playback
- Enable `Loop Scenes` for repeating cinematics
- Use `OnSceneLoaded` event to trigger dialogue/audio/effects
- Example: 5-second delay between scenes for a cutscene

### For Player-Controlled Scenes
- Set `Auto Transition Delay` to 0
- Create UI buttons calling `TransitionToNextScene()`
- Or use keyboard/gamepad input in a custom controller script
- Disable auto-transition for interactive experiences

### For Seamless Transitions
- Enable `Load Additive` mode
- Enable `Unload Previous Scene`
- Increases memory usage temporarily but eliminates flicker
- Best for similar scenes (same lighting/skybox)

### Performance
- UI Toolkit is more performant than Canvas
- Fade overlay has minimal overhead
- Additive loading uses more memory temporarily
- Monitor with Profiler if loading large scenes

## Troubleshooting

**Transitions are instant / No fade effect**
- Ensure UIDocument is assigned to `Fade UI Document`
- Verify the UXML contains an element named `fade-overlay`
- Check that fade durations are > 0

**Scenes not found**
- Click "Add to Build Settings" in the Inspector
- Verify scene names match exactly (case-sensitive)
- Check scenes exist in `Assets/_Project/Scenes/Animated_Scenes/`

**UI Toolkit overlay not visible**
- Check UIDocument's Sort Order (should be high, e.g., 1000)
- Ensure UXML is assigned to UIDocument
- Verify overlay element covers full screen in UXML

## Files Created
- **Scripts**: `Assets/_Project/_Core/Scripts/AnimatedSceneController.cs`
- **UXML**: `Assets/_Project/Features/UI/SceneTransitionOverlay.uxml`
- **USS**: `Assets/_Project/Features/UI/SceneTransitionOverlay.uss`
- **Editor**: `Assets/_Project/_Core/Editor/Scripts/AnimatedSceneControllerEditor.cs`
- **Example**: `Assets/_Project/_Core/Scripts/SceneControllerExample.cs`
