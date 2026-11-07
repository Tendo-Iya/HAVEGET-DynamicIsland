using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Media;

namespace HavegetDynamicIsland
{
    /// <summary>
    /// A critically damped spring used for smooth island sizing,
    /// tab indicator and content transitions. Every frame is driven by
    /// WPF's composition clock, so animation cost stays tied to the monitor.
    /// </summary>
    public sealed class SpringAnimation
    {
        private readonly Action<double> _onUpdate;
        private readonly double _stiffness;
        private readonly double _damping;
        private bool _running;
        private double _target;

        public SpringAnimation(double initial, double stiffness, double damping, Action<double> onUpdate)
        {
            _stiffness = stiffness;
            _damping = damping;
            _onUpdate = onUpdate;
            Value = initial;
            _target = initial;
            if (onUpdate != null) onUpdate(initial);
        }

        public double Value { get; private set; }

        public bool IsRunning { get { return _running; } }

        public event Action Settled;

        public void To(double target, bool kick)
        {
            _target = target;
            if (Math.Abs(Value - target) < 0.0005 && Math.Abs(_velocity) < 0.002)
            {
                Value = target;
                if (_onUpdate != null) _onUpdate(Value);
                return;
            }
            if (kick) _velocity += (target - Value) * 5.0;
            if (!_running)
            {
                _running = true;
                SpringEngine.Current.Add(this);
            }
        }

        public void Snap(double value)
        {
            Value = value;
            _target = value;
            _velocity = 0;
            if (_onUpdate != null) _onUpdate(value);
        }

        private double _velocity;

        internal bool Step(double dt)
        {
            dt = Math.Max(0.001, Math.Min(0.045, dt));
            // Sub-step for stable, smooth motion even on a slow frame.
            int steps = 3;
            double h = dt / steps;
            for (int i = 0; i < steps; i++)
            {
                double accel = -_stiffness * (Value - _target) - _damping * _velocity;
                _velocity += accel * h;
                Value += _velocity * h;
            }
            if (Math.Abs(Value - _target) < 0.0015 && Math.Abs(_velocity) < 0.006)
            {
                Value = _target;
                _velocity = 0;
                if (_onUpdate != null) _onUpdate(Value);
                _running = false;
                if (Settled != null) Settled();
                return false;
            }
            if (_onUpdate != null) _onUpdate(Value);
            return true;
        }
    }

    public sealed class SpringEngine
    {
        private static readonly SpringEngine _instance = new SpringEngine();
        private readonly List<SpringAnimation> _active = new List<SpringAnimation>();
        private readonly List<SpringAnimation> _pending = new List<SpringAnimation>();
        private readonly Stopwatch _clock = new Stopwatch();
        private bool _hooked;
        private TimeSpan _last;

        private SpringEngine() { }

        public static SpringEngine Current { get { return _instance; } }

        public void Add(SpringAnimation animation)
        {
            if (animation == null) return;
            if (!_pending.Contains(animation)) _pending.Add(animation);
            if (!_hooked)
            {
                _hooked = true;
                _clock.Start();
                _last = _clock.Elapsed;
                CompositionTarget.Rendering += OnRendering;
            }
        }

        private void OnRendering(object sender, EventArgs e)
        {
            TimeSpan now = _clock.Elapsed;
            double dt = (now - _last).TotalSeconds;
            if (dt <= 0.0) dt = 1.0 / 60.0;
            _last = now;

            if (_pending.Count > 0)
            {
                for (int i = 0; i < _pending.Count; i++)
                {
                    if (!_active.Contains(_pending[i])) _active.Add(_pending[i]);
                }
                _pending.Clear();
            }

            for (int i = _active.Count - 1; i >= 0; i--)
            {
                SpringAnimation s = _active[i];
                if (!s.Step(dt)) _active.RemoveAt(i);
            }

            if (_active.Count == 0 && _pending.Count == 0)
            {
                CompositionTarget.Rendering -= OnRendering;
                _clock.Stop();
                _hooked = false;
            }
        }
    }
}
