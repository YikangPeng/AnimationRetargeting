using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(LoadAniamtionData))]
public class LoadAniamtionDataUI : Editor
{
    private bool isbonebake;

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        LoadAniamtionData script = target as LoadAniamtionData;
                

        script.RootBone = (Transform)EditorGUILayout.ObjectField("根骨骼", script.RootBone, typeof(Transform), true);
        script.ImportAnimationData = (AnimationData)EditorGUILayout.ObjectField("动画数据", script.ImportAnimationData, typeof(AnimationData), true);
        script.animationframerate = EditorGUILayout.IntField("动画帧率", script.animationframerate);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Load Bone Information") && (script.RootBone !=null))
        {
            LoadAllBone(script.RootBone, script.AllMeta);
            script.isbonebake = true;
        }
        if (GUILayout.Button("Load Animation Data") && (script.ImportAnimationData!=null))
        {
            script.bonedatalist = LoadBoneData(script.ImportAnimationData);
            script.animationframenumber = script.ImportAnimationData.AllBoneData[0].Frame.Count;

            if (script.isbonebake)
            {
                for (int i = 0; i < script.AllMeta.Count; i++)
                {
                    for (int j = 0; j < script.bonedatalist.Length;j++)
                    {
                        if (script.AllMeta[i].RetargetBone.name == script.bonedatalist[j])
                            script.AllMeta[i].DataIndex = j;
                    }
                }
            }

            script.isanidatabake = true;
        }
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Bake Animation Clip"))
        {
            script.isbakeclip = true;
        }

        EditorGUILayout.BeginHorizontal();
        script.GlobalType = (LoadAniamtionData.enumRetargetType)EditorGUILayout.EnumPopup("全局重定向类型", script.GlobalType);
        if (GUILayout.Button("Apply"))
        {
            if (script.isbonebake)
            {
                for (int i = 0; i < script.AllMeta.Count; i++)
                {
                    script.AllMeta[i].Type = script.GlobalType;
                }
            }
            
            
        }
        
        EditorGUILayout.EndHorizontal();


        if (script.isbonebake)
        {

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("目标" + script.AllMeta.Count);
            EditorGUILayout.LabelField("源骨骼");
            EditorGUILayout.LabelField("类型");            
            EditorGUILayout.EndHorizontal();

            for (int i =0; i< script.AllMeta.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.LabelField(script.AllMeta[i].RetargetBone.name);
                if (script.isanidatabake)
                {
                    script.AllMeta[i].DataIndex = EditorGUILayout.Popup(script.AllMeta[i].DataIndex, script.bonedatalist);
                }
                else
                {
                    EditorGUILayout.LabelField("没有动画数据");
                }
                
                script.AllMeta[i].Type = (LoadAniamtionData.enumRetargetType)EditorGUILayout.EnumPopup(script.AllMeta[i].Type);

                EditorGUILayout.EndHorizontal();
            }

                        
        }
                
        Repaint();

    }

    //获取所有骨骼
    private void LoadAllBone(Transform root, List<LoadAniamtionData.RetargetMeta> list)
    {
        list.Clear();

        //list.Add(root);
        if (root.childCount > 0)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>())
            {
                LoadAniamtionData.RetargetMeta temp = new LoadAniamtionData.RetargetMeta();
                temp.RetargetBone = child;
                temp.RetargetBoneLength = Vector3.Distance(child.position, child.parent.position);                
                list.Add(temp);
            }
            list[0].RetargetBoneLength = 1.0f;
        }
    }

    private string[] LoadBoneData(AnimationData data)
    {
        string[] list = new string[data.AllBoneData.Count];


        for (int i = 0; i < data.AllBoneData.Count; i++)
        {
            list[i] = data.AllBoneData[i].BoneName;
        }

        return list;
    }

}
