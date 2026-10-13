# Shadow Line Protocol — Game Design Doc

Oct 12, 2026 · @Cameron McRoberts

## Vision & pillars

*Shadow Line Protocol* is a turn-based colony, research and squad game set at the lunar south pole, where every research decision moves the story and your people are the scarcest resource you have.

**Pitch:** You lead a first-generation lunar colony. Earth funds you until it doesn't. Research buys independence, rival and partner colonies decide whether you survive, and someone among your allies is working against everyone.

**Inspirations:** Civilization (research tree, eras, diplomacy, evolving rivals) · XCOM 2 (squad attachment, base building, robot teammates, turn-based tactics) · Darkest Dungeon (stress and mental state) · Astroneer (exploration and crafting) · Ad Astra-style colony sims (real science and logistics).

**Design pillars**

1. **People are everything.** Breakthroughs come from your crew, not from stacking resources. Early losses are near-fatal.
2. **Research writes the story.** The tech tree is also the narrative tree.
3. **Combat is one option among many.** Every mission can be solved by talking, engineering, sneaking, trading or fighting. Nobody is a pure soldier.
4. **Real science is the antagonist you can't negotiate with.** Radiation, the 14-day lunar night, dust, comms shadows and launch economics shape every decision.
5. **Nobody survives alone.** Alliances with other colonies are necessary, and trust is earned through actions.
6. **Robots multiply people.** Late-game teams pair each human with several robots, but humans stay the command nodes and the only ones who adapt.

## Setting & lunar science

The game is set at the lunar south pole because that is where real colonization will happen: water ice, near-constant sunlight on some peaks, and permanent shadow in nearby craters. Each real constraint below becomes a mechanic. Figures are approximate and from general knowledge, so they should be checked before they're locked in.

| Real constraint | Approx. figure | Game mechanic |
| --- | --- | --- |
| Water ice in permanently shadowed craters (Shackleton, Shoemaker, etc.) | Crater floors below −170°C | Ice is the strategic resource for water, oxygen and rocket fuel. Mining it means operating in extreme cold and darkness. |
| Peaks of near-eternal light on crater rims | Sunlit most of the year | Prime solar real estate that colonies compete for |
| Lunar night (away from the peaks) | \~14 Earth days | A recurring survival phase: power rationing, cold-soak, robots on reserves |
| Regolith oxygen content | \~40–45% by mass | ISRU (making what you need from local material): oxygen extraction, a core research line |
| Regolith as building material | Sinter, melt or 3D-print | Shielded habitats, landing pads, berms. Large-format printing is a construction tech line. |
| No magnetosphere or atmosphere | Surface dose many times Earth's | Crew dose budget in the early game, overcome through research (see Crew). Solar particle events force everyone into shelter. |
| Abrasive, electrostatic dust | Clings to everything | Wear on suits, seals and robot joints. A maintenance economy. |
| Short horizon, no ionosphere | \~2.4 km for a standing person | Line-of-sight comms. Relays and handlers matter (see Robots). |
| Lava tubes and pits | Natural radiation shelter | Late-game expansion sites, and mystery locations |
| Earth–Moon distance | \~3-day transit, \~1.3 s signal delay | Rescues from Earth are possible but slow. Live calls home are possible. |
| One-sixth gravity | 1.62 m/s² | Long jumps, slow falls, hopper robots. Bone loss over long stays. |

## Campaign setup

Difficulty is set by how many other colonies start on the map. More colonies mean more opportunity and more competition.

| Other colonies at start | Opportunity | Competition |
| --- | --- | --- |
| 4 | Sufficient | Low |
| 10 | Sufficient | High |

**Alliance thresholds.** The player needs 2 allies to survive, at least early on. The number needed to thrive scales with colony count: 5 in a 10-colony game, fewer in smaller games (half the other colonies, rounded up, minimum 3).

**Starting partner.** One colony starts at partner level with the player. The player picks it from 3 candidates, each offering different strengths, needs and baggage.

