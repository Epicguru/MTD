using System.Collections.Generic;
using MTD.Entities;

namespace MTD.Jobs
{
    public abstract class Job
    {
        public JobManager Manager { get; internal set; }
        public SentientPawn Pawn
        {
            get
            {
                return Manager?.Pawn;
            }
        }
        public bool IsDone { get; protected set; }
        public bool HasStarted { get; private set; }
        public Job Parent { get; private set; }

        public Job Current
        {
            get
            {
                return children.TryPeek(out var result) ? result : null;
            }
        }
        public Job DeepestStarted
        {
            get
            {
                if (Current != null && Current.HasStarted)
                    return Current;
                if(this.HasStarted)
                    return this;
                return null;
            }
        }

        private readonly Queue<Job> children = new Queue<Job>();

        public virtual bool TickInternal(out bool hasSentInterrupt)
        {
            while (children.Count > 0)
            {
                var job = children.Peek();
                if (job.IsDone)
                {
                    children.Dequeue();
                    continue;
                }

                if (!job.HasStarted)
                {
                    job.HasStarted = true;
                    job.OnStart();
                }
                bool canContinue = job.TickInternal(out bool hasSent);
                if (!canContinue)
                {
                    if(!hasSent)
                        SendInterruptUp();
                    hasSentInterrupt = true;
                    return false;
                }
                break;
            }
            hasSentInterrupt = false;
            return true;
        }

        internal void SendInterruptUp()
        {
            Job job = this;
            while (job != null)
            {
                job.OnInterrupted();
                job = job.Parent;
            }
        }

        /// <summary>
        /// Called every frame while this job is active. Should return false if the
        /// job can not be completed.
        /// </summary>
        /// <returns>False if the job can not continue.</returns>
        public abstract bool Tick();

        /// <summary>
        /// Called on the first frame that this job becomes active, followed immediately by a
        /// call to <see cref="TickActive"/>.
        /// </summary>
        public abstract void OnStart();

        /// <summary>
        /// Called if this job has been interrupted.
        /// </summary>
        public abstract void OnInterrupted();

        public virtual Job Require(Job job)
        {
            if (job == null)
                throw new System.ArgumentNullException(nameof(job), "Job is null");
            if (job.Parent != null)
                throw new System.Exception("Job already has a parent.");

            if (!children.Contains(job))
            {
                job.Parent = this;
                children.Enqueue(job);
            }
            else
            {
                throw new System.Exception("Job already added as child");
            }

            return job;
        }
    }
}
