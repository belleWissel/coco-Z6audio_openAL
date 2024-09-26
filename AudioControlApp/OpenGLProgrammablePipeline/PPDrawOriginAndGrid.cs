using System;
using System.Collections.Generic;
using System.Text;
using OpenTK;
using OpenTK.Graphics.OpenGL;


namespace AudioControlApp.OpenGLProgrammablePipeline
{
    class PPDrawOriginAndGrid
    {
        // ****************************************************************************
        // AXIS geometry
        private static int numAxisPoints = 4;
        private Vector3[] axisVboData = new Vector3[numAxisPoints]; // ends of axis
        private static int numAxisIndices = 6;
        private uint[] axisIndicesVboData = new uint[numAxisIndices]; // connect above points with 3 lines
        
        // ****************************************************************************
        // BOX geometry
        private static int numBoxPoints = 8;
        private Vector3[] boxVboData = new Vector3[numBoxPoints]; // corners of box
        private static int numBoxIndices = 24;
        private uint[] boxIndicesVboData = new uint[numBoxIndices]; // connect above points with 12 lines
        private float boxXwidth = 1000;
        private float boxYstart = -1000;
        private float boxYend = 1000;
        private float boxZnear = 0;
        private float boxZfar = 1000;
        
        // ****************************************************************************
        // GRID geometry
        private static int maxNumberOfLines = 51;
        private int actualNumberOfGridLinesToDraw;
        private float gridW = 2000f;
        private float gridH = 2000f;
        private int gridLineCount = 20;

        // ****************************************************************************
        // draw variables
        private int positionVboHandle, indicesVboHandle, vaoID;
        private Vector3[] positionVboData = new Vector3[((maxNumberOfLines * 2 * 2) + numAxisPoints) * 3]; // each line has 2 positions, each grid has horiz & vert lines
        uint[] indicesVboData = new uint[((maxNumberOfLines * 2) + numAxisIndices)*3]; // total points / 2 points per line * 3 planes

        private int runningVertCounter = 0;
        private int runningIndexCounter = 0;


        public PPDrawOriginAndGrid(float whichGridW, float whichGridH, int whichNumberOfGridLines)
        {
            gridW = whichGridW;
            gridH = whichGridH;
            gridLineCount = whichNumberOfGridLines;

            if (gridLineCount > maxNumberOfLines)
                gridLineCount = maxNumberOfLines;

            actualNumberOfGridLinesToDraw = gridLineCount * 2 * 3;

            /*
            initGridPoints();
            initAxisPoints();
            initBoxPoints();
            packBuffers();*/

        }

        private void initGridPoints()
        {

            float gridLeftEdge = 0.0f - gridW / 2.0f;
            float gridRightEdge = gridW / 2.0f;
            float gridBottomEdge = 0.0f - gridH / 2.0f;
            float gridTopEdge = gridH / 2.0f;

            float gridDrawDepth = 0.0f;
            int i;

            
            float lineHorzPos = 0.0f;
            float lineVertPos = 0.0f;


            runningVertCounter = 0;
            runningIndexCounter = 0;

            // ******************************** XY PLANE ********************************
            // layout vertical lines:
            for (i = 0; i < gridLineCount; ++i)
            {
                lineHorzPos = gridLeftEdge + (float)i * (gridW / ((float)(gridLineCount-1)));
                positionVboData[runningVertCounter] = new Vector3(lineHorzPos, gridBottomEdge, gridDrawDepth);
                runningVertCounter += 1;
                positionVboData[runningVertCounter] = new Vector3(lineHorzPos, gridTopEdge, gridDrawDepth);
                runningVertCounter += 1;
            }

            // layout horz lines:          
            for (i = 0; i < gridLineCount; ++i)
            {
                lineVertPos = gridBottomEdge + (float)i * (gridH / ((float)(gridLineCount - 1)));
                positionVboData[runningVertCounter] = new Vector3(gridLeftEdge, lineVertPos, gridDrawDepth);
                runningVertCounter += 1;
                positionVboData[runningVertCounter] = new Vector3(gridRightEdge, lineVertPos, gridDrawDepth);
                runningVertCounter += 1;
            }


            // ******************************** YZ PLANE ********************************
            // layout vertical lines:
            for (i = 0; i < gridLineCount; ++i)
            {
                lineHorzPos = gridLeftEdge + (float)i * (gridW / ((float)(gridLineCount - 1)));
                positionVboData[runningVertCounter] = new Vector3(gridDrawDepth, lineHorzPos, gridBottomEdge);
                runningVertCounter += 1;
                positionVboData[runningVertCounter] = new Vector3(gridDrawDepth, lineHorzPos, gridTopEdge);
                runningVertCounter += 1;
            }

            // layout horz lines:          
            for (i = 0; i < gridLineCount; ++i)
            {
                lineVertPos = gridBottomEdge + (float)i * (gridH / ((float)(gridLineCount - 1)));
                positionVboData[runningVertCounter] = new Vector3(gridDrawDepth, gridLeftEdge, lineVertPos);
                runningVertCounter += 1;
                positionVboData[runningVertCounter] = new Vector3(gridDrawDepth, gridRightEdge, lineVertPos);
                runningVertCounter += 1;
            }

            // ******************************** XZ PLANE ********************************
            // layout vertical lines:
            for (i = 0; i < gridLineCount; ++i)
            {
                lineHorzPos = gridLeftEdge + (float)i * (gridW / ((float)(gridLineCount - 1)));
                positionVboData[runningVertCounter] = new Vector3(lineHorzPos, gridDrawDepth, gridBottomEdge);
                runningVertCounter += 1;
                positionVboData[runningVertCounter] = new Vector3(lineHorzPos, gridDrawDepth, gridTopEdge);
                runningVertCounter += 1;
            }

            // layout horz lines:          
            for (i = 0; i < gridLineCount; ++i)
            {
                lineVertPos = gridBottomEdge + (float)i * (gridH / ((float)(gridLineCount - 1)));
                positionVboData[runningVertCounter] = new Vector3(gridLeftEdge, gridDrawDepth, lineVertPos);
                runningVertCounter += 1;
                positionVboData[runningVertCounter] = new Vector3(gridRightEdge, gridDrawDepth, lineVertPos);
                runningVertCounter += 1;
            }


            // just connect the dots for display grid:
            for (i = 0; i < runningVertCounter; ++i)
            {
                indicesVboData[i] = (uint)i;
                runningIndexCounter += 1;
            }
        }


