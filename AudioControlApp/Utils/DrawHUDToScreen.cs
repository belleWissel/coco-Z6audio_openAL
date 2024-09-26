using System;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

using System.Drawing;

using AudioControlApp.Shaders;
//using SensorControlApp.DisplayText;

namespace AudioControlApp.Utils
{
    class DrawHUDToScreen
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


        // dynamic Position of quad:
        Vector3 screenOffsetPosition = new Vector3(10.0f, 20.0f, 10.0f);

        // ****************************************************
        // display text to screen vars:
        DirectTextView displayText;
        private int textFieldW = 1500; // total size of the text field area
        private int textFieldH = 700;
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

        private bool waitForDraw = false;
        private int drawWaitCounter = 0;
        private int drawWaitCounterLimit = 5;


        private static int numberOfPointsInVBO = 4; // 1 quads, each with 4 points
        private OpenTK.Vector3[] positionVboData = new OpenTK.Vector3[numberOfPointsInVBO];
        private OpenTK.Vector2[] uvVboData = new OpenTK.Vector2[numberOfPointsInVBO];
        private OpenTK.Vector3[] normVboData = new OpenTK.Vector3[numberOfPointsInVBO];

        private float quadW, quadH;

        
        // ********************************************
        // code from unity for scrolling text window
        private static int maxNumberOfLinesOfText = 60;
        private string[] lineOfContent = new string[maxNumberOfLinesOfText];
        private int currentLineOfText = 0;

        private float currentScreenW;
        private float currentScreenH;
        
        private RecordHUDLogToFile recordToLocalFile;
        private bool isRecordingToFile = false;
        // *********************************************

        public DrawHUDToScreen()
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
            setTextView(ref displayText, "Arial", 10, textFieldW, textFieldH, System.Windows.Forms.HorizontalAlignment.Center, Color4.White); // must be white for proper color modulation
            updateWithText("test text");

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
            
