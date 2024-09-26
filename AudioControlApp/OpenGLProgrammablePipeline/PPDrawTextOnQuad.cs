using System;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics;
using System.IO;

using AudioControlApp.Shaders;
using AudioControlApp.Utils;

namespace AudioControlApp.OpenGLProgrammablePipeline
{
    class PPDrawTextOnQuad
    {
        private int textureObject0 = -1; // pointer to texture memory
        private int textureObject1 = -1; // pointer to texture memory
        private int currentlyUsingTexture = 0;
        private bool doSwapTexturesOnNextUpdate = false;

        private float quadW, quadH;

        // buffer arrays:
        int positionVboHandle,
            normalVboHandle,
            uvVboHandle,
            indicesVboHandle;

        private uint vaoID;

        //public string pathToTexture;

        //private Bitmap textureBitmap;

        private DirectTextView formattedTextField;

        private bool readyToDraw = false;

        private bool isFirstPass = true;


        // ****************************************************
        // display text to screen vars:
        //DirectTextView displayText;
        private int textFieldW = 600; // total size of the text field area
        private int textFieldH = 300;
        public float textAreaW = 600; // used space within text field area
        public float textAreaH = 300;
        private float textAreaPosX = 600; // distance between edge of text field and text inside
        private float textAreaPosY = 300;

        private Vector3 addTextFieldOffset = new Vector3(0.0f, 0.0f, 0.0f); // position offset needed to center or otherwise justify text
        private Vector3 textFieldPosition = new Vector3(0.0f, 0.0f, 0.0f); // desired location of text

        private string prevTextValue = "";
        public string currentTextValue = "";
        // ****************************************************

        private bool doCenterText = false;
        private bool doJustifyLeft = false;
        private bool doJustifyRight = false;

        // ****************************************************
        private bool waitForDraw = false;
        private int drawWaitCounter = 0;
        private int drawWaitCounterLimit = 5;

        // create texture first
        // init openGL second
        // generate VAO third (last)

        public PPDrawTextOnQuad(string whichFontFamily, int whichPointSize, System.Windows.Forms.HorizontalAlignment whichAlignment, Color4 whichColor, string whichFormat)
        {
            setTextView(ref formattedTextField, whichFontFamily, whichPointSize, textFieldW, textFieldH, whichAlignment, whichColor); // must be white for proper color modulation
            updateText("test text");

            if (whichAlignment == System.Windows.Forms.HorizontalAlignment.Center)
            {

                doCenterText = true;
                //System.Diagnostics.Debug.WriteLine("[DRAWTEXTQUAD] adjusting field to be centered");

            }
            else if (whichAlignment == System.Windows.Forms.HorizontalAlignment.Right)
            {
                doJustifyRight = true;
            }
            else
            {
                doJustifyLeft = true; // default is justified left
            }

            switch (whichFormat)
            {
                case "italic":
                case "Italic":
                case "ITALIC":
                    formattedTextField._fontStyle = FontStyle.Italic;
                    break;
                case "bold":
                case "Bold":
                case "BOLD":
                    formattedTextField._fontStyle = FontStyle.Bold;
                    break;
                default:
                    formattedTextField._fontStyle = FontStyle.Regular;
                    break;
            }
        }


        public PPDrawTextOnQuad(string whichFontFamily, int whichPointSize, System.Windows.Forms.HorizontalAlignment whichAlignment, Color4 whichColor, string whichFormat, int whichW, int whichH)
        {
            textFieldW = whichW;
            textFieldH = whichH;

            setTextView(ref formattedTextField, whichFontFamily, whichPointSize, textFieldW, textFieldH, whichAlignment, whichColor); // must be white for proper color modulation
            updateText("test text2");
            if (whichAlignment == System.Windows.Forms.HorizontalAlignment.Center)
            {

                doCenterText = true;
                //System.Diagnostics.Debug.WriteLine("[DRAWTEXTQUAD] adjusting field to be centered");

            }
            else if (whichAlignment == System.Windows.Forms.HorizontalAlignment.Right)
            {
                doJustifyRight = true;
            }
            else
            {
                doJustifyLeft = true; // default is justified left
            }

            switch (whichFormat)
            {
                case "italic":
                case "Italic":
                case "ITALIC":
                    formattedTextField._fontStyle = FontStyle.Italic;
                    break;
                case "bold":
                case "Bold":
                case "BOLD":
                    formattedTextField._fontStyle = FontStyle.Bold;
                    break;
                default:
                    formattedTextField._fontStyle = FontStyle.Regular;
                    break;
            }
        }

