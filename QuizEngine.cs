using System;
using System.Collections.Generic;

namespace CyberSecurityChatbotP3
{
    // ============================================================
    //  QuizEngine.cs  —  Cybersecurity quiz with 12 questions
    //  Mix of multiple-choice and true/false formats.
    //  Used by QuizWindow.xaml.cs
    // ============================================================
    public class QuizEngine
    {
        // ── Single question model ────────────────────────────────
        public class Question
        {
            public string Text { get; set; } = "";
            public string[] Options { get; set; } = Array.Empty<string>();
            public int CorrectIndex { get; set; } = 0;   // 0-based index into Options
            public string Explanation { get; set; } = "";
            public bool IsTrueFalse { get; set; } = false;
        }

        // ── Question bank ────────────────────────────────────────
        private List<Question> allQuestions = new List<Question>()
        {
            // ── Multiple choice ──────────────────────────────────

            new Question
            {
                Text = "Which of the following is the BEST way to create a strong password?",
                Options = new[]
                {
                    "A) Use your pet's name and birth year",
                    "B) Use a short word that's easy to remember",
                    "C) Use a passphrase with uppercase, lowercase, numbers and symbols",
                    "D) Reuse a password you already know well"
                },
                CorrectIndex = 2,
                Explanation  = "A passphrase mixing character types is long AND complex — the two key factors in password strength."
            },

            new Question
            {
                Text = "What does 'phishing' mean in cybersecurity?",
                Options = new[]
                {
                    "A) A type of computer virus",
                    "B) A trick to steal personal info through fake emails or websites",
                    "C) A method of encrypting data",
                    "D) A firewall bypass technique"
                },
                CorrectIndex = 1,
                Explanation  = "Phishing uses deceptive messages to trick you into revealing passwords, credit card numbers, or other sensitive info."
            },

            new Question
            {
                Text = "What does HTTPS in a website URL indicate?",
                Options = new[]
                {
                    "A) The website is popular",
                    "B) The site is free to use",
                    "C) Your connection to the site is encrypted",
                    "D) The website has been verified by the government"
                },
                CorrectIndex = 2,
                Explanation  = "HTTPS means the data between your browser and the server is encrypted using TLS — look for the padlock icon."
            },

            new Question
            {
                Text = "What is Two-Factor Authentication (2FA)?",
                Options = new[]
                {
                    "A) Having two different passwords for the same account",
                    "B) A second verification step (e.g. a code on your phone) after your password",
                    "C) Logging into two accounts at the same time",
                    "D) A type of encryption algorithm"
                },
                CorrectIndex = 1,
                Explanation  = "2FA requires something you KNOW (password) + something you HAVE (phone/key). Even a stolen password won't work alone."
            },

            new Question
            {
                Text = "Which action is SAFEST when using public WiFi?",
                Options = new[]
                {
                    "A) Check your bank account quickly",
                    "B) Use a VPN to encrypt your connection",
                    "C) Share your hotspot with strangers",
                    "D) Disable your firewall for faster speed"
                },
                CorrectIndex = 1,
                Explanation  = "A VPN encrypts all your traffic on public WiFi, preventing eavesdroppers from intercepting your data."
            },

            new Question
            {
                Text = "What should you do FIRST if you receive a suspicious email asking for your password?",
                Options = new[]
                {
                    "A) Reply and ask if it's real",
                    "B) Click the link to investigate",
                    "C) Delete it and contact the organisation directly via their official website",
                    "D) Forward it to your friends as a warning"
                },
                CorrectIndex = 2,
                Explanation  = "Never click links in suspicious emails. Go directly to the official site and contact support if unsure."
            },

            new Question
            {
                Text = "What is ransomware?",
                Options = new[]
                {
                    "A) Software that speeds up your computer",
                    "B) A type of antivirus program",
                    "C) Malware that locks your files and demands payment to unlock them",
                    "D) A tool for encrypted communication"
                },
                CorrectIndex = 2,
                Explanation  = "Ransomware encrypts your files and demands money for the key. Prevention: regular backups and avoiding suspicious links."
            },

            new Question
            {
                Text = "Which of these passwords is the STRONGEST?",
                Options = new[]
                {
                    "A) password123",
                    "B) John1990",
                    "C) Tr0ub4dor&3",
                    "D) qwerty"
                },
                CorrectIndex = 2,
                Explanation  = "Tr0ub4dor&3 mixes uppercase, lowercase, numbers and symbols. The others are common or based on personal info — easy to guess."
            },

            // ── True / False ─────────────────────────────────────

            new Question
            {
                Text       = "TRUE or FALSE: You should reuse the same password across multiple websites to make them easier to remember.",
                Options    = new[] { "A) True", "B) False" },
                CorrectIndex = 1,
                IsTrueFalse  = true,
                Explanation  = "FALSE. If one site is breached, attackers try your password on every other site — called 'credential stuffing'. Use a password manager instead."
            },

            new Question
            {
                Text       = "TRUE or FALSE: A VPN (Virtual Private Network) can help protect your privacy on public WiFi.",
                Options    = new[] { "A) True", "B) False" },
                CorrectIndex = 0,
                IsTrueFalse  = true,
                Explanation  = "TRUE. A VPN encrypts your traffic so even if someone intercepts it on public WiFi, they see only scrambled data."
            },

            new Question
            {
                Text       = "TRUE or FALSE: Antivirus software alone is enough to protect you from all cyber threats.",
                Options    = new[] { "A) True", "B) False" },
                CorrectIndex = 1,
                IsTrueFalse  = true,
                Explanation  = "FALSE. Antivirus helps, but good security also needs strong passwords, 2FA, software updates, safe browsing habits, and awareness of social engineering."
            },

            new Question
            {
                Text       = "TRUE or FALSE: It is safe to click any link in an email as long as the email looks professional.",
                Options    = new[] { "A) True", "B) False" },
                CorrectIndex = 1,
                IsTrueFalse  = true,
                Explanation  = "FALSE. Phishing emails are often carefully designed to look professional. Always verify the sender's address and hover over links before clicking."
            }
        };

