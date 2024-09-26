using System;
using OpenTK;
using OpenTK.Graphics.OpenGL;


namespace AudioControlApp.RealSenseCamera
{
    public class RSCameraDataHiRes
    {
        // **************
        int DataID = 0;

        public bool doUpdateGraphicsOnNextPass = false;

        private bool debugmode = false;

        public bool grabBackgroundState = true;

        //private static int maxResolution = 129600; //  (480 * 270) part RES // has to be changed in RSCameraData AND cam control
        //private static int maxResolution = 230400; //  (640 * 360) part RES // has to be changed in RSCameraData AND cam control
        private static int maxResolution = 407040; //  (848 * 480) FULL RES
        //private static int maxResolution = 368640; //  (640 * 576) FULL RES
        //private static int maxResolution = 92160; //  (320 * 288) HALF RES

        private Vertex[] data = new Vertex[maxResolution]; // array of 3d vectors (512 * 424)
        private Vertex[] transfData = new Vertex[maxResolution]; // array of transformed and filtered 3d vectors (512 * 424)

        private struct Vertex
        {
            public Vector3 Position;
            public int Data;
        }

        private float globalDisplayScale = 1.79f; // determines how points are mapped onto screen space (physical size of screen)

        private int pointsToDraw = 0;
        public int transfPointsToDraw = 0;

        private int horizontalResolution;
        private int verticalResolution;

        //private double HFOV = 50.1; // estimate from right angle
        //private double VFOV = 40.5; // determined onsite
        private double HFOV = 58.1; // estimate from right angle (room corner)
        private double VFOV = 50.5; // estimate from right angle (ceiling meets wall)

        // compensate for distance to floor
        private float additionalZOffset = 0;

        // compensate for distance between sensors
        private float additionalXOffset = 0;

        private int i, j;

        private int maxRangeCamera = 5000; // measured in mm
        private int minRangeCamera = 0; // measured in mm

        private float depthPlaneWidthConstant; // notice float means we are working with transformed points.
        private float depthPlaneHeightConstant; // notice float means we are working with transformed points.

        private float cutoffPointsBelow = 0;
        private float cutoffPointsAbove = 100;
        private bool floorCeilFilterActive = false;
        //vertex buffer object
        private bool usingVBO;
        private bool cameraIsRotated;
        private bool cameraIsRotatedCW;
        private bool cameraIsInverted;
        private bool mirrorData;

        //private static int maxResolution = (DepthCamera.DepthCameraObjectOpenNI.FRAME_W * DepthCamera.DepthCameraObjectOpenNI.FRAME_H) / DepthCamera.DepthCameraObjectOpenNI.DOWNSAMPLE_FACTOR;

        public Vector3[] positionVboData = new Vector3[maxResolution]; // 3d points of entire grid
        uint[] indicesVboData = new uint[maxResolution]; // each point has one index
        private Vector4[] pointVertColors = new Vector4[maxResolution]; // each point has one color

        RSPositionCalibration depthCameraPosCalibrationVar;

        private Vector4 myPointColor;

        public RSCameraDataHiRes(int whichCameraID, bool whichDebugMode, double whichGlobalDisplayScale, double whichAdditionalXOffset, double whichAdditionalZOffset, int whichHres, int whichVres, bool isUsingVBO, bool isRotated, bool isRotatedCW, bool isReversed, bool isInverted)
        {
            DataID = whichCameraID;
            mirrorData = isReversed;

            debugmode = whichDebugMode;
            globalDisplayScale = (float)whichGlobalDisplayScale;
            additionalZOffset = (float)whichAdditionalZOffset;
            additionalXOffset = (float)whichAdditionalXOffset;

            horizontalResolution = whichHres;
            verticalResolution = whichVres;
            usingVBO = isUsingVBO;
            cameraIsRotated = isRotated;
            cameraIsRotatedCW = isRotatedCW;
            cameraIsInverted = isInverted;

            depthPlaneWidthConstant = (float)(Math.Tan(HFOV * Math.PI / 180));
            depthPlaneHeightConstant = (float)(Math.Tan(VFOV * Math.PI / 180));

            depthCameraPosCalibrationVar = new RSPositionCalibration(DataID, isRotated, isRotatedCW);
        }


