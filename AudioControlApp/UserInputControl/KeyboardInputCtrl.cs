using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK.Input;

namespace AudioControlApp.UserInputControl
{
    class KeyboardInputCtrl
    {

        KeyboardState prevKeyboardState;
        private static int maxNumberOfKeysToTrack = 30;
        private int actualNumberOfKeysToTrack = 0;
        KeyClass[] classyKeys = new KeyClass[maxNumberOfKeysToTrack];
        private bool inputReceivedFromOutsideSource = false;

        private bool doUseOutsideSource = false;

        public KeyboardInputCtrl(bool whichVNCCompatibility)
        {
            doUseOutsideSource = whichVNCCompatibility;
            int i;
            prevKeyboardState = Keyboard.GetState();
            for (i = 0; i < maxNumberOfKeysToTrack; ++i)
            {
                classyKeys[i] = new KeyClass();
            }

            i = 0;
            classyKeys[i].whichKey = Key.Escape;
            ++i;
            /*
            classyKeys[i].whichKey = Key.Z;
            ++i;
            classyKeys[i].whichKey = Key.X;
            ++i;
            classyKeys[i].whichKey = Key.F;
            ++i;
            classyKeys[i].whichKey = Key.G;
            ++i;
            classyKeys[i].whichKey = Key.H;
            ++i;
            classyKeys[i].whichKey = Key.M;
            ++i;
            classyKeys[i].whichKey = Key.P;
            ++i;
            classyKeys[i].whichKey = Key.Q;
            ++i;
            // i = 9
            
            classyKeys[i].whichKey = Key.Number1;
            ++i;
            classyKeys[i].whichKey = Key.Number2;
            ++i;
            classyKeys[i].whichKey = Key.Number3;
            ++i;
            classyKeys[i].whichKey = Key.Number4;
            ++i;
            classyKeys[i].whichKey = Key.Left;
            ++i;
            classyKeys[i].whichKey = Key.Right;
            ++i;
            classyKeys[i].whichKey = Key.Up;
            ++i;
            classyKeys[i].whichKey = Key.Down;
            ++i;
            
            // i = 17

            classyKeys[i].whichKey = Key.Keypad8;
            ++i;
            classyKeys[i].whichKey = Key.Keypad2;
            ++i;
            classyKeys[i].whichKey = Key.Keypad4;
            ++i;
            classyKeys[i].whichKey = Key.Keypad6;
            ++i;
            classyKeys[i].whichKey = Key.Keypad9;
            ++i;
            classyKeys[i].whichKey = Key.Keypad3;
            ++i;
            classyKeys[i].whichKey = Key.Keypad7;
            ++i;
            classyKeys[i].whichKey = Key.Keypad1;
            ++i;
            */
            classyKeys[i].whichKey = Key.Space;
            ++i;

            classyKeys[i].whichKey = Key.V;
            ++i;
            classyKeys[i].whichKey = Key.B;
            ++i;
            classyKeys[i].whichKey = Key.N;
            ++i;
            classyKeys[i].whichKey = Key.R;
            ++i;
            classyKeys[i].whichKey = Key.A;
            ++i;
            classyKeys[i].whichKey = Key.S;
            ++i;
            classyKeys[i].whichKey = Key.H;
            ++i;
            classyKeys[i].whichKey = Key.J;
            ++i;
            
            actualNumberOfKeysToTrack = i;
            // i = 30
        }

        /// <summary>
        /// NOTE THAT THIS METHOD DOES NOT APPEAR TO WORK OVER REMOTE CONNECTION
        /// TODO: come up with al method that accomodates remote connection
        /// </summary>
        /// <param name="whichKeyState"></param>
        /// <returns></returns>
        public Key checkForKeyboardEvents(KeyboardState whichKeyState)
        {
            Key valueToReturn = Key.Unknown;

            if (doUseOutsideSource) // older method that works with VNC
            {
                if (inputReceivedFromOutsideSource)
                {
                    //if (whichKeyState.IsAnyKeyDown) // skip this step if nothing is down
                    //{
                    for (int i = 0; i<maxNumberOfKeysToTrack; ++i)
                    {
                        if (classyKeys[i].isDown)
                        {
                            valueToReturn = classyKeys[i].whichKey;
                        }
                    }
                    
                    //}

                    inputReceivedFromOutsideSource = false;
                    resetAllKeys();
                }
            }
            else
            {
                // did anything change?
                if (whichKeyState != prevKeyboardState)
                {
                    prevKeyboardState = whichKeyState;
                    // is something down that wasn't before?
                    if (whichKeyState.IsAnyKeyDown) // skip this step if nothing is down
                    {
                        for (int i = 0; i < maxNumberOfKeysToTrack; ++i)
                        {
                            if (whichKeyState.IsKeyDown(classyKeys[i].whichKey))
                            {
                                valueToReturn = testForDown(i);
                            }
                        }

                    }

                    // is something up that was down before?
                    resetAllKeys();
                }
            }
            return valueToReturn;
        }

        private Key testForDown(int whichkeyClassID)
        {
            Key valueToReturn = Key.Unknown;

            if (!classyKeys[whichkeyClassID].isDown) // is it new
            {
                classyKeys[whichkeyClassID].isDown = true;
                valueToReturn = classyKeys[whichkeyClassID].whichKey;
            }
            return valueToReturn;
        }

        private void resetAllKeys()
        {
            for (int i = 0; i < maxNumberOfKeysToTrack; ++i)
            {
                classyKeys[i].isDown = false;
            }
        }

        //method that works over VNC
        public void keyboardEntryFromWindow(Key whichKey)
        {
            for (int i = 0; i < actualNumberOfKeysToTrack; ++i)
            {
                if (whichKey == classyKeys[i].whichKey)
                {
                    classyKeys[i].isDown = true;
                    inputReceivedFromOutsideSource = true;
                }
            }
        }
    }
}
