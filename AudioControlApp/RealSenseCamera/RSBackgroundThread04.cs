using Intel.RealSense;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace AudioControlApp.RealSenseCamera
{
    public abstract class RSBackgroundThread04 : IDisposable
    {
        private int whichDepthW = 848;
        private int whichDepthH = 480;
        
        public static RSBackgroundThread04 CreateForDevice(Sensor whichSensor01, string whichSeralNum01, int whichCamID01, Sensor whichSensor02, string whichSeralNum02, int whichCamID02)
            => new DeviceReadingLoop(whichSensor01, whichSeralNum01, whichCamID01, whichSensor02, whichSeralNum02, whichCamID02);

        protected readonly Thread backgroundThread;
        protected volatile bool isRunning;
        
        //protected volatile FrameQueue fq;
        private static int CAPACITY = 5; // allow max latency of 5 frames
        
        protected RSBackgroundThread04() => backgroundThread = new Thread(BackgroundLoop) { IsBackground = true };

        
        public virtual void Dispose()
        {
            System.Diagnostics.Debug.WriteLine("[RSMultiTHREAD] disposing thread");

            if (isRunning)
            {
                isRunning = false;
                if (backgroundThread.ThreadState != System.Threading.ThreadState.Unstarted)
                    backgroundThread.Join();

                //if (fq != null)
                //{
                //    fq.Dispose();
                //}
            }
        }
        
        protected abstract void BackgroundLoop();

        public void Run()
        {
            if (isRunning)
            {
                System.Diagnostics.Debug.WriteLine("[RSMultiTHREAD] you can only run thread once, silly!");
                throw new InvalidOperationException();
            }
            System.Diagnostics.Debug.WriteLine("[RSMultiTHREAD] starting thread");

            isRunning = true;
            backgroundThread.Start();

            //new Thread(() => startPollingFrameQueue()) { IsBackground = true }.Start();
            //startPollingFrameQueue();
        }

        private void reStart()
        {
            System.Diagnostics.Debug.WriteLine("[RSMultiTHREAD] restarting thread");
            isRunning = false;
            
            
            //if (backgroundThread.ThreadState != System.Threading.ThreadState.Unstarted)
            //    backgroundThread.Join(); // this causes it to hang
  
            
            //backgroundThread.Start(); // this causes exception
            isRunning = true;
            BackgroundLoop();
            
        }
        

        
        public event EventHandler<CaptureReadyEventArgs04> CaptureReady;
        public event EventHandler<Cam0CaptureReadyEventArgs04> Capture0Ready;
        public event EventHandler<Cam1CaptureReadyEventArgs04> Capture1Ready;
        public event EventHandler<ProcessFramesEventArgs04> ProcessFrames;
        //public event EventHandler<MultiCaptureReadyEventArgs04> MultiCaptureReady;
        public event EventHandler<FailedEventArgs04> Failed;

        private sealed class DeviceReadingLoop : RSBackgroundThread04
        {
            private readonly Sensor sensor01;
            private readonly Sensor sensor02;
            private string devSerialNum01;
            private string devSerialNum02;
            private int camID01 = -1;
            private int camID02 = -1;
            private FrameQueue fq;
            private bool isPolling = false;
           

            /*
            public void setSerialNum(string whichSerialNum)
            {
                devSerialNum = whichSerialNum;
            }*/
            
            public DeviceReadingLoop(Sensor whichSensor01, string whichSerialNum01, int whichCamID01, Sensor whichSensor02, string whichSerialNum02, int whichCamID02)
            {
                this.sensor01 = whichSensor01;
                devSerialNum01 = whichSerialNum01;
                camID01 = whichCamID01;
                this.sensor02 = whichSensor02;
                devSerialNum02 = whichSerialNum02;
                camID02 = whichCamID02;
                fq = new FrameQueue(CAPACITY); // initialize queue
            }
            
            public override void Dispose()
            {
                isRunning = false;
                isPolling = false;
                System.Diagnostics.Debug.WriteLine("[RSMultiTHREAD] disposing read loop");
                base.Dispose();
                if (sensor01!=null)
                    sensor01.Dispose();
                if (sensor02!=null)
                    sensor02.Dispose();
            }
            
            private void startPollingFrameQueue()
            {
                isPolling = true;
                while (isRunning)
                {
                    DepthFrame quedframe;
                    if (fq.PollForFrame(out quedframe))
                    {
                        //System.Diagnostics.Debug.WriteLine("[RSThread] frame poll !"+fq.QueueSize());
                        using (quedframe)
                        {
                            CaptureReady?.Invoke(this, new CaptureReadyEventArgs04(quedframe));
                            //quedframe.Dispose(); // this failed to prevent stack empty error...
                        }
                    }
                }
            }

            private void startPollingFrameQueue02()
            {
                isPolling = true;
                while (isRunning)
                {
                    DepthFrame frameFromQueue;
                    if (fq.PollForFrame(out frameFromQueue))
                    {
                        ///System.Diagnostics.Debug.WriteLine("[RSMultiTHREAD] fq " + fq.QueueSize()); // locked on 4 when set to 5
                        using (frameFromQueue)
                        {
                            // Get the serial number of the current frame's device
                            string sn = frameFromQueue.Sensor.Info[CameraInfo.SerialNumber];
                            if (sn == devSerialNum01)
                            {
                                Capture0Ready?.Invoke(this, new Cam0CaptureReadyEventArgs04(frameFromQueue)); // send data from cam 0
                            }
                            else if (sn == devSerialNum02)
                            {
                                Capture1Ready?.Invoke(this, new Cam1CaptureReadyEventArgs04(frameFromQueue)); // send data from cam 1
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine("[RSMultiTHREAD] sn not recognized ");
                            }

                            frameFromQueue.Dispose();

                        }

                    }

                    ProcessFrames?.Invoke(this, new ProcessFramesEventArgs04(true));
                }
            }
            
            private Thread myThread;
            protected override void BackgroundLoop()
            {
                Config config01 = new Config();
                Config config02 = new Config();
                try
                {
                    if (devSerialNum01 != null)
                    {
                        config01.EnableDevice(devSerialNum01);
                        System.Diagnostics.Debug.WriteLine("[RSMultiTHREAD] enabling device #"+camID01+": sn "+devSerialNum01 );
                    }
                    if (devSerialNum02 != null)
                    {
                        config02.EnableDevice(devSerialNum02);
                        System.Diagnostics.Debug.WriteLine("[RSMultiTHREAD] enabling device #"+camID02+": sn "+devSerialNum02 );
                    }

                    config01.EnableStream(Stream.Depth, whichDepthW, whichDepthH, Format.Z16, 30);
                    config02.EnableStream(Stream.Depth, whichDepthW, whichDepthH, Format.Z16, 30);
                    
                    var pipes = new List<Pipeline>();
                    Pipeline pipe01 = new Pipeline();
                    PipelineProfile profile01 = pipe01.Start(config01);  // adding the config here was the key to enabling multiple cameras...
                    AdvancedDevice adv01 = AdvancedDevice.FromDevice(profile01.Device);
                    adv01.JsonConfiguration = System.IO.File.ReadAllText("XML\\primaryCamSettings.json");

                    pipes.Add(pipe01);
                    
                    Pipeline pipe02 = new Pipeline();
                    PipelineProfile profile02 = pipe02.Start(config02);  // adding the config here was the key to enabling multiple cameras...
                    AdvancedDevice adv02 = AdvancedDevice.FromDevice(profile02.Device);
                    adv02.JsonConfiguration = System.IO.File.ReadAllText("XML\\secondaryCamSettings.json");

                    pipes.Add(pipe02);

                    if (!isPolling)
                    {
                        //myThread = new Thread(() => startPollingFrameQueue02()) { IsBackground = true };
                        //myThread.Start();
                        new Thread(() => startPollingFrameQueue()) { IsBackground = true }.Start();
                    }

                    
                    double prevTimestamp = 0;
                    while (isRunning)
                    {
                        foreach (var camPipe in pipes)
                        {
                            /*
                            // this works, but is also dropping frames...
                            using (var frames = camPipe.WaitForFrames())
                            {
                                using (var frameToQueue = frames.DepthFrame)
                                {
                                    if (frameToQueue.Timestamp != prevTimestamp) // make sure it's new?
                                    {
                                        fq.Enqueue(frameToQueue);
                                        prevTimestamp = frameToQueue.Timestamp;
                                    }
                                }
                            }*/
                            
                            // this works but is dropping frames...
                            if (camPipe.PollForFrames(out FrameSet frames))
                            {
                                DepthFrame frameToQueue = frames.DepthFrame;
                                //if (frameToQueue.Timestamp != prevTimestamp) // make sure it's new?
                                //{
                                    fq.Enqueue(frameToQueue);
                                    //prevTimestamp = frameToQueue.Timestamp;
                                //}
                                /*using (frames)
                                {
                                    foreach (var f in frames)
                                        using (f)
                                        using (var p = f.Profile)
                                        {
                                            
                                            Console.WriteLine($"    {p.Stream} {p.Format,4} #{f.Number} {f.TimestampDomain} {f.Timestamp:F2}");
                                        }
                                }*/
                            }

                        }

                        
                        /*
                        DepthFrame frameFromQueue;
                        if (fq.PollForFrame(out frameFromQueue))
                        {
                            System.Diagnostics.Debug.WriteLine("[RSMultiTHREAD] fq "+fq.QueueSize()); // this swings between 9 and 3
                            using (frameFromQueue)
                            {
                                // Get the serial number of the current frame's device
                                string sn =  frameFromQueue.Sensor.Info[CameraInfo.SerialNumber];
                                if (sn == devSerialNum01)
                                {
                                    Capture0Ready?.Invoke(this, new Cam0CaptureReadyEventArgs04(frameFromQueue)); // send data from cam 0
                                }
                                else if (sn == devSerialNum02)
                                {
                                    Capture1Ready?.Invoke(this, new Cam1CaptureReadyEventArgs04(frameFromQueue)); // send data from cam 1
                                }
                                else
                                {
                                    System.Diagnostics.Debug.WriteLine("[RSMultiTHREAD] sn not recognized ");
                                }
                                
                               //frameFromQueue.Dispose();
                                
                            }
                            
                        }
                        ProcessFrames?.Invoke(this, new ProcessFramesEventArgs04(true));*/
                        
                        //using (var frames = pipe.WaitForFrames())
                        //{
                        //    using (var depth = frames.DepthFrame)
                        //    {
                        //        fq.Enqueue(depth); // using poll method
                                //depth.Dispose(); // this causes crash on ReadingLoop_CaptureReady0 (disposed object)
                                //CaptureReady?.Invoke(this, new CaptureReadyEventArgs03(depth));
                        //    }
                        //}
                    } // end of endless loop
                    foreach (var pipeToKill in pipes)
                    {
                        pipeToKill.Stop();
                    }
                }
                catch (Exception exc)
                {
                    isRunning = false; // this should stop the thread...
                    //isPolling = false;
                    
                    //myThread.Abort();
                    //myThread = null;
                    config01.DisableAllStreams();
                    config01.Dispose();
                    config02.DisableAllStreams();
                    config02.Dispose();
                    
                    System.Diagnostics.Debug.WriteLine("[RSMultiTHREAD] exception: [" + exc.Message + " ]");
                    //reStart();
                }
            }
        }
    }
    
    public sealed class FailedEventArgs04 : EventArgs
    {
        public FailedEventArgs04(Exception exception)
            => Exception = exception;

        public Exception Exception { get; }
    }

    public sealed class CaptureReadyEventArgs04 : EventArgs
    {
        public CaptureReadyEventArgs04(DepthFrame depth)
            => Capture = depth;

        public DepthFrame Capture { get; }
    }
    
    public sealed class Cam0CaptureReadyEventArgs04 : EventArgs
    {
        public Cam0CaptureReadyEventArgs04(DepthFrame depth)
            => Capture = depth;

        public DepthFrame Capture { get; }
    }
    
    public sealed class Cam1CaptureReadyEventArgs04 : EventArgs
    {
        public Cam1CaptureReadyEventArgs04(DepthFrame depth)
            => Capture = depth;

        public DepthFrame Capture { get; }
    }

    public sealed class ProcessFramesEventArgs04 : EventArgs
    {
        public ProcessFramesEventArgs04(bool doProcess)
            => Capture = doProcess;
        public bool Capture { get; }
    }

    /*
    public sealed class MultiCaptureReadyEventArgs04 : EventArgs
    {
        public MultiCaptureReadyEventArgs04(FrameSet depthFrames)
            => Capture = depthFrames;

        public FrameSet Capture { get; }
    }*/

}