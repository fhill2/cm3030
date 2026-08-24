using UnityEngine;
using UnityEngine.InputSystem;

namespace Art_Equilibrium
{
    public class AE_Door : MonoBehaviour
    {
        bool trig, open;
        public float smooth = 2.0f;
        public float DoorOpenAngle = 87.0f;
        private Quaternion defaultRot;
        private Quaternion openRot;
        private Vector3 defaultLocalPos;
        private Vector3 targetLocalSlidePos;

        [Header("Door Type")]
        public bool isSlidingDoor = false;                  
        public Vector3 slideOffset = new Vector3(1, 0, 0);  

        [Header("GUI Settings")]
        public string openMessage = "Open F";
        public string closeMessage = "Close F";
        [Tooltip("Leave empty to use the game's MedievalSharp font from Resources/UI.")]
        public Font messageFont;
        public int fontSize = 18;
        public Color fontColor = Color.white;
        public Vector2 messagePosition = new Vector2(0.5f, 0.5f);

        private string doorMessage = "";

        [Header("Audio Settings")]
        public AudioClip openSound;
        public AudioClip closeSound;
        private AudioSource audioSource;

        private void Start()
        {
            defaultRot = transform.rotation;
            openRot = Quaternion.Euler(transform.eulerAngles.x, transform.eulerAngles.y + DoorOpenAngle, transform.eulerAngles.z);
            defaultLocalPos = transform.localPosition;
            targetLocalSlidePos = defaultLocalPos + slideOffset;

            audioSource = gameObject.AddComponent<AudioSource>();
        }

        private void Update()
        {
            if (isSlidingDoor)
            {
                Vector3 targetPos = open ? targetLocalSlidePos : defaultLocalPos;
                transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, Time.deltaTime * smooth);
            }
            else
            {
                Quaternion targetRot = open ? openRot : defaultRot;
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * smooth);
            }

            // F, not E: E is the shout/taunt (see PlayerTaunt), and standing in a
            // doorway would otherwise fire both on one press.
            if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame && trig)
            {
                open = !open;
                PlayDoorSound();
            }

            doorMessage = trig ? (open ? closeMessage : openMessage) : "";
        }

        private GUIStyle messageStyle;

        // Same font the rest of the HUD uses (UIBuilder.GameFontPath), loaded by
        // name so a door dropped into a scene matches without anyone wiring it.
        // An explicitly assigned messageFont still wins.
        private const string GameFontPath = "UI/MedievalSharp-Regular";
        private static Font s_gameFont;

        private Font ResolveFont()
        {
            if (messageFont != null) return messageFont;
            if (s_gameFont == null) s_gameFont = Resources.Load<Font>(GameFontPath);
            // Null is fine: GUIStyle falls back to the built-in skin font.
            return s_gameFont;
        }

        private void OnGUI()
        {
            if (string.IsNullOrEmpty(doorMessage)) return;

            if (messageStyle == null)
            {
                messageStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = fontSize,
                    normal = { textColor = fontColor },
                    wordWrap = false,
                    // The skin's label padding eats into the drawable area and
                    // clips the glyphs; the rect below is sized to the text.
                    padding = new RectOffset(0, 0, 0, 0),
                    // Last resort: draw past the rect rather than chop the text
                    // if the measurement is still short.
                    clipping = TextClipping.Overflow
                };
                messageStyle.font = ResolveFont();
            }

            float screenWidth = Screen.width;
            float screenHeight = Screen.height;
            Vector2 labelSize = messageStyle.CalcSize(new GUIContent(doorMessage));

            // CalcSize under-reports height for decorative fonts, whose
            // ascenders and descenders reach past the nominal line box — which
            // is what was slicing the tops and tails off the prompt. Give the
            // rect a full font-size of headroom in both directions.
            labelSize.x += fontSize;
            labelSize.y += fontSize;
            float labelX = screenWidth * messagePosition.x - labelSize.x / 2;
            float labelY = screenHeight * messagePosition.y - labelSize.y / 2;

            GUI.Label(new Rect(labelX, labelY, labelSize.x, labelSize.y), doorMessage, messageStyle);
        }

        private void OnTriggerEnter(Collider coll)
        {
            if (coll.CompareTag("Player"))
            {
                doorMessage = open ? closeMessage : openMessage;
                trig = true;
            }
        }

        private void OnTriggerExit(Collider coll)
        {
            if (coll.CompareTag("Player"))
            {
                doorMessage = "";
                trig = false;
            }
        }

        private void PlayDoorSound()
        {
            if (audioSource != null)
            {
                if (open && openSound != null)
                {
                    audioSource.clip = openSound;
                    audioSource.Play();
                }
                else if (!open && closeSound != null)
                {
                    audioSource.clip = closeSound;
                    audioSource.Play();
                }
            }
        }
    }
}
