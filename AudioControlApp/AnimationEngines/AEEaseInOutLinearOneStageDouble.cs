using System;


namespace AudioControlApp.AnimationEngines
{
    class AEEaseInOutLinearOneStageDouble
    {
        private double defaultVal, targetVal; // animate from defaul to target and back
        private double cuePoint0, cuePoint1; // begin animation at 0, end at 1

        private double animatedValue;
        /*
        public Float myValue
        {
            get
            {
                return animatedValue;
            }
            set
            {
                animatedValue = value;
            }
        }
        */

        /// <summary>
        /// 
        /// </summary>
        /// <param name="startValue"> start value </param>
        /// <param name="endValue"> final target value </param>
        /// <param name="que0"> begin animation frame </param>
        /// <param name="que1"> end animation frame </param>
        public void setAnimationValues(double startValue, double endValue, int que0, int que1)
        {
            defaultVal = startValue;
            targetVal = endValue;

            cuePoint0 = (double)que0;
            cuePoint1 = (double)que1;
        }

        public double returnFinalValue()
        {
            return targetVal;
        }

        public double getAnimatedValue(int currentFrameInt)
        {
            unsafe
            {
                double currentFrame = (double)currentFrameInt;

                if ((currentFrame >= cuePoint0) & (currentFrame < cuePoint1))
                {
                    double valueToReturn = linear((currentFrame - cuePoint0), defaultVal, (targetVal - defaultVal), (cuePoint1 - cuePoint0));
                    return valueToReturn;
                }
                else if (currentFrame >= cuePoint1)
                {
                    return targetVal;
                }
                else
                {
                    // default with this value
                    return defaultVal;
                }
            }
        }
        public double linear(double t, double b, double c, double d)
        {
            return c * t / d + b;
        }
    }
}
