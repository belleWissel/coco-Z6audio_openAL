using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Threading;


namespace AudioControlApp.Utils
{
    class HiPerfTimer
    {
        [DllImport("Kernel32.dll")]
        private static extern bool QueryPerformanceCounter(
            out long lpPerformanceCount);

        [DllImport("Kernel32.dll")]
        private static extern bool QueryPerformanceFrequency(
            out long lpFrequency);

        private long startTime, stopTime, elapsed;
        private long freq;
        private double freq_dbl;
        public long longLimit;

        // Constructor
        public HiPerfTimer()
        {
            startTime = 0;
            stopTime = 0;

            if (QueryPerformanceFrequency(out freq) == false)
            {
                // high-performance counter not supported
                throw new Win32Exception();
            }

            freq_dbl = (double)freq; // store locally as a double
        }

        public long getLongLimit(double whichFrameRate)
        {
            double valueToReturnf = whichFrameRate * (double)freq;
            long valueToReturn = (long)Math.Floor(valueToReturnf);
            return valueToReturn;
        }

        // Start the timer
        public void Start()
        {
            // lets do the waiting threads there work
            Thread.Sleep(0);
            QueryPerformanceCounter(out startTime);
        }

        // Stop the timer
        public void Stop()
        {
            QueryPerformanceCounter(out stopTime);
        }

        // how much time has elapsed
        public double Elapsed()
        {
            QueryPerformanceCounter(out elapsed);
            //get
            //{
            //double valueToReturn = (double)((elapsed - startTime) / freq);
            //return valueToReturn;
            //}
            return (double)(elapsed - startTime) / freq_dbl;
        }
        // how much time has elapsed (LONG version)
        public long ElapsedL()
        {
            QueryPerformanceCounter(out elapsed);
            return (elapsed - startTime);
        }

        // Returns the duration of the timer (in seconds)
        public double Duration
        {
            get
            {
                return (double)(stopTime - startTime) / freq_dbl;
            }
        }
    }
}
