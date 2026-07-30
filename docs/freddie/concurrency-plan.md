# Concurrency Plan — MVP (P0 Tasks)

How 4 team members work concurrently on the Unity project without causing git conflicts.

---

## Core Principle

**Git conflicts only happen when two people edit the SAME FILE.** Different files in the same folder merge cleanly. So conflict avoidance is about controlling **who edits which files** — not which folders.

In Unity, there are exactly **5 file types** that cause real conflicts:

| File type | Why it conflicts | Example |
|---|---|---|
| **Scene** (`.unity`) | Serialized YAML, practically unmergeable | Two people edit MainArena.unity |
| **Prefab** (`.prefab`) | Serialized YAML, very hard to merge | Two people edit fighter.prefab |
| **Animator Controller** (`.controller`) | Serialized YAML, unmergeable | Two people add states/parameters |
| **ProjectSettings** (`TagManager.asset` etc.) | Single shared config file | Two people add tags |
| **Same `.cs` file** | Standard code conflict | Two people edit GameManager.cs |

Everything else — different `.cs` files, different `.asset` files, different `.prefab` files, different folders — merges automatically with zero conflict.

This means: if 4 people each create their **own** scripts, their **own** prefabs, and their **own** test scenes, they can work at full concurrency with **zero git conflicts**. The only files that need ownership rules are the 5 types above.

---

## Task Assignment

### Member A — Combat Systems + UI Art Assets (37h)

| Task | Hours |
|---|---|
| Combat controller (melee, hitbox, directional) | 14 |
| Light + heavy melee | 6 |
| Block + Parry | 8 |
| Hit-stun / knockback | 4 |
| UI art (sprite/texture creation only) | 5 |

