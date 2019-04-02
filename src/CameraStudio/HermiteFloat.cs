using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace CameraStudio
{
    struct HermiteFloat
    {
        public float left, value, right;
        public HermiteFloat(float value)
        {
            this.value = value;
            this.left = this.right = 0;
        }
        public void GenerateDerivatives(HermiteFloat left, HermiteFloat right, float leftTime, float rightTime)
        {
            if (leftTime <= 0) leftTime = rightTime;
            if (rightTime <= 0) rightTime = leftTime;
            if (leftTime <= 0) leftTime = rightTime = 1;
            float leftPortion = leftTime / (leftTime + rightTime);
            float rightPortion = rightTime / (leftTime + rightTime);
            //求position的引导点
            float mv = this.value - (left.value + right.value) / 2;
            this.left = Mathf.Lerp(this.value, left.value + mv, leftPortion * 2 / 3f);
            this.right = Mathf.Lerp(this.value, right.value + mv, rightPortion * 2 / 3f);
        }
        public static float HermiteLerp(HermiteFloat first, HermiteFloat second, float t)
        {
            if (t < 0)
            {
                return first.value;
            }
            if (t > 1)
            {
                return second.value;
            }
            return first.value * (1 - t) * (1 - t) * (1 - t) + 3 * first.right * (1 - t) * (1 - t) * t +
                3 * second.left * (1 - t) * t * t + second.value * t * t * t;
        }
    }
}
