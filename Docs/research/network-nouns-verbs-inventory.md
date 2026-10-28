# Uptime — Network nouns and verbs

**Status:** Small, exploratory vocabulary bank. Not a selected feature set, implementation backlog, or declaration that any listed concept exists in Connections.

**Purpose:** Keep plausible infrastructure nouns and operator verbs in one place. Discuss individual terms, starting with those easiest to define, before designing dependencies, upgrades, node-to-tool associations, or larger network-building rules.

## Node discussion order

Discuss these **in this order**, beginning with nodes:

1. **Load Balancer**
2. **Firewall**
3. **Replica** — Open question: is a replica meaningfully different from ordinary target growth as the game gets harder? Would target workloads naturally spread across more nodes anyway? Is a replica even a separate player choice?
4. **Cache**

The order is a discussion queue, not an implementation order. No mechanics for these four are decided here.

## Nouns — infrastructure / nodes

- External / Ingress
- Gateway
- Router
- Firewall
- Proxy
- API Server
- Authentication Server
- Database
- Cache
- Load Balancer
- Replica
- Queue / Buffer
- Quarantine Server
- Monitoring Server
- Honeypot
- Backup Server

These are names to consider, not a formal node taxonomy. Some may prove better as node capabilities, temporary deployables, or ordinary growth rather than separate node types.

## Verbs — tools / actions

- `inspect`
- `scan`
- `clean`
- `kill`
- `trace`
- `block`
- `probe`
- `throttle`
- `priority`
- `route`
- `boost`
- `quarantine`
- `failover`
- `repair`
- `policy`

Some verbs already exist in Connections, some were implemented differently in Packet, and others are just names for possible future actions. They are not all approved command names.

## Existing design references

- [Topology node roles](../Design/topology-nodes.md) and [Topology system](../Design/toplogy-system-v1.md)
- [Shared game design](../Design/game.md) and [Upgrade strategies](../Design/upgrade-strategies.md)
- [Packet commands](../Packet/gpt-prompts.md) and [Packet keywords](../Packet/keywords.md)
- [Connections threat-investigation ideas](../Connections/connections-threat-investigation-ideas.md)

Grow or prune this bank as concepts become clearer. Do not turn it into a mechanics spec until the discussion warrants one.
