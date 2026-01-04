# Scene Transition Test Implementation Plan

## Objective
Create a test setup where the character automatically transitions from `Scene_Riding_Stop.unity` to `Scene_Riding_Looping.unity` when it finishes its path using **additive scene loading**.

## Architecture Overview

### Additive Scene Loading Approach
This implementation uses a **persistent manager scene** with **additive content scenes**:

1. **SceneTransitionTest.unity** (Persistent Manager Scene)
   - Contains SceneTransitionManager with AnimatedSceneController and UI overlay
   - Never unloads - persists throughout the entire flow
   - Manages scene transitions

2. **Scene_Riding_Stop.unity** (Additive Content Scene)
   - Loads additively when test starts (auto-load enabled)
   - Contains character with path that ends (Stop behavior)
   - Unloads when transitioning to next scene

3. **Scene_Riding_Looping.unity** (Additive Content Scene)
   - Loads additively when transition occurs
   - Contains looping character animation

**Benefits**:
- UI overlay and managers persist between scene changes
- Clean separation of systems (manager) and content (gameplay scenes)
- No need to duplicate managers across scenes
- Easier to extend with more scenes in sequence

## Components Overview

### 1. CharacterPathFollower (Already Exists)
- **Location**: `Assets/_Project/Features/Character/Scripts/CharacterPathFollower.cs`
- **Key Event**: `OnPathComplete` (UnityEvent)
  - Fires when `EndBehavior` is set to `Stop` and character reaches end of path (progress = 1.0)
  - Located at line 95, exposed as property on line 203
- **Required Settings**:
  - `EndBehavior` = `Stop`
  - `AutoStartFollowing` = `true` (to start automatically)
  - Path reference must be assigned

### 2. AnimatedSceneController (Already Exists)
- **Location**: `Assets/_Project/_Core/Scripts/AnimatedSceneController.cs`
- **Key Method**: `TransitionToNextScene()` - triggers transition to next scene in list
- **Required Components**:
  - UIDocument component (for fade overlay)
  - Scene list configured with scenes in order
  - Transition settings (fade duration, auto-transition delay, etc.)
- **Scene Load Mode**: **Additive** (loads new scene while keeping manager scene)

### 3. UI Overlay (Already Exists)
- **UXML**: `Assets/_Project/Features/UI/SceneTransitionOverlay.uxml`
- **USS**: `Assets/_Project/Features/UI/SceneTransitionOverlay.uss`
- Contains `fade-overlay` element required by AnimatedSceneController

### 4. Automated Setup Script (Created)
- **Location**: `Assets/_Project/Editor/Scripts/SceneTransitionTestSetup.cs`
- **Menu Items**:
  - `Tools → Scene Transition Test → Create Test Scene` - Creates fully configured test scene
  - `Tools → Scene Transition Test → Add Scenes to Build Settings` - Adds scenes to build
  - `Tools → Scene Transition Test → Open Test Scene` - Opens the test scene

## Automated Implementation (Recommended)

### Quick Start - Use the Editor Menu
The editor script automatically handles all setup!

1. **Create the test scene**:
   - Menu: `Tools → Scene Transition Test → Create Test Scene`
   - This automatically:
     - Creates SceneTransitionTest.unity with SceneTransitionManager
     - Copies character and path from Scene_Riding_Stop
     - Configures AnimatedSceneController (Additive mode, 2 scenes)
     - Sets CharacterPathFollower to Stop behavior with auto-start
     - Wires OnPathComplete event to TransitionToNextScene()
     - Saves the scene

2. **Add scenes to Build Settings**:
   - Menu: `Tools → Scene Transition Test → Add Scenes to Build Settings`

3. **Test**:
   - Menu: `Tools → Scene Transition Test → Open Test Scene`
   - Press Play!

**That's it!** The entire setup is automated.

## Manual Implementation (If Needed)

### Step 1: Create Test Scene
**File**: `Assets/_Project/Scenes/SceneTransitionTest.unity`

