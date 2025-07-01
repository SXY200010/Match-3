# Match Quest: Fruit Crush

**Author:** Xiaoyu Shi  
**Submission:** Assignment One - Part Three  
**Platform:** Unity (WebGL or Editor Play Mode)

---

## Game Overview

**Match Quest: Fruit Crush** is a 2D match-3 puzzle game built with Unity. Players swap adjacent fruits to form horizontal or vertical matches of three or more identical fruits. Matched fruits are cleared and replaced through cascading logic. The game ends when no more valid moves are available or the player chooses to end the game manually.

---

## Core Features

- Classic match-3 swapping logic
- Cascading matches with continuous scoring
- 3D particle effect upon matching
- JSON-based save/load system
- Pause menu with volume control and "End Game" confirmation
- Name input on game over for score record
- Persistent top score display system

---

## Controls

- **Mouse Click + Drag:** Swap two adjacent fruits
- **ESC Key:** Open or close pause menu
- **Pause Menu Options:**
  - **Save:** Save current game state to file
  - **End Game:** Submit score and return to main menu
  - **"Quit" Button:** Returns to the main menu.
  - **Volume Slider:** Adjust background music volume

---

## Main Menu

The main menu includes the following options:

- **New Game**: Clears existing save data and starts a new game (`GameScene`)
- **Load Game**: Loads previously saved board state from file if available
- **Setting**: Opens a panel to adjust music volume using a slider
- **Records**: Opens the leaderboard panel showing previously recorded scores with player names (stored locally)
- **Quit**: Exits the game (effective only in a built version, not in the Unity editor)

---

## Save System

The game uses a **file-based save system** (`savegame.json`) implemented in `SaveSystem.cs`. It serializes the current score, grid layout, and available fruit types. Data is stored in `Application.persistentDataPath` and loaded automatically at the start of the game if a save file exists.

---

## Scoring System

| Match Type | Points Awarded |  
|------------|----------------|  
| 3 Fruits   | 30             |  
| 4 Fruits   | 60 (no special effect) |  
| 5+ Fruits  | 100 (no special effect) |

> ⚠️ Special fruit logic (e.g., Row Blaster, Color Bomb) is **not implemented** in this version.

---

## Game Objective

Match and clear as many fruits as possible to increase your score.  
The game ends in either of the following situations:
- No more valid moves left
- Player manually ends the game via the pause menu

Upon ending the game, the player can enter their name to save the score. A leaderboard panel displays top scores in descending order.

---

## Project Structure

## 📂 Project Structure

- `Assets/Scripts/`: Game logic, UI control, save/load, particle control, leaderboard system
- `Assets/Scenes/`: Main scenes including `GameScene` and `Mainmenu`
- `Assets/Simasart/Hungrybat/Prefabs/`: Fruit prefabs from external asset package
- `Assets/Effect/`: Particle prefabs and textures for match effects
- `Assets/Resources/`: Contains glow material (`GlowMaterial`) for selected fruits
- `Assets/Audio/`: Background music and sound effects (`swapSound`, `matchSound`)
- `Assets/Shader/`: Custom shaders (if any used for visual effects)
- `Persistent Data Path`: Stores `savegame.json` (game board save data) and leaderboard (`PlayerPrefs` JSON)

---

## How to Run

1. Open the project in Unity 2022.x or newer
2. Open the scene named `GameScene.unity`
3. Click ▶️ to run or use **File → Build and Run (WebGL)**
4. Use the pause menu (ESC key) to save or end the game
5. On game over, enter your name to record your score

---

## Known Issues

- Special fruit effects (e.g., row/column clear) are planned but not implemented
- No tutorial or move suggestions system
- Quit game doesn't work in WGL build

---

