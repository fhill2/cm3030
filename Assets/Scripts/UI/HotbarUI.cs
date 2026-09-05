using UnityEngine;
using UnityEngine.UI;
using Game.Combat;
using Game.Core;
using Game.Movement;

namespace Game.UI
{
    public class HotbarUI : MonoBehaviour
    {
        public const string IconFolder = "UI/hotbar_icons";

        private const int TotalSlots = 14;
        private const float SlotSize = 64f;
        private const float SlotGap = 0f;
        private const float AnchoredY = 0.04f;

        private const string AttackIcon = "melee_attack";
        private const string GuardIcon = "block";
        private const string TauntIcon = "taunt";
        private const string ThrowIcon = "throw_weapon";

        private class Slot
        {
            public Image Icon;
            public Image Recovery;
            public Image Sweep;
            public Outline Border;
            public Sprite Sprite;
            public Text Count;
        }

        private class SpellSlot
        {
            public SpellSchool School;
            public int Level;
            public Slot Slot;
            public SpellDef Spell;
        }

        private readonly System.Collections.Generic.List<SpellSlot> spells = new System.Collections.Generic.List<SpellSlot>();

        private GameObject root;
        private Slot attack;
        private Slot guard;
        private Slot tauntSlot;
        private Slot knockbackSlot;
        private Slot throwSlot;
        private Slot collectSlot;
        private Slot healthPotion;
        private Slot staminaPotion;

        private SpellBook spellBook;
        private SpellCaster caster;
        private Melee melee;
        private PlayerTaunt taunt;
        private PlayerMovement movement;
        private PotionBelt belt;
        private ArcStrikes strikes;

        private static readonly Color GuardActive = new Color(1f, 0.9f, 0.5f, 1f);
        private static readonly Color Dimmed = new Color(1f, 1f, 1f, 0.45f);
        private static readonly Color LockedDim = new Color(1f, 1f, 1f, 0.2f);
        private static readonly Color CooldownDim = new Color(0.35f, 0.35f, 0.35f, 1f);
        private static readonly Color SweepColor = new Color(0.75f, 0.75f, 0.75f, 0.9f);
        private static readonly Color GoldBorder = new Color(0.85f, 0.72f, 0.35f, 1f);
        private static readonly Color SlotBackground = new Color(16.5f / 255f, 16.5f / 255f, 16.5f / 255f, 1f);

        private void Awake()
        {
            spellBook = FindFirstObjectByType<SpellBook>();
            caster = GetComponent<SpellCaster>();
            if (caster == null) caster = FindFirstObjectByType<SpellCaster>();
            melee = GetComponent<Melee>();
            taunt = GetComponent<PlayerTaunt>();
            movement = GetComponent<PlayerMovement>();
            belt = GetComponent<PotionBelt>();
            strikes = GetComponent<ArcStrikes>();
        }

        private void OnEnable()
        {
            EventManager.OnGameStateChanged += HandleGameStateChanged;
        }

        private void OnDisable()
        {
            EventManager.OnGameStateChanged -= HandleGameStateChanged;
        }

        private void HandleGameStateChanged(GameStateChangedArgs e)
        {
            bool visible = e.Current != GameStateId.Menu && e.Current != GameStateId.GameOver;
            if (root != null && root.activeSelf != visible) root.SetActive(visible);
        }

        private void Update()
        {
            if (root == null || !root.activeSelf) return;
            UpdateSlots();
        }

        public void BuildInto(Transform parent)
        {
            var bar = UIBuilder.CreateRect(parent,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 1080f * AnchoredY),
                Vector2.zero);
            bar.gameObject.name = "Hotbar";
            root = bar.gameObject;

            attack = CreateSlot(bar.transform, 1, "LClick", AttackIcon);
            guard = CreateSlot(bar.transform, 2, "RClick", GuardIcon);
            knockbackSlot = CreateSlot(bar.transform, 3, "Q", "knockback");
            tauntSlot = CreateSlot(bar.transform, 4, "E", TauntIcon);
            throwSlot = CreateSlot(bar.transform, 5, "R", ThrowIcon);
            healthPotion = CreatePotionSlot(bar.transform, 6, "C", "health_potion");
            staminaPotion = CreatePotionSlot(bar.transform, 7, "V", "stamina_potion");
            collectSlot = CreateSlot(bar.transform, 0, "TAB", "collect");

