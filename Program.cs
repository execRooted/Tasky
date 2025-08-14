using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
using System.Text;
using System.Media;

public static class ConsoleHelper
{
    public static void WriteLine(string text, ConsoleColor color)
    {
        var originalColor = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.WriteLine(text);
        Console.ForegroundColor = originalColor;
    }

    public static void Write(string text, ConsoleColor color)
    {
        var originalColor = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.Write(text);
        Console.ForegroundColor = originalColor;
    }
}

class Program
{
    static List<Task> todoList = new List<Task>();
    static string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tasks.json");
    static bool notificationShown = false;

    static void Main(string[] args)
    {
        Console.Title = "Tasky by execRooted";
        Console.OutputEncoding = Encoding.UTF8;
        
        LoadTasks();
        
        var notificationThread = new Thread(CheckDueTasksBackground);
        notificationThread.IsBackground = true;
        notificationThread.Start();

        bool running = true;

        while (running)
        {
            Console.Clear();
            DisplayHeader();
            DisplayMenu();

            var choice = Console.ReadKey(true).KeyChar.ToString();
            
            switch (choice)
            {
                case "1": AddTask(); break;
                case "2": RemoveTask(); break;
                case "3": ViewTasks(); break;
                case "4": MarkTaskStatus(); break;
                case "5": SortTasks(); break;
                case "6": SearchTasks(); break;
                case "7": running = false; break;
                case "8": DisplayStatistics(); break;
                default:
                    ShowError("Invalid choice, please try again.");
                    WaitForInput();
                    break;
            }

            SaveTasks();
        }

        Console.WriteLine("\nSaving tasks and exiting...");
        Thread.Sleep(1000);
    }

