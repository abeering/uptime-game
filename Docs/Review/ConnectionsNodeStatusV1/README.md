# Connections Node Status Presentation V1 review

This branch tests the imported V2 Node Stamps in `ConnectionsStatusLab.unity`.
It changes presentation only. The eight lab buttons still drive the same detached
status state; stamp playback does not own or delay that state. Ordinary Connections
scenes share the compact status panel prefab and remaining-seconds text, while
stamp triggering and exterior-band suppression stay in the lab.

The supplied 1920×1536 background is imported as 24 centered 320×384 frames,
ordered left to right and top to bottom. Each 320×384 symbol is a centered single
Sprite. All imports retain their original PNGs and GUIDs, alpha, no mipmaps, and
uncompressed texture data. The authored `NodeEventStamp.prefab` exposes its
overall size, 24 FPS playback, one-second lifetime, fade onset, tint, opacity,
icon delay and icon scale in the Inspector. It is centered on AUTH inside the
linked status view, above the card and the panel stack, and has no raycast
targets. A new event replaces the current one; CLEAR ALL cancels it.

The shared `NodeStatus.prefab` now has three separate label rows and three
right-aligned TMP timer fields per family. Timers use the simulation or lab's
authoritative expiry time, rounded up to whole seconds. Untimed statuses have
no timer. Family labels and ordering are unchanged. This panel change affects
both ordinary Connections scenes. Their exterior bands remain enabled; the lab
turns them off when its driver starts. No stamp is connected to simulation
events in gameplay scenes.

## Play Mode review

1. Open `Assets/Scenes/ConnectionsStatusLab.unity`, set Game view to 1920×1080,
   and enter Play Mode. Repeat at 1280×720.
2. Click each of the eight status buttons. Check the matching symbol, family
   tint, centered placement over AUTH, translucent node underneath, and full
   one-second playback. OFFLINE and INFECTED only announce when toggled on.
3. Click CLEAN, then SCAN JAMMED, then several different buttons rapidly.
   Check that each click immediately replaces the previous stamp and leaves no
   stray symbol. Reapply SCAN JAMMED to restart both its status timer and stamp.
4. Combine OFFLINE, INFECTED, SCAN JAMMED, PORT SCANNED, CLEAN, ISOLATED and
   INTERCEPT. Check three-family coexistence, three aligned Pain rows, compact
   timer column, and panel visibility during stamps. Let timers expire, then
   press CLEAR ALL during another stamp.
5. Open `Connections.unity` and `ConnectionsAdvanced.unity` to review the shared
   panel width/placement with live nodes, `inspect` and command focus. Stamps
   should appear only in the lab. Packet scenes are outside this change.

The scripted lab witness checks authored controls, mappings, cancellation,
timer refresh and state independence. The production status witness checks the
shared prefab in both gameplay scenes. Scripted captures support layout review;
physical mouse feel and normal Game view focus need a human Play Mode pass.

## Checks and captures on this branch

- Unity 6000.3.11f1 compiled and imported the isolated project copy.
- `NodeStatusLabWitness.Run` and `NodeStatusWitness.Run` passed. The lab witness
  was rerun after the final band-suppression change.
- The stamp now draws over panels. The lab witness checks sibling order, and
  the eight captures below were refreshed for that placement.
- Scripted Editor captures were rendered and inspected at both resolutions.
  These are single-frame scene renders, not Play Mode screenshots or an animation.
- Two batch Play Mode capture attempts stalled after Unity's startup Search
  indexing exception, before the first lab check. Interactive Play Mode and
  physical input were therefore not validated in this pass.

[CLEAN at 1920×1080](clean-1920x1080.png) ·
[SCAN JAMMED at 1920×1080](scan-jammed-1920x1080.png) ·
[All families at 1920×1080](all-families-1920x1080.png) ·
[Rapid CAUGHT replacement at 1920×1080](rapid-caught-1920x1080.png)

The same four captures are also available at 1280×720:
[CLEAN](clean-1280x720.png),
[SCAN JAMMED](scan-jammed-1280x720.png),
[all families](all-families-1280x720.png), and
[rapid CAUGHT](rapid-caught-1280x720.png).
