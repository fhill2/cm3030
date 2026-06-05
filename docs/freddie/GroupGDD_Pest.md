# Pest — Game Design Document

---

## Elevator Pitch

You are a rat living inside the walls of an apartment building. When the human who lives there spots you, they call pest control — and your home becomes a death trap. Scurry through ducts, steal food from under their nose, dodge a broom-wielding flat owner, and escape a poison gas extermination in a frantic, physics-driven game of survival.

---

## Summary

*Pest* is a 3D third-person game about being a rat in a human world. The camera sits low and close behind the rat, placing the player at ground level inside the ventilation shafts, wall cavities, and under-floor spaces of an apartment building — environments built at rat scale, where a gap behind the fridge is a doorway and a dropped crisp is a feast. The game alternates between stealth (stealing food while the occupant sleeps) and evasion (dodging broom sweeps). Over five scenes, the game escalates from casual scavenging to a desperate race against poison gas flooding through your ductwork home. 

The game is controlled with keyboard and mouse: WASD to move, mouse to look, shift to squeeze through tight spaces, and a handful of context-sensitive actions.

---

## Unique Selling Points

1. **Rat-perspective scale** — The game world is experienced from the ground level from a rats perspective. This perspective shift makes every mundane object interesting and threatening.

2. **The Broom Mechanic** — When the flat owner spots you, they attack with a broom. Hits send the player across the room with force. The broom sweeps objects around the room, dynamically reshaping the level. The human tracks your *last known position*, not your actual position, allowing for feints and misdirection.

3. **Infrastructure as level design** — Ducts, wall cavities, pipe chases, and under-floor crawlspaces are the primary play spaces.

4. **Dual-mode tension** — The game shifts between slow, held-breath stealth (stealing food while someone sleeps in the next room) and chaos (being flicked across a living room by a broom). This contrast will make the game enjoyable.

5. **One-building scope** — Every scene takes place in an apartment building. This keeps scope tight while allowing maximum variety (ducts, rooms, staircases, exterior). It also creates a coherent, believable world rather than a disjointed series of levels.

---

## The Team

TO DISCUSS WITH TEAM

| Role | Responsibilities |
|---|---|
| Programmer 1 | Core movement system, third-person camera, physics interactions, scene management |
| Programmer 2 | Human AI (broom chase, detection, pathfinding), pest control systems, gas spread, cat AI |
| 3D Artist / Level Designer | Character models/animations, environment models, textures, duct layouts, room layouts, lighting, pacing |
| Audio / UI Designer | Sound effects, ambient soundscapes, music, spatial audio, voice, UI screens, control hints, visual effects |

---

## Evidence of Prototyping

TODO

---

## Key Mechanics

### Movement

| Control | Action |
|---|---|
| WASD | Move (relative to camera) |
| Mouse | Third-person camera orbit |
| Space | Jump (~2x body height, ledge grab) |
| Shift (hold) | Squeeze through tight gaps (40% speed) |
| W against wall | Climb rough surfaces (~30% speed) |

Slight momentum on movement for a "scurrying" feel. Camera pushes in during squeeze (claustrophobia) and shifts side-on during climbing. Smooth surfaces (glass, polished metal) cannot be climbed.

### Scent / Sniff

**Hold Q** — freezes the rat in place. Reveals:

- Poison bait stations as red pulsing glow (through walls, small radius)
- Snap traps as rhythmic clicking (audio)
- Other rats' scent trails as faint amber lines (safe route hints)

Risk/reward: the rat is vulnerable while sniffing.

### Carry System

Walk over food to auto-pickup. Speed reduced 25% while carrying. Drop food at nest/hole. Broom hits drop the food, potentially into inaccessible spots and have to be retrieved.

| Item | Points |
|---|---|
| Bread crust / Cheese / Cereal | 1 |
| Biscuit / Pizza crust | 2 |
| Half sausage | 3 |

Larger items make the rat more visible to the human.

### Broom Evasion (Signature Mechanic)

```
SPOTTED → "A RAT!" → 1.5s freeze (player's head start)
  ↓
CHASE → Human moves to LAST KNOWN POSITION (not real-time)
  ↓ If rat visible: update position, continue
  ↓ If rat hidden:
SEARCH → Checks behind nearby furniture (2s scan)
  ↓ After 10s: return to idle
```

**Broom stats:** 120° sweep arc, 0.6s duration, 3-4 body-length knockback, 1s stun. Also sweeps furniture objects (dynamic cover/blockages).

**Player counters:** Misdirection (run one way, hide another), furniture hopping, squeeze escape, bait broom into knocking objects for new cover, freeze to avoid detection.

### Gas Escape (Scene 4)

Visible yellow-green fog advances through ducts at 60% of rat speed. Rat is faster, but wrong turns cost time. Some grates sealed by exterminator (visible bolts). Other rats flee through ducts as route hints. Screen distortion when gas is close.

