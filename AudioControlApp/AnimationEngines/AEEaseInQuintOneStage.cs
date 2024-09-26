using System;


namespace AudioControlApp.AnimationEngines
{
    class AEEaseInQuintOneStage
    {
        private float defaultVal, targetVal; // animate from defaul to target
        private float cuePoint0, cuePoint1; // begin animation at 0, end at 1

        private float animatedValue;

        public void setAnimationValues(float startValue, float endValue, int que0, int que1)
        {
            defaultVal = startValue;
            targetVal = endValue;

            cuePoint0 = (float)que0;
            cuePoint1 = (float)que1;
        }

        public float getAnimatedValue(int currentFrameInt)
        {
            float currentFrame = (float)currentFrameInt;
            unsafe
            {
                if ((currentFrame >= cuePoint0) & (currentFrame < cuePoint1))
                {
                    animatedValue = easeInQuint((currentFrame - cuePoint0), defaultVal, (targetVal - defaultVal), (cuePoint1 - cuePoint0));
                    return animatedValue;
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

        private static float _mathPI = (float)Math.PI;

        public static float easeInQuint(float t, float b, float c, float d)
        {
            return c * (t /= d) * t * t * t * t + b;
        }
    }
}
