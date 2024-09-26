using System;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

using System.Drawing;
using AudioControlApp.Shaders;

using System.Threading;
using System.Runtime.InteropServices;

namespace AudioControlApp.Utils
{
    class DrawFPSToScreen
    {
        private bool readyToDraw = false;
        private bool flagForVertPositionUpdate = false;
        shaderFileLoader simpleTextureShaderSource;

        private int textureObject = -1; // pointer to texture memory

        int handleVAO, handleShader;

        // local shader variables:
        int shaderlocPosition, shaderlocTexture,
            shaderlocModelMatrix, shaderlocProjMatrix,
            shaderlocOffset;

        // buffer arrays:
        int positionVboHandle,
            normalVboHandle,
            uvVboHandle,
            indicesVboHandle;

        private bool firstPass = true;

        private uint vaoID;


        Matrix4 projectToScreenMatrix, cameraViewMatrix;


        //VertexPosTex[] vertices;


        // dynamic Position of quad:
        Vector3 screenOffsetPosition = new Vector3(10.0f, 20.0f, 10.0f);

        // ****************************************************
        // display text to screen vars:
        DirectTextView displayText;
        private int textFieldW = 600; // total size of the text field area
        private int textFieldH = 300;
        //private float textAreaW = 600; // used space within text field area
        //private float textAreaH = 300;
        private string prevTextValue = "";
        // ****************************************************

        // ****************************************************
        // set up camera and view matrix:

        private float cameraNearClip = -1.0f;
        private float cameraFarClip = 5000.0f;
        Vector3 cameraPosition = new Vector3(0.0f, 0.0f, 1000.0f);
        // used for both orthogrphic and perspective projection:
        Vector3 cameraLookAtPosition = new Vector3(0.0f, 0.0f, 0.0f);
        Vector3 cameraUpVector = new Vector3(0.0f, 1.0f, 0.0f);

        private float screenW = 1024f;
        private float screenH = 768f;

        public DrawFPSToScreen()
        {
            // always fixed:
            projectToScreenMatrix = Matrix4.CreateOrthographic(screenW, screenH, cameraNearClip, cameraFarClip);

            // this never changes:
            cameraViewMatrix = Matrix4.LookAt(cameraPosition, cameraLookAtPosition, cameraUpVector);

        }

        public void initOpenGL()
        {
            simpleTextureShaderSource = new shaderFileLoader();
            simpleTextureShaderSource.loadShaders("shaders\\simpleTextureWithPosnHUD.vp", "shaders\\simpleTexture.fp");

            // ****************************************************

            // 1. load texture to memory (texture object)
            setTextView(ref displayText, "Technic", 12, textFieldW, textFieldH, System.Windows.Forms.HorizontalAlignment.Center, Color4.LightGray); // must be white for proper color modulation
            update("test text");

            updateTextureFromTextField();
            updateVertPositions();

            // ****************************************************
            // create shader app:
            handleShader = ShaderLoader.CreateProgram(simpleTextureShaderSource.vertexShaderSource,
                                                        simpleTextureShaderSource.fragmentShaderSource);

            // ****************************************************
            GL.UseProgram(handleShader);

            // 5. retreive shader locations
            // uniforms:
            shaderlocPosition = GL.GetAttribLocation(handleShader, "vPosition");
            shaderlocTexture = GL.GetAttribLocation(handleShader, "vTexCoord");
            shaderlocOffset = GL.GetUniformLocation(handleShader, "vPositionOffset");

            //attributes
            shaderlocModelMatrix = GL.GetUniformLocation(handleShader, "mModelMatrix");
            shaderlocProjMatrix = GL.GetUniformLocation(handleShader, "mProjectionMatrix");

            // assign variables in shader:
            GL.UniformMatrix4(shaderlocModelMatrix, false, ref cameraViewMatrix);
            GL.UniformMatrix4(shaderlocProjMatrix, false, ref projectToScreenMatrix);

            // ****************************************************
            // 6. create VAO:
            createVAO(handleShader, "vPosition", "", "vTexCoord");

            GL.UseProgram(0);
            // ****************************************************

            readyToDraw = true;
        }


