using System;
using OpenTK;

namespace AudioControlApp.AnimationEngines
{
    class GeneralPurposeDisplayAnimation
    {

        // ********************************************************************************
        // 3d Position
        public Vector3 currentPosition;
        public float[] basePosition = new float[3];
        public float[] posn = new float[3];
        public float[] targetPosn = new float[3];

        //private Vector3 targetPosn;
        private int[] posnAnimationFrame = new int[3]; // x, y, and z
        private bool[] posnAnimationActive = new bool[3]; // x, y, and z
        private int[] posnAnimationLength = new int[3]; // x, y, and z
        private AEEaseOutBackOneStage[] targetPosnAnim = new AEEaseOutBackOneStage[3]; // x, y, and z

        // ********************************************************************************
        // 3d Rotation 
        public Vector3 currentRotation;
        public float[] baseRotation = new float[3];
        public float[] rotn = new float[3];
        public float[] targetRotn = new float[3];

        //private Vector3 targetPosn;
        private int[] rotnAnimationFrame = new int[3]; // x, y, and z
        private bool[] rotnAnimationActive = new bool[3]; // x, y, and z
        private int[] rotnAnimationLength = new int[3]; // x, y, and z
        private AEEaseInOutQuintOneStage[] targetRotnAnim = new AEEaseInOutQuintOneStage[3]; // x, y, and z

        // ********************************************************************************
        // Scale
        public float scale;
        public float targetScale;
        private bool scaleAnimationActive = false;
        private int scaleAnimationFrame = 0;
        private int scaleAnimationLength = 20;
        private AEEaseOutElasticOneStage targetScaleAnim;

        // ********************************************************************************
        // Transparency
        public float alpha;
        public float targetAlpha;
        private bool alphaAnimationActive = false;
        private int alphaAnimationFrame = 0;
        private int alphaAnimationLength = 20;
        private AEEaseInOutLinearOneStage targetAlphaAnim;

        // ********************************************************************************
        // other animated value
        public float value;
        public float targetValue;
        private bool valueAnimationActive = false;
        private int valueAnimationFrame = 0;
        private int valueAnimationLength = 20;
        private AEEaseInOutLinearOneStage targetValueAnim;

        public GeneralPurposeDisplayAnimation()
        {
            currentPosition = Vector3.Zero;
            
            alpha = 0.0f;
            targetAlpha = 0.1f;
            scale = 0.0f;
            targetScale = 100.0f;
            value = 0.0f;
            targetValue = 0.0f;

            int i;
            for (i = 0; i < 3; ++i)
            {
                basePosition[i] = 0.0f;
                posn[i] = 0.0f;
                targetPosn[i] = 0.0f;
                targetPosnAnim[i] = new AEEaseOutBackOneStage();

                baseRotation[i] = 0.0f;
                rotn[i] = 0.0f;
                targetRotn[i] = 0.0f;
                targetRotnAnim[i] = new AEEaseInOutQuintOneStage();

            }
            
            targetScaleAnim = new AEEaseOutElasticOneStage();
            targetAlphaAnim = new AEEaseInOutLinearOneStage();

            targetValueAnim = new AEEaseInOutLinearOneStage();
        }

        public void setPosnOffsets(float whichX, float whichY, float whichZ)
        {
            basePosition[0] = whichX;
            basePosition[1] = whichY;
            basePosition[2] = whichZ;

            int animLeng = startPositionAnimation(2); // animate to Z position
            startPositionAnimation(0, animLeng);
            startPositionAnimation(1, animLeng);
            startAlphaAnimation(animLeng);
            startScaleAnimation(animLeng);
        }

        public void setRotnOffsets(float whichX, float whichY, float whichZ)
        {
            baseRotation[0] = whichX;
            baseRotation[1] = whichY;
            baseRotation[2] = whichZ;
 
            int animLeng = startRotationAnimation(2); // animate to Z position
            startRotationAnimation(0, animLeng);
            startRotationAnimation(1, animLeng);
        }

        // ****************************************************************************************************************************************************************

        #region set new values and start aniamtions

