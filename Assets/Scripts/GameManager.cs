using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Collections;

namespace VectorSandboxLab.MemoryGame
{
    public sealed class GameManager : MonoBehaviour
    {
        [SerializeField] private GameObject gameLayoutPrefab;
        [SerializeField] private GameObject cardPrefab;
        [SerializeField] private BoardLayoutPreset initialLayout = BoardLayoutPreset.FourByFour;

        private readonly List<CardView> flippedCards = new(2);
        private readonly ScoreManager scoreManager = new();
        private readonly SaveSystem saveSystem = new();
        private AudioManager audioManager;
        private BoardManager boardManager;
        private GameLayoutView layoutView;
        private Coroutine resolveRoutine;
        private BoardLayoutPreset activeLayout;

        private void Start()
        {
            if (gameLayoutPrefab == null || cardPrefab == null)
            {
                Debug.LogError("GameManager is missing prefab references.");
                return;
            }

            EnsureEventSystem();
            audioManager = new AudioManager(gameObject);

            var layoutInstance = Instantiate(gameLayoutPrefab);
            layoutView = layoutInstance.GetComponent<GameLayoutView>();
            if (layoutView == null)
            {
                Debug.LogError("Game layout prefab is missing GameLayoutView.");
                return;
            }

            if (layoutView.ScoreText != null)
            {
                layoutView.ScoreText.text = $"Score: {scoreManager.Score}";
            }

            boardManager = new BoardManager(layoutView.BoardArea, cardPrefab);

            var layoutToLoad = initialLayout;
            GameSaveData loadedSave = null;

            if (saveSystem.TryLoad(out var saveData) && saveData?.pairIds != null && saveData.pairIds.Length > 0)
            {
                loadedSave = saveData;
                layoutToLoad = saveData.boardLayout;
                scoreManager.Restore(saveData.score);
                boardManager.GenerateBoard(layoutToLoad, OnCardSelected, RestoreDeck(saveData));
                RestoreMatchedCards(saveData);
            }
            else
            {
                boardManager.GenerateBoard(layoutToLoad, OnCardSelected);
            }

            activeLayout = layoutToLoad;
            RefreshScore();

            if (layoutView.StatusText != null)
            {
                layoutView.StatusText.text = loadedSave == null
                    ? $"Layout: {BoardLayoutDefinition.FromPreset(layoutToLoad)}"
                    : "Progress restored.";
            }
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
            audioManager?.PlayFlip();
            flippedCards.Add(cardView);

            if (layoutView?.StatusText == null)
            {
                return;
            }

            layoutView.StatusText.text = flippedCards.Count == 1
                ? "Pick one more card."
                : "Checking cards...";

            if (flippedCards.Count == 2)
            {
                resolveRoutine = StartCoroutine(ResolveSelection());
            }
        }

        private IEnumerator ResolveSelection()
        {
            yield return new WaitForSeconds(0.2f);

            var firstCard = flippedCards[0];
            var secondCard = flippedCards[1];
            var isMatch = firstCard.PairId == secondCard.PairId;

            if (isMatch)
            {
                firstCard.SetMatched(true);
                secondCard.SetMatched(true);
                var gainedPoints = scoreManager.RegisterMatch();
                audioManager?.PlayMatch();
                RefreshScore();

                if (layoutView?.StatusText != null)
                {
                    layoutView.StatusText.text = $"Match found: {firstCard.Symbol} (+{gainedPoints})";
                }
            }
            else
            {
                var scoreDelta = scoreManager.RegisterMismatch();
                audioManager?.PlayMismatch();
                RefreshScore();
                yield return new WaitForSeconds(0.5f);
                firstCard.PlayPlaceholderFlip(false);
                secondCard.PlayPlaceholderFlip(false);

                if (layoutView?.StatusText != null)
                {
                    layoutView.StatusText.text = $"Not a match. {scoreDelta}";
                }
            }

            flippedCards.Clear();
            SaveProgress();
            resolveRoutine = null;
        }

        private void RefreshScore()
        {
            if (layoutView?.ScoreText != null)
            {
                layoutView.ScoreText.text = $"Score: {scoreManager.Score}";
            }
        }

        private void RestoreMatchedCards(GameSaveData saveData)
        {
            if (saveData?.matchedCardIndices == null)
            {
                return;
            }

            foreach (var index in saveData.matchedCardIndices)
            {
                if (index < 0 || index >= boardManager.ActiveCards.Count)
                {
                    continue;
                }

                var card = boardManager.ActiveCards[index];
                card.ShowFaceImmediate(true);
                card.SetMatched(true);
            }
        }

        private static CardDefinition[] RestoreDeck(GameSaveData saveData)
        {
            var restoredDeck = new CardDefinition[saveData.pairIds.Length];

            for (var index = 0; index < restoredDeck.Length; index++)
            {
                var symbol = saveData.symbols != null && index < saveData.symbols.Length
                    ? saveData.symbols[index]
                    : "?";
                restoredDeck[index] = new CardDefinition(index, saveData.pairIds[index], symbol);
            }

            return restoredDeck;
        }

        private void SaveProgress()
        {
            if (boardManager == null)
            {
                return;
            }

            var matched = new List<int>();
            for (var index = 0; index < boardManager.ActiveCards.Count; index++)
            {
                if (boardManager.ActiveCards[index].IsMatched)
                {
                    matched.Add(index);
                }
            }

            var deck = boardManager.CurrentDeck;
            var pairIds = new int[deck.Count];
            var symbols = new string[deck.Count];

            for (var index = 0; index < deck.Count; index++)
            {
                pairIds[index] = deck[index].PairId;
                symbols[index] = deck[index].Symbol;
            }

            saveSystem.Save(new GameSaveData
            {
                boardLayout = activeLayout,
                score = scoreManager.Score,
                matchedCardIndices = matched.ToArray(),
                pairIds = pairIds,
                symbols = symbols
            });
        }
    }
}
