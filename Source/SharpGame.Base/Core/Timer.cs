using System.Diagnostics;

namespace SharpGame
{
    public class Timer : Object
    {
        public static double SecondsPerCount { get; }
        public static double MilliSecondsPerCount { get; }

        private double deltaTime;
        private long baseTime;
        private long pausedTime;
        private long stopTime;
        private long prevTime;
        private long currTime;
        private bool stopped;
        private int frameNum_ = 0;

        static Timer()
        {
            SecondsPerCount = 0.0;
            long countsPerSec = Stopwatch.Frequency;
            SecondsPerCount = 1.0 / countsPerSec;
            MilliSecondsPerCount = 1000.0f / countsPerSec;
        }

        public Timer()
        {
            Debug.Assert(Stopwatch.IsHighResolution,
                "System does not support high-resolution performance counter.");

            deltaTime = -1.0;
            baseTime = 0;
            pausedTime = 0;
            prevTime = 0;
            currTime = 0;
            stopped = false;

        }

        public float TotalTime
        {
            get
            {
                if (stopped)
                    return (float)((stopTime - pausedTime - baseTime) * SecondsPerCount);

                return (float)((currTime - pausedTime - baseTime) * SecondsPerCount);
            }
        }

        public float DeltaTime => (float)deltaTime;

        public int FrameNum => frameNum_;

        public void Reset()
        {
            long curTime = Stopwatch.GetTimestamp();
            baseTime = curTime;
            prevTime = curTime;
            stopTime = 0;
            stopped = false;
            frameNum_ = 0;
        }

        public void Start()
        {
            long startTime = Stopwatch.GetTimestamp();
            if (stopped)
            {
                pausedTime += (startTime - stopTime);
                prevTime = startTime;
                stopTime = 0;
                stopped = false;
            }
        }

        public void Stop()
        {
            if (!stopped)
            {
                long curTime = Stopwatch.GetTimestamp();
                stopTime = curTime;
                stopped = true;
            }
        }

        public void Tick()
        {
            if (stopped)
            {
                deltaTime = 0.0;
                return;
            }

            long curTime = Stopwatch.GetTimestamp();
            currTime = curTime;
            deltaTime = (currTime - prevTime) * SecondsPerCount;

            prevTime = currTime;
            if (deltaTime < 0.0)
                deltaTime = 0.0;

            frameNum_ ++;
        }
    }

    public class Time
    {
        public static float Delta => InstanceHoler<Timer>.inst.DeltaTime;
        public static float Total => InstanceHoler<Timer>.inst.TotalTime;
        public static int FrameNum => InstanceHoler<Timer>.inst.FrameNum;
    }
}