        // ── Quiz state ───────────────────────────────────────────
        private List<Question> shuffledQuestions = new List<Question>();
        private int currentIndex = 0;
        private int score = 0;

        // ============================================================
        //  StartQuiz — shuffles questions and resets state
        // ============================================================
        public void StartQuiz()
        {
            // Fisher-Yates shuffle for randomised order each game
            shuffledQuestions = new List<Question>(allQuestions);
            var rng = new Random();
            for (int i = shuffledQuestions.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (shuffledQuestions[i], shuffledQuestions[j]) =
                    (shuffledQuestions[j], shuffledQuestions[i]);
            }

            currentIndex = 0;
            score = 0;
        }

        // ============================================================
        //  CurrentQuestion — returns the active question or null if done
        // ============================================================
        public Question? CurrentQuestion =>
            currentIndex < shuffledQuestions.Count
                ? shuffledQuestions[currentIndex]
                : null;

        // ============================================================
        //  TotalQuestions — how many questions in the quiz
        // ============================================================
        public int TotalQuestions => shuffledQuestions.Count;

        // ============================================================
        //  CurrentQuestionNumber — 1-based number for display
        // ============================================================
        public int CurrentQuestionNumber => currentIndex + 1;

        // ============================================================
        //  Score — current number of correct answers
        // ============================================================
        public int Score => score;

        // ============================================================
        //  SubmitAnswer — checks the answer and advances the quiz
        //  Returns true if the answer was correct
        // ============================================================
        public bool SubmitAnswer(int selectedIndex)
        {
            if (CurrentQuestion == null) return false;

            bool correct = selectedIndex == CurrentQuestion.CorrectIndex;
            if (correct) score++;

            currentIndex++;
            return correct;
        }

        // ============================================================
        //  IsFinished — true when all questions have been answered
        // ============================================================
        public bool IsFinished => currentIndex >= shuffledQuestions.Count;

        // ============================================================
        //  GetFinalFeedback — score-based message at the end
        // ============================================================
        public string GetFinalFeedback()
        {
            double percent = (double)score / TotalQuestions * 100;

            if (percent == 100)
                return "Perfect score! You're a true cybersecurity expert! 🏆";
            if (percent >= 80)
                return "Great job! You're a cybersecurity pro — just a couple of gaps to fill!";
            if (percent >= 60)
                return "Good effort! You have solid foundations — keep learning to strengthen your knowledge.";
            if (percent >= 40)
                return "Not bad, but there's room to grow. Review the explanations and try again!";

            return "Keep learning to stay safe online! Every attempt makes you more cyber-aware. 💪";
        }
    }
}