        public int setNewPosn(int whichAxis, float whichPosn)
        {
            targetPosn[whichAxis] = whichPosn;
            return startPositionAnimation(whichAxis);
        }
        public int setNewPosn(int whichAxis, float whichPosn, int whichAnimLength)
        {
            targetPosn[whichAxis] = whichPosn;
            return startPositionAnimation(whichAxis, whichAnimLength);
        }
        public int setNewPosn(int whichAxis, float whichPosn, int whichAnimLength, int whichDelay)
        {
            targetPosn[whichAxis] = whichPosn;
            return startPositionAnimation(whichAxis, whichAnimLength, whichDelay);
        }



        // ****************************************************************************************************************************************************************

        public int setNewRotn(int whichAxis, float whichRotn)
        {
            targetRotn[whichAxis] = whichRotn;
            return startRotationAnimation(whichAxis);
        }
        public int setNewRotn(int whichAxis, float whichRotn, int whichAnimLength)
        {
            targetRotn[whichAxis] = whichRotn;
            return startRotationAnimation(whichAxis, whichAnimLength);
        }
        public int setNewRotn(int whichAxis, float whichRotn, int whichAnimLength, int whichDelay)
        {
            targetRotn[whichAxis] = whichRotn;
            return startRotationAnimation(whichAxis, whichAnimLength, whichDelay);
        }



        // ****************************************************************************************************************************************************************

        public void setNewAlpha(float whichA)
        {
            //if (whichA != targetAlpha)
            //{
                targetAlpha = whichA;
                startAlphaAnimation(-1);
            //}
            //else
            //{
            //    stopAlphaAnimation();
            //}
        }
        public void setNewAlpha(float whichA, int whichL)
        {
            //if (whichA != targetAlpha)
            //{
                targetAlpha = whichA;
                startAlphaAnimation(whichL);
            //}
            //else
            //{
            //    stopAlphaAnimation();
            //}
        }
        public void setNewAlpha(float whichA, int whichL, int whichDelay)
        {
            //if (whichA != targetAlpha)
            //{
                targetAlpha = whichA;
                startAlphaAnimation(whichL, whichDelay);
            //}
            //else
            //{
            //    stopAlphaAnimation();
            //}
        }
        // ****************************************************************************************************************************************************************

        public void setNewValue(float whichV)
        {
            targetValue = whichV;
            startValueAnimation(-1);
        }
        public void setNewValue(float whichV, int whichL)
        {
            targetValue = whichV;
            startValueAnimation(whichL);
        }
        public void setNewValue(float whichV, float whichLf)
        {
            int whichL = (int)Math.Floor((double)whichLf);
            targetValue = whichV;
            startValueAnimation(whichL);
        }
        public void setNewValue(float whichV, int whichL, int whichDelay)
        {
            targetValue = whichV;
            startValueAnimation(whichL, whichDelay);
        }

        // ****************************************************************************************************************************************************************

        public void setNewScale(float whichS)
        {
            //if (whichS != targetScale)
            //{
                targetScale = whichS;
                startScaleAnimation(-1);
            //}
        }
        public void setNewScale(float whichS, int whichL)
        {
            //if (whichS != targetScale)
            //{
                targetScale = whichS;
                startScaleAnimation(whichL);
            //}
        }
        public void setNewScale(float whichS, int whichL, int whichDelay)
        {
            //if (whichS != targetScale)
            //{
                targetScale = whichS;
                startScaleAnimation(whichL, whichDelay);
            //}
        }

        #endregion set new values and start aniamtions

        // ****************************************************************************************************************************************************************

