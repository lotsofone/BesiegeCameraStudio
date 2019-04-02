using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace CameraStudio
{
    class HermitePosRot
    {
        //Hermite三次差值运动帧，包含位置和旋转
        public Quaternion leftDerRotation, rotation, rightDerRotation;
        public Vector3 leftDerPosition, position, rightDerPosition;
        public HermitePosRot(Vector3 position, Quaternion rotation)
        {
            this.position = position; this.rotation = rotation;
        }
        public HermitePosRot()
        {

        }
        public HermitePosRot(HermitePosRot other)
        {
            this.position = other.position; this.leftDerPosition = other.leftDerPosition; this.rightDerPosition = other.rightDerPosition;
            this.rotation = other.rotation; this.leftDerRotation = other.leftDerRotation; this.rightDerRotation = other.rightDerRotation;
        }
        public void GenerateDerivatives(HermitePosRot left, HermitePosRot right, float leftTime, float rightTime)
        {
            if (left == this) left = new HermitePosRot(this);
            if (right == this) right = new HermitePosRot(this);
            if (leftTime <= 0) leftTime = rightTime;
            if (rightTime <= 0) rightTime = leftTime;
            if (leftTime <= 0) leftTime = rightTime = 1;
            float leftPortion = leftTime / (leftTime + rightTime);
            float rightPortion = rightTime / (leftTime + rightTime);
            //求position的引导点
            Vector3 mv = this.position - (left.position + right.position) / 2;
            this.leftDerPosition = Vector3.Lerp(this.position, left.position + mv, leftPortion * 2 / 3f);
            this.rightDerPosition = Vector3.Lerp(this.position, right.position + mv, rightPortion * 2 / 3f);
            //求rotation的引导点
            Quaternion mq = this.rotation * Quaternion.Inverse(Quaternion.Lerp(left.rotation, right.rotation, 0.5f));
            this.leftDerRotation = Quaternion.Lerp(this.rotation, mq * left.rotation, leftPortion * 2 / 3f);
            this.rightDerRotation = Quaternion.Lerp(this.rotation, mq * right.rotation, rightPortion * 2 / 3f);
        }
        public static void HermiteLerp(HermitePosRot first, HermitePosRot second, float t, out Vector3 position, out Quaternion rotation)
        {
            if (t < 0)
            {
                position = first.position; rotation = first.rotation; return;
            }
            if (t > 1)
            {
                position = second.position; rotation = second.rotation; return;
            }
            Quaternion a1 = Quaternion.Lerp(first.rotation, first.rightDerRotation, t);
            Quaternion a2 = Quaternion.Lerp(first.rightDerRotation, second.leftDerRotation, t);
            Quaternion a3 = Quaternion.Lerp(second.leftDerRotation, second.rotation, t);
            Quaternion b1 = Quaternion.Lerp(a1, a2, t);
            Quaternion b2 = Quaternion.Lerp(a2, a3, t);
            rotation = Quaternion.Lerp(b1, b2, t);

            position = first.position * (1 - t) * (1 - t) * (1 - t) + 3 * first.rightDerPosition * (1 - t) * (1 - t) * t +
                3 * second.leftDerPosition * (1 - t) * t * t + second.position * t * t * t;
        }
        public string ToStringData()
        {
            string str = "" + this.position[0] + "," + this.position[1] + "," + this.position[2] + "," +
                this.rotation[0] + "," + this.rotation[1] + "," + this.rotation[2] + "," + this.rotation[3];
            return str;
        }
        public void FromStringData(string str)
        {
            string[] strs = str.Split(',');
            position[0] = Convert.ToSingle(strs[0]);
            position[1] = Convert.ToSingle(strs[1]);
            position[2] = Convert.ToSingle(strs[2]);
            rotation = new Quaternion(Convert.ToSingle(strs[3]), Convert.ToSingle(strs[4]), Convert.ToSingle(strs[5]), Convert.ToSingle(strs[6]));
        }
    }
}
