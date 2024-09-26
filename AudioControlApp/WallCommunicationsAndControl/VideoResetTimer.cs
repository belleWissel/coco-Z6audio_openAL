using System;
using System.Collections.Generic;
using System.Text;
// using local timer to update screen
using System.Timers; // high(er) performance timer
using SecondstoryCommon;

namespace SensorControlApp.WallCommunicationsAndControl
{
    class VideoResetTimer
    {
        // for passsing events to MainClass
        public event AppEventHandler OnEvent;
        private string myEventSource = "timerControl";
        //System.Timers.Timer checkStatusTimer;
        
        System.Timers.Timer localTimer;

        bool isRepeating = false;

        public bool isRunning = false;

        // **************************************
        // GET/SET has state changed?
        // **************************************
        public bool dirty = false;
        public bool isDirty()
        {
            bool valueToReturn = dirty;
            if (dirty)
            {
                dirty = false; // clean it
            }
            return valueToReturn;
        }

        public VideoResetTimer(string whichEventSource, double whichExpirationTime, bool whichRepeat)
        {
            myEventSource = whichEventSource;
            isRepeating = whichRepeat;

            localTimer = new System.Timers.Timer();
            localTimer.Interval = (double)whichExpirationTime * 1000.0;
            localTimer.AutoReset = true;
            localTimer.Elapsed += new ElapsedEventHandler(localTimer_Tick);
        }

        private void localTimer_Tick(object sender, EventArgs e)
        {
            if (!isRepeating)
            {
                stopTimer();
            }
            dirty = true;
            sendTimerExpired();
        }

        public void startTimer()
        {
            localTimer.Stop();
            localTimer.Start();
            isRunning = true;
        }

        public void stopTimer()
        {
            localTimer.Stop();
            isRunning = false;

        }

        public void restart()
        {
            localTimer.Stop();
            localTimer.Start();
            isRunning = true;
        }

        public void adjustTimerTime(int whichExpirationTime)
        {
            localTimer.Interval = (double)whichExpirationTime * 1000.0;
        }

        public void killTimer()
        {
            isRunning = false;
            localTimer.Stop();
            localTimer.Dispose();
        }

        private void sendTimerExpired()
        {
            
            AppEvent evtData = new AppEvent();
            evtData.EventSource = myEventSource;
            evtData.EventString = "videoTimerExpired";
            OnEvent(this, evtData);
        }
    }
}
