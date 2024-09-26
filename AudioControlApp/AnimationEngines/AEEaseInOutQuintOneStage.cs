using System;


namespace AudioControlApp.AnimationEngines
{
    class AEEaseInOutQuintOneStage
    {
        private float defaultVal, targetVal; // animate from defaul to target and back
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
                    animatedValue = easeInOutQuint((currentFrame - cuePoint0), defaultVal, (targetVal - defaultVal), (cuePoint1 - cuePoint0));
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

        private float easeInOutQuint(float t, float b, float c, float d)
        {
            if ((t /= d / 2) < 1) return c / 2 * t * t * t * t * t + b;
            return c / 2 * ((t -= 2) * t * t * t * t + 2) + b;
        }
    }
}
