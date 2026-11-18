# Connections Surprise V0 — review

This review is for the Connections implementation branch only. Packet scenes and mechanics were not changed.

## What changed

The shared planner fixes a concrete consequence plan when a Duel is scheduled. Its default pool mixes one atomic infection and three compound Tactics: Breach & Lockdown, Condition Worm outbreak, and Payload Burst. Each effect attempts through an existing authoritative system; partial outcomes and rejections are recorded. The Advanced hostile opportunity mix can now select a direct Duel or a killable connection that schedules a Duel on operation completion. Both new weights remain zero in the four-node scene.

The temporary CLEAN LOCKED Condition rejects new CLEAN attempts for 15 simulation seconds and appears in the Pain stack with its remaining time. The four-row authored Pain panel gives INTRUSION and CLEAN LOCKED priority; in an extreme simultaneous stack, lower-priority statuses can be hidden until it thins. This is a presentation limit to inspect in Play Mode, not an omitted game effect.

## Unity evidence

Unity 6000.3.11f1 was run against an isolated copy of the branch because the primary project was open in the Editor. The following Edit Mode witnesses passed after the implementation changes:

- `ConsequenceWitness`: configuration modes, override/exclusion, target and weight rejection, seeded selection, frozen targets, independent and single execution, stale-plan reset rejection, partial Breach result, lock lifecycle and existing CLEAN, Worm creation/isolation, Payload launch/interception/delivery, direct and carrier Duel activity, killed carrier, and recorder fields.
- `DuelWitness`, `ConditionWitness`, `WormWitness`, `PayloadWitness`, `InfectionWitness`, `TrafficWitness`, `AmbientHostileActivityWitness`, `RecorderWitness`, and `AdvancedSceneWitness` passed. The ambient witness was updated to set both new Duel weights to zero when asserting the prior selector's zero-total case.

`SurpriseCapture` ran in scripted Advanced Play Mode at 1920×1080 and 1280×720, with generated benign traffic enabled and an ordinary benign plan queued alongside each forced Tactic. Each image shows one direct execution of the shared plan on API; these images do not represent a physical-keyboard Duel win or loss. Six frames:

| Tactic | 1920×1080 | 1280×720 |
| --- | --- | --- |
| Breach & Lockdown | [capture](Captures/breach-1920x1080.png) | [capture](Captures/breach-1280x720.png) |
| Condition Worm outbreak | [capture](Captures/worm-1920x1080.png) | [capture](Captures/worm-1280x720.png) |
| Payload Burst | [capture](Captures/burst-1920x1080.png) | [capture](Captures/burst-1280x720.png) |

## Manual review in the primary Unity project

Open `Assets/Scenes/ConnectionsAdvanced.unity` at 1920×1080 and 1280×720 in Play Mode with generated traffic. Let hostile opportunities run; watch for a direct INTRUSION and a hostile carrier connection. Kill a carrier before its operation finishes and confirm no Duel starts. After a carrier completes and its Duel is scheduled, killing the carrier should not clear the countdown.

Force each loss outcome without waiting for the weighted pool: `duel API 2 breach`, `duel API 2 worm`, and `duel API 2 burst`. Lose the race, then try CLEAN during and after the lock, isolate an outbreak node, and intercept a red Payload. `duel API 2 infection` exercises the atomic case; `duel API 2 default` exercises the weighted pool. Confirm the result receipt does not disclose covert infection identity. Inspect and issue another terminal command after each Duel to check focus restoration. Repeat with an already running CLEAN and with both simulation continuation policies.

Review node-status crowding on API with INTRUSION, CLEAN LOCKED, INFECTION knowledge, BACKDOOR/SCAN JAMMED and OFFLINE together; the first four Pain rows are shown by priority. Also review Worm and Payload overlap, path labels under ordinary traffic, and the command prompt at both resolutions. The captures verify scripted visuals and runtime state; human keyboard playtesting remains outstanding.
