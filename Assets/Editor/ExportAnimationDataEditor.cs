using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections;
using System.Collections.Generic;

public class ExportAnimationDataEditor : EditorWindow
{

    //所有动画列表
    private List<AnimatorState> allStates = new List<AnimatorState>();
    private string[] aniLieBiaoString;//存放动画名字数组

    //是否处于选择并播放动画的状态
    protected bool lockSelection = false;//是否选定了动画并播放
    private bool hongPei = false;//是否初始化装载动画
    private bool playtime = false;//是否自动播放动画

    private GameObject aniamtionObject;
    private Animator animator;
    private AnimatorController animatorController;

    private int aniLieBiaoID = 0;//当前选择的动画ID号
    private AnimatorState aniState;//当前选择的动画State
    protected float clipTime = 0.0f;//当前动画Clip的动画长度
    protected float frameRate = 0.0f;//当前动画Clip的帧率
    private int bakeFrameNumber;//当前动画的帧数
    private float bakeTime = 0;//烘培帧号

    private string aniZhuangTai = "";//根据是否选定了动画 显示 "开启动画/关闭动画"

    private string path = "";

    /// <summary>
	/// 时间增量
	/// </summary>
	private double delta;

    /// <summary>
    /// 当前运行时间
    /// </summary>
    private float m_RunningTime;

    /// <summary>
    /// 上一次系统时间
    /// </summary>
    private double m_PreviousTime;

    /// <summary>
	/// 最大时间长度	
	/// </summary>
	private float aniTime = 0.0f;//选定的动画时长
    private float RecordEndTime = 0;

    private bool isInspectorUpdate = false;

    private Transform RootBone;//根骨骼
    private List<Transform> AllBone = new List<Transform>();//临时储存全部骨骼list
    public AnimationData ExportAnimationData;//导出动画数据

    [MenuItem("Tool/Aniamtion/ExportAnimationData")]
    public static void Init()
    {

        ExportAnimationDataEditor window = (ExportAnimationDataEditor)GetWindow(typeof(ExportAnimationDataEditor));

                
    }
    
    private void OnSelectionChange()
    {
        if (!lockSelection)
        {
            aniamtionObject = Selection.activeGameObject;
            animator = null;
            if (aniamtionObject != null && aniamtionObject.GetComponent<Animator>() != null)
            {
                ChuShiHua();
                animator = aniamtionObject.GetComponent<Animator>();
                GetAllStateName();
                LieBiaoShuaXin();
                AnimaterHongPei(aniState);

            }
            Repaint();
        }
    }

    private void ChuShiHua()
    {
        lockSelection = false;
        allStates.Clear();
        aniLieBiaoString = null;
        aniLieBiaoID = 0;
        //clipTime = 0.0f;        
        playtime = false;
        path = Application.dataPath;

        ExportAnimationData = new AnimationData();
    }

    //获取当前物体动画机内所有Animaiton clip
    private void GetAllStateName()
    {
        if (animator != null)
        {
            var runAnimator = animator.runtimeAnimatorController;
            animatorController = runAnimator as AnimatorController;

            foreach (var layer in animatorController.layers)
            {
                GetAnimState(layer.stateMachine);
            }

        }
    }

    private void GetAnimState(AnimatorStateMachine ASM)
    {
        foreach (var s in ASM.states)
        {
            if (s.state.motion == null)
                continue;
            var clip = GetClip(s.state.motion.name);
            if (clip != null)
            {
                allStates.Add(s.state);
            }
        }

        foreach (var MS in ASM.stateMachines)
        {
            GetAnimState(MS.stateMachine);
        }
    }

    private AnimationClip GetClip(string name)
    {
        foreach (var clip in animatorController.animationClips)
        {
            if (clip.name.Equals(name))
                return clip;
        }

        return null;
    }

    private void LieBiaoShuaXin()
    {
        aniLieBiaoString = new string[allStates.Count];
        for (int i = 0; i < allStates.Count; i++)
        {
            aniLieBiaoString[i] = allStates[i].name;
        }

    }