        public void update()
        {
            float delta;

            unsafe
            {
                for (int i = 0; i < 3; ++i)
                {
                    if (posnAnimationActive[i])
                    {
                        delta = targetPosn[i] - posn[i];
                        //System.Diagnostics.Debug.WriteLine("new position: " + posn.Y + " target:" + targetPosn.Y);
                        if (posnAnimationFrame[i] < posnAnimationLength[i] * 2) // still animating target
                        {
                            posnAnimationFrame[i] += 1;
                            targetPosn[i] = targetPosnAnim[i].getAnimatedValue(posnAnimationFrame[i]);
                            delta = targetPosn[i] - posn[i];
                            posn[i] += delta / 2.0f;
                        }
                        else
                        {
                            if (Math.Abs(delta) > 1.0) // still not there
                            {
                                posn[i] += delta / 2.0f;
                            }
                            else
                            {
                                posn[i] = targetPosnAnim[i].getAnimatedValue(posnAnimationLength[i] * 2); // set to end value
                                posnAnimationActive[i] = false;
                            }
                        }
                    }


                    if (rotnAnimationActive[i])
                    {
                        delta = targetRotn[i] - rotn[i];
                        if (rotnAnimationFrame[i] < rotnAnimationLength[i] * 2) // still animating target
                        {
                            rotnAnimationFrame[i] += 1;
                            targetRotn[i] = targetRotnAnim[i].getAnimatedValue(rotnAnimationFrame[i]);
                            delta = targetRotn[i] - rotn[i];
                            rotn[i] += delta / 2.0f;
                        }
                        else
                        {
                            if (Math.Abs(delta) > 1.0) // still not there
                            {
                                rotn[i] += delta / 2.0f;
                            }
                            else
                            {
                                rotn[i] = targetRotnAnim[i].getAnimatedValue(rotnAnimationLength[i] * 2); // set to end value
                                rotnAnimationActive[i] = false;
                            }
                        }
                    }
                }

                if (scaleAnimationActive)
                {
                    delta = targetScale - scale;
                    if (scaleAnimationFrame < scaleAnimationLength * 2) // still animating target
                    {
                        scaleAnimationFrame += 1;
                        targetScale = targetScaleAnim.getAnimatedValue(scaleAnimationFrame);
                        delta = targetScale - scale;
                        scale += delta / 4.0f;
                    }
                    else
                    {
                        if (Math.Abs(delta) > 0.01) // still not there
                        {
                            scale += delta / 4.0f;
                        }
                        else
                        {
                            scale = targetScaleAnim.getAnimatedValue(scaleAnimationLength * 2); // set to end value
                            scaleAnimationActive = false;
                        }
                    }
                }

                if (alphaAnimationActive)
                {
                    delta = targetAlpha - alpha;
                    if (alphaAnimationFrame < alphaAnimationLength * 2) // still animating target
                    {
                        alphaAnimationFrame += 1;
                        targetAlpha = targetAlphaAnim.getAnimatedValue(alphaAnimationFrame);
                        delta = targetAlpha - alpha;
                        alpha += delta / 4.0f;
                    }
                    else
                    {
                        if (Math.Abs(delta) > 0.01) // still not there
                        {
                            alpha += delta / 4.0f;
                        }
                        else
                        {
                            alpha = targetAlphaAnim.getAnimatedValue(alphaAnimationLength * 2); // set to end value
                            alphaAnimationActive = false;
                        }
                    }
                }
                if (valueAnimationActive)
                {
                    delta = targetValue - value;
                    if (valueAnimationFrame < valueAnimationLength * 2) // still animating target
                    {
                        valueAnimationFrame += 1;
                        targetValue = targetValueAnim.getAnimatedValue(valueAnimationFrame);
                        delta = targetValue - value;
                        value += delta / 4.0f;
                    }
                    else
                    {
                        //if (Math.Abs(delta) > 0.01) // still not there
                        if (Math.Abs(delta) > Math.Abs((targetValueAnim.returnFinalValue() / 100.0f))) // still not there (off by more than 1 percent)
                        {
                            value += delta / 4.0f;
                        }
                        else
                        {
                            //value = targetValueAnim.getAnimatedValue(valueAnimationLength * 2); // set to end value
                            value = targetValueAnim.returnFinalValue();
                            valueAnimationActive = false;
                        }
                    }
                }
            }// end of unsafe
        }

        // ****************************************************************************************************************************************************************
        #region position animation

        public Vector3 getCurrentPosition()
        {
            currentPosition.X = posn[0] + basePosition[0];
            currentPosition.Y = posn[1] + basePosition[1];
            currentPosition.Z = posn[2] + basePosition[2];

            return currentPosition;
        }

        public Vector3 getCurrentPositionWoBaseOffset()
        {
            Vector3 valueToReturn;

            valueToReturn.X = posn[0];
            valueToReturn.Y = posn[1];
            valueToReturn.Z = posn[2];

            return valueToReturn;
        }

        public Vector3 addBaseOffsetTo(Vector3 incomingPosition)
        {
            Vector3 valueToReturn;

            valueToReturn.X = incomingPosition.X + basePosition[0];
            valueToReturn.Y = incomingPosition.Y + basePosition[1];
            valueToReturn.Z = incomingPosition.Z + basePosition[2];

            return valueToReturn;
        }


