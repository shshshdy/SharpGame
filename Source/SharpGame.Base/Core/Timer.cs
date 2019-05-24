using System.Diagnostics;

namespace SharpGame
{
    public class Timer : Object
    {
        public static double SecondsPerTick { get; }
        public static double MilliSecsPerTick { get; }
        public static double MicroSecsPerTick { get; }

        private Stopwatch stopwatch;
        static Timer()
        {
            SecondsPerTick = 0.0;
            long countsPerSec = Stopwatch.Frequency;
            SecondsPerTick = 1.0 / countsPerSec;
            MilliSecsPerTick = 1000.0f / countsPerSec;
            MicroSecsPerTick = 1000000.0f / countsPerSec;
        }

        public Timer()
        {
            Debug.Assert(Stopwatch.IsHighResolution,
                "System does not support high-resolution performance counter.");
            stopwatch = new Stopwatch();
        }

        public long ElapsedMicroseconds => (long)(stopwatch.ElapsedTicks * MicroSecsPerTick);
        public long ElapsedMilliseconds => stopwatch.ElapsedMilliseconds;
        public float ElapsedSeconds => stopwatch.ElapsedMilliseconds / 1000.0f;
        public void Reset() =>stopwatch.Reset();
        public void Start() => stopwatch.Start();
        public void Restart() => stopwatch.Restart();
        public void Stop() => stopwatch.Stop();

    }

    public class Time
    {
        static float delta;
        public static float Delta => delta;

        static float elapsed = 0;
        public static float Elapsed => elapsed;

        static int frameNum = 0;
        public static int FrameNum => frameNum;

        public static void Tick(float timeStep)
        {
            frameNum += 1;
            delta = timeStep;
            elapsed += delta;
        }
    }
}
