# BrickBloom MR

> **Finish a small brick model on your real desk, using only your hands.**
>
> Devpost story draft · Tagline: 68 characters · Story target: under 500 words

## Project at a glance

| Platform | Play style | Session |
| --- | --- | --- |
| Quest 3 | Hands-only, seated mixed reality | Offline |

| Board | Pieces | Goal |
| --- | --- | --- |
| 8 × 8 studs | Three models of 6, 7 and 9 bricks: 2×2 and 2×4 | Complete each ghost model |

## Project screenshots

![Pinch interaction with a virtual brick](screenshots/01_Hand_Tracking.png)

*Pinch interaction with a virtual brick. The tracked hand, placement targets, and progress indicators are visible in the mixed reality scene.*

![Completed brick structure](screenshots/02_Level_Complete.png)

*The completed brick structure is shown with gold progress indicators and a level-completion prompt.*

---

## Inspiration

Hand practice can feel like a list of motions with no finish line: pinch, reach, turn, release, repeat. We wanted to wrap those familiar movements in a small, satisfying task. Building a model gives each reach and grasp a clear purpose, visible progress, and a moment of completion.

## What it does

BrickBloom MR is an offline, seated, hands-only building game for Quest 3. On an 8 × 8 board, players match red, white, blue, and yellow 2×2 and 2×4 bricks to a translucent guide. A brick snaps into place only if its target is empty, the layer below supports it, its color and rotation match, and it is released close enough to the target. Otherwise, it returns to the tray. Progress pips and a completion celebration make the result clear.

Players can build three models in sequence: a six-brick bridge, a seven-brick pyramid, and a nine-brick castle. Reset clears the board for another attempt. The app requires no controllers, account, or network connection.

## How we built it

We built the Quest experience in Unity with Meta XR hand interaction and passthrough. The placement rules—support, rotation, ownership, and snap distance—live in plain C# without engine references, so NUnit Edit Mode tests can exercise them independently of the headset. The XR layer reports a release pose; the board applies the same deterministic rules every time.

## Challenges we ran into

Hand tracking makes a precise release harder than a mouse click. We kept placement forgiving but deliberate, with explicit color, rotation, support, and distance checks, and added a preview to show the likely target before release. Testing also exposed a reset edge case: a held hand interaction could keep moving a piece after reset. We fixed the release flow so reset returns the piece to the tray reliably.

## Accomplishments that we're proud of

All three models have been completed on Quest, including advancing from one model to the next and resetting for another build. The experience now includes hand grabbing, placement feedback, progress indicators, a how-to-play sign, and focused tests for the core rules. We also built a non-development release APK; its headset installation and final release validation are still ahead.

## What we learned

A rule can be correct in code and still feel difficult in a headset. Reach, hand pose, feedback, and control placement all matter. We learned to validate the full build loop with real hands, then adjust the interaction without weakening the rules that make a build stable.

## What's next for BrickBloom MR

Next, we want to install and verify the release build, refine any remaining interaction friction, and explore models with shorter reaches or different piece sizes. A future coach could explain a rejected placement and suggest a legal next brick; the player would still do the building.

BrickBloom MR is a game, not a medical device, and does not measure recovery.
