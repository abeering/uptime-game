# Packet Keywords

Packet keywords are modular behavioral traits attached to packets.

They define how packets behave, interact with the network, and interfere with the player.
Keywords should be:
- Composable (multiple per packet)
- Discoverable (via scan / help)
- Mechanically meaningful (no purely flavor keywords)

---

## Design Goals

- Tie behavior directly to readable traits
- Enable emergent gameplay via combinations
- Support `help <keyword>` for player clarity
- Avoid hardcoding behavior into packet types
- Keep systems extensible and data-driven

---

## Behavior Categories

---

### 1. Self Behavior

Affects only the packet itself.

#### Mutating (Obfuscated)
- Changes its ID every N ticks
- Effectively unscannable or unreliable scan results
- Encourages blocking at node instead of inspection

#### Desynced
- Does not follow normal tick timing
- Moves or updates irregularly

#### Invulnerable
- Cannot be blocked
- Must be handled indirectly

---

### 2. Packet Interaction

Affects nearby or related packets.

#### Accelerant
- Speeds up nearby packets
- Can affect benign traffic

#### Drag Field
- Slows down nearby packets
- Can affect benign traffic

#### Chameleon
- Copies traits of nearby packets
- May inherit keywords or appearance

#### Leech *(name TBD)*
- Destroys benign packets it touches

#### Linked
- Two packets share fate
- Destroying one affects the other

#### Carrier
- Spawns alongside another packet
- Same time, same speed

---

### 3. Node / Network Interaction

Affects nodes or network topology.

#### Infectious
- Applies effects to nodes

##### Infection Types

###### Spawning
- Node begins spawning threats
- May spawn in reverse direction

###### Blocking
- Node blocks normal packets

###### Throttling *(Delayer?)*
- Increases latency at node
- Can sometimes be beneficial (traffic control)

###### Degrading
- Reduces node efficiency

Examples:
- Firewall degraded → slower scans
- API degraded → reduced visibility / identity

###### Bridging
- Connects two unconnected nodes
- Allows threats to traverse new paths

---

### 4. Player / System Disruption

Affects player tools, UI, or command capabilities.

#### Jammer
- Disables scanning for N ticks

#### Scrambler
- Scrambles console output if scanned

#### Cloak *(Hidden / needs name)*
- Hides nearby packets
- May be fully invisible

---

### 5. Event / Triggered Behavior

Triggers on specific actions or events.

#### Volatile
- Splits into multiple packets if scanned

#### Echo
- Creates a fake duplicate packet

#### Floodgate
- Moves slowly
- Collects packets
- Releases all at once

---

## Additional Concepts

### Needs Clean
- Certain threats may require a `clean <node>` action instead of block
- Could tie into infection system

---

## Recommended First Implementation

To validate the keyword system, implement:

### 1. Mutating
- Tests self-contained behavior
- Introduces uncertainty

### 2. Accelerant
- Tests packet-to-packet interaction
- Introduces spatial gameplay

### 3. Jammer
- Tests player/system disruption
- Forces timing decisions

---

## Future Expansion Ideas

- Keyword stacking rules (do effects stack or override?)
- Keyword rarity / weighting
- Hidden vs revealed keywords (scan depth)
- Visual language per keyword (color, animation, distortion)
- Keyword synergies (e.g. Mutating + Jammer)

---

## Open Questions

- Can packets have unlimited keywords?
- Should some keywords be mutually exclusive?
- Are keywords always revealed on scan?
- Should infections be keywords or a separate system?
- How does `help <keyword>` scale with partial information?

---