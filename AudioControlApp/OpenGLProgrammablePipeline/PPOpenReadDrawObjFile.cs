using System;
using System.Diagnostics;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System.IO;
using Meshomatic;

namespace AudioControlApp.OpenGLProgrammablePipeline
{
    class PPOpenReadDrawObjFile
    {

        float globalPixelScale = 18.9f;
        bool modelLoaded = false;
        string modelFile = "Models/kinect.obj";

        // meshomatic object:
        MeshData m;

        // buffer offsets:
        //uint dataBuffer;
        //uint indexBuffer;
        //uint tex;
        //int vertOffset, normOffset, texcoordOffset;

        // buffer arrays:
        int positionVboHandle,
            normalVboHandle,
            uvVboHandle,
            indicesVboHandle;

        private uint vaoID;

        private int lengthOfIndexAray = 0;



        public PPOpenReadDrawObjFile(double whichGlobalPixelScale, string whichModelFile)
        {
            modelFile = whichModelFile;
            
            globalPixelScale = (float)whichGlobalPixelScale;
        }



        public void initOpenGL()
        {
            if (File.Exists(FileUtils.MakeAbsolutePath(modelFile)))
            {
                m = new ObjLoader().LoadFile(modelFile);
            }

            /*
            if (File.Exists(Utils.MakeAbsolutePath(textureFile)))
            {
                tex = LoadTex(textureFile);
            }
            */

            if (m != null) // did it load successfully?
            {
                LoadBuffers(m);
                modelLoaded = true;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("* Failed to load file: " + modelFile);
            }
        }


        void LoadBuffers(MeshData m)
        {
            float[] verts, norms, texcoords;
            uint[] indices;
            m.OpenGLArrays(out verts, out norms, out texcoords, out indices);

            // need to define lengths of vector arrays before instantiating them:
            int numVerts = (int)Math.Floor((double)verts.Length / 3.0);
            int numNorms = (int)Math.Floor((double)norms.Length / 3.0);
            int numTexCoords = (int)Math.Floor((double)texcoords.Length / 2.0);
            lengthOfIndexAray = indices.Length;

            OpenTK.Vector3[] positionVboData = new OpenTK.Vector3[numVerts];
            OpenTK.Vector3[] normVboData = new OpenTK.Vector3[numNorms];
            OpenTK.Vector2[] uvVboData = new OpenTK.Vector2[numTexCoords];

            int i;
            int vertexCounter = 0;
            for (i = 0; i < verts.Length; i = i + 3)
            {
                positionVboData[vertexCounter].X = verts[i] * globalPixelScale;
                positionVboData[vertexCounter].Z = verts[i + 1] * globalPixelScale;
                positionVboData[vertexCounter].Y = verts[i + 2] * globalPixelScale;
                vertexCounter += 1;
            }

            vertexCounter = 0;
            for (i = 0; i < verts.Length; i = i + 3)
            {
                normVboData[vertexCounter].X = norms[i];
                normVboData[vertexCounter].Y = norms[i + 1];
                normVboData[vertexCounter].Z = norms[i + 2];
                vertexCounter += 1;
            }
            vertexCounter = 0;
            int normIndexCounter = 0;
            for (i = 0; i < numVerts; ++i)
            {
                
                uvVboData[i].X = norms[normIndexCounter];
                normIndexCounter += 1;
                uvVboData[i].Y = norms[normIndexCounter];
                normIndexCounter += 1;
                
            }


            // pack the buffers:

            GL.GenBuffers(1, out positionVboHandle);
            GL.BindBuffer(BufferTarget.ArrayBuffer, positionVboHandle);
            GL.BufferData<OpenTK.Vector3>(BufferTarget.ArrayBuffer,
                new IntPtr(positionVboData.Length * OpenTK.Vector3.SizeInBytes),
                positionVboData, BufferUsageHint.StaticDraw);
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

            //GL.BufferData<uint>(BufferTarget.ElementArrayBuffer,
            //    new IntPtr(indices.Length * OpenTK.Vector3.SizeInBytes),
            //    indices, BufferUsageHint.StaticDraw);
            // clear for new buffer:
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);

            // now proceed with the VAO creation:
            //createVAO();


            // TODO: check for VAO success

            modelLoaded = true;
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
            // clear for new VAO:
            GL.BindVertexArray(0);
        }

        public bool draw(bool drawTwoSided)
        {
            bool drawWireframe = false;
            if (modelLoaded)
            {
                if (vaoID == 0)
                    return false;
                GL.BindVertexArray(vaoID);

                if (drawWireframe)
                {
                    GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
                }
                else
                {
                    if (drawTwoSided)
                    {
                        GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
                    }
                    else
                    {
                        GL.PolygonMode(MaterialFace.Back, PolygonMode.Line);
                        GL.PolygonMode(MaterialFace.Front, PolygonMode.Fill);
                    }
                }
                GL.DrawElements(BeginMode.Triangles, lengthOfIndexAray,
                    DrawElementsType.UnsignedInt, IntPtr.Zero);

                GL.BindVertexArray(0);

                //GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
                return true;
            }
            else
                return false;
        }

        public void exitApp()
        {
            GL.DeleteVertexArrays(1, ref vaoID);
        }
    }
}