            /* code from unity for scrolling text window
            setElementSizes();
            
            currentLineOfText = -1;
            logText("> LogText Initialized");
            logText("> press 't' to toggle this window");
            logText("> ");
            */
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
                System.Diagnostics.Debug.WriteLine("[DRAWHUD] error creating text (check that font is installed) exception: " + txtExc);
            }
        }

        private void update()
        {
            /*
            if (waitForDraw)
            {
                drawWaitCounter += 1;
                if (drawWaitCounter > drawWaitCounterLimit)
                {
                    //System.Diagnostics.Debug.WriteLine("[DRAWHUD] draw wait complete "); // THIS HAPPENS A LOT 
                    waitForDraw = false;
                    readyToDraw = true;
                }
            }*/
        }

        public void updateWithText(string whichText)
        {
            //System.Diagnostics.Debug.WriteLine("[DrawFPS2Screen] update: [" + whichText + "]");
            if (whichText != prevTextValue)
            {
                //readyToDraw = false; // disable drawing

                RectangleF textRectangle = new RectangleF(0f, 0f, (float)textFieldW, (float)textFieldH);
                bool doCenterText = false;

                prevTextValue = whichText;


                displayText.Text = whichText;

                displayText.update(textRectangle, doCenterText);

                // upper left justified:
                float screenOffsetX = 0 - screenW / 2.0f + quadW / 2.0f;
                float screenOffsetY = 0 - screenH / 2.0f + quadH / 2.0f;


                screenOffsetPosition.X = (float)Math.Round((double)screenOffsetX);
                screenOffsetPosition.Y = (float)Math.Round((double)screenOffsetY);
                screenOffsetPosition.Z = 0.0f;

                updateTextureFromTextField();

                flagForVertPositionUpdate = true;

                drawWaitCounter = 0;
                waitForDraw = true;

            }

            if (flagForVertPositionUpdate)
            {
                flagForVertPositionUpdate = false;
                updateVertPositions();
                projectToScreenMatrix = Matrix4.CreateOrthographic(screenW, screenH, cameraNearClip, cameraFarClip);

            }

            update();
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
                // redundant poly mode call:
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
            //Bitmap bitmap = displayText._textBitmap; // copy locally
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
            bitmap.Dispose();
        }

        public void onClosing()
        {
            GL.DeleteProgram(handleShader);
            GL.DeleteVertexArrays(1, ref handleVAO);
        }


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
            screenOffsetPosition.X = 0 - screenW / 2.0f + quadW / 2.0f; // left justify
            //screenOffsetPosition.Y = 0 - screenH / 2.0f + quadH / 2.0f + 200f; // above mid screen
            screenOffsetPosition.Y = 0 - screenH / 2.0f + quadH; // bot justified
            //screenOffsetPosition.Y = 0 - screenH / 2.0f + quadH / 2.0f - 100f; // bottom justify

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
        
        /* this is code from Unity for scrolling text window 
        public void prepForRecordToFile(string whichFilePath, bool doActivateRecordToFile)
        {
            isRecordingToFile = doActivateRecordToFile;
            
            recordToLocalFile = new RecordHUDLogToFile(doActivateRecordToFile);
            
            recordToLocalFile.setModeAndVariables(doActivateRecordToFile, whichFilePath);
        }
        
        public void logText(string whichText)
        {
            currentLineOfText += 1;
            if (currentLineOfText >= maxNumberOfLinesOfText)
            {
                shiftTextUp();
                currentLineOfText = maxNumberOfLinesOfText - 1;
            }
            lineOfContent[currentLineOfText] = "> " + whichText;

            if (isRecordingToFile)
                recordToLocalFile.recordGenericEvent(whichText);
            
            updateTextField();
        }

        public void toggleLogVisibility()
        {
            
        }

        public void setWindowSize(int whichW, int whichH)
        {
            currentScreenW = whichW;
            currentScreenH = whichH;
            
            Vector3 upperRight = new Vector3(5000f+whichW/2f - targetWidth/2f - 5f, whichH/2f- targetHeight/2f - 5f, 500f);
            this.transform.position = upperRight;
            
        }

        private void setElementSizes()
        {
            // auto size the text field
            
            //Vector3 whichScale = new Vector3(350, 1000, 10);
            Vector3 whichScale = new Vector3(350, targetHeight, 10);
            Vector3 whichBgScale = new Vector3(30, 1, 100);
            
            if (targetHeight != -1 && targetWidth != -1)
            {
                whichScale.x = targetWidth;
                whichScale.y = targetHeight;

                whichBgScale.x = targetWidth / 10f;
                whichBgScale.y = 1;
                whichBgScale.z = targetHeight / 10f;
            }
            
            textField.GetComponent<RectTransform>().sizeDelta = new Vector2(whichScale.x-5f, whichScale.y-5f);
            textFieldBackground.transform.localScale = whichBgScale;
        }

        private void setElementPositions()
        {
            textField.ForceMeshUpdate(); // forces to most current size of field
            
            Vector3 positionOfText = new Vector3();
            Vector3 currentSizeOfText = textField.textBounds.size;
            
            // this assumes it is hugging right side of screen
            //positionOfText.x = 5000f + (currentScreenW / 2f) - (targetWidth / 2f) - 5f;
            positionOfText.x = this.transform.position.x;
            
            // move so it hugs bottom of target area:
            //positionOfText.y = (currentScreenH / 2f) - targetHeight + (currentSizeOfText.y/2f) +  5f ;
            positionOfText.y = this.transform.position.y - targetHeight + currentSizeOfText.y + 30f;
            positionOfText.z = 500f;

            textFieldContainer.transform.position = positionOfText;
        }
        
        private void updateTextField()
        {
            textField.text = "";
            
            for (int i = 0; i <= currentLineOfText; ++i)
            {
                textField.text += lineOfContent[i] + "\n";
            }

            setElementPositions();
        }

        private void shiftTextUp()
        {
            string[] oldArray = new string[maxNumberOfLinesOfText];
            System.Array.Copy(lineOfContent, oldArray, maxNumberOfLinesOfText);
            
            //var newArray = new int?[oldArray.Length];
            System.Array.Copy(oldArray, 1, lineOfContent, 0, oldArray.Length - 1);
            //Array.Copy(oldArray, 1, lineOfContent, 0, oldArray.Length - 1);
        }

        public void onProgramExit()
        {
            if (isRecordingToFile)
                recordToLocalFile.onProgramExit();
        }*/
    }

}