        private void setTextView(ref DirectTextView textView, string fontName, int fontSize, int width, int height, System.Windows.Forms.HorizontalAlignment alignment, Color4 color)
        {
            try
            {
                if (textView == null)
                {
                    textView = new DirectTextView(fontName, fontSize, width, height);
                    textView.Alignment = alignment;
                    textView.setColor(color.R, color.G, color.B, color.A);
                    textView.update();
                }
                else
                {
                    textView.FontName = fontName;
                    textView.FontSize = fontSize;
                    textView.Width = width;
                    textView.Height = height;
                }

                textView.Alignment = alignment;
                textView.setColor(color.R, color.G, color.B, color.A);
                textView.update();
            }
            catch (Exception txtExc)
            {
                System.Diagnostics.Debug.WriteLine("[FactoidFollower] error creating text (check that font is installed) exception: " + txtExc);
            }
        }

        public void update(string whichText)
        {
            //System.Diagnostics.Debug.WriteLine("[DrawFPS2Screen] update: [" + whichText + "]");
            if (whichText != prevTextValue)
            {
                if (!isLoadingBitmap)
                {
                    RectangleF textRectangle = new RectangleF(0f, 0f, (float)textFieldW, (float)textFieldH);
                    bool doCenterText = false;

                    prevTextValue = whichText;


                    displayText.Text = whichText;

                    displayText.update(textRectangle, doCenterText);

                    // upper left justified:
                    float screenOffsetX = 0 - screenW / 2.0f + quadW / 2.0f;
                    float screenOffsetY = screenH / 2.0f - quadH / 2.0f;


                    screenOffsetPosition.X = (float)Math.Round((double)screenOffsetX);
                    screenOffsetPosition.Y = (float)Math.Round((double)screenOffsetY);
                    screenOffsetPosition.Z = 0.0f;

                    updateTextureFromTextField();
                    //loadBitmapOnSeparateThread();
                    //flagForVertPositionUpdate = true;
                }
            }

            if (bitmapLoaded)
            {
                bitmapLoaded = false;
                
                placeNewBitmapInOpenGL();

                flagForVertPositionUpdate = true;
                isLoadingBitmap = false;
            }


            if (flagForVertPositionUpdate)
            {
                flagForVertPositionUpdate = false;
                updateVertPositions();
                projectToScreenMatrix = Matrix4.CreateOrthographic(screenW, screenH, cameraNearClip, cameraFarClip);

            }
        }


        public bool draw()
        {

            if (readyToDraw)
            {
                if (vaoID == 0)
                    return false;


                GL.Disable(EnableCap.DepthTest); // force to top

                //GL.Enable(EnableCap.Blend);
                //GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
                //GL.Enable(EnableCap.Blend);
                //GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);


                GL.UseProgram(handleShader);

                GL.UniformMatrix4(shaderlocModelMatrix, false, ref cameraViewMatrix);
                GL.UniformMatrix4(shaderlocProjMatrix, false, ref projectToScreenMatrix);

                // apply screen offset:
                GL.Uniform3(shaderlocOffset, screenOffsetPosition);

                GL.BindVertexArray(vaoID);

                // ********************************************************
                // bind the texture:           
                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, textureObject);
                // ********************************************************

                //redundant polygon call?
                //GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
                GL.DrawElements(BeginMode.Triangles, indices.Length, DrawElementsType.UnsignedInt, 0);

                // ********************************************************
                // unbind the texture:
                
                GL.BindTexture(TextureTarget.Texture2D, 0);
                // ********************************************************
                GL.BindVertexArray(0);

                GL.UseProgram(0);
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


            // clear for new VAO:
            GL.BindVertexArray(0);
        }


        private static int numberOfPointsInVBO = 4; // 1 quads, each with 4 points
        private OpenTK.Vector3[] positionVboData = new OpenTK.Vector3[numberOfPointsInVBO];
        private OpenTK.Vector2[] uvVboData = new OpenTK.Vector2[numberOfPointsInVBO];
        private OpenTK.Vector3[] normVboData = new OpenTK.Vector3[numberOfPointsInVBO];



        private void createAndPackBuffers()
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

        }

