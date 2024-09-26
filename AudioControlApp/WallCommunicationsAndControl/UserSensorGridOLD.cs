using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace SensorControlApp.WallCommunicationsAndControl
{
    class UserSensorGridOLD
    {
        private int maxResolution = 200;
        private int actualResolutionH = 200;
        private int actualResolutionV = 200;
        private float maxDepth = 9999.9f;

        public float[,] minDepthMeasured = new float[200, 200]; // 200 = max resolution of grid
        public float[,] prevDepthMeasured = new float[200, 200]; // 200 = max resolution of grid
        public float[, ,] gridPositions = new float[200, 200, 2]; // precalculate position of each part of grid 200x200 x and y

        public byte[,] convertedDepthData = new byte[200, 200];
        private int maxDepthDataXCount = 200;
        private int maxDepthDataYCount = 200;
        public byte[,] prevConvertedDepthData = new byte[200, 200];


        public float gridWidth = 5120.0f;
        public float gridHeight = 1333.0f;
        public float gridStartX = -2560.0f;
        public float gridStartY = -800.0f;

        private float gridResolutionW = 100.0f;
        private float gridResolutionH = 100.0f;

        private float minTransmitRange = 0.0f;
        private float maxTransmitRange = 5000.0f;


        public string changedData;




        private DepthDataStreamWriter depthDataSreamWriterVar;

        public VideoSensorGrid videoSensorGridVar;


        public UserSensorGridOLD(string whichIP, int whichPort, int whichHCount, int whichVCount)
        {
            actualResolutionH = whichHCount;
            actualResolutionV = whichVCount;

            if (actualResolutionH > maxDepthDataXCount)
            {
                actualResolutionH = maxDepthDataXCount;
            }
            if (actualResolutionV > maxDepthDataYCount)
            {
                actualResolutionV = maxDepthDataYCount;
            }

            gridResolutionW = gridWidth / (float)whichHCount;
            gridResolutionH = gridHeight / (float)whichVCount;

            setGridPositionArray();

            initDepthData();
            resetDepthData();


            depthDataSreamWriterVar = new DepthDataStreamWriter(whichIP, whichPort);

            videoSensorGridVar = new VideoSensorGrid(whichHCount, whichVCount);
        }

        public void attemptToReconnect()
        {
            depthDataSreamWriterVar.attemptReconnect();
        }

        public void haltingProgram()
        {
            depthDataSreamWriterVar.halt();
        }

        private void setGridPositionArray()
        {
            int i, j;
            for (i = 0; i < maxResolution; ++i)
            {
                for (j = 0; j < maxResolution; ++j)
                {
                    gridPositions[i, j, 0] = gridStartX + (gridResolutionW * (float)i);
                    gridPositions[i, j, 1] = gridStartY + (gridResolutionH * (float)j);
                }
            }
        }

        /*
        public void initOpenGL()
        {
            //particleEngineControlVar.initPaticleSystem();
        }
        */

        public void setDepthRanges(double whichNear, double whichFar)
        {
            minTransmitRange = (float)whichNear;
            maxTransmitRange = (float)whichFar;

            videoSensorGridVar.setDepthRanges(minTransmitRange, maxTransmitRange);
        }

        private void initDepthData()
        {
            int i, j;
            for (i = 0; i < maxResolution; ++i)
            {
                for (j = 0; j < maxResolution; ++j)
                {
                    minDepthMeasured[i, j] = maxDepth;
                    prevDepthMeasured[i, j] = maxDepth;
                }
            }
        }
        private void resetDepthData()
        {
            int i, j;
            for (i = 0; i < actualResolutionH; ++i)
            {
                for (j = 0; j < actualResolutionV; ++j)
                {
                    convertedDepthData[i, j] = (byte)0;
                }
            }
        }
        public void resetMinMeasured()
        {
            Array.Copy(minDepthMeasured, prevDepthMeasured, minDepthMeasured.Length);
            Array.Copy(convertedDepthData, prevConvertedDepthData, convertedDepthData.Length);

            resetDepthData();

            int i, j;
            for (i = 0; i < maxResolution; ++i)
            {
                for (j = 0; j < maxResolution; ++j)
                {
                    minDepthMeasured[i, j] = maxDepth;
                }
            }
        }
        private int returnGridIndexPositionX(float whichX)
        {
            int valueToReturn = 0;

            valueToReturn = (int)Math.Floor((whichX - gridStartX) / gridResolutionW);

            return valueToReturn;
        }
        private int returnGridIndexPositionY(float whichY)
        {
            int valueToReturn = 0;

            valueToReturn = (int)Math.Floor((whichY - gridStartY) / gridResolutionH);

            return valueToReturn;
        }



        public void testPointOnGrid(float whichX, float whichY, float whichDepth)
        {
            float percentDepth;
            byte byteData = 0;

            int gridPosnX = returnGridIndexPositionX(whichX);
            int gridPosnY = returnGridIndexPositionY(whichY);

            if ((gridPosnX > 0) && (gridPosnX < actualResolutionH))
            {
                if ((gridPosnY > 0) && (gridPosnY < actualResolutionV))
                {
                    if (whichDepth < minDepthMeasured[gridPosnX, gridPosnY])
                    {
                        minDepthMeasured[gridPosnX, gridPosnY] = whichDepth;

                        // and store as byte data as well:
                        if (minDepthMeasured[gridPosnX, gridPosnY] == maxDepth)
                            percentDepth = 1;
                        else
                            percentDepth = (minDepthMeasured[gridPosnX, gridPosnY] - minTransmitRange) / (maxTransmitRange - minTransmitRange);

                        if (percentDepth < 0)
                            percentDepth = 0;
                        else if (percentDepth > 1)
                            percentDepth = 1;
                        byteData = (byte)Math.Round((double)percentDepth * 254);
                        convertedDepthData[gridPosnX, gridPosnY] = byteData;
                    }
                    //else
                    //{
                    //    convertedDepthData[gridPosnX, gridPosnY] = (byte)0;
                    //}
                }
            }
        }


        public void updateAllDepthData()
        {
            int i, j;
            for (i = 0; i < actualResolutionH; ++i)
            {
                for (j = 0; j < actualResolutionV; ++j)
                {
                    // send ALL data for transport:
                    depthDataSreamWriterVar.collectDataForTransport(i, j, convertedDepthData[i, j]);
                }
            }
        }

        public bool checkForVideoSpawn()
        {
            //videoSensorGridVar.testForUserActivation(minDepthMeasured);
            videoSensorGridVar.testForUserActivation(convertedDepthData);

            return videoSensorGridVar.isDirty();
        }


        /*
        public void updateChangedData()
        {
            string additiveChangedData = "";
            string immediateChangedData = "";
            int i, j;
            for (i = 0; i < actualResolutionH; ++i)
            {
                for (j = 0; j < actualResolutionV; ++j)
                {
                    // send ALL data for transport:
                    depthDataSreamWriterVar.collectDataForTransport(i, j, convertedDepthData[i, j]);
                }
            }

            // this would send more occasional, larger packets:
            //if (depthDataSreamWriterVar.isConnected)
            //{
            //    if ((depthDataSreamWriterVar.writeToStream(changedData)) && (depthDataSreamWriterVar.flushStream()))
            //    {
            //        Console.WriteLine("sent updated data");
            //    }
            //    else
            //    {
            //        Console.WriteLine("data send failed");
            //    }
            //}
            

            Array.Copy(convertedDepthData, prevConvertedDepthData, convertedDepthData.Length);

            //if (changedData!="")
            //    Console.WriteLine(changedData);

            //return changedData;

            //depthDataSreamWriterVar.sendTestDataOverNetwork();
        }
        */


        public void draw()
        {
            drawUserGrid();
            videoSensorGridVar.draw();

        }


        private void drawUserGrid()
        {
            float gridPosnX;
            float gridPosnY;
            float redValue;
            float depthTestValue;
            GL.Color4(0.6f, 0.110f, 0.85f, 1.0f);

            GL.Begin(BeginMode.Quads);

            int i, j;
            for (i = 0; i < actualResolutionH; ++i)
            {
                for (j = 0; j < actualResolutionV; ++j)
                {
                    //depthTestValue = 0 - minDepthMeasured[i, j];

                    depthTestValue = 0 - ((float)convertedDepthData[i, j] / 254.0f) * (maxTransmitRange - minTransmitRange);
                    depthTestValue -= minTransmitRange;

                    if (minDepthMeasured[i, j] < maxDepth)
                    {
                        gridPosnX = ((float)i * gridResolutionW) + gridStartX;
                        gridPosnY = ((float)j * gridResolutionH) + gridStartY;
                        redValue = depthTestValue / 1500.0f;

                        GL.Color4(redValue, 0.110f, 0.85f, 1.0f);
                        GL.Vertex3(gridPosnX, depthTestValue, gridPosnY);
                        GL.Vertex3(gridPosnX + gridResolutionW, depthTestValue, gridPosnY);
                        GL.Vertex3(gridPosnX + gridResolutionW, depthTestValue, gridPosnY + gridResolutionH);
                        GL.Vertex3(gridPosnX, depthTestValue, gridPosnY + gridResolutionH);


                    }
                }
            }

            GL.End();
        }
    }
}
