using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

//using System;
//using System.Windows;
//using System.Windows.Controls;

namespace CyberSecurityChatbotP3
{
    // ============================================================
    //  TaskWindow.xaml.cs  —  Task manager UI (CRUD operations)
    //  All data is persisted to MySQL via DatabaseHelper
    // ============================================================
    public partial class TaskWindow : Window
    {
        private DatabaseHelper db;
        private ActivityLog log;

        // ── Constructor ──────────────────────────────────────────
        public TaskWindow(DatabaseHelper database, ActivityLog activityLog)
        {
            InitializeComponent();
            db = database;
            log = activityLog;
            LoadTasks();
        }

        // ============================================================
        //  LoadTasks — fetches all tasks from DB and populates the list
        // ============================================================
        private void LoadTasks()
        {
            try
            {
                var tasks = db.GetAllTasks();
                TaskListView.ItemsSource = tasks;
                TxtTaskStatus.Text = tasks.Count + " task(s) loaded.";
            }
            catch (Exception ex)
            {
                TxtTaskStatus.Text = "DB error: " + ex.Message;
            }
        }

        // ============================================================
        //  BtnAddTask_Click — validates and inserts a new task
        // ============================================================
        private void BtnAddTask_Click(object sender, RoutedEventArgs e)
        {
            string title = TxtTaskTitle.Text.Trim();
            string desc = TxtTaskDesc.Text.Trim();

            // Validate title
            if (string.IsNullOrWhiteSpace(title))
            {
                TxtTaskStatus.Text = "Please enter a task title.";
                TxtTaskTitle.Focus();
                return;
            }

            // Parse optional reminder days
            DateTime? reminder = null;
            string daysText = TxtReminderDays.Text.Trim();
            if (!string.IsNullOrWhiteSpace(daysText))
            {
                if (int.TryParse(daysText, out int days) && days > 0)
                    reminder = DateTime.Now.AddDays(days);
                else
                {
                    TxtTaskStatus.Text = "Reminder days must be a positive whole number.";
                    return;
                }
            }

            // Auto-generate a description if the user left it blank
            if (string.IsNullOrWhiteSpace(desc))
                desc = BuildAutoDescription(title);

            try
            {
                var task = new CyberTask
                {
                    Title = title,
                    Description = desc,
                    ReminderDate = reminder,
                    CreatedAt = DateTime.Now
                };

                int newId = db.AddTask(task);
                task.Id = newId;

                // Log the action
                string logMsg = "Task added: \"" + title + "\"";
                if (reminder.HasValue)
                    logMsg += " (Reminder: " + reminder.Value.ToString("dd MMM yyyy") + ")";
                log.Add("Task", logMsg);

                // Clear the form
                TxtTaskTitle.Text = "";
                TxtTaskDesc.Text = "";
                TxtReminderDays.Text = "";

                TxtTaskStatus.Text = "✔ Task added: \"" + title + "\"";
                LoadTasks();
            }
            catch (Exception ex)
            {
                TxtTaskStatus.Text = "Failed to add task: " + ex.Message;
            }
        }

        // ============================================================
        //  BtnComplete_Click — marks the selected task as completed
        // ============================================================
        private void BtnComplete_Click(object sender, RoutedEventArgs e)
        {
            if (TaskListView.SelectedItem is CyberTask selected)
            {
                if (selected.IsCompleted)
                {
                    TxtTaskStatus.Text = "This task is already marked as complete.";
                    return;
                }

                try
                {
                    db.MarkCompleted(selected.Id);
                    log.Add("Task", "Task completed: \"" + selected.Title + "\"");
                    TxtTaskStatus.Text = "✔ Marked as complete: \"" + selected.Title + "\"";
                    LoadTasks();
                }
                catch (Exception ex)
                {
                    TxtTaskStatus.Text = "Failed to update task: " + ex.Message;
                }
            }
            else
            {
                TxtTaskStatus.Text = "Please select a task from the list first.";
            }
        }

        // ============================================================
        //  BtnDelete_Click — deletes the selected task after confirmation
        // ============================================================
        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (TaskListView.SelectedItem is CyberTask selected)
            {
                var result = MessageBox.Show(
                    "Delete task \"" + selected.Title + "\"?\nThis cannot be undone.",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        db.DeleteTask(selected.Id);
                        log.Add("Task", "Task deleted: \"" + selected.Title + "\"");
                        TxtTaskStatus.Text = "🗑 Deleted: \"" + selected.Title + "\"";
                        LoadTasks();
                    }
                    catch (Exception ex)
                    {
                        TxtTaskStatus.Text = "Failed to delete task: " + ex.Message;
                    }
                }
            }
            else
            {
                TxtTaskStatus.Text = "Please select a task from the list first.";
            }
        }

        // ============================================================
        //  BtnRefresh_Click — reloads tasks from the database
        // ============================================================
        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadTasks();
            TxtTaskStatus.Text = "Task list refreshed.";
        }

        // ============================================================
        //  BuildAutoDescription — creates a helpful default description
        //  based on common cybersecurity task keywords
        // ============================================================
        private string BuildAutoDescription(string title)
        {
            string lower = title.ToLower();

            if (lower.Contains("2fa") || lower.Contains("two-factor") || lower.Contains("two factor"))
                return "Enable two-factor authentication to add a second layer of security to your accounts.";
            if (lower.Contains("password"))
                return "Review and update your password to ensure it is strong, unique, and not reused elsewhere.";
            if (lower.Contains("privacy"))
                return "Review your account privacy settings to ensure your data is protected and not publicly visible.";
            if (lower.Contains("update") || lower.Contains("software"))
                return "Check for and install pending software updates to patch known security vulnerabilities.";
            if (lower.Contains("backup"))
                return "Back up your important data using the 3-2-1 rule: 3 copies, 2 media types, 1 offsite.";
            if (lower.Contains("antivirus") || lower.Contains("virus"))
                return "Run a full antivirus scan and update your definitions to protect against the latest threats.";
            if (lower.Contains("vpn"))
                return "Set up a VPN to encrypt your internet traffic, especially on public WiFi networks.";
            if (lower.Contains("firewall"))
                return "Review your firewall settings to ensure only trusted traffic is allowed through your network.";
            if (lower.Contains("phishing"))
                return "Review phishing awareness resources and check your email filters are catching suspicious messages.";

            // Generic fallback
            return title + " — added as a cybersecurity awareness task.";
        }
    }
}