        private void updateAndRepackBuffers()
        {
            // repack the buffers:
            GL.BindBuffer(BufferTarget.ArrayBuffer, positionVboHandle);
            GL.BufferData<OpenTK.Vector3>(BufferTarget.ArrayBuffer,
                new IntPtr(positionVboData.Length * OpenTK.Vector3.SizeInBytes),
                positionVboData, BufferUsageHint.DynamicDraw);

            GL.BindBuffer(BufferTarget.ArrayBuffer, normalVboHandle);
            GL.BufferData<OpenTK.Vector3>(BufferTarget.ArrayBuffer,
                new IntPtr(normVboData.Length * OpenTK.Vector3.SizeInBytes),
                normVboData, BufferUsageHint.StaticDraw);

            GL.BindBuffer(BufferTarget.ArrayBuffer, uvVboHandle);
            GL.BufferData<OpenTK.Vector2>(BufferTarget.ArrayBuffer,
                new IntPtr(uvVboData.Length * OpenTK.Vector2.SizeInBytes),
                uvVboData, BufferUsageHint.StaticDraw);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, indicesVboHandle);
            GL.BufferData(BufferTarget.ElementArrayBuffer,
              new IntPtr(indices.Length * sizeof(uint)),
              indices, BufferUsageHint.StaticDraw);