        public void resetData() // used to reset data when camera becomes unavailable
        {
            int i;
            for (i = 0; i < positionVboData.Length; ++i)
            {
                positionVboData[i].X = 0f;
                positionVboData[i].Y = 0f;
                positionVboData[i].Z = 0f;
            }

            for (i = 0; i < indicesVboData.Length; ++i)
            {
                indicesVboData[i] = (uint)i; // just points, so these just count up.
            }

            if (DataID == 0)
            {
                myPointColor.X = 0.3f; // red 
                myPointColor.Y = 1.0f; // green
                myPointColor.Z = 0.3f; // blue
                myPointColor.W = 0.9f; // alpha
            }
            else if (DataID == 1)
            {
                myPointColor.X = 1.0f;
                myPointColor.Y = 0.0f;
                myPointColor.Z = 0.0f;
                myPointColor.W = 1.0f;
            }
            else if (DataID == 3)
            {
                myPointColor.X = 0.0f;
                myPointColor.Y = 0.0f;
                myPointColor.Z = 1.0f;
                myPointColor.W = 1.0f;
            }
            else // yellow
            {
                myPointColor.X = 1.0f;
                myPointColor.Y = 1.0f;
                myPointColor.Z = 0.0f;
                myPointColor.W = 1.0f;
            }


            for (i = 0; i < pointVertColors.Length; ++i)
            {
                if (DataID == 0)
                {
                    pointVertColors[i].X = 1.0f;
                    pointVertColors[i].Y = 0f;
                    pointVertColors[i].Z = 0f;
                    pointVertColors[i].W = 1.0f;
                }
                else if (DataID == 1)
                {
                    pointVertColors[i].X = 0.0f;
                    pointVertColors[i].Y = 1.0f;
                    pointVertColors[i].Z = 0f;
                    pointVertColors[i].W = 1.0f;
                }
                else if (DataID == 2)
                {
                    pointVertColors[i].X = 0.0f;
                    pointVertColors[i].Y = 0.0f;
                    pointVertColors[i].Z = 1.0f;
                    pointVertColors[i].W = 1.0f;
                }
                else if (DataID == 3)
                {
                    pointVertColors[i].X = 0.0f;
                    pointVertColors[i].Y = 1.0f;
                    pointVertColors[i].Z = 1.0f;
                    pointVertColors[i].W = 1.0f;
                }
            }

        }

        public void initOpenGL()
        {
            resetData();

            createPositionBuffer();
            createIndexBuffer();
            createColorBuffer();
        }

        public void adjustFOV(bool adjustHorizontal, bool increaseIt)
        {
            if (adjustHorizontal)
            {
                if (increaseIt)
                {
                    HFOV += 0.25;
                }
                else
                {
                    HFOV -= 0.25;
                }
            }
            else
            {
                if (increaseIt)
                {
                    VFOV += 0.25;
                }
                else
                {
                    VFOV -= 0.25;
                }
            }

            System.Diagnostics.Debug.WriteLine("[DEPTHDATA] new HFOV:[" + HFOV + "] new VFOV:[" + VFOV + "]");
            //stepAngleHorz = HFOV / (double)horizontalResolution;
            //stepAngleVert = VFOV / (double)verticalResolution;

            depthPlaneWidthConstant = (float)(Math.Tan(HFOV * Math.PI / 180));
            depthPlaneHeightConstant = (float)(Math.Tan(VFOV * Math.PI / 180));
        }

        #region sensorPosition

        public void setSensorRange(double whichMax, double whichMin)
        {
            maxRangeCamera = (int)Math.Floor(whichMax);
            minRangeCamera = (int)Math.Floor(whichMin);
        }

        public void setSensorFloorCeil(double whichFloor, double whichCeil)
        {
            cutoffPointsBelow = (float)whichFloor;
            cutoffPointsAbove = (float)whichCeil;
        }

        public void toggleSensorFloorCeil()
        {
            floorCeilFilterActive = !floorCeilFilterActive;
        }
        public void toggleSensorFloorCeil(bool forceItTo)
        {
            floorCeilFilterActive = forceItTo;
        }

