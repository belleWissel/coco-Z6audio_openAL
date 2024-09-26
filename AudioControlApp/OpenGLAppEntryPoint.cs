using System;
using MainApp;

// for config file
using System.Collections;
using System.Collections.Specialized;

//using AudioSystems;
//using OpenTK.Audio;
//using OpenTK.Audio.OpenAL;

namespace OpenGLAppEntryPoint
{
    class OpenGLAppEntryPoint
    {
        /// <summary>
        /// The main entry point for the application
        /// </summary>
        [STAThread]
        static void Main()
        {

            using (MainApp.MyWindow myWindow = new MainApp.MyWindow())
            {
                bool doFixFrameRate = true;

                //myWindow.KeyDown += MyWindow_KeyDown;
                //myWindow.MouseDown += MyWindow_MouseDown;
                if (doFixFrameRate)
                {
                    myWindow.VSync = OpenTK.VSyncMode.On;
                    myWindow.Run(60, 60); // locked update and render
                }
                else
                {
                    myWindow.VSync = OpenTK.VSyncMode.Off;
                    myWindow.Run(); // unlocked update and render
                }
            }


        }
        /*
        private static void MyWindow_MouseDown(object sender, OpenTK.Input.MouseButtonEventArgs e)
        {
            throw new NotImplementedException();
        }

        private static void MyWindow_KeyDown(object sender, OpenTK.Input.KeyboardKeyEventArgs e)
        {
            throw new NotImplementedException();
        }*/
    }
}
