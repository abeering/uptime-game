# Uptime documentation

Uptime is one game design explored through two independent gameplay prototypes: Packet and Connections. Both remain playable and neither is declared the final implementation.

## ChatGPT and Codex workflow

**ChatGPT is the design, documentation, exploration, and review workspace; Codex/local Unity is the production implementation workspace.** This is a division of *work*, not a blanket prohibition on writing code or editing documents in ChatGPT.

- **Design and review in ChatGPT:** Research the current repository read-only as needed, brainstorm and settle small gameplay contracts, write Codex handoff prompts, and review Codex PRs, run evidence, and playtests. Agreeing on a design or saying “let's continue” is **not** authorization for ChatGPT to implement it.
- **Small documentation-only PRs in ChatGPT:** When the user explicitly requests it, ChatGPT can create and update focused docs-only branches and PRs to capture ideas, design decisions, research, planning notes, or documentation maintenance. This is an ordinary, supported part of the workflow, not a rare exception. Keep changes to documentation and its required generated artifacts; do not attach gameplay code, Unity scenes/prefabs, or unrelated implementation changes.
- **Independent exploration and simulation in ChatGPT:** Use reasoning, temporary scripts, Python, numerical or Monte Carlo simulations, and other standalone experiments to test design hypotheses and share results. Such exploratory code and artifacts are **not production Uptime implementation**; do not silently commit them to the game repository. If an experiment needs integration into Unity, hand that work to Codex. Do not imply a simulation or other task runs in the background unless an actual scheduled/async tool was configured.
- **Handoff directly to Codex:** When an implementation is agreed, provide a **complete, copy-ready Codex prompt in the conversation itself**. Do not create GitHub issues/tickets as handoffs, ask Codex to work from an issue instead of a prompt, or replace the prompt with a vague summary or external link. The user takes the prompt to Codex.
- **Production implementation in Codex/local Unity:** Codex owns gameplay/runtime code, scene/prefab authoring, implementation-specific docs, integration tests, Unity validation, implementation branches, commits, and reviewable unmerged PRs. Return branch/commit, exact changes, checks actually run, caveats, and manual Play Mode review instructions to this discussion.
- **Explicit GitHub actions:** Read-only repository/PR inspection is fine here. ChatGPT may perform a particular requested write action, including a docs-only PR, replying to inline PR questions, or merging a specified PR. Such authorization is limited to the requested action; it does not authorize implementation or issue creation by implication.

A Codex handoff prompt should identify current repository/branch implementation authority; objective; agreed mechanics and boundaries; relevant docs, code, scenes, and prefabs to inspect; non-goals; observable acceptance criteria; automated and manual Unity checks; and expectation of an unmerged, reviewable PR. Prefer small, independent PRs. ChatGPT can review and iterate on Codex's work without taking over implementation.

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
