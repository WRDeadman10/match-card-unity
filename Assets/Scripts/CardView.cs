using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace VectorSandboxLab.MemoryGame
{
    [RequireComponent(typeof(Button))]
    public sealed class CardView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private GameObject frontFace;
        [SerializeField] private GameObject backFace;
        [SerializeField] private Text frontLabel;
        [SerializeField] private Text backLabel;
        [SerializeField] private float flipDuration = 0.14f;

        private Coroutine flipRoutine;

        public bool IsFaceUp { get; private set; }

        public Button Button => button;

        public void SetFrontLabel(string label)
        {
            if (frontLabel != null)
            {
                frontLabel.text = label;
            }
        }

        public void SetBackLabel(string label)
        {
            if (backLabel != null)
            {
                backLabel.text = label;
            }
        }

        public void SetInteractable(bool value)
        {
            if (button != null)
            {
                button.interactable = value;
            }
        }

        public void ShowFaceImmediate(bool showFront)
        {
            IsFaceUp = showFront;

            if (frontFace != null)
            {
                frontFace.SetActive(showFront);
            }

            if (backFace != null)
            {
                backFace.SetActive(!showFront);
            }
        }

        public void PlayPlaceholderFlip(bool showFront)
        {
            if (!isActiveAndEnabled)
            {
                ShowFaceImmediate(showFront);
                return;
            }

            if (flipRoutine != null)
            {
                StopCoroutine(flipRoutine);
            }

            flipRoutine = StartCoroutine(FlipRoutine(showFront));
        }

        private IEnumerator FlipRoutine(bool showFront)
        {
            IsFaceUp = showFront;

            var initialScale = transform.localScale;
            var halfDuration = Mathf.Max(0.01f, flipDuration * 0.5f);

            yield return AnimateScale(initialScale, new Vector3(0.04f, initialScale.y, initialScale.z), halfDuration);

            if (frontFace != null)
            {
                frontFace.SetActive(showFront);
            }

            if (backFace != null)
            {
                backFace.SetActive(!showFront);
            }

            yield return AnimateScale(transform.localScale, initialScale, halfDuration);
            flipRoutine = null;
        }

        private IEnumerator AnimateScale(Vector3 from, Vector3 to, float duration)
        {
            var elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var progress = Mathf.Clamp01(elapsed / duration);
                transform.localScale = Vector3.LerpUnclamped(from, to, progress);
                yield return null;
            }

            transform.localScale = to;
        }

        private void Reset()
        {
            button = GetComponent<Button>();
            backgroundImage = GetComponent<Image>();
        }
    }
}
