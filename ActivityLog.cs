using System;
using System.Collections.Generic;
using System.Linq;

namespace CyberSecurityChatbotP3
{
    // ============================================================
    //  ActivityLog.cs  —  Records all chatbot actions with timestamps
    //
    //  Categories tracked:
    //    Task      — add, complete, delete
    //    Reminder  — set / cleared
    //    Quiz      — started, answer given, completed
    //    NLP       — any keyword-detected intent
    //    Chat      — general conversation actions
    // ============================================================
    public class ActivityLog
    {
        // ── Single log entry ─────────────────────────────────────
        public class LogEntry
        {
            public DateTime Timestamp { get; set; } = DateTime.Now;
            public string Category { get; set; } = "";   // Task / Reminder / Quiz / NLP / Chat
            public string Message { get; set; } = "";

            // Display format for the UI list
            public string Display =>
                "[" + Timestamp.ToString("HH:mm:ss") + "] " +
                Category.PadRight(8) + " — " + Message;
        }

        // ── Internal list of all entries (no cap — we paginate in the UI)
        private List<LogEntry> entries = new List<LogEntry>();

        // ============================================================
        //  Add — appends a new log entry
        // ============================================================
        public void Add(string category, string message)
        {
            entries.Add(new LogEntry
            {
                Timestamp = DateTime.Now,
                Category = category,
                Message = message
            });
        }

        // ============================================================
        //  GetRecent — returns the last N entries (newest first)
        // ============================================================
        public List<LogEntry> GetRecent(int count = 10)
        {
            return entries
                .AsEnumerable()
                .Reverse()
                .Take(count)
                .ToList();
        }

        // ============================================================
        //  GetAll — returns every entry (newest first)
        // ============================================================
        public List<LogEntry> GetAll()
        {
            return entries
                .AsEnumerable()
                .Reverse()
                .ToList();
        }

        // ============================================================
        //  Count — total number of log entries
        // ============================================================
        public int Count => entries.Count;

        // ============================================================
        //  BuildSummaryText — formats a readable log summary
        // ============================================================
        public string BuildSummaryText(int count = 10)
        {
            var recent = GetRecent(count);
            if (recent.Count == 0)
                return "No activity logged yet. Start chatting, add tasks, or take the quiz!";

            var lines = new System.Text.StringBuilder();
            lines.AppendLine("Here are my last " + recent.Count + " actions:\n");

            int i = 1;
            foreach (var entry in recent)
            {
                lines.AppendLine(i + ". [" + entry.Timestamp.ToString("HH:mm") + "] " +
                                 entry.Category + " — " + entry.Message);
                i++;
            }

            if (entries.Count > count)
                lines.AppendLine("\n(Showing " + count + " of " + entries.Count +
                                 " total. Say 'show full log' to see everything.)");

            return lines.ToString().Trim();
        }
    }
}