using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace Aramaa.DakochiteGimmick.Editor
{
    /// <summary>
    /// みんなでつかめるだこちてギミックのEditorWindowが保持するセッション固有のデータ管理クラス。
    /// このクラスは、EditorWindowの生存期間中のみ有効な、ゲームオブジェクト参照、
    /// 一時的な設定、状態などを保持し、ウィンドウの破棄とともに解放されます。
    /// </summary>
    public class GimmickData // staticではない通常のクラス
    {
        // ====================================================================================================
        // EditorWindowが参照・操作するGameObject
        // ====================================================================================================

        /// <summary>
        /// 現在選択されている、または操作対象のアバターのルートGameObject。
        /// </summary>
        public GameObject AvatarRootObject { get; set; } = null;

        public VRCAvatarDescriptor AvatarDescriptor { get; set; } = null;

        public Transform HipsBone { get; set; } = null;

        public Transform HeadBone { get; set; } = null;

        public GameObject GimmickPrefabAssetCache { get; set; } = null;

        public GameObject GimmickPrefabInstance { get; set; } = null;

        public UpdateCallbackState CallbackState { get; set; } = UpdateCallbackState.None;

        // 遅延呼び出し用フレームカウンター
        public int DelayedCallFrameCounter { get; set; } = 0;

        public EditorApplication.CallbackFunction UpdateCallback { get; set; } = null;

        /// <summary>
        /// 開発者向けの詳細情報を表示するかどうかのフラグ。
        /// </summary>
        public bool ShowDeveloperInfo { get; set; } = false;

        public GameObjectListHolder Holder { get; set; } = null;

        // ====================================================================================================
        // コンストラクタ (初期化処理)
        // ====================================================================================================

        /// <summary>
        /// GimmickEditorDataの新しいインスタンスを初期化します。
        /// EditorWindowが開かれる際に呼び出されます。
        /// </summary>
        public GimmickData()
        {
            ResetData();

            Debug.Log("[GimmickEditorData] New instance created. Editor data initialized.");
        }

        // ====================================================================================================
        // データのリセットやクリアメソッド
        // ====================================================================================================

        /// <summary>
        /// 全ての管理データを初期状態にリセットします。
        /// 例えば、アバター選択が解除された場合などに呼び出せます。
        /// </summary>
        public void ResetData()
        {
            AvatarRootObject = null;
            GimmickPrefabAssetCache = null;
            ShowDeveloperInfo = false;
            Holder = null;

            ClearCurrentCallbackContext();

            Debug.Log("[GimmickEditorData] All data has been reset.");
        }

        public void ClearCurrentCallbackContext()
        {
            DelayedCallFrameCounter = 0;
            CallbackState = UpdateCallbackState.None;
            RemoveUpdateCallbackIfNeeded();
            AvatarDescriptor = null;
            HipsBone = null;
            HeadBone = null;
            GimmickPrefabInstance = null;
        }

        public void SetWaiting()
        {
            DelayedCallFrameCounter = 0;
            CallbackState = UpdateCallbackState.Waiting;
        }

        /// <summary>
        /// 既存のコールバックがあれば解除しておく
        /// </summary>
        public void RemoveUpdateCallbackIfNeeded()
        {
            if (UpdateCallback == null)
            {
                return;
            }

            EditorApplication.update -= UpdateCallback;
            UpdateCallback = null;
        }

        // EditorApplication.update に登録
        public void AddUpdateCallback(EditorApplication.CallbackFunction function)
        {
            RemoveUpdateCallbackIfNeeded();

            UpdateCallback = function;
            EditorApplication.update += UpdateCallback;
        }

        public void DestroyInstanceImmediateIfNeeded()
        {
            if (GimmickPrefabInstance == null)
            {
                return;
            }

            GameObject.DestroyImmediate(GimmickPrefabInstance);
            GimmickPrefabInstance = null;
        }
    }
}
