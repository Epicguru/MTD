using Nez;

namespace MTD.Jobs.Tasks
{
    public class WaitTask : Task
    {
        public override string Name { get; }

        private float timeRemaining;

        public WaitTask(float time)
        {
            // Instantly complete if time is zero.
            if (time <= 0f)
                Complete();

            timeRemaining = time;
            Name = $"Waiting for {time:F1} seconds.";
        }

        public override void Tick()
        {
            timeRemaining -= Time.DeltaTime;
            if (timeRemaining <= 0f)
                Complete();
        }
    }
}