        private int startPositionAnimation(int whichAxis)
        {
            int numberOfFrames = (int)Math.Ceiling(Math.Abs((double)(targetPosn[whichAxis] - posn[whichAxis])) / 12.0);
            if (numberOfFrames < 15)
                numberOfFrames = 15;

            posnAnimationLength[whichAxis] = numberOfFrames;
            posnAnimationFrame[whichAxis] = 0;
            posnAnimationActive[whichAxis] = true;

            targetPosnAnim[whichAxis].setAnimationValues(posn[whichAxis], targetPosn[whichAxis], posnAnimationFrame[whichAxis], posnAnimationLength[whichAxis]);

            return posnAnimationLength[whichAxis];
        }

        private int startPositionAnimation(int whichAxis, int whichLength)
        {
            //int numberOfFrames = (int)Math.Ceiling(Math.Abs((double)(targetPosn[whichAxis] - posn[whichAxis])) / 30.0);
            //if (numberOfFrames < 15)
            //    numberOfFrames = 15;

            posnAnimationLength[whichAxis] = whichLength;
            posnAnimationFrame[whichAxis] = 0;
            posnAnimationActive[whichAxis] = true;

            targetPosnAnim[whichAxis].setAnimationValues(posn[whichAxis], targetPosn[whichAxis], posnAnimationFrame[whichAxis], posnAnimationLength[whichAxis]);

            return posnAnimationLength[whichAxis];
        }

        private int startPositionAnimation(int whichAxis, int whichLength, int whichDelay)
        {
            //int numberOfFrames = (int)Math.Ceiling(Math.Abs((double)(targetPosn[whichAxis] - posn[whichAxis])) / 30.0);
            //if (numberOfFrames < 15)
            //    numberOfFrames = 15;

            posnAnimationLength[whichAxis] = whichLength + whichDelay;
            posnAnimationFrame[whichAxis] = 0;
            posnAnimationActive[whichAxis] = true;

            targetPosnAnim[whichAxis].setAnimationValues(posn[whichAxis], targetPosn[whichAxis], posnAnimationFrame[whichAxis] + whichDelay, posnAnimationLength[whichAxis]);

            return posnAnimationLength[whichAxis];
        }

        #endregion position animation

        // ****************************************************************************************************************************************************************


        // ****************************************************************************************************************************************************************
        #region rotation animation

        public Vector3 getCurrentRotation()
        {
            currentRotation.X = rotn[0] + baseRotation[0];
            currentRotation.Y = rotn[1] + baseRotation[0];
            currentRotation.Z = rotn[2] + baseRotation[0];

            return currentRotation;
        }

        /// <summary>
        /// returns normalized vector 0-1
        /// </summary>
        /// <returns></returns>
        public Vector3 getCurrentRotationForShader()
        {
            currentRotation.X = (rotn[0] + baseRotation[0]) / 360.0f;
            currentRotation.Y = (rotn[1] + baseRotation[1]) / 360.0f;
            currentRotation.Z = (rotn[2] + baseRotation[2]) / 360.0f;

            return currentRotation;
        }


        private int startRotationAnimation(int whichAxis)
        {
            int numberOfFrames = (int)Math.Ceiling(Math.Abs((double)(targetRotn[whichAxis] - rotn[whichAxis])) / 12.0);
            if (numberOfFrames < 15)
                numberOfFrames = 15;

            rotnAnimationLength[whichAxis] = numberOfFrames;
            rotnAnimationFrame[whichAxis] = 0;
            rotnAnimationActive[whichAxis] = true;

            targetRotnAnim[whichAxis].setAnimationValues(rotn[whichAxis], targetRotn[whichAxis], rotnAnimationFrame[whichAxis], rotnAnimationLength[whichAxis]);

            return rotnAnimationLength[whichAxis];
        }

