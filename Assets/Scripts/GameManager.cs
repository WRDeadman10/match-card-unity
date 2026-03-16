using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VectorSandboxLab.MemoryGame
{
    public sealed class GameManager : MonoBehaviour
    {
        [SerializeField] private GameObject gameLayoutPrefab;
        [SerializeField] private GameObject cardPrefab;
        [SerializeField] private BoardLayoutPreset initialLayout = BoardLayoutPreset.FourByFour;

        private readonly List<CardView> flippedCards = new(2);
        private readonly Queue<CardView> queuedSelections = new();
        private readonly ScoreManager scoreManager = new();
        private SaveSystem saveSystem;
        private AudioManager audioManager;
        private BoardManager boardManager;
        private GameLayoutView layoutView;
        private Coroutine resolveRoutine;
        private BoardLayoutPreset activeLayout;
        private bool hasCompletedGame;
        private bool gameActive;

        private GameObject menuOverlay;
        private TextMeshProUGUI menuSubtitleText;
        private Button continueButton;
        private TextMeshProUGUI continueButtonLabel;
        private static readonly BoardLayoutPreset[] MenuLayouts =
        {
            BoardLayoutPreset.TwoByTwo,
            BoardLayoutPreset.TwoByThree,
            BoardLayoutPreset.FourByFour,
            BoardLayoutPreset.FiveBySix
        };

        private void Awake()
        {
            saveSystem = new SaveSystem(Application.persistentDataPath);
        }

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

            boardManager = new BoardManager(layoutView.BoardArea, cardPrefab);
            BuildMenuUi();
            ShowMenu("Choose a board layout or continue your saved run.");
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
            if (!gameActive || cardView == null || cardView.IsFaceUp || cardView.IsMatched)
            {
                return;
            }

            if (resolveRoutine != null || flippedCards.Count >= 2)
            {
                EnqueueSelection(cardView);
                return;
            }

            SelectCard(cardView);
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

                if (!hasCompletedGame && IsBoardComplete())
                {
                    hasCompletedGame = true;
                    gameActive = false;
                    audioManager?.PlayGameOver();
                    saveSystem.DeleteSave();
                    UpdateContinueAvailability();

                    if (layoutView?.StatusText != null)
                    {
                        layoutView.StatusText.text = $"Game complete. Final score: {scoreManager.Score}";
                    }

                    ShowMenu("Round complete. Start a new game or pick another layout.");
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
            ProcessQueuedSelections();
        }

        private void StartNewGame(BoardLayoutPreset layout)
        {
            HideMenu();
            ResetRuntimeState();
            activeLayout = layout;
            scoreManager.Reset();
            boardManager.GenerateBoard(layout, OnCardSelected);
            gameActive = true;
            RefreshScore();
            SaveProgress();

            if (layoutView?.StatusText != null)
            {
                layoutView.StatusText.text = $"Layout: {BoardLayoutDefinition.FromPreset(layout)}";
            }
        }

        private void ContinueSavedGame()
        {
            if (!saveSystem.TryLoad(out var saveData) || saveData?.pairIds == null || saveData.pairIds.Length == 0)
            {
                ShowMenu("No saved game found. Start a new game instead.");
                return;
            }

            HideMenu();
            ResetRuntimeState();
            activeLayout = saveData.boardLayout;
            scoreManager.Restore(saveData.score);
            boardManager.GenerateBoard(activeLayout, OnCardSelected, RestoreDeck(saveData));
            RestoreMatchedCards(saveData);
            gameActive = true;
            RefreshScore();

            if (layoutView?.StatusText != null)
            {
                layoutView.StatusText.text = "Progress restored.";
            }
        }

        private void ReturnToMenu()
        {
            gameActive = false;
            ShowMenu(saveSystem.HasSave()
                ? "Saved progress is available. Continue or start a fresh run."
                : "Start a new game by choosing a layout.");
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
            if (!gameActive || hasCompletedGame || boardManager == null)
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

            UpdateContinueAvailability();
        }

        private bool IsBoardComplete()
        {
            for (var index = 0; index < boardManager.ActiveCards.Count; index++)
            {
                if (!boardManager.ActiveCards[index].IsMatched)
                {
                    return false;
                }
            }

            return boardManager.ActiveCards.Count > 0;
        }

        private void SelectCard(CardView cardView)
        {
            cardView.PlayPlaceholderFlip(true);
            audioManager?.PlayFlip();
            flippedCards.Add(cardView);

            if (layoutView?.StatusText != null)
            {
                layoutView.StatusText.text = flippedCards.Count == 1
                    ? "Pick one more card."
                    : "Checking cards...";
            }

            if (flippedCards.Count == 2)
            {
                resolveRoutine = StartCoroutine(ResolveSelection());
            }
        }

        private void EnqueueSelection(CardView cardView)
        {
            foreach (var queuedCard in queuedSelections)
            {
                if (queuedCard == cardView)
                {
                    return;
                }
            }

            queuedSelections.Enqueue(cardView);
        }

        private void ProcessQueuedSelections()
        {
            while (queuedSelections.Count > 0 && flippedCards.Count < 2 && resolveRoutine == null)
            {
                var nextCard = queuedSelections.Dequeue();
                if (nextCard == null || nextCard.IsFaceUp || nextCard.IsMatched)
                {
                    continue;
                }

                SelectCard(nextCard);
            }
        }

        private void ResetRuntimeState()
        {
            if (resolveRoutine != null)
            {
                StopCoroutine(resolveRoutine);
                resolveRoutine = null;
            }

            flippedCards.Clear();
            queuedSelections.Clear();
            hasCompletedGame = false;
        }

        private void BuildMenuUi()
        {
            menuOverlay = CreatePanel("MenuOverlay", layoutView.Root, new Color(0.03f, 0.05f, 0.08f, 0.92f));
            var overlayRect = (RectTransform)menuOverlay.transform;
            StretchToParent(overlayRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var card = CreatePanel("MenuCard", overlayRect, new Color(0.09f, 0.12f, 0.17f, 0.96f));
            var cardRect = (RectTransform)card.transform;
            cardRect.anchorMin = new Vector2(0.18f, 0.14f);
            cardRect.anchorMax = new Vector2(0.82f, 0.86f);
            cardRect.offsetMin = Vector2.zero;
            cardRect.offsetMax = Vector2.zero;

            var header = CreateText("MenuTitle", cardRect, "Memory Match", 54, FontStyles.Bold, new Color(0.98f, 0.95f, 0.88f));
            var headerRect = (RectTransform)header.transform;
            headerRect.anchorMin = new Vector2(0.08f, 0.82f);
            headerRect.anchorMax = new Vector2(0.92f, 0.95f);
            headerRect.offsetMin = Vector2.zero;
            headerRect.offsetMax = Vector2.zero;

            menuSubtitleText = CreateText("MenuSubtitle", cardRect, string.Empty, 24, FontStyles.Normal, new Color(0.8f, 0.84f, 0.9f));
            var subtitleRect = (RectTransform)menuSubtitleText.transform;
            subtitleRect.anchorMin = new Vector2(0.08f, 0.68f);
            subtitleRect.anchorMax = new Vector2(0.92f, 0.81f);
            subtitleRect.offsetMin = Vector2.zero;
            subtitleRect.offsetMax = Vector2.zero;

            continueButton = CreateButton(cardRect, "ContinueButton", "Continue", new Vector2(0.08f, 0.56f), new Vector2(0.92f, 0.65f), out continueButtonLabel);
            continueButton.onClick.AddListener(ContinueSavedGame);

            var menuButton = CreateButton(layoutView.Root, "OpenMenuButton", "Menu", new Vector2(0.78f, 0.9f), new Vector2(0.94f, 0.96f), out _);
            menuButton.onClick.AddListener(ReturnToMenu);

            var layoutLabel = CreateText("LayoutLabel", cardRect, "New Game", 28, FontStyles.Bold, new Color(0.98f, 0.95f, 0.88f));
            var layoutLabelRect = (RectTransform)layoutLabel.transform;
            layoutLabelRect.anchorMin = new Vector2(0.08f, 0.47f);
            layoutLabelRect.anchorMax = new Vector2(0.92f, 0.54f);
            layoutLabelRect.offsetMin = Vector2.zero;
            layoutLabelRect.offsetMax = Vector2.zero;

            for (var index = 0; index < MenuLayouts.Length; index++)
            {
                var row = index / 2;
                var column = index % 2;
                var min = new Vector2(0.08f + (column * 0.43f), 0.26f - (row * 0.13f));
                var max = new Vector2(0.49f + (column * 0.43f), 0.36f - (row * 0.13f));
                var preset = MenuLayouts[index];
                var label = BoardLayoutDefinition.FromPreset(preset).ToString();
                var button = CreateButton(cardRect, $"Layout_{label}", label, min, max, out _);
                button.onClick.AddListener(() => StartNewGame(preset));
            }

            var clearButton = CreateButton(cardRect, "ClearSaveButton", "Clear Save", new Vector2(0.08f, 0.05f), new Vector2(0.45f, 0.14f), out _);
            clearButton.onClick.AddListener(ClearSavedProgress);

            var quitButton = CreateButton(cardRect, "QuitButton", "Quit", new Vector2(0.55f, 0.05f), new Vector2(0.92f, 0.14f), out _);
            quitButton.onClick.AddListener(QuitGame);

            UpdateContinueAvailability();
        }

        private void ShowMenu(string subtitle)
        {
            if (menuOverlay == null)
            {
                return;
            }

            menuOverlay.SetActive(true);
            if (menuSubtitleText != null)
            {
                menuSubtitleText.text = subtitle;
            }

            if (layoutView?.StatusText != null && !gameActive)
            {
                layoutView.StatusText.text = subtitle;
            }

            UpdateContinueAvailability();
        }

        private void HideMenu()
        {
            if (menuOverlay != null)
            {
                menuOverlay.SetActive(false);
            }
        }

        private void UpdateContinueAvailability()
        {
            var canContinue = saveSystem != null && saveSystem.HasSave();
            if (continueButton != null)
            {
                continueButton.interactable = canContinue;
            }

            if (continueButtonLabel != null)
            {
                continueButtonLabel.text = canContinue ? "Continue" : "Continue (No Save)";
            }
        }

        private void ClearSavedProgress()
        {
            saveSystem.DeleteSave();
            UpdateContinueAvailability();
            ShowMenu("Saved progress cleared. Pick a layout to start fresh.");
        }

        private void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private static GameObject CreatePanel(string name, RectTransform parent, Color color)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rect = (RectTransform)panel.transform;
            rect.SetParent(parent, false);

            var image = panel.GetComponent<Image>();
            image.color = color;
            return panel;
        }

        private static TextMeshProUGUI CreateText(string name, RectTransform parent, string value, int fontSize, FontStyles fontStyle, Color color)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            var rect = (RectTransform)textObject.transform;
            rect.SetParent(parent, false);

            var text = textObject.GetComponent<TextMeshProUGUI>();
            text.text = value;
            text.font = TMP_Settings.defaultFontAsset;
            text.fontSharedMaterial = TMP_Settings.defaultFontAsset != null ? TMP_Settings.defaultFontAsset.material : null;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = TextAlignmentOptions.Center;
            text.color = color;
            text.enableAutoSizing = true;
            text.fontSizeMin = 14;
            text.fontSizeMax = fontSize;
            text.raycastTarget = false;
            return text;
        }

        private static Button CreateButton(RectTransform parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax, out TextMeshProUGUI labelText)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            var rect = (RectTransform)buttonObject.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.21f, 0.32f, 0.46f, 1f);

            var button = buttonObject.GetComponent<Button>();
            var colors = button.colors;
            colors.normalColor = image.color;
            colors.highlightedColor = new Color(0.29f, 0.41f, 0.57f, 1f);
            colors.pressedColor = new Color(0.16f, 0.24f, 0.36f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.18f, 0.2f, 0.24f, 0.7f);
            button.colors = colors;

            labelText = CreateText($"{name}_Label", rect, label, 26, FontStyles.Bold, new Color(0.96f, 0.96f, 0.96f, 1f));
            var labelRect = (RectTransform)labelText.transform;
            StretchToParent(labelRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return button;
        }

        private static void StretchToParent(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                SaveProgress();
            }
        }

        private void OnApplicationQuit()
        {
            SaveProgress();
        }
    }
}
