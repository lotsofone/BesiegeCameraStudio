using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace CameraStudio
{
    class ReCamKey
    {
        public float time;
        public HermitePosRot posRot = new HermitePosRot();//艾米插值的坐标和姿态
        public HermiteFloat fov = new HermiteFloat();//艾米插值Fov
        public float nearClip = 0.3f;//线性插值nearClip
        public float farClip = 1500;//线性插值farClip
        public ReCamKey()
        {

        }
        public ReCamKey(ReCamKey other)
        {
            this.time = other.time; this.posRot = other.posRot; this.fov.value = other.fov.value;
            this.posRot = new HermitePosRot(other.posRot);
        }
        public void GenerateDerivatives(ReCamKey left, ReCamKey right)
        {
            float leftTime = this.time - left.time; float rightTime = right.time - this.time;
            posRot.GenerateDerivatives(left.posRot, right.posRot, leftTime, rightTime);
            fov.GenerateDerivatives(left.fov, right.fov, leftTime, rightTime);
        }
        public static ReCamKey Lerp(ReCamKey first, ReCamKey second, float t)
        {
            ReCamKey ret = new ReCamKey();
            HermitePosRot.HermiteLerp(first.posRot, second.posRot, t, out ret.posRot.position, out ret.posRot.rotation);
            ret.fov.value = HermiteFloat.HermiteLerp(first.fov, second.fov, t);
            ret.nearClip = Mathf.Lerp(first.nearClip, second.nearClip, t);
            ret.farClip = Mathf.Lerp(first.farClip, second.farClip, t);
            return ret;
        }
        public string ToStringData()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(time);
            sb.Append(":");
            sb.Append(posRot.ToStringData());
            sb.Append(":");
            sb.Append(fov.value);
            sb.Append(":");
            sb.Append(farClip);
            sb.Append(":");
            sb.Append(nearClip);
            return sb.ToString();
        }
        public void fromStringData(string str)
        {
            string[] strs = str.Split(':');
            this.time = Convert.ToSingle(strs[0]);
            this.posRot.FromStringData(strs[1]);
            this.fov.value = Convert.ToSingle(strs[2]);
            this.farClip = Convert.ToSingle(strs[3]);
            this.nearClip = Convert.ToSingle(strs[4]);
        }
    }
}
