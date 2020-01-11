using System.Threading;

namespace Sayo_Installer
{
    class CountDown
    {
        public CountDown(uint ticks)
        {
            this.defaultTime = this.ctime = ticks + 1;
            AutoResetEvent autoEvent = new AutoResetEvent(false);
            timer = new Timer(this.Tick, autoEvent, Timeout.Infinite, 1000);
            this.interval = 1000;
        }

        // interval 单位为ms
        public CountDown(uint ticks, uint interval)
        {
            this.defaultTime = this.ctime = ticks + 1;
            AutoResetEvent autoEvent = new AutoResetEvent(false);
            timer = new Timer(this.Tick, autoEvent, Timeout.Infinite, interval);
            this.interval = interval;
        }

        private uint ctime { get; set; }
        private uint defaultTime { get; set; }
        private uint interval { get; set; }
        private Timer timer;

        public delegate void OnTick(uint t);
        public delegate void OnTimerDone();
        public event OnTick OnTickEvent;
        public event OnTimerDone OnTimerDoneEvent;

        public void Start()
        {
            timer.Change(0, this.interval);
        }

        public void Pause()
        {
            timer.Change(Timeout.Infinite, this.interval);
        }

        // 强制结束
        public void Done()
        {
            timer.Change(Timeout.Infinite, this.interval);
            OnTimerDoneEvent?.Invoke();
        }

        public void Reset()
        {
            timer.Change(Timeout.Infinite, this.interval);
            this.ctime = this.defaultTime;
        }

        private void Tick(object stateInfo)
        {
            AutoResetEvent autoEvent = (AutoResetEvent)stateInfo;
            OnTickEvent?.Invoke(--ctime);
            if (this.ctime == 0)
            {
                Done();
                autoEvent.Set();
            }
        }

        ~CountDown()
        {
            timer.Dispose();
        }
    }
}