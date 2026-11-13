using System;
using System.Collections.Generic;

namespace Uptime.Connections
{
    // Rules and timing are independent of the authored window and of simulation time.
    public sealed class CommandRaceSession
    {
        public sealed class Competitor
        {
            private readonly string[] commands;
            public int Line { get; private set; }
            public string Draft { get; private set; } = string.Empty;
            public int ErrorStartIndex { get; private set; } = -1;
            public int Mistakes { get; private set; }
            public int Corrections { get; private set; }
            public bool Complete => Line >= commands.Length;
            public IReadOnlyList<string> Commands => commands;
            public int TotalCharacters { get; }
            public int CorrectCharacters
            {
                get
                {
                    int count = 0;
                    for (int i = 0; i < Line; i++) count += commands[i].Length;
                    if (!Complete)
                        count += ErrorStartIndex >= 0 ? ErrorStartIndex : Draft.Length;
                    return count;
                }
            }
            public float Progress => TotalCharacters == 0 ? 1f :
                (float)CorrectCharacters / TotalCharacters;

            internal Competitor(string[] source)
            {
                commands = (string[])source.Clone();
                foreach (string command in commands) TotalCharacters += command.Length;
            }

            public bool Type(char character, bool allowErrorRun = false)
            {
                if (Complete || Draft.Length >= commands[Line].Length) return false;
                if (ErrorStartIndex >= 0 && !allowErrorRun) return false;
                bool correct = ErrorStartIndex < 0 &&
                    character == commands[Line][Draft.Length];
                if (!correct)
                {
                    if (ErrorStartIndex < 0) ErrorStartIndex = Draft.Length;
                    Mistakes++;
                }
                Draft += character;
                if (correct && Draft == commands[Line])
                {
                    Line++;
                    Draft = string.Empty;
                    ErrorStartIndex = -1;
                }
                return correct;
            }

            public bool Backspace()
            {
                if (Complete || Draft.Length == 0) return false;
                if (ErrorStartIndex >= 0) Corrections++;
                Draft = Draft.Substring(0, Draft.Length - 1);
                if (Draft.Length <= ErrorStartIndex) ErrorStartIndex = -1;
                return true;
            }

            public char NextExpected => Complete || Draft.Length >= commands[Line].Length ?
                '\0' : commands[Line][Draft.Length];
        }

        private readonly Random random;
        private readonly float characterSeconds;
        private readonly float variance;
        private readonly float mistakeChance;
        private readonly float hesitationChance;
        private float nextActionAt;
        private bool correcting;

        public Competitor Player { get; }
        public Competitor Hacker { get; }
        public float ElapsedSeconds { get; private set; }
        public float? PlayerCompletedAt { get; private set; }
        public float? HackerCompletedAt { get; private set; }
        public bool? PlayerWon { get; private set; }

        public CommandRaceSession(string[] playerCommands, string[] hackerCommands,
            int seed, float characterSeconds, float variance, float mistakeChance,
            float hesitationChance)
        {
            if (playerCommands == null || hackerCommands == null ||
                playerCommands.Length == 0 || hackerCommands.Length == 0)
                throw new ArgumentException("Command Race needs two nonempty sequences.");
            foreach (string line in playerCommands)
                if (string.IsNullOrEmpty(line)) throw new ArgumentException("Empty player command.");
            foreach (string line in hackerCommands)
                if (string.IsNullOrEmpty(line)) throw new ArgumentException("Empty hacker command.");
            Player = new Competitor(playerCommands);
            Hacker = new Competitor(hackerCommands);
            random = new Random(seed);
            this.characterSeconds = Math.Max(.01f, characterSeconds);
            this.variance = Math.Max(0f, Math.Min(.9f, variance));
            this.mistakeChance = Math.Max(0f, Math.Min(1f, mistakeChance));
            this.hesitationChance = Math.Max(0f, Math.Min(1f, hesitationChance));
            nextActionAt = Delay();
        }

        public void TypePlayer(char character)
        {
            if (PlayerWon.HasValue || character < ' ' || character > '~') return;
            Player.Type(character, allowErrorRun: true);
            if (Player.Complete)
            {
                PlayerCompletedAt = ElapsedSeconds;
                PlayerWon = true;
            }
        }

        public void BackspacePlayer()
        {
            if (!PlayerWon.HasValue) Player.Backspace();
        }

        public void Advance(float realSeconds)
        {
            if (PlayerWon.HasValue || realSeconds <= 0f) return;
            ElapsedSeconds += realSeconds;
            while (nextActionAt <= ElapsedSeconds && !PlayerWon.HasValue)
            {
                if (correcting)
                {
                    Hacker.Backspace();
                    correcting = false;
                }
                else if (Hacker.Draft.Length > 0 &&
                    Hacker.Draft[^1] != Hacker.Commands[Hacker.Line][Hacker.Draft.Length - 1])
                {
                    Hacker.Backspace();
                }
                else if (random.NextDouble() < mistakeChance)
                {
                    char expected = Hacker.NextExpected;
                    char wrong = expected == 'x' ? 'z' : 'x';
                    Hacker.Type(wrong);
                    correcting = true;
                }
                else Hacker.Type(Hacker.NextExpected);

                if (Hacker.Complete)
                {
                    HackerCompletedAt = nextActionAt;
                    PlayerWon = false;
                }
                nextActionAt += Delay();
            }
        }

        private float Delay()
        {
            // Pauses and bursts come from the seeded opponent stream, never player progress.
            float factor = 1f + ((float)random.NextDouble() * 2f - 1f) * variance;
            if (random.NextDouble() < hesitationChance) factor *= 2.5f;
            else if (random.NextDouble() < .16f) factor *= .55f;
            return Math.Max(.01f, characterSeconds * factor);
        }
    }
}
