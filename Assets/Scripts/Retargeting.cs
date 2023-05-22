using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class Retargeting : MonoBehaviour
{
            

    [System.Serializable]
    public class MyretargetSetting
    {
        public Transform resourceBone;
        public Transform targetBone;
        public retargetmode mode;
        public enum retargetmode
        {
            Animation ,
            AnimationScaled ,
            AnimationRelative ,
            Skeleton ,
            OrientAndScale
        };
    }    

    [Header("源物体 Animator")]
    public GameObject ResourceObj;
    [Header("源骨骼")]
    public Transform ResRoot;
    public Transform ResHips;
    public Transform ResPelvis;
    public Transform ResSpine;
    public Transform ResLeftThigh;
    public Transform ResLeftCalf;
    public Transform ResLeftFoot;
    public Transform ResRightThigh;
    public Transform ResRightCalf;
    public Transform ResRightFoot;

    private List<Transform> ResBone = new List<Transform>();

    private List<Vector3> ResBoneInitialPosition = new List<Vector3>();
    private List<Quaternion> ResBoneInitialRotation = new List<Quaternion>();


    [Header("目标物体 Animator")]
    public GameObject TargetObj;
    [Header("目标骨骼")]
    public Transform Root;
    public Transform Hips;
    public Transform Pelvis;
    public Transform Spine;
    public Transform LeftThigh;
    public Transform LeftCalf;
    public Transform LeftFoot;
    public Transform RightThigh;
    public Transform RightCalf;
    public Transform RightFoot;

    /*[Header("标记 Locator")]
    public Transform Left_Foot_Locator;*/

    private List<Transform> TargetBone = new List<Transform>();

    private List<Vector3> TargetBoneInitialPosition = new List<Vector3>();
    private List<Quaternion> TargetBoneInitialRotation = new List<Quaternion>();
           

    

    private void OnEnable()
    {

        

    }

    // Start is called before the first frame update
    void Start()
    {

        ResBone = new List<Transform> { ResRoot, ResHips, ResPelvis, ResSpine, ResLeftThigh, ResLeftCalf, ResLeftFoot, ResRightThigh, ResRightCalf, ResRightFoot };
        TargetBone = new List<Transform> { Root, Hips, Pelvis, Spine, LeftThigh, LeftCalf, LeftFoot, RightThigh, RightCalf, RightFoot };


        for (int i = 0; i < TargetBone.Count; i++)
        {

            //TPose骨骼的初始世界Position
            //ResBoneInitialPosition.Add(ResBone[i].transform.position);
            //TargetBoneInitialPosition.Add(TargetBone[i].transform.position);
            if (TargetBone[i] != null)
            {
                //TPose骨骼在Root坐标系下的初始Position
                ResBoneInitialPosition.Add(ResourceObj.transform.worldToLocalMatrix.MultiplyPoint3x4(ResBone[i].transform.position));
                TargetBoneInitialPosition.Add(TargetObj.transform.worldToLocalMatrix.MultiplyPoint3x4(TargetBone[i].transform.position));
                //TPose骨骼在Root坐标系下的初始rotation
                ResBoneInitialRotation.Add(Quaternion.Inverse(ResourceObj.transform.rotation) * ResBone[i].transform.rotation);
                TargetBoneInitialRotation.Add(Quaternion.Inverse(TargetObj.transform.rotation) * TargetBone[i].transform.rotation);
            }
            else
            {
                //TPose骨骼在Root坐标系下的初始Position
                ResBoneInitialPosition.Add(ResourceObj.transform.worldToLocalMatrix.MultiplyPoint3x4(ResBone[i].transform.position));
                TargetBoneInitialPosition.Add(default);
                //TPose骨骼在Root坐标系下的初始rotation
                ResBoneInitialRotation.Add(Quaternion.Inverse(ResourceObj.transform.rotation) * ResBone[i].transform.rotation);
                TargetBoneInitialRotation.Add(default);

            }

                        
        }


    }

    // Update is called once per frame
    void Update()
    {

        //Debug.Log(TargetBoneInitialRotation[1]);

    }

    private void LateUpdate()
    {
        for (int i = 0; i < ResBone.Count; i++)
        {

            if (TargetBone[i] == null)
                continue;

            //Debug.Log(i);

            //在对应的物体坐标系下，骨骼位移的变化量是相等的
            //（源骨骼TPose的position - 源骨骼当前世界坐标position） * 源骨骼世界坐标转Root坐标系 = （目标骨骼TPose的position - 目标骨骼当前世界坐标position）* 目标骨骼世界坐标转Root坐标系
            //目标骨骼当前世界坐标position = 目标骨骼世界坐标转Root坐标系 * 源骨骼世界坐标转Root坐标系的逆 *（源骨骼当前世界坐标position - 源骨骼TPose的position）+ 目标骨骼TPose的position
            //TargetBone[i].transform.position = TargetObj.transform.rotation * Quaternion.Inverse(ResourceObj.transform.rotation) * (ResBone[i].transform.position - ResBoneInitialPosition[i]) + TargetBoneInitialPosition[i];

            //源骨骼在Root坐标系下的position
            Vector3 ResPosition = ResourceObj.transform.worldToLocalMatrix.MultiplyPoint3x4(ResBone[i].transform.position);
            //源骨骼在Root坐标系下相对位移
            Vector3 ResPositionChange = ResPosition - ResBoneInitialPosition[i];
            //目标骨骼在Root坐标系下的position
            Vector3 TargetPosition = TargetBoneInitialPosition[i] + ResPositionChange;
            //目标骨骼在世界坐标系下的position
            TargetBone[i].transform.position = TargetObj.transform.localToWorldMatrix.MultiplyPoint3x4(TargetPosition);


            //在对应的物体坐标系下，骨骼角度的变化量是相等的
            //源骨骼TPose的rotation * 源骨骼当前世界坐标rotation * 源骨骼世界坐标转Root坐标系 = 目标骨骼TPose的rotation * 目标骨骼当前世界坐标rotation * 目标骨骼世界坐标转Root坐标系
            //TargetBone[i].transform.rotation = TargetObj.transform.rotation * Quaternion.Inverse(ResourceObj.transform.rotation) * (ResBone[i].transform.rotation * (Quaternion.Inverse(ResBoneInitialRotation[i]) * ResourceObj.transform.rotation)) * TargetBoneInitialRotation[i];

            //TargetBone[i].transform.rotation = Quaternion.Inverse(ResourceObj.transform.rotation) * Quaternion.Inverse(ResBoneInitialRotation[i]) * ResBone[i].transform.rotation * TargetObj.transform.rotation * TargetBoneInitialRotation[i];

            //**坐标系转换是左乘一个四元数，旋转的变化量是右乘一个四元数!!!
            //源骨骼在Root坐标系下的rotation
            Quaternion ResRotation = Quaternion.Inverse(ResourceObj.transform.rotation) * ResBone[i].transform.rotation ;
            //源骨骼在Root坐标系下相对旋转
            Quaternion ResRotationChange = ResRotation * Quaternion.Inverse(ResBoneInitialRotation[i]);
            //目标骨骼在Root坐标系下的rotation
            Quaternion TargetRotation = ResRotationChange * TargetBoneInitialRotation[i];
            //目标骨骼在世界坐标系下的rotation
            TargetBone[i].transform.rotation = TargetObj.transform.rotation * TargetRotation;
            

            
        }


        
    }
}