    static void DisplayHeader()
    {
        Console.WriteLine(@"
       _            _                  _            _       _        _   
      /\ \         / /\               / /\         /\_\    /\ \     /\_\ 
      \_\ \       / /  \             / /  \       / / /  _ \ \ \   / / / 
      /\__ \     / / /\ \           / / /\ \__   / / /  /\_\\ \ \_/ / /  
     / /_ \ \   / / /\ \ \         / / /\ \___\ / / /__/ / / \ \___/ /   
    / / /\ \ \ / / /  \ \ \        \ \ \ \/___// /\_____/ /   \ \ \_/    
   / / /  \/_// / /___/ /\ \        \ \ \     / /\_______/     \ \ \     
  / / /      / / /_____/ /\ \   _    \ \ \   / / /\ \ \         \ \ \    
 / / /      / /_________/\ \ \ /_/\__/ / /  / / /  \ \ \         \ \ \   
/_/ /      / / /_       __\ \_\\ \/___/ /  / / /    \ \ \         \ \_\  
\_\/       \_\___\     /____/_/ \_____\/   \/_/      \_\_\         \/_/  
                                                                         
                        Made by execRooted

");
        
        var pendingCount = todoList.Count(t => !t.IsCompleted);
        ConsoleHelper.WriteLine($"You have {pendingCount} pending task(s)", 
                         pendingCount > 0 ? ConsoleColor.DarkYellow : ConsoleColor.Green);
        Console.WriteLine();
    }

    static void DisplayMenu()
    {
        ConsoleHelper.WriteLine("1. Add Task", ConsoleColor.Blue);
        ConsoleHelper.WriteLine("2. Remove Task", ConsoleColor.Red);
        ConsoleHelper.WriteLine("3. View Tasks", ConsoleColor.Green);
        ConsoleHelper.WriteLine("4. Mark Task Status", ConsoleColor.Yellow);
        ConsoleHelper.WriteLine("5. Sort Tasks", ConsoleColor.Magenta);
        ConsoleHelper.WriteLine("6. Search Tasks", ConsoleColor.Cyan);
        ConsoleHelper.WriteLine("7. Exit", ConsoleColor.Gray);
        ConsoleHelper.WriteLine("8. Statistics", ConsoleColor.DarkCyan);
        Console.Write("\nChoose an option (1-8): ");
    }

    static void AddTask()
    {
        Console.Clear();
        DisplayHeader();
        ConsoleHelper.WriteLine("ADD NEW TASK", ConsoleColor.Blue);
        Console.WriteLine("---------------");

        Console.Write("\nTask description: ");
        string description = Console.ReadLine()?.Trim();

        if (string.IsNullOrWhiteSpace(description))
        {
            ShowError("Task description cannot be empty.");
            WaitForInput();
            return;
        }

        Console.Write("\nPriority (1-Low, 2-Medium, 3-High): ");
        Priority priority = Priority.Medium;
        if (Enum.TryParse(Console.ReadLine(), out Priority inputPriority))
        {
            priority = inputPriority;
        }

        Console.Write("\nCategory (Work, Personal, Shopping, Other): ");
        string category = Console.ReadLine()?.Trim();
        category = string.IsNullOrWhiteSpace(category) ? "Other" : category;

        DateTime? dueDate = null;
        Console.Write("\nAdd due date/time? (y/n): ");
        if (Console.ReadKey(true).Key == ConsoleKey.Y)
        {
            Console.Write("\nDue date (MM/dd/yyyy) or press Enter for today: ");
            string dateInput = Console.ReadLine();
            DateTime datePart = string.IsNullOrWhiteSpace(dateInput) ? DateTime.Today : DateTime.Parse(dateInput);

            Console.Write("Time (hh:mm) or press Enter for no specific time: ");
            string timeInput = Console.ReadLine();
            
            if (!string.IsNullOrWhiteSpace(timeInput))
            {
                TimeSpan timePart = TimeSpan.Parse(timeInput);
                dueDate = datePart.Add(timePart);
            }
            else
            {
                dueDate = datePart;
            }
        }

        var newTask = new Task
        {
            Id = todoList.Count > 0 ? todoList.Max(t => t.Id) + 1 : 1,
            Description = description,
            Priority = priority,
            Category = category,
            DueDate = dueDate,
            CreatedDate = DateTime.Now
        };

        todoList.Add(newTask);
        
        ConsoleHelper.WriteLine("\nTask added successfully!", ConsoleColor.Green);
        Console.WriteLine($"\nID: {newTask.Id}");
        Console.WriteLine($"Description: {newTask.Description}");
        ConsoleHelper.WriteLine($"Priority: {newTask.Priority}", GetPriorityColor(newTask.Priority));
        Console.WriteLine($"Category: {newTask.Category}");
        if (newTask.DueDate.HasValue)
        {
            ConsoleHelper.WriteLine($"Due: {newTask.DueDate.Value:MMM dd, yyyy hh:mm tt}", 
                            newTask.IsOverdue ? ConsoleColor.Red : ConsoleColor.White);
        }
        
        WaitForInput();
    }

    static void RemoveTask()
    {
        Console.Clear();
        DisplayHeader();
        ConsoleHelper.WriteLine("REMOVE TASK", ConsoleColor.Red);
        Console.WriteLine("--------------");

        if (!todoList.Any())
        {
            ShowInfo("Your to-do list is empty.");
            WaitForInput();
            return;
        }

        DisplayTaskList();
        Console.Write("\nEnter task ID to remove (or 0 to cancel): ");
        
        if (int.TryParse(Console.ReadLine(), out int taskId))
        {
            if (taskId == 0) return;

            var task = todoList.FirstOrDefault(t => t.Id == taskId);
            if (task != null)
            {
                todoList.Remove(task);
                // Renumber remaining tasks to maintain sequential IDs
                for (int i = 0; i < todoList.Count; i++)
                {
                    todoList[i].Id = i + 1;
                }
                ConsoleHelper.WriteLine("\nTask removed successfully!", ConsoleColor.Green);
            }
            else
            {
                ShowError("Task not found.");
            }
        }
        else
        {
            ShowError("Invalid task ID format.");
        }
        
        WaitForInput();
    }

    static void ViewTasks()
    {
        Console.Clear();
        DisplayHeader();
        ConsoleHelper.WriteLine("VIEW TASKS", ConsoleColor.Green);
        Console.WriteLine("-------------");

        if (!todoList.Any())
        {
            ShowInfo("Your to-do list is empty.");
            WaitForInput();
            return;
        }

        Console.WriteLine("\nFilter options:");
        Console.WriteLine("1. All tasks");
        Console.WriteLine("2. Pending only");
        Console.WriteLine("3. Completed only");
        Console.WriteLine("4. Overdue tasks");
        Console.WriteLine("5. By category");
        Console.Write("\nChoose filter (1-5): ");

        var filterChoice = Console.ReadKey(true).KeyChar.ToString();
        Console.WriteLine();

        IEnumerable<Task> filteredTasks = todoList;

        switch (filterChoice)
        {
            case "2": filteredTasks = todoList.Where(t => !t.IsCompleted); break;
            case "3": filteredTasks = todoList.Where(t => t.IsCompleted); break;
            case "4": filteredTasks = todoList.Where(t => t.IsOverdue); break;
            case "5":
                Console.Write("Enter category: ");
                string category = Console.ReadLine();
                filteredTasks = todoList.Where(t => t.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
                break;
        }

        if (!filteredTasks.Any())
        {
            ShowInfo("No tasks match your filter criteria.");
            WaitForInput();
            return;
        }

        DisplayTaskList(filteredTasks);
        
        Console.WriteLine("\nOptions:");
        Console.WriteLine("1. Export to text file");
        Console.WriteLine("2. Return to menu");
        Console.Write("\nChoose option: ");

        var option = Console.ReadKey(true).KeyChar.ToString();
        if (option == "1") ExportTasksToFile(filteredTasks);
    }

    static void MarkTaskStatus()
    {
        Console.Clear();
        DisplayHeader();
        ConsoleHelper.WriteLine("MARK TASK STATUS", ConsoleColor.Yellow);
        Console.WriteLine("-------------------");

        if (!todoList.Any())
        {
            ShowInfo("Your to-do list is empty.");
            WaitForInput();
            return;
        }

        DisplayTaskList();
        Console.Write("\nEnter task ID to toggle status (or 0 to cancel): ");
        
        if (int.TryParse(Console.ReadLine(), out int taskId))
        {
            if (taskId == 0) return;

            var task = todoList.FirstOrDefault(t => t.Id == taskId);
            if (task != null)
            {
                task.IsCompleted = !task.IsCompleted;
                task.CompletedDate = task.IsCompleted ? DateTime.Now : (DateTime?)null;

                ConsoleHelper.WriteLine($"\nTask marked as {(task.IsCompleted ? "completed" : "pending")}!", ConsoleColor.Green);
                
                if (task.IsCompleted)
                {
                    ConsoleHelper.WriteLine("\n🎉 Great job! 🎉", ConsoleColor.Yellow);
                    if (!todoList.Any(t => !t.IsCompleted))
                    {
                        ConsoleHelper.WriteLine("\n🌟 You've completed all your tasks! You're amazing! 🌟", ConsoleColor.Yellow);
                        PlayCompletionSound();
                    }
                }
            }
            else
            {
                ShowError("Task not found.");
            }
        }
        else
        {
            ShowError("Invalid task ID format.");
        }
        
        WaitForInput();
    }

    static void SortTasks()
    {
        Console.Clear();
        DisplayHeader();
        ConsoleHelper.WriteLine("SORT TASKS", ConsoleColor.Magenta);
        Console.WriteLine("-------------");

        if (!todoList.Any())
        {
            ShowInfo("Your to-do list is empty.");
            WaitForInput();
            return;
        }

        Console.WriteLine("\nSort by:");
        Console.WriteLine("1. Priority (High to Low)");
        Console.WriteLine("2. Due Date (Earliest first)");
        Console.WriteLine("3. Date Created (Newest first)");
        Console.WriteLine("4. Category (A-Z)");
        Console.WriteLine("5. Description (A-Z)");
        Console.Write("\nChoose sort option (1-5): ");

        var sortChoice = Console.ReadKey(true).KeyChar.ToString();
        Console.WriteLine();

        switch (sortChoice)
        {
            case "1":
                todoList = todoList.OrderByDescending(t => t.Priority)
                                 .ThenBy(t => t.DueDate ?? DateTime.MaxValue)
                                 .ToList();
                break;
            case "2":
                todoList = todoList.OrderBy(t => t.DueDate ?? DateTime.MaxValue)
                                 .ThenByDescending(t => t.Priority)
                                 .ToList();
                break;
            case "3":
                todoList = todoList.OrderByDescending(t => t.CreatedDate).ToList();
                break;
            case "4":
                todoList = todoList.OrderBy(t => t.Category).ToList();
                break;
            case "5":
                todoList = todoList.OrderBy(t => t.Description).ToList();
                break;
            default:
                ShowError("Invalid option. Tasks remain unsorted.");
                WaitForInput();
                return;
        }

        ConsoleHelper.WriteLine("\nTasks sorted successfully!", ConsoleColor.Green);
        DisplayTaskList();
        WaitForInput();
    }

    static void SearchTasks()
    {
        Console.Clear();
        DisplayHeader();
        ConsoleHelper.WriteLine("SEARCH TASKS", ConsoleColor.Cyan);
        Console.WriteLine("---------------");

        if (!todoList.Any())
        {
            ShowInfo("Your to-do list is empty.");
            WaitForInput();
            return;
        }

        Console.Write("\nEnter search term: ");
        string searchTerm = Console.ReadLine()?.Trim();

        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            ShowError("Search term cannot be empty.");
            WaitForInput();
            return;
        }

        var results = todoList.Where(t => 
            t.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
            t.Category.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (!results.Any())
        {
            ShowInfo("No tasks found matching your search.");
            WaitForInput();
            return;
        }

        ConsoleHelper.WriteLine($"\nFound {results.Count} matching task(s):", ConsoleColor.Green);
        DisplayTaskList(results);
        WaitForInput();
    }

    static void DisplayStatistics()
    {
        Console.Clear();
        DisplayHeader();
        ConsoleHelper.WriteLine("TASK STATISTICS", ConsoleColor.Magenta);
        Console.WriteLine("------------------");

        if (!todoList.Any())
        {
            ShowInfo("No tasks to display statistics for.");
            WaitForInput();
            return;
        }

        int totalTasks = todoList.Count;
        int completedTasks = todoList.Count(t => t.IsCompleted);
        int pendingTasks = totalTasks - completedTasks;
        int overdueTasks = todoList.Count(t => t.IsOverdue);

        Console.WriteLine($"\nTotal tasks: {totalTasks}");
        ConsoleHelper.WriteLine($"Completed: {completedTasks} ({completedTasks * 100 / totalTasks}%)", ConsoleColor.Green);
        ConsoleHelper.WriteLine($"Pending: {pendingTasks}", pendingTasks > 0 ? ConsoleColor.DarkYellow : ConsoleColor.Green);
        ConsoleHelper.WriteLine($"Overdue: {overdueTasks}", overdueTasks > 0 ? ConsoleColor.Red : ConsoleColor.Green);

        double productivityScore = completedTasks * 100.0 / Math.Max(1, totalTasks);
        ConsoleHelper.WriteLine($"\nProductivity Score: {productivityScore:F1}/100", 
                         GetProductivityColor(productivityScore));

        Console.WriteLine("\nCategories:");
        var categories = todoList.GroupBy(t => t.Category)
                                .OrderByDescending(g => g.Count());
        
        foreach (var group in categories)
        {
            int catCompleted = group.Count(t => t.IsCompleted);
            Console.WriteLine($"{group.Key}: {group.Count()} tasks ({catCompleted} completed)");
        }

        Console.WriteLine("\nPriority Distribution:");
        foreach (Priority priority in Enum.GetValues(typeof(Priority)))
        {
            int priorityCount = todoList.Count(t => t.Priority == priority);
            ConsoleHelper.WriteLine($"{priority}: {priorityCount} tasks", GetPriorityColor(priority));
        }

        var oldestPending = todoList.Where(t => !t.IsCompleted)
                                  .OrderBy(t => t.CreatedDate)
                                  .FirstOrDefault();
        
        if (oldestPending != null)
        {
            Console.WriteLine($"\nOldest pending task: {oldestPending.Description}");
            Console.WriteLine($"Created: {oldestPending.CreatedDate:MMM dd, yyyy} ({(DateTime.Now - oldestPending.CreatedDate).TotalDays:F0} days ago)");
        }

        WaitForInput();
    }

    static void DisplayTaskList(IEnumerable<Task> tasks = null)
    {
        tasks ??= todoList;
        
        Console.WriteLine();
        Console.WriteLine($"{"ID",-5} {"Status",-12} {"Priority",-10} {"Description",-30} {"Category",-15} {"Due Date",-20}");
        Console.WriteLine(new string('-', 100));

        foreach (var task in tasks)
        {
            Console.Write($"{task.Id,-5} ");
            
            ConsoleHelper.Write(task.IsCompleted ? 
                $"{"✓ Completed",-12} " : 
                $"{(task.IsOverdue ? "⚠ Overdue" : "Pending"),-12} ", 
                task.IsCompleted ? ConsoleColor.Green : (task.IsOverdue ? ConsoleColor.Red : ConsoleColor.DarkYellow));
            
            ConsoleHelper.Write($"{task.Priority,-10} ", GetPriorityColor(task.Priority));
            
            Console.Write($"{task.Description.Truncate(28),-30} ");
            Console.Write($"{task.Category.Truncate(13),-15} ");
            
            if (task.DueDate.HasValue)
            {
                ConsoleHelper.Write($"{task.DueDate.Value:MMM dd, yyyy hh:mm tt}", 
                            task.IsOverdue ? ConsoleColor.Red : ConsoleColor.White);
            }
            else
            {
                Console.Write("No due date");
            }
            
            Console.WriteLine();
        }
    }

    static void CheckDueTasksBackground()
    {
        while (true)
        {
            var now = DateTime.Now;
            var dueTasks = todoList.Where(t => 
                !t.IsCompleted && 
                t.DueDate.HasValue && 
                t.DueDate.Value <= now && 
                t.DueDate.Value > now.AddMinutes(-1));

            foreach (var task in dueTasks)
            {
                if (!notificationShown)
                {
                    notificationShown = true;
                    ShowNotification(task);
                }
            }

            notificationShown = false;
            Thread.Sleep(60000);
        }
    }

    static void ShowNotification(Task task)
    {
        PlayNotificationSound();

        Console.Clear();
        Console.BackgroundColor = ConsoleColor.DarkRed;
        Console.ForegroundColor = ConsoleColor.White;
        
        Console.WriteLine(" ⚠ ⚠ ⚠  TASK REMINDER  ⚠ ⚠ ⚠ ");
        Console.ResetColor();
        
        Console.WriteLine($"\nIt's time for your task: {task.Description}");
        Console.WriteLine($"Due: {task.DueDate:MMM dd, yyyy hh:mm tt}");
        ConsoleHelper.Write($"Priority: {task.Priority}", GetPriorityColor(task.Priority));
        
        Console.WriteLine("\nPress any key to acknowledge...");
        Console.ReadKey(true);
    }

    static void ExportTasksToFile(IEnumerable<Task> tasks)
    {
        string exportPath = $"tasks_export_{DateTime.Now:yyyyMMddHHmmss}.txt";
        
        using (var writer = new StreamWriter(exportPath))
        {
            writer.WriteLine($"TASK EXPORT - {DateTime.Now:MMMM dd, yyyy}");
            writer.WriteLine(new string('-', 40));
            writer.WriteLine();
            
            foreach (var task in tasks)
            {
                writer.WriteLine($"ID: {task.Id}");
                writer.WriteLine($"Description: {task.Description}");
                writer.WriteLine($"Status: {(task.IsCompleted ? "Completed" : "Pending")}");
                writer.WriteLine($"Priority: {task.Priority}");
                writer.WriteLine($"Category: {task.Category}");
                writer.WriteLine($"Created: {task.CreatedDate:MMM dd, yyyy}");
                if (task.DueDate.HasValue)
                {
                    writer.WriteLine($"Due: {task.DueDate.Value:MMM dd, yyyy hh:mm tt}");
                    writer.WriteLine($"Status: {(task.IsOverdue ? "OVERDUE" : "On time")}");
                }
                writer.WriteLine(new string('-', 30));
            }
            
            writer.WriteLine();
            writer.WriteLine($"Total tasks: {tasks.Count()}");
            writer.WriteLine($"Completed: {tasks.Count(t => t.IsCompleted)}");
            writer.WriteLine($"Pending: {tasks.Count(t => !t.IsCompleted)}");
        }
        
        ConsoleHelper.WriteLine($"\nTasks exported to {exportPath}", ConsoleColor.Green);
        WaitForInput();
    }

    static void PlayNotificationSound()
    {
        
        
        if (File.Exists("beep.mp3"))
        {
            try
            {
                // Works on Linux/Arch
                System.Diagnostics.Process.Start("mpv", "--really-quiet beep.mp3");
            }
            catch
            {
                ConsoleHelper.WriteLine("Could not play sound. Install mpv.", ConsoleColor.Red);
            }
    }
      
            
        
        
    }

    static void PlayCompletionSound()
    {
         if (!todoList.Any(t => !t.IsCompleted) && File.Exists("beep.mp3"))
        {
            try
            {
                System.Diagnostics.Process.Start("mpv", "--really-quiet beep.mp3");
            }
            catch
            {
                ConsoleHelper.WriteLine("Could not play sound. Install mpv.", ConsoleColor.Red);
            }
        }
    }

    static ConsoleColor GetPriorityColor(Priority priority)
    {
        switch (priority)
        {
            case Priority.High: return ConsoleColor.Red;
            case Priority.Medium: return ConsoleColor.Yellow;
            case Priority.Low: return ConsoleColor.Blue;
            default: return ConsoleColor.White;
        }
    }

    static ConsoleColor GetProductivityColor(double score)
    {
        if (score >= 80) return ConsoleColor.Green;
        if (score >= 50) return ConsoleColor.Yellow;
        return ConsoleColor.Red;
    }

    static void ShowError(string message)
    {
        ConsoleHelper.WriteLine($"\n❌ {message}", ConsoleColor.Red);
    }

    static void ShowInfo(string message)
    {
        ConsoleHelper.WriteLine($"\nℹ️ {message}", ConsoleColor.Blue);
    }

    static void WaitForInput()
    {
        Console.WriteLine("\nPress any key to continue...");
        Console.ReadKey(true);
    }

    static void SaveTasks()
    {
        try
        {
            string json = JsonConvert.SerializeObject(todoList, Formatting.Indented);
            File.WriteAllText(filePath, json);
        }
        catch (Exception ex)
        {
            ShowError($"Error saving tasks: {ex.Message}");
        }
    }

    static void LoadTasks()
    {
        try
        {
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                todoList = JsonConvert.DeserializeObject<List<Task>>(json) ?? new List<Task>();
            }
        }
        catch (Exception ex)
        {
            ShowError($"Error loading tasks: {ex.Message}");
            todoList = new List<Task>();
        }
    }
}

public class Task
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty; 
    public bool IsCompleted { get; set; }
    public Priority Priority { get; set; } = Priority.Medium;
    public string Category { get; set; } = "Other";
    public DateTime? DueDate { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? CompletedDate { get; set; }

    public bool IsOverdue => !IsCompleted && DueDate.HasValue && DueDate.Value < DateTime.Now;
}

public enum Priority
{
    Low = 1,
    Medium = 2,
    High = 3
}


public static class StringExtensions
{
    public static string Truncate(this string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return value.Length <= maxLength ? value : value.Substring(0, maxLength - 3) + "...";
    }
}