        public void rotateSensorHeadX(bool rotateUp)
        {
            depthCameraPosCalibrationVar.rotateSensorHeadX(rotateUp);
        }
        public void rotateSensorHeadY(bool rollLeft)
        {
            depthCameraPosCalibrationVar.rotateSensorHeadY(rollLeft);
        }
        public void rotateSensorHeadZ(bool rotateLeft)
        {
            depthCameraPosCalibrationVar.rotateSensorHeadZ(rotateLeft);
        }
        public void moveSensorHeadZ(bool lowerIt, bool jumpFar)
        {
            depthCameraPosCalibrationVar.moveSensorHeadZ(lowerIt, jumpFar);
        }
        public void moveSensorHeadY(bool moveLeft, bool jumpFar)
        {
            depthCameraPosCalibrationVar.moveSensorHeadY(moveLeft, jumpFar);
        }
        public void moveSensorHeadX(bool moveLeft, bool jumpFar)
        {
            depthCameraPosCalibrationVar.moveSensorHeadX(moveLeft, jumpFar);
        }

        #endregion sensorPosition



        private float depthPlaneWidth(float whichRange)
        {
            //return (whichRange * Math.Tan(HFOV * Math.PI / 180));
            return (whichRange * depthPlaneWidthConstant);
        }

        private float depthPlaneHeight(float whichRange)
        {
            return (whichRange * depthPlaneHeightConstant);
        }

        private float posnInPlaneHoriz(float whichRange, int whichDataPoint)
        {
            float PlaneW = depthPlaneWidth(whichRange);
            return ((float)whichDataPoint / (float)horizontalResolution - 0.5f) * PlaneW;
        }
        private float posnInPlaneVert(float whichRange, int whichDataPoint)
        {
            float PlaneH = depthPlaneHeight(whichRange);
            return ((float)whichDataPoint / (float)verticalResolution - 0.5f) * PlaneH;
        }

        private float getPixelSize(float whichRange)
        {
            return (depthPlaneHeight(whichRange) / (float)verticalResolution);
        }
        /*
        public void startBackgroundCapture()
        {
            grabBackgroundState = true;
            grabBackgroundStep = 0;
        }
        */

