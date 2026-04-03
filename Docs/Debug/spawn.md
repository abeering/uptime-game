spawn threat virus ext1 fw web cache inf:blackout infrule:nth:2

spawn threat worm a b inf:spawner infp:spawner.cadence=4 infp:spawner.burst=2
spawn threat worm a b inf:spawner infp:spawner.spawnkind=virus infp:spawner.scandifficulty=50
spawn threat virus a b inf:throttle infp:throttle.latencypenalty=3