            var hotkeys = caster != null ? caster.HotkeySpells : null;

            for (int i = 0; i < 6; i++)
            {
                SpellSchool school = i < 3 ? SpellSchool.Fire : SpellSchool.Ice;
                int level = i % 3 + 1;
                string icon = (school == SpellSchool.Fire ? "fire_" : "ice_") + level;

                Slot slot = CreateSlot(bar.transform, 8 + i, (i + 1).ToString(), icon);

                SpellDef spell = null;
                if (hotkeys != null && i < hotkeys.Count) spell = hotkeys[i];

                spells.Add(new SpellSlot { School = school, Level = level, Slot = slot, Spell = spell });
            }
        }

        private Slot CreatePotionSlot(Transform parent, int index, string key, string iconName)
        {
            Slot slot = CreateSlot(parent, index, key, iconName);

            var countRt = UIBuilder.CreateRect(slot.Icon.transform,
                new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-4f, -4f), new Vector2(40f, 20f));
            countRt.gameObject.name = "Count";
            var count = UIBuilder.AttachText(countRt, "0", TextAnchor.UpperRight, 16,
                UITheme.Default.text, UIBuilder.GetFont(UITheme.Default));
            count.horizontalOverflow = HorizontalWrapMode.Overflow;
            count.raycastTarget = false;
            slot.Count = count;

