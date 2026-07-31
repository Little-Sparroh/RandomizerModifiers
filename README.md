# RandomizerModifiers

A BepInEx mod for Mycopunk that re-enables two vanilla mission modifiers that were disabled (spawn weight set to 0):

- **Split Personality** — randomizes your employee/character on revive
- **Butter Fingers** — randomizes your weapons on revive

## Features

- Restores spawn weights for the existing vanilla modifiers (no reimplementation needed)
- Identifies modifiers by their UnityEvent method bindings (`RandomizeCharacterOnRevive` / `RandomizeGearOnRevive`),
  with API name fallbacks
- Configurable enable toggles and spawn weights (0–100)
- Optional debug dump of the full mission modifier pool

## Dependencies

- Mycopunk (base game)
- [BepInExPack_Mycopunk](https://thunderstore.io/c/mycopunk/p/BepInEx/BepInExPack_Mycopunk/) — Version 5.4.2403 or
  compatible

## Building

```bash
dotnet build --configuration Release
```

Output: `bin/Release/netstandard2.1/RandomizerModifiers.dll`

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

| Section | Key                      | Default | Description                                                                 |
|---------|--------------------------|---------|-----------------------------------------------------------------------------|
| General | Enable Split Personality | true    | Re-enable Split Personality (randomize employee/character on revive)        |
| General | Enable Butter Fingers    | true    | Re-enable Butter Fingers (randomize weapons on revive)                      |
| Weights | Split Personality Weight | 1       | Spawn weight for Split Personality (vanilla used 0 to disable; range 0–100) |
| Weights | Butter Fingers Weight    | 1       | Spawn weight for Butter Fingers (vanilla used 0 to disable; range 0–100)    |
| Debug   | Log Modifiers On Load    | false   | Log every mission modifier API name, weight, and methods when Global loads  |

## Multiplayer

Mission modifiers are synced as **indices** into the shared modifier pool. Host and clients should all run this mod so
rolls stay consistent. Weights only affect which modifiers get selected when missions refresh.

## Help

- **Modifiers still never appear?** Check the BepInEx log for successful weight changes. Raise the weights, or enable
  `Log Modifiers On Load` to confirm they were found.
- **Could not find modifier?** Enable `Log Modifiers On Load` and check which entries expose
  `RandomizeCharacterOnRevive` / `RandomizeGearOnRevive`.
- **Mod not loading?** Verify BepInEx is installed and check the console for errors.

## Authors

- Sparroh

## License

This project is licensed under the MIT License — see the LICENSE file for details.
