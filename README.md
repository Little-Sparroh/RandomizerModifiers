# RandomizerModifiers

A BepInEx mod for Mycopunk that re-enables two vanilla mission modifiers that were disabled (spawn weight set to 0):

- **Split Personality** — randomizes your employee/character on revive
- **Butter Fingers** — randomizes your weapons on revive and locks gear switching for the mission

## Features

- Restores spawn weights for the existing vanilla modifiers (no reimplementation needed)
- Identifies modifiers by their UnityEvent method bindings (`RandomizeCharacterOnRevive` / `RandomizeGearOnRevive`)
- Configurable enable toggles and weights
- Optional debug dump of the full mission modifier pool

## Dependencies

* Mycopunk (base game)
* [BepInEx](https://github.com/BepInEx/BepInEx) - Version 5.4.2403 or compatible
* .NET Framework 4.8

## Building

```bash
dotnet build --configuration Release
```

Output: `bin/Release/net48/RandomizerModifiers.dll`

## Installing

**Via Thunderstore / r2modman (recommended)**  
Install the package normally.

**Manual**  
Place `RandomizerModifiers.dll` in:

```
<Mycopunk>/BepInEx/plugins/
```

## Configuration

Config file: `BepInEx/config/sparroh.randomizermodifiers.cfg`

| Section | Key | Default | Description |
|---|---|---|---|
| General | EnableSplitPersonality | true | Re-enable Split Personality |
| General | EnableButterFingers | true | Re-enable Butter Fingers |
| Weights | SplitPersonalityWeight | 1 | Relative roll weight (vanilla used 0 to disable) |
| Weights | ButterFingersWeight | 1 | Relative roll weight (vanilla used 0 to disable) |
| Debug | LogModifiersOnLoad | false | Log all modifier API names/weights/methods on load |

## Multiplayer

Mission modifiers are synced as **indices** into the shared modifier pool. Host and clients should all run this mod so rolls stay consistent. Weights only affect which modifiers get selected when missions refresh.

## Help

* **Modifiers still never appear?** Check the BepInEx log for successful weight changes. Raise the weights, or enable `LogModifiersOnLoad` to confirm they were found.
* **Could not find modifier?** Enable `Debug.LogModifiersOnLoad` and check which entries expose `RandomizeCharacterOnRevive` / `RandomizeGearOnRevive`.
* **Mod not loading?** Verify BepInEx is installed and check the console for errors.

## Authors

- Sparroh

## License

This project is licensed under the MIT License - see the LICENSE file for details
