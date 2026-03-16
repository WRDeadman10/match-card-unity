namespace VectorSandboxLab.MemoryGame
{
    public sealed class ScoreManager
    {
        private int comboStreak;

        public int Score { get; private set; }

        public int RegisterMatch()
        {
            var multiplier = comboStreak + 1;
            var points = 100 * multiplier;
            Score += points;
            comboStreak++;
            return points;
        }

        public int RegisterMismatch()
        {
            comboStreak = 0;
            Score -= 10;
            return -10;
        }
    }
}