        public void initOpenGL(Vector3 whichPosition)
        {
            textFieldPosition = whichPosition;

            updateTextureFromTextField();

            updateVertPositions(); // these vertex positions change depending on the size of the (text)texture bitmap
            packBuffers(); // repack buffers
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
                System.Diagnostics.Debug.WriteLine("[DRAWTEXTQUAD] error creating text (check that font is installed) exception: " + txtExc);
            }
        }

        public void updateText(string whichText)
        {
            
            if (whichText != prevTextValue)
            {
                //System.Diagnostics.Debug.WriteLine("[DRAWTEXTQUAD] updating text to: " + whichText);

                readyToDraw = false; // disable drawing while things update
                //drawWaitCounter = 0;
                //waitForDraw = true;

                RectangleF textRectangle = new RectangleF(0f, 0f, (float)textFieldW, (float)textFieldH);
                //bool doCenterText = false;
                int textW, textH;
                int textX, textY;

                prevTextValue = whichText;


                formattedTextField.Text = whichText;

                formattedTextField.update(textRectangle, doCenterText);

                formattedTextField.getDim(out textW, out textH);
                formattedTextField.getPos(out textX, out textY);


                textAreaW = (float)textW;
                textAreaH = (float)textH;
                textAreaPosX = (float)textX;
                textAreaPosY = (float)textY;

                //System.Diagnostics.Debug.WriteLine("[DRAWTEXTQUAD] update: [" + whichText + "]");
                /*
                float textOffsetH = (float)textFieldW / 2.0f - (float)textW / 2.0f;
                if (doCenterText)
                    textOffsetH -= (textRectangle.Width - (float)textW) / 2.0f; // if centering text, it centers within the rectangle
                float textOffsetV = (float)textFieldH / 2.0f - (float)textH / 2.0f;
                if (doCenterText)
                    textOffsetV -= (textRectangle.Height - (float)textH) / 2.0f;
                */

                addTextFieldOffset.Z = 0.0f; // scoot it in front

                if (doCenterText)
                {
                    addTextFieldOffset.X = 0.0f - (((float)textFieldW / 2.0f) - textAreaPosX - (textAreaW / 2.0f));
                    addTextFieldOffset.Y = 0.0f - (((float)textFieldH / 2.0f) - textAreaPosY - (textAreaH / 2.0f));
                }
                else if (doJustifyLeft)
                {
                    addTextFieldOffset.X = ((float)textFieldW / 2.0f) - textAreaPosX;
                    addTextFieldOffset.Y = 0.0f - ((float)textFieldH / 2.0f);
                }


                // upper right justified:

                /*
                float screenOffsetX = screenW / 2.0f - quadW / 2.0f;
                float screenOffsetY = screenH / 2.0f - quadH / 2.0f;

                screenOffsetX -= textAreaW;
                screenOffsetX += quadW;
                */

                // upper left justified:
                /*
                float screenOffsetX = 0 - screenW / 2.0f + quadW / 2.0f;
                float screenOffsetY = screenH / 2.0f - quadH / 2.0f;


                screenOffsetPosition.X = (float)Math.Round((double)screenOffsetX);
                screenOffsetPosition.Y = (float)Math.Round((double)screenOffsetY);
                */

                updateTextureFromTextField();
                updateVertPositions(); // these vertex positions change depending on the size of the (text)texture bitmap

                //System.Diagnostics.Debug.WriteLine("[DRAWTEXTQUAD] current txr: "+currentlyUsingTexture+" updating text on quad to [" + whichText +"]");
                

                if (isFirstPass)
                {
                    packBuffers(); // pack all buffers
                    isFirstPass = false;
                    drawWaitCounter = 0;
                    waitForDraw = true;

                }
                else
                {
                    packPositionBuffer(); // only the position buffer needs to be updated
                    readyToDraw = true;
                }

                // wait for changes to take effect:
                //drawWaitCounter = 0;
                //waitForDraw = true;
                
            }
        }

        public string getCurrentText()
        {
            if (formattedTextField != null)
                return formattedTextField.Text;
            else
                return "";
        }

        public Vector3 getTextFieldOffset()
        {
            //System.Diagnostics.Debug.WriteLine("[DRAWTEXTQUAD] text field offset requested, sending [" + addTextFieldOffset.X + ", " + addTextFieldOffset.Y + ", " + addTextFieldOffset.Z +" ]");

            return addTextFieldOffset;               
        }


