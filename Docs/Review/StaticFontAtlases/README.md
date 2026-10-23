# Shared JetBrains Mono font atlases

The Regular and Bold SDF assets in `Assets/Fonts/` are shared presentation resources used by Connections and Packet. Both now contain a pre-baked, Static atlas. Existing asset GUIDs, embedded texture/material IDs, material settings, face metrics, 90-point sampling size, 9-pixel padding, SDF render mode, and single 2048×2048 atlas dimensions are preserved.

Previously both fonts used Dynamic population and enabled `m_ClearDynamicDataOnBuild`. TextMeshPro added glyphs as text appeared, modifying serialized atlas data. The installed uGUI package also uses that clearing flag when the Editor quits, so simply committing a populated Dynamic atlas would not stop regeneration. The assets now use Static population and disable that clearing flag. No package code or global TMP settings were changed.

The bake retained every existing character and added printable ASCII (U+0020–U+007E), Latin-1 (U+00A0–U+00FF), and the following punctuation/symbols supported by both source fonts: U+2013, U+2014, U+2018, U+2019, U+201C, U+201D, U+2022, U+2026, U+2190–U+2193, U+25A1, U+25B2, and U+25BC. This includes the middle dot, arrows, and triangles used by current Connections and Packet UI. U+200B zero-width space, used by input fields, is synthesized by TMP and requires no atlas bitmap.

Regular contains 207 character/glyph records, including its previously present carriage-return record; Bold contains 206. Both cover the same 206 printable characters. The saved creation settings contain the exact Unicode Range (Hex) repertoire for future explicit updates.

## Updating the fonts later

New characters outside the baked repertoire require an explicit atlas update. Select the existing SDF asset and use **Update Atlas Texture** to open TextMeshPro's Font Asset Creator. Extend the saved Unicode Range (Hex) sequence, retain the current source TTF/sampling/padding/SDF settings, and update the existing asset while keeping its GUID and material references. Keep Population Mode Static and commit both atlas changes together when expanding shared character coverage. Recheck text rendering and Play/Stop stability afterward.

Static assets intentionally do not grow in response to arbitrary user input. Supporting additional scripts or a broader Unicode input policy would require expanded atlases or a deliberate fallback-font design. See Unity's [font asset documentation](https://docs.unity3d.com/Packages/com.unity.textmeshpro@4.0/manual/FontAssets.html).

## Validation actually run

Unity 6000.3.11f1 generated and tested the assets in an isolated copy of the project, leaving the user's open Editor untouched. The one-time bake/review script existed only in that copy.

- All source-supported requested characters fit in each existing atlas; no new atlas textures were needed.
- Every previously baked character was retained. Material blocks, face metrics, and embedded object IDs matched the original assets exactly.
- After an Editor shutdown/relaunch, TMP generated text geometry for all 206 printable characters in each font with no missing-font-character warnings.
- Two automated Play/Stop cycles each in `Connections.unity`, `GameScene.unity`, and `ABScene.unity` left both serialized font assets byte-for-byte unchanged. Hashes also matched after the verification Editor quit.
- `git diff --check` passed after copying back the verified assets.

These are font stability and text-generation checks, not comprehensive gameplay or visual acceptance tests. The copied Editor emitted a Unity Search database indexing exception during domain reloads. `ABScene` also reported an existing `OperationsPanelController.Update` null reference at line 27: its serialized component has no `scoreDirector` reference. Neither issue originated in the font assets; neither was changed here.

In the normal Editor, reload the changed fonts, play Connections, inspect several connections, check the middle-dot headings/arrows/ellipsis, and type ordinary command punctuation. Stop Play Mode and confirm Git still shows no font modifications. Packet's console triangles and cleaning text should retain their previous appearance. Review at 16:9 for normal text/layout readability; interactive review was not performed by the automated checks.
