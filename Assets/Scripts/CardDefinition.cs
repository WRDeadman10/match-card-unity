namespace VectorSandboxLab.MemoryGame
{
    public readonly struct CardDefinition
    {
        public CardDefinition(int index, int pairId, string symbol)
        {
            Index = index;
            PairId = pairId;
            Symbol = symbol;
        }

        public int Index { get; }
        public int PairId { get; }
        public string Symbol { get; }
    }
}
