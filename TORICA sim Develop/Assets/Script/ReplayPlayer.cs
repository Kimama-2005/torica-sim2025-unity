using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReplayPlayer : MonoBehaviour
{
    public bool isReplaying = false; // 現在リプレイ再生中か

    private GameObject plane;         // 飛行機オブジェクト
    private Rigidbody planeRigidbody; // 飛行機のRigidbody
    private List<ReplayData> replayDataList; // 記録データ（MyGameManegerから取得）

    private int replayIndex = 0;      // 現在再生中のインデックス
    private float replayStartTime = 0f; // リプレイ開始時の実時間

    void Start()
    {
        // MyGameManeger から飛行機オブジェクトとリプレイデータを取得
        plane = MyGameManeger.instance.Plane;
        planeRigidbody = plane.GetComponent<Rigidbody>();
        replayDataList = MyGameManeger.instance.replayDataList;
    }

    // --- リプレイ制御用の関数 ---

    // [1] リプレイ開始（UIボタンなどから呼び出す）
    public void StartReplay()
    {
        // データがない、または既に再生中なら何もしない
        if (replayDataList == null || replayDataList.Count == 0 || isReplaying)
        {
            Debug.LogWarning("リプレイデータがないか、再生中です");
            return;
        }

        Debug.Log("リプレイ開始");
        isReplaying = true;
        replayIndex = 0;
        replayStartTime = Time.time; // 「今」の時刻を記録

        // ★重要★
        // 物理演算の影響を止める（Kinematic = 運動学的にする）
        // これをしないと、物理演算とリプレイがケンカしてガクガクになります
        planeRigidbody.isKinematic = true;
        planeRigidbody.velocity = Vector3.zero;
        planeRigidbody.angularVelocity = Vector3.zero;

        // 飛行制御スクリプトなども無効化するのが望ましい
        // (例: plane.GetComponent<AerodynamicCalculator>().enabled = false;)
    }

    // [2] リプレイ停止
    public void StopReplay()
    {
        if (!isReplaying) return;

        Debug.Log("リプレイ停止");
        isReplaying = false;

        // 物理演算を元に戻す
        planeRigidbody.isKinematic = false;

        // 無効化したスクリプトを元に戻す
        // (例: plane.GetComponent<AerodynamicCalculator>().enabled = true;)
    }


    // --- メインの再生処理 ---

    void Update()
    {
        // リプレイ中じゃなければ何もしない
        if (!isReplaying)
        {
            return;
        }

        // 記録データがなければ停止
        if (replayDataList == null || replayDataList.Count == 0)
        {
            StopReplay();
            return;
        }

        // 現在の再生時間（リプレイ開始から何秒経ったか）
        float currentReplayTime = Time.time - replayStartTime;

        // [A] 再生すべきデータインデックス(replayIndex) を探す
        //     (currentReplayTime が dataList[i].time と dataList[i+1].time の間になる i を探す)
        while (replayIndex < replayDataList.Count - 2 && // リストの最後から2番目まで
               replayDataList[replayIndex + 1].time <= currentReplayTime)
        {
            // 次のデータポイントの時間が、現在の再生時間より過去なら、インデックスを進める
            replayIndex++;
        }

        // リストの最後に到達したらリプレイ終了
        if (replayIndex >= replayDataList.Count - 1)
        {
            // 最後のデータを適用
            plane.transform.position = replayDataList[replayDataList.Count - 1].position;
            plane.transform.rotation = replayDataList[replayDataList.Count - 1].rotation;
            StopReplay();
            return;
        }

        // [B] 2点間の補間（スムージング）
        // これにより、0.1秒間隔の記録でも滑らかに動いているように見えます。

        ReplayData currentData = replayDataList[replayIndex];     // 補間元のデータ (例: 1.0秒時点)
        ReplayData nextData = replayDataList[replayIndex + 1]; // 補間先のデータ (例: 1.1秒時点)

        // 2点間の時間差 (例: 1.1 - 1.0 = 0.1秒)
        float duration = nextData.time - currentData.time;
        // 2点間で、現在がどのあたりにいるか (0.0 ～ 1.0 の値)
        // (例: (1.03 - 1.0) / 0.1 = 0.3。 30%の位置)
        float progress = (currentReplayTime - currentData.time) / duration;

        // [C] 飛行機の位置・回転を強制的に上書き
        // Vector3.Lerp : 位置を線形補間
        plane.transform.position = Vector3.Lerp(currentData.position, nextData.position, progress);
        // Quaternion.Slerp : 回転を球面線形補間（こちらの方が滑らか）
        plane.transform.rotation = Quaternion.Slerp(currentData.rotation, nextData.rotation, progress);
    }
}