        /*
        public void initiateCalibration()
        {
            //if (!depthCameraPosCalibrationVar.performingCalibration)
           // {
           //     depthCameraPosCalibrationVar.initCalibration();
            //}

            // after calibration, automatically remove floor:
            floorCeilFilterActive = true;
        }
        */
        public void updateXYZData(float[] whichZYXRangeData)
        {
            int index;
            float xPosn, yPosn, depthPosn;
            pointsToDraw = 0;

            // prep vars outside of unsafe loop:
            float offsetX = (float)depthCameraPosCalibrationVar.sensorPosnX + (float)depthCameraPosCalibrationVar.addedOffsetX;
            float offsetY = (float)depthCameraPosCalibrationVar.sensorPosnY + (float)depthCameraPosCalibrationVar.addedOffsetY;
            float offsetZ = (float)depthCameraPosCalibrationVar.sensorPosnZ + (float)depthCameraPosCalibrationVar.addedOffsetZ;

            float cosQ = (float)Math.Cos(depthCameraPosCalibrationVar.sensorTiltX * Math.PI / 180.0);
            float sinQ = (float)Math.Sin(depthCameraPosCalibrationVar.sensorTiltX * Math.PI / 180.0);

            float tempX, tempY, tempZ;
            float tempX2, tempY2, tempZ2;

            float cosR = (float)Math.Cos(depthCameraPosCalibrationVar.sensorTiltY * Math.PI / 180.0);
            float sinR = (float)Math.Sin(depthCameraPosCalibrationVar.sensorTiltY * Math.PI / 180.0);

            float cosS = (float)Math.Cos(depthCameraPosCalibrationVar.sensorYaw * Math.PI / 180.0);
            float sinS = (float)Math.Sin(depthCameraPosCalibrationVar.sensorYaw * Math.PI / 180.0);


            float newZPosn, newYPosn;
            transfPointsToDraw = 0;
            //float newX, newY;
            float z3Dmillimeters = 0;
            bool proceedWithStore = false;
            // transform points here
            unsafe
            {
                for (i = 0; i < horizontalResolution; ++i)
                {
                    for (j = 0; j < verticalResolution; ++j)
                    {
                        proceedWithStore = false; // reset value to false
                        index = i * 3 + j * horizontalResolution * 3;
                        z3Dmillimeters = whichZYXRangeData[index + 2] * 1000f;
                        if (z3Dmillimeters != 0)
                        {
                            if (floorCeilFilterActive)
                            {
                                if ((z3Dmillimeters > (float)minRangeCamera) && (z3Dmillimeters < (float)maxRangeCamera))
                                    proceedWithStore = true;
                            }
                            else
                            {
                                proceedWithStore = true;
                            }
                        }
                        if (proceedWithStore)
                        {
                            depthPosn = (float)z3Dmillimeters * globalDisplayScale;
                            if (cameraIsInverted)
                            {
                                xPosn = 0.0f - (float)whichZYXRangeData[index] * 1000f * globalDisplayScale;
                                yPosn = 0.0f - (float)whichZYXRangeData[index + 1] * 1000f * globalDisplayScale;
                            }
                            else
                            {
                                xPosn = (float)whichZYXRangeData[index] * 1000f * globalDisplayScale;
                                yPosn = (float)whichZYXRangeData[index + 1] * 1000f * globalDisplayScale;
                            }

                            if (cameraIsRotated)
                            {
                                tempX = xPosn;
                                tempY = yPosn;
                                if (cameraIsRotatedCW)
                                {
                                    xPosn = tempY;
                                    yPosn = tempX;
                                }
                                else
                                {
                                    xPosn = 0 - tempY;
                                    yPosn = 0 - tempX;
                                }
                            }
                            //depthPosn *= globalDisplayScale;
                            if (mirrorData)
                                xPosn *= -1f; // make negative unless you want to "mirror" data on display
                            data[pointsToDraw].Position = new Vector3(xPosn, yPosn, depthPosn);
                            pointsToDraw += 1;
                        }

                    }
                }
                
                //System.Diagnostics.Debug.WriteLine("[RSCamData] found "+pointsToDraw +" valid points to draw");

                
                for (i = 0; i < pointsToDraw; ++i)
                {
                    // rotation about X axis:   y' = y*cos q - z*sin q
                    //                          z' = y*sin q + z*cos q
                    // negating y axis:
                    //                          y' = z*sin q - y*cos q
                    //                          z' = y*sin q + z*cos q

                    tempY = (data[i].Position.Z * sinQ) - (data[i].Position.Y * cosQ);
                    tempZ = (data[i].Position.Y * sinQ) + (data[i].Position.Z * cosQ);
                    tempX = data[i].Position.X;

                    // rotation about Y axis:   z' = z*cos R - x*sin R
                    //                          x' = z*sin R + x*cos R
                    tempZ2 = (tempZ * cosR) - (tempX * sinR);
                    tempX2 = (tempZ * sinR) + (tempX * cosR);
                    tempY2 = tempY;


                    // rotation about Z axis:   x' = x*cos S - y*sin S
                    //                          y' = x*sin S + y*cos S
                    //                          z' = z 
                    transfData[transfPointsToDraw].Position.X = (tempX2 * cosS) - (tempY2 * sinS);
                    transfData[transfPointsToDraw].Position.Y = (tempX2 * sinS) + (tempY2 * cosS);
                    transfData[transfPointsToDraw].Position.Z = tempZ2;


                    // finally add offsets:
                    transfData[transfPointsToDraw].Position.X += offsetX;
                    transfData[transfPointsToDraw].Position.Y += offsetY;
                    transfData[transfPointsToDraw].Position.Z += offsetZ;

                    newZPosn = transfData[transfPointsToDraw].Position.Z;
                    newYPosn = transfData[transfPointsToDraw].Position.Y;
                    if (floorCeilFilterActive)
                    {
                        if ((newYPosn > cutoffPointsBelow) && (newYPosn < cutoffPointsAbove))
                        {

                            positionVboData[transfPointsToDraw].X = 0 - transfData[transfPointsToDraw].Position.X - additionalXOffset;
                            positionVboData[transfPointsToDraw].Y = transfData[transfPointsToDraw].Position.Y;
                            positionVboData[transfPointsToDraw].Z = newZPosn - additionalZOffset;
                            transfPointsToDraw += 1; // proceed to next point (otherwise ignore last point and overwrite)
                        }
                    }
                    else
                    {
                        positionVboData[transfPointsToDraw].X = 0 - transfData[transfPointsToDraw].Position.X - additionalXOffset;
                        positionVboData[transfPointsToDraw].Y = transfData[transfPointsToDraw].Position.Y;
                        positionVboData[transfPointsToDraw].Z = newZPosn - additionalZOffset;
                        transfPointsToDraw += 1; // proceed to next point (otherwise ignore last point and overwrite)
                    }

                }
            } // end of unsafe

            doUpdateGraphicsOnNextPass = true;
        }

