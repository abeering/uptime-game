# Connections Hacker Duel V0 review

Scope: Connections implementation in `Connections.unity` and
`ConnectionsAdvanced.unity`. Packet is unchanged. This is the first DUEL
encounter, using Command Race. CLEAN remains the separate player initiated PID
Killer. The broader consequences and attack surface ideas in
`Docs/Connections/connections-hacker-command-races.md` and
`Docs/Connections/connections-entities-conditions-events-ideas.md` remain open
design work.

## Launch and play

1. Open either Connections scene and enter Play Mode. Use `duel auth 5` or
   `duel db 5` in the terminal. The number is simulation seconds before launch;
   omit it for the Inspector default. The target shows `INTRUSION` and a
   countdown. `duel auth 0` opens the window immediately, types an unscaled
   `INTRUSION DETECTED / AUTH` startup with `ssh operator@auth`, then reveals
   the race. The splash establishes the operator's remote session; no racer
   clock or hacker typing advances until the splash ends and held keys release.
2. Type the displayed commands in the **YOU** pane. The first command now
   checks the active SSH connection rather than reconnecting. The `>` prompts are not
   typed; Enter is not needed. A wrong character must be removed with Backspace.
   The hacker types independently, including visible mistakes and corrections.
3. For a connection-triggered demonstration, run `spawn duel-auth`. It queues a
   hostile AUTH probe whose completion schedules the same Duel with the default
   warning. This fixed profile has no ambient weight. Killing the connection
   before completion prevents scheduling.
4. For interruption, run `infect db phantom-load 2`, then `clean db` and leave
   the PID input in progress. Trigger `duel auth 0` from a scripted director
   call or let the `spawn duel-auth` carrier finish. The Duel covers CLEAN and
   takes the keyboard; the CLEAN session, draft, and process list remain below.
   On result close, focus returns to CLEAN if it is still valid, otherwise to
   the terminal. During a Duel, Escape has no dismiss action.

`ConnectionsDuelDirector.TrySchedule` is the director-facing entry point.
Only one pending or active Duel is allowed across all nodes. Rejections are
recorded with a reason. The slot releases when the race resolves; the brief
result display can remain on screen. Reset closes the Duel and clears the slot.

## Typing UX experiment

The player's authored reference commands remain visible and stationary while
typing. The active command has a restrained backing highlight and brighter
prompt. The current whitespace-delimited token is underlined and brighter;
the next expected character has a light translucent teal backing and a bright
glyph. Correct input lights the reference prefix in teal. A wrong key leaves
the expected character in place and colors it red. Further typing colors the
following reference positions red, even if those keys happen to match; no
mistyped characters replace the reference. Correct progress stays at the first
mistake. Backspace removes one red position at a time, and the normal cursor
returns when the whole error run is gone. The run stops at the authored line
end. Each red-run keystroke counts as a mistake in the recorder; each removed
red position counts as a correction. The next line becomes active immediately
when a command completes; Enter is ignored. The hacker still displays its own
typed characters, including mistakes and corrections. This remains a single text
surface with no extra input field.

For a manual typing check, trigger `duel auth 0`, wait for the startup to
finish, type `ss -tnp | grep `, then press `xut`. Confirm the expected `:22`
remains visible in red while the
progress bar stays at the clean prefix. Backspace three times, checking that
the red run shrinks one character at a time, then continue through
`ss -tnp | grep :22`. Press Enter and immediately type `t`:
the second command should accept it. Check the active token again while
typing the `/var/log/auth.log` path, and finish all commands without Enter.

## Tuning and subjective review

The scene's `ConnectionsDuelDirector` Inspector owns warning duration,
simulation continuation policy (default on), player and hacker command arrays,
hacker character time, timing variance, hesitation and typo chances, and the
loss infection type/parameter. The initial player sequence is about 86 typed
characters; the default hacker is intended to finish around 18–20 real seconds.
This needs physical keyboard playtesting, particularly novice readability and
whether the race pressure is enjoyable. `ConnectionsDuelOverlay` owns the
unscaled splash character interval (0.012 seconds), the 0.25-second handoff,
and result display duration. The startup uses the actual node ID; it does not
assert that an infection is already installed. The race starts both competitors
together after the splash.

At **1920×1080** and **1280×720**, inspect the two-pane text size and line
spacing, target-aware placement around crowded nodes, simultaneous CLEAN and
call windows, INTRUSION status among other statuses, and the terminal's
command focus after both victory and defeat. Also test held keys, Backspace,
Escape, and a CLEAN session that ends authoritatively while it is covered.

## Scripted checks and captures

`Tools > Connections > Run Duel Witness` runs deterministic race and scene
checks in Edit Mode. `Tools > Connections > Capture Duel Play Mode Review`
starts a scripted Play Mode run for a scene selected with `-duelScene` and a
size selected with `-duelWidth`/`-duelHeight`. Its seventeen frames show the
countdown, startup opening/partial reveal/full text/handoff, race start, partial input,
one error, a three-character error run, two stages of Backspace correction,
automatic line advance, a punctuation-heavy
path token, victory, Duel over CLEAN, defeat, and restored CLEAN. These
captures use scripted character entry; they are **not
physical keyboard playtest evidence**. The capture runner also checks that the
parsed reference and every glyph position, baseline, and line number stay unchanged
through the race, as well as no typing or hacker progress during startup, the
AUTH and API target text, live versus paused simulation, Escape ownership,
and focus transfer.

Unity 6000.3.11f1 validation on an isolated project copy passed the Duel
Edit Mode witness, the CLEAN window Edit Mode witness in both scenes, and the
Escape/video-call Play Mode witness in both scenes. All four scripted capture
runs passed: both scenes at 1920×1080 and 1280×720. The earlier V0 Infection
Edit Mode witness also passed before this presentation refinement.
The 68 PNGs in `Captures/` are the final authored UI captures. For batch
captures only, the isolated copy suppressed a broken Unity Search startup
callback; the workaround is not part of this PR. Physical-keyboard timing,
long holds, and subjective difficulty still need a human Play Mode pass.

The V0 loss effect goes through `SimulationHost.TryApplyDuelInfection` and the
normal infection system. A duplicate or invalid application is recorded as a
rejection; the receipt does not claim installation. Hidden infection knowledge
is unchanged. Future consequence packages, beneficial wins, Worm effects,
CLEAN LOCKED, randomized event selection, and a Level Director remain for a
separate design and implementation slice.
