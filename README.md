# RevivalMod for SPT-AKI

## Overview
RevivalMod is a client-side mod for SPT-AKI (Single Player Tarkov) that adds a revival mechanic to the game. Instead of immediately dying when taking lethal damage, players enter a critical state and can use a designated medical item to revive themselves.

## Features
- **Critical State System**: Instead of dying instantly from lethal damage, players enter a critical state
- **Manual Revival**: Press F5 while in critical state to use your defibrillator/medical item and revive yourself
- **Temporary Invulnerability**: After revival, enjoy 10 seconds of invulnerability to get to safety
- **AI Avoidance**: Bots will temporarily ignore you while in critical state
- **Visual Indicators**: Clear notifications when entering critical state and during revival
- **Cooldown System**: 3-minute cooldown between revivals for balance

## Requirements
- SPT-AKI (compatible with latest version)
- BepInEx

## Installation
1. Download the latest release from GitHub
2. Extract the `RevivalMod.dll` file to your `{SPT-AKI folder}/BepInEx/plugins/` directory
3. Start the game and enjoy!

## Configuration
The mod includes some configurable options in the `Constants.cs` file:

```csharp
// Change this ID to a defibrillator if available, or keep it as a bandage for testing
// Defibrillator ID from Escape from Tarkov: "60540bddd93c884912009818"
// Personal medkit ID: "5e99711486f7744bfc4af328"
// CMS kit ID: "5d02778e86f774203e7dedbe"
// Bandage ID for testing: "544fb25a4bdc2dfb738b4567"
public const string ITEM_ID = "544fb25a4bdc2dfb738b4567"; // Using bandage for testing purposes

// Set to true for testing without requiring an actual defibrillator item
public const bool TESTING = false;
```

You can modify the following settings:
- `ITEM_ID`: Choose which item will function as the revival item
- `TESTING`: Set to `true` if you want to test without consuming any items

Additional settings in `Features.cs`:
- `INVULNERABILITY_DURATION`: Duration of invulnerability after revival (default: 10 seconds)
- `MANUAL_REVIVAL_KEY`: Key to trigger manual revival (default: F5)
- `REVIVAL_COOLDOWN`: Cooldown between revivals (default: 180 seconds/3 minutes)

## How to Use
1. Make sure you have the required revival item in your inventory (default: bandage for testing)
2. When taking lethal damage, you'll enter critical state instead of dying
3. A notification will appear instructing you to press F5 to use your revival item
4. After pressing F5, you'll be revived with:
   - Full health restoration
   - Negative effects removed
   - 10 seconds of invulnerability
   - Energy and hydration restored

## Known Issues
- Visual effects for critical state are currently disabled in code
- Some post-revival visual effects may not work perfectly in all conditions

## Credits
- Created by KaiKiNoodles
- Based on the SPT-AKI Client Mod Examples

## License
This project is licensed under the MIT License - see the LICENSE file for details.

## Contributing
Contributions are welcome! Feel free to submit pull requests or open issues on the GitHub repository.

## Changelog
- v1.0.0: Initial release
  - Added critical state system
  - Added manual revival feature
  - Added temporary invulnerability
  - Added AI avoidance during critical state
