using System.Diagnostics;

namespace SharpGame
{
    public class Timer : Object
    {
        public static double SecondsPerCount { get; }
        public static double MilliSecondsPerCount { get; }

        private double _deltaTime;
        private long _baseTime;
        private long _pausedTime;
        private long _stopTime;
        private long _prevTime;
        private long _currTime;
        private bool _stopped;
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

            _deltaTime = -1.0;
            _baseTime = 0;
            _pausedTime = 0;
            _prevTime = 0;
            _currTime = 0;
            _stopped = false;

        }

        public float TotalTime
        {
            get
            {
                if (_stopped)
                    return (float)((_stopTime - _pausedTime - _baseTime) * SecondsPerCount);

                return (float)((_currTime - _pausedTime - _baseTime) * SecondsPerCount);
            }
        }

        public float DeltaTime => (float)_deltaTime;

        public int FrameNum => frameNum_;

        public void Reset()
        {
            long curTime = Stopwatch.GetTimestamp();
            _baseTime = curTime;
            _prevTime = curTime;
            _stopTime = 0;
            _stopped = false;
            frameNum_ = 0;
        }

        public void Start()
        {
            long startTime = Stopwatch.GetTimestamp();
            if (_stopped)
            {
                _pausedTime += (startTime - _stopTime);
                _prevTime = startTime;
                _stopTime = 0;
                _stopped = false;
            }
        }

        public void Stop()
        {
            if (!_stopped)
            {
                long curTime = Stopwatch.GetTimestamp();
                _stopTime = curTime;
                _stopped = true;
            }
        }

        public void Tick()
        {
            if (_stopped)
            {
                _deltaTime = 0.0;
                return;
            }

            long curTime = Stopwatch.GetTimestamp();
            _currTime = curTime;
            _deltaTime = (_currTime - _prevTime) * SecondsPerCount;

            _prevTime = _currTime;
            if (_deltaTime < 0.0)
                _deltaTime = 0.0;

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
