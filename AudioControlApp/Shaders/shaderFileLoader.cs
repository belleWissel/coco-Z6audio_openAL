using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Windows.Forms; // error message

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;


namespace AudioControlApp.Shaders
{
    class shaderFileLoader
    {
        private string fragFilepath = "shaders\\fragShaderSample1.fp";
        private string vertFilepath = "shaders\\vertShaderSample1.vp";

        public string fragmentShaderSource = "";
        public string vertexShaderSource = "";


        public void loadShaders()
        {
            vertexShaderSource = readShaderFile(vertFilepath);
            fragmentShaderSource = readShaderFile(fragFilepath);

            //System.Diagnostics.Debug.WriteLine(fragmentShaderSource);
            //System.Diagnostics.Debug.WriteLine(vertexShaderSource);

        }

        public void loadShaders(string whichVertFilepath, string whichFragFilepath)
        {
            vertexShaderSource = readShaderFile(whichVertFilepath);
            fragmentShaderSource = readShaderFile(whichFragFilepath);

            //System.Diagnostics.Debug.WriteLine(fragmentShaderSource);
            //System.Diagnostics.Debug.WriteLine(vertexShaderSource);

        }

        private string readShaderFile(string whichFilePath)
        {
            string readProg = File.ReadAllText(whichFilePath);
            return readProg;
        }
        
    }


    public static class ShaderLoader
    {
        public static int CreateProgram(string vertexSource, string fragmentSource)
        {
            int vert = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vert, vertexSource);
            GL.CompileShader(vert);
            DebugGL.CheckGLSL(ref vert);

            int frag = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(frag, fragmentSource);
            GL.CompileShader(frag);
            DebugGL.CheckGLSL(ref frag);

            int program = GL.CreateProgram();
            GL.AttachShader(program, vert);
            GL.AttachShader(program, frag);
            GL.LinkProgram(program);
            DebugGL.CheckGL();

            return program;
        }
    }


    public static class DebugGL
    {
        public static void CheckGL()
        {
            ErrorCode code = GL.GetError();

            if (code != ErrorCode.NoError)
            {
                throw new ApplicationException(code.ToString());
            }
        }

        public static void CheckGLSL(ref int shader)
        {
            int result;
            GL.GetShader(shader, ShaderParameter.CompileStatus, out result);

            if (result == 0)
            {
                string info;
                GL.GetShaderInfoLog(shader, out info);
                throw new ApplicationException(info);
            }
        }
    }
}
