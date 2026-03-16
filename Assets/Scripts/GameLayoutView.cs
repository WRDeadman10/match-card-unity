using UnityEngine;
using UnityEngine.UI;

namespace VectorSandboxLab.MemoryGame
{
    public sealed class GameLayoutView : MonoBehaviour
    {
        [SerializeField] private RectTransform boardArea;
        [SerializeField] private Text titleText;
        [SerializeField] private Text scoreText;
        [SerializeField] private Text statusText;

        public RectTransform BoardArea => boardArea;
        public Text TitleText => titleText;
        public Text ScoreText => scoreText;
        public Text StatusText => statusText;
    }
}
