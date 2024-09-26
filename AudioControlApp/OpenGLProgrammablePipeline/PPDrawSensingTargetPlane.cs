using System;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;


namespace AudioControlApp.OpenGLProgrammablePipeline
{
    class PPDrawSensingTargetPlane
    {
        

        // buffer arrays:
        int positionVboHandle,
            normalVboHandle,
            uvVboHandle,
            indicesVboHandle;
        // outline of shae buffer arrays:
        int outlineIndicesVboHandle;

        private uint vaoID;
        private uint outlineVaoID;

        private bool readyToDraw = false;



        public bool drawFill()
        {
            if (readyToDraw)
            {
                if (vaoID == 0)
                    return false;

                GL.BindVertexArray(vaoID);

                GL.DrawElements(BeginMode.Triangles, indices.Length, DrawElementsType.UnsignedInt, 0);

                GL.BindVertexArray(0);
                return true;
            }
            else
                return false;
        }

        public bool drawOutline()
        {
            if (readyToDraw)
            {
                if (outlineVaoID == 0)
                    return false;

                GL.BindVertexArray(outlineVaoID);

                GL.DrawElements(BeginMode.Lines, outlineIndices.Length, DrawElementsType.UnsignedInt, 0);

                GL.BindVertexArray(0);
                return true;
            }
            else
                return false;
        }

        /// <summary>
        /// pass name of variable inside shader program
        /// passing "" skips that attribute in the VAO
        /// </summary>
        /// <param name="whichShaderHandle"></param>
        /// <param name="whichVertPosVarName"></param>
        /// <param name="whichNormVarName"></param>
        /// <param name="whichUVVarName"></param>
        public void createVAO(int whichShaderHandle, string whichVertPosVarName, string whichNormVarName, string whichUVVarName)
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

            if (whichNormVarName != "")
            {
                GL.BindBuffer(BufferTarget.ArrayBuffer, normalVboHandle);
                GL.VertexAttribPointer(arrayIndexCounter, 3, VertexAttribPointerType.Float, false, OpenTK.Vector3.SizeInBytes, 0);
                GL.EnableVertexAttribArray(arrayIndexCounter);
                GL.BindAttribLocation(whichShaderHandle, arrayIndexCounter, whichNormVarName);
                arrayIndexCounter += 1;
            }

            if (whichUVVarName != "")
            {
                GL.BindBuffer(BufferTarget.ArrayBuffer, uvVboHandle);
                GL.VertexAttribPointer(arrayIndexCounter, 2, VertexAttribPointerType.Float, false, OpenTK.Vector2.SizeInBytes, 0);
                GL.EnableVertexAttribArray(arrayIndexCounter);
                GL.BindAttribLocation(whichShaderHandle, arrayIndexCounter, whichUVVarName);
            }

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, indicesVboHandle);


            // clear for new VAO (outline of shape)
            GL.BindVertexArray(0);


            GL.GenVertexArrays(1, out outlineVaoID);
            GL.BindVertexArray(outlineVaoID);

            arrayIndexCounter = 0;

