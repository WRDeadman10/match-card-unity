using System;

namespace VectorSandboxLab.MemoryGame
{
    [Serializable]
    public sealed class GameSaveData
    {
        public BoardLayoutPreset boardLayout;
        public int score;
        public int[] matchedCardIndices;
        public int[] pairIds;
        public string[] symbols;
    }
}
