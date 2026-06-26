using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace CyberSecurityChatbotP3
{
    // ============================================================
    //  NlpProcessor.cs  —  Simulated NLP using keyword + pattern matching
    //
    //  Detects user INTENT from natural language so the chatbot
    //  can respond even when commands are phrased differently.
    //
    //  Intents:
    //    AddTask        — user wants to add a cybersecurity task
    //    SetReminder    — user wants a reminder for an existing/new task
    //    ViewTasks      — user wants to see their task list
    //    DeleteTask     — user wants to remove a task
    //    CompleteTask   — user wants to mark a task done
    //    StartQuiz      — user wants to play the quiz
    //    ShowLog        — user wants the activity log
    //    ShowMemory     — user wants memory recall
    //    GeneralTopic   — maps to a cybersecurity keyword
    //    Unknown        — couldn't determine intent
    // ============================================================
    public class NlpProcessor
    {
        // ── Detected intent result ───────────────────────────────
        public class NlpResult
        {
            public string Intent { get; set; } = "Unknown";
            public string ExtractedText { get; set; } = ""; // e.g. the task title
            public int? DaysFromNow { get; set; } = null; // parsed reminder days
            public string MatchedTopic { get; set; } = ""; // matched cybersecurity keyword
        }

        // ── Intent keyword groups ────────────────────────────────

        // "Add task" synonyms
        private static readonly string[] AddTaskKeywords =
        {
            "add task", "create task", "new task", "add a task", "make a task",
            "i need to", "i want to", "i should", "set a task", "log a task",
            "add reminder", "set reminder", "remind me to", "remind me about",
            "can you remind me", "don't let me forget", "remember to"
        };

        // "View tasks" synonyms
        private static readonly string[] ViewTaskKeywords =
        {
            "show tasks", "view tasks", "list tasks", "my tasks", "what tasks",
            "show my tasks", "display tasks", "all tasks", "pending tasks",
            "what do i need to do", "what are my tasks"
        };

        // "Delete task" synonyms
        private static readonly string[] DeleteTaskKeywords =
        {
            "delete task", "remove task", "cancel task", "get rid of task",
            "delete the task", "remove the task"
        };

        // "Complete task" synonyms
        private static readonly string[] CompleteTaskKeywords =
        {
            "complete task", "mark task", "done task", "finish task",
            "mark as done", "mark as complete", "i finished", "i've done",
            "task done", "task complete", "completed the task"
        };

        // "Quiz" synonyms
        private static readonly string[] QuizKeywords =
        {
            "start quiz", "play quiz", "take quiz", "begin quiz", "quiz me",
            "test me", "test my knowledge", "cybersecurity quiz", "start game",
            "play the game", "i want a quiz", "quiz"
        };

        // "Show log" synonyms
        private static readonly string[] LogKeywords =
        {
            "show log", "activity log", "show activity", "what have you done",
            "show history", "recent actions", "what did you do", "show full log",
            "view log", "log history"
        };

        // "Memory recall" synonyms
        private static readonly string[] MemoryKeywords =
        {
            "what do you remember", "what do you know about me",
            "what have i told you", "show memory", "recall", "what i said"
        };

        // ── Cybersecurity topic keyword map ──────────────────────
        // Maps NLP-detected words to chatbot topic keywords
        private static readonly Dictionary<string, string> TopicKeywordMap =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "password",           "password"          },
                { "passwords",          "password"          },
                { "phishing",           "phishing"          },
                { "privacy",            "privacy"           },
                { "malware",            "malware"           },
                { "virus",              "virus"             },
                { "ransomware",         "ransomware"        },
                { "encryption",         "encryption"        },
                { "firewall",           "firewall"          },
                { "2fa",                "2fa"               },
                { "two factor",         "2fa"               },
                { "two-factor",         "2fa"               },
                { "wifi",               "wifi"              },
                { "wi-fi",              "wifi"              },
                { "update",             "update"            },
                { "updates",            "update"            },
                { "scam",               "scam"              },
                { "safe browsing",      "safe browsing"     },
                { "vpn",                "vpn"               },
                { "backup",             "backup"            },
                { "backups",            "backup"            },
                { "antivirus",          "antivirus"         },
                { "hacker",             "hacker"            },
                { "hackers",            "hacker"            },
                { "social engineering", "social engineering"},
                { "identity theft",     "identity theft"    },
                { "dark web",           "dark web"          }
            };

        // ============================================================
        //  Analyse — main entry point; returns an NlpResult
        // ============================================================
        public NlpResult Analyse(string input)
        {
            string lower = input.ToLower().Trim();

            // ── Check intents in priority order ──────────────────

            if (MatchesAny(lower, QuizKeywords))
                return new NlpResult { Intent = "StartQuiz" };

            if (MatchesAny(lower, LogKeywords))
                return new NlpResult { Intent = "ShowLog" };

            if (MatchesAny(lower, MemoryKeywords))
                return new NlpResult { Intent = "ShowMemory" };

            if (MatchesAny(lower, CompleteTaskKeywords))
                return new NlpResult { Intent = "CompleteTask" };

            if (MatchesAny(lower, DeleteTaskKeywords))
                return new NlpResult { Intent = "DeleteTask" };

            if (MatchesAny(lower, ViewTaskKeywords))
                return new NlpResult { Intent = "ViewTasks" };

            // ── Add task / reminder — try to extract task title ──
            if (MatchesAny(lower, AddTaskKeywords))
            {
                string extracted = ExtractTaskTitle(lower, input);
                int? days = ExtractDays(lower);

                // If the phrase contains a reminder-specific keyword, treat as SetReminder
                bool isReminder = lower.Contains("remind") || lower.Contains("reminder");

                return new NlpResult
                {
                    Intent = isReminder ? "SetReminder" : "AddTask",
                    ExtractedText = extracted,
                    DaysFromNow = days
                };
            }

            // ── Check for cybersecurity topic keywords ───────────
            string matched = MatchTopic(lower);
            if (matched != "")
                return new NlpResult { Intent = "GeneralTopic", MatchedTopic = matched };

            // ── Couldn't determine intent ────────────────────────
            return new NlpResult { Intent = "Unknown" };
        }

        // ============================================================
        //  ExtractTaskTitle — strips trigger phrases to get the title
        // ============================================================
        private string ExtractTaskTitle(string lower, string original)
        {
            // Ordered list of prefixes to strip (longest first to avoid partial matches)
            string[] prefixes = {
                "add a task to", "add task to", "create a task to", "create task to",
                "add a task called", "add task called", "new task:", "new task to",
                "remind me to", "remind me about", "can you remind me to",
                "set a reminder to", "set reminder to", "don't let me forget to",
                "remember to", "i need to", "i want to", "i should",
                "add a task", "add task", "create a task", "create task",
                "set a task to", "log a task to"
            };

            foreach (string prefix in prefixes)
            {
                if (lower.StartsWith(prefix))
                {
                    // Return the original-cased remainder, trimmed
                    int start = prefix.Length;
                    if (start < original.Length)
                        return original.Substring(start).Trim(' ', '.', '!', '?');
                    return "";
                }
            }

            // Fallback: try to find the phrase inline
            foreach (string prefix in prefixes)
            {
                int idx = lower.IndexOf(prefix);
                if (idx >= 0)
                {
                    int start = idx + prefix.Length;
                    if (start < original.Length)
                        return original.Substring(start).Trim(' ', '.', '!', '?');
                }
            }

            return "";
        }

        // ============================================================
        //  ExtractDays — uses regex to pull "X days" from the input
        // ============================================================
        private int? ExtractDays(string lower)
        {
            // Matches: "in 3 days", "in 7 days", "in 1 day", "3 days from now", "tomorrow"
            if (lower.Contains("tomorrow"))
                return 1;

            var match = Regex.Match(lower, @"in\s+(\d+)\s+days?");
            if (match.Success && int.TryParse(match.Groups[1].Value, out int days))
                return days;

            match = Regex.Match(lower, @"(\d+)\s+days?\s+from\s+now");
            if (match.Success && int.TryParse(match.Groups[1].Value, out int days2))
                return days2;

            match = Regex.Match(lower, @"in\s+a\s+week");
            if (match.Success) return 7;

            match = Regex.Match(lower, @"next\s+week");
            if (match.Success) return 7;

            match = Regex.Match(lower, @"in\s+a\s+month");
            if (match.Success) return 30;

            return null;
        }

        // ============================================================
        //  MatchTopic — checks if input contains a known cyber topic
        // ============================================================
        private string MatchTopic(string lower)
        {
            foreach (var pair in TopicKeywordMap)
            {
                if (lower.Contains(pair.Key))
                    return pair.Value;
            }
            return "";
        }

        // ============================================================
        //  MatchesAny — returns true if input contains any keyword
        // ============================================================
        private bool MatchesAny(string lower, string[] keywords)
        {
            foreach (string kw in keywords)
            {
                if (lower.Contains(kw))
                    return true;
            }
            return false;
        }
    }
}