    public void OnGUI()
    {
        
        if (aniamtionObject == null || animator == null)
        {
            EditorGUILayout.HelpBox("请选择一个带Animator的物体！", MessageType.Info);
            return;
        }

        RootBone = (Transform)EditorGUILayout.ObjectField("根骨骼", RootBone, typeof(Transform), true);

        var oldAniID = aniLieBiaoID;
        aniLieBiaoID = EditorGUILayout.Popup("动画列表", aniLieBiaoID, aniLieBiaoString);
        
        aniState = allStates[aniLieBiaoID];
        
        if (oldAniID != aniLieBiaoID)
        {
            hongPei = false;
            AnimaterHongPei(aniState);
        }

        EditorGUILayout.BeginHorizontal();

        EditorGUI.BeginChangeCheck();
        if (!lockSelection)
        {
            aniZhuangTai = "开启动画";
        }
        else
        {
            aniZhuangTai = "关闭动画";
        }
        GUILayout.Toggle(AnimationMode.InAnimationMode(), aniZhuangTai, EditorStyles.toolbarButton);
        if (EditorGUI.EndChangeCheck())
        {
            lockSelection = !lockSelection;
            if (!lockSelection)
            {
                GuanBi();
            }
            else
            {
                
                if (RootBone)
                {
                    GetAllBone(RootBone,AllBone);

                    ExportAnimationData.AllBoneData.Clear();
                    SaveTPoseData(AllBone, ExportAnimationData);
                }
                else
                {
                    EditorGUILayout.HelpBox("未选择根骨骼！", MessageType.Info);
                }

                AnimaterHongPei(aniState);

                path += "/Data/" + GetClip(aniState.motion.name).name + ".txt";

            }
        }
        GUILayout.FlexibleSpace();
        
        if (GUILayout.Button("Bake"))
        {
            if ((lockSelection) && (!playtime))
            {
                playtime = true;

                if (aniState != null)
                {
                    bakeFrameNumber = (int)Mathf.Floor(aniTime * frameRate + 0.3f);
                }

                bakeTime = 0;
                /*
                ExportAnimationData.AllBoneData.Clear();
                                
                if (RootBone)
                {
                    SaveTPoseData(AllBone, ExportAnimationData);
                }
                else
                {
                    EditorGUILayout.HelpBox("未选择根骨骼！", MessageType.Info);
                }*/
            }
            
            
        }

        /*
        if (GUILayout.Button("Stop"))
        {
            playtime = false;
        }
        */

        EditorGUILayout.EndHorizontal();

        if (aniState != null)
        {
            if (!playtime)
            {
                float startTime = 0.0f;
                float stopTime = RecordEndTime;
                clipTime = EditorGUILayout.Slider(clipTime, startTime, stopTime);
                var zhen = clipTime * 30;
                EditorGUILayout.LabelField("帧数：" + (int)zhen);
            }
        }

        path = GUILayout.TextField(path);

        Repaint();
    }


    //给动画机装载一个动画，播放一遍得到动画时长和帧数
    void AnimaterHongPei(AnimatorState state)
    {
        if (Application.isPlaying || state == null || !lockSelection)
        {
            return;
        }

        aniTime = 0.0f;

        aniTime = GetClip(state.motion.name).length;

        frameRate = GetClip(state.motion.name).frameRate;
        int frameCount = (int)((aniTime * frameRate) + 2);
        animator.StopPlayback();
        animator.Play(state.name);
        animator.recorderStartTime = 0;

        animator.StartRecording(frameCount);

        for (var j = 0; j < frameCount - 1; j++)
        {
            animator.Update(1.0f / frameRate);
        }

        animator.StopRecording();
        RecordEndTime = animator.recorderStopTime;
        animator.StartPlayback();
        hongPei = true;
    }

    //回到Tpose
    void GuanBi()
    {
        if (animator != null)
        {
            //m_RunningTime = 0;
            //animator.playbackTime = m_RunningTime;
            //animator.Update(0);

            path = Application.dataPath;

            if (RootBone)
            {
                foreach (Transform boneTrans in AllBone)
                {
                    PrefabUtility.RevertObjectOverride(boneTrans, InteractionMode.AutomatedAction);
                }
            }
            
            
        }
    }

