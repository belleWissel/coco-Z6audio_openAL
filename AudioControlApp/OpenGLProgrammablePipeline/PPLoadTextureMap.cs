using System;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
// texture image:
using System.Drawing;
using System.Drawing.Imaging;
// check file system:
using System.IO;

using AudioControlApp.Utils;


namespace AudioControlApp.OpenGLProgrammablePipeline
{
    class PPLoadTextureMap
    {
        public bool readyToDraw = false;
        public float width, height;
        public string pathToTexture;

        public int textureObject;
        private float quadLeft, quadRight, quadTop, quadBottom;

        public PPLoadTextureMap()
        {

            width = 200.0f;
            height = 200.0f;
            updateQuadSize();

            updateImageTo("Data\\TextureMaps\\factoid_type0\\OpenGL-Logo.png");
           
        }

        public void initOpenGL()
        {
            updateImageTo("TextureMaps\\OpenGL-Logo.png"); // load dummy image to initiate!?
        }


        public void updateImageTo(string whichTexture)
        {
            System.Diagnostics.Debug.WriteLine("[LOAD TEXTURE] updateImageTo " + whichTexture);

            readyToDraw = false; // disable drawing... 

            pathToTexture = whichTexture;
            if (confirmImageFilesExist(pathToTexture))
            {
                Bitmap newBitmap = new Bitmap(pathToTexture);
                loadBitmapIntoTextureMemory(newBitmap);
                
                //readyToDraw = true; // enable drawing... 
                newBitmap.Dispose();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[LOAD TEXTURE] fialed to locate texture file " + pathToTexture);
                readyToDraw = false;
            }
        }
 

        private bool confirmImageFilesExist(string whichFile)
        {
            return File.Exists(AudioControlApp.FileUtils.MakeAbsolutePath(whichFile));
        }


        public void setDrawingSize(float whichW, float whichH)
        {
            width = whichW;
            height = whichH;
            updateQuadSize();
        }

        private void updateQuadSize()
        {
            quadLeft = 0.0f - width / 2.0f;
            quadRight = width / 2.0f;

            quadBottom = 0.0f - height / 2.0f;
            quadTop = height / 2.0f;
        }



        public void bindTexture()
        {
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, textureObject);
        }

        public void unbindTexture()
        {
            GL.BindTexture(TextureTarget.Texture2D, 0);
        }


        #region load textures

        private void loadBitmapIntoTextureMemory(Bitmap whichNewBitmap)
        {
            if (whichNewBitmap != null)
            {
                System.Drawing.Imaging.BitmapData data = whichNewBitmap.LockBits(
                    new Rectangle(0, 0, whichNewBitmap.Width, whichNewBitmap.Height),
                    System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                if (textureObject == -1) // only create texture object once... 
                    textureObject = GL.GenTexture();


                GL.BindTexture(TextureTarget.Texture2D, textureObject);
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
                              whichNewBitmap.Width, whichNewBitmap.Height, 0, OpenTK.Graphics.OpenGL.PixelFormat.Bgra,
                              PixelType.UnsignedByte, data.Scan0);

                whichNewBitmap.UnlockBits(data);

                GL.TexParameter(TextureTarget.Texture2D,
                                TextureParameterName.TextureMinFilter,
                                (int)TextureMinFilter.Linear);

                GL.TexParameter(TextureTarget.Texture2D,
                                TextureParameterName.TextureMagFilter,
                                (int)TextureMagFilter.Linear);

                // update size of quad based upon bitmap:
                width = (float)whichNewBitmap.Width;
                height = (float)whichNewBitmap.Height;

                GL.BindTexture(TextureTarget.Texture2D, 0);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[TEXTUREDQUAD] error loading bitmap " + pathToTexture);
                readyToDraw = false;
            }
        }


        #endregion load textures
 
    }
}
