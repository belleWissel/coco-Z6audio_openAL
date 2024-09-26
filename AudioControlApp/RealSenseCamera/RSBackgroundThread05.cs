using Intel.RealSense;

using System;
using System.Diagnostics;
using System.Threading;

namespace AudioControlApp.RealSenseCamera
{
    public abstract class RSBackgroundThread05 : IDisposable
    {
        private int whichDepthW = 848;
        private int whichDepthH = 480;
        
        public static RSBackgroundThread05 CreateForDevice(Sensor whichSensor, string whichSeralNum, int whichCamID)
            => new DeviceReadingLoop(whichSensor, whichSeralNum, whichCamID);

        protected readonly Thread backgroundThread;
        protected volatile bool isRunning;
        
        //protected volatile FrameQueue fq;
        private static int CAPACITY = 2; // allow max latency of 5 frames
        
        protected RSBackgroundThread05() => backgroundThread = new Thread(BackgroundLoop) { IsBackground = true };

        
        public virtual void Dispose()
        {
            System.Diagnostics.Debug.WriteLine("[RSThread] disposing thread");

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
                System.Diagnostics.Debug.WriteLine("[RSThread] you can only run thread once, silly!");
                throw new InvalidOperationException();
            }
            System.Diagnostics.Debug.WriteLine("[RSThread] starting thread");

            isRunning = true;
            backgroundThread.Start();

            //new Thread(() => startPollingFrameQueue()) { IsBackground = true }.Start();
            //startPollingFrameQueue();
        }

        private void reStart()
        {
            System.Diagnostics.Debug.WriteLine("[RSTHREAD] restarting thread");
            isRunning = false;
            
            
            //if (backgroundThread.ThreadState != System.Threading.ThreadState.Unstarted)
            //    backgroundThread.Join(); // this causes it to hang
  
            
            //backgroundThread.Start(); // this causes exception
            isRunning = true;
            BackgroundLoop();
            
        }
        

        
        public event EventHandler<CaptureReadyEventArgs05> CaptureReady;
        public event EventHandler<FailedEventArgs05> Failed;

        private sealed class DeviceReadingLoop : RSBackgroundThread05
        {
            private readonly Sensor sensor;
            private string devSerialNum;
            private int camID = -1;
            private FrameQueue fq;
            private bool isPolling = false;
           

            /*
            public void setSerialNum(string whichSerialNum)
            {
                devSerialNum = whichSerialNum;
            }*/
            
            public DeviceReadingLoop(Sensor whichSensor, string whichSerialNum, int whichCamID)
            {
                this.sensor = whichSensor;
                devSerialNum = whichSerialNum;
                camID = whichCamID;
                fq = new FrameQueue(CAPACITY); // initialize queue
            }
            
