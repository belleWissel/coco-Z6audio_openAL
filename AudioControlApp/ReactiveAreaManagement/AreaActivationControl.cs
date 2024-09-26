

namespace AudioControlApp.ReactiveAreaManagement
{
    class AreaActivationControl
    {

        private static int maxNumberOfAreas = 12;
        private int actualNumberOfAreas = 5;
        private int areaStartIndex = 0;
        private AreaActivationState[] activationState = new AreaActivationState[maxNumberOfAreas];

        public string currentActivationFeedback = "ACTIVATION: left to right: [0] [0] [0] [0] [0] [0] ";
        public string currentActivationFeedbackForClient = "0 0 0 0 0 0 0";

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

        public AreaActivationControl()
        {
            

            for (int i = 0; i < maxNumberOfAreas; ++i)
            {
                activationState[i] = new AreaActivationState();
            }
        }

        public void assignActualNumberOfAreas(int whichAreaNumber)
        {
            actualNumberOfAreas = whichAreaNumber;
        }

        public void assignAreaStartIndex(int whichIndexNumber)
        {
            areaStartIndex = whichIndexNumber;
        }

        public void updateActivation(int whichArea, int whichLevel, bool doActivate) // level 0 = closest
        {
            //System.Diagnostics.Debug.WriteLine("[TABLECOMM] updating Activation: area:["+whichArea+"] level: ["+whichLevel+"] isactive: ["+doActivate+"]");
            
            if (whichArea >= maxNumberOfAreas)
                return;

            if (doActivate)
                activationState[whichArea].activeState(whichLevel);
            else
                activationState[whichArea].deactiveState(whichLevel);

            if (activationState[whichArea].isDirty()) // something relevant changed as a result of the change in activation
            {
                dirty = true;
                updateDebugFeedback();
            }
        }

        public void init()
        {
            updateDebugFeedback();
        }


        private void updateDebugFeedback()
        {
            int areaIndex = areaStartIndex; // index of activation area may be offset
            
            currentActivationFeedback = "ACTIVATION: left to right: [" + areaIndex + ", " + activationState[0].currentClosestActivation + "]";
            currentActivationFeedbackForClient = "" + areaIndex + " " + activationState[0].currentClosestActivation;
            for (int i = 1; i < actualNumberOfAreas; ++i)
            {
                areaIndex = areaStartIndex + i;
                currentActivationFeedback += " [" + areaIndex + ", " + activationState[i].currentClosestActivation + "]";
                currentActivationFeedbackForClient += " " + areaIndex + " " + activationState[i].currentClosestActivation;
            } 
            currentActivationFeedback += " \n";
            currentActivationFeedbackForClient += "\n";
        }
    }
}
