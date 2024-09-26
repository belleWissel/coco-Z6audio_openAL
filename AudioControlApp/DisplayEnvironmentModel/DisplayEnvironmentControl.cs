using System;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

using AudioControlApp.Shaders;
using AudioControlApp.OpenGLProgrammablePipeline;


namespace AudioControlApp.DisplayEnvironmentModel
{
    class DisplayEnvironmentControl
    {
        private bool doDrawTableForwards = false;

        // ********************************************************************************
        // shader vars

        private shaderFileLoader visualizationShaderSource;
        int shaderProgHandle;

        // local shader variables:
        // attributes:
        int shaderAttribPosition, shaderAttribTexture;

        // uniforms matrices:
        int shaderModelMatrix, shaderProjMatrix;

        // uniform vec3:
        int shaderPositionOffset, shaderScaleFactor, shaderlocDepthScale;

        // uniform vec 4:
        int shaderColorOffset;



        // uniform switch (int)
        int shaderTextureEnablei;
        //int shaderTextureEnableV, shaderTextureEnableF;

        // ********************************************************************************
        // model vars

        PPOpenReadDrawObjFileWithMetrics modelOfEnvironment;


        // ********************************************************************************
        // state vars

        // ********************************************************************************
        // 

        private float displayHeight = 1200f;
        private float displayDepth = 800f;

        private bool readyToDraw = false;

        string modelToLoad = "Models/cube2.obj";

        public DisplayEnvironmentControl(string whichModelFile)
        {
            modelToLoad = whichModelFile;
        }

        /*
        public void updateDisplayHeight(int whichHeight, int whichDepth)
        {
            // keep the items on the floor of the box on the floor of the virtual box

            displayHeight = (float)whichHeight;
            displayDepth = (float)whichDepth;

        }
        */

        #region initGraphics
        public void initOpenGL(float whichPixelScale)
        {
            modelOfEnvironment = new PPOpenReadDrawObjFileWithMetrics(whichPixelScale, modelToLoad);

            
            // ****************************************************************************
            // load shaders
            visualizationShaderSource = new shaderFileLoader();
            visualizationShaderSource.loadShaders("shaders\\simpleTextureWithPosnAndScale.vp", "shaders\\simpleTextureWithAlphaAndSwitch.fp");

            // ****************************************************************************
            // create shader app
            shaderProgHandle = ShaderLoader.CreateProgram(visualizationShaderSource.vertexShaderSource,
                                                        visualizationShaderSource.fragmentShaderSource);

            GL.UseProgram(shaderProgHandle);

            // ****************************************************************************
            // retreive shader locations

            // attributes
            shaderAttribPosition = GL.GetAttribLocation(shaderProgHandle, "vPosition");
            shaderAttribTexture = GL.GetAttribLocation(shaderProgHandle, "vTexCoord");

            // uniforms
            shaderScaleFactor = GL.GetUniformLocation(shaderProgHandle, "vScaleFactor"); // (animated) scale applied to vertex position
            shaderPositionOffset = GL.GetUniformLocation(shaderProgHandle, "vPositionOffset"); // (animated) position offset
            shaderColorOffset = GL.GetUniformLocation(shaderProgHandle, "vColorAdjust"); // vertex color (when not using texture)
            shaderTextureEnablei = GL.GetUniformLocation(shaderProgHandle, "iApplyTextureF"); // shader integer (in frag shader)
            shaderlocDepthScale = GL.GetUniformLocation(shaderProgHandle, "fDepthScale");

            // update per scene camera/projection
            shaderModelMatrix = GL.GetUniformLocation(shaderProgHandle, "mModelMatrix");
            shaderProjMatrix = GL.GetUniformLocation(shaderProgHandle, "mProjectionMatrix");

            modelOfEnvironment.initOpenGL();
            modelOfEnvironment.createVAO(shaderProgHandle, "vPosition", "", "vTexCoord");
            
            GL.UseProgram(0);
            readyToDraw = true;
        }



        #endregion initGraphics

        #region update and draw

        public void draw(Matrix4 whichviewMat, Matrix4 whichProjMat)
        {
            if (!readyToDraw)
                return;

            int i;
            Vector3 unityScale = new Vector3(1.0f, 1.0f, 1.0f);
            if (!doDrawTableForwards)
                unityScale.Z = -1f; // reverse the room
            Vector3 updatedScale = new Vector3(1.0f, 1.0f, 1.0f);
            Vector3 updatedPosition;
            Vector3 addOffset = new Vector3(0.0f, 0.0f, 0.0f);
            Vector3 addModelPositionOffset = new Vector3(0.0f, -500.0f, 0.0f);
            Vector4 updatedColor = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
            Vector4 unityColorBlack = new Vector4(0.0f, 0.0f, 0.0f, 1.0f);
            Vector4 unityColorWhite = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
            Vector4 darkColor = new Vector4(0.2f, 0.2f, 0.2f, 0.5f);
            Vector4 lightColor = new Vector4(0.8f, 0.8f, 0.8f, 0.5f);


            GL.UseProgram(shaderProgHandle);

            GL.Disable(EnableCap.DepthTest); // draw in order

            GL.Enable(EnableCap.Blend);
            //GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            //GL.Enable(EnableCap.DepthTest); 

            // udpate uniforms in shaders:
            GL.UniformMatrix4(shaderModelMatrix, false, ref whichviewMat);
            GL.UniformMatrix4(shaderProjMatrix, false, ref whichProjMat);
            GL.Uniform1(shaderlocDepthScale, 1.0f);

            // update uniforms:
            GL.Uniform3(shaderScaleFactor, unityScale);
            
            // draw cube model representation:
            GL.Uniform1(shaderTextureEnablei, 0); // disable texture mode in shader
            GL.Uniform4(shaderColorOffset, ref lightColor);
            GL.Uniform3(shaderPositionOffset, addModelPositionOffset);

            modelOfEnvironment.draw(true);
            modelOfEnvironment.draw(false);

            // restore polygon mode:
            //GL.PolygonMode(MaterialFace.Back, PolygonMode.Line);
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);

            GL.UseProgram(0);
        }

        #endregion update and draw

        public void exitApp()
        {
            modelOfEnvironment.exitApp();
        }
    }
}