**Scene Setup**:
1. Create new scene (File → New Scene → Basic (Built-in))
2. This will be the **persistent manager scene**

#### a. SceneTransitionManager GameObject
**Components**:
- `AnimatedSceneController`
  - Scene List (2 entries):
    - Scene 0: `Scene_Riding_Stop`
    - Scene 1: `Scene_Riding_Looping`
  - Transition Settings:
    - Fade Duration: `0.5s`
    - Scene Load Mode: **`Additive`** ⚠️ **IMPORTANT**
    - Auto Load First Scene: `true` (loads Scene_Riding_Stop on start)
    - Auto Transition: `false`
- `UIDocument`
  - Source Asset: `SceneTransitionOverlay.uxml`
  - Panel Settings: `Assets/_Project/Features/Dialogue/UI/New Panel Settings.asset`

#### b. Character (Copied from Scene_Riding_Stop)
1. Load Scene_Riding_Stop additively
2. Find character with CharacterPathFollower
3. Copy character and its path to SceneTransitionTest
4. Configure CharacterPathFollower:
   - **Path Provider Component**: Assign copied path
   - **Speed**: Keep existing (10.0)
   - **Starting Progress**: `0.0`
   - **End Behavior**: **`Stop`** ⚠️ **CRITICAL**
   - **Auto Start Following**: `true`
   - **Events** → **On Path Complete**:
     - Add event listener
     - Target: `SceneTransitionManager`
     - Function: `AnimatedSceneController.TransitionToNextScene()`

### Step 2: Add Scenes to Build Settings
1. Open File → Build Settings
2. Add scenes in order:
   - SceneTransitionTest.unity
   - Scene_Riding_Stop.unity
   - Scene_Riding_Looping.unity

### Step 3: Test Flow

**Expected Behavior (Additive Mode)**:
1. Play SceneTransitionTest scene
2. AnimatedSceneController auto-loads Scene_Riding_Stop additively (on Start)
   - SceneTransitionTest remains loaded (manager scene)
   - Scene_Riding_Stop loads on top (content scene)
3. Character starts moving along path (auto-start enabled)
4. Character reaches end of path (progress = 1.0)
5. OnPathComplete event fires
6. AnimatedSceneController.TransitionToNextScene() called
7. Fade out animation plays (0.5s)
8. **Scene_Riding_Stop unloads**
9. **Scene_Riding_Looping loads additively**
10. Fade in animation plays
11. Both SceneTransitionTest (manager) and Scene_Riding_Looping (content) are active

### Step 4: Verification Checklist
- [ ] SceneTransitionTest scene has SceneTransitionManager
- [ ] Character and path are in SceneTransitionTest scene
- [ ] Character has CharacterPathFollower with Stop behavior
- [ ] OnPathComplete is wired to TransitionToNextScene
- [ ] AnimatedSceneController is set to Additive mode
- [ ] Auto Load First Scene is enabled
- [ ] All 3 scenes are in Build Settings
- [ ] Character moves along path automatically
- [ ] Character stops at end of path
- [ ] Fade out effect triggers when path completes
- [ ] Scene_Riding_Stop unloads, Scene_Riding_Looping loads
- [ ] SceneTransitionManager persists throughout
- [ ] No errors in console
- [ ] Transition is smooth (no flickering)

## Scene Loading Flow Diagram

```
Start:
  [SceneTransitionTest] (Manager - Persistent)
    ↓ (Auto-load first scene)
  [SceneTransitionTest] + [Scene_Riding_Stop] (Additive)
    ↓ (Character completes path)
  OnPathComplete fires
    ↓
  TransitionToNextScene()
    ↓ (Fade out)
  Unload Scene_Riding_Stop
    ↓
  [SceneTransitionTest] only
    ↓ (Load next scene additively)
  [SceneTransitionTest] + [Scene_Riding_Looping] (Additive)
    ↓ (Fade in)
  Done!
```

## Common Issues & Solutions

