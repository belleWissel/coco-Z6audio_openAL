using System;


namespace AudioControlApp.AnimationEngines
{
    class AEEaseOutQuintOneStage
    {
        private float defaultVal, targetVal; // animate from defaul to target
        private float cuePoint0, cuePoint1; // begin animation at 0, end at 1

        private float animatedValue;
        /*
        public Double myValue
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
                    //animatedValue = easeOutElastic((currentFrame - cuePoint0), defaultVal, (targetVal - defaultVal), (cuePoint1 - cuePoint0));
                    animatedValue = easeOutQuint((currentFrame - cuePoint0), defaultVal, (targetVal - defaultVal), (cuePoint1 - cuePoint0));
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

        // original:
        public static float easeOutQuint(float t, float b, float c, float d)
        {
            return c * ((t = t / d - 1f) * t * t * t * t + 1f) + b;
        }

        public static float easeInQuint(float t, float b, float c, float d)
        {
            return c * (t /= d) * t * t * t * t + b;
        }


    }
}
