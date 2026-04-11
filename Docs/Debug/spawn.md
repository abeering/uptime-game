spawn threat virus ext1 fw web cache inf:blackout infrule:nth:2
spawn threat worm a b inf:spawner infp:spawner.cadence=4 infp:spawner.burst=2
spawn threat worm a b inf:spawner infp:spawner.spawnkind=virus infp:spawner.scandifficulty=50
spawn threat virus a b inf:throttle infp:throttle.latencypenalty=3

spawn threat virus ext1 fw kw:mutating
spawn threat virus ext1 fw kw:mutating:4

spawn threat worm ext1 db kw:jittery
spawn threat worm ext1 db kw:jittery:2

spawn threat spyware ext1 api kw:surging
spawn threat spyware ext1 api kw:surging:2:3:1

spawn threat virus ext1 fw kw:desynced
spawn threat virus ext1 fw kw:desynced:2:4

spawn threat virus ext1 fw kw:accelerating
spawn threat virus ext1 fw kw:accelerating:2:-1
spawn threat virus ext1 fw kw:accelerating:3:-2

spawn threat ddos ext1 fw kw:dragging
spawn threat worm ext1 db kw:dragging:2:1
spawn threat ddos ext1 fw kw:dragging:8:2


kw:mutating
kw:mutating:<ticksPerMutation>

kw:jittery
kw:jittery:<jitterAmount>

kw:surging
kw:surging:<stallTicks>:<burstTicks>:<burstMoveInterval>

kw:desynced
kw:desynced:<stallTicks>:<teleportSteps>

kw:accelerating:<radius>:<delta>:<ignoreSameClassAndKind>
kw:dragging:<radius>:<slowAmount>:<ignoreSameClassAndKind>