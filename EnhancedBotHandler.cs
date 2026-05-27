using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CyberBot_PART_2
{
    // ══════════════════════════════════════════════════════════════════════
    //  Conversation history entry
    // ══════════════════════════════════════════════════════════════════════
    public class ConversationEntry
    {
        public string Role { get; set; }   // "User" or "Bot"
        public string Message { get; set; }
        public DateTime Time { get; set; }
    }

    // ══════════════════════════════════════════════════════════════════════
    //  Quiz question model
    // ══════════════════════════════════════════════════════════════════════
    public class QuizQuestion
    {
        public string Question { get; set; }
        public string[] Options { get; set; }
        public int CorrectIndex { get; set; }
        public string Explanation { get; set; }
    }

    // ══════════════════════════════════════════════════════════════════════
    //  Sentiment enum
    // ══════════════════════════════════════════════════════════════════════
    public enum Sentiment { Neutral, Worried, Frustrated, Curious }

    // ══════════════════════════════════════════════════════════════════════
    // Adapter to make CyberBot work with GUI
    // with all Part 2 features
    // ══════════════════════════════════════════════════════════════════════
}