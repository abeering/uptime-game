# TARGET LEVEL DEFINITION SHAPE

## Example: Company 1 + Hacker 1 + Day 4

```csharp
LevelDefinition BuildCompany1_Hacker1_Day4()
{
    return new LevelDefinition
    {
        // --------------------------------------------------
        // IDENTITY
        // --------------------------------------------------
        levelId = "company1_hacker1_day4",
        companyId = "company1",
        villainId = "hacker1",
        dayNumber = 4,

        title = "Day 4 - Coordinated Service Disruption",
        description = "Traffic is elevated, the company is tense, and the attacker has moved from probing into deliberate pressure.",

        // --------------------------------------------------
        // WIN / LOSS FRAME
        // --------------------------------------------------
        durationTicks = 600,

        failureRules = new FailureRules
        {
            maxGoodTrafficLoss = 18,
            criticalNodeIds = [ "root", "db" ],
            runEndsOnFailure = true
        },

        // --------------------------------------------------
        // BASELINE TRAFFIC PROFILE
        // --------------------------------------------------
        trafficProfile = new TrafficProfile
        {
            autoSpawnEnabled = true,

            startingSpawnIntervalTicks = 8,
            minSpawnIntervalTicks = 5,
            ticksPerSpawnIntervalStep = 180,
            spawnIntervalJitter = 2,

            startingMalwareChance = 0.08f,
            maxMalwareChance = 0.22f,
            malwareChanceRampPerTick = 0.0006f,

            startingPriorityChance = 0.12f,
            maxPriorityChance = 0.18f,
            priorityChanceRampPerTick = 0.0002f,

            minBaseMoveInterval = 1,
            maxBaseMoveInterval = 3,

            minScanDifficulty = 15,
            maxScanDifficulty = 45,

            routeBias = "commerce-heavy" // pseudocode only
        },

        // --------------------------------------------------
        // PACING PLAN
        // --------------------------------------------------
        phases = new[]
        {
            new LevelPhase
            {
                name = "Opening Flow",
                startTick = 0,
                endTick = 120,
                purpose = "Let the player stabilize and read the network.",
                trafficModifier = "normal",
                events = new[]
                {
                    Notify("Ops", "Traffic looks mostly normal. Stay sharp."),
                    Notify("CEO", "We have a promotion running today, so no surprises please.")
                }
            },

            new LevelPhase
            {
                name = "Early Pressure",
                startTick = 121,
                endTick = 220,
                purpose = "Introduce first meaningful stress without overwhelming the player.",
                trafficModifier = "slightly_heavier",
                events = new[]
                {
                    SpawnPattern("villain_probe_wave"),
                    Notify("Villain", "You look busy already.")
                }
            },

            new LevelPhase
            {
                name = "Recovery Window",
                startTick = 221,
                endTick = 280,
                purpose = "Give the player room to recover and prepare.",
                trafficModifier = "lull",
                events = new[]
                {
                    Notify("Ops", "Pressure dropped. Use the window.")
                }
            },

            new LevelPhase
            {
                name = "Primary Attack",
                startTick = 281,
                endTick = 420,
                purpose = "This is the defining stress moment of the level.",
                trafficModifier = "high_pressure",
                events = new[]
                {
                    SpawnPattern("ddos_surge"),
                    SpawnPattern("keyword_threat_mix"),
                    Notify("Villain", "Let’s see what breaks first."),
                    ThresholdWarning("goodTrafficLoss", 0.50f, "CEO", "Why are orders slowing down?")
                }
            },

            new LevelPhase
            {
                name = "Compounding Instability",
                startTick = 421,
                endTick = 520,
                purpose = "The player is now managing fallout, not just the initial attack.",
                trafficModifier = "stressed",
                events = new[]
                {
                    SpawnPattern("infection_followup"),
                    ThresholdWarning("goodTrafficLoss", 0.75f, "CEO", "Customers are screaming."),
                    ThresholdWarning("criticalNodeRisk", 1.00f, "Ops", "Critical infrastructure at risk.")
                }
            },

            new LevelPhase
            {
                name = "Final Push",
                startTick = 521,
                endTick = 600,
                purpose = "Short closing burst that tests whether the player can hold on.",
                trafficModifier = "moderate_but_tense",
                events = new[]
                {
                    SpawnPattern("final_mixed_wave"),
                    Notify("Villain", "Almost had you.")
                }
            }
        },

        // --------------------------------------------------
        // AUTHORED EVENT LIBRARY FOR THIS LEVEL
        // --------------------------------------------------
        eventSet = new[]
        {
            Event("villain_probe_wave")
                .atTicks(140, 156, 170)
                .spawnThreat(kind: "spyware", count: 2)
                .withHigherScanDifficulty(),

            Event("ddos_surge")
                .fromTick(300)
                .forDuration(80)
                .spawnRepeatedThreat(
                    kind: "ddos",
                    everyTicksMin: 4,
                    everyTicksMax: 6,
                    countMin: 10,
                    countMax: 14)
                .sameBatchSource()
                .withKeyword("dragging:6:2"),

            Event("keyword_threat_mix")
                .atTicks(318, 332, 346)
                .spawnThreatMix("worm", "virus")
                .targetRoutes("db", "cache"),

            Event("infection_followup")
                .atTicks(440, 460, 490)
                .spawnThreat(kind: "worm", count: 1)
                .withInfection("spawner"),

            Event("final_mixed_wave")
                .atTicks(540, 552, 566, 580)
                .spawnMixedTrafficPressure()
        },

        // --------------------------------------------------
        // FLAVOR / NARRATIVE VOICE
        // --------------------------------------------------
        narrative = new NarrativeProfile
        {
            companyTone = "frazzled DTC exec energy",
            villainTone = "taunting but controlled",
            opsTone = "grounded warnings",

            introMessages = new[]
            {
                "[CEO] Big day. Keep the site alive.",
                "[Ops] We’ve seen unusual traffic this morning."
            },

            successMessages = new[]
            {
                "[CEO] I don’t know what happened, but we’re still up.",
                "[Ops] Nice hold. That was getting ugly."
            },

            failureMessages = new[]
            {
                "[CEO] This is unacceptable. We’re done here.",
                "[Finance] We can discuss accountability offline."
            }
        },

        // --------------------------------------------------
        // DESIGN INTENT
        // --------------------------------------------------
        notes = new LevelIntent
        {
            thesis = "Day 4 should feel like the attacker is intentionally stacking pressure instead of throwing random noise.",
            playerSkillTest = "Sustain control through a main surge and a messy aftermath.",
            emotionalGoal = "Barely survive while feeling one step behind."
        }
    };
}
```

## What this function is really declaring

This is not just “what spawns when.”

It is declaring:

* **what the day is about**
* **how the pressure breathes**
* **what failure looks like**
* **what the player is expected to handle**
* **what emotional arc the level should produce**

## Minimum fields every authored level should define

If you want the leanest possible version later, every level should still define:

* `levelId`
* `companyId`
* `villainId`
* `dayNumber`
* `title`
* `durationTicks`
* `failureRules`
* `trafficProfile`
* `phases`
* `eventSet`
* `narrative`
* `notes`

## The most important section

If you only preserve one thing from this structure, preserve:

### `phases`

That is where level cohesion lives.

Because a good level is not:

* just spawn settings
* just events
* just flavor

A good level is:

> **a sequence of authored pressure states**

## Litmus test for a cohesive level

When you define a level, you should be able to answer:

* What is this day testing?
* Where does the player get breathing room?
* What is the main overload moment?
* What makes this different from Day 3?
* Why does this belong to this company?
* Why does this belong to this villain?

```
```
