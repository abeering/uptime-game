# Inspect header identity pill

The inspect header now uses nested instances of the same `IdentityPill.prefab` used for connection IDs on network lines. The header keeps `INSPECT /` and the claimed purpose as separate text labels. The identity pill keeps the blue-green header accent while the operation pill below continues to show green, orange, or red performance.

- [Full 1920×1080 Connections scene](nominal-1920x1080.png)
- [Active connection close-up](nominal-operation.png)
- [Slowed connection close-up](slowed-operation.png)

These are Unity Editor renders from a deterministic simulation preview, not Play Mode screenshots. In Play Mode at 16:9, check `inspect c1`, `inspect c2`, and `inspect c3` in terminal history, including a completed entry. Confirm that long purposes fit and the input prompt retains focus.
