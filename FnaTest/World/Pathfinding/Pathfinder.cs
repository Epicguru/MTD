using Microsoft.Xna.Framework;
using Nez;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace MTD.World.Pathfinding
{
    public class Pathfinder : IDisposable
    {
        public class Request
        {
            // From request.
            public Point Start { get; internal set; }
            public Point End { get; internal set; }
            public object UserObject { get; internal set; }
            public Action<PathResult, List<Point>, object> UponComplete { get; internal set; }

            // From response.
            public List<Point> Path;
            public PathResult Result;
        }

        public readonly int ThreadCount;
        private Queue<Request> requestPool;

        private ConcurrentQueue<Request> pendingRequests;
        private ConcurrentQueue<Request> pendingReturns;

        public int ThreadIdleWaitTimeMS = 1;
        public int MaxOpenNodes = 500;

        private Thread[] threads;
        private PathCalculator[] calculators;
        private int[][] pathTimes;
        private int[] pathTimesHeads;
        private float[] threadProcessingPercentages;
        private int[] threadRequestsProcessedPerSecond;
        private bool run;

        public Pathfinder(int threadCount)
        {
            ThreadCount = threadCount;
            if (threadCount < 1)
                throw new ArgumentOutOfRangeException(nameof(threadCount), "Must use at least 1 thread.");

            requestPool = new Queue<Request>();
            pendingRequests = new ConcurrentQueue<Request>();
            pendingReturns = new ConcurrentQueue<Request>();
        }

        public void Update()
        {
            while (pendingReturns.TryDequeue(out var result))
            {
                result.UponComplete?.Invoke(result.Result, result.Path, result.UserObject);
                ReturnRequest(result);
            }
        }

        public void Start(Map map)
        {
            if (threads != null)
            {
                Debug.Error("Already started pathfinding manager.");
                return;
            }

            if (map == null)
                throw new ArgumentNullException(nameof(map));

            Debug.Log($"Starting pathfinder with {ThreadCount} threads.");

            run = true;
            threads = new Thread[ThreadCount];
            calculators = new PathCalculator[ThreadCount];
            pathTimes = new int[ThreadCount][];
            pathTimesHeads = new int[ThreadCount];
            threadProcessingPercentages = new float[ThreadCount];
            threadRequestsProcessedPerSecond = new int[ThreadCount];
            for (int i = 0; i < ThreadCount; i++)
            {
                pathTimes[i] = new int[100];

                var calc = new PathCalculator(map, MaxOpenNodes);
                calculators[i] = calc;

                var thread = new Thread(ThreadRun);
                thread.Priority = ThreadPriority.AboveNormal;
                threads[i] = thread;

                thread.Start();
            }
        }

        public void Stop(bool forceQuit = false)
        {
            if (threads == null)
            {
                Debug.Error("Pathfinding manager not started, cannot stop.");
                return;
            }

            Debug.Log($"Shutting down pathfinder, forceQuit = {forceQuit}.");

            run = false;

            requestPool.Clear();
            requestPool = null;

            if (forceQuit)
            {
                foreach (var thread in threads)
                {
                    thread.Abort();
                }
            }
            threads = null;
        }

        private void ThreadRun(object threadIndexObj)
        {
            int threadIndex = (int) threadIndexObj;
            var calc = calculators[threadIndex];
            double timeSpentThisSecond = 0.0;
            int processedThisSecond = 0;
            var stopwatch = new System.Diagnostics.Stopwatch();
            var secondTimer = new System.Diagnostics.Stopwatch();
            secondTimer.Start();

            while (run)
            {
                if (pendingRequests == null)
                    break;

                if (secondTimer.Elapsed.TotalSeconds >= 1.0)
                {
                    secondTimer.Restart();
                    float percentageThisSecond = (float) timeSpentThisSecond;
                    threadProcessingPercentages[threadIndex] = percentageThisSecond;
                    threadRequestsProcessedPerSecond[threadIndex] = processedThisSecond;
                    timeSpentThisSecond = 0.0;
                    processedThisSecond = 0;
                }

                if (pendingRequests.Count == 0)
                {
                    Thread.Sleep(ThreadIdleWaitTimeMS);
                    continue;
                }
                if (!pendingRequests.TryDequeue(out var req))
                {
                    Thread.Sleep(ThreadIdleWaitTimeMS);
                    continue;
                }

                if (req.UponComplete == null)
                {
                    // Useless...
                    Debug.Warn($"Pathfinding request from {req.Start} to {req.End} has null UponComplete action, skipping.");
                    continue;
                }

                stopwatch.Restart();
                var result = calc.Calculate(req.Start, req.End, out var list);
                stopwatch.Stop();

                req.Path = list;
                req.Result = result;
                pendingReturns.Enqueue(req);

                processedThisSecond++;
                int time = (int)stopwatch.ElapsedMilliseconds;
                timeSpentThisSecond += stopwatch.Elapsed.TotalSeconds;
                int head = pathTimesHeads[threadIndex];
                pathTimes[threadIndex][head] = time;
                head++;
                if (head >= pathTimes[threadIndex].Length)
                    head = 0;
                pathTimesHeads[threadIndex] = head;
            }
        }

        public void FindPath(Point start, Point end, Action<PathResult, List<Point>, object> uponComplete, object userObject = null)
        {
            if (uponComplete == null)
                return;

            var req = CreateRequestObject(start, end, uponComplete, userObject);
            pendingRequests.Enqueue(req);
        }

        private Request CreateRequestObject(Point start, Point end, Action<PathResult, List<Point>, object> uponComplete, object userObject = null)
        {
            if (requestPool.Count > 0)
            {
                var fromPool = requestPool.Dequeue();
                fromPool.Start = start;
                fromPool.End = end;
                fromPool.UponComplete = uponComplete;
                fromPool.UserObject = userObject;
                return fromPool;
            }

            var req = new Request();
            req.Start = start;
            req.End = end;
            req.UponComplete = uponComplete;
            req.UserObject = userObject;
            return req;
        }

        private void ReturnRequest(Request r)
        {
            if (r == null)
            {
                Debug.Error("Tried to return request that is null.");
                return;
            }

            r.UserObject = null;
            r.UponComplete = null;
            r.Path = null;
            r.Result = PathResult.ERROR_INTERNAL;
            requestPool.Enqueue(r);
        }

        public void GetThreadStats(int threadIndex, out int[] pathTimes, out int pathTimesHeader, out float usagePercentage, out int processedPerSecond)
        {
            if (threadIndex < 0 || threadIndex >= ThreadCount)
                throw new ArgumentOutOfRangeException(nameof(threadIndex));

            pathTimes = this.pathTimes[threadIndex];
            pathTimesHeader = pathTimesHeads[threadIndex];
            usagePercentage = threadProcessingPercentages[threadIndex];
            processedPerSecond = threadRequestsProcessedPerSecond[threadIndex];
        }

        public void Dispose()
        {
            Stop(true);

            if (requestPool == null)
                return;

            Thread.Sleep(100);

            requestPool.Clear();
            requestPool = null;
            pendingReturns.Clear();
            pendingReturns = null;
        }
    }
}
