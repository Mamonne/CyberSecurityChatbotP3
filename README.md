# CyberBot Part 3 — Setup & Integration Guide

## Files Included

| File | Purpose |
|------|---------|
| `ChatBot.cs` | Core chatbot logic (Parts 1 & 2 — improved) |
| `NlpProcessor.cs` | NLP intent detection (Task 3) |
| `DatabaseHelper.cs` | MySQL CRUD for task storage (Task 1) |
| `CyberTask.cs` | Task data model |
| `ActivityLog.cs` | Action history log (Task 4) |
| `QuizEngine.cs` | 12-question quiz logic (Task 2) |
| `QuizWindow.xaml` + `.cs` | Quiz popup window |
| `TaskWindow.xaml` + `.cs` | Task manager popup window |
| `MainWindow.xaml` + `.cs` | Updated main window (integrates all features) |

---

## Step 1 — NuGet Package (MySQL)

In Visual Studio:
1. Right-click your project → **Manage NuGet Packages**
2. Search for: `MySql.Data`
3. Install version **8.x**

---

## Step 2 — MySQL Database Setup

Run this SQL in MySQL Workbench or your MySQL client:

```sql
CREATE DATABASE IF NOT EXISTS cyberbot;

USE cyberbot;

CREATE TABLE IF NOT EXISTS tasks (
    id           INT AUTO_INCREMENT PRIMARY KEY,
    title        VARCHAR(255) NOT NULL,
    description  TEXT,
    is_completed TINYINT(1) DEFAULT 0,
    reminder     DATETIME NULL,
    created_at   DATETIME DEFAULT CURRENT_TIMESTAMP
);
```

---

## Step 3 — Update Connection String

Open `DatabaseHelper.cs` and edit the connection string:

```csharp
private const string ConnectionString =
    "Server=localhost;" +
    "Database=cyberbot;" +
    "User ID=root;" +
    "Password=YOUR_PASSWORD_HERE;" +   // ← your MySQL password
    "Port=3306;";
```

---

## Step 4 — Add Files to Project

1. Add all `.cs` files to your project (right-click project → Add → Existing Item)
2. Add `QuizWindow.xaml` and `TaskWindow.xaml` as WPF Windows
3. Replace your existing `MainWindow.xaml` and `MainWindow.xaml.cs`

---

## What Each Feature Does

### Task 1 — Task Assistant
- Type: `"add task to enable 2FA"` → saved to MySQL automatically
- Type: `"remind me to update my password in 7 days"` → task + reminder saved
- Click **📋 Task Manager** to view, complete, or delete tasks
- All changes reflect in the database immediately

### Task 2 — Security Quiz
- Click **🎮 Security Quiz** or type `"start quiz"` / `"quiz me"`
- 12 randomised questions (multiple choice + true/false)
- Immediate feedback + explanation after each answer
- Final score with personalised feedback message

### Task 3 — NLP Simulation
- Understands natural phrasing — no exact commands needed
- Examples:
  - `"Can you remind me to check my privacy settings?"` → sets reminder
  - `"I want to know about phishing"` → gives phishing tip
  - `"Show me my tasks"` → opens Task Manager
  - `"Test my knowledge"` → starts quiz
  - `"What actions have you taken?"` → shows activity log

### Task 4 — Activity Log
- Click **📜 Activity Log** or type `"show activity log"`
- Type `"show full log"` to see the complete history
- Logs: tasks added/completed/deleted, quiz answers, reminders set, NLP intents

---

## NLP Phrases the Bot Understands

| What you want | Example phrases |
|---------------|----------------|
| Add a task | "add task to...", "i need to...", "create a task called...", "log a task to..." |
| Set a reminder | "remind me to...", "don't let me forget to...", "set a reminder for..." |
| View tasks | "show my tasks", "what do i need to do", "list tasks" |
| Start quiz | "quiz me", "test my knowledge", "start quiz", "play the game" |
| Activity log | "show activity log", "what have you done", "recent actions" |
| Memory recall | "what do you remember", "what have i told you" |
