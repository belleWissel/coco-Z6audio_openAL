using System;


namespace AudioControlApp.AnimationEngines
{
    class AEEaseOutElasticOneStage
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
                    animatedValue = easeOutElastic((currentFrame - cuePoint0), defaultVal, (targetVal - defaultVal), (cuePoint1 - cuePoint0));
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

        public static float easeOutElastic(float t, float b, float c, float d)
        {
            if ((t /= d) == 1) return b + c;
            float p = d * .3f;
            float s = p / 4f;
            return (c * (float)Math.Pow(2f, -10f * t) * (float)Math.Sin((t * d - s) * (2f * _mathPI) / p) + c + b);
        }

    }
}