    /// <summary>
    /// 运行时通过加载Update函数来更新
    /// </summary>
    void OnEnable()
    {
        m_PreviousTime = EditorApplication.timeSinceStartup;
        EditorApplication.update += inspectorUpdate;
        isInspectorUpdate = true;
    }

    void OnDestroy()
    {
        EditorApplication.update -= inspectorUpdate;
        isInspectorUpdate = false;
        GuanBi();
    }

    private void inspectorUpdate()
    {
        delta = EditorApplication.timeSinceStartup - m_PreviousTime;
        m_PreviousTime = EditorApplication.timeSinceStartup;

        if (!Application.isPlaying)
        {
            m_RunningTime = m_RunningTime + (float)delta;
            update();
        }
    }

    void update()
    {
        if (Application.isPlaying || !lockSelection)
        {
            return;
        }

        if (aniamtionObject == null)
            return;

        if (aniState == null)
            return;

        if (animator != null && animator.runtimeAnimatorController == null)
            return;

        if (!hongPei)
        {
            return;
        }

        if (playtime)
        {
            /*
            if (m_RunningTime <= aniTime)
            {
                animator.playbackTime = m_RunningTime;
                animator.Update(0);
            }
            if (m_RunningTime >= aniTime)
            {
                m_RunningTime = 0.0f;
            }*/

            animator.playbackTime = bakeTime / (float)bakeFrameNumber * aniTime;
            animator.Update(0);

            SaveBoneData(AllBone,ExportAnimationData);

            bakeTime += 1.0f;                       

            if (bakeTime> bakeFrameNumber)
            {

                //ExportAnimationData.SaveToJSON(path);
                
                string assetpath = path + ".asset";
                assetpath = assetpath.Substring(assetpath.IndexOf("Assets"));                
                AssetDatabase.DeleteAsset(assetpath);
                AssetDatabase.CreateAsset(ExportAnimationData, assetpath);
                //AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                
                playtime = false;
            }
        }
        else
        {
            m_RunningTime = clipTime;
            animator.playbackTime = m_RunningTime;
            animator.Update(0);
        }
    }

    //获取所有骨骼的List
    private void GetAllBone(Transform root, List<Transform> list)
    {
        list.Clear();

        //list.Add(root);
        if (root.childCount > 0)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>())
            {
                list.Add(child);
            }
        }
    }

    //保存TPose信息
    private void SaveTPoseData(List<Transform> list , AnimationData data)
    {
        foreach (Transform child in list)
        {
            
            BoneData tempBoneData = new BoneData();                       

            tempBoneData.BoneName = child.name;
            tempBoneData.IntinialPosition = aniamtionObject.transform.worldToLocalMatrix.MultiplyPoint3x4(child.transform.position);
            tempBoneData.IntinialRotation = Quaternion.Inverse(aniamtionObject.transform.rotation) * child.transform.rotation;
            tempBoneData.IntinialScale = child.transform.localScale;

            Transform parentbone = child.parent.GetComponent<Transform>();
            if (parentbone != null)
            {
                float bonelength = Vector3.Distance(parentbone.transform.position, child.transform.position);
                tempBoneData.BoneLength = bonelength;
            }

            data.AllBoneData.Add(tempBoneData);

        }
        data.AllBoneData[0].BoneLength = 1.0f;
    }

    private void SaveBoneData(List<Transform> list, AnimationData data)
    {
        for (int i = 0; i < data.AllBoneData.Count; i++)
        {

            BoneFrameData tempData = new BoneFrameData();

            //当前骨骼的帧号、LocalPosition、LocalRotation、Scale
            tempData.frameNumber = (int)bakeTime;
            tempData.LocalPosition = list[i].transform.localPosition;
            tempData.LocalRotation = list[i].transform.localRotation;
            tempData.LocalScale = list[i].transform.localScale;

            //当前骨骼相对于根骨骼坐标系的Position 和 Rotation
            tempData.RootPosition = aniamtionObject.transform.worldToLocalMatrix.MultiplyPoint3x4(list[i].transform.position);
            tempData.RootRotation = Quaternion.Inverse(aniamtionObject.transform.rotation) * list[i].transform.rotation;

            data.AllBoneData[i].Frame.Add(tempData);


        }
    }
}