        private void updateTextureFromTextField()
        {
            if (textureObject0 == -1) // only generate texture object once!
            {
                textureObject0 = GL.GenTexture();
                textureObject1 = GL.GenTexture();
            }

            Bitmap bitmap = formattedTextField._textBitmap; // copy locally

            System.Drawing.Imaging.BitmapData data = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);



            if (currentlyUsingTexture == 1)
            {
                GL.BindTexture(TextureTarget.Texture2D, textureObject0); // update texture 0 since it is not being used
                //doSwapTexturesOnNextUpdate = true;
            }
            else
            {
                GL.BindTexture(TextureTarget.Texture2D, textureObject1);
                //doSwapTexturesOnNextUpdate = true;
            }
            
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
                          bitmap.Width, bitmap.Height, 0, OpenTK.Graphics.OpenGL.PixelFormat.Bgra,
                          PixelType.UnsignedByte, data.Scan0);
            bitmap.UnlockBits(data);

            GL.TexParameter(TextureTarget.Texture2D,
                            TextureParameterName.TextureMinFilter,
                            (int)TextureMinFilter.Linear);

            GL.TexParameter(TextureTarget.Texture2D,
                            TextureParameterName.TextureMagFilter,
                            (int)TextureMagFilter.Linear);

            // update size of quad based upon bitmap:
            quadW = (float)bitmap.Width;
            quadH = (float)bitmap.Height;
            GL.BindTexture(TextureTarget.Texture2D, 0);
            doSwapTexturesOnNextUpdate = true;

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



        public void update()
        {
            if (waitForDraw)
            {
                drawWaitCounter += 1;
                if (drawWaitCounter > drawWaitCounterLimit)
                {
                    waitForDraw = false;
                    readyToDraw = true;
                }
            }

            if (doSwapTexturesOnNextUpdate)
            {
                doSwapTexturesOnNextUpdate = false;

                if (currentlyUsingTexture == 1)
                {
                    currentlyUsingTexture = 0;
                }
                else
                {
                    currentlyUsingTexture = 1;
                }
                //System.Diagnostics.Debug.WriteLine("[DRAWTEXTQUAD] update: swapping textures to "+currentlyUsingTexture+" for text [" + prevTextValue + "]");
            }
        }

        public bool draw()
        {
            if (readyToDraw)
            {
                if (vaoID == 0) // we haven't generated VAO yet
                    return false;

                GL.BindVertexArray(vaoID);

                // ********************************************************
                // bind the texture:           
                GL.ActiveTexture(TextureUnit.Texture0);

                if (currentlyUsingTexture == 0)
                {
                    GL.BindTexture(TextureTarget.Texture2D, textureObject0);
                }
                else
                {
                    GL.BindTexture(TextureTarget.Texture2D, textureObject1);
                }

                //GL.BindTexture(TextureTarget.Texture2D, textureObject);
                // ********************************************************

                // redundant polygon call?
                //GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
                GL.DrawElements(BeginMode.Triangles, indices.Length, DrawElementsType.UnsignedInt, 0);

                // ********************************************************
                // unbind the texture:
                GL.BindTexture(TextureTarget.Texture2D, 0);
                // ********************************************************

                return true;
            }
            else
                return false;
        }

        public void exitApp() // clean up:
        {
            GL.DeleteVertexArrays(1, ref vaoID);
        }

        private void updateVertPositions()
        {
            float zdepth = 0.0f;
            int i;

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

            for (i = 0; i < positionVboData.Length; ++i)
            {
                positionVboData[i] += textFieldPosition; // move entire quad area to desired text position
            }

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
            for (i = 0; i < normVboData.Length; ++i)
            {
                normVboData[i].X = 0.0f;
                normVboData[i].Y = 0.0f;
                normVboData[i].Z = 1.0f;
            }

        }

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

        private void packPositionBuffer()
        {
            GL.GenBuffers(1, out positionVboHandle);
            GL.BindBuffer(BufferTarget.ArrayBuffer, positionVboHandle);
            GL.BufferData<OpenTK.Vector3>(BufferTarget.ArrayBuffer,
                new IntPtr(positionVboData.Length * OpenTK.Vector3.SizeInBytes),
                positionVboData, BufferUsageHint.DynamicDraw);
            // clear for new buffer:
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

        }

    }
}