**The first launch.** Before landing, the player packs the payload of their first launch to the Moon within a mass budget, choosing between equipment, robots, supplies and habitat modules. They also decide what goes back to Earth on the return flight, such as samples, data or people. Later launches are earned and unlocked, and other colonies compete for the supplies each launch can carry.

## Campaign pacing

A campaign targets about 25 hours, roughly 60 turns and 5 in-game years. Each turn is about one lunar day cycle (about 29.5 Earth days), with a day phase and a night phase.

| Era | Turns | Approx. play time | Focus |
| --- | --- | --- | --- |
| 1 | 1–20 | \~8 h | Landing, survival, beating radiation, first alliances |
| 2 | 21–40 | \~8 h | Expansion, colony politics, robots networked, antagonists surface |
| 3 | 41–60 | \~9 h | Crises, unmasking, racing toward a victory |

Each era ends with a crisis or turning point, so a campaign never drifts into a late-game slog.

## Story, research & the mystery

The research tree is the narrative tree: each major tech choice unlocks capabilities and also opens, closes or reshapes story threads.

**Research that branches the story**

- The first story arc is beating radiation: early robots let you ration exposure, and shielding research frees your crew to explore. Prioritizing ice mining over helium-3 pulls you into a water-rights conflict with a rival colony.
- Expanding AI autonomy starts a storyline about trust, and about who else might be using your network.
- Deep-drilling the lava tubes uncovers something no colony planned for.
- Research choices also change which colonists want to join you, and which colonies see you as a threat.

**Eras.** As in Civilization 7, every few major techs move the colony into a new era. The world reacts: rivals adapt their strategies, Earth's leverage over you shifts, and new threats appear.

The full stage-by-stage story and mission structure is in Story arc.

**Threats escalate in layers**

1. **The environment:** solar particle events, micrometeoroids, moonquakes, dust and the lunar night.
2. **Rival colonies and corporations** that evolve with your strategy.
3. **Rescue scenarios** that drive the protection theme: stranded crews, crashed landers, failing habitats.
4. **The hidden antagonist** (below).

**The hidden antagonist**

- Never one of your own team members. Each antagonist is a team member of a rival colony: an NPC you deal with directly or indirectly, who works colony politics against you, possibly from inside a colony you depend on. Like Clue, each playthrough randomly picks 1–3 antagonists, scaled by colony count and difficulty, from the other colonies' members, so the mystery is different every game. Every character's hidden pressure becomes their motive if they're picked.
- Their sabotage is physical or political. Their motive varies by antagonist: profit, ideology, a grudge, jealousy, fear or greed. Evidence eventually surfaces through communications with their own team members or yours, physical evidence, tracking or camera footage from a habitat or communications tower, intel from captured robots, and anomalies the central AI spots.
- Unmasking takes evidence of wrongdoing, such as stealing, lying or violence. Clues can be found by your team members, by you, or by the central AI. A successful unmasking brings the antagonist to justice, where they are reformed, and boosts your relations with their colony. A wrong accusation costs you relations instead.
- Their methods escalate with your tech. As your fleet becomes networked, they target the network.

## Win conditions & achievements

Like Civilization, there are several ways to win, and each one is its own ending.

| Victory | Condition |
| --- | --- |
| Diplomatic | Reach Ally status with 80% of all other colonies (e.g., 4 of 5) |
| Expansion | Develop 60% of the playable south polar region and defend it for 5 turns |
| Liberation | Defeat or liberate all outlaw and enemy colonies |
| Shared survival | Share 10 high-survival techs with 5 or more partner colonies |

**High-ranking achievements**

| Achievement | Tiers |
| --- | --- |
| Marry and raise a family on the Moon | — |
| Unmask an antagonist (there may be several, in different colonies) | Per antagonist |
| Build a colony that supports 100+ people | — |
| Train your team to 10 members with elite status | — |
| Shelter colonists from neighboring colonies | 10 · 20 · 30 |
| Rescue your own people or others | 5 · 10 · 15 |
| Capture robots | 5 · 10 · 15 |

## Crew

Every team member grows, learns, builds and relates. The crew is where breakthroughs come from, and overlapping skills and training are what make the team resilient.

