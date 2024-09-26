using System;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

using AudioControlApp.Shaders;
using AudioControlApp.OpenGLProgrammablePipeline;

namespace AudioControlApp.Utils
{
    class DrawGridOnScreen
    {
        private bool readyToDraw = false;

        shaderFileLoader simpleTextureShaderSource;
        PPDrawOriginAndGrid drawGridVar;
        double localGridSenseVolNear, localGridSenseVolFar, localGridSenseVolWidth, localGridSenseVolStart, localGridSenseVolEnd;


        private float gridW = 1000f;
        private float gridH = 1000f;
        private int gridLineCount = 10;

        // ****************************************************************************
        // shader variables
        int handleShader;

        private int shaderlocPosition,
            shaderlocColor, shaderlocOffset,
            shaderlocModelMatrix, shaderlocProjMatrix;

        Vector4 darkGreenColor, brightYellowColor, brightBlueColor;
        Vector3 noOffset = new Vector3(0.0f, 0.0f, 0.0f);
        // ****************************************************************************

        public DrawGridOnScreen(float whichGridW, float whichGridH, int whichNumberOfGridLines)
        {
            gridW = whichGridW;
            gridH = whichGridH;
            gridLineCount = whichNumberOfGridLines;

            // define colors used:
            darkGreenColor = new Vector4(0.1f, 0.6f, 0.1f, 1.0f);
            brightYellowColor = new Vector4(1f, 1f, 0.1f, 1.0f);
            brightBlueColor = new Vector4(0.2f, 0.2f, 0.9f, 1.0f);

        }

        public void initOpenGL()
        {
            simpleTextureShaderSource = new shaderFileLoader();
            simpleTextureShaderSource.loadShaders("shaders\\FlatShader2.vp", "shaders\\FlatShader2.fp");

            // ****************************************************

            drawGridVar = new PPDrawOriginAndGrid(gridW, gridH, gridLineCount);
            drawGridVar.setNearAndFar(localGridSenseVolNear, localGridSenseVolFar);
            drawGridVar.setExtents(localGridSenseVolWidth, localGridSenseVolStart, localGridSenseVolEnd);
            drawGridVar.forceUpdateOfModelBuffer();

            // ****************************************************
            // create shader app:
            handleShader = ShaderLoader.CreateProgram(simpleTextureShaderSource.vertexShaderSource,
                                            simpleTextureShaderSource.fragmentShaderSource);

            // ****************************************************
            GL.UseProgram(handleShader);

            
            // uniforms:
            shaderlocPosition = GL.GetAttribLocation(handleShader, "vPosition");


            //attributes
            shaderlocModelMatrix = GL.GetUniformLocation(handleShader, "mModelMatrix");
            shaderlocProjMatrix = GL.GetUniformLocation(handleShader, "mProjectionMatrix");
            shaderlocColor = GL.GetUniformLocation(handleShader, "vColorValue");
            shaderlocOffset = GL.GetUniformLocation(handleShader, "vPositionOffset");

            // ****************************************************
            // 6. create VAO:
            drawGridVar.createVAO(handleShader, "vPosition");

            GL.UseProgram(0);
            // ****************************************************

            readyToDraw = true;
        }


        public void draw(Matrix4 whichviewMat, Matrix4 whichProjMat)
        {
            if (!readyToDraw)
                return;

            //GL.Enable(EnableCap.DepthTest);
            //GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
            //GL.LineWidth(1.0f);

            GL.UseProgram(handleShader);

            // udpate uniforms in shaders:
            GL.UniformMatrix4(shaderlocModelMatrix, false, ref whichviewMat);
            GL.UniformMatrix4(shaderlocProjMatrix, false, ref whichProjMat);

            GL.Uniform3(shaderlocOffset, noOffset);

            GL.LineWidth(0.5f);
            GL.Uniform4(shaderlocColor, darkGreenColor);
            drawGridVar.drawGrid();
            GL.LineWidth(2.0f);
            GL.Uniform4(shaderlocColor, brightYellowColor);
            drawGridVar.drawAxis();
            GL.Uniform4(shaderlocColor, brightBlueColor);
            drawGridVar.drawSensingVolume();
            GL.LineWidth(1.0f);

            GL.UseProgram(0);

        }


        public void setXmitRanges(double whichNear, double whichFar)
        {
            localGridSenseVolNear = whichNear;
            localGridSenseVolFar = whichFar;
            // grid doesn't exist yet:
            //drawGridVar.setNearAndFar(whichNear, whichFar);
        }

        public void setUserGridMeasurement(double whichWidth, double whichBottom, double whichTop)
        {
            localGridSenseVolWidth = whichWidth;
            localGridSenseVolStart = whichBottom;
            localGridSenseVolEnd = whichTop;

            // grid doesn't exist yet:
            //drawGridVar.setExtents(whichWidth, whichBottom, whichTop);
        }
        /*
        public void updateGridGeometry()
        {
            drawGridVar.forceUpdateOfModelBuffer();
        }
        */
        public void onClosing()
        {
            GL.DeleteProgram(handleShader);

            drawGridVar.exitApp();
        }
    }
}