            public override void Dispose()
            {
                System.Diagnostics.Debug.WriteLine("[RSThread] disposing read loop");
                base.Dispose();
                if (sensor!=null)
                    sensor.Dispose();
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
                            CaptureReady?.Invoke(this, new CaptureReadyEventArgs05(quedframe));
                            //quedframe.Dispose(); // this failed to prevent stack empty error...
                        }
                    }
                    
                    
                }
            }

            private void startPollingFrameQueue02() // performing this functionoutside the loop causes STACK EMPTY error
            {
                DepthFrame frameFromQueue;
                isPolling = true;
                while (isRunning)
                {
                    if (fq.PollForFrame(out frameFromQueue))
                    {
                        //System.Diagnostics.Debug.WriteLine("[RSMultiTHREAD] fq "+fq.QueueSize()); // this swings between 9 and 3
                        using (frameFromQueue)
                        {
                            // Get the serial number of the current frame's device
                            //string sn =  frameFromQueue.Sensor.Info[CameraInfo.SerialNumber];
                            //if (sn == devSerialNum01)
                            //{
                            CaptureReady?.Invoke(this, new CaptureReadyEventArgs05(frameFromQueue)); // send data from cam 0
                            //}


                            //frameFromQueue.Dispose();

                        }

                    }
                }
            }
            /*
            private void restartBackgroundLoop()
            {
                System.Diagnostics.Debug.WriteLine("[RSTHREAD] restarting read loop");
                
                backgroundThread.Abort();
                backgroundThread.Start();
                isRunning = true;
            }*/

            private void getFrameFromQueueOnDemand()
            {
                DepthFrame frameFromQueue;
                if (fq.PollForFrame(out frameFromQueue))
                {
                    //System.Diagnostics.Debug.WriteLine("[RSMultiTHREAD] fq "+fq.QueueSize()); // this swings between 9 and 3
                    using (frameFromQueue)
                    {
                        // Get the serial number of the current frame's device
                        //string sn =  frameFromQueue.Sensor.Info[CameraInfo.SerialNumber];
                        //if (sn == devSerialNum01)
                        //{
                        CaptureReady?.Invoke(this, new CaptureReadyEventArgs05(frameFromQueue)); // send data from cam 0
                        //}
                           
                                
                        //frameFromQueue.Dispose();
                                
                    }
                            
                }
            }
            
            
            private Thread myThread;
            protected override void BackgroundLoop()
            {
                Config config = new Config();
                try
                {
                    

                    if (devSerialNum != null)
                    {
                        config.EnableDevice(devSerialNum);
                        System.Diagnostics.Debug.WriteLine("[RSTHREAD] enabling device #"+camID+": sn "+devSerialNum );
                    }
                    
                    //string cameraDataSettingsFileToRead = "XML\\primaryCamSettings.json"; // TODO try different camera settings here
                    //if (camID == 1)
                    //    cameraDataSettingsFileToRead = "XML\\secondaryCamSettings.json";
                    
                    //string cameraDataSettingsFileToRead = "XML\\highDensitySettingsFromViewer.json"; // override with new settings for both...
                    string cameraDataSettingsFileToRead = "XML\\highAccuracySettingsFromViewer.json"; // override with new settings for both...
                    
                    config.EnableStream(Stream.Depth, whichDepthW, whichDepthH, Format.Z16, 15);
                    
                    Pipeline pipe = new Pipeline();
                    PipelineProfile profile = pipe.Start(config);  // adding the config here was the key to enabling multiple cameras...
                    AdvancedDevice adv = AdvancedDevice.FromDevice(profile.Device);
                    adv.JsonConfiguration = System.IO.File.ReadAllText(cameraDataSettingsFileToRead);

                    //if (!isPolling)
                    //{
                    //    myThread = new Thread(() => startPollingFrameQueue()) { IsBackground = true };
                    //    myThread.Start();
                        //new Thread(() => startPollingFrameQueue()) { IsBackground = true }.Start();
                    //}
                    //else
                    //{
                    //    startPollingFrameQueue();
                    //}
                    
                    double prevTimestamp = 0;
                    while (isRunning)
                    {
                        if (pipe.PollForFrames(out FrameSet frames))  // described in detail here https://intelrealsense.github.io/librealsense/doxygen/classrs2_1_1pipeline.html#a9069a979fd9e1e28881b945a6aec1b79
                        {
                            DepthFrame frameToQueue = frames.DepthFrame;
                            //if (frameToQueue.Timestamp != prevTimestamp) // make sure it's new? (doesn't seem to have any effect)
                            //{
                                fq.Enqueue(frameToQueue);
                                //prevTimestamp = frameToQueue.Timestamp;
                            //}

                            getFrameFromQueueOnDemand();
                        }
                        else
                        {
                            // testing... this happens a lot:
                            //System.Diagnostics.Debug.WriteLine("[RSTHREAD] no update from device #"+camID); // this swings between 9 and 3
                        }
                        
                        /*
                        using (var frames = pipe.WaitForFrames())  // this results in error [Frame didn't arrive within 5000 ]
                        {
                            using (var depth = frames.DepthFrame)
                            {
                                fq.Enqueue(depth); // using poll method
                                //depth.Dispose(); // this causes crash on ReadingLoop_CaptureReady0 (disposed object)
                                //CaptureReady?.Invoke(this, new CaptureReadyEventArgs03(depth));
                            }
                        }
                        */
                        
                        /*
                        DepthFrame frameFromQueue;
                        if (fq.PollForFrame(out frameFromQueue))
                        {
                            //System.Diagnostics.Debug.WriteLine("[RSMultiTHREAD] fq "+fq.QueueSize()); // this swings between 9 and 3
                            using (frameFromQueue)
                            {
                                // Get the serial number of the current frame's device
                                //string sn =  frameFromQueue.Sensor.Info[CameraInfo.SerialNumber];
                                //if (sn == devSerialNum01)
                                //{
                                CaptureReady?.Invoke(this, new CaptureReadyEventArgs05(frameFromQueue)); // send data from cam 0
                                //}
                           
                                
                                //frameFromQueue.Dispose();
                                
                            }
                            
                        }*/
                        
                    }
                    
                    pipe.Stop();
                    pipe.Dispose();
                }
                catch (Exception exc)
                {
                    isRunning = false; // this should stop the thread...
                    //isPolling = false;
                    
                    //myThread.Abort();
                    //myThread = null;
                    config.DisableAllStreams();
                    config.Dispose();
                    
                    System.Diagnostics.Debug.WriteLine("[RSTHREAD] exception on dev sn "+devSerialNum +": [" + exc.Message + " ]");
                    System.Diagnostics.Debug.WriteLine("[RSTHREAD] fq size= "+fq.QueueSize()); //
                    reStart();
                }
            }
        }
    }
    
    public sealed class FailedEventArgs05 : EventArgs
    {
        public FailedEventArgs05(Exception exception)
            => Exception = exception;

        public Exception Exception { get; }
    }

    public sealed class CaptureReadyEventArgs05 : EventArgs
    {
        public CaptureReadyEventArgs05(DepthFrame depth)
            => Capture = depth;

        public DepthFrame Capture { get; }
    }

}