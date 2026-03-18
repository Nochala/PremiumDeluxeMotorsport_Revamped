using GTA;

namespace PDMCD4
{
    public class Timer
    {
        public bool Enabled { get; set; }
        public int Interval { get; set; }
        public int Waiter { get; set; }

        public Timer(int interval)
        {
            Interval = interval;
            Waiter = 0;
            Enabled = false;
        }

        public Timer()
        {
            Interval = 0;
            Waiter = 0;
            Enabled = false;
        }

        public void Start()
        {
            Waiter = Game.GameTime + Interval;
            Enabled = true;
        }

        public void Reset()
        {
            Waiter = Game.GameTime + Interval;
        }
    }
}
