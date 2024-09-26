using System;
using System.Collections.Generic;
using System.Text;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace AudioControlApp.OpenGLProgrammablePipeline
{
    class PPOpenGLRingWithPerctFill
    {
        // position and size variables
        private float innerCircleRadius = 15.0f;
        private float outerCircleRadius = 20.0f;
        private float clockPosZ = 0.0f;

        private int currentTimePointInCircle;

        private static int totalPointsInCircle = 90;
        private float[,] innerCirclePositions = new float[totalPointsInCircle, 2]; // 36 points in circle, x and y for each position
        private float[,] outerCirclePositions = new float[totalPointsInCircle, 2]; // 36 points in circle, x and y for each position



        public float[] circleColorA = new float[4]; // RGBA of active color
        public float[] circleColorB = new float[4]; // RGBA of secondary color

        private float unfilledAlphaValue = 0.8f; // this requires "boost" when used with the blur shader

        public PPOpenGLRingWithPerctFill()
        {
            assignDefaultColors();
            initCirclePositions(); // users default radius settings
            packBuffers();
        }

        private void assignDefaultColors()
        {
            int i;
            for (i = 0; i < 3; ++i)
            {
                circleColorA[i] = 1.0f;
                circleColorB[i] = 1.0f;
            }

            // alpha is different:
            circleColorA[3] = unfilledAlphaValue;
            circleColorB[3] = 1.0f;

        }

        public void updateColors(float whichR, float whichG, float whichB)
        {
            circleColorA[0] = whichR;
            circleColorB[0] = whichR;
            circleColorA[1] = whichG;
            circleColorB[1] = whichG;
            circleColorA[2] = whichB;
            circleColorB[2] = whichB;

            // alpha is different:
            circleColorA[3] = unfilledAlphaValue;
            circleColorB[3] = 1.0f;
        }

        public void setCircleSizes(float whichInner, float whichOuter)
        {
            innerCircleRadius = whichInner;
            outerCircleRadius = whichOuter;

            initCirclePositions();

            packBuffers();
        }

        private void initCirclePositions()
        {
            int i, ringPointCounter, indexCounter;
            double whichAngle, whichPosXi, whichPosYi, whichPosXo, whichPosYo;
            double deltaAngle = 360.0 / (double)totalPointsInCircle;

            whichPosXi = 0.0;
            whichPosXo = 0.0;
            whichPosYi = (double)innerCircleRadius;
            whichPosYo = (double)outerCircleRadius;

            ringPointCounter = 0;
            indexCounter = 0;

            for (i = 0; i < totalPointsInCircle; ++i)
            {
                whichAngle = 360.0 - (double)i * deltaAngle;
                if ((whichAngle > 0.1) & (whichAngle < 90))
                {
                    //whichPosX = (double)circleRadius * Math.Cos(whichAngle * Math.PI / 180.0);
                    //whichPosY = (double)circleRadius * Math.Sin(whichAngle * Math.PI / 180.0);
                    whichPosXi = ((double)innerCircleRadius * Math.Sin(whichAngle * Math.PI / 180.0));
                    whichPosYi = (double)innerCircleRadius * Math.Cos(whichAngle * Math.PI / 180.0);
                    whichPosXo = ((double)outerCircleRadius * Math.Sin(whichAngle * Math.PI / 180.0));
                    whichPosYo = (double)outerCircleRadius * Math.Cos(whichAngle * Math.PI / 180.0);

                }
                else if ((whichAngle >= 90) & (whichAngle < 180))
                {
                    whichAngle -= 90.0;
                    //whichPosX = 0.0 - ((double)circleRadius * Math.Sin(whichAngle * Math.PI / 180.0));
                    //whichPosY = (double)circleRadius * Math.Cos(whichAngle * Math.PI / 180.0);
                    whichPosXi = ((double)innerCircleRadius * Math.Cos(whichAngle * Math.PI / 180.0));
                    whichPosYi = 0.0 - (double)innerCircleRadius * Math.Sin(whichAngle * Math.PI / 180.0);
                    whichPosXo = ((double)outerCircleRadius * Math.Cos(whichAngle * Math.PI / 180.0));
                    whichPosYo = 0.0 - (double)outerCircleRadius * Math.Sin(whichAngle * Math.PI / 180.0);
                }
                else if ((whichAngle >= 180) & (whichAngle < 270))
                {
                    whichAngle -= 180.0f;
                    //whichPosX = 0.0 - ((double)circleRadius * Math.Cos(whichAngle * Math.PI / 180.0));
                    //whichPosY = 0.0 - ((double)circleRadius * Math.Sin(whichAngle * Math.PI / 180.0));
                    whichPosXi = 0.0 - (double)innerCircleRadius * Math.Sin(whichAngle * Math.PI / 180.0);
                    whichPosYi = 0.0 - ((double)innerCircleRadius * Math.Cos(whichAngle * Math.PI / 180.0));
                    whichPosXo = 0.0 - (double)outerCircleRadius * Math.Sin(whichAngle * Math.PI / 180.0);
                    whichPosYo = 0.0 - ((double)outerCircleRadius * Math.Cos(whichAngle * Math.PI / 180.0));
                }
                else if ((whichAngle >= 270) & (whichAngle < 359.9))
                {

                    whichAngle -= 270.0f;
                    //whichPosX = (double)circleRadius * Math.Sin(whichAngle * Math.PI / 180.0);
                    //whichPosY = 0.0 - ((double)circleRadius * Math.Cos(whichAngle * Math.PI / 180.0));
                    whichPosXi = 0.0 - (double)innerCircleRadius * Math.Cos(whichAngle * Math.PI / 180.0);
                    whichPosYi = (double)innerCircleRadius * Math.Sin(whichAngle * Math.PI / 180.0);
                    whichPosXo = 0.0 - (double)outerCircleRadius * Math.Cos(whichAngle * Math.PI / 180.0);
                    whichPosYo = (double)outerCircleRadius * Math.Sin(whichAngle * Math.PI / 180.0);

                }
                else // if 360 or 0
                {
                    //whichPosX = (double)circleRadius;
                    //whichPosY = 0.0;
                    whichPosXi = 0.0;
                    whichPosYi = (double)innerCircleRadius;
                    whichPosXo = 0.0;
                    whichPosYo = (double)outerCircleRadius;
                }

                /*
                innerCirclePositions[i, 0] = (float)whichPosXi;
                innerCirclePositions[i, 1] = (float)whichPosYi;
                outerCirclePositions[i, 0] = (float)whichPosXo;
                outerCirclePositions[i, 1] = (float)whichPosYo;
                */

                // fill ring points inside 0, outside 0, inside 1, outside 1.... 
                positionVboData[ringPointCounter].X = (float)whichPosXo;
                positionVboData[ringPointCounter].Z = (float)whichPosYo;
                positionVboData[ringPointCounter].Y = 0.0f;
                ringPointCounter += 1;
                positionVboData[ringPointCounter].X = (float)whichPosXi;
                positionVboData[ringPointCounter].Z = (float)whichPosYi;
                positionVboData[ringPointCounter].Y = 0.0f;
                ringPointCounter += 1;

                
            }

            numberOfIndeces = 0;
            numberOfPositions = 0;
            // connect the dots with index data 0, 2, 1,  2, 3, 1,  2, 4, 3,  4, 5, 3..
            for (i = 0; i < totalPointsInCircle; ++i)
            {
                indicesVboData[numberOfIndeces] = (uint)numberOfPositions;
                numberOfIndeces += 1;
                indicesVboData[numberOfIndeces] = (uint)(numberOfPositions + 2);
                numberOfIndeces += 1;
                indicesVboData[numberOfIndeces] = (uint)(numberOfPositions + 1);
                numberOfIndeces += 1;

                indicesVboData[numberOfIndeces] = (uint)(numberOfPositions + 2);
                numberOfIndeces += 1;
                indicesVboData[numberOfIndeces] = (uint)(numberOfPositions + 3);
                numberOfIndeces += 1;
                indicesVboData[numberOfIndeces] = (uint)(numberOfPositions + 1);
                numberOfIndeces += 1;

                numberOfPositions += 2;
            }

            //numberOfPositions -= 2;
            // there are n number of divisions in circle, there are n+1 quads in circle
            // finish off with one last set of vertices (closing loop)
            indicesVboData[numberOfIndeces] = (uint)(numberOfPositions-2);
            numberOfIndeces += 1;
            indicesVboData[numberOfIndeces] = (uint)0;
            numberOfIndeces += 1;
            indicesVboData[numberOfIndeces] = (uint)(numberOfPositions - 1);
            numberOfIndeces += 1;

            indicesVboData[numberOfIndeces] = (uint)0;
            numberOfIndeces += 1;
            indicesVboData[numberOfIndeces] = (uint)1;
            numberOfIndeces += 1;
            indicesVboData[numberOfIndeces] = (uint)(numberOfPositions - 1);
            numberOfIndeces += 1;

            //numberOfPositions += 2;


            /*
            for (i = 0; i < indicesVboData.Length; i = i + 6)
            {

                indicesVboData[i] = (uint)indexCounter;
                indicesVboData[i + 1] = (uint)(indexCounter + 2);
                indicesVboData[i + 2] = (uint)(indexCounter + 1);

                indicesVboData[i + 3] = (uint)(indexCounter + 2);
                indicesVboData[i + 4] = (uint)(indexCounter + 3);
                indicesVboData[i + 5] = (uint)(indexCounter + 1);
                
                indexCounter += 2;

            }
            

            // close the circle with the final set of points (which is also the first set of points)
            indexCounter = 0;

            indicesVboData[0] = (uint)indexCounter;
            indicesVboData[1] = (uint)(indexCounter + 2);
            indicesVboData[2] = (uint)(indexCounter + 1);

            indicesVboData[3] = (uint)(indexCounter + 2);
            indicesVboData[4] = (uint)(indexCounter + 3);
            indicesVboData[5] = (uint)(indexCounter + 1);

            indicesVboData[indicesVboData.Length - 6] = (uint)indexCounter;
            indicesVboData[indicesVboData.Length - 5] = (uint)(indexCounter + 2);
            indicesVboData[indicesVboData.Length - 4] = (uint)(indexCounter + 1);

            indicesVboData[indicesVboData.Length - 3] = (uint)(indexCounter + 2);
            indicesVboData[indicesVboData.Length - 2] = (uint)(indexCounter + 3);
            indicesVboData[indicesVboData.Length - 1] = (uint)(indexCounter + 1); 
            */

        }

        public void updatePercentageFill(float whichPercentage)
        {
            if (whichPercentage >= 1.0f)
                whichPercentage = 0.99f;
            currentTimePointInCircle = (int)Math.Ceiling((double)whichPercentage * (double)totalPointsInCircle);
            if (currentTimePointInCircle > totalPointsInCircle)
                currentTimePointInCircle = totalPointsInCircle;

        }

        private int positionVboHandle, indicesVboHandle, vaoID;
        private Vector3[] positionVboData = new Vector3[totalPointsInCircle * 2]; // 3d points of inside and outside over # of points
        private int numberOfPositions = 0;
        uint[] indicesVboData = new uint[((totalPointsInCircle+2) * 6)]; // total points * 2 triangles * 3 points/triangle
        private int numberOfIndeces = 0;


        private void packBuffers()
        {

            // pack the buffers:

            GL.GenBuffers(1, out positionVboHandle);
            GL.BindBuffer(BufferTarget.ArrayBuffer, positionVboHandle);
            GL.BufferData<OpenTK.Vector3>(BufferTarget.ArrayBuffer,
                new IntPtr(numberOfPositions * OpenTK.Vector3.SizeInBytes),
                positionVboData, BufferUsageHint.StaticDraw);
            // clear for new buffer:
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

            
            GL.GenBuffers(1, out indicesVboHandle);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, indicesVboHandle);
            GL.BufferData(BufferTarget.ElementArrayBuffer,
              new IntPtr(numberOfIndeces * sizeof(uint)),
              indicesVboData, BufferUsageHint.StaticDraw);
            // clear for new buffer:
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);

        }


        /// <summary>
        /// pass name of variable inside shader program
        /// passing "" skips that attribute in the VAO
        /// </summary>
        /// <param name="whichShaderHandle"></param>
        /// <param name="whichVertPosVarName"></param>
        /// <param name="whichNormVarName"></param>
        /// <param name="whichUVVarName"></param>
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

        int pointToDrawFrom = 0;
        int pointToDrawTo = totalPointsInCircle;
        int pointsToDraw = totalPointsInCircle;
        int indexOffset = (0 * 6) * sizeof(uint);
        int noOffset = (0 * 6) * sizeof(uint);
        public void drawCirclePartA()  // this is the "unused" part of the circle (transparent white)
        {

            pointToDrawFrom = 0; // the part that is fixed at the top
            pointToDrawTo = totalPointsInCircle - currentTimePointInCircle; // the part that mates up to part B below

            pointsToDraw = (pointToDrawTo - pointToDrawFrom) * 6;

            indexOffset = (pointToDrawFrom * 6) * sizeof(uint);

            GL.BindVertexArray(vaoID);
            GL.DrawElements(BeginMode.Triangles, pointsToDraw, DrawElementsType.UnsignedInt, indexOffset);
            GL.BindVertexArray(0);
            
        }

        // TODO: change primary color in shader between these draw commands

        public void drawCirclePartB() // this is the main part of the circle (white)
        {

            pointToDrawFrom = totalPointsInCircle - currentTimePointInCircle; // the part that mates up to the transparent part
            //pointToDrawFrom = 0;

            pointToDrawTo = totalPointsInCircle + 1; // the part that is fixed at the top

            pointsToDraw = (pointToDrawTo - pointToDrawFrom) * 6;

            indexOffset = (pointToDrawFrom * 6) * sizeof(uint);

            GL.BindVertexArray(vaoID);
            GL.DrawElements(BeginMode.Triangles, pointsToDraw, DrawElementsType.UnsignedInt, indexOffset);
            //GL.DrawElements(BeginMode.Triangles, indicesVboData.Length, DrawElementsType.UnsignedInt, noOffset);
            GL.BindVertexArray(0);
            
        }

        public Vector4 getColorA(float whichAddedAlpha)
        {
            return new Vector4(circleColorA[0], circleColorA[1], circleColorA[2], circleColorA[3] * whichAddedAlpha);
        }

        public Vector4 getColorB(float whichAddedAlpha)
        {
            return new Vector4(circleColorB[0], circleColorB[1], circleColorB[2], circleColorB[3] * whichAddedAlpha);
        }

        /*
        public void draw(float whichAddedAlpha)
        {
            int i;

            if (currentTimePointInCircle > 0)
            {
                GL.Color4(circleColorA[0], circleColorA[1], circleColorA[2], circleColorA[3] * whichAddedAlpha);
                GL.Begin(BeginMode.QuadStrip);

                for (i = 0; i < (totalPointsInCircle - currentTimePointInCircle + 1); ++i) // active portion of timer
                {
                    GL.Vertex3(0 - innerCirclePositions[i, 0], innerCirclePositions[i, 1], clockPosZ);
                    GL.Vertex3(0 - outerCirclePositions[i, 0], outerCirclePositions[i, 1], clockPosZ);

                }

                GL.End();
            }


            if (currentTimePointInCircle > 0)
            {
                GL.Color4(circleColorB[0], circleColorB[1], circleColorB[2], circleColorB[3] * whichAddedAlpha);
                GL.Begin(BeginMode.QuadStrip);

                for (i = (totalPointsInCircle - currentTimePointInCircle); i < totalPointsInCircle; ++i) // inactive portion of timer
                {
                    GL.Vertex3(0 - innerCirclePositions[i, 0], innerCirclePositions[i, 1], clockPosZ);
                    GL.Vertex3(0 - outerCirclePositions[i, 0], outerCirclePositions[i, 1], clockPosZ);
                }

                GL.Vertex3(0 - innerCirclePositions[0, 0], innerCirclePositions[0, 1], clockPosZ); // final point closes circle
                GL.Vertex3(0 - outerCirclePositions[0, 0], outerCirclePositions[0, 1], clockPosZ); // final point closes circle

                GL.End();
            }
        }
        */

        public void exitApp()
        {
            GL.DeleteVertexArrays(1, ref vaoID);
        }
    }
}