**Disciplines.** Geologist, engineer, medic, pilot and roboticist, each with a branching class tree. Every character can fight competently, but no class is combat-focused.

**Characters.** Every colonist, in your colony and the others, has starting attributes and can then be developed in any direction, though they stay strongest in their main discipline. The full game will have hundreds of profiles with backstories, Clue-style. The starting palette has 8 per discipline (40 total), enough to fill the starting teams of 8 colonies: Character palette

**Radiation, an early-game challenge.** Radiation is an early-game pressure that research overcomes, not a permanent career clock. At first, every EVA spends part of a person's dose budget, so you ration exposure carefully. Robots taking the high-dose jobs, then better shelters and suits, shrink the problem until only solar storms really matter.

**Radiation research line**

| Stage | Unlock | Effect |
| --- | --- | --- |
| 1 | Dose rationing and robot deployment | Robots take high-dose tasks; crew dose is tracked per EVA |
| 2 | Regolith-shielded habitats and storm shelters | Near-zero dose indoors; safe havens during solar particle events |
| 3 | Solar storm forecasting | Early warning gives crews time to get to shelter |
| 4 | Shielded suit (hydrogen-rich polymer or water-jacket layers) | Greatly reduced EVA dose |
| 5 | Biological countermeasures (speculative) | Dose becomes a minor factor; only major solar storms are dangerous |

**Collaboration with diminishing returns.** Output from n people working together is n^0.68:

| Team size | Multiplier | Per person |
| --- | --- | --- |
| 1 | 1.00x | 100% |
| 2 | 1.60x | 80% |
| 3 | 2.11x | 70% |
| 4 | 2.56x | 64% |

Modifiers on top of the curve:

- Complementary skills soften the penalty.
- Strong relationships add a bonus.
- Friction between teammates makes it worse.

**Roadblocks.** Research and building projects sometimes stall until someone with a different skill or trait looks at the problem. A geologist stuck on ice extraction might get unstuck in a day by a roboticist. Each breakthrough creates a bond and a small story moment, so collaboration grows naturally through play.

**Stress and mental state (kept light).** Managing your own people should not be a chore, but their mental state matters.

- Stress rises with EVAs, close calls, isolation, conflict and the lunar night.
- At its peak, a character either breaks (a temporary negative quirk) or rises to it (a lasting positive trait).
- Recovery comes from habitat amenities like the greenhouse, real downtime, and live calls home.

**Permadeath that scales with colony size**

- **3–5 crew:** each person covers a role nobody else can. A loss isn't an instant game over, but the colony can't sustain itself and you watch the cascade.
- **6+ crew:** cross-training creates overlap, so the colony can recover from a loss, though it leaves a lasting mark on morale and story.
- **Bleed-out state:** a downed crew member has a few turns before their suit fails. Getting them back to the airlock is a core rescue loop.

## Robots & the AI

Robots multiply people, and humans are the ones who adapt. The strongest late-game teams pair each human with several robots for redundancy, survivability, production and growth, and to absorb risk in combat.

**Robot types**

| Chassis | Strength | Weakness |
| --- | --- | --- |
| Rover | Hauling, mobile workshop, fast on flats | Useless in rough terrain |
| Quadruped | Crater rims and rubble, all-rounder | Middling at everything |
| Hopper | Thruster hops (no air for rotors), reaches anywhere | Limited propellant |
| Crawler / snake | Lava tubes, wreckage, pipes | Slow, low capacity |
| Heavy manipulator | Construction, regolith printing | Slow, a big target |
| Micro-swarm | Surveys, repairs, distraction | Fragile individually |

Robots don't gain experience the way humans do. They improve through research, parts and upgrades, which ties them to the launch economy.

**Independent or networked.** Every robot starts as its own independent unit. A late-game upgrade lets each robot either join the central AI or stay specialized.

