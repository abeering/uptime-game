# Connection inspect readability review

These are Unity Editor renders of the inspect output prefab, captured from the authored Connections scenario at the same simulation points before and after the UI change. They are not Play Mode screenshots.

| State | Before | After |
| --- | --- | --- |
| Active c1 and c3 | [before-active.png](before-active.png) | [after-active.png](after-active.png) |
| Active c1, completed c3 | [before-completed.png](before-completed.png) | [after-completed.png](after-completed.png) |

In Unity Play Mode, review the whole Connections scene at 16:9. Run `inspect c1`, `inspect c1 c3`, and later `inspect c3` after it completes. Check title and pill legibility in the terminal column, that mixed active/completed entries do not overlap, that the active progress bar and ETA update, and that the command prompt keeps focus. Also check the network labels when multiple connections share a path.
