using System;


namespace AudioControlApp.AnimationEngines
{
    class AEEaseInOutLinearOneStage
    {
        private float defaultVal, targetVal; // animate from defaul to target and back
        private float cuePoint0, cuePoint1; // begin animation at 0, end at 1

        private float animatedValue;
 
        /// <summary>
        /// 
        /// </summary>
        /// <param name="startValue"> start value </param>
        /// <param name="endValue"> final target value </param>
        /// <param name="que0"> begin animation frame </param>
        /// <param name="que1"> end animation frame </param>
        public void setAnimationValues(float startValue, float endValue, int que0, int que1)
        {
            defaultVal = startValue;
            targetVal = endValue;

            cuePoint0 = (float)que0;
            cuePoint1 = (float)que1;
        }

        public float returnFinalValue()
        {
            return targetVal;
        }

        public float getAnimatedValue(int currentFrameInt)
        {
            unsafe
            {
                float currentFrame = (float)currentFrameInt;

                if ((currentFrame >= cuePoint0) & (currentFrame < cuePoint1))
                {
                    float valueToReturn = linear((currentFrame - cuePoint0), defaultVal, (targetVal - defaultVal), (cuePoint1 - cuePoint0));
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
        public float linear(float t, float b, float c, float d)
        {
            return c * t / d + b;
        }
    }
}
