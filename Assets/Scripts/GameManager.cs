using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace VectorSandboxLab.MemoryGame
{
    public sealed class GameManager : MonoBehaviour
    {
        [SerializeField] private GameObject gameLayoutPrefab;
        [SerializeField] private GameObject cardPrefab;
        [SerializeField] private BoardLayoutPreset initialLayout = BoardLayoutPreset.FourByFour;

        private readonly List<CardView> flippedCards = new(2);
        private BoardManager boardManager;
        private GameLayoutView layoutView;

        private void Start()
        {
            if (gameLayoutPrefab == null || cardPrefab == null)
            {
                Debug.LogError("GameManager is missing prefab references.");
                return;
            }

            EnsureEventSystem();

            var layoutInstance = Instantiate(gameLayoutPrefab);
            layoutView = layoutInstance.GetComponent<GameLayoutView>();
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
            boardManager.GenerateBoard(initialLayout, OnCardSelected);
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

        private void OnCardSelected(CardView cardView)
        {
            if (cardView == null || cardView.IsFaceUp || flippedCards.Count >= 2)
            {
                return;
            }

            cardView.PlayPlaceholderFlip(true);
            flippedCards.Add(cardView);

            if (layoutView?.StatusText == null)
            {
                return;
            }

            layoutView.StatusText.text = flippedCards.Count == 1
                ? "Pick one more card."
                : "Two cards selected. Matching logic comes next.";
        }
    }
}