            // clear for new buffer:
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
        }




        private int CreateStruct(VertexPosTex[] vertices)
        {
            int buffer = 0;

            GL.GenBuffers(1, out buffer);
            GL.BindBuffer(BufferTarget.ArrayBuffer, buffer);
            GL.BufferData(BufferTarget.ArrayBuffer,
                          new IntPtr(vertices.Length * VertexPosTex.Size),
                          vertices, BufferUsageHint.StaticDraw);
            DebugGL.CheckGL();

            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

            return buffer;
        }

        private int CreateIndex(uint[] indices)
        {
            int buffer = 0;

            GL.GenBuffers(1, out buffer);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, buffer);
            GL.BufferData(BufferTarget.ElementArrayBuffer,
                          new IntPtr(indices.Length * sizeof(uint)),
                          indices, BufferUsageHint.StaticDraw);

            DebugGL.CheckGL();

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);

            return buffer;
        }



        private void updateTextureFromTextField()
        {
            Bitmap bitmap = (Bitmap)displayText._textBitmap.Clone(); // copy locally
            
            System.Drawing.Imaging.BitmapData data = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            if (textureObject == -1) // only generate texture object once!
                textureObject = GL.GenTexture();

            GL.BindTexture(TextureTarget.Texture2D, textureObject);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
                          bitmap.Width, bitmap.Height, 0, OpenTK.Graphics.OpenGL.PixelFormat.Bgra,
                          PixelType.UnsignedByte, data.Scan0);

            GL.TexParameter(TextureTarget.Texture2D,
                            TextureParameterName.TextureMinFilter,
                            (int)TextureMinFilter.Linear);

            GL.TexParameter(TextureTarget.Texture2D,
                            TextureParameterName.TextureMagFilter,
                            (int)TextureMagFilter.Linear);

            GL.BindTexture(TextureTarget.Texture2D, 0);

            // update size of quad based upon bitmap:
            quadW = (float)bitmap.Width;
            quadH = (float)bitmap.Height;

            bitmap.UnlockBits(data);

            //Thread.Sleep(100); // can't do this unlocked (disposes before it is loaded)
            bitmap.Dispose();

        }

        private static int maxResolution = 1000;
        byte[] Pixels = new byte[maxResolution * maxResolution * 8];
        bool isLoadingBitmap = false;
        bool bitmapLoaded = false;

        private void placeNewBitmapInOpenGL() // this is done inside openGL loop (uses pixel array from separate thread)
        {
            if (textureObject == -1) // only create texture object once... 
                textureObject = GL.GenTexture();


            GL.BindTexture(TextureTarget.Texture2D, textureObject);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
                          quadWi, quadHi, 0, OpenTK.Graphics.OpenGL.PixelFormat.Bgra,
                          PixelType.UnsignedByte, Pixels);

            GL.TexParameter(TextureTarget.Texture2D,
                            TextureParameterName.TextureMinFilter,
                            (int)TextureMinFilter.Linear);

            GL.TexParameter(TextureTarget.Texture2D,
                            TextureParameterName.TextureMagFilter,
                            (int)TextureMagFilter.Linear);

            GL.BindTexture(TextureTarget.Texture2D, 0);

            Pixels.Initialize();
        }

        private void loadBitmapOnSeparateThread()
        {
            if (!isLoadingBitmap)
            {
                isLoadingBitmap = true;
                Thread localThread = new Thread(delegate()
                {

                    Bitmap newLocalBitmap = displayText._textBitmap; // copy locally

                    // **************************************************************************************

                    Thread.Sleep(100); // this wait time is arbitrary(?)
                    // bitmap load completed

                    quadWi = newLocalBitmap.Width;
                    quadHi = newLocalBitmap.Height;
                    // update size of quad based upon bitmap:
                    quadW = (float)quadWi;
                    quadH = (float)quadHi;

                    // Create rectangle to lock
                    Rectangle rect = new Rectangle(0, 0, quadWi, quadHi);


                    int Depth = System.Drawing.Bitmap.GetPixelFormatSize(newLocalBitmap.PixelFormat);
                    // create byte array to copy pixel values
                    //int step = Depth / 8;
                    int step = 3;
                    int PixelCount = quadWi * quadHi;
                    int currentPixelSize = PixelCount * step;
                    if (currentPixelSize > Pixels.Length) // avoid memory error
                        currentPixelSize = Pixels.Length;

                    //IntPtr Iptr = IntPtr.Zero;

                    System.Diagnostics.Debug.WriteLine("[TEXTUREDQUADwBlur] copying bitmap data: [" + quadWi + " x " + quadHi + " ]  depth: [ " + Depth + " ] ");// size: [ " + currentPixelSize + " ]");

                    //  load bitmap into local array:
                    // **************************************************************************************

                    System.Drawing.Imaging.BitmapData data = newLocalBitmap.LockBits(rect,
                       System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                    //System.Drawing.Imaging.BitmapData data = newLocalBitmap.LockBits(rect,
                    //   System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);


                    //Iptr = data.Scan0;


                    // copy files to fixed array:
                    //Marshal.Copy(Iptr, Pixels, 0, PixelCount * step);
                    Marshal.Copy(data.Scan0, Pixels, 0, currentPixelSize);

                    newLocalBitmap.UnlockBits(data);

                    // **************************************************************************************
                    // bitmap load into local array completed

                    Thread.Sleep(100); // this wait time is arbitrary(?)

                    // clean up:

                    //Iptr = IntPtr.Zero;
                    data = null;

                    newLocalBitmap.Dispose();

                    // indicate load complete:
                    //drawWaitCounter0 = 0; // this will load image into graphics memory
                    bitmapLoaded = true;

                });

                localThread.Start();
                localThread.CurrentCulture.ClearCachedData();
            }

            //GC.Collect();
            //GC.WaitForPendingFinalizers();
        }

        public void onClosing()
        {
            GL.DeleteProgram(handleShader);
            GL.DeleteVertexArrays(1, ref handleVAO);
        }

        private float quadW, quadH;
        private int quadWi, quadHi;

        private void updateVertPositions()
        {
            float zdepth = 100.0f;

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

            // upper left:
            screenOffsetPosition.X = 0 - screenW / 2.0f + quadW / 2.0f;
            screenOffsetPosition.Y = screenH / 2.0f - quadH / 2.0f;

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


            uvVboData[0].X = 0.0f;
            uvVboData[0].Y = 1.0f;
            uvVboData[1].X = 1.0f;
            uvVboData[1].Y = 1.0f;
            uvVboData[2].X = 1.0f;
            uvVboData[2].Y = 0.0f;
            uvVboData[3].X = 0.0f;
            uvVboData[3].Y = 0.0f;




            // norms always the same here:
            for (int i = 0; i < normVboData.Length; ++i)
            {
                normVboData[i].X = 0.0f;
                normVboData[i].Y = 0.0f;
                normVboData[i].Z = 1.0f;
            }

            if (firstPass)
            {
                firstPass = false;
                createAndPackBuffers();
            }
            else
            {
                updateAndRepackBuffers();
            }

        }

        private float[] Position = {
            // Front face
            -1000f, -1000f, -1000f,	
            1000f, -1000f, 1000f, 	
            1000f, 1000f, 1000f, 	
            -1000f, 1000f, 1000f
                                         };

        private uint[] indices = {
			// Font face
			0, 1, 2, 2, 3, 0
                                     };



        public void onWindowResize(float whichW, float whichH, float globalDisplayRatio, float globalScale, float twoDdrawingScale)
        {
            
            //System.Diagnostics.Debug.WriteLine("[DrawFPS2Screen] screenResize");
            screenW = whichW;
            screenH = whichH;

            flagForVertPositionUpdate = true;
            

        }
    }

}