|  | Networked (central AI) | Specialized |
| --- | --- | --- |
| Intel on destruction or capture | Only what's gathered since the last sync is lost | All of it is lost |
| Learning | Lessons spread across the fleet | Higher individual skill ceilings |
| Coordination | Synchronized tactics, shared sensors | Each acts on its own orders |
| Weakness | One compromised unit can spread it; jamming hurts more | Knowledge stays siloed |

**Capture and hacking.** Robot hacking is a research unlock.

1. **Disrupt power:** EMP charges, cut cables, targeted batteries or dust contamination force a reboot lasting a few turns.
2. **Hack during the reboot:** that's the capture window. Hacking skill, chassis and enemy defenses set how long it takes.
3. **Extract and reprogram at base.** A captured robot is useless until reprogrammed. It carries enemy intel that can reveal clues about the antagonist, and it might carry a hidden backdoor unless you take the time to scan it.

The enemy can capture your robots too, so downed robots become recovery objectives. A researchable self-wipe destroys a robot's intel to deny it.

**Comms disruption.** A jammed robot keeps executing its current order. It can't receive new orders or be told to stop.

- Research answer: conditional standing orders, such as "retreat if hull below 40%" or "hold if a human is in the line of fire."
- Countermeasures: laser comms (line-of-sight, hard to jam), mesh relays, and a nearby human handler.
- You can jam enemy robots and bait them into following their last orders.

**Why humans stay essential**

- **The Moon blocks signals.** Robots out of relay or handler range fall back to basic autonomy, so humans are the command nodes.
- **Command limits.** Each human directs a number of robots set by their skills.
- **Only humans improvise.** Roadblocks and novel situations need a human, and humans get improvisation actions in combat.
- **Only humans talk.** Negotiation and de-escalation are human-only, and enemies surrender to people, not machines.
- **Humans can't be hacked.**
- **Robot upkeep.** Dust, temperature swings, the lunar night and radiation glitches all mean repairs and spare parts that compete for launch mass.
- **Heroic moments are human.** Stress can make a person stronger. Robots never rise to the occasion.

**The central AI**

The central AI is a character with a personality that develops slowly along five axes, which the player shapes and adjusts:

| Axis | What it strengthens |
| --- | --- |
| Warmth / team orientation | Insight into crew stress, relationships and growth |
| Research orientation | Research speed and roadblock suggestions |
| Production orientation | Base development and resource efficiency |
| Political orientation | Reading colony politics and NPC motives |
| Antagonist investigation | Spotting anomalies, patterns and suspects |

- **A research tool you power up.** More power, more processors and AI research make it stronger, which competes with everything else for power and launch mass.
- **Insights, not just hints.** As it improves, the AI surfaces knowledge the player couldn't otherwise have: a colony about to rebel, a crew member near breaking, a base layout flaw, an inconsistency in an NPC's story.
- **The insight log.** Every insight is logged, and the player decides whether and how to act on it.

## Missions & tactical layer

Missions are turn-based, as in XCOM, and most can be solved without fighting. Combat should come up in roughly every other mission at most, and even then it's one option.

**Mission types:** rescue, survey, repair, salvage, diplomacy, investigation, and occasional confrontation.

**Approaches on every mission:** talk, engineer, sneak, trade or fight. Non-lethal and clever outcomes improve diplomacy. Violence carries a relationship cost even when it works.

**Tactical ingredients**

- Low gravity changes movement: long jumps, falls you survive, and hoppers that cross obstacles.
- Comms shadows behind crater walls and ridges cut robots off from new orders.
- Power and comms disruption are central tools on both sides (see Robots & the AI).
- Lighting matters: long, raking polar shadows hide units and block solar power.
- Bleed-out: a downed crew member has a few turns before their suit fails. Getting them to the airlock or a medic is a core loop.
- Suit integrity, oxygen and dose are per-mission resources, alongside health.

**Mission flow**

