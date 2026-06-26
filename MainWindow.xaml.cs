using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace CyberSecurityChatbotP3
{
    // ============================================================
    //  MainWindow.xaml.cs  —  Part 3 main UI controller
    //
    //  Integrates:
    //    • ChatBot         — keyword + sentiment responses (Parts 1 & 2)
    //    • NlpProcessor    — intent detection from natural language
    //    • DatabaseHelper  — MySQL task storage
    //    • ActivityLog     — action history log
    //    • QuizWindow      — popup quiz game
    //    • TaskWindow      — popup task manager
    // ============================================================
    public partial class MainWindow : Window
    {
        // ── Core components ──────────────────────────────────────
        private ChatBot bot = new ChatBot();
        private NlpProcessor nlp = new NlpProcessor();
        private DatabaseHelper db = new DatabaseHelper();
        private ActivityLog log = new ActivityLog();

        // Whether the name entry flow is complete
        private bool nameEntered = false;

        // Whether the database connected successfully
        private bool dbConnected = false;

        // ── Constructor ──────────────────────────────────────────
        public MainWindow()
        {
            InitializeComponent();
        }

        // ============================================================
        //  Window_Loaded — startup: greeting, DB check, overdue tasks
        // ============================================================
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            bot.PlayVoiceGreeting();

            // Try connecting to MySQL
            InitialiseDatabase();

            ShowBotMessage("Welcome to CyberBot v3! Before we begin, what is your name?");
            TxtStatus.Text = "Enter your name to get started.";
        }

        // ============================================================
        //  InitialiseDatabase — tests the DB connection and creates table
        // ============================================================
        private void InitialiseDatabase()
        {
            try
            {
                dbConnected = db.TestConnection();
                if (dbConnected)
                {
                    db.EnsureTableExists();
                    log.Add("Chat", "Database connected successfully");
                }
                else
                {
                    log.Add("Chat", "Database unavailable — tasks will not persist");
                }
            }
            catch (Exception ex)
            {
                dbConnected = false;
                log.Add("Chat", "DB error: " + ex.Message);
            }
        }

        // ============================================================
        //  BtnSend_Click — fires when the Send button is clicked
        // ============================================================
        private void BtnSend_Click(object sender, RoutedEventArgs e)
        {
            SendMessage();
        }

        // ============================================================
        //  TxtInput_KeyDown — Enter key sends message
        // ============================================================
        private void TxtInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                SendMessage();
        }

        // ============================================================
        //  SendMessage — main dispatcher: name entry or NLP-routed chat
        // ============================================================
        private void SendMessage()
        {
            string userText = TxtInput.Text.Trim();
            if (string.IsNullOrWhiteSpace(userText))
            {
                ShowErrorMessage("You didn't type anything. Please enter a message.");
                return;
            }

            TxtInput.Clear();

            // ── Name entry flow (first message) ─────────────────
            if (!nameEntered)
            {
                HandleNameEntry(userText);
                return;
            }

            // ── Show user bubble ─────────────────────────────────
            ShowUserMessage(userText);
            log.Add("Chat", "User said: \"" + userText + "\"");

            // ── NLP intent detection ─────────────────────────────
            var intent = nlp.Analyse(userText);
            log.Add("NLP", "Intent detected: " + intent.Intent);

            string response = DispatchIntent(intent, userText);
            ShowBotMessage(response);
            TxtStatus.Text = "Ready.";

            // Auto-close on exit
            string lower = userText.ToLower().Trim();
            if (lower == "exit" || lower == "quit" || lower == "bye")
                ScheduleWindowClose();
        }

        // ============================================================
        //  DispatchIntent — routes NLP intent to the right handler
        // ============================================================
        private string DispatchIntent(NlpProcessor.NlpResult intent, string originalInput)
        {
            switch (intent.Intent)
            {
                case "StartQuiz":
                    OpenQuiz();
                    return "Opening the Security Quiz... Good luck, " + bot.UserName + "! 🎮";

                case "ShowLog":
                    bool showFull = originalInput.ToLower().Contains("full");
                    return log.BuildSummaryText(showFull ? 50 : 10);

                case "ShowMemory":
                    return bot.GetResponse("what do you remember");

                case "AddTask":
                    return HandleAddTaskFromNlp(intent);

                case "SetReminder":
                    return HandleSetReminderFromNlp(intent);

                case "ViewTasks":
                    OpenTaskManager();
                    return "Opening the Task Manager so you can see all your tasks, " + bot.UserName + "!";

                case "DeleteTask":
                    OpenTaskManager();
                    return "Opening the Task Manager — select the task you want to delete.";

                case "CompleteTask":
                    OpenTaskManager();
                    return "Opening the Task Manager — select the task to mark as complete.";

                case "GeneralTopic":
                    // Pass matched topic directly to chatbot
                    return bot.GetResponse(intent.MatchedTopic);

                default:
                    // Fall through to chatbot keyword/sentiment engine
                    return bot.GetResponse(originalInput);
            }
        }

        // ============================================================
        //  HandleAddTaskFromNlp — creates a task from natural language
        // ============================================================
        private string HandleAddTaskFromNlp(NlpProcessor.NlpResult intent)
        {
            string title = string.IsNullOrWhiteSpace(intent.ExtractedText)
                ? "Cybersecurity task"
                : CapitaliseFirst(intent.ExtractedText);

            DateTime? reminder = intent.DaysFromNow.HasValue
                ? DateTime.Now.AddDays(intent.DaysFromNow.Value)
                : (DateTime?)null;

            if (!dbConnected)
            {
                log.Add("Task", "Task requested but DB unavailable: \"" + title + "\"");
                return "I'd love to save that task, but the database isn't connected right now. " +
                       "Open the Task Manager and add it manually — or check your MySQL connection.";
            }

            try
            {
                var task = new CyberTask
                {
                    Title = title,
                    Description = title + " — added via natural language command.",
                    ReminderDate = reminder,
                    CreatedAt = DateTime.Now
                };

                int id = db.AddTask(task);

                string logMsg = "Task added via NLP: \"" + title + "\"";
                if (reminder.HasValue)
                    logMsg += " (reminder: " + reminder.Value.ToString("dd MMM") + ")";
                log.Add("Task", logMsg);

                string response = "Task added: \"" + title + "\". " +
                                  "I'll keep that in your to-do list, " + bot.UserName + ".";

                if (reminder.HasValue)
                    response += " Reminder set for " + reminder.Value.ToString("dd MMM yyyy") + ".";
                else
                    response += " Would you like a reminder? Say 'remind me in X days'.";

                return response;
            }
            catch (Exception ex)
            {
                return "Sorry, I couldn't save that task: " + ex.Message;
            }
        }

        // ============================================================
        //  HandleSetReminderFromNlp — sets a reminder from natural language
        // ============================================================
        private string HandleSetReminderFromNlp(NlpProcessor.NlpResult intent)
        {
            if (!intent.DaysFromNow.HasValue)
            {
                // No days detected — treat as an add task without a specific timeframe
                return HandleAddTaskFromNlp(intent) +
                       "\n\nHow many days from now would you like to be reminded? E.g., 'remind me in 3 days'.";
            }

            string title = string.IsNullOrWhiteSpace(intent.ExtractedText)
                ? "Reminder task"
                : CapitaliseFirst(intent.ExtractedText);

            DateTime reminderDate = DateTime.Now.AddDays(intent.DaysFromNow.Value);

            if (!dbConnected)
            {
                log.Add("Reminder", "Reminder requested but DB unavailable: \"" + title + "\"");
                return "I'd love to set that reminder, but the database isn't connected. " +
                       "Please check your MySQL connection and use the Task Manager.";
            }

            try
            {
                var task = new CyberTask
                {
                    Title = title,
                    Description = title + " — reminder set via natural language.",
                    ReminderDate = reminderDate,
                    CreatedAt = DateTime.Now
                };

                db.AddTask(task);

                log.Add("Reminder",
                    "Reminder set: \"" + title + "\" on " + reminderDate.ToString("dd MMM yyyy"));

                return "Got it, " + bot.UserName + "! Reminder set for \"" + title +
                       "\" on " + reminderDate.ToString("dd MMM yyyy") + ".";
            }
            catch (Exception ex)
            {
                return "Sorry, I couldn't set that reminder: " + ex.Message;
            }
        }

        // ============================================================
        //  HandleNameEntry — validates and saves the user's name
        // ============================================================
        private void HandleNameEntry(string userText)
        {
            foreach (char c in userText)
            {
                if (!char.IsLetter(c) && c != ' ')
                {
                    ShowErrorMessage("Names can only contain letters and spaces. Please try again.");
                    return;
                }
            }

            bot.UserName = userText;
            nameEntered = true;

            ShowUserMessage(userText);
            TxtUserLabel.Text = "User: " + bot.UserName;
            TxtStatus.Text = "Logged in as " + bot.UserName;

            string dbStatus = dbConnected
                ? " The task database is connected and ready."
                : " Note: the task database is offline — tasks won't persist.";

            log.Add("Chat", "User logged in as: " + bot.UserName);

            ShowBotMessage("Hello " + bot.UserName + "! Welcome to CyberBot v3." + dbStatus + "\n\n" +
                           "You can:\n" +
                           "• Ask about cybersecurity topics (password, phishing, privacy...)\n" +
                           "• Say 'add task to enable 2FA' — I'll save it for you\n" +
                           "• Say 'remind me to update my password in 7 days'\n" +
                           "• Say 'start quiz' to test your cybersecurity knowledge\n" +
                           "• Say 'show activity log' to see what I've been up to\n" +
                           "• Use the buttons above to open any feature\n\n" +
                           "Type 'help' for the full topic list.");
        }

        // ============================================================
        //  Feature button handlers
        // ============================================================

        private void BtnOpenTasks_Click(object sender, RoutedEventArgs e)
        {
            if (!nameEntered) { ShowErrorMessage("Please enter your name first."); return; }
            OpenTaskManager();
            ShowBotMessage("Opening the Task Manager for you, " + bot.UserName + "!");
            log.Add("Task", "Task Manager opened by user");
        }

        private void BtnOpenQuiz_Click(object sender, RoutedEventArgs e)
        {
            if (!nameEntered) { ShowErrorMessage("Please enter your name first."); return; }
            OpenQuiz();
        }

        private void BtnShowLog_Click(object sender, RoutedEventArgs e)
        {
            if (!nameEntered) { ShowErrorMessage("Please enter your name first."); return; }
            ShowBotMessage(log.BuildSummaryText(10));
        }

        private void BtnMemory_Click(object sender, RoutedEventArgs e)
        {
            if (!nameEntered) { ShowErrorMessage("Please enter your name first."); return; }
            ShowBotMessage(bot.GetResponse("what do you remember"));
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            ChatPanel.Children.Clear();
            ShowBotMessage("Chat cleared! What would you like to explore?");
            log.Add("Chat", "Chat cleared by user");
        }

        // ============================================================
        //  TopicButton_Click — quick topic shortcut buttons
        // ============================================================
        private void TopicButton_Click(object sender, RoutedEventArgs e)
        {
            if (!nameEntered)
            {
                ShowErrorMessage("Please enter your name first.");
                return;
            }
            Button clicked = (Button)sender;
            TxtInput.Text = clicked.Content.ToString();
            SendMessage();
        }

        // ============================================================
        //  OpenTaskManager — opens the TaskWindow as a dialog
        // ============================================================
        private void OpenTaskManager()
        {
           // if (!dbConnected)
            {
                //ShowErrorMessage("Task Manager requires MySQL. " +
                           //      "Check your connection string in DatabaseHelper.cs.");
               // return;
            }

            var taskWin = new TaskWindow(db, log) { Owner = this };
            taskWin.ShowDialog();
        }

        // ============================================================
        //  OpenQuiz — opens the QuizWindow and shows result in chat
        // ============================================================
        private void OpenQuiz()
        {
            var quizWin = new QuizWindow(log) { Owner = this };
            bool? result = quizWin.ShowDialog();

            if (result == true && quizWin.Tag is string scoreStr)
            {
                ShowBotMessage("Quiz complete! Your score: " + scoreStr +
                               ". Well done for testing your knowledge, " + bot.UserName + "! " +
                               "Keep practising to stay cyber-aware.");
                log.Add("Quiz", "Quiz result shown in chat: " + scoreStr);
            }
        }

        // ============================================================
        //  ScheduleWindowClose — closes 2 seconds after goodbye
        // ============================================================
        private void ScheduleWindowClose()
        {
            var timer = new System.Windows.Threading.DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(2);
            timer.Tick += (s, args) => { timer.Stop(); Close(); };
            timer.Start();
        }

        // ============================================================
        //  CapitaliseFirst — uppercases the first letter of a string
        // ============================================================
        private string CapitaliseFirst(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return char.ToUpper(s[0]) + s.Substring(1);
        }

        // ============================================================
        //  ShowBotMessage — left-aligned green bot bubble
        // ============================================================
        private void ShowBotMessage(string message)
        {
            TextBlock senderLabel = new TextBlock
            {
                Text = "CyberBot:",
                Foreground = new SolidColorBrush(Color.FromRgb(0, 255, 156)),
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                FontFamily = new FontFamily("Consolas"),
                Margin = new Thickness(0, 0, 0, 2)
            };
            TextBlock messageText = new TextBlock
            {
                Text = message,
                Foreground = new SolidColorBrush(Color.FromRgb(230, 237, 243)),
                FontSize = 13,
                TextWrapping = TextWrapping.Wrap
            };
            StackPanel stack = new StackPanel();
            stack.Children.Add(senderLabel);
            stack.Children.Add(messageText);

            Border bubble = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(28, 35, 51)),
                Padding = new Thickness(12, 8, 12, 8),
                Margin = new Thickness(0, 4, 80, 4),
                HorizontalAlignment = HorizontalAlignment.Left,
                Child = stack
            };
            ChatPanel.Children.Add(bubble);
            ChatScroller.ScrollToBottom();
        }

        // ============================================================
        //  ShowUserMessage — right-aligned user chat bubble
        // ============================================================
        private void ShowUserMessage(string message)
        {
            TextBlock senderLabel = new TextBlock
            {
                Text = "You:",
                Foreground = new SolidColorBrush(Color.FromRgb(0, 255, 156)),
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                FontFamily = new FontFamily("Consolas"),
                Margin = new Thickness(0, 0, 0, 2),
                HorizontalAlignment = HorizontalAlignment.Right
            };
            TextBlock messageText = new TextBlock
            {
                Text = message,
                Foreground = new SolidColorBrush(Color.FromRgb(0, 255, 156)),
                FontSize = 13,
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            StackPanel stack = new StackPanel();
            stack.Children.Add(senderLabel);
            stack.Children.Add(messageText);

            Border bubble = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0, 68, 42)),
                Padding = new Thickness(12, 8, 12, 8),
                Margin = new Thickness(80, 4, 0, 4),
                HorizontalAlignment = HorizontalAlignment.Right,
                Child = stack
            };
            ChatPanel.Children.Add(bubble);
            ChatScroller.ScrollToBottom();
        }

        // ============================================================
        //  ShowErrorMessage — red system error bubble
        // ============================================================
        private void ShowErrorMessage(string message)
        {
            TextBlock senderLabel = new TextBlock
            {
                Text = "System:",
                Foreground = new SolidColorBrush(Color.FromRgb(255, 123, 114)),
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                FontFamily = new FontFamily("Consolas"),
                Margin = new Thickness(0, 0, 0, 2)
            };
            TextBlock messageText = new TextBlock
            {
                Text = "Warning: " + message,
                Foreground = new SolidColorBrush(Color.FromRgb(255, 123, 114)),
                FontSize = 13,
                TextWrapping = TextWrapping.Wrap
            };
            StackPanel stack = new StackPanel();
            stack.Children.Add(senderLabel);
            stack.Children.Add(messageText);

            Border bubble = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(61, 16, 16)),
                Padding = new Thickness(12, 8, 12, 8),
                Margin = new Thickness(0, 4, 80, 4),
                HorizontalAlignment = HorizontalAlignment.Left,
                Child = stack
            };
            ChatPanel.Children.Add(bubble);
            ChatScroller.ScrollToBottom();
        }
    }
}