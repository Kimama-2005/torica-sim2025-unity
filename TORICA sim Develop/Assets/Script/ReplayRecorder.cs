using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReplayRecorder : MonoBehaviour
{
    private Transform planeTransform; // 監視対象（飛行機）のTransform
    private List<ReplayData> dataList; // MyGameManegerから受け取るリストの参照

    // AirDate.cs と同じロジックの変数
    private decimal frameNumber;
    [SerializeField] private decimal interval = 0.1m; // 記録間隔[s] (インスペクターで調整可能)
    private float timeSinceStart; // 記録開始からの経過時間

    void Start()
    {
        // 飛行機のTransformを取得
        planeTransform = MyGameManeger.instance.Plane.transform;

        // MyGameManeger にリストがなければ作成し、参照を保持
        if (MyGameManeger.instance.replayDataList == null)
        {
            MyGameManeger.instance.replayDataList = new List<ReplayData>();
        }
        dataList = MyGameManeger.instance.replayDataList;
        
        // リストをクリア
        dataList.Clear();

        // カウンタをリセット
        frameNumber = 0;
        timeSinceStart = 0f;
    }

    void FixedUpdate()
    {
        // 飛行中 (EnterFlight) のみ記録する
        if (!MyGameManeger.instance.EnterFlight)
        {
            return;
        }

        // 経過時間を加算 (FixedUpdateなので fixedDeltaTime を使う)
        timeSinceStart += Time.fixedDeltaTime;

        // AirDate.cs と同じ間隔チェック (0.02m の代わりに Time.fixedDeltaTime を使うとより安全)
        if (frameNumber % (interval / (decimal)Time.fixedDeltaTime) == 0)
        {
            // ★ここが AirDate.cs との違い★
            // 飛行データではなく、「位置」と「回転」を記録する
            ReplayData data = new ReplayData(
                timeSinceStart,
                planeTransform.position,
                planeTransform.rotation
            );

            // リストにデータを追加
            dataList.Add(data);
        }

        frameNumber++; // フレームカウント
    }
}