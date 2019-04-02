using Modding;
using Modding.Blocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace CameraStudio
{
    class ModMainUI : SafeUIBehaviour
    {
        //private readonly int windowID = ModUtility.GetWindowId();
        //public Rect windowRect = new Rect(300, 700, 100, 50);
        private List<ReCamKey> frames = new List<ReCamKey>();
        private readonly Texture2D selectedTexture = new Texture2D(8, 32);
        protected override void Awake()
        {
            base.Awake();
            Events.OnMachineSimulationToggle += MySimulationToggle;

            Events.OnMachineLoaded += LoadCameraData;
            Events.OnMachineLoaded += StopReplayOnLoad;
            Events.OnMachineSave += SaveCameraData;
            Events.OnMachineDestroyed += MachineDestroyed;

            ScaleTimeLine(scaleSliderValue);
            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 32; y++)
                {
                    selectedTexture.SetPixel(0, 0, new Color(0.95f, 0.90f, 0));
                }
            }
        }
        public void StopReplayOnLoad(PlayerMachineInfo info)
        {
            this.buildReplayCamera = false;
        }
        public void MachineDestroyed()
        {
            if (!syncWithMachine) return;
            this.frames.Clear();
        }
        public void LoadCameraData(PlayerMachineInfo info)
        {
            if (!syncWithMachine) return;
            this.frames.Clear();
            if (info.MachineData == null) return;
            if (info.MachineData.Read("lto_ReCam") == null) return;
            string loadString = info.MachineData.ReadString("lto_ReCam");
            if (loadString == null) return;
            string[] strs = loadString.Split('|');
            foreach (var str in strs)
            {
                ReCamKey newkey = new ReCamKey();
                newkey.fromStringData(str);
                frames.Add(newkey);
                if (newkey.time > maxFrameTime) maxFrameTime = newkey.time;
            }
        }
        public void SaveCameraData(PlayerMachineInfo info)
        {
            if (!syncWithMachine) return;
            StringBuilder sb = new StringBuilder();
            foreach (var key in frames)
            {
                sb.Append(key.ToStringData());
                sb.Append("|");
            }
            if (sb.Length > 0)
                sb.Remove(sb.Length - 1, 1);
            if (sb.Length > 0)
            {
                info.MachineData.Write("lto_ReCam", sb.ToString());
            }
            else
            {
                info.MachineData.Remove("lto_Recam");
            }
        }

        private void MySimulationToggle(PlayerMachine machine, bool start)
        {
            if (!simulateReplayCamera) return;
            GameObject cameraObject = GameObject.Find("Main Camera");
            ReCamBehaviour behaviour = cameraObject.GetComponent<ReCamBehaviour>() ?? cameraObject.AddComponent<ReCamBehaviour>();
            if (start)
            {
                cameraObject.GetComponent<MouseOrbit>().enabled = false;
                ComputeKeys();
                behaviour.frames = this.frames;
                behaviour.enabled = true;
                behaviour._frameTime = timeSelectorTime;
            }
            else
            {
                behaviour.enabled = false;
                if (!buildReplayCamera)
                    cameraObject.GetComponent<MouseOrbit>().enabled = true;
                else
                    cameraObject.GetComponent<MouseOrbit>().enabled = false;
            }
        }

        void ComputeKeys()
        {
            for (int i = 0; i < frames.Count; i++)
            {
                ReCamKey left = i > 0 ? frames[i - 1] : frames[i];
                ReCamKey right = i < frames.Count - 1 ? frames[i + 1] : frames[i];
                frames[i].GenerateDerivatives(left, right);
            }
        }
        public override bool ShouldShowGUI() { return !StatMaster.hudHidden && !StatMaster.levelSimulating; }

        readonly int timeLineWidth = 1200;
        float timeLimeScrollX;
        float timeLineScaler = 10;
        float timeSelectorTime = 0;
        float maxFrameTime = 120f;
        int selectedFrameID = -1;

        bool buildReplayCamera = false;
        bool simulateReplayCamera = true;
        bool syncWithMachine = true;

        float shiftSliderValue;//移动帧的slider
        float scaleSliderValue;//缩放时间线的slider

        float translateX, translateY, translateZ;
        float rotateX, rotateY, rotateZ;

        float fovSliderValue, nearClipSliderValue, farClipSliderValue;

        protected override void WindowContent(int windowID)
        {
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("K帧"))
            {
                KeyFrame();
            }
            if (GUILayout.Button("清空"))
            {
                selectedFrameID = -1;
                frames.Clear();
            }
            if (GUILayout.Button("删除"))
            {
                RemoveFrame();
            }

            bool lastDoReplayToCamera = buildReplayCamera;
            buildReplayCamera = GUILayout.Toggle(buildReplayCamera, "控制相机");
            if (lastDoReplayToCamera != buildReplayCamera)
            {
                if (buildReplayCamera)
                {
                    GameObject.Find("Main Camera").GetComponent<MouseOrbit>().enabled = false;
                }
                else
                {
                    GameObject.Find("Main Camera").GetComponent<MouseOrbit>().enabled = true;
                }
            }

            simulateReplayCamera = GUILayout.Toggle(simulateReplayCamera, "回放相机");
            GUILayout.Label("T:" + timeSelectorTime, GUILayout.Width(80));
            scaleSliderValue = AdjustingHorizontalSlider(scaleSliderValue, 2, "缩放");

            try
            {
                maxFrameTime = Convert.ToSingle(GUILayout.TextField(maxFrameTime.ToString(), GUILayout.Width(50)));
            }
            catch (Exception) { }
            syncWithMachine = GUILayout.Toggle(syncWithMachine, "与存档同步");
            GUILayout.EndHorizontal();
            //时间线界面
            timeLimeScrollX = GUILayout.BeginScrollView(new Vector2(timeLimeScrollX, 0), true, false, GUILayout.Height(70), GUILayout.Width(timeLineWidth)).x;
            {

                timeSelectorTime = GUILayout.HorizontalSlider(timeSelectorTime, 0, maxFrameTime, GUILayout.Width(maxFrameTime * timeLineScaler + 12));
                for (int i = 0; i < frames.Count; i++)
                {
                    bool buttonReturn;
                    if (i == selectedFrameID)
                    {
                        GUIStyle style = new GUIStyle(GUI.skin.button);
                        style.onActive.background = style.onFocused.background = style.onHover.background
                            = style.normal.background = selectedTexture;
                        style.margin = new RectOffset(0, 0, 0, 0);
                        buttonReturn = GUI.Button(new Rect(Mathf.Round(frames[i].time * timeLineScaler + 0.5f + 6), 20, 8, 32), "", style);
                    }
                    else
                    {
                        buttonReturn = GUI.Button(new Rect(Mathf.Round(frames[i].time * timeLineScaler + 0.5f + 6), 20, 8, 32), "");
                    }
                    if (buttonReturn)
                    {
                        selectedFrameID = i;
                        timeSelectorTime = frames[i].time;
                    }
                }
            }
            GUILayout.EndScrollView();
            //参数界面
            GUILayout.BeginHorizontal();
            if (selectedFrameID >= 0 && selectedFrameID < frames.Count)
            {
                GUILayout.BeginVertical();
                shiftSliderValue = AdjustingHorizontalSlider(shiftSliderValue, 3, "移动帧");
                GUILayout.Label("nearClip:" + frames[selectedFrameID].nearClip);
                GUILayout.Label("farClip:" + frames[selectedFrameID].farClip);
                GUILayout.EndVertical();

                GUILayout.BeginVertical();
                fovSliderValue = AdjustingHorizontalSlider(fovSliderValue, -3, "视角");
                nearClipSliderValue = AdjustingHorizontalSlider(nearClipSliderValue, 3, "nearClip");
                farClipSliderValue = AdjustingHorizontalSlider(farClipSliderValue, 3, "farClip");
                GUILayout.EndVertical();
                GUILayout.BeginVertical();
                translateX = AdjustingHorizontalSlider(translateX, 5, "左右");
                translateY = AdjustingHorizontalSlider(translateY, 5, "上下");
                translateZ = AdjustingHorizontalSlider(translateZ, 5, "前后");
                GUILayout.EndVertical();
                GUILayout.BeginVertical();
                rotateX = AdjustingHorizontalSlider(rotateX, -2, "俯仰");
                rotateY = AdjustingHorizontalSlider(rotateY, 2, "偏航");
                rotateZ = AdjustingHorizontalSlider(rotateZ, -2, "滚转");
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUI.DragWindow();
        }
        private float AdjustingHorizontalSlider(float value, float range)
        {
            float ret = GUILayout.HorizontalSlider(value, -range, range, GUILayout.Width(120));
            if (!Input.GetMouseButton(0)) ret = 0;
            return ret;
        }
        private float AdjustingHorizontalSlider(float value, float range, string content)
        {
            GUILayout.BeginHorizontal();
            value = AdjustingHorizontalSlider(value, range);
            GUILayout.Label(content);
            GUILayout.EndHorizontal();
            return value;
        }
        public ReCamKey GetCameraKeyAt(float time)
        {
            if (frames.Count == 0) return null;//无帧可插值
            if (frames.Count == 1) return new ReCamKey(frames[0]);
            int frameID = 0;
            while (frameID < frames.Count && time > frames[frameID].time)
            {
                frameID++;
            }
            if (frameID == 0) return new ReCamKey(frames[0]);
            if (frameID >= frames.Count) return new ReCamKey(frames[frames.Count - 1]);
            //找到frameID
            //重新算节点导数
            frames[frameID].GenerateDerivatives(frames[frameID - 1], frameID + 1 < frames.Count ? frames[frameID + 1] : frames[frameID]);
            frames[frameID - 1].GenerateDerivatives(frameID - 2 > 0 ? frames[frameID - 2] : frames[frameID - 1], frames[frameID]);

            float t = (time - frames[frameID - 1].time) / (frames[frameID].time - frames[frameID - 1].time);
            var lerppedKey = ReCamKey.Lerp(frames[frameID - 1], frames[frameID], t);
            return lerppedKey;
        }
        public void SetCameraPosRot(float time)
        {
            var lerppedKey = GetCameraKeyAt(time);
            Transform transform = GameObject.Find("Main Camera").transform;
            transform.position = lerppedKey.posRot.position; transform.rotation = lerppedKey.posRot.rotation;
            var camera = GameObject.Find("Main Camera").GetComponent<Camera>();
            //ModConsole.Log(lerppedKey.fov.value.ToString());
            camera.fieldOfView = lerppedKey.fov.value;
            camera.farClipPlane = lerppedKey.farClip;
            camera.nearClipPlane = lerppedKey.nearClip;
        }
        void Update()
        {
            if (ShouldShowGUI())
            {
                //缩放时间线
                if (scaleSliderValue != 0)
                {
                    ScaleTimeLine(scaleSliderValue);
                }
                //移动帧
                if (shiftSliderValue != 0)
                {
                    ShiftFrame(shiftSliderValue);
                }
                //非物理播放摄像机
                if (buildReplayCamera)
                {
                    SetCameraPosRot(timeSelectorTime);
                }
                //移动调整相机
                if ((translateX != 0 || translateY != 0 || translateZ != 0))
                {
                    if (selectedFrameID < 0 || selectedFrameID >= frames.Count) return;
                    Vector3 offset = new Vector3(ComputeAdjustingValueFromExp(translateX), ComputeAdjustingValueFromExp(translateY),
                        ComputeAdjustingValueFromExp(translateZ)) * Time.deltaTime;
                    frames[selectedFrameID].posRot.position += frames[selectedFrameID].posRot.rotation * offset;
                }
                //旋转调整相机
                if ((rotateX != 0 || rotateY != 0 || rotateZ != 0))
                {
                    if (selectedFrameID < 0 || selectedFrameID >= frames.Count) return;
                    Vector3 r = new Vector3(ComputeAdjustingValueFromExp(rotateX), ComputeAdjustingValueFromExp(rotateY),
                        ComputeAdjustingValueFromExp(rotateZ)) * Time.deltaTime;
                    if (r.sqrMagnitude == 0) return;
                    Quaternion q = Quaternion.AngleAxis((float)(r.magnitude * 180 / Math.PI), r.normalized);
                    frames[selectedFrameID].posRot.rotation = frames[selectedFrameID].posRot.rotation * q;
                }
                //调整fov
                if (fovSliderValue != 0)
                {
                    if (selectedFrameID < 0 || selectedFrameID >= frames.Count) return;
                    frames[selectedFrameID].fov.value *= Mathf.Exp(fovSliderValue * Time.deltaTime);
                    if (frames[selectedFrameID].fov.value > 179.9f) frames[selectedFrameID].fov.value = 179.9f;
                }
                //nearClip
                if (nearClipSliderValue != 0)
                {
                    if (selectedFrameID < 0 || selectedFrameID >= frames.Count) return;
                    frames[selectedFrameID].nearClip *= Mathf.Exp(nearClipSliderValue * Time.deltaTime);
                }
                //farClip
                if (farClipSliderValue != 0)
                {
                    if (selectedFrameID < 0 || selectedFrameID >= frames.Count) return;
                    frames[selectedFrameID].farClip *= Mathf.Exp(farClipSliderValue * Time.deltaTime);
                }
            }
        }
        public float ComputeAdjustingValueFromExp(float e)
        {
            if (e > 0) return Mathf.Exp(e) - 1;
            return -Mathf.Exp(-e) + 1;
        }
        public void ShiftFrame(float sliderValue)
        {
            if (selectedFrameID < 0 || selectedFrameID >= frames.Count) return;
            bool forward = (sliderValue > 0);
            float velocity = Mathf.Exp(Mathf.Abs(sliderValue)) - 1;
            frames[selectedFrameID].time += forward ? velocity * Time.deltaTime : -velocity * Time.deltaTime;
            if (frames[selectedFrameID].time < 0) frames[selectedFrameID].time = 0;
            if (frames[selectedFrameID].time > maxFrameTime) maxFrameTime = frames[selectedFrameID].time;
            timeSelectorTime = frames[selectedFrameID].time;
        }
        public void ScaleTimeLine(float sliderValue)
        {
            float fixedTime = (timeLimeScrollX + timeLineWidth / 2) / timeLineScaler;
            timeLineScaler *= Mathf.Exp(sliderValue * Time.deltaTime);
            timeLimeScrollX = Mathf.RoundToInt(fixedTime * timeLineScaler) - timeLineWidth / 2;
            if (timeLineScaler * maxFrameTime + 16 < timeLineWidth)
            {
                timeLineScaler = (timeLineWidth - 16) / maxFrameTime;
            }
        }
        public void RemoveFrame()
        {
            if (selectedFrameID < 0 || selectedFrameID >= frames.Count) return;
            frames.RemoveAt(selectedFrameID);
            selectedFrameID = -1;
        }
        public void KeyFrame()
        {
            selectedFrameID = -1;
            GameObject cameraObject = GameObject.Find("Main Camera");
            ReCamKey newkey = new ReCamKey();
            newkey.time = timeSelectorTime;
            newkey.posRot.position = cameraObject.transform.position;
            newkey.posRot.rotation = cameraObject.transform.rotation;
            newkey.fov.value = cameraObject.GetComponent<Camera>().fieldOfView;
            newkey.farClip = cameraObject.GetComponent<Camera>().farClipPlane;
            newkey.nearClip = cameraObject.GetComponent<Camera>().nearClipPlane;

            for (int i = 0; i < frames.Count; i++)
            {
                if (frames[i].time - newkey.time < 0.0001f && -0.0001f < frames[i].time - newkey.time)
                {
                    frames[i] = newkey; return;
                }
                if (newkey.time < frames[i].time)
                {
                    frames.Insert(i, newkey);
                    return;
                }
            }
            frames.Add(newkey);
            //PlayerMachine.GetLocal().MachineData.ReadString("lto_CameraPosQuaData");
        }
        public override string InitialWindowName()
        {
            return "ReCamTool";
        }
        public override Rect InitialWindowRect()
        {
            return new Rect(300, 800, 100, 50);
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();
            Events.OnMachineSimulationToggle -= MySimulationToggle;
            Events.OnMachineLoaded -= LoadCameraData;
            Events.OnMachineLoaded -= StopReplayOnLoad;
            Events.OnMachineSave -= SaveCameraData;
            Events.OnMachineDestroyed -= MachineDestroyed;
        }
    }
}