            return slot;
        }

        private Slot CreateSlot(Transform parent, int index, string key, string iconName)
        {
            float x = (index - (TotalSlots - 1) * 0.5f) * (SlotSize + SlotGap);

            var bgRt = UIBuilder.CreateRect(parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(x, 0f), new Vector2(SlotSize, SlotSize));
            bgRt.gameObject.name = "Slot_" + key;
            UIBuilder.AttachImage(bgRt, SlotBackground);

            var border = bgRt.gameObject.AddComponent<Outline>();
            border.effectColor = GoldBorder;
            border.effectDistance = new Vector2(2f, 2f);
            border.useGraphicAlpha = false;
            border.enabled = false;

            var iconRt = UIBuilder.CreateRect(bgRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(SlotSize, SlotSize));
            iconRt.gameObject.name = "Icon";
            var icon = UIBuilder.AttachImage(iconRt, Color.white);
            icon.raycastTarget = false;

            Sprite sprite = UIBuilder.LoadTextureSprite(IconFolder + "/" + iconName);
            if (sprite != null)
            {
                sprite.texture.filterMode = FilterMode.Bilinear;
                icon.sprite = sprite;
            }
            else
            {
                icon.color = new Color(0.2f, 0.2f, 0.24f, 1f);
            }

            var recoveryRt = UIBuilder.CreateRect(bgRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(SlotSize, SlotSize));
            recoveryRt.gameObject.name = "Recovery";
            var recovery = UIBuilder.AttachImage(recoveryRt, Color.white);
            recovery.sprite = sprite;
            recovery.type = Image.Type.Filled;
            recovery.fillMethod = Image.FillMethod.Radial360;
            recovery.fillOrigin = (int)Image.Origin360.Top;
            recovery.fillClockwise = true;
            recovery.fillAmount = 0f;
            recovery.raycastTarget = false;
            recovery.enabled = sprite != null;

            var sweepGo = new GameObject("Sweep");
            sweepGo.transform.SetParent(bgRt, false);
            var sweepRt = sweepGo.AddComponent<RectTransform>();
            sweepRt.anchorMin = new Vector2(0.5f, 0.5f);
            sweepRt.anchorMax = new Vector2(0.5f, 0.5f);
            sweepRt.pivot = new Vector2(0.5f, 0f);
            sweepRt.anchoredPosition = Vector2.zero;
            sweepRt.sizeDelta = new Vector2(1f, SlotSize * 0.5f);
            var sweep = sweepGo.AddComponent<Image>();
            sweep.color = SweepColor;
            sweep.raycastTarget = false;
            sweep.enabled = false;

            var keyRt = UIBuilder.CreateRect(bgRt, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(4f, 2f), new Vector2(44f, 16f));
            keyRt.gameObject.name = "Key";
            var keyLabel = UIBuilder.AttachText(keyRt, key, TextAnchor.LowerLeft, 12,
                UITheme.Default.dimText, UIBuilder.GetFont(UITheme.Default));
            keyLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
            keyLabel.raycastTarget = false;

            return new Slot
            {
                Icon = icon,
                Recovery = recovery,
                Sweep = sweep,
                Border = border,
                Sprite = sprite
            };
        }

        private void UpdateSlots()
        {
            SetReady(collectSlot, Color.white);

            UpdateCooldown(attack,
                melee != null ? melee.CooldownRemaining : 0f,
                melee != null ? melee.CooldownDuration : 1f,
                Color.white);

            bool blocking = movement != null && movement.IsBlocking;
            SetReady(guard, blocking ? GuardActive : Color.white);
            guard.Border.enabled = blocking;

            UpdateCooldown(knockbackSlot,
                strikes != null ? strikes.CooldownRemaining : 0f,
                strikes != null ? strikes.CooldownDuration : 1f,
                Color.white);

            UpdateCooldown(tauntSlot,
                taunt != null ? taunt.CooldownRemaining : 0f,
                taunt != null ? taunt.CooldownDuration : 1f,
                Color.white);

            UpdatePotion(healthPotion,
                belt != null ? belt.HealthCooldownRemaining : 0f,
                belt != null ? belt.CooldownDuration : 30f,
                belt != null ? belt.HealthCount : 0);
            UpdatePotion(staminaPotion,
                belt != null ? belt.StaminaCooldownRemaining : 0f,
                belt != null ? belt.CooldownDuration : 30f,
                belt != null ? belt.StaminaCount : 0);

            foreach (SpellSlot s in spells)
            {
                UpdateCooldown(s.Slot,
                    caster != null ? caster.CooldownRemaining(s.School) : 0f,
                    caster != null ? caster.CooldownDuration(s.School) : 0f,
                    SpellSlotColor(s));
            }
        }

        private void UpdatePotion(Slot slot, float remaining, float duration, int count)
        {
            if (slot == null) return;

            if (slot.Count != null) slot.Count.text = count.ToString();

            if (count <= 0)
            {
                SetReady(slot, LockedDim);
                if (slot.Recovery != null)
                {
                    slot.Recovery.fillAmount = 0f;
                    slot.Recovery.enabled = false;
                }
                if (slot.Sweep != null) slot.Sweep.enabled = false;
                return;
            }

            UpdateCooldown(slot, remaining, duration, Color.white);
        }

        private Color SpellSlotColor(SpellSlot s)
        {
            if (spellBook == null) return Color.white;

            if (s.Spell != null)
            {
                if (spellBook.IsOwned(s.Spell)) return Color.white;
                return spellBook.IsUnlocked(s.Spell) ? Dimmed : LockedDim;
            }

            SpellDef best = spellBook.BestOwned(s.School);
            return best != null && s.Level <= best.Level ? Color.white : LockedDim;
        }

        private void UpdateCooldown(Slot slot, float remaining, float duration, Color readyColor)
        {
            if (remaining <= 0f || duration <= 0f)
            {
                SetReady(slot, readyColor);
                return;
            }

            float recovered = Mathf.Clamp01(1f - remaining / duration);

            slot.Icon.color = CooldownDim;

            if (slot.Recovery != null && slot.Sprite != null)
            {
                slot.Recovery.enabled = true;
                slot.Recovery.fillAmount = recovered;
            }

            if (slot.Sweep != null)
            {
                slot.Sweep.enabled = true;
                slot.Sweep.transform.localRotation =
                    Quaternion.Euler(0f, 0f, -360f * recovered);
            }
        }

        private void SetReady(Slot slot, Color baseColor)
        {
            slot.Icon.color = baseColor;
            if (slot.Recovery != null)
            {
                slot.Recovery.fillAmount = 0f;
                slot.Recovery.enabled = false;
            }
            if (slot.Sweep != null) slot.Sweep.enabled = false;
        }
    }
}