### Issue: OnPathComplete Not Firing
**Check**:
- EndBehavior is set to `Stop` (not Loop or PingPong)
- Character actually reaches progress = 1.0
- Path has valid length > 0
- Character is in SceneTransitionTest (not in Scene_Riding_Stop)

### Issue: Fade Overlay Not Visible
**Check**:
- UIDocument has correct UXML assigned
- Panel Settings is configured (sort order, scale mode)
- UI is rendering on top of camera
- SceneTransitionManager is in the persistent manager scene

### Issue: Scene Doesn't Load Additively
**Check**:
- Scene Load Mode is set to `Additive` (not Single)
- Scenes are added to Build Settings
- Auto Load First Scene is enabled on AnimatedSceneController
- Check console for loading errors

### Issue: Manager Scene Gets Unloaded
**Check**:
- Scene Load Mode must be `Additive` (Single mode replaces everything)
- SceneTransitionTest should never be in the scene list (it's the base scene)

### Issue: Character Doesn't Move
**Check**:
- Path Provider is assigned
- Auto Start Following is enabled
- Speed > 0
- Path has valid data (check in Scene view gizmos)
- Character is in the SceneTransitionTest scene (not Scene_Riding_Stop)

### Issue: Duplicate Characters/Paths
**Check**:
- Character and path should be copied TO SceneTransitionTest
- They should NOT remain in Scene_Riding_Stop when running the test
- If using manual setup, ensure you're copying, not referencing

## Files Created/Modified

### New Files:
1. `Assets/_Project/Scenes/SceneTransitionTest.unity` (persistent manager scene)
2. `Assets/_Project/Editor/Scripts/SceneTransitionTestSetup.cs` (automated setup)

### Modified Files:
- `Assets/_Project/Editor/Editor.asmdef` (added Core assembly reference)
- `ProjectSettings/EditorBuildSettings.asset` (added scenes)

### No Code Changes Required:
- All functionality already exists in:
  - CharacterPathFollower.cs
  - AnimatedSceneController.cs
  - UI overlay files

## Implementation Time

### Automated Setup (Recommended):
- Run menu command: 1 minute
- Add to Build Settings: 30 seconds
- Testing: 2-3 minutes
- **Total**: ~5 minutes

### Manual Setup:
- Scene setup: 10-15 minutes
- Testing & debugging: 5-10 minutes
- **Total**: ~20-25 minutes

## Next Steps After Implementation

1. **Test the basic transition flow**
   - Verify character moves and transitions correctly
   - Check fade effects are smooth
   - Ensure no console errors

2. **Verify additive loading**
   - Open Hierarchy during Play mode
   - Confirm SceneTransitionTest persists
   - Confirm Scene_Riding_Stop loads then unloads
   - Confirm Scene_Riding_Looping loads

3. **Optimize if needed**
   - Adjust fade duration/timing
   - Fine-tune character speed for better demo pacing
   - Consider adding audio feedback during transition

4. **Extend the system** (optional)
   - Add more scenes to the sequence (A → B → C → D)
   - Add loading screens between transitions
   - Implement scene transition triggers beyond path completion
   - Add scene-specific setup/teardown logic

## Advanced: Extending the System

Once the basic test works, you can extend it:

### 1. Multi-Scene Sequences
Add more scenes to the sequence:
```
SceneTransitionTest (Manager)
  → Scene_Riding_Stop
  → Scene_Riding_Looping  
  → Scene_Walking
  → Scene_Cutscene
  → etc.
```

### 2. Scene-Specific Logic
Each content scene can have its own trigger for transitioning:
- Path completion
- Timer (auto-transition after X seconds)
- Button press
- Dialogue completion
- Game event

### 3. Loading States
Add intermediate states between scenes:
- Loading screen overlay
- Progress bars
- Tips/hints display

### 4. Scene Data Persistence
Pass data between scenes:
- Player state
- Inventory
- Quest progress
- Save/load system integration
