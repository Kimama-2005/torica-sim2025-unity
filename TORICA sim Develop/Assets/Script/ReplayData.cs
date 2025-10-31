using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// MonoBehaviour を継承しない、ただのデータ格納用クラス（または構造体）
[System.Serializable]

public struct ReplayData 
{
    public float time;       // 記録開始からの時間
    public Vector3 position;   // その時点での位置
    public Quaternion rotation; // その時点での回転

    // データを簡単に追加するための「コンストラクタ」
    public ReplayData(float t, Vector3 pos, Quaternion rot)
    {
        time = t;
        position = pos;
        rotation = rot;
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
