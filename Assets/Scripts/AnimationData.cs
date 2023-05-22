using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using LitJsons;


[CreateAssetMenu(fileName ="New Animation Data" , menuName = "Animation Data")]

[System.Serializable]
public class AnimationData : ScriptableObject
{

    //[Serializable]
    public List<BoneData> AllBoneData;

    public AnimationData()
    {
        AllBoneData = new List<BoneData>();
    }

    
    /*
    //LitJsons的序列化与反序列化
    public void SaveToJSON(string path)
    {
        Debug.LogFormat("Saving game settings to {0}", path);
        //string jsonStr = JsonUtility.ToJson(AllBoneData, true);
        System.IO.File.WriteAllText(path, JsonMapper.ToJson(this));
        //System.IO.File.WriteAllText(path, JsonConvert.SerializeObject(this));
    }

    public AnimationData LoadJSON(string path)
    {
        AnimationData instance = JsonMapper.ToObject<AnimationData>(System.IO.File.ReadAllText(path));

        return instance;
    }*/

    

}

[System.Serializable]
public class BoneData /*: ScriptableObject*/
{
    public string BoneName;//骨骼名字
    public float BoneLength;//骨骼长度
    public Vector3 IntinialPosition;//TPose的Position
    public Quaternion IntinialRotation;//TPose的Rotation
    public Vector3 IntinialScale;//TPose的缩放
    public List<BoneFrameData> Frame;

    public BoneData()
    {
        Frame = new List<BoneFrameData>();
    }

}

[System.Serializable]
public class BoneFrameData /*: ScriptableObject*/
{
    public int frameNumber;//帧号
    public Vector3 LocalPosition;
    public Quaternion LocalRotation;
    public Vector3 LocalScale;

    public Vector3 RootPosition;//相对于Root的Position
    public Quaternion RootRotation;//相当于Root的Rotation
    //public Vector3 RootScale;

    //PositionOffset = thisframe - last frame
    public Vector3 PositionOffset;//相当于上一帧的位移(暂时没用)
}





