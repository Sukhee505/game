# 5 DAKİKA: EV (5 MINUTES)

A 3D third‑person horror escape game made in Unity (URP, C#). You wake up in a locked bedroom in a two‑storey house. A hacked TV tells you that you have ten minutes, but you only get **5:00**. Escape with the help of **UNIT‑7**, your robot companion.

---

## 1. How to open and run the game

1. Open the project (`C:\sukhe\unity project\game`) in **Unity 6** (tested target: 6000.6.x LTS line, URP).
2. Wait until the scripts finish compiling. The Console should show no red errors.
3. Put your intro video at **`Assets/Media/IntroductionVideo.mp4`** (see section 3).
4. From the top menu, choose **5 MINUTES ▸ Build Game Scene**.
   - This generates the whole game into `Assets/SevenMinutes/Scenes/SevenMinutes_TheHouse.unity`. That includes the house, puzzles, traps, characters, lighting, audio and UI.
   - It also makes that scene the first one in the build. Older scenes stay in the list but are disabled.
5. Press **Play**.

You can run the menu again at any time to regenerate the scene. For example, run it after adding or replacing the video.

## 2. Recommended Unity version

Use Unity **6 (6000.x)** with the **Universal Render Pipeline**, which this project already uses. Required packages are Input System, uGUI/TextMeshPro and URP. The project already contains all of them.

## 3. Intro video

- Copy your video to `Assets/Media/IntroductionVideo.mp4`, then run **5 MINUTES ▸ Build Game Scene**. The clip is connected to the TV automatically.
- To connect it manually, select **House ▸ Level1_Bedroom ▸ Television**. Then drag the clip into **TelevisionController ▸ Intro Clip**.
- Alternative: put the file into `Assets/StreamingAssets/IntroductionVideo.mp4`. It is used when no clip is assigned.
- The video plays once on the TV screen. It keeps its aspect ratio (Fit Inside) and uses its own audio. When it ends, the 5:00 countdown starts.
- If no video is found, the TV shows a placeholder that explains where to put the file. The game then continues normally.

## 4. Controls

| Action | Key |
|---|---|
| Move | W A S D |
| Run | Left Shift |
| Jump | Space |
| Crouch (hold) | Left Ctrl |
| Interact / read / talk to the robot | E |
| Look | Mouse |
| Pause | Esc |
| Keypad | number keys + Enter (or click) |
| Computer terminal | type + Enter |

## 5. Game flow

1. **Opening.** The character wakes up in bed, sits up and looks around. The robot boots up.
2. **TV (15 s after waking).** The lights flicker, then static, then glitches, then your video. The TV dies and **05:00** starts.
3. **Level 1, bedroom.** Search the drawers, cabinet and wardrobe. The real note sits at a random one of 4 locations each game. The fake notes mislead you. The note's equations give **6674** for the keypad.
4. **House maze.** Upstairs hallway, a dead‑end corridor, a closet, and Easter egg room **A** (photo, newspaper, child's diary). The stairs lead down. Downstairs there are locked doors, a barred window that shows the garden, and Easter egg room **B** (workstation with the final door code, plus the prototype robot head).
5. **Level 2, memory floor (4 × 10 tiles).** The button shows the safe snake path for about 2 s. A wrong tile collapses and you fall back to the room start. Reaching the end enables the lever. The lever opens the door, and the robot comes around through its catwalk.
6. **Level 3, lava pit.** The robot crosses through a maintenance duct that only it fits through. Type **HELP** on the computer. The robot answers "Okay.", walks to the red button and presses it, and the bridge extends. Cross the bridge and pull the lever.
7. **Level 4, trap corridor.**
   - A swinging spiked ball.
   - Crushing walls (open 4 s → close 0.8 s → closed 0.8 s → open 0.8 s).
   - Three lasers: jump the low one, crouch under the high one, and pass the moving one when it is up.
   - Any hit sends you back to the corridor start.
8. **Final door.** Talk to the robot. It says "Understood. I'll activate the power system." and stands on the heavy plate. Enter **★ ▲ ● ■** (Star, Triangle, Circle, Square) on the console. The bolts retract, the robot says "We did it. Let's get out of here.", and the door opens.
9. **Garden.** Walk through the door. A cinematic plays, then the victory screen shows your stats.

At **00:00** the game freezes, fades to black and shows **TIME IS UP**, with a Restart button.

## 6. Changing the timer

Select **GameSystems ▸ GameTimer ▸ Duration Seconds** (default `300` = 5:00). Re-running the builder resets it, so for a permanent change edit the default in `Scripts/Game/GameTimer.cs`.

The TV delay (15 s) is set in **GameSystems ▸ IntroSequence ▸ Tv Delay After Waking**.

## 7. Adjusting the puzzles

| Puzzle | Where |
|---|---|
| Keypad code / note text | `Scripts/Puzzles/MathPuzzle.cs` (`code`, `Secret`, `FalseNotes`) |
| Secret note locations | `HouseBuilder.Upstairs.cs` → `candidates` list |
| Safe tile path / preview time | `Scripts/Puzzles/TileMemoryPuzzle.cs` (`SafePath`, `previewSeconds`) |
| Words accepted by the robot | `Scripts/Puzzles/LavaBridgePuzzle.cs` → `OnMessage` |
| Final symbol sequence | `Scripts/Puzzles/FinalDoorPuzzle.cs` → `sequence` (also update the workstation text and texture in the builder) |
| Trap timings | `SwingingBallTrap`, `CrushingWallsTrap`, `LaserTrap` components (Inspector) |

## 8. Robot behaviour

- `Scripts/Robot/RobotController.cs` handles NavMesh follow distance, the stuck recovery (re-path, then an off-screen warp) and commands (`MoveTo`, `WaitAt`, `PressButton`, `Follow`).
- `Scripts/Robot/RobotRig.cs` handles the procedural walk, the head tracking, the glowing eyes and the button-press arm.
- `Scripts/Robot/RobotHintSystem.cs` holds the context hints: automatic ones when you are stuck, and immediate ones when you press **E** on the robot.
- `Scripts/Game/LevelDirector.cs` decides where the robot goes when each level starts.
- The NavMesh is baked at runtime (`RuntimeNavMeshBaker`). The robot is small, so it fits through the 1.08 m maintenance ducts that the player cannot use.

## 9. Building a Windows executable

1. Go to **File ▸ Build Profiles** and choose **Windows**.
2. Check that `Assets/SevenMinutes/Scenes/SevenMinutes_TheHouse.unity` is the first enabled scene. The builder does this for you.
3. Click **Build** and choose an output folder, for example `Build/`. Then run `5 DAKIKA - EV.exe`.

## Architecture

```
Assets/SevenMinutes/
  Scripts/Core         interfaces (IInteractable, IHazard, IPlayerTrigger, ISteppable), input, settings, NavMesh bake
  Scripts/Game         GameManager (state machine), GameTimer, CheckpointManager, LevelDirector, Victory/Defeat
  Scripts/Player       PlayerController, PlayerCamera, PlayerInteraction, HumanoidRig
  Scripts/Robot        RobotController, RobotRig, RobotDialogue, RobotHintSystem, RobotInteraction
  Scripts/Puzzles      MathPuzzle, Keypad, SearchableContainer, TileMemoryPuzzle, LavaBridgePuzzle, FinalDoorPuzzle...
  Scripts/Traps        SwingingBallTrap, CrushingWallsTrap, LaserTrap
  Scripts/Environment  DoorController, LeverController, PushButton, zones, lights, lava
  Scripts/UI           HUD, menus, keypad, terminal, notes, screen effects
  Scripts/Audio        AudioManager, ProceduralAudio (all sounds are synthesised), TensionDirector
  Scripts/Intro        IntroSequence, TelevisionController
  Editor               HouseBuilder (generates the scene, materials and textures)
  Generated/           textures, materials, post-processing profile (created by the builder)
```

Game states are **MainMenu → Introduction → Playing ⇄ Paused → Victory / Defeat**. The timer can only start when the TV video ends. It stops on victory and defeat, and it freezes while paused.

### Placeholders and limitations

- The characters, furniture and house are built from primitives. The human and the robot move with code-driven (procedural) animation, and there are no imported models.
- All sound effects and music are synthesised at runtime. You can swap any clip in `AudioManager.Generate()`.
- Lighting is realtime, not baked, to keep the scene fully generated.
