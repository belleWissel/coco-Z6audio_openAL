using System;
using System.Collections.Generic;
using System.Text;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace ConnectionsWallDisplay.OpenGLProgrammablePipeline
{
    class PPRingOfLines
    {
        // position and size variables
        private float innerCircleRadius = 15.0f;
        private float outerCircleRadius = 20.0f;

        private int numberOfLinesInCircle = 25;
        private static int maxNumberOfLines = 500;

        private float[,] innerCirclePositions = new float[maxNumberOfLines, 2]; // 36 points in circle, x and y for each position
        private float[,] outerCirclePositions = new float[maxNumberOfLines, 2]; // 36 points in circle, x and y for each position

        private int positionVboHandle, indicesVboHandle, vaoID;
        private Vector3[] positionVboData = new Vector3[maxNumberOfLines * 2]; // 3d points of inside and outside over # of points
        private int positionCounter = 0;
        uint[] indicesVboData = new uint[maxNumberOfLines]; // total points / 2 points per line
        private int indexCounter = 0; // final count of 


        public PPRingOfLines(float whichInner, float whichOuter, int whichNumberOfLines)
        {
            innerCircleRadius = whichInner;
            outerCircleRadius = whichOuter;
            numberOfLinesInCircle = whichNumberOfLines;

            if (numberOfLinesInCircle > maxNumberOfLines)
                numberOfLinesInCircle = maxNumberOfLines;

            
            
            initCirclePositions(); // users default radius settings
            packBuffers();
        }




        private void initCirclePositions()
        {
            int i;
            double whichAngle, whichPosXi, whichPosYi, whichPosXo, whichPosYo;
            double deltaAngle = 360.0 / (double)numberOfLinesInCircle;

            whichPosXi = 0.0;
            whichPosXo = 0.0;
            whichPosYi = (double)innerCircleRadius;
            whichPosYo = (double)outerCircleRadius;

            positionCounter = 0;
            // define points in ring:
            for (i = 0; i < numberOfLinesInCircle; ++i)
            {
                whichAngle = 360.0 - (double)i * deltaAngle;
                if ((whichAngle > 0.01) & (whichAngle < 90))
                {
                    whichPosXi = ((double)innerCircleRadius * Math.Sin(whichAngle * Math.PI / 180.0));
                    whichPosYi = (double)innerCircleRadius * Math.Cos(whichAngle * Math.PI / 180.0);
                    whichPosXo = ((double)outerCircleRadius * Math.Sin(whichAngle * Math.PI / 180.0));
                    whichPosYo = (double)outerCircleRadius * Math.Cos(whichAngle * Math.PI / 180.0);
                }
                else if ((whichAngle >= 90) & (whichAngle < 180))
                {
                    whichAngle -= 90.0;
                    whichPosXi = ((double)innerCircleRadius * Math.Cos(whichAngle * Math.PI / 180.0));
                    whichPosYi = 0.0 - (double)innerCircleRadius * Math.Sin(whichAngle * Math.PI / 180.0);
                    whichPosXo = ((double)outerCircleRadius * Math.Cos(whichAngle * Math.PI / 180.0));
                    whichPosYo = 0.0 - (double)outerCircleRadius * Math.Sin(whichAngle * Math.PI / 180.0);
                }
                else if ((whichAngle >= 180) & (whichAngle < 270))
                {
                    whichAngle -= 180.0f;
                    whichPosXi = 0.0 - (double)innerCircleRadius * Math.Sin(whichAngle * Math.PI / 180.0);
                    whichPosYi = 0.0 - ((double)innerCircleRadius * Math.Cos(whichAngle * Math.PI / 180.0));
                    whichPosXo = 0.0 - (double)outerCircleRadius * Math.Sin(whichAngle * Math.PI / 180.0);
                    whichPosYo = 0.0 - ((double)outerCircleRadius * Math.Cos(whichAngle * Math.PI / 180.0));
                }
                else if ((whichAngle >= 270) & (whichAngle < 359.9))
                {
                    whichAngle -= 270.0f;
                    whichPosXi = 0.0 - (double)innerCircleRadius * Math.Cos(whichAngle * Math.PI / 180.0);
                    whichPosYi = (double)innerCircleRadius * Math.Sin(whichAngle * Math.PI / 180.0);
                    whichPosXo = 0.0 - (double)outerCircleRadius * Math.Cos(whichAngle * Math.PI / 180.0);
                    whichPosYo = (double)outerCircleRadius * Math.Sin(whichAngle * Math.PI / 180.0);
                }
                else // if 360 or 0
                {
                    whichPosXi = 0.0;
                    whichPosYi = (double)innerCircleRadius;
                    whichPosXo = 0.0;
                    whichPosYo = (double)outerCircleRadius;
                }

                // fill ring points inside 0, outside 0, inside 1, outside 1.... 
                positionVboData[positionCounter].X = (float)whichPosXo;
                positionVboData[positionCounter].Y = 0.0f;
                positionVboData[positionCounter].Z = (float)whichPosYo;
                positionCounter += 1;
                positionVboData[positionCounter].X = (float)whichPosXi;
                positionVboData[positionCounter].Y = 0.0f;
                positionVboData[positionCounter].Z = (float)whichPosYi;
                positionCounter += 1;
            }

            indexCounter = 0;
            // connect the dots with index data 0-1, 2-3, 4-5... (using "GL.Lines" mode)
            for (i = 0; i < numberOfLinesInCircle; ++i)
            {
                indicesVboData[indexCounter] = (uint)indexCounter;
                indicesVboData[indexCounter + 1] = (uint)(indexCounter + 1);
                
                indexCounter += 2;
            }
        }


        private void packBuffers()
        {
            GL.GenBuffers(1, out positionVboHandle);
            GL.BindBuffer(BufferTarget.ArrayBuffer, positionVboHandle);
            GL.BufferData<OpenTK.Vector3>(BufferTarget.ArrayBuffer,
                new IntPtr(positionCounter * OpenTK.Vector3.SizeInBytes),
                positionVboData, BufferUsageHint.StaticDraw);
            // clear for new buffer:
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

            GL.GenBuffers(1, out indicesVboHandle);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, indicesVboHandle);
            GL.BufferData(BufferTarget.ElementArrayBuffer,
              new IntPtr(indexCounter * sizeof(uint)),
              indicesVboData, BufferUsageHint.StaticDraw);
            // clear for new buffer:
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
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

        public void draw()
        {
            GL.BindVertexArray(vaoID);
            GL.DrawElements(BeginMode.Lines, indexCounter, DrawElementsType.UnsignedInt, 0);
            GL.BindVertexArray(0);
        }

        public void exitApp()
        {
            GL.DeleteVertexArrays(1, ref vaoID);
        }
    }
}
