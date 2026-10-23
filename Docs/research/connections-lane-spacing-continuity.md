# Connections — lane spacing continuity (parked research)

**Status:** Parked idea, not an approved implementation task (October 22, 2026).  
**Scope:** Connections prototype presentation only. No changes to Packet, simulation, LOAD, WORK, or gameplay rules.

## Observation

Route-aware lane ordering in [PR #12](https://github.com/abeering/uptime-game/pull/12) made a substantial improvement in Play Mode. In the five reviewed network screenshots, most arrangements already looked good. One ordinary case still looked slightly wrong: four connections shared **EXT1 → API (E1)**, then split into two connections toward **DB (E2)** and two toward **AUTH (E3)**. The pairs stayed intelligently ordered, but appeared markedly farther apart after leaving API.

The suspected discontinuity is primarily **spacing**, not merely line width. The reader's eye expects adjacent connections traveling together through a node to retain some physical relationship. A sudden change in separation can make a connection seem less continuous, even when its ID and lane ordering are correct.

Existing geometry and identity packing can choose larger line-to-line gaps on relatively empty edges to make room for centered ID pills. That may contribute to this case; it has **not** been isolated as the sole cause.

## Intent and constraints

- Target ordinary gameplay of roughly **5–6 simultaneous logical connections**. Preserve dense-traffic fallbacks, but do not sacrifice the normal presentation to theoretical maximum occupancy.
- **Keep beams straight** along each physical link. Do not introduce curved, bent, or gradually diverging segments to smooth a transition.
- Preserve the good occupancy language: **quiet links should look spacious, busy links should look packed**. A universal minimum or tight maximum gap is not the desired fix.
- Avoid making most layouts worse to fix one uncommon-looking arrangement. The present behavior is acceptable enough to leave alone.
- Connection identity pills are annotations. Explore whether they should be placed *after* choosing good line geometry, rather than determining that geometry primarily to accommodate labels.
- Width, gap, and ordering are different variables; the visual relationship of two adjacent beams may be best measured by **lane pitch** (center-to-center spacing, approximately width + gap for equal-width beams).

## Possible direction — not a specification

A proposed sequence for eventual exploration:

1. **Position/order:** Determine route-aware lane order and placement based on the active network (Phase 1 / PR #12).
2. **Size:** Choose connection widths appropriate to occupancy and available physical-link space.
3. **Gap:** Choose attractive, occupancy-responsive spacing, with a possible *soft* preference for continuity between adjacent connections on neighboring links.
4. **Pills:** Place ID annotations on the resulting geometry; center, stagger, overlay, or rotate as needed instead of automatically spreading the underlying beams.

Width and gap may need to be evaluated together; this sequence describes **priority**, not necessarily four independent solvers.

The smallest continuity hypothesis is: **calculate each physical link's preferred spacing in isolation, then gently bias spacing when the same pair of connections is adjacent across a node.** For example, two connections packed together before a split might leave slightly less separated than the isolated two-connection layout would choose. The bias should not require equal spacing, nor should it compress a quiet route that was already naturally spacious.

Open questions: whether incoming and outgoing links should influence each other symmetrically; how to handle joins and multiple neighbors without oscillation; how to retain straight, parallel lanes and reasonable card-edge attachments; and whether staggered ID pills can remain legible when they no longer drive line separation.

## If revisited

Compare against the untouched current presentation before accepting any change. Useful cases:

- A quiet one- or two-connection route: retain generous, attractive spacing.
- Four on E1 splitting 2+2 across E2/E3: reduce the odd discontinuity without losing the branch separation.
- Typical five- or six-connection traffic with arrivals, completions, and blocks: avoid jitter or frequent unnecessary reshuffles.
- Reverse routes, merges, and dense edge fallback: retain correct ordering, widths, legible pills, and node attachments.
- Verify the visual result in 16:9 **Play Mode**, not solely geometry tests. Inspect pill tracking, command focus, and the existing movement.

**Decision:** No immediate work. PR #12's ordering improvement can stand on its own. Revisit spacing only if this discontinuity continues to distract during playtesting or a small, reversible, measured experiment becomes clear. Subtle per-connection color tests remain a separate possible presentation phase.
