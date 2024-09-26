using Intel.RealSense;

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace AudioControlApp.RealSenseCamera
{
    public abstract class RSBackgroundThread : IDisposable
    {
        private int whichDepthW = 848;
        private int whichDepthH = 480;
        
        public static RSBackgroundThread CreateForDevice(Sensor whichSensor, string whichSeralNum)
            => new DeviceReadingLoop(whichSensor, whichSeralNum);

        protected readonly Thread backgroundThread;
        protected volatile bool isRunning;
        
        protected RSBackgroundThread()
            => backgroundThread = new Thread(BackgroundLoop) { IsBackground = true };


        public virtual void Dispose()
        {
            System.Diagnostics.Debug.WriteLine("[RSThread] disposing thread");

            if (isRunning)
            {
                isRunning = false;
                if (backgroundThread.ThreadState != System.Threading.ThreadState.Unstarted)
                    backgroundThread.Join();
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
        }
        
        public event EventHandler<CaptureReadyEventArgs> CaptureReady;

        public event EventHandler<FailedEventArgs> Failed;

        private sealed class DeviceReadingLoop : RSBackgroundThread
        {
            private readonly Sensor sensor;
            private string devSerialNum;

            public void setSerialNum(string whichSerialNum)
            {
                devSerialNum = whichSerialNum;
            }
            public DeviceReadingLoop(Sensor whichSensor, string whichSerialNum)
            {
                this.sensor = whichSensor;
                devSerialNum = whichSerialNum;
                //DepthMode = depthMode;
                //ColorResolution = colorResolution;
                //FrameRate = frameRate;
            }
            
            public override void Dispose()
            {
                System.Diagnostics.Debug.WriteLine("[RSThread] disposing read loop");
                base.Dispose();
                if (sensor!=null)
                    sensor.Dispose();
            }

            private DepthFrame depth;
            
            protected override void BackgroundLoop()
            {
                try
                {
                    //string jsonContents = System.IO.File.ReadAllText("XML\\highAccuracySettingsFromViewer.json");
                    
                    // trying to get more than one camera to turn on:
                    // taken from https://github.com/IntelRealSense/librealsense/issues/3432
                    Config config = new Config();
                    //string sn = this.sensor.Info[CameraInfo.SerialNumber]; // this crashes (exception)
                    if (devSerialNum != null)
                    {
                        config.EnableDevice(devSerialNum);
                        System.Diagnostics.Debug.WriteLine("[RSTHREAD] enabling device: sn "+devSerialNum );
                    }

                    //var cfg = new Config();
                    config.EnableStream(Stream.Depth, whichDepthW, whichDepthH, Format.Z16, 15);
                    
                    
                    //config.EnableStream(Stream.Color, Format.Rgb8);
                    //config.DisableStream(Stream.Color);
                    
                    //cfg.EnableStream(Stream.Depth, 0);
            
                    
                    Pipeline pipe = new Pipeline();
                    PipelineProfile profile = pipe.Start(config);  // adding the config here was the key to enabling multiple cameras...
                    AdvancedDevice adv = AdvancedDevice.FromDevice(profile.Device);
                    adv.JsonConfiguration = System.IO.File.ReadAllText("XML\\highAccuracySettingsFromViewer.json");
                    
                    //var adv = AdvancedDevice.FromDevice(profil)
                    //var pc = new PointCloud();
                    
                    //pipe.Start(cfg);
                    //PipelineProfile profile = pipe.Start(config); // using config crashes camera after a few frames?

                    //int depthImageWidth = 848;
                    //int depthImageHeight = 480;
                    
                    //ushort[] depthData = new ushort[depthImageWidth*depthImageHeight];

                    //DepthFrame depth;
                    //depth.
                    
                    while (isRunning)
                    {
                        using (var frames = pipe.WaitForFrames())
                        {
                            using (depth = frames.DepthFrame)
                            {
                                
                                
                                /*
                                using (var points = pc.Process(depth).As<Points>())
                                {
                                    var vertices = new float[points.Count * 3];
                                    points.CopyVertices(vertices);
                                    CaptureReady?.Invoke(this, new CaptureReadyEventArgs(vertices));
                                }
                                */
                                
                                //System.Diagnostics.Debug.WriteLine("received depth frame with resolution [" + depth.Width +", " + depth.Height + " ]");
                                //System.Diagnostics.Debug.WriteLine("The camera is pointing at an object " +
                                //                                   depth.GetDistance(depth.Width / 2, depth.Height / 2) +
                                //                                   " meters away");
                                
                                // unsafe code block to avoid marshaling frame data
                                
                                //System.Diagnostics.Debug.WriteLine("received depth frame with resolution [" + depth.Width +", " + depth.Height + " ]");
                                //System.Diagnostics.Debug.WriteLine("received depth frame with size [" + depth.DataSize + " ]");

                                //long fps = depth.GetFrameMetadata(FrameMetadataValue.ActualFps);
                                //System.Diagnostics.Debug.WriteLine("current fps [" + fps + " ]");

                                CaptureReady?.Invoke(this, new CaptureReadyEventArgs(depth));
                                
                                //Marshal.Copy(depth.Data, localDepthCopy.Data, 0, depth.DataSize);

                                /*
                                using (ushort[] depthData = new ushort[depthImageWidth * depthImageHeight])
                                {
                                    unsafe
                                    {
                                        ushort* depth_data = (ushort*) depth.Data.ToPointer();
                                        for (int i = 0; i < depth.Width * depth.Height; ++i)
                                        {
                                            depthData[i] = depth_data[i];
                                        }
                                    }

                                    CaptureReady?.Invoke(this, new CaptureReadyEventArgs(depthData));
                                }*/
                            }
                        }
                    }
                    /*
                    device.StartCameras(new DeviceConfiguration
                    {
                        CameraFps = FrameRate,
                        ColorFormat = ImageFormat.ColorBgra32,
                        ColorResolution = ColorResolution,
                        DepthMode = DepthMode,
                        WiredSyncMode = WiredSyncMode.Standalone,
                    });

                    while (isRunning)
                    {
                        var res = device.TryGetCapture(out var capture);
                        if (res)
                        {
                            using (capture)
                            {
                                CaptureReady?.Invoke(this, new CaptureReadyEventArgs(capture));
                            }
                        }
                        else
                        {
                            if (!device.IsConnected)
                                throw new DeviceConnectionLostException(device.DeviceIndex);
                            Thread.Sleep(1);
                        }
                    }*/
                }
                catch (Exception exc)
                {
                    //
                    System.Diagnostics.Debug.WriteLine("[RSTHREAD] exception on dev sn "+devSerialNum +": [" + exc.Message + " ]");
                }
            }
        }
    }
    
    public sealed class FailedEventArgs : EventArgs
    {
        public FailedEventArgs(Exception exception)
            => Exception = exception;

        public Exception Exception { get; }
    }

    public sealed class CaptureReadyEventArgs : EventArgs
    {
        public CaptureReadyEventArgs(DepthFrame depth)
            => Capture = depth;

        public DepthFrame Capture { get; }
    }
    /*
    public sealed class CaptureReadyEventArgs : EventArgs
    {
        public CaptureReadyEventArgs(Capture capture)
            => Capture = capture;

        public Capture Capture { get; }
    }*/
}