using MTD.Entities;

namespace MTD.Jobs
{
    /// <summary>
    /// Tasks are the 'atomic' components that make up Goals.
    /// A task should be as simple and straightforward as possible, such as
    /// 'move to (x, y)' or 'heal self' or 'sleep (time)'.
    /// Multiple tasks are chained together to achieve a goal.
    /// Tasks have three important qualities:
    /// <list type="number">
    /// <item>They must be able to be interrupted.</item>
    /// <item>They must have an end condition (tasks cannot be indefinite)</item>
    /// <item>They must be able to detect an impossibility to complete</item>
    /// </list>
    /// </summary>
    public abstract class Task
    {
        public abstract string Name { get; }
        public bool IsComplete { get; private set; }
        public bool IsFailed { get; private set; }

        public Goal Goal { get; internal set; }
        public JobManager Manager
        {
            get
            {
                return Goal?.Manager;
            }
        }
        public SentientPawn Pawn
        {
            get
            {
                return Manager?.Pawn;
            }
        }

        public abstract void Tick();

        public virtual void UponInterrupt()
        {

        }

        protected void Complete()
        {
            if(!this.IsFailed)
                this.IsComplete = true;
        }

        protected void Fail()
        {
            if(!this.IsComplete)
                this.IsFailed = true;
        }

        public override string ToString()
        {
            string s = $"{Name}";
            if (IsComplete)
                s += " (Complete)";
            if (IsFailed)
                s += " (Failed)";
            return s;
        }
    }
}