        public void updateDrawGeometry()
        {
            updatePositionBuffer();
        }

        int currentNumberOfPointsToDraw;
        int currentNumberOfSkeletonPointsToDraw;
        int currentNumberOfSkeletonIndicesToDraw;

        public void drawTransformedDataPointsProgPipeline(int whichShaderColorPointer)
        {
            GL.Uniform4(whichShaderColorPointer, myPointColor);

            GL.BindVertexArray(handleVAO);
            GL.DrawElements(BeginMode.Points, currentNumberOfPointsToDraw, DrawElementsType.UnsignedInt, 0);
            GL.BindVertexArray(0);
        }

        /*
        public Vector3[] transformSkeletonVects(K4AdotNet.Float3[] whichVects)
        {
            int incomingLength = whichVects.Length;
            Vector3[] vectorsToReturn = new Vector3[incomingLength];

            int i;
            //int index;
            float xPosn, yPosn, depthPosn;


            // prep vars outside of unsafe loop:
            float offsetX = (float)depthCameraPosCalibrationVar.sensorPosnX + (float)depthCameraPosCalibrationVar.addedOffsetX;
            float offsetY = (float)depthCameraPosCalibrationVar.sensorPosnY + (float)depthCameraPosCalibrationVar.addedOffsetY;
            float offsetZ = (float)depthCameraPosCalibrationVar.sensorPosnZ + (float)depthCameraPosCalibrationVar.addedOffsetZ;

            float cosQ = (float)Math.Cos(depthCameraPosCalibrationVar.sensorTiltX * Math.PI / 180.0);
            float sinQ = (float)Math.Sin(depthCameraPosCalibrationVar.sensorTiltX * Math.PI / 180.0);

            float tempX, tempY, tempZ;
            float tempX2, tempY2, tempZ2;

            float cosR = (float)Math.Cos(depthCameraPosCalibrationVar.sensorTiltY * Math.PI / 180.0);
            float sinR = (float)Math.Sin(depthCameraPosCalibrationVar.sensorTiltY * Math.PI / 180.0);

            float cosS = (float)Math.Cos(depthCameraPosCalibrationVar.sensorYaw * Math.PI / 180.0);
            float sinS = (float)Math.Sin(depthCameraPosCalibrationVar.sensorYaw * Math.PI / 180.0);

            // transform points here
            unsafe
            {
                for (i = 0; i < incomingLength; ++i)
                {
                    depthPosn = whichVects[i].Z * globalDisplayScale;
                    if (cameraIsInverted)
                    {
                        xPosn = 0.0f - whichVects[i].X * globalDisplayScale;
                        yPosn = 0.0f - whichVects[i].Y * globalDisplayScale;
                    }
                    else
                    {
                        xPosn = whichVects[i].X * globalDisplayScale;
                        yPosn = whichVects[i].Y * globalDisplayScale;
                    }

                    if (cameraIsRotated)
                    {
                        tempX = xPosn;
                        tempY = yPosn;
                        if (cameraIsRotatedCW)
                        {
                            xPosn = tempY;
                            yPosn = tempX;
                        }
                        else
                        {
                            xPosn = 0 - tempY;
                            yPosn = 0 - tempX;
                        }
                    }
                    //depthPosn *= globalDisplayScale;
                    if (!mirrorData) // opposite with skeletal data for some reason...
                        xPosn *= -1f; // make negative unless you want to "mirror" data on display

                    // rotation about X axis:   y' = y*cos q - z*sin q
                    //                          z' = y*sin q + z*cos q
                    // negating y axis:
                    //                          y' = z*sin q - y*cos q
                    //                          z' = y*sin q + z*cos q

                    tempY = (depthPosn * sinQ) - (yPosn * cosQ);
                    tempZ = (yPosn * sinQ) + (depthPosn * cosQ);
                    tempX = xPosn;

                    // rotation about Y axis:   z' = z*cos R - x*sin R
                    //                          x' = z*sin R + x*cos R
                    tempZ2 = (tempZ * cosR) - (tempX * sinR);
                    tempX2 = (tempZ * sinR) + (tempX * cosR);
                    tempY2 = tempY;


                    // rotation about Z axis:   x' = x*cos S - y*sin S
                    //                          y' = x*sin S + y*cos S
                    //                          z' = z 
                    vectorsToReturn[i].X = (tempX2 * cosS) - (tempY2 * sinS);
                    vectorsToReturn[i].Y = (tempX2 * sinS) + (tempY2 * cosS);
                    vectorsToReturn[i].Z = tempZ2;


                    // finally add offsets:
                    vectorsToReturn[i].X -= offsetX; // opposite for skeletal data for some reason
                    vectorsToReturn[i].Y += offsetY;
                    vectorsToReturn[i].Z += offsetZ;
                }
            } // end of unsafe
            return vectorsToReturn;
        }*/

