# TOPOLOGY RUNTIME SPEC – V1

---

# PURPOSE

This document defines the **implementation-facing contract** for topology generation.

It bridges design → runtime by specifying:

- Data model
- System ownership
- Generation pipeline
- Layout contract
- First implementation milestone

---

# 1. DATA MODEL

## TopologyNode

Fields:
- id (string)
- role (Entry, Transit, Service, Core, Buffer, Control)
- tags (string[])
- state (active, disabled, damaged)
- tier (int)
- region (placement region hint)
- parentId (optional, for branch tracking)

---

## TopologyEdge

Fields:
- id (string)
- fromNodeId (string)
- toNodeId (string)
- lengthClass (short, medium, long)
- latency (float or int)
- kind (spine, branch, control, ingress)
- enabled (bool)

---

## TopologyGraph

Fields:
- nodes (list of TopologyNode)
- edges (list of TopologyEdge)
- seed (int)
- templateId (string)
- appliedMutationIds (list)
- dayIndex (int)

---

# 2. SYSTEM OWNERSHIP

## TopologyDirector (NEW)

Responsible for:
- Creating logical graph
- Applying mutations
- Running validation
- Assigning regions
- Triggering layout + build

---

## LevelDirector

- Requests topology for current day
- Provides context (company, dayIndex)

---

## NetworkRuntime

- Registers nodes/edges after build
- Provides lookup for other systems

---

## TrafficDirector

- Consumes topology for routing/spawning

---

# 3. GENERATION PIPELINE

1. Select template (based on company)
2. Build seed spine
3. Apply day-based rules (optional early features)
4. Apply persistent mutations
5. Validate graph
6. Assign placement regions
7. Solve layout positions
8. Assign edge lengths / latency
9. Build runtime objects (NodeView, ConnectionView)

---

# 4. LAYOUT CONTRACT

TopologyDirector outputs:
- node positions (world space)
- edge connections

Layout solver uses:
- predefined placement regions (scene rects)
- role-based placement preferences

Goals:
- clear mainline
- readable branches
- minimal crossing
- full space utilization (including L-shape)

---

# 5. PREFAB / VIEW MAPPING

Role → Prefab mapping:

- Entry → EntryNodeView
- Transit → TransitNodeView
- Service → ServiceNodeView
- Core → CoreNodeView
- Buffer → BufferNodeView
- Control → ControlNodeView

State/tag modifies:
- color
- effects
- labels

---

# 6. FIRST PATCH SCOPE (CRITICAL)

## Goal

Generate and render a **single valid topology** in-scene.

---

## Included

- Data model (Node, Edge, Graph)
- TopologyDirector (basic)
- One template (single spine)
- Optional buffer spur (hardcoded chance)
- Simple layout (region-based, not optimized)
- Instantiate NodeView + ConnectionView

---

## Excluded (for now)

- Player mutations
- Persistent cross-level graph
- Quarantine/control behavior
- Advanced layout solver
- Multiple templates

---

# 7. IMPLEMENTATION SEQUENCE

## Patch A
- Define data model
- Enums for role, edge kind, length class

## Patch B
- Implement TopologyDirector
- Build seed spine + optional buffer

## Patch C
- Basic layout pass (assign positions)
- Instantiate scene objects

## Patch D
- Route TrafficDirector through generated graph

---

# 8. DESIGN CONSTRAINT

> Build ONE vertical slice end-to-end before expanding.

---

# FINAL PRINCIPLE

> The goal is not full procgen —  
> the goal is a **reliable, testable topology pipeline** that future systems can build on.
