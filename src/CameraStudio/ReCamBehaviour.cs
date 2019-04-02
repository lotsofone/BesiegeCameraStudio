using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace CameraStudio
{
    class ReCamBehaviour : MonoBehaviour
    {
        public List<ReCamKey> frames;
        public float FrameTime { get { return _frameTime + _smallFrameTime; } }
        public float _frameTime;//浮点数精度不够，为了保证精度分成两级
        public float _smallFrameTime;//每个deltaTime常常是10^-3数量级
        public int frameID;
        public void OnEnable()
        {
            frameID = 1;
            _frameTime = 0;
            _smallFrameTime = 0;
        }
        public void LateUpdate()
        {
            _smallFrameTime += Time.deltaTime;
            if (_smallFrameTime > 1)
            {
                _frameTime += _smallFrameTime; _smallFrameTime = 0;
            }
            if (!StatMaster.levelSimulating) this.enabled = false;
            if (frames.Count <= 1)
            {
                GetComponent<MouseOrbit>().enabled = true;
                this.enabled = false;
                return;
            }
            while (frameID < frames.Count && FrameTime > frames[frameID].time)
            {
                frameID++;
            }
            if (frameID >= frames.Count)
            {
                GetComponent<MouseOrbit>().enabled = true;
                this.enabled = false;
                return;
            }
            float t = (FrameTime - frames[frameID - 1].time) / (frames[frameID].time - frames[frameID - 1].time);
            var lerppedKey = ReCamKey.Lerp(frames[frameID - 1], frames[frameID], t);

            transform.position = lerppedKey.posRot.position; transform.rotation = lerppedKey.posRot.rotation;
            var camera = GetComponent<Camera>();
            camera.fieldOfView = lerppedKey.fov.value;
            camera.farClipPlane = lerppedKey.farClip;
            camera.nearClipPlane = lerppedKey.nearClip;
        }
    }
}