            if (whichVertPosVarName != "")
            {
                GL.BindBuffer(BufferTarget.ArrayBuffer, positionVboHandle);
                GL.VertexAttribPointer(arrayIndexCounter, 3, VertexAttribPointerType.Float, false, OpenTK.Vector3.SizeInBytes, 0);
                GL.EnableVertexAttribArray(arrayIndexCounter);
                GL.BindAttribLocation(whichShaderHandle, arrayIndexCounter, whichVertPosVarName);
                arrayIndexCounter += 1;
            }

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, outlineIndicesVboHandle);


            // clear for new VAO:
            GL.BindVertexArray(0);

            readyToDraw = true;
        }

        /*
        public void assignQuadSizeAndPosition(float whichWidth, float whichHeight, float whichX, float whichY, float whichZ)
        {
            quadW = whichHeight;
            quadH = whichHeight;

            float zdepth = whichZ;

            Position[0] = -quadW / 2.0f;
            Position[1] = -quadH / 2.0f;
            Position[2] = zdepth;
            Position[3] = quadW / 2.0f;
            Position[4] = -quadH / 2.0f;
            Position[5] = zdepth;
            Position[6] = quadW / 2.0f;
            Position[7] = quadH / 2.0f;
            Position[8] = zdepth;
            Position[9] = -quadW / 2.0f;
            Position[10] = quadH / 2.0f;
            Position[11] = zdepth;

            // convert to vector3 for VBO
            positionVboData[0].X = Position[0] + whichX;
            positionVboData[0].Y = Position[1] + whichY;
            positionVboData[0].Z = Position[2] + whichZ;

            positionVboData[1].X = Position[3] + whichX;
            positionVboData[1].Y = Position[4] + whichY;
            positionVboData[1].Z = Position[5] + whichZ;

            positionVboData[2].X = Position[6] + whichX;
            positionVboData[2].Y = Position[7] + whichY;
            positionVboData[2].Z = Position[8] + whichZ;

            positionVboData[3].X = Position[9] + whichX;
            positionVboData[3].Y = Position[10] + whichY;
            positionVboData[3].Z = Position[11] + whichZ;

            // texture is always the same here:
            uvVboData[0].X = Texture[0];
            uvVboData[0].Y = Texture[1];
            uvVboData[1].X = Texture[2];
            uvVboData[1].Y = Texture[3];
            uvVboData[2].X = Texture[4];
            uvVboData[2].Y = Texture[5];
            uvVboData[3].X = Texture[6];
            uvVboData[3].Y = Texture[7];

            // norms always the same here:
            for (int i = 0; i < normVboData.Length; ++i)
            {
                normVboData[i].X = 0.0f;
                normVboData[i].Y = 0.0f;
                normVboData[i].Z = 1.0f;
            }

            repackPositionBuffer();
        }

        */
        public void initOpenGL(Vector3 ULPosn, Vector3 LRPosn)
        {
            // convert to vector3 for VBO
            positionVboData[0] = ULPosn;

            positionVboData[1].X = LRPosn.X;
            positionVboData[1].Y = ULPosn.Y;
            positionVboData[1].Z = ULPosn.Z;

            positionVboData[2] = LRPosn;

            positionVboData[3].X = ULPosn.X;
            positionVboData[3].Y = LRPosn.Y;
            positionVboData[3].Z = LRPosn.Z;

            // texture is always the same here:
            uvVboData[0].X = Texture[0];
            uvVboData[0].Y = Texture[1];
            uvVboData[1].X = Texture[2];
            uvVboData[1].Y = Texture[3];
            uvVboData[2].X = Texture[4];
            uvVboData[2].Y = Texture[5];
            uvVboData[3].X = Texture[6];
            uvVboData[3].Y = Texture[7];

            // norms always the same here:
            for (int i = 0; i < normVboData.Length; ++i)
            {
                normVboData[i].X = 0.0f;
                normVboData[i].Y = 0.0f;
                normVboData[i].Z = 1.0f;
            }

            //repackPositionBuffer();
            packBuffers();
        }
        /*
        private void updateVertPositions()
        {
            System.Diagnostics.Debug.WriteLine("[TEXTUREDQUAD] updating vert positions for [" + quadW + ", " + quadH + " ]");

            float zdepth = 2.0f;

            Position[0] = -quadW / 2.0f;
            Position[1] = -quadH / 2.0f;
            Position[2] = zdepth;
            Position[3] = quadW / 2.0f;
            Position[4] = -quadH / 2.0f;
            Position[5] = zdepth;
            Position[6] = quadW / 2.0f;
            Position[7] = quadH / 2.0f;
            Position[8] = zdepth;
            Position[9] = -quadW / 2.0f;
            Position[10] = quadH / 2.0f;
            Position[11] = zdepth;

            // convert to vector3 for VBO
            positionVboData[0].X = Position[0];
            positionVboData[0].Y = Position[1];
            positionVboData[0].Z = Position[2];

            positionVboData[1].X = Position[3];
            positionVboData[1].Y = Position[4];
            positionVboData[1].Z = Position[5];

            positionVboData[2].X = Position[6];
            positionVboData[2].Y = Position[7];
            positionVboData[2].Z = Position[8];

            positionVboData[3].X = Position[9];
            positionVboData[3].Y = Position[10];
            positionVboData[3].Z = Position[11];

            // texture is always the same here:
            uvVboData[0].X = Texture[0];
            uvVboData[0].Y = Texture[1];
            uvVboData[1].X = Texture[2];
            uvVboData[1].Y = Texture[3];
            uvVboData[2].X = Texture[4];
            uvVboData[2].Y = Texture[5];
            uvVboData[3].X = Texture[6];
            uvVboData[3].Y = Texture[7];

            // norms always the same here:
            for (int i = 0; i < normVboData.Length; ++i)
            {
                normVboData[i].X = 0.0f;
                normVboData[i].Y = 0.0f;
                normVboData[i].Z = 1.0f;
            }

        }
        */
        private float[] Position = {
            // Front face
            -1000f, -1000f, -1000f,	
            1000f, -1000f, 1000f, 	
            1000f, 1000f, 1000f, 	
            -1000f, 1000f, 1000f
                                   };

        private OpenTK.Vector3[] positionVboData = new OpenTK.Vector3[4];
        private OpenTK.Vector2[] uvVboData = new OpenTK.Vector2[4];
        private OpenTK.Vector3[] normVboData = new OpenTK.Vector3[4];

        private float[] Texture = {
			// Font Face
			0f, 1f, 
			1f, 1f, 
			1f, 0f, 
			0f, 0f
                                        };
        private uint[] indices = {
			// Font face
			0, 1, 2, 2, 3, 0
                                     };
        private uint[] outlineIndices = {
			// Font face
			0, 1, 1, 2, 2, 3, 3, 0
                                     };


        private void repackPositionBuffer()
        {

            GL.BindBuffer(BufferTarget.ArrayBuffer, positionVboHandle);
            GL.BufferData<OpenTK.Vector3>(BufferTarget.ArrayBuffer,
                new IntPtr(positionVboData.Length * OpenTK.Vector3.SizeInBytes),
                positionVboData, BufferUsageHint.DynamicDraw);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        }

        private void packBuffers()
        {

            // pack the buffers:

            GL.GenBuffers(1, out positionVboHandle);
            GL.BindBuffer(BufferTarget.ArrayBuffer, positionVboHandle);
            GL.BufferData<OpenTK.Vector3>(BufferTarget.ArrayBuffer,
                new IntPtr(positionVboData.Length * OpenTK.Vector3.SizeInBytes),
                positionVboData, BufferUsageHint.DynamicDraw);
            // clear for new buffer:
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);


            GL.GenBuffers(1, out normalVboHandle);
            GL.BindBuffer(BufferTarget.ArrayBuffer, normalVboHandle);
            GL.BufferData<OpenTK.Vector3>(BufferTarget.ArrayBuffer,
                new IntPtr(normVboData.Length * OpenTK.Vector3.SizeInBytes),
                normVboData, BufferUsageHint.StaticDraw);
            // clear for new buffer:
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);


            GL.GenBuffers(1, out uvVboHandle);
            GL.BindBuffer(BufferTarget.ArrayBuffer, uvVboHandle);
            GL.BufferData<OpenTK.Vector2>(BufferTarget.ArrayBuffer,
                new IntPtr(uvVboData.Length * OpenTK.Vector2.SizeInBytes),
                uvVboData, BufferUsageHint.StaticDraw);
            // clear for new buffer:
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);


            GL.GenBuffers(1, out indicesVboHandle);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, indicesVboHandle);
            GL.BufferData(BufferTarget.ElementArrayBuffer,
              new IntPtr(indices.Length * sizeof(uint)),
              indices, BufferUsageHint.StaticDraw);
            // clear for new buffer:
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);

            GL.GenBuffers(1, out outlineIndicesVboHandle);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, outlineIndicesVboHandle);
            GL.BufferData(BufferTarget.ElementArrayBuffer,
              new IntPtr(outlineIndices.Length * sizeof(uint)),
              outlineIndices, BufferUsageHint.StaticDraw);
            // clear for new buffer:
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);

        }


        public void exitApp() // clean up:
        {
            GL.DeleteVertexArrays(1, ref vaoID);
        }
    }
}