        private Vector3 transformSingleVect(Vector3 whichVect)
        {
            Vector3 vectorToReturn;
            //int index;
            float xPosn, yPosn, depthPosn;

            // prep vars outside of unsafe loop:
            float offsetX = (float)depthCameraPosCalibrationVar.sensorPosnX + (float)depthCameraPosCalibrationVar.addedOffsetX;
            float offsetY = (float)depthCameraPosCalibrationVar.sensorPosnY + (float)depthCameraPosCalibrationVar.addedOffsetY;
            float offsetZ = (float)depthCameraPosCalibrationVar.sensorPosnZ + (float)depthCameraPosCalibrationVar.addedOffsetZ;

            float cosQ = (float)Math.Cos(depthCameraPosCalibrationVar.sensorTiltX * Math.PI / 180.0);
            float sinQ = (float)Math.Sin(depthCameraPosCalibrationVar.sensorTiltX * Math.PI / 180.0);

            float tempX, tempY, tempZ;
            float tempX2, tempY2, tempZ2;

            float cosR = (float)Math.Cos(depthCameraPosCalibrationVar.sensorTiltY * Math.PI / 180.0);
            float sinR = (float)Math.Sin(depthCameraPosCalibrationVar.sensorTiltY * Math.PI / 180.0);

            float cosS = (float)Math.Cos(depthCameraPosCalibrationVar.sensorYaw * Math.PI / 180.0);
            float sinS = (float)Math.Sin(depthCameraPosCalibrationVar.sensorYaw * Math.PI / 180.0);

            // transform points here
            unsafe
            {

                depthPosn = whichVect.Z * globalDisplayScale;
                if (cameraIsInverted)
                {
                    xPosn = 0.0f - whichVect.X * globalDisplayScale;
                    yPosn = 0.0f - whichVect.Y * globalDisplayScale;
                }
                else
                {
                    xPosn = whichVect.X * globalDisplayScale;
                    yPosn = whichVect.Y * globalDisplayScale;
                }

                if (cameraIsRotated)
                {
                    tempX = xPosn;
                    tempY = yPosn;
                    if (cameraIsRotatedCW)
                    {
                        xPosn = tempY;
                        yPosn = tempX;
                    }
                    else
                    {
                        xPosn = 0 - tempY;
                        yPosn = 0 - tempX;
                    }
                }
                //depthPosn *= globalDisplayScale;
                if (mirrorData)
                    xPosn *= -1f; // make negative unless you want to "mirror" data on display

                // rotation about X axis:   y' = y*cos q - z*sin q
                //                          z' = y*sin q + z*cos q
                // negating y axis:
                //                          y' = z*sin q - y*cos q
                //                          z' = y*sin q + z*cos q

                tempY = (depthPosn * sinQ) - (yPosn * cosQ);
                tempZ = (yPosn * sinQ) + (depthPosn * cosQ);
                tempX = xPosn;

                // rotation about Y axis:   z' = z*cos R - x*sin R
                //                          x' = z*sin R + x*cos R
                tempZ2 = (tempZ * cosR) - (tempX * sinR);
                tempX2 = (tempZ * sinR) + (tempX * cosR);
                tempY2 = tempY;


                // rotation about Z axis:   x' = x*cos S - y*sin S
                //                          y' = x*sin S + y*cos S
                //                          z' = z 
                vectorToReturn.X = (tempX2 * cosS) - (tempY2 * sinS);
                vectorToReturn.Y = (tempX2 * sinS) + (tempY2 * cosS);
                vectorToReturn.Z = tempZ2;


                // finally add offsets:
                vectorToReturn.X += offsetX;
                vectorToReturn.Y += offsetY;
                vectorToReturn.Z += offsetZ;

            } // end of unsafe
            return vectorToReturn;
        }
        