        private int startRotationAnimation(int whichAxis, int whichLength)
        {
            //int numberOfFrames = (int)Math.Ceiling(Math.Abs((double)(targetPosn[whichAxis] - posn[whichAxis])) / 30.0);
            //if (numberOfFrames < 15)
            //    numberOfFrames = 15;

            rotnAnimationLength[whichAxis] = whichLength;
            rotnAnimationFrame[whichAxis] = 0;
            rotnAnimationActive[whichAxis] = true;

            targetRotnAnim[whichAxis].setAnimationValues(rotn[whichAxis], targetRotn[whichAxis], rotnAnimationFrame[whichAxis], rotnAnimationLength[whichAxis]);

            return rotnAnimationLength[whichAxis];
        }

        private int startRotationAnimation(int whichAxis, int whichLength, int whichDelay)
        {
            //int numberOfFrames = (int)Math.Ceiling(Math.Abs((double)(targetPosn[whichAxis] - posn[whichAxis])) / 30.0);
            //if (numberOfFrames < 15)
            //    numberOfFrames = 15;

            rotnAnimationLength[whichAxis] = whichLength + whichDelay;
            rotnAnimationFrame[whichAxis] = 0;
            rotnAnimationActive[whichAxis] = true;

            targetRotnAnim[whichAxis].setAnimationValues(rotn[whichAxis], targetRotn[whichAxis], rotnAnimationFrame[whichAxis] + whichDelay, rotnAnimationLength[whichAxis]);

            return rotnAnimationLength[whichAxis];
        }

        #endregion rotation animation

        // ****************************************************************************************************************************************************************
        

        // ****************************************************************************************************************************************************************
        #region scale animation

        private void startScaleAnimation(int whichNumberOfFrames)
        {
            if (whichNumberOfFrames == -1)
                whichNumberOfFrames = 20;

            scaleAnimationLength = whichNumberOfFrames;
            scaleAnimationFrame = 0;
            scaleAnimationActive = true;

            targetScaleAnim.setAnimationValues(scale, targetScale, scaleAnimationFrame, scaleAnimationLength);
        }

        private void startScaleAnimation(int whichNumberOfFrames, int whichDelay)
        {
            if (whichNumberOfFrames == -1)
                whichNumberOfFrames = 20;

            scaleAnimationLength = whichNumberOfFrames + whichDelay;
            scaleAnimationFrame = 0;
            scaleAnimationActive = true;

            targetScaleAnim.setAnimationValues(scale, targetScale, whichDelay, scaleAnimationLength);
        }

        #endregion scale animation

        // ****************************************************************************************************************************************************************
        #region alpha animation

        private void startAlphaAnimation(int whichNumberOfFrames)
        {
            if (whichNumberOfFrames == -1)
                whichNumberOfFrames = 15;

            alphaAnimationLength = whichNumberOfFrames;
            alphaAnimationFrame = 0;
            alphaAnimationActive = true;

            targetAlphaAnim.setAnimationValues(alpha, targetAlpha, 0, alphaAnimationLength);
        }
        private void startAlphaAnimation(int whichNumberOfFrames, int whichDelay)
        {
            if (whichNumberOfFrames == -1)
                whichNumberOfFrames = 15;

            alphaAnimationLength = whichNumberOfFrames + whichDelay;
            alphaAnimationFrame = 0;
            alphaAnimationActive = true;

            targetAlphaAnim.setAnimationValues(alpha, targetAlpha, whichDelay, alphaAnimationLength);
        }

        private void stopAlphaAnimation()
        {
            alphaAnimationActive = false;
        }

        #endregion alpha animation
        
        // ****************************************************************************************************************************************************************
        #region value animation

        private void startValueAnimation(int whichNumberOfFrames)
        {
            if (whichNumberOfFrames == -1)
                whichNumberOfFrames = 15;

            valueAnimationLength = whichNumberOfFrames;
            valueAnimationFrame = 0;
            valueAnimationActive = true;

            targetValueAnim.setAnimationValues(value, targetValue, valueAnimationFrame, valueAnimationLength);
        }

        private void startValueAnimation(int whichNumberOfFrames, int whichDelay)
        {
            if (whichNumberOfFrames == -1)
                whichNumberOfFrames = 15;

            valueAnimationLength = whichNumberOfFrames + whichDelay;
            valueAnimationFrame = 0;
            valueAnimationActive = true;

            targetValueAnim.setAnimationValues(value, targetValue, whichDelay, valueAnimationLength);
        }

        #endregion value animation

        // ****************************************************************************************************************************************************************

    }
}
