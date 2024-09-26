using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AudioControlApp.ReactiveAreaManagement
{
    class AreaActivationState
    {
        // each activation region is represented by this class. hardwired for 3 activation deistances
        public int currentClosestActivation = 3;

        private static int numberOfActivationLevels = 3; // 0 = closest, 1 = middle, 2 = farthest

        private bool[] isActive = new bool[numberOfActivationLevels]; // isActive[0] = true

        // **************************************
        // GET/SET has state changed?
        // **************************************
        public bool dirty = false;
        public bool isDirty()
        {
            bool valueToReturn = dirty;
            if (dirty)
            {
                dirty = false;
            }
            return valueToReturn;
        }

        public void activeState(int whichLevel) // 0 = closest
        {
            if (!isActive[whichLevel])
            {
                isActive[whichLevel] = true; 
            }
            checkForClosestActivation();
        }

        public void deactiveState(int whichLevel)
        {
            if (isActive[whichLevel])
            {
                isActive[whichLevel] = false;
            }
            checkForClosestActivation();
        }

        private void checkForClosestActivation()
        {
            int newClosestActivation = 3; // nothing activated

            if (isActive[2])
                newClosestActivation = 2; // farthest
            if (isActive[1])
                newClosestActivation = 1; // mid
            if (isActive[0])
                newClosestActivation = 0; // close


            if (newClosestActivation != currentClosestActivation) // somthing changed
            {
                currentClosestActivation = newClosestActivation;
                dirty = true;
            }
        }
    }
}