        // ***********************************
        #region create and update VBOs
        // ***********************************
        int handlePosnVBO, handleColorVBO, handleIndicesVBO, handleVAO, handleShader;

        private void createPositionBuffer()
        {
            GL.GenBuffers(1, out handlePosnVBO);
            GL.BindBuffer(BufferTarget.ArrayBuffer, handlePosnVBO);
            GL.BufferData<OpenTK.Vector3>(BufferTarget.ArrayBuffer,
                new IntPtr(positionVboData.Length * OpenTK.Vector3.SizeInBytes),
                positionVboData, BufferUsageHint.StreamDraw);
            // clear for new buffer:
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        }

        private void updatePositionBuffer()
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, handlePosnVBO);
            GL.BufferData<OpenTK.Vector3>(BufferTarget.ArrayBuffer,
                new IntPtr(positionVboData.Length * OpenTK.Vector3.SizeInBytes),
                positionVboData, BufferUsageHint.StreamDraw);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

            currentNumberOfPointsToDraw = transfPointsToDraw;
        }

        private void createColorBuffer()
        {
            GL.GenBuffers(1, out handleColorVBO);
            GL.BindBuffer(BufferTarget.ArrayBuffer, handleColorVBO);
            GL.BufferData<OpenTK.Vector4>(BufferTarget.ArrayBuffer,
                new IntPtr(pointVertColors.Length * OpenTK.Vector4.SizeInBytes),
                pointVertColors, BufferUsageHint.StreamDraw);
            // clear for new buffer:
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        }

        private void updateColorBuffer()
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, handleColorVBO);
            GL.BufferData<OpenTK.Vector4>(BufferTarget.ArrayBuffer,
                new IntPtr(pointVertColors.Length * OpenTK.Vector4.SizeInBytes),
                pointVertColors, BufferUsageHint.StreamDraw);
        }


        private void createIndexBuffer()
        {
            GL.GenBuffers(1, out handleIndicesVBO);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, handleIndicesVBO);
            GL.BufferData(BufferTarget.ElementArrayBuffer,
                  new IntPtr(indicesVboData.Length * sizeof(uint)),
                  indicesVboData, BufferUsageHint.StreamDraw);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
        }

        private void updateIndexBuffer()
        {
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, handleIndicesVBO);
            GL.BufferData(BufferTarget.ElementArrayBuffer,
                  new IntPtr(indicesVboData.Length * sizeof(uint)),
                  indicesVboData, BufferUsageHint.StreamDraw);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
        }


        public void createVAO(int whichShaderHandle)
        {
            GL.GenVertexArrays(1, out handleVAO);
            GL.BindVertexArray(handleVAO);

            int arrayIndexCounter = 0;

            // set positions to vPosition:
            GL.BindBuffer(BufferTarget.ArrayBuffer, handlePosnVBO);
            GL.VertexAttribPointer(arrayIndexCounter, 3, VertexAttribPointerType.Float, false, OpenTK.Vector3.SizeInBytes, 0);
            GL.EnableVertexAttribArray(arrayIndexCounter);
            GL.BindAttribLocation(whichShaderHandle, arrayIndexCounter, "vPosition");

            arrayIndexCounter += 1;

            // set colors to vColorValue:
            GL.BindBuffer(BufferTarget.ArrayBuffer, handleColorVBO);
            GL.VertexAttribPointer(arrayIndexCounter, 4, VertexAttribPointerType.Float, false, OpenTK.Vector4.SizeInBytes, 0);
            GL.EnableVertexAttribArray(arrayIndexCounter);
            GL.BindAttribLocation(whichShaderHandle, arrayIndexCounter, "vColor");

            //arrayIndexCounter += 1;

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, handleIndicesVBO);

            // clear for new VAO:
            GL.BindVertexArray(0);
        }

        

        #endregion create and update VBOs
        // ***********************************

        public void onClosing()
        {
            GL.DeleteVertexArrays(1, ref handleVAO);
        }
    }
}