        private void initAxisPoints()
        {
            // origin:
            axisVboData[0].X = 0.0f;
            axisVboData[0].Y = 0.0f;
            axisVboData[0].Z = 0.0f;
            // x axis:
            axisVboData[1].X = 200.0f;
            axisVboData[1].Y = 0.0f;
            axisVboData[1].Z = 0.0f;
            // y axis:
            axisVboData[2].X = 0.0f;
            axisVboData[2].Y = 150.0f;
            axisVboData[2].Z = 0.0f;
            // z axis:
            axisVboData[3].X = 0.0f;
            axisVboData[3].Y = 0.0f;
            axisVboData[3].Z = 100.0f;

            //. X
            axisIndicesVboData[0] = 0;
            axisIndicesVboData[1] = 1;
            //. Y
            axisIndicesVboData[2] = 0;
            axisIndicesVboData[3] = 2;
            //. Z
            axisIndicesVboData[4] = 0;
            axisIndicesVboData[5] = 3;

            // add these vertices and indeces to the existing Vbo data
            int i;
            int currentVertCount = runningVertCounter;

            for (i = 0; i < numAxisPoints; ++i)
            {
                positionVboData[runningVertCounter] = axisVboData[i];
                runningVertCounter += 1;
            }

            for (i = 0; i < numAxisIndices; ++i)
            {
                indicesVboData[runningIndexCounter] = axisIndicesVboData[i] + (uint)currentVertCount;
                runningIndexCounter += 1;
            }
        }

        private void initBoxPoints()
        {
            // top:
            boxVboData[0].X = 0f - boxXwidth / 2f;
            boxVboData[0].Y = boxYstart;
            boxVboData[0].Z = boxZnear;
            // x axis:
            boxVboData[1].X = boxXwidth / 2f;
            boxVboData[1].Y = boxYstart;
            boxVboData[1].Z = boxZnear;
            // y axis:
            boxVboData[2].X = boxXwidth / 2f;
            boxVboData[2].Y = boxYend;
            boxVboData[2].Z = boxZnear;
            // z axis:
            boxVboData[3].X = 0f - boxXwidth / 2f;
            boxVboData[3].Y = boxYend;
            boxVboData[3].Z = boxZnear;

            // bottom:
            boxVboData[4].X = 0f - boxXwidth / 2f;
            boxVboData[4].Y = boxYstart;
            boxVboData[4].Z = boxZfar;
            // x axis:
            boxVboData[5].X = boxXwidth / 2f;
            boxVboData[5].Y = boxYstart;
            boxVboData[5].Z = boxZfar;
            // y axis:
            boxVboData[6].X = boxXwidth / 2f;
            boxVboData[6].Y = boxYend;
            boxVboData[6].Z = boxZfar;
            // z axis:
            boxVboData[7].X = 0f - boxXwidth / 2f;
            boxVboData[7].Y = boxYend;
            boxVboData[7].Z = boxZfar;

            //. TOP
            boxIndicesVboData[0] = 0;
            boxIndicesVboData[1] = 1;

            boxIndicesVboData[2] = 1;
            boxIndicesVboData[3] = 2;

            boxIndicesVboData[4] = 2;
            boxIndicesVboData[5] = 3;

            boxIndicesVboData[6] = 3;
            boxIndicesVboData[7] = 0;

            //. BOTTOM
            boxIndicesVboData[8] = 4;
            boxIndicesVboData[9] = 5;

            boxIndicesVboData[10] = 5;
            boxIndicesVboData[11] = 6;

            boxIndicesVboData[12] = 6;
            boxIndicesVboData[13] = 7;

            boxIndicesVboData[14] = 7;
            boxIndicesVboData[15] = 4;

            //. SIDES
            boxIndicesVboData[16] = 0;
            boxIndicesVboData[17] = 4;

            boxIndicesVboData[18] = 1;
            boxIndicesVboData[19] = 5;

            boxIndicesVboData[20] = 2;
            boxIndicesVboData[21] = 6;

            boxIndicesVboData[22] = 3;
            boxIndicesVboData[23] = 7;


            // add these vertices and indeces to the existing Vbo data
            int i;
            int currentVertCount = runningVertCounter;

            for (i = 0; i < numBoxPoints; ++i)
            {
                positionVboData[runningVertCounter] = boxVboData[i];
                runningVertCounter += 1;
            }

            for (i = 0; i < numBoxIndices; ++i)
            {
                indicesVboData[runningIndexCounter] = boxIndicesVboData[i] + (uint)currentVertCount;
                runningIndexCounter += 1;
            }
        }

