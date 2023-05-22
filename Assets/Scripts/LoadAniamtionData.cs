using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class LoadAniamtionData : MonoBehaviour
{
    
    [HideInInspector]
    public Transform RootBone;//根骨骼
    [HideInInspector]
    public AnimationData ImportAnimationData;//动画数据
    [HideInInspector]
    public enumRetargetType GlobalType;//根骨骼

    [HideInInspector]
    public bool isbonebake = false;//是否读取了骨骼信息
    [HideInInspector]
    public bool isanidatabake = false;//是否读取了动画数据
    [HideInInspector]
    public bool isbakeclip = false;//是否烘培动画数据
    [HideInInspector]
    public string[] bonedatalist;//动画数据骨骼列表
    [HideInInspector]    
    public int animationframenumber = 0;//动画帧数
    [HideInInspector]
    public int animationframerate = 30;//动画帧率


    private float playtime = 0.0f;

    [HideInInspector]
    public enum enumRetargetType
    {
        None,//不做任何处理

        Animation,//完全复制源动画数据

        AnimationScaled,//在Animation的基础上，根据骨骼长度比进行骨骼缩放

        //AnimationRelative//在Animation基础上应用了目标参考姿势和源参考姿势之间的差值

        //Skeleton,//使用动画的旋转和参考姿势的骨骼在父空间的位置

        //OrientAndScale//Animation之后把骨架中的骨骼旋转“扭”到基于参考姿势来说合理的姿势上，并根据骨骼长度比进行骨骼缩放

        Skeleton, //根据源动画数据与源Tpose的差值 应用到目标的Tpose上

        SkeletonRotationOnly,  //只修改旋转，不修改位移数据

        SkeletonScaled //在Skeleton的基础上，根据骨骼长度比进行骨骼缩放
    }

    //一个需要重定向的单元，包含骨骼 骨骼长度 源数据序号 重定向类型 
    [System.Serializable]
    public class RetargetMeta
    {
        
        public Transform RetargetBone;
        public float RetargetBoneLength;
        public int DataIndex = 0;
        public enumRetargetType Type;

    }

    [HideInInspector]
    [SerializeField]
    public List<RetargetMeta> AllMeta = new List<RetargetMeta>();//全部重定向list
    
    //记录目标骨骼TPose的Position 和 Ratation
    private List<Vector3> TargetBoneInitialPosition = new List<Vector3>();
    private List<Quaternion> TargetBoneInitialRotation = new List<Quaternion>();
    

    // create a new AnimationClip
    private AnimationClip bakeclip;

    static private string[] propertyname = { "localPosition.x" , "localPosition.y", "localPosition.z",
        "localRotation.x", "localRotation.y", "localRotation.z","localRotation.w", "localScale.x", "localScale.y", "localScale.z" };
    
    //每根动画曲线需要的数据结构
    private class SetCurveData
    {
        public string path;//骨骼结构路径
        public metacurve[] keys;//曲线数据数组

        public SetCurveData(int index)
        {
            keys = new metacurve[10];

            for(var i = 0; i < keys.Length; i++)
            {
                metacurve meta = new metacurve(index);
                meta.name = propertyname[i];
                keys[i] = meta;
            }
            
                        
        }              

    }

    private class metacurve
    {
        public string name;//曲线对应属性名称
        public Keyframe[] key;//曲线数据

        public metacurve(int index)
        {
             key = new Keyframe[index];
        }
    }
    
    private List<SetCurveData> AllCurveData;   


    private void OnEnable()
    {
        isbakeclip = false;
    }


    // Start is called before the first frame update
    void Start()
    {
        if (isbonebake)        
        {
            for (int i = 0; i < AllMeta.Count; i++)
            {
                //保存目标骨骼的TPose的Position Rotation信息
                TargetBoneInitialPosition.Add(transform.worldToLocalMatrix.MultiplyPoint3x4(AllMeta[i].RetargetBone.transform.position));
                TargetBoneInitialRotation.Add(Quaternion.Inverse(transform.rotation) * AllMeta[i].RetargetBone.transform.rotation);
                                
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        playtime += Time.deltaTime * animationframerate;

        if (playtime > ((float)animationframenumber - 1))
        {
            playtime = playtime - (float)animationframenumber + 1;
        }


        if (isbonebake && isanidatabake)
        {
            SetAnimationData(playtime);

            
            
        }

        if (isbakeclip)
        {
            BakeAnimationClip();
        }
                
    }

    public void SetAnimationData(float loadtime)
    {
        //animationframenumber = ImportAnimationData.AllBoneData[0].Frame.Count;

        for (int i = 0; i < AllMeta.Count; i++)
        {
            int currentdframe = (int)Mathf.Floor(loadtime);
            float bias = loadtime - Mathf.Floor(loadtime);

            if (loadtime == (animationframenumber-1))
            {
                currentdframe = animationframenumber - 2;
                bias = 1.0f;
            }

            int index = AllMeta[i].DataIndex;

            switch (AllMeta[i].Type)
            {
                case enumRetargetType.None:
                    break;

                case enumRetargetType.Animation:
                    
                    AllMeta[i].RetargetBone.localPosition = Vector3.Lerp(ImportAnimationData.AllBoneData[index].Frame[currentdframe].LocalPosition, 
                        ImportAnimationData.AllBoneData[index].Frame[currentdframe + 1].LocalPosition, bias);
                    AllMeta[i].RetargetBone.localRotation = Quaternion.Lerp(ImportAnimationData.AllBoneData[index].Frame[currentdframe].LocalRotation, 
                        ImportAnimationData.AllBoneData[index].Frame[currentdframe + 1].LocalRotation, bias);
                    break;

                case enumRetargetType.AnimationScaled:

                    //position基于骨骼长度比例
                    float sourcebonelength = ImportAnimationData.AllBoneData[index].BoneLength;
                    if (sourcebonelength < 0.001f)
                        sourcebonelength = 1.0f;

                    AllMeta[i].RetargetBone.localPosition = Vector3.Lerp(ImportAnimationData.AllBoneData[index].Frame[currentdframe].LocalPosition,
                        ImportAnimationData.AllBoneData[index].Frame[currentdframe + 1].LocalPosition, bias)
                        / sourcebonelength * AllMeta[i].RetargetBoneLength;
                    AllMeta[i].RetargetBone.localRotation = Quaternion.Lerp(ImportAnimationData.AllBoneData[index].Frame[currentdframe].LocalRotation,
                        ImportAnimationData.AllBoneData[index].Frame[currentdframe + 1].LocalRotation, bias);

                    break;

                case enumRetargetType.Skeleton:

                    //源骨骼在Root坐标系下的position
                    Vector3 ResPosition = Vector3.Lerp(ImportAnimationData.AllBoneData[index].Frame[currentdframe].RootPosition, 
                        ImportAnimationData.AllBoneData[index].Frame[currentdframe + 1].RootPosition, bias);
                    //源骨骼在Root坐标系下相对位移
                    Vector3 ResPositionChange = ResPosition - ImportAnimationData.AllBoneData[index].IntinialPosition;
                    //目标骨骼在Root坐标系下的position
                    Vector3 TargetPosition = TargetBoneInitialPosition[i] + ResPositionChange;
                    //目标骨骼在世界坐标系下的position
                    AllMeta[i].RetargetBone.position = transform.localToWorldMatrix.MultiplyPoint3x4(TargetPosition);

                    //源骨骼在Root坐标系下的rotation                    
                    Quaternion ResRotation = Quaternion.Lerp(ImportAnimationData.AllBoneData[index].Frame[currentdframe].RootRotation, 
                        ImportAnimationData.AllBoneData[index].Frame[currentdframe + 1].RootRotation, bias);
                    //源骨骼在Root坐标系下相对旋转
                    Quaternion ResRotationChange = ResRotation * Quaternion.Inverse(ImportAnimationData.AllBoneData[index].IntinialRotation);
                    //目标骨骼在Root坐标系下的rotation
                    Quaternion TargetRotation = ResRotationChange * TargetBoneInitialRotation[i];
                    //目标骨骼在世界坐标系下的rotation
                    AllMeta[i].RetargetBone.rotation = transform.rotation * TargetRotation;                   

                    break;

                case enumRetargetType.SkeletonRotationOnly:                    

                    //源骨骼在Root坐标系下的rotation                    
                    Quaternion ResRotationRotationOnly = Quaternion.Lerp(ImportAnimationData.AllBoneData[index].Frame[currentdframe].RootRotation,
                        ImportAnimationData.AllBoneData[index].Frame[currentdframe + 1].RootRotation, bias);
                    //源骨骼在Root坐标系下相对旋转
                    Quaternion ResRotationChangeRotationOnly = ResRotationRotationOnly * Quaternion.Inverse(ImportAnimationData.AllBoneData[index].IntinialRotation);
                    //目标骨骼在Root坐标系下的rotation
                    Quaternion TargetRotationRotationOnly = ResRotationChangeRotationOnly * TargetBoneInitialRotation[i];
                    //目标骨骼在世界坐标系下的rotation
                    AllMeta[i].RetargetBone.rotation = transform.rotation * TargetRotationRotationOnly;

                    break;

                case enumRetargetType.SkeletonScaled:


                    break;
            }

        }
    }

        

    //构造动画曲线数据
    private void BakeAnimationClip()
    {
        if (isbonebake)
        {
            
            bakeclip = new AnimationClip();
            AllCurveData = new List<SetCurveData>();

            for (int i = 0; i < AllMeta.Count; i++)
            {
                SetCurveData tempSetCurveData = new SetCurveData(animationframenumber);
                tempSetCurveData.path = GetObjectPath(AllMeta[i].RetargetBone,this.transform);
                AllCurveData.Add(tempSetCurveData);
            }

            for (int i = 0; i< animationframenumber;i++)
            {
                SetAnimationData((float)i);

                KeyAllCurve(i);
            }

            for (int i = 0; i < AllMeta.Count; i++)
            {
                
                foreach (metacurve everymetacurve in AllCurveData[i].keys)
                {
                    AnimationCurve tempcurve = new AnimationCurve(everymetacurve.key);
                    bakeclip.SetCurve(AllCurveData[i].path, typeof(Transform), everymetacurve.name, tempcurve);
                }
                
            }

            //Debug.Log(Application.dataPath + "/Data/" + "test.anim");

            AssetDatabase.CreateAsset(bakeclip, /*Application.dataPath +*/ "Assets/Data/" + "test.anim");
            
        }

        isbakeclip = false;

    }

    //获取物体在场景大纲中的路径
    private string GetObjectPath(Transform bone, Transform root)
    {
                
        if ((bone.parent == null) || (bone.parent == root))
        {
            return bone.name;
        }

        return GetObjectPath(bone.parent,root) + "/" + bone.name;
    }

    private void KeyAllCurve(int time)
    {
        for (int i = 0; i < AllMeta.Count; i++)
        {
            Vector3 localpos = AllMeta[i].RetargetBone.localPosition;
            float frametime = (float)time / bakeclip.frameRate/*60.0f*/;

            AllCurveData[i].keys[0].key[time] = new Keyframe(frametime, localpos.x);
            AllCurveData[i].keys[1].key[time] = new Keyframe(frametime, localpos.y);
            AllCurveData[i].keys[2].key[time] = new Keyframe(frametime, localpos.z);


            /*
            Vector3 localrot = AllMeta[i].RetargetBone.localRotation.eulerAngles;     
            AllCurveData[i].keys[3].key[time] = new Keyframe(frametime, localrot.x);  //"localEulerAnglesRaw.x"
            AllCurveData[i].keys[4].key[time] = new Keyframe(frametime, localrot.y);  //"localEulerAnglesRaw.y"
            AllCurveData[i].keys[5].key[time] = new Keyframe(frametime, localrot.z);  //"localEulerAnglesRaw.z"
            */

            Quaternion localrot = AllMeta[i].RetargetBone.localRotation;
            AllCurveData[i].keys[3].key[time] = new Keyframe(frametime, localrot.x);  //"localRotation.x"
            AllCurveData[i].keys[4].key[time] = new Keyframe(frametime, localrot.y);  //"localRotation.y"
            AllCurveData[i].keys[5].key[time] = new Keyframe(frametime, localrot.z);  //"localRotation.z"
            AllCurveData[i].keys[6].key[time] = new Keyframe(frametime, localrot.w);  //"localRotation.w"
                       
                        
            Vector3 localscale = AllMeta[i].RetargetBone.localScale;
            AllCurveData[i].keys[7].key[time] = new Keyframe(frametime, localscale.x);
            AllCurveData[i].keys[8].key[time] = new Keyframe(frametime, localscale.y);
            AllCurveData[i].keys[9].key[time] = new Keyframe(frametime, localscale.z);
            
        }
    }
}