1. **Trigger.** An event appears on the colony map. Selecting it opens the mission.
2. **Ready room.** A zoomed-in view of the hab's ready room (the lander airlock before a hab exists). Choose who goes and what each person carries: a suit (oxygen vs. integrity), two tools and one consumable, drawn from the colony's shared gear locker. Gear taken on a mission can be used up or lost with its wearer. Injured crew and crew at their dose limit stay home. Squad size depends on transport.
3. **Transit.** Walk, or drive once vehicles are unlocked. The colony clock pauses during the mission but advances by the travel time each way, so distant sites cost the colony more. Walking costs suit oxygen and dose before the mission starts.
4. **Turn-based encounter.** Two actions per person per turn. Each person's crew perk matters: a hauler moves heavy loads, an engineer fixes things without tools, a prospector scans. Hard problems are built so two people working together can solve what one can't, like forcing a hatch together or carrying a downed teammate home. Each mission has a turn limit tied to something in the story (the shadow line arriving, a tank about to rupture, suit reserves). It ends when that limit runs out, everyone is down, or the objectives succeed or fail. The team can also withdraw from the staging zone.
5. **Debrief.** Results flow back into the colony: resources, gear spent and found, dose, injuries, and permanent losses. A failed objective changes the story rather than ending the run.

The playable sketch implements this end to end for the Era 1 cargo pod recovery. See the README.

**Joint missions.** Missions with other colonies change with your relationship level.

| Relationship | Joint missions | Their team members |
| --- | --- | --- |
| Enemy / Acquaintance | Possible | AI-controlled; you can't command them |
| Partner | Possible | You can command them |
| Friend | Many missions are joint | Members from both teams take part, and you can command them |
| Ally | Many missions are joint | You can also, rarely, request one of their members for your own mission |

## Colony diplomacy

Relations with other colonies work like Civilization's diplomacy, except they're driven by what you actually do for or against their people, not by abstract trade deals.

**Relationship levels:** Enemy → Acquaintance → Partner → Friend → Ally.

**Two layers of relations**

- **Colony standing:** the overall relationship between your colony and theirs.
- **Personal bonds:** relationships between individual colonists. Each NPC has an influence weight, so saving a colony's chief engineer moves the whole colony, and offending a junior tech barely registers.

**Meeting a colony's people.** You don't just deal with a colony's leader. Once a colony reaches acquaintance level, you can form relationships with any of its first 5 key members, and any of them could be an antagonist.

- **First contact** happens on a mission, on a visit to their colony, or when your comms network comes within range of their communications towers.
- **Staying in touch:** after first contact, you can talk to that person at any time, as long as your communications towers reach theirs.
- **Comms reach is diplomatic reach.** Extending your tower network is how you open and keep relationships, and losing a relay can cut you off from an ally at the worst moment.

**Starting grievances.** Some colonies begin with history against you: your sponsor won an ice claim they wanted, or an earlier mission caused an accident they blame you for. Overcoming that takes repeated concrete acts.

**Alliances you need.** Critical capabilities are spread across colonies, so you can't win alone. Examples:

- One colony has the only fission reactor that can carry you through the lunar night.
- Another has access to the ice in a shadowed crater.
- A third has the medical bay you'll need after a bad mission.

**What moves relations.** Every driver can go either way, depending on how it turns out.

| Driver | Improves relations | Degrades relations |
| --- | --- | --- |
| Trade (opens at Partner level) | Fair, reliable deals | Broken or exploitative deals |
| Missions | Successful joint or aid missions | Failures, collateral damage |
| Individual friendships | Bonds with influential members | Insults, betrayals |
| Common enemies | Standing together against a threat | Abandoning them under threat |
| Acts of kindness | Rescue, sheltering, supply aid | Refusing help in a crisis |

Trade is part of how a colony reaches Partner level in the first place.

**The antagonist's sabotage.** Antagonists often force one of these drivers at the worst moment: a trade that goes bad, a mission set up to fail, a rumor that turns a friendship. Their goal is to degrade relations between colonies.

**Who runs the colonies.** Colonies aren't strictly national. Some are joint projects of several nations, like the space station. Others are corporate. Any of them can change course under pressure, and this is where the real story develops:

- **Rebel** against their sponsors and declare independence.
- **Grow desperate** and offer risky or lopsided trades.
- **Turn violent** out of necessity or greed, becoming outlaw colonies.

