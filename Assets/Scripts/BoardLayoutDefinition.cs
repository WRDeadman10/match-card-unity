using UnityEngine;

namespace VectorSandboxLab.MemoryGame
{
    public readonly struct BoardLayoutDefinition
    {
        public BoardLayoutDefinition(int rows, int columns)
        {
            Rows = rows;
            Columns = columns;
        }

        public int Rows { get; }
        public int Columns { get; }
        public int CardCount => Rows * Columns;
        public int PairCount => CardCount / 2;

        public static BoardLayoutDefinition FromPreset(BoardLayoutPreset preset)
        {
            return preset switch
            {
                BoardLayoutPreset.TwoByTwo => new BoardLayoutDefinition(2, 2),
                BoardLayoutPreset.TwoByThree => new BoardLayoutDefinition(2, 3),
                BoardLayoutPreset.FourByFour => new BoardLayoutDefinition(4, 4),
                BoardLayoutPreset.FiveBySix => new BoardLayoutDefinition(5, 6),
                _ => new BoardLayoutDefinition(4, 4)
            };
        }

        public override string ToString()
        {
            return $"{Rows}x{Columns}";
        }
    }
}