**Why this grouping maximizes concurrency:** Combat is the largest single chunk of gameplay code (32h). It depends on the player rig existing (Member C's work) and Animator parameters being defined (shared contract). By assigning the standalone UI art task (5h) to the same person, Member A has productive work to do in the early phase while waiting for C to export the character. UI art is sprite/texture creation only — image files that Member B will later reference in the HUD prefab. A never touches the HUD prefab itself.

### Member B — Game Systems + ALL Audio (55h)

| Task | Hours |
|---|---|
| Health & damage system + shared contracts | 6 |
| Round / game state | 4 |
| Wave manager (single wave) | 4 |
| Enemy AI v1 | 12 |
| UI manager + HUD | 5 |
| Menu system | 4 |
| Audio manager (event-driven, mixer, 2D/3D) | 5 |
| Audio event hooks + AudioMapping SO | 4 |
| Audio mixer setup | 2 |
| Combat SFX | 6 |
| Footstep SFX + animation-event sync | 3 |

**Why game systems and audio are combined in one member:** The audio system is fundamentally a *consumer* of game events. `AudioEventRouter` listens to `GameEvents` that the systems layer defines. When the same person owns both the event definitions and the audio listener code, they can iterate on event names and audio hooks freely without coordinating with another team member. B defines an event in `GameEvents.cs`, then immediately writes the `AudioEventRouter` subscription in the next file — no meeting needed, no API agreement, no risk of mismatched names.

B also owns the `AudioMapping` ScriptableObject, so B controls both what events exist and what sounds they trigger. This gives B the entire "game feel feedback loop": game state changes → events fire → audio plays.

This workload is heavier than other members (55h vs 18-37h). The trade-off is that it eliminates the A↔audio coordination point entirely and keeps all event-driven logic in one person's head.

**How B manages the workload:** The systems layer (health, game state, waves, AI, UI) is foundational and must be built first. The audio layer (manager, mixer, SFX, hooks) is additive — it layers on top of the systems once events are defined. B can front-load the systems work, then build the audio system against their own events at a natural pace.

### Member C — Characters + Animation (33h)

| Task | Hours |
|---|---|
| Player knight (CC5 export) | 7 |
| Weapon mesh + hand attach | 3 |
| Enemy type 1 (CC5) | 5 |
| Combat/locomotion anims + animator wiring | 18 |

**Why this grouping maximizes concurrency:** The Animator Controller (`.controller`) is one of the 5 unmergeable file types. By assigning ALL character creation AND animation work to one person, no one else ever edits a `.controller` file. C also owns the base character prefabs that A and B build upon via prefab variants — so C can update character visuals/rigs freely without conflicting with A's combat additions or B's AI additions. C's work has no code dependencies on other members; C references agreed Animator parameter names (defined by B) but never edits anyone else's scripts.

### Member D — Level Design + Scene Integration (18h)

| Task | Hours |
|---|---|
| Zone blockout + boundaries + spawn | 7 |
| Zone environment (one zone) | 7 |
| Basic onboarding (minimal text) | 3 |
| Credits / attribution register | 1 |

**Why this role is lighter and what D does with the extra time:** Member D is the **sole editor of `MainArena.unity`** — the scene integrator. This is the most important conflict-avoidance role in the project. D's level design work happens directly in the scene D already owns. D's lighter P0 workload (18h) gives D flexibility to:
- Handle scene integration requests from the team as prefabs are completed (ongoing throughout development)
- Set up and configure the playtest build (placing all prefabs, verifying references)
- Get a head start on P1 visual tasks (lighting, post-processing, VFX) that naturally belong to the level designer

D is never idle — scene integration is an ongoing responsibility that ramps up as other members complete work.

---

## File Ownership Map

Every file in the project has exactly one owner.

```
Assets/
├── Scripts/
│   ├── Shared/              → Member B defines ONCE, then read-only for everyone
│   ├── Combat/              → Member A (sole creator/editor)
│   ├── Systems/             → Member B (sole creator/editor)
│   └── Audio/               → Member B (sole creator/editor)
│
├── Prefabs/
│   ├── Player/
│   │   ├── fighter_base.prefab     → Member C owns (mesh, animator, weapon)
│   │   └── PlayerCombat.prefab     → Member A owns (VARIANT — adds combat scripts)
│   ├── Enemies/
│   │   ├── enemy1_base.prefab      → Member C owns (mesh, animator)
│   │   └── EnemyAI.prefab          → Member B owns (VARIANT — adds AI scripts)
│   ├── UI/
│   │   ├── HUDCanvas.prefab        → Member B owns
│   │   └── MenuCanvas.prefab       → Member B owns
│   └── Environment/                → Member D owns
│
├── Art/
│   ├── Characters/          → Member C
│   ├── UI/                  → Member A (sprite PNGs only)
│   └── Environment/         → Member D
│
├── Animation/
│   └── Controllers/         → Member C (SOLE editor of all .controller files)
│
├── Audio/                   → Member B (ALL sound files, mixers, groups)
│
├── Data/
│   ├── Audio/               → Member B (AudioMapping SO)
│   ├── Enemies/             → Member B (enemy stat SOs)
│   └── Waves/               → Member B (wave config SOs)
│
├── Scenes/
│   ├── MainArena.unity      → Member D (sole editor + scene integrator)
│   ├── A_Test.unity         → Member A
│   ├── B_Test.unity         → Member B
│   └── C_Test.unity         → Member C
│
├── Settings/                → Member D (URP assets, lighting, post-processing)
└── ProjectSettings/
    └── TagManager.asset     → Member B (defined once at start, then locked)
```

---

## The 5 Conflict-Critical Files

| File | Sole Owner | What everyone else does instead |
|---|---|---|
| `Scenes/MainArena.unity` | **D** | Work in your own test scene |
| `fighter_base.prefab` | **C** | Use a prefab variant |
| `enemy1_base.prefab` | **C** | Use a prefab variant |
| `*.controller` (Animator) | **C** | Reference parameters by name in code |
| `TagManager.asset` | **B** (one-time) | Use existing tags; request new ones from B |

---

## Procedure: How Each Member Works Without Touching Anyone Else's Files

### Member A — Combat Systems

**Creates:**
- `Scripts/Combat/CombatController.cs`
- `Scripts/Combat/Hitbox.cs`
- `Scripts/Combat/BlockParrySystem.cs`
- `Scripts/Combat/HitStunSystem.cs`
- `Prefabs/Player/PlayerCombat.prefab` (variant of C's `fighter_base.prefab`)
- `Art/UI/*.png` (health bar sprite, HUD frame, etc.)
- `Scenes/A_Test.unity`

**How A tests combat:**

A works entirely in `A_Test.unity`. This scene contains a ground plane, a light, and an instance of C's `fighter_base.prefab` (dragged in — the prefab file itself is not modified). A adds their combat scripts to this instance, or creates their `PlayerCombat.prefab` variant and places that in the test scene.

**How A uses C's animator without editing it:**

A's combat code triggers animations by **parameter name**, using a shared constants class:

```csharp
// In Scripts/Shared/AnimParams.cs — created by B on Day 1, read-only after
public static class AnimParams
{
    public static readonly int Speed       = Animator.StringToHash("Speed");
    public static readonly int AttackLight = Animator.StringToHash("AttackLight");
    public static readonly int AttackHeavy = Animator.StringToHash("AttackHeavy");
    public static readonly int Block       = Animator.StringToHash("Block");
    public static readonly int Parry       = Animator.StringToHash("Parry");
    public static readonly int Hit         = Animator.StringToHash("Hit");
    public static readonly int Die         = Animator.StringToHash("Die");
}
```

```csharp
// In A's CombatController.cs — A's own file
animator.SetTrigger(AnimParams.AttackLight);  // triggers the animation C set up
```

A never opens the Animator window. A never edits the `.controller` file. A just calls `SetTrigger` with the agreed name. C has already created the matching parameter and state transition in the controller.

**How A uses B's health system without editing it:**

A's `CombatController` deals damage by calling the interface B defined:

```csharp
// A's code — finds IDamageable on whatever A's weapon hits
var target = hit.collider.GetComponent<IDamageable>();
target?.TakeDamage(damageAmount, DamageType.Light, gameObject);
```

A doesn't reference B's `EnemyHealth` class directly. A references `IDamageable` — the interface B created in `Scripts/Shared/`. Any MonoBehaviour implementing `IDamageable` (B's `EnemyHealth`, A's own player, a future boss) receives damage. A's code works against the **contract**, not the implementation.

**How A creates the PlayerCombat prefab variant:**

1. In Unity, right-click C's `fighter_base.prefab` in the Project window
2. `Create > Prefab Variant`
3. Name it `PlayerCombat.prefab`
4. Open it in Prefab Mode
5. Add A's combat scripts (`CombatController`, `Hitbox`, `BlockParrySystem`) as components
6. Add hitbox colliders (empty child GameObjects with trigger colliders)
7. Save

The variant is a **separate `.prefab` file** that only stores A's additions. C can update the base (change materials, swap mesh, adjust rig) and those changes automatically propagate to A's variant. Git sees two different files — zero conflict.

**How A creates UI art without touching B's HUD prefab:**

A creates sprite assets (`Art/UI/healthbar_fill.png`, `Art/UI/hud_frame.png`, etc.). These are standalone image files. B references them in the HUD prefab by dragging them into material/sprite fields in the Inspector. Only B edits `HUDCanvas.prefab`. A only creates image files.

---

### Member B — Game Systems + Audio

**Creates:**
- `Scripts/Shared/IDamageable.cs`, `Scripts/Shared/GameEvents.cs`, `Scripts/Shared/AnimParams.cs`
- `Scripts/Systems/HealthSystem.cs`, `Scripts/Systems/GameManager.cs`, `Scripts/Systems/RoundManager.cs`
- `Scripts/Systems/WaveManager.cs`, `Scripts/Systems/EnemyAI.cs`
- `Scripts/Systems/UIManager.cs`, `Scripts/Systems/MenuManager.cs`
- `Scripts/Audio/AudioManager.cs`, `Scripts/Audio/AudioEventRouter.cs`
- `Scripts/Audio/AudioMapping.cs`, `Scripts/Audio/FootstepAnimator.cs`
- `Prefabs/UI/HUDCanvas.prefab`, `Prefabs/UI/MenuCanvas.prefab`
- `Prefabs/Enemies/EnemyAI.prefab` (variant of C's `enemy1_base.prefab`)
- `Data/Enemies/*.asset` (enemy stat ScriptableObjects)
- `Data/Waves/*.asset` (wave config ScriptableObjects)
- `Data/Audio/AudioMapping.asset` (audio mapping ScriptableObject)
- `Audio/SFX/` (all sound effect files)
- `Audio/Mixers/CombatMixer.mixer`
- `Scenes/B_Test.unity`

**Day 1 responsibility — shared contracts:**

B's first task is to create the shared contract files and push them immediately. These are the **API** that all other members build against:

```csharp
// Scripts/Shared/IDamageable.cs — B creates, everyone implements
public interface IDamageable
{
    void TakeDamage(float amount, DamageType type, GameObject source);
    float CurrentHealth { get; }
    float MaxHealth { get; }
    bool IsAlive { get; }
}
```

```csharp
// Scripts/Shared/GameEvents.cs — B creates, everyone subscribes/publishes
public static class GameEvents
{
    // Combat (A fires these, B listens for audio)
    public static event Action<GameObject, float, DamageType> OnDamageDealt;
    public static event Action<GameObject> OnEnemyKilled;
    public static event Action OnPlayerBlock;
    public static event Action OnPlayerParry;

    // Game state (B fires these, A listens)
    public static event Action<int> OnWaveChanged;
    public static event Action OnRoundStart;
    public static event Action OnRoundEnd;
    public static event Action OnGameStart;
    public static event Action OnGameOver;
}
```

After this initial commit, `Scripts/Shared/` is **treated as read-only**. If a new event or interface method is needed, B adds it, commits, and pushes. Nobody else edits these files.

**How B tests game state without touching MainArena:**

B works in `B_Test.unity`. This scene has:
- A `GameManager` GameObject (with B's `GameManager.cs`)
- A `WaveManager` GameObject (with B's `WaveManager.cs`)
- An `AudioManager` GameObject (with B's `AudioManager.cs`)
- An `AudioEventRouter` GameObject (with B's `AudioEventRouter.cs`)
- Empty GameObjects tagged `"SpawnPoint"` placed at test positions
- An instance of C's `enemy1_base.prefab` (or B's own `EnemyAI.prefab` variant) dragged in
- A test player — a simple capsule with a `TestHealth` component that implements `IDamageable`
- A `HUDCanvas` (B's own UI prefab) for testing the HUD

B tests round transitions, wave spawning, enemy AI, and audio playback entirely in this scene. None of this touches `MainArena.unity`.

**How B uses C's enemy prefab without editing it:**

B creates `EnemyAI.prefab` as a **prefab variant** of C's `enemy1_base.prefab`:
1. Right-click `enemy1_base.prefab` → `Create > Prefab Variant`
2. Name it `EnemyAI.prefab`
3. Open in Prefab Mode
4. Add B's `EnemyAI.cs`, `NavMeshAgent` component, collider adjustments
5. Save

C owns the base (mesh, animator, materials). B owns the variant (AI logic). Two different files. C can update the base and it propagates to B's variant automatically.

**How B uses A's UI art without editing A's files:**

B creates `HUDCanvas.prefab`. In the Inspector, B drags A's sprite assets (from `Art/UI/`) into the `Image` components on the HUD elements. B is referencing A's files, not editing them. If A updates a sprite PNG, the reference automatically picks up the new texture.

**How B's enemy AI uses spawn points from any scene:**

B's `WaveManager` discovers spawn points dynamically by tag — it works in any scene:

```csharp
// B's WaveManager.cs — works in B_Test.unity AND MainArena.unity
void FindSpawnPoints()
{
    spawnPoints = GameObject.FindGameObjectsWithTag("SpawnPoint");
}
```

In `B_Test.unity`, B places SpawnPoint-tagged empties at test positions. In `MainArena.unity`, D (scene owner) places the real SpawnPoint-tagged empties at the actual arena positions. Same code, different scenes, no conflict.

**How B's audio system hooks into combat without editing A's code:**

Since B owns both the event definitions and the audio listener, this is a single-person loop. B creates `AudioEventRouter.cs` — a component that listens to the events B also defined in `GameEvents.cs`:

```csharp
// B's file — Scripts/Audio/AudioEventRouter.cs
public class AudioEventRouter : MonoBehaviour
{
    [SerializeField] private AudioManager audioManager;
    [SerializeField] private AudioMapping mapping;

    void OnEnable()
    {
        GameEvents.OnDamageDealt += HandleDamage;
        GameEvents.OnEnemyKilled += HandleKill;
        GameEvents.OnPlayerBlock += HandleBlock;
        GameEvents.OnPlayerParry += HandleParry;
        GameEvents.OnRoundStart += HandleRoundStart;
        GameEvents.OnWaveChanged += HandleWaveChanged;
    }

    void OnDisable()
    {
        GameEvents.OnDamageDealt -= HandleDamage;
        // ... unsubscribe all
    }

    void HandleDamage(GameObject target, float amount, DamageType type)
    {
        string clipName = mapping.GetHitClip(type);
        audioManager.PlaySFX(clipName, target.transform.position);
    }
}
```

A fires `GameEvents.OnDamageDealt?.Invoke(target, amount, type)` from inside A's combat code. B's router catches it and plays the sound. B never edits A's `CombatController.cs`. A never edits B's `AudioManager.cs`. They communicate **only through the event channel** that B defined and B consumes.

**How B's AudioMapping SO works:**

B creates `Data/Audio/AudioMapping.asset` — a ScriptableObject that maps event types to audio clip names:

```csharp
// B's file — Scripts/Audio/AudioMapping.cs (defines the SO)
[CreateAssetMenu(fileName = "AudioMapping", menuName = "Game/Audio Mapping")]
public class AudioMapping : ScriptableObject
{
    public List<CombatAudioEntry> combatEntries;
    public List<UIAudioEntry> uiEntries;

    public string GetHitClip(DamageType type) { /* lookup */ }
    public string GetBlockClip() { /* lookup */ }
}
```

B fills in the clip assignments in the Inspector (B's own `.asset` file). If B needs to change which sound plays on a heavy hit, B edits the SO — not anyone's code.

**How B's footstep SFX syncs with C's animations:**

B needs to trigger footstep sounds synchronized with locomotion animations. The animation clips are inside C's FBX files (which C owns). B cannot edit C's FBX.

*Approach 1 (recommended): B detects footstep timing via script at runtime.*

```csharp
// B's file — Scripts/Audio/FootstepAnimator.cs
// Attaches to the player, listens for normalized time thresholds
void Update()
{
    var state = animator.GetCurrentAnimatorStateInfo(0);
    if (state.IsName("RunForward"))
    {
        float t = state.normalizedTime % 1f;
        if (t > lastFootstepTime + 0.5f)  // every half-cycle
        {
            audioManager.PlaySFX("footstep_run", transform.position);
            lastFootstepTime = t;
        }
    }
}
```

B's script lives on a component B adds to their own audio rig in the test scene. C's animation clips and controller are untouched.

*Approach 2: B creates AnimationClip overrides.*

B creates `.anim` override clips in `Animation/Clips/` that add footstep events. These are B's own files. C's original clips in the FBX are untouched. B places the overrides in an `AnimatorOverrideController` (B's own `.overrideController` file).

Either way, C's files are never modified.

---

### Member C — Characters + Animation

**Creates:**
- `Art/Characters/Player/` (FBX, textures, materials from CC5)
- `Art/Characters/Enemy1/` (FBX, textures, materials from CC5)
- `Prefabs/Player/fighter_base.prefab` (the base character prefab)
- `Prefabs/Enemies/enemy1_base.prefab` (the base enemy prefab)
- `Animation/Controllers/Player.controller`
- `Animation/Controllers/Enemy1.controller`
- `Animation/Clips/` (animation clips, if extracted from FBX)
- `Scenes/C_Test.unity`

**How C tests animations:**

C works in `C_Test.unity` — a barebones scene with a ground plane and instances of the character prefabs. C tests animation blending, state transitions, and parameter responses here. C is the **sole editor** of the `.controller` files, so there is never a conflict on animator state.

**How C's animator parameters match A's combat code:**

Before A starts writing combat code, C and A agree on the Animator parameter list (the `AnimParams.cs` constants class that B committed). C creates these exact parameters in the Animator Controller:

| Parameter | Type | C sets up | A triggers via code |
|---|---|---|---|
| `Speed` | Float | Blend tree for locomotion | `animator.SetFloat(AnimParams.Speed, moveSpeed)` |
| `AttackLight` | Trigger | Transition to light attack state | `animator.SetTrigger(AnimParams.AttackLight)` |
| `AttackHeavy` | Trigger | Transition to heavy attack state | `animator.SetTrigger(AnimParams.AttackHeavy)` |
| `Block` | Bool | Transition to block stance | `animator.SetBool(AnimParams.Block, true)` |
| `Parry` | Trigger | Transition to parry animation | `animator.SetTrigger(AnimParams.Parry)` |
| `Hit` | Trigger | Transition to hit reaction | `animator.SetTrigger(AnimParams.Hit)` |
| `Die` | Trigger | Transition to death animation | `animator.SetTrigger(AnimParams.Die)` |

C builds the state machine. A calls the parameters from code. They never edit each other's files.

**How C exports the player prefab for A and B to use:**

1. C imports the CC5-exported FBX into `Art/Characters/Player/`
2. C sets the Rig tab to **Humanoid** (required for Mecanim retargeting)
3. C creates `fighter_base.prefab` by dragging the FBX into the scene and configuring it (materials, animator, weapon attach points)
4. C commits and pushes

Now A can create `PlayerCombat.prefab` (variant) and B can reference the prefab in their test scene. C can continue updating the base — changes propagate to variants automatically.

**What C never does:**
- Never adds combat scripts to the prefab (that's A's job, via variant)
- Never adds AI scripts to the enemy prefab (that's B's job, via variant)
- Never edits `MainArena.unity` (that's D's job)
- Never creates code files in `Scripts/Combat/`, `Scripts/Systems/`, or `Scripts/Audio/`

---

### Member D — Level Design + Scene Integration

**Creates:**
- `Scenes/MainArena.unity` (sole editor)
- `Prefabs/Environment/` (level geometry, props, boundaries)
- `Settings/` (lighting, URP assets, post-processing)
- `Art/Environment/` (if custom environment textures needed)

**How D owns the scene and integrates everyone's work:**

D is the **sole editor** of `MainArena.unity`. D does two types of work in this scene:

*Level design work:* Building the arena — placing castle walls, ground, boundaries, spawn points, lighting. This is D's own creative work.

*Integration work:* Placing other members' prefabs into the scene so the full game comes together. When a member's work is tested and ready:

1. They tell D: "PlayerCombat prefab is ready for integration"
2. D pulls the latest `main` (gets the new prefab file)
3. D opens `MainArena.unity`
4. D drags the prefab from the Project window into the scene
5. D positions it, assigns references (spawn points, etc.)
6. D saves and commits

Only D edits `MainArena.unity`. No one else ever opens it for editing.

**How D places spawn points that B's WaveManager discovers:**

D places empty GameObjects in `MainArena.unity` at the desired spawn locations and tags them `"SpawnPoint"`. B's `WaveManager` discovers these at runtime via `FindGameObjectsWithTag("SpawnPoint")`. D never writes code; B never edits the scene. The tag is the contract between them.

**What D never does:**
- Never creates `.cs` script files (all code belongs to A or B)
- Never edits prefabs owned by other members (drag instances into the scene instead)
- Never edits `.controller` files
- Never edits `TagManager.asset` (B defines tags; D uses them)

---

## How Cross-Member Work Flows

### The contract layer (B defines, everyone uses)

```
Scripts/Shared/
├── IDamageable.cs       → B creates, A implements on player, B implements on enemies
├── GameEvents.cs        → B creates, A fires combat events, B listens for audio + game state
└── AnimParams.cs        → B creates, A references in code, C creates matching params in Animator
```

This is the **decoupling layer**. It's the reason nobody needs to edit anyone else's files.

### The prefab hierarchy

```
C creates base prefabs
├── fighter_base.prefab     → A creates variant: PlayerCombat.prefab (adds combat scripts)
└── enemy1_base.prefab      → B creates variant: EnemyAI.prefab (adds AI scripts)

B creates UI prefabs
├── HUDCanvas.prefab        → references A's UI art sprites
└── MenuCanvas.prefab

D creates environment prefabs
└── (castle walls, props, etc.)

D integrates ALL prefabs into MainArena.unity (sole scene editor)
```

### The event flow

```
Player presses attack button
    │
    ▼
A's CombatController.cs
    calls animator.SetTrigger(AnimParams.AttackLight)  →  C's Animator plays swing animation
    calls IDamageable.TakeDamage() on hit               →  B's EnemyHealth reduces HP
    fires GameEvents.OnDamageDealt                      →  B's AudioEventRouter plays hit SFX
    fires GameEvents.OnEnemyKilled (if dead)            →  B's WaveManager updates kill count
                                                         →  B's AudioEventRouter plays death SFX
```

Nobody edits anyone else's file. Every arrow in this flow crosses file boundaries via interfaces, events, or prefab references.

---

## Tags and Layers (Defined Once by B)

Member B commits these in `ProjectSettings/TagManager.asset` on Day 1. After this commit, nobody edits `TagManager.asset` again.

**Tags:** `Player`, `Enemy`, `SpawnPoint`, `Weapon`, `Hitbox`, `Environment`, `Boundary`

**Layers:**

| # | Layer |
|---|---|
| 0 | Default |
| 1 | TransparentFX |
| 2 | IgnoreRaycast |
| 3 | Ground |
| 4 | Player |
| 5 | Enemy |
| 6 | Hitbox |
| 7 | Environment |
| 8 | UI |

---

## Communication Coordination Points

These are NOT git conflicts — they are times when two members need to talk to each other. Resolved by agreeing on names/APIs, not by editing the same files.

| Who | What to agree on | When |
|---|---|---|
| A + C | Animator parameter names (the table in Member C's section above) | Before A starts writing combat code |
| C + ALL | Base prefab readiness: C pushes fighter_base and enemy1_base before A and B create variants | Before A and B start prefab variant work |
| D + ALL | Scene integration: each member tells D when their prefab is tested and ready for placement in MainArena | Ongoing, as work completes |

Note: Because B owns both the game events and the audio listener, there is **no A↔audio coordination point**. B defines the events and consumes them in the same workflow.

---

## Daily Workflow (All Members)

```
1. git checkout main && git pull
2. Open Unity, let it import any new assets from teammates
3. git checkout -b yourname/feature-name
4. Work in YOUR files and YOUR test scene only
5. If you need something from a teammate that doesn't exist yet:
   - Use a test stub (temporary script implementing the interface)
   - Swap the stub for the real component when it's ready
6. Commit and push frequently (multiple times per day)
7. Communicate in team chat: "PlayerCombat.prefab is ready for integration"
8. Close Unity before git operations (prevents file locks)
```

**The one rule that prevents 95% of conflicts:** Never open `MainArena.unity` unless you are Member D. Never open a `.prefab` or `.controller` file you don't own. If you need to test with someone's prefab, drag an **instance** into your test scene — the prefab file stays untouched.

---

## Test Stubs (How to Work Before Dependencies Exist)

When a member needs to test against a system that hasn't been built yet, they create a **temporary test stub** — a minimal script that implements the expected interface. This is placed in the member's own folder and deleted when the real implementation is ready.

**Example — B testing damage before A's CombatController exists:**

```csharp
// B's temporary file: Scripts/Systems/TestDamageDealer.cs
// Deleted once A's real CombatController is integrated
public class TestDamageDealer : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // Finds any IDamageable in front and deals damage
            // Works against B's EnemyHealth (which implements IDamageable)
            // Will also work against A's future PlayerCombatController
            Collider[] hits = Physics.OverlapSphere(transform.position + transform.forward, 2f);
            foreach (var hit in hits)
            {
                var damageable = hit.GetComponent<IDamageable>();
                damageable?.TakeDamage(10f, DamageType.Light, gameObject);
            }
        }
    }
}
```

B tests the full damage pipeline (health reduction, death, events firing) using this stub. When A's real combat system is ready, B deletes the stub — no changes needed to B's health code because it works against `IDamageable`, not against any specific class.

**Example — B testing audio before A's combat events exist:**

```csharp
// B's temporary file: Scripts/Audio/TestAudioTrigger.cs
// Deleted once A's real combat events are firing
public class TestAudioTrigger : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.H))
            GameEvents.OnDamageDealt?.Invoke(gameObject, 10f, DamageType.Light);
        if (Input.GetKeyDown(KeyCode.K))
            GameEvents.OnEnemyKilled?.Invoke(gameObject);
    }
}
```

B presses H to simulate a hit, K to simulate a kill. B's AudioEventRouter plays the correct sounds. When A's real combat code fires these events, B deletes the stub — the router works unchanged.
