using TMPro;
using UnityEngine;

namespace VectorSandboxLab.MemoryGame
{
    public sealed class GameLayoutView : MonoBehaviour
    {
        [SerializeField] private RectTransform boardArea;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI statusText;

        public RectTransform Root => (RectTransform)transform;
        public RectTransform BoardArea => boardArea;
        public TextMeshProUGUI TitleText => titleText;
        public TextMeshProUGUI ScoreText => scoreText;
        public TextMeshProUGUI StatusText => statusText;
    }
}
