using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;

namespace CyberSecurityChatbotP3
{
    // ============================================================
    //  DatabaseHelper.cs  —  SQLite CRUD operations for task storage
    //  No installation required — database is a single .db file
    //  stored in the same folder as the application
    // ============================================================
    public class DatabaseHelper
    {
        // ── Path to the SQLite database file ────────────────────
        private static readonly string DbPath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "cyberbot.db");

        private static readonly string ConnectionString =
            "Data Source=" + DbPath + ";Version=3;";

        // ============================================================
        //  TestConnection — always returns true for SQLite
        // ============================================================
        public bool TestConnection()
        {
            try
            {
                using (var conn = new SQLiteConnection(ConnectionString))
                {
                    conn.Open();
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        // ============================================================
        //  EnsureTableExists — creates the tasks table if missing
        // ============================================================
        public void EnsureTableExists()
        {
            using (var conn = new SQLiteConnection(ConnectionString))
            {
                conn.Open();
                string sql = @"
                    CREATE TABLE IF NOT EXISTS tasks (
                        id           INTEGER PRIMARY KEY AUTOINCREMENT,
                        title        TEXT NOT NULL,
                        description  TEXT,
                        is_completed INTEGER DEFAULT 0,
                        reminder     TEXT NULL,
                        created_at   TEXT DEFAULT (datetime('now'))
                    );";
                using (var cmd = new SQLiteCommand(sql, conn))
                    cmd.ExecuteNonQuery();
            }
        }

        // ============================================================
        //  AddTask — inserts a new task row; returns the new ID
        // ============================================================
        public int AddTask(CyberTask task)
        {
            using (var conn = new SQLiteConnection(ConnectionString))
            {
                conn.Open();
                string sql = @"
                    INSERT INTO tasks (title, description, is_completed, reminder, created_at)
                    VALUES (@title, @desc, 0, @reminder, @created);
                    SELECT last_insert_rowid();";

                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@title", task.Title);
                    cmd.Parameters.AddWithValue("@desc", task.Description);
                    cmd.Parameters.AddWithValue("@reminder", task.ReminderDate.HasValue
                        ? task.ReminderDate.Value.ToString("yyyy-MM-dd HH:mm:ss")
                        : (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@created",
                        task.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));

                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }

        // ============================================================
        //  GetAllTasks — returns every task ordered by created_at
        // ============================================================
        public List<CyberTask> GetAllTasks()
        {
            var tasks = new List<CyberTask>();

            using (var conn = new SQLiteConnection(ConnectionString))
            {
                conn.Open();
                string sql = "SELECT * FROM tasks ORDER BY created_at DESC;";

                using (var cmd = new SQLiteCommand(sql, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var task = new CyberTask
                        {
                            Id = Convert.ToInt32(reader["id"]),
                            Title = reader["title"].ToString(),
                            Description = reader["description"] == DBNull.Value
                                            ? "" : reader["description"].ToString(),
                            IsCompleted = Convert.ToInt32(reader["is_completed"]) == 1,
                            CreatedAt = DateTime.Parse(reader["created_at"].ToString()),
                            ReminderDate = reader["reminder"] == DBNull.Value
                                            ? (DateTime?)null
                                            : DateTime.Parse(reader["reminder"].ToString())
                        };
                        tasks.Add(task);
                    }
                }
            }

            return tasks;
        }

        // ============================================================
        //  MarkCompleted — sets is_completed = 1 for a given task ID
        // ============================================================
        public void MarkCompleted(int taskId)
        {
            using (var conn = new SQLiteConnection(ConnectionString))
            {
                conn.Open();
                string sql = "UPDATE tasks SET is_completed = 1 WHERE id = @id;";
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", taskId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // ============================================================
        //  DeleteTask — removes a task row by ID
        // ============================================================
        public void DeleteTask(int taskId)
        {
            using (var conn = new SQLiteConnection(ConnectionString))
            {
                conn.Open();
                string sql = "DELETE FROM tasks WHERE id = @id;";
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", taskId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // ============================================================
        //  UpdateReminder — sets or clears the reminder date
        // ============================================================
        public void UpdateReminder(int taskId, DateTime? reminderDate)
        {
            using (var conn = new SQLiteConnection(ConnectionString))
            {
                conn.Open();
                string sql = "UPDATE tasks SET reminder = @reminder WHERE id = @id;";
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@reminder", reminderDate.HasValue
                        ? reminderDate.Value.ToString("yyyy-MM-dd HH:mm:ss")
                        : (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@id", taskId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // ============================================================
        //  GetOverdueTasks — returns tasks with reminders in the past
        // ============================================================
        public List<CyberTask> GetOverdueTasks()
        {
            var tasks = new List<CyberTask>();

            using (var conn = new SQLiteConnection(ConnectionString))
            {
                conn.Open();
                string sql = @"
                    SELECT * FROM tasks
                    WHERE reminder IS NOT NULL
                      AND reminder < datetime('now')
                      AND is_completed = 0
                    ORDER BY reminder ASC;";

                using (var cmd = new SQLiteCommand(sql, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        tasks.Add(new CyberTask
                        {
                            Id = Convert.ToInt32(reader["id"]),
                            Title = reader["title"].ToString(),
                            Description = reader["description"] == DBNull.Value
                                            ? "" : reader["description"].ToString(),
                            IsCompleted = Convert.ToInt32(reader["is_completed"]) == 1,
                            CreatedAt = DateTime.Parse(reader["created_at"].ToString()),
                            ReminderDate = DateTime.Parse(reader["reminder"].ToString())
                        });
                    }
                }
            }

            return tasks;
        }
    }
}
        
    
