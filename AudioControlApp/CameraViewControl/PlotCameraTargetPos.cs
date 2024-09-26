using System;
using System.Collections.Generic;
using System.Text;

namespace AudioControlApp.CameraViewControl
{
    class PlotCameraTargetPos
    {
        public double targetAzimuth = 0;
        public double targetElevation = 0;
        public double range = 4500;
        public double lookAtX = 0;
        public double lookAtY = 0;
        public double lookAtZ = 0;

        private double maxRange = 7000;
        private double minRange = 2000;

        private double maxElevationAngle = 85;
        private double minElevationAngle = 5;

        // position of camera is shared with mousevelcalculator
        public double xPos, yPos, zPos = 0;

        public double aziOffset = 0;


        private System.Timers.Timer animationFrameTimer = new System.Timers.Timer();
        private bool orbitingCamera = false;

        private double framesToRoundPark = 820;
        //private double framesToRoundPark = 72000;

        public PlotCameraTargetPos()
        {
            animationFrameTimer.Interval = 33;
            animationFrameTimer.Elapsed += new System.Timers.ElapsedEventHandler(animateCameraStep);
        }

        public void udpateCameraPos(double whichAzi, double whichElev)
        {
            targetAzimuth = whichAzi * 360;
            targetElevation = minElevationAngle + (whichElev * (maxElevationAngle - minElevationAngle));

            plotCameraObject();
        }

        public void toggleOrbitingCamera()
        {
            if (orbitingCamera)
            {
                orbitingCamera = false;
                animationFrameTimer.Stop();
            }
            else
            {
                orbitingCamera = true;
                animationFrameTimer.Start();
                targetElevation = 35;
            }
        }

        public void startStopOrbitingCamera(bool startingOrbit)
        {
            if (startingOrbit)
            {
                if (!orbitingCamera)
                {
                    orbitingCamera = true;
                    animationFrameTimer.Start();
                    targetElevation = 35;
                }
            }
            else
            {
                if (orbitingCamera)
                {
                    orbitingCamera = false;
                    animationFrameTimer.Stop();
                }
            }
        }

        private void animateCameraStep(object sender, EventArgs e)
        {
            targetAzimuth += 360 / framesToRoundPark;
            if (targetAzimuth > 360)
            {
                targetAzimuth -= 360;
            }
            else if (targetAzimuth < 0)
            {
                targetAzimuth += 360;
            }
            plotCameraObject();
        }

        public void plotCameraObject()
        {
            yPos = range * Math.Sin(targetElevation * Math.PI / 180);
            yPos += lookAtY;
            double trans = range * Math.Cos(targetElevation * Math.PI / 180);

            xPos = trans * Math.Cos((targetAzimuth + aziOffset) * Math.PI / 180);
            xPos += lookAtX;
            zPos = trans * Math.Sin((targetAzimuth + aziOffset) * Math.PI / 180);
            zPos += lookAtZ;
        }
    }
}