---

## Characters and Settings

### Characters

**The Rat (Player)** — Small brown rat, no dialogue. Emotion conveyed through animation (cowering near danger, perking near food, scurrying when detected). No health bar — hazards have specific consequences:

| Hazard | Consequence |
|---|---|
| Snap trap | Death — respawn at checkpoint |
| Poison bait | Gradual screen distortion over 15s, then death — find water to flush |
| Broom hit | Ragdoll knockback + 1s stun |
| Gas contact | Gradual slowdown over 5s, then death |

**The Flat Owner (Scenes 2, 3)** — Only feet, lower legs, and broom are visible. Asleep in Scene 2 (snoring with false-alarm pauses). Hostile in Scene 3 (broom chase, shoe throwing). Leaves after calling pest control.

**The Exterminator (Scene 4)** — Never seen. Presence communicated through audio only: heavy boots above, gas hissing, grates being bolted shut, muffled radio chatter. Unseen = more terrifying, and requires zero character assets.

### Settings

---

## Scene 1 — The Duct Network (Tutorial)

ventilation shafts with riveted seams, dusty grates, junction boxes. Older sections rusted with holes to wall cavities. Vertical shafts connect floors. The rat's nest is here — shredded paper behind a warm pipe.

**Opening:** Rat curled up in dark, warm nest. Deep vibration, crash. Rat startles awake. Duct collapses behind the nest, forcing the player forward. Only one direction to go — WASD learned by necessity.

**Duct Escape:** Collapsing duct forces forward movement (tutorial). Encounters: floor gap (jump — the gap IS the prompt, failure loops back via lower duct), low ceiling (crawl, speed reduced), vertical shaft (climb — walking into the wall latches the rat on, natural discovery), scratched grate (squeeze — one-time hint: "HOLD SHIFT — SQUEEZE").

**Duct Navigation:** Ducts open into branching network. No time pressure. Three paths: left (leads up toward fridge hum, dead rat warns of trap — teaches sniff mechanic), middle (dark, food smell — route to kitchen), right (cool air, dripping — blocked for now). Player naturally takes middle path toward food.

TODO: Possibly remove this Scene, hard to find prefabs.
TODO: Possibly the idea of a base, the food has to be dropped here ?

---

## Scene 2 — The Kitchen (Stealth)

Fridge (humming monolith, gap behind leads to ducts), bin (climbable, slightly open), counter (accessed via stool → bin → counter platforming), floor (trap zone — snap traps near bin, poison under sink).

**Kitchen Entry:** Emerge behind fridge. Squeeze through gap. Kitchen opens up — dark, snoring audible from next room. Bread crust near bin, cereal under stool.

**Food Collection:** Collect bread crust (easy), cereal (requires moving into open floor — snoring stops mid-run as false alarm), cheese on counter (platforming: stool → bin lid → counter edge). Discover snap trap near bin — die to learn caution, or spot it first. Dying to camouflaged poison triggers: "HOLD Q — SNIFF FOR DANGER." Death is the teacher.

**Carry food:** Walk over food → auto-pickup. Nest hole visible. Carry it back. Intuitive.

---

## Scene 3 — The Living Room (Broom Chase)

Sofa (rat runs underneath, visual cover), coffee table (objects on top knocked down by broom), TV unit (gap behind leads to ducts), bookshelf (flush to wall, blocks escape), door (squeeze-under gap to hallway).

**Entry:** Squeeze under kitchen door → hallway → living room. More cover, more food (pizza crust, crisp).

**Discovery:** Phone alarm. Footsteps. Flat owner walks past. Goes to kitchen: "What the hell... they've been in the FOOD." Human spots rat. Chaos begins. First broom swing deliberately misses — player learns the threat before being hit.

**Broom evasion:** Human chases with broom, throws shoes. Player must reach escape point (gap under skirting board) while dodging. Misdirection, furniture hopping, squeeze escapes.

---

## Scene 4 — The Duct Network (Gas Escape)

Returns to the duct network but now under threat. Same environment, drastically different tone.

**The gas:** Flat owner called pest control. Exterminator arrives. Poison gas floods the ducts from one direction — visible yellow-green fog advancing at 60% of rat speed. Player must navigate duct maze faster than the gas spreads. Some grates sealed by exterminator (visible bolts). Other rats flee through ducts as route hints. Screen distortion when gas is close.

**Exterminator presence:** Never seen. Communicated through audio only: heavy boots above, gas hissing, grates being bolted shut, muffled radio chatter.

---

## Beginning the Game

TODO

---

## First 2-5 Minutes — Walkthrough

TODO

---

## Art Style

**Perspective:** 3D third-person camera at rat eye-level (~6 inches). Follows with slight lag, orbits with mouse. Pushes in during squeeze, shifts side-on during climb, pulls back during ragdoll.

**Aesthetic:** Cartoonish, low-poly textures.

