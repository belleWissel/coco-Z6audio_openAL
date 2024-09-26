using Intel.RealSense;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace AudioControlApp.RealSenseCamera
{
    public abstract class RSBackgroundThreadSingleCam03 : IDisposable
    {
        private int whichDepthW = 640; //640; //848; // has to be changed in RSCameraData AND cam control
        private int whichDepthH = 360; //360; //480;
        private static int cameraFrameRate = 15; // 5 or 15 or 30 fps only...
        
        public static RSBackgroundThreadSingleCam03 CreateForDevice(Sensor whichSensor01, string whichSeralNum01, int whichCamID01)
            => new DeviceReadingLoop03(whichSensor01, whichSeralNum01, whichCamID01);

        protected readonly Thread backgroundThread;
        protected volatile bool isRunning;
        
        //protected volatile FrameQueue fq;
        private static int CAPACITY = 1; // allow max latency of 5 frames
        
        protected RSBackgroundThreadSingleCam03() => backgroundThread = new Thread(BackgroundLoop) { IsBackground = true };

        public event EventHandler<CaptureReady01EventArgsSingleCam03> CaptureReady01;
        //public event EventHandler<CaptureReady02EventArgsSingleCam03> CaptureReady02;
        public event EventHandler<FailedEventArgsSingleCam03> Failed;

        protected abstract void BackgroundLoop();
        
        // recover from camera loss or fault
        public bool errorEncountered = false;
        public string errorMessage = "NA";

        public virtual void Dispose()
        {
            System.Diagnostics.Debug.WriteLine("[RSMultiTHREAD] disposing thread");

            if (isRunning)
            {
                isRunning = false;
                if (backgroundThread.ThreadState != System.Threading.ThreadState.Unstarted)
                    backgroundThread.Join();
            }
        }

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
        }
        
        public void resetAndRestartCameras()
        {
            System.Diagnostics.Debug.WriteLine("[RSMultiTHREAD] disposing thread");
            
            if (isRunning)
            {
                isRunning = false;
                if (backgroundThread.ThreadState != System.Threading.ThreadState.Unstarted)
                    backgroundThread.Join();
            }
            
            
            System.Diagnostics.Debug.WriteLine("[RSMultiTHREAD] starting thread");

            isRunning = true;
            backgroundThread.Start();
        }

        private sealed class DeviceReadingLoop03 : RSBackgroundThreadSingleCam03
        {
            private readonly Sensor sensor01;
            //private readonly Sensor sensor02;
            private string devSerialNum01;
            //private string devSerialNum02;
            private int camID01 = -1;
            //private int camID02 = -1;

            public DeviceReadingLoop03(Sensor whichSensor01, string whichSerialNum01, int whichCamID01)
            {
                this.sensor01 = whichSensor01;
                devSerialNum01 = whichSerialNum01;
                camID01 = whichCamID01;
                //this.sensor02 = whichSensor02;
                //devSerialNum02 = whichSerialNum02;
                //camID02 = whichCamID02;
            }
            
            public override void Dispose()
            {
                isRunning = false;
                System.Diagnostics.Debug.WriteLine("[RSMultiTHREAD] disposing read loop");
                base.Dispose();
                if (sensor01!=null)
                    sensor01.Dispose();
                //if (sensor02!=null)
                //    sensor02.Dispose();
            }

            protected override void BackgroundLoop()
            {
                Config config01 = new Config();
                //Config config02 = new Config();
                try
                {
                    if (devSerialNum01 != null)
                    {
                        config01.EnableDevice(devSerialNum01);
                        System.Diagnostics.Debug.WriteLine("[RSMultiTHREAD] enabling device #"+camID01+": sn "+devSerialNum01 );
                    }
                    else
                    {
                        errorEncountered = true;
                        errorMessage = "[RSMultiTHREAD] nomatch for dev 1";
                        return;
                    }
                    
                    /*
                    if (devSerialNum02 != null)
                    {
                        config02.EnableDevice(devSerialNum02);
                        System.Diagnostics.Debug.WriteLine("[RSMultiTHREAD] enabling device #"+camID02+": sn "+devSerialNum02 );
                    }
                    else
                    {
                        errorEncountered = true;
                        errorMessage = "[RSMultiTHREAD] nomatch for dev 2";
                        return;
                    }*/
                    config01.EnableStream(Stream.Depth, whichDepthW, whichDepthH, Format.Z16, cameraFrameRate); 
                    //config02.EnableStream(Stream.Depth, whichDepthW, whichDepthH, Format.Z16, cameraFrameRate);
                    
                    var pipes = new List<Pipeline>();
                    Pipeline pipe01 = new Pipeline(); 
                    //Pipeline pipe02 = new Pipeline();
                    pipes.Add(pipe01);
                    //pipes.Add(pipe02);
                    
                    PipelineProfile profile01 = pipe01.Start(config01);  // adding the config here was the key to enabling multiple cameras...
                    //PipelineProfile profile02 = pipe02.Start(config02);  // adding the config here was the key to enabling multiple cameras...
                    
                    while (isRunning) // begin endless loop
                    {
                        foreach (var camPipe in pipes) // when using pipe collection, do we waitForFrames or PollForFrames? (Poll)
                        {
                            if (camPipe.PollForFrames(out FrameSet frames))
                            {
                                //if (frames.Sensor.Info[CameraInfo.SerialNumber] == devSerialNum01)
                                //{
                                    CaptureReady01?.Invoke(this, new CaptureReady01EventArgsSingleCam03(frames.DepthFrame));
                                //}
                                //else
                                //{
                                //    CaptureReady02?.Invoke(this, new CaptureReady02EventArgsDualCam03(frames.DepthFrame));
                                //}
                            }
                        }
                    } // end of endless loop
                    
                    foreach (var pipeToKill in pipes)
                    {
                        pipeToKill.Stop();
                        pipeToKill.Dispose();
                    }
                }
                catch (Exception exc)
                {
                    isRunning = false; // this should stop the loops...
                    
                    config01.DisableAllStreams();
                    config01.Dispose();
                    //config02.DisableAllStreams();
                    //config02.Dispose();

                    errorEncountered = true;
                    errorMessage = "[RSMultiTHREAD] exception: " + exc.Message;
                    
                    System.Diagnostics.Debug.WriteLine("[RSMultiTHREAD] exception: [" + exc.Message + " ]");
                }
            }
        }
    }
    
    public sealed class FailedEventArgsSingleCam03 : EventArgs
    {
        public FailedEventArgsSingleCam03(Exception exception)
            => Exception = exception;

        public Exception Exception { get; }
    }

    public sealed class CaptureReady01EventArgsSingleCam03 : EventArgs
    {
        public CaptureReady01EventArgsSingleCam03(DepthFrame depth)
            => Capture = depth;

        public DepthFrame Capture { get; }
    }
    
    /*
    public sealed class CaptureReady02EventArgsSingleCam03 : EventArgs
    {
        public CaptureReady02EventArgsSingleCam03(DepthFrame depth)
            => Capture = depth;

        public DepthFrame Capture { get; }
    }*/

}