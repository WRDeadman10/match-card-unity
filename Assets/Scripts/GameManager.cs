using UnityEngine;
using UnityEngine.EventSystems;

namespace VectorSandboxLab.MemoryGame
{
    public sealed class GameManager : MonoBehaviour
    {
        [SerializeField] private GameObject gameLayoutPrefab;
        [SerializeField] private GameObject cardPrefab;
        [SerializeField] private BoardLayoutPreset initialLayout = BoardLayoutPreset.FourByFour;

        private BoardManager boardManager;

        private void Start()
        {
            if (gameLayoutPrefab == null || cardPrefab == null)
            {
                Debug.LogError("GameManager is missing prefab references.");
                return;
            }

            EnsureEventSystem();

            var layoutInstance = Instantiate(gameLayoutPrefab);
            var layoutView = layoutInstance.GetComponent<GameLayoutView>();
            if (layoutView == null)
            {
                Debug.LogError("Game layout prefab is missing GameLayoutView.");
                return;
            }

            if (layoutView.ScoreText != null)
            {
                layoutView.ScoreText.text = "Score: 0";
            }

            if (layoutView.StatusText != null)
            {
                layoutView.StatusText.text = $"Layout: {BoardLayoutDefinition.FromPreset(initialLayout)}";
            }

            boardManager = new BoardManager(layoutView.BoardArea, cardPrefab);
            boardManager.GenerateBoard(initialLayout);
        }

        private static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            var eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();
        }
    }
}
