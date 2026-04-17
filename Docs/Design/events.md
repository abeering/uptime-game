# events 

events are definitions of scripted actions that take place during a level execution, they have several different components
- attackplan - unbuilt (an actual set of packet spawn plans and instructions for timing )
-- can represent coordinated attacks (virus burst, threats mingled with benign traffic, ddos swarm)
- "slack messages" - unbuilt (a notification that ties packet/attackplan behavior to story/flavor)
-- "sale goes live now" -- big burst of benign packets 
-- "i will pwn u" -- hacker villain is making a big attack 
-- "someone changed my password???" -- threats are incoming 

timing:
- we do not use real world time in these definitions
- in a given level we say 
-- bewteen ticks (.5s) 20-50, this event occurs 
-- between ticks 90-120, this event occurs 
-- events have their own concepts 
-- tick 1-5 spawn 2 packets 
-- tick 5-15 slack message 
-- tick 20-30 spawn 10 packets 
-- tick 25 spawn 2 viruses with these infections + keywords 
-- etc 