**Their problems are your problems.** The state of the other colonies matters almost as much as your own. A neighbor's failing life support becomes your refugee crisis, and their collapse can take your supply line with it. Their successes can be shared: tech, goods, water, food, items, and possibly personnel.

## Base, economy & launches

Every kilogram from Earth is expensive, so research is how you buy independence, and independence changes your story with Earth.

**Launch economy**

- Resupply is a market and a calendar. Delivered cost starts near today's commercial lander prices (roughly $1M per kg to the surface, approximate) and falls as launch tech matures through the eras.
- Each manifest is a mass budget: crew, robots, spare parts, science payloads or habitat modules. Return flights carry samples, data or people back to Earth. Other colonies compete for capacity on every launch.
- Transit takes about 3 days, so emergency resupply and rescue from Earth are possible but expensive.
- Earth's sponsors attach conditions to funding. Self-sufficiency loosens their grip, which opens story branches.

**ISRU and production lines**

- Ice mining: water, oxygen, and hydrogen/oxygen propellant
- Regolith oxygen extraction
- Regolith construction: sintering, melting and large-format 3D printing for habitats, shielding and landing pads
- Power: solar on the lit peaks, batteries and fuel cells, and eventually fission for the lunar night
- Food: greenhouse modules, which also help stress recovery

**Base building.** XCOM-style base layout with real constraints: shielding depth, thermal management, power for the night, and dust control at airlocks.

**Placement and logistics (Per Aspera-style).** No visible grid or hexes. Structures go almost anywhere the terrain allows, as long as they're within range of power and within the road-length limit. Mining and construction take time, but transporting goods has no load limits and only travel time along the road network.

## Map & exploration

The 3D map is the real lunar south pole, built from NASA's public-domain LRO (Lunar Reconnaissance Orbiter) LOLA elevation data, which is especially high-resolution at the poles.

- **Real landmarks:** Shackleton crater's rim, the peaks of near-eternal light, and the cold, shadowed crater floors where the ice is.
- **Real lighting:** low sun angles cast long, raking shadows that sweep across the terrain. The lunar night is a recurring phase.
- **Exploration, Astroneer-style:** drive rovers and walk EVAs, collect samples, scout sites, and place relay towers to extend your comms network.
- **Discovery feeds research:** samples and survey data unlock techs and story threads, including the lava tubes.
- **The map is shared:** other colonies hold territory, claims overlap, and the best sites are contested.

## Engine & prototype plan

Platform: PC first, for local testing. **Engine: Unity** for the playable sketch. It fits both the zoomed-out management view and the tactical missions, iterates quickly for systems-heavy strategy, and has the largest supply of ready-made assets (RTS camera, fog of war, splines). Godot is the fallback if fully open source matters; Unreal is an option later for a visual showpiece.

**Views**

- **Management view:** mostly zoomed out over real south pole terrain, with one continuous camera that can zoom down to ground level anywhere.
- **Fog of war:** terrain is known for the whole map. Fog hides the current state of areas, with three states: never explored, explored but stale, and actively monitored by towers, robots or crew.
- **Missions:** a limited area of engagement, like XCOM 2, loaded as a separate scene from a high-resolution chunk of the same elevation data. Isometric camera rotates in 90° steps; interactable items are highlighted and clearly visible when zoomed in.

**Technical approach**

- Terrain from LOLA south pole elevation data, cropped to a 20–50 km playable region for the sketch.
- Placement by checking where the cursor hits the terrain, then testing slope, power range and road length.
- Roads as splines with a length budget; goods move by pathfinding over the road network.
- A fog mask texture read by the terrain shader.
- A low directional sun for the long polar shadows.

**Sketch milestones**

1. South pole terrain, zoomable camera and fog of war
2. Place 3 structures (habitat, solar array, comms tower) with power range and roads
3. Goods moving between structures
4. One small turn-based mission map with a rotating camera and highlighted interactables

## Open questions

- [ ] Set the exact antagonist count for each colony-count and difficulty combination.
- [ ] Verify the science figures in the Setting section before locking in balance numbers.