        public void setNearAndFar(double whichNear, double whichFar)
        {
            boxZnear = (float)whichNear;
            boxZfar = (float)whichFar;
        }
        
       
        public void setExtents(double whichWidth, double whichBottom, double whichTop)
        {
            boxXwidth = (float)whichWidth;
            boxYstart = (float)whichBottom;
            boxYend = (float)whichTop;
        }

        public void forceUpdateOfModelBuffer()
        {
            initGridPoints();
            initAxisPoints();
            initBoxPoints();

            packBuffers();
        }

        private void packBuffers()
        {
            try
            {
                GL.GenBuffers(1, out positionVboHandle);
                GL.BindBuffer(BufferTarget.ArrayBuffer, positionVboHandle);
                GL.BufferData<OpenTK.Vector3>(BufferTarget.ArrayBuffer,
                    new IntPtr(runningVertCounter * OpenTK.Vector3.SizeInBytes),
                    positionVboData, BufferUsageHint.StaticDraw);
                // clear for new buffer:
                GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

                GL.GenBuffers(1, out indicesVboHandle);
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, indicesVboHandle);
                GL.BufferData(BufferTarget.ElementArrayBuffer,
                  new IntPtr(runningIndexCounter * sizeof(uint)),
                  indicesVboData, BufferUsageHint.StaticDraw);
                // clear for new buffer:
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
            }
            catch (Exception e)
            {
                //failed to access buffers
            }
        }

        public void createVAO(int whichShaderHandle, string whichVertPosVarName)
        {

            GL.GenVertexArrays(1, out vaoID);
            GL.BindVertexArray(vaoID);

            int arrayIndexCounter = 0;

            if (whichVertPosVarName != "")
            {
                GL.BindBuffer(BufferTarget.ArrayBuffer, positionVboHandle);
                GL.VertexAttribPointer(arrayIndexCounter, 3, VertexAttribPointerType.Float, false, OpenTK.Vector3.SizeInBytes, 0);
                GL.EnableVertexAttribArray(arrayIndexCounter);
                GL.BindAttribLocation(whichShaderHandle, arrayIndexCounter, whichVertPosVarName);
                arrayIndexCounter += 1;
            }


            GL.BindBuffer(BufferTarget.ElementArrayBuffer, indicesVboHandle);


            // clear for new VAO:
            GL.BindVertexArray(0);
        }

        public void drawGrid()
        {
            GL.BindVertexArray(vaoID);
            GL.DrawElements(BeginMode.Lines, actualNumberOfGridLinesToDraw * 2, DrawElementsType.UnsignedInt, 0);
            GL.BindVertexArray(0);
        }

        public void drawAxis()
        {
            GL.BindVertexArray(vaoID);
            GL.DrawElements(BeginMode.Lines, numAxisIndices, DrawElementsType.UnsignedInt, (runningIndexCounter - numAxisIndices - numBoxIndices) * sizeof(uint));
            GL.BindVertexArray(0);
        }

        public void drawSensingVolume()
        {
            GL.BindVertexArray(vaoID);
            GL.DrawElements(BeginMode.Lines, numBoxIndices, DrawElementsType.UnsignedInt, (runningIndexCounter - numBoxIndices) * sizeof(uint));
            GL.BindVertexArray(0);
        }

        public void exitApp()
        {
            GL.DeleteVertexArrays(1, ref vaoID);
        }
    }
}
