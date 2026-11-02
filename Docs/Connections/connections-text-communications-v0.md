# Connections — Text Communications V0

**Scope:** Connections presentation, from `main` at `c7a857330a49386207c73a4d9e52a5934ad07b51` after PR #73. Both `Connections.unity` and `ConnectionsAdvanced.unity` author the same passive communication surface. Packet remains independent. [Notifications design](../Design/notifications.md) supplies longer-term narrative direction; its cue resolution, speakers, escalation, priority and scheduling proposals are not implemented here.

## Content and delivery

Instant messaging is the primary fiction. One Connections-owned `TextCommunication` data type, `TextCommunicationDirector` and `TextCommunicationView` support `InstantMessage` and `SystemNotification`. Future callers supply identity and meaning through a serialized reference to the scene director:

```csharp
bool delivered = textCommunications.PushMessage(new TextCommunication {
    Type = TextCommunicationType.InstantMessage,
    Sender = "Sarah Mitchell",
    Body = "We're rehearsing the sale in five minutes. Ready?",
    Avatar = optionalPortraitSprite,
    Timestamp = optionalDisplayTimestamp,
    LifetimeSeconds = optionalLifetimeSeconds
});
```

`Avatar` and `Timestamp` default to null; `LifetimeSeconds` is nullable. An omitted timestamp snapshots the existing workstation clock label. It stays fixed for that message. An explicit timestamp is supplied display text, not a new clock source. Missing or whitespace sender identity displays `Unknown`; null body displays empty. TMP rich text and control-character parsing are disabled on all supplied text. Explicit newlines wrap normally. No identity registry, portrait generation or permanent character roster is introduced.

Null data, unknown enum values and disabled/unavailable presentation return false. Valid delivery has no dependency on narrative, traffic generation or Escape coordination. The optional Sprite preserves aspect ratio in a prominent 52-unit avatar slot. Without it, an instant message shows an authored neutral person silhouette; a system notification shows an authored monitor icon in the same slot. These fallback elements need no new character artwork.

The director adapts Packet's newest-first stack, automatic expiry, oldest eviction and smooth repositioning. The view adapts Packet's short unscaled fade/slide. Connections owns the code and prefab; it does not invoke or change Packet's director, view, notification model, prefabs or audio.

## Authored presentation and lifecycle

Each scene authors a clipped overlay container within NetworkPanel, 16 reference units from its left/right edges, 64 below its top and 12 above its bottom. The stack therefore begins below the permanent 40-unit titlebar. The default preferred entry width is 440 units, limited by the container's available width. Ordinary entries have text-driven heights with an 88-unit minimum and an eight-unit gap. The stack never controls pane layout or topology.

`Assets/Prefabs/Connections/TextCommunicationEntry.prefab` authors the dark surface, crisp one-unit border, narrow left accent rail, avatar, sender/timestamp row and body. Instant messages use serialized teal/cyan; system notifications use serialized amber/gold. The colors are configurable on the scene director. There are no repeated type labels, titlebars, pins, window frames, close/minimize controls, buttons or raycast targets.

All body characters are assigned immediately. The container fades/slides in over approximately 0.16/0.18 seconds, stays for its reading duration, then fades/slides out over 0.22 seconds. Reflow uses a 0.18-second cubic ease. Arrival opacity and stack movement are independent, so moving a still-arriving entry cannot cancel its fade. All lifecycle and animation timing uses unscaled real time; no coroutine is created per entry.

Default reading time is `clamp(6 + bodyLength / 24, 6, 16)` seconds after the arrival fade. A finite positive explicit lifetime overrides that range, which is useful for authored delivery and short lifecycle tests. Invalid overrides use the default. This is presentation timing, independent of the workstation's accelerated simulation clock.

Four live entries are allowed by default. A new message retires the oldest when the count or available stack height would be exceeded. Departures remain briefly visible while fading; under a rapid burst, at most four departing objects are retained alongside the live entries. New arrivals and reflow continue during departure. Departing entries do not consume permanent stack slots. A paragraph taller than the entire available viewport is height-capped with TMP ellipsis; long ordinary paragraphs are fully wrapped. This overflow safeguard does not create a history/replay surface. Authors should keep ephemeral messages readable within the viewport.

Resizing recalculates active entry widths/heights and fits the stack again. Reset, component/container disable and destruction immediately hide and destroy owned entries, clear both lists. Disable/destruction also remove the simulation-reset subscription. No message or animation resumes on re-enable.

## Input and other windows

Messages neither select UI objects nor touch input values/selections, simulation time or Escape registration. They do not pause, require acknowledgment, accept replies, or add an inbox/history. They expire and can be missed. Terminal and CLEAN retain their existing typing and Escape behavior.

`ConnectionsWindowPlacement` adds the current entries' authored avatar, identity and body rectangles to its existing useful-content overlap score, including briefly departing content. Empty stack space contributes nothing. Opening a call or CLEAN still uses the existing deterministic candidate list and weights, once on opening. No corner is permanently reserved. New notifications do not reposition or reorder open call/CLEAN windows. Later-opened gameplay windows retain their normal sibling ordering. Constrained space can still produce overlap.

## Developer delivery

Editor/development builds expose:

```text
notifytest im
notifytest system
notifytest burst
notifytest im delay=5
```

All examples identify themselves as demonstration content and make no assertion about live conditions. Delays accept invariant decimal seconds from 1 through 30. Each terminal permits one pending notification sample; further immediate/delayed tests are rejected until it delivers or is canceled. The delay uses real time, then invokes the same delivery API without writing terminal history or touching focus. Terminal disable/destruction or simulation reset cancels it. Call-test scheduling remains independent.

The director's context menus **Push Developer Instant Message** and **Push Developer System Notification** deliver directly while CLEAN owns typing. These tools are demonstration entry points, not a Level Director or story scheduler. No Health, infection, kill, hostile intent, traffic or other gameplay event automatically generates messages.

## Validation and review

`Tools/Connections/Check Text Communication Bindings` checks both scenes and prefab passivity. `Tools/Connections/Run Text Communication Witness (Play Mode)` exercises delivery, styling, Sprite/fallback handling, timestamp snapshots, full text, dynamic height, animation/expiry/reflow, burst eviction, input preservation, Escape order, placement, reset/disable cleanup and actual generated traffic. Batch arguments match the existing call witness: `-witnessScene`, `-witnessWidth`, `-witnessHeight` and `-capturePath`.

See [review evidence and manual checklist](../Review/ConnectionsTextCommunicationsV0/README.md) for actual results, captures, reproduction commands and remaining human review. These are scripted Play Mode Canvas renders, with the render size fixed before placement. They do not establish physical keyboard/IME behavior or human reading comfort in the native Game view.