**Colour palette:**

| Environment | Primary | Accent |
|---|---|---|
| Ducts | Warm grey, rust, dull silver | Amber (light through grates) |
| Kitchen (night) | Blue-grey, warm yellow | White (food items) |
| Living room | Dark brown, navy | Orange (reading lamp) |
| Gas sequence | Grey ducts | Green-yellow gas, red warning lights |
| Exterior (dawn) | Cool blue-grey, pink horizon | Gold (rising sun) |

**The Rat:** Low-poly brown rat, Compact when still, stretched when running.

**The Human:** Only legs/feet + broom visible. Low-detail — imposing through scale, not polygon count.

**Lighting:** Strong contrast. Point/spot lights from specific sources (fridge gap, streetlamp, lamp). Most of the apt in the dark, dimly light. Light = danger (visibility), dark = safety (cover). Light slits through grates in the ductwork.

**Influences:** *Limbo* (lighting, atmosphere), *Untitled Goose Game* (tone, chaos), *Moss* (small-character camera), *Stray* (animal-perspective navigation).

---

## Audio

### Design Philosophy

1. **Gameplay information** — 3D spatial audio cues for off-screen threats (snoring changes, approaching footsteps, distant gas hiss). Direction and distance are audible.
2. **Atmosphere** — Building is alive: pipe ticks, fridge hum, wind through vents. Player feels *inside* the walls.
3. **Emotional tone** — Soundscape shifts with mood: quiet tension → frantic percussion → low drones → gentle piano.

### Music

| Scene | Music | Mood |
|---|---|---|
| 1 (Ducts) | None — ambient only | Vulnerability |
| 2 (Kitchen) | None — ambient only | Tension |
| 3 (Broom chase) | Fast percussion + plucked strings | Panic, comedy |
| 4 (Gas escape) | Low drone → crescendo | Dread, urgency |
| 5 (Exterior) | Gentle piano melody | Relief |

### Key Sound Effects

**Rat:** Surface-dependent footsteps (metal/wood/tile/carpet), squeak (hit/startled), sniff (rhythmic inhale).

**Environment:** Fridge hum, pipe ticking, water drip, wind through vents, floorboard creak (human approaching), door slam.

**Human:** Snoring (with false-alarm pauses), heavy footsteps (directional), "A RAT!" (triggers chase), broom swoosh, impact thud, object crash, shoe throw, muttering (lost sight of rat).

**Gas:** Hissing canister (increasing volume), metal banging (grates sealed), muffled radio, boots above, distant rat squeaks.

**Exterior:** Dawn birdsong, distant traffic, cat hiss.

---

## SWOT Analysis

### Strengths

- **Tight scope** — One building, five scenes, no procedural generation or online features.
- **Novel perspective** — Rat's-eye third-person makes ordinary environments feel fresh.
- **Strong signature mechanic** — Broom evasion is immediately fun and memorable.
- **Low asset requirements** — Confined spaces, no full human model, low-poly style, many free prefabs available.
- **Pick up and play** — Teaches itself through environment design, almost zero text.

### Weaknesses

- **Broom AI complexity** — Last-known-position tracking must feel fair, not cheap. playtesting needed.
- **Duct navigation clarity** — Maze-like ducts in 3D need visual landmarks at junctions to prevent player frustration, especially during timed gas escape.
- **Camera in tight spaces** — May clip through walls in narrow ducts. We could increase the size of the ducts relative to the rat to accomodate.
- **Stealth pacing** — Slow sections may bore players who don't understand objectives. Clear visual cues needed.
- **Low replayability** — 5-10 min linear content. A score/time system on end screen could help.
- **Animation workload** — Rat needs 8+ animations for one artist.

### Opportunities

- **3D immersion** — Looking up at a towering fridge or seeing a broom swing from above creates moments 2D cannot match.
- **Playtesting iteration** — "Stealth" and "chaos" mode can be independently balanced.
- **Humour as differentiator** — Most student games are serious. *Pest* is genuinely funny and will stand out.
- **Audio as strength** — Environmental audio as a core gameplay mechanic aligns with the marking criteria for Sound.

### Threats

- **Scope creep** — Every addition must be weighed against polish time.
- **Physics unpredictability** — Broom sweep may send rat through walls. Constraints and extensive testing needed.
- **Difficulty balancing** — Broom scene must stay fun, not frustrating. Generous checkpointing mitigates this.
- **Camera complexity** — Third-person camera for tight ducts, climbing, ragdoll, and squeeze is non-trivial. Let's Prototype early.

---

## Production Schedule

TODO

---

## Asset List

Check [ASSET_RESEARCH.md](./ASSET_RESEARCH.md)

---

## Credits / Asset Attribution

**Credit format for each external asset:**

| Asset | Source | Author | License |
|---|---|---|---|
| *(to be filled during development)* | | | |

---
