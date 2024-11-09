using System;
using System.Diagnostics;
using PFRelay.Util; // Import LoggerHelper

namespace PFRelay.Util
{
    public class TimedBool
    {
        private bool running;
        private readonly float time;
        private readonly Stopwatch watch;

        private static DateTime nextStopLogTime = DateTime.MinValue; // Timestamp for the next allowed stop log
        private const int StopLogCooldownSeconds = 120; // Cooldown period in seconds for stop log messages

        public TimedBool(float time)
        {
            try
            {
                watch = new Stopwatch();
                running = false;
                this.time = time;
                LoggerHelper.LogDebug($"TimedBool initialized with time: {time} seconds.");
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("Error initializing TimedBool", ex);
                throw; // Re-throw to indicate an issue during initialization
            }
        }

        public bool Value
        {
            get
            {
                try
                {
                    var sw = watch.Elapsed.TotalSeconds < time;
                    if (!sw) Stop();
                    return running && sw;
                }
                catch (Exception ex)
                {
                    LoggerHelper.LogError("Error checking TimedBool value", ex);
                    return false;
                }
            }
        }

        public TimedBool Start()
        {
            try
            {
                running = true;
                watch.Restart();
                LoggerHelper.LogDebug("TimedBool started.");
                return this;
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("Error starting TimedBool", ex);
                throw; // Re-throw to indicate an issue starting the timer
            }
        }

        public TimedBool Stop()
        {
            try
            {
                running = false;
                watch.Stop();

                // Log "TimedBool stopped" only if the cooldown period has passed
                if (DateTime.UtcNow >= nextStopLogTime)
                {
                    LoggerHelper.LogDebug("TimedBool stopped.");
                    nextStopLogTime = DateTime.UtcNow.AddSeconds(StopLogCooldownSeconds);
                }

                return this;
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("Error stopping TimedBool", ex);
                throw; // Re-throw to indicate an issue stopping the timer
            }
        }
    }
}
