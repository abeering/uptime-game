# Uptime documentation

Uptime is one game design explored through two independent gameplay prototypes: Packet and Connections. Both remain playable and neither is declared the final implementation.

## ChatGPT and Codex workflow

**ChatGPT is the design, planning, and review workspace; Codex/local Unity is the implementation workspace.** This governs collaboration on Uptime, not any gameplay mechanic.

- **In ChatGPT:** Research the current repository read-only as needed, discuss alternatives, agree on a small design contract, prepare review criteria, and review Codex PRs, test results, and playtest evidence. Do not implement gameplay code, author scenes/prefabs, create branches or implementation PRs, or run a development workflow from this conversation by default, even when GitHub write tools are available.
- **Handoff directly to Codex:** Once a design is approved, provide a **complete, copy-ready Codex implementation prompt in the chat itself**. Do not create GitHub issues/tickets as handoffs, ask Codex to work from an issue instead of a prompt, or substitute a vague task summary or external link for the prompt. The user takes that prompt to Codex.
- **Codex/local Unity:** Own code and scene changes, implementation documentation, tests and Unity validation, feature branches, commits, and unmerged PRs. Return branch/commit, exact changes, checks actually run, caveats, and manual Play Mode review instructions to this discussion.
- **Approval is not implementation authorization:** Replies such as “sounds good,” “agreed,” or “let's continue” authorize the next design or Codex handoff step, **not** repository writes from ChatGPT. Read/review GitHub material here freely, but take GitHub write actions (for example a specifically requested docs update, PR review reply, or merge) **only when the user explicitly requests that particular action**.

A handoff prompt should state the current repository/branch as implementation authority; objective; agreed mechanics and boundaries; relevant docs, code, scenes, and prefabs to inspect; non-goals; observable acceptance criteria; automated and manual Unity checks; and expectation of an unmerged, reviewable PR. Prefer small, independent PRs. ChatGPT may discuss and review that PR afterward without assuming responsibility for its implementation.

## Start here

1. [Uptime Current State](Uptime_Current_State.md) — project posture, implementation boundaries, validation, and source authority at the documented baseline.
2. [Uptime Design](Design/Uptime_Design.md) — living overview of the whole game and links to the full design documents.
3. [Packet Current State](Packet/Current_State.md) or [Connections Current State](Connections/Current_State.md) — what the relevant prototype currently implements and where to inspect its executable truth.
4. The relevant code and serialized Unity scene on the working branch when exact behavior matters.

## Where sources live

- [Design](Design/Uptime_Design.md) holds shared Uptime game direction, including company identity, runs, scoring, upgrades, threats, cleaning, notifications, events, and topology. Packet terminology in a source does not by itself make its larger design intent Packet-only. Documents may contain both active direction and unsettled mechanics.
- [Packet](Packet/Current_State.md) holds Packet-specific scanning, keywords, traversal, command examples, and interface ideas. Packet is parked and retained.
- [Connections](Connections/Current_State.md) holds the integrated [LOAD](Connections/connections-arcade-load-v1.md), [operation arcs](Connections/connections-arcade-operation-arcs-v1.md), [traffic](Connections/connections-arcade-traffic-v0.md), and [recorder](Connections/connections-run-recorder-v0.md) contracts, plus the [recorder example](Connections/examples/connections-run-recorder-example.json). Their status notes distinguish original phase deferrals from later merged work.
- [Research](research/README.md) preserves unresolved examples and implementation proposals that are not current authority. [Historical](historical/README.md) preserves superseded contracts, completed checkpoints, and unverified old observations. The [Connections V0 brief](historical/connections-v0.md) and [network and connection grammar](historical/network-connection-grammar-v1.md) remain complete; the grammar's older CPU/disk/memory/link allocation is not the current LOAD model.
- `Review/` retains Connections inspect UI Editor-preview folders with their READMEs and images. These are presentation evidence, not interactive Play Mode validation.

Phase 2 moved source documents without condensing their design material. Follow the links above or the prototype current-state pages; a directory, versioned title, or old "next" list alone does not establish current authority.

## Source authority

For **implemented behavior**, inspect current branch runtime code and serialized scene/prefab settings first. Current-state documents summarize that evidence; focused prototype contracts explain intended behavior but must be corrected if execution differs. For **the game we are designing**, use [Uptime Design](Design/Uptime_Design.md) and its linked full source documents, distinguishing established intentions and active working designs from unresolved details. Shared intent does not assert that either prototype implements it.

Research, old phase briefs, review captures, PR descriptions, and Git history remain evidence and provenance. They do not override current execution or silently authorize new mechanics. When a claim changes, update the appropriate individual source and its current-state pointer.

## ChatGPT Project Core

The [generated Uptime Project Core](_generated/Uptime_Project_Core.md) is a cold-start snapshot containing exactly this map, Uptime Current State, Uptime Design, Packet Current State, and Connections Current State, in that order. It excludes the full design archive, prototype contracts, research, historical sources, review images, and recorder JSON. Its SHA-256 source digest covers the ordered paths and LF-normalized source contents; freshness does not depend on the clock or Git HEAD. Links inside the aggregate target GitHub `main` and may lead to newer material than the snapshot.

From the repository root, run `python scripts/build_uptime_project_core.py` to regenerate, `python scripts/build_uptime_project_core.py --check` to verify without writing, or `python scripts/build_uptime_project_core.py --list` to see the manifest. The script also works when invoked from another directory; `--repo-root <path>` can select a repository copy explicitly.

When any of those five source documents changes, regenerate the Core and commit the source and generated file together. Changes to deeper documents require regeneration only if an included source is also updated. Edit the individual sources, never the aggregate. After merging a change to a Core source, replace or refresh the uploaded ChatGPT Project Source manually; GitHub and the generator do not synchronize that upload. For current implementation or recent PRs, inspect the working repository or GitHub rather than assuming the uploaded snapshot is live.
