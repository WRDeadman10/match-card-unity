using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VectorSandboxLab.MemoryGame
{
    public sealed class BoardManager
    {
        private readonly RectTransform boardArea;
        private readonly GameObject cardPrefab;
        private readonly List<CardView> activeCards = new();
        private readonly RectOffset padding = new(18, 18, 18, 18);
        private readonly Vector2 spacing = new(16f, 16f);

        private GridLayoutGroup gridLayoutGroup;
        private BoardAreaLayoutWatcher layoutWatcher;
        private BoardLayoutDefinition currentLayout;
        private bool isConfigured;

        public BoardManager(RectTransform boardArea, GameObject cardPrefab)
        {
            this.boardArea = boardArea;
            this.cardPrefab = cardPrefab;
        }

        public IReadOnlyList<CardView> ActiveCards => activeCards;

        public void GenerateBoard(BoardLayoutPreset preset)
        {
            currentLayout = BoardLayoutDefinition.FromPreset(preset);
            EnsureGrid();
            ClearExistingCards();
            CreateCardViews();
            RefreshCellSize();
            isConfigured = true;
        }

        private void EnsureGrid()
        {
            gridLayoutGroup = boardArea.GetComponent<GridLayoutGroup>();
            if (gridLayoutGroup == null)
            {
                gridLayoutGroup = boardArea.gameObject.AddComponent<GridLayoutGroup>();
            }

            gridLayoutGroup.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayoutGroup.constraintCount = currentLayout.Columns;
            gridLayoutGroup.spacing = spacing;
            gridLayoutGroup.padding = padding;
            gridLayoutGroup.startAxis = GridLayoutGroup.Axis.Horizontal;
            gridLayoutGroup.startCorner = GridLayoutGroup.Corner.UpperLeft;
            gridLayoutGroup.childAlignment = TextAnchor.MiddleCenter;

            layoutWatcher = boardArea.GetComponent<BoardAreaLayoutWatcher>();
            if (layoutWatcher == null)
            {
                layoutWatcher = boardArea.gameObject.AddComponent<BoardAreaLayoutWatcher>();
            }

            layoutWatcher.DimensionsChanged -= RefreshCellSize;
            layoutWatcher.DimensionsChanged += RefreshCellSize;
        }

        private void ClearExistingCards()
        {
            for (var index = boardArea.childCount - 1; index >= 0; index--)
            {
                var child = boardArea.GetChild(index);
                Object.Destroy(child.gameObject);
            }

            activeCards.Clear();
        }

        private void CreateCardViews()
        {
            var deck = BuildDeck(currentLayout);

            for (var index = 0; index < deck.Count; index++)
            {
                var cardObject = Object.Instantiate(cardPrefab, boardArea);
                cardObject.name = $"Card_{index:00}";

                var cardView = cardObject.GetComponent<CardView>();
                cardView.SetFrontLabel(deck[index].Symbol);
                cardView.SetBackLabel("?");
                cardView.SetInteractable(false);
                cardView.ShowFaceImmediate(false);

                activeCards.Add(cardView);
            }
        }

        private void RefreshCellSize()
        {
            if (!isConfigured && currentLayout.CardCount == 0)
            {
                return;
            }

            var rect = boardArea.rect;
            if (rect.width <= 0f || rect.height <= 0f)
            {
                return;
            }

            var availableWidth = rect.width - padding.left - padding.right - (spacing.x * (currentLayout.Columns - 1));
            var availableHeight = rect.height - padding.top - padding.bottom - (spacing.y * (currentLayout.Rows - 1));
            var widthPerCell = availableWidth / currentLayout.Columns;
            var heightPerCell = availableHeight / currentLayout.Rows;
            var edge = Mathf.Floor(Mathf.Min(widthPerCell, heightPerCell));

            gridLayoutGroup.cellSize = new Vector2(edge, edge);
            LayoutRebuilder.ForceRebuildLayoutImmediate(boardArea);
        }

        private static List<CardDefinition> BuildDeck(BoardLayoutDefinition layout)
        {
            var definitions = new List<CardDefinition>(layout.CardCount);
            var symbols = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

            for (var pairId = 0; pairId < layout.PairCount; pairId++)
            {
                var symbol = pairId < symbols.Length ? symbols[pairId].ToString() : $"#{pairId + 1}";
                definitions.Add(new CardDefinition(definitions.Count, pairId, symbol));
                definitions.Add(new CardDefinition(definitions.Count, pairId, symbol));
            }

            Shuffle(definitions);
            return definitions;
        }

        private static void Shuffle(List<CardDefinition> definitions)
        {
            for (var index = definitions.Count - 1; index > 0; index--)
            {
                var swapIndex = Random.Range(0, index + 1);
                (definitions[index], definitions[swapIndex]) = (definitions[swapIndex], definitions[index]);
            }
        }
    }
}
