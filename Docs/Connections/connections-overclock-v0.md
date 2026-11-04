# Connections — Overclock V0

**Scope:** Connections implementation in `Connections.unity` and
`ConnectionsAdvanced.unity`. Packet is unchanged. This is one global player
ability, with no charges, heat, cost, or traffic-generation change.

`overclock <nodeId>` immediately raises the selected node's effective LOAD
capacity to 150% of its serialized base for 15 simulation seconds. One node
may be active at a time. A 60-simulation-second global cooldown starts at
activation, so the active window is included. Invalid syntax, unknown nodes,
busy attempts, and cooldown attempts leave both deadlines untouched. Reset
and scene disable restore base capacity and clear the ability.

The host owns the active node and deadlines. The resource model keeps its
serialized base capacity and exposes effective capacity to the existing
allocator, node LOAD bar, terminal LOAD diagnostics, and recorder snapshots.
After activation and expiry, allocation is recomputed immediately without
advancing WORK or Health. Requested LOAD, required WORK, effective WORK-rate
multipliers, operation timing, and allocation priority do not change. Both
legitimate and hostile connection requests share any newly available LOAD by
the normal proportional rule, never exceeding their own requests.

The permanent tool strip adds OVERCLOCK beside Probe and Scan. It uses their
green `READY`, teal `ACTIVE Ns`, and amber cooldown `Ns` treatment, with
positive remaining simulation seconds rounded up. The affected node's
existing beneficial status stack shows `OVERCLOCKED` and the remaining seconds.
Its authored fourth row allows CLEAN, ISOLATED, INTERCEPT, and OVERCLOCKED to
coexist. Pain and awareness statuses, including OFFLINE and INFECTED, remain
independent. Overclock does not mark infection or assert a diagnosis.

## Validation

Run `Tools/Connections/Run Overclock Witness`, Resource Witness, Tool Readiness
Witness, and Node Status Witness. The Overclock witness exercises both scenes,
10-to-15 capacity under 16 demand, legitimate and hostile proportional shares,
unchanged request and WORK values, immediate LOAD bar and recorder state,
expiry, cooldown, rejected commands, stationary rendering, and reset.

On the implementation branch, Unity 6000.3.11f1 compiled an isolated project
copy. Overclock, Resource, Tool Readiness, and Node Status witnesses passed.
The graphical Tool Readiness witness rendered READY, simultaneous ACTIVE, and
cooldown captures in both scenes at 1280×720 and 1920×1080. The ACTIVE captures
show the affected node's OVERCLOCKED countdown and boosted LOAD denominator.
These are scripted Editor renders, not interactive Play Mode evidence.

In Play Mode at 1280×720 and 1920×1080, confirm the three tool indicators do
not overlap the Health strip, the affected node's countdown stays legible with
OFFLINE/INFECTED and other conditions, and the status vanishes at expiry and
reset. Check `overclock db`, `load db`, `inspect`, terminal prompt focus and
command recall, and node-status placement amid multiple connection labels.
