using System.Collections.Generic;
using MTD.Entities;
using Nez;

namespace MTD.Jobs
{
    /// <summary>
    /// A goal is the highest level component of the job system.
    ///
    /// A goal is something like 'eat food', 'escape', or 'build'.
    /// Goals are intentionally quite vague, because 'eating food' can be achieved a great number of ways.
    /// For example, the pawn could walk to a fridge, take out a meal and eat it.
    /// They could also harvest a crop, cook that crop, then eat that resulting meal.
    /// In either case, the goal is achieved, but the means of reaching that goal are vastly different.
    ///<para></para>
    /// A goal only has 2 very important requirements:
    /// <list type="bullet">
    /// <item>It must know when the goal has been achieved.</item>
    /// <item>???</item>
    /// </list>
    /// Goals are provided by GoalProviders.
    /// </summary>
    public abstract class Goal
    {
        public abstract string Name { get; }

        public JobManager Manager { get; internal set; }
        public SentientPawn Pawn { get { return Manager?.Pawn; } }
        public bool IsInterrupted { get; private set; }
        public Task CurrentTask
        {
            get
            {
                if (tasks.TryPeek(out var current))
                    return current;
                return null;
            }
        }

        private Queue<Task> tasks = new Queue<Task>();

        public abstract void Plan();

        public virtual void Tick()
        {
            // Don't update current if goal has been interrupted.
            if (IsInterrupted)
                return;

            // No tasks? Don't update.
            if (!tasks.TryPeek(out var current))
                return;

            // Any tasks have failed? Don't update.
            if (IsFailed())
                return;

            // If the current task is not complete or failed, tick it.
            // NOTE: Using default implementation, it is impossible for current.IsFailed to be true here.
            if(!current.IsComplete && !current.IsFailed)
                current.Tick();

            // If the current is complete, remove it so that the next task is started.
            // Note that is should not be possible for task to be failed and compete at the same time.
            if (current.IsComplete)
                tasks.Dequeue();
        }

        internal void Interrupt()
        {
            if (IsInterrupted)
            {
                Debug.Error("Interrupt already called before.");
                return;
            }
            if (IsFailed())
            {
                Debug.Error("Cannot interrupt, already failed.");
                return;
            }
            if (IsComplete())
            {
                Debug.Error("Cannot interrupt, already complete.");
                return;
            }

            IsInterrupted = true;

            if (tasks.TryPeek(out var current))
            {
                if (!current.IsComplete && !current.IsFailed)
                    current.UponInterrupt();
            }
        }

        public virtual bool IsComplete()
        {
            foreach (var task in tasks)
            {
                if (!task.IsComplete)
                    return false;
            }
            return true;
        }

        public virtual bool IsFailed()
        {
            foreach (var task in tasks)
            {
                if (task.IsFailed)
                    return true;
            }
            return false;
        }

        protected Goal AddTask(Task task)
        {
            if (task.Goal != null)
            {
                Debug.Error($"Task '{task}' already in use or used previously!");
                return this;
            }

            tasks.Enqueue(task);

            task.Goal = this;
            return this;
        }

        public virtual string DebugString
        {
            get
            {
                string s = $"{Name}";
                if (IsComplete())
                    s += " (Complete)";
                if (IsFailed())
                    s += " (Failed)";
                if (IsInterrupted)
                    s += " (Interrupted)";
                s += "\nTasks:\n";

                foreach (var task in tasks)
                {
                    s += $" - {task}\n";
                }

                return s;
            }
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
