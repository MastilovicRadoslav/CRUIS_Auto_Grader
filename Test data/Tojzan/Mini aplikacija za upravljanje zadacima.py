# todo_app.py

import datetime

class Task:
    def __init__(self, title, description):
        self.title = title
        self.description = description
        self.created_at = datetime.datetime.now()
        self.completed = False

    def mark_completed(self):
        self.completed = True

    def __str__(self):
        status = "✔️ Done" if self.completed else "❌ Not done"
        return f"[{status}] {self.title} - {self.description} (created {self.created_at.strftime('%Y-%m-%d %H:%M')})"


class ToDoApp:
    def __init__(self):
        self.tasks = []

    def add_task(self, title, description):
        task = Task(title, description)
        self.tasks.append(task)
        print(f"Task '{title}' added successfully!")

    def list_tasks(self):
        if not self.tasks:
            print("No tasks yet.")
            return
        print("\n--- Your Tasks ---")
        for i, task in enumerate(self.tasks, 1):
            print(f"{i}. {task}")
        print("------------------\n")

    def complete_task(self, index):
        try:
            task = self.tasks[index - 1]
            task.mark_completed()
            print(f"Task '{task.title}' marked as completed!")
        except IndexError:
            print("Invalid task number.")

    def delete_task(self, index):
        try:
            task = self.tasks.pop(index - 1)
            print(f"Task '{task.title}' deleted.")
        except IndexError:
            print("Invalid task number.")


def main():
    app = ToDoApp()

    while True:
        print("\n--- ToDo App ---")
        print("1. Add task")
        print("2. List tasks")
        print("3. Complete task")
        print("4. Delete task")
        print("5. Exit")
        choice = input("Choose an option (1-5): ")

        if choice == "1":
            title = input("Enter task title: ")
            desc = input("Enter task description: ")
            app.add_task(title, desc)
        elif choice == "2":
            app.list_tasks()
        elif choice == "3":
            app.list_tasks()
            num = int(input("Enter task number to complete: "))
            app.complete_task(num)
        elif choice == "4":
            app.list_tasks()
            num = int(input("Enter task number to delete: "))
            app.delete_task(num)
        elif choice == "5":
            print("Exiting ToDo App. Goodbye!")
            break
        else:
            print("Invalid option. Try again.")


if __name__ == "__main__":
    main()
