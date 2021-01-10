namespace MTD.Jobs.Tasks
{
    /// <summary>
    /// A version of <see cref="Task"/> that includes an
    /// <see cref="OnStart"/> method, called the first time <see cref="Tick"/>
    /// is called.
    /// </summary>
    public abstract class TaskWithStart : Task
    {
        private bool isFirstTick = true;

        /// <summary>
        /// Called every frame while this task is active.
        /// Invoke base.Tick() BEFORE running your own code.
        /// </summary>
        public override void Tick()
        {
            if (isFirstTick && !IsFailed && !IsComplete)
            {
                OnStart();
                isFirstTick = false;
            }
        }

        public abstract void OnStart();
    }
}
