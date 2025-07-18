using UnityEditor;
using UnityEngine;
using VRC.Dynamics;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Dynamics.Constraint.Components;

namespace Aramaa.DakochiteGimmick.Editor
{
    /// <summary>
    /// VRChatアバターへのギミックのセットアップ処理をカプセル化するサービス。
    /// UIロジックから分離され、再利用性とテスト容易性を向上させます。
    /// </summary>
    public static class ConstraintSetupService
    {
        private static GameObject _gimmickPrefabAssetCache; // プレハブアセットのキャッシュ
        private static int _delayedCallFrameCounter = 0; // 遅延呼び出し用フレームカウンター
        private const int DELAY_FRAMES_FOR_CONSTRAINT_UPDATE = 180; // コンストレイントの更新待ちフレーム数

        private static EditorApplication.CallbackFunction _updateCallback;
        private static GameObject _avatarRootObject; // コールバック内で使用するアバターのルート
        private static GameObject _gimmickPrefabInstance; // コールバック内で使用するギミックインスタンス
        private static Transform _currentHeadBone; // コールバック内で使用するHeadボーン

        /// <summary>
        /// VRChatアバターに対するギミックのフルセットアップを実行します。
        /// 既存ギミックがある場合はそれを削除し、新規に生成します。
        /// </summary>
        /// <param name="avatarRootObject">セットアップ対象のアバターのルートGameObject。</param>
        /// <returns>セットアップが成功したかどうか。</returns>
        public static bool PerformFullSetup(GameObject avatarRootObject)
        {
            if (avatarRootObject == null)
            {
                EditorUtility.DisplayDialog("エラー", GimmickConstants.MSG_AVATAR_NOT_SELECTED, "OK");
                return false;
            }

            // 既存ギミックの検出と削除
            string gimmickPrefabName = GimmickConstants.GIMMICK_PREFAB_PATH.Substring(GimmickConstants.GIMMICK_PREFAB_PATH.LastIndexOf('/') + 1);
            Transform existingGimmickInstance = HierarchyUtility.FindChildRecursive(avatarRootObject.transform, gimmickPrefabName);

            if (existingGimmickInstance != null)
            {
                // MissingReferenceExceptionの調査をしたが、不明点が多く解決しないので一旦保留
                // SafeDestroyUtility.SafeDestroyGameObject(existingGimmickInstance.gameObject);

                GameObject.DestroyImmediate(existingGimmickInstance.gameObject);
                EditorUtility.DisplayDialog("削除完了", GimmickConstants.MSG_EXISTING_GIMMICK_DELETED, "OK");
                return false;
            }

            // 疑似ビューポイントがずれるため、アバターのルートの座標が (0,0,0) からわずかでも離れている場合にエラーとする
            if (Vector3.Distance(avatarRootObject.transform.position, Vector3.zero) > GimmickConstants.AVATAR_ROOT_POSITION_TOLERANCE)
            {
                EditorUtility.DisplayDialog("エラー", GimmickConstants.AVATAR_ROOT_POSITION_ERROR, "OK");
                return false;
            }

            // Hipsボーンの取得
            Transform hipsBone = AvatarUtility.GetAnimatorHipsBone(avatarRootObject);
            if (hipsBone == null)
            {
                EditorUtility.DisplayDialog("エラー", GimmickConstants.MSG_HIPS_NOT_FOUND, "OK");
                return false;
            }

            // Headボーンの取得（EyeOffset調整に必要だが必須ではない）
            Transform headBone = AvatarUtility.GetAnimatorHeadBone(avatarRootObject);
            if (headBone == null)
            {
                Debug.LogWarning(GimmickConstants.MSG_HEAD_NOT_FOUND);
            }

            // ギミックプレハブのロード
            if (_gimmickPrefabAssetCache == null)
            {
                _gimmickPrefabAssetCache = Resources.Load<GameObject>(GimmickConstants.GIMMICK_PREFAB_PATH);
            }
            GameObject gimmickPrefabAsset = _gimmickPrefabAssetCache;

            if (gimmickPrefabAsset == null)
            {
                EditorUtility.DisplayDialog("エラー", GimmickConstants.MSG_PREFAB_NOT_FOUND, "OK");
                Debug.LogError(string.Format(GimmickConstants.LOG_PREFAB_NOT_FOUND_AT_PATH, GimmickConstants.GIMMICK_PREFAB_PATH));
                return false;
            }

            // ギミックプレハブのインスタンス化とアバターへのアタッチ
            GameObject gimmickPrefabInstance = PrefabUtility.InstantiatePrefab(gimmickPrefabAsset) as GameObject;
            if (gimmickPrefabInstance == null)
            {
                EditorUtility.DisplayDialog("エラー", GimmickConstants.MSG_PREFAB_INSTANTIATION_FAILED, "OK");
                Debug.LogError(GimmickConstants.LOG_PREFAB_INSTANTIATION_FAILED);
                return false;
            }

            gimmickPrefabInstance.transform.SetParent(avatarRootObject.transform, false);
            gimmickPrefabInstance.transform.localPosition = Vector3.zero;
            gimmickPrefabInstance.transform.localRotation = Quaternion.identity;
            gimmickPrefabInstance.transform.localScale = Vector3.one;

            EditorUtility.SetDirty(gimmickPrefabInstance);

            // VRCParentConstraintの設定
            VRCParentConstraint parentConstraint = HierarchyUtility.FindConstraintInHierarchy(gimmickPrefabInstance.transform, GimmickConstants.CONSTRAINT_PATH_INSIDE_PREFAB);
            if (parentConstraint == null)
            {
                EditorUtility.DisplayDialog("エラー", GimmickConstants.MSG_CONSTRAINT_NOT_FOUND, "OK");
                Debug.LogError(string.Format(GimmickConstants.LOG_CONSTRAINT_NOT_FOUND_IN_PREFAB, gimmickPrefabInstance.name, GimmickConstants.CONSTRAINT_PATH_INSIDE_PREFAB));
                return false;
            }

            parentConstraint.TargetTransform = hipsBone; // Hipsボーンをターゲットに設定
            EditorUtility.SetDirty(parentConstraint);

            RefreshEditorWindows(gimmickPrefabInstance); // 強制再描画で反映を促す

            // EyeOffsetの調整処理をEditorApplication.updateで呼び出す
            _delayedCallFrameCounter = 0; // フレームカウンターをリセット

            // コールバック内で必要な引数を静的変数にキャッシュ
            _avatarRootObject = avatarRootObject;
            _gimmickPrefabInstance = gimmickPrefabInstance;
            _currentHeadBone = headBone;

            // 既存のコールバックがあれば解除しておく（念のため）
            if (_updateCallback != null)
            {
                EditorApplication.update -= _updateCallback;
            }

            // EditorApplication.update に登録する CallbackFunction 型のラムダ式を定義
            _updateCallback = () =>
            {
                _delayedCallFrameCounter++;
                if (_delayedCallFrameCounter < DELAY_FRAMES_FOR_CONSTRAINT_UPDATE)
                {
                    // 指定フレーム数に達するまで待機を継続
                    return;
                }

                // 指定フレーム数に達したので、デリゲートから削除
                EditorApplication.update -= _updateCallback;
                // 後処理のために参照をクリア
                _updateCallback = null;

                // 遅延実行中にアバターやギミックが削除されていないかチェック
                if (_avatarRootObject == null || _gimmickPrefabInstance == null)
                {
                    Debug.LogWarning(GimmickConstants.LOG_EYEOFFSET_ADJUSTMENT_SKIPPED_NULL);
                    ClearCurrentCallbackContext(); // コンテキストをクリア
                    return;
                }

                bool eyeOffsetAdjusted = AdjustEyeOffset(_avatarRootObject, _gimmickPrefabInstance.transform, _currentHeadBone);

                // EyeOffsetの調整が成功した場合のみPhysBoneのセットアップに進む
                if (eyeOffsetAdjusted)
                {
                    // PhysBoneGimmickAutomationを呼び出す（PhysBone関連の追加設定を行う別スクリプト）
                    bool physBoneSetupSuccess = PhysBoneGimmickAutomation.GeneratePhysBoneHoldGimmickSetup(_avatarRootObject);

                    if (!physBoneSetupSuccess)
                    {
                        EditorUtility.DisplayDialog("エラー", "PhysBone Hold Gimmick Setupの実行に失敗しました。", "OK");
                    }
                }
                ClearCurrentCallbackContext(); // コンテキストをクリア
            };

            EditorApplication.update += _updateCallback; // EditorApplication.update に登録

            return true;
        }

        /// <summary>
        /// 強制的にModular AvatarやVRCConstraintに更新を書けるための保険処理
        /// </summary>
        /// <param name="gimmickPrefabInstance"></param>
        private static void RefreshEditorWindows(GameObject gimmickPrefabInstance)
        {
            // --- コンストレイントの強制更新とエディタ描画の促し ---
            var constraints = gimmickPrefabInstance.GetComponentsInChildren<VRCConstraintBase>(true);
            if (constraints != null && constraints.Length > 0)
            {
                Debug.Log($"SafeDestroyUtility: {constraints.Length} 個のVRCConstraintBaseをリフレッシュします。");

                // 注意！このメソッドは非推奨のため将来使えなくなる可能性がある
                VRCConstraintManager.Sdk_ManuallyRefreshGroups(constraints);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            foreach (EditorWindow window in Resources.FindObjectsOfTypeAll<EditorWindow>())
            {
                window.Repaint();
            }

            SceneView.RepaintAll();
            EditorApplication.QueuePlayerLoopUpdate();
        }

        /// <summary>
        /// EyeOffsetオブジェクトのワールドTransformをVRCAvatarDescriptorのViewPositionに合わせて調整します。
        /// </summary>
        /// <param name="avatarRootObject">アバターのルートGameObject。</param>
        /// <param name="gimmickRootTransform">生成されたギミックプレハブのルートTransform。</param>
        /// <param name="headBone">アバターのHeadボーンのTransform。</param>
        /// <returns>EyeOffsetの調整が正常に完了したかどうか。</returns>
        private static bool AdjustEyeOffset(GameObject avatarRootObject, Transform gimmickRootTransform, Transform headBone)
        {
            if (avatarRootObject == null || gimmickRootTransform == null)
            {
                return false;
            }

            // ギミック内のEyeOffsetに対応するTransformを探す
            Transform eyeOffsetTransform = HierarchyUtility.FindChildTransformByRelativePath(gimmickRootTransform, GimmickConstants.EYEOFFSET_PATH_INSIDE_PREFAB);

            if (eyeOffsetTransform != null)
            {
                VRCAvatarDescriptor avatarDescriptor = avatarRootObject.GetComponent<VRCAvatarDescriptor>();
                if (avatarDescriptor == null)
                {
                    EditorUtility.DisplayDialog("エラー", GimmickConstants.MSG_AVATARDESCRIPTOR_NOT_FOUND, "OK");
                    Debug.LogError(GimmickConstants.LOG_AVATARDESCRIPTOR_NOT_FOUND);
                    return false;
                }
                else if (headBone == null) // Headボーンが見つからない場合は調整できない
                {
                    EditorUtility.DisplayDialog("エラー", GimmickConstants.MSG_EYEOFFSET_HEAD_BONE_MISSING, "OK");
                    Debug.LogError(GimmickConstants.MSG_EYEOFFSET_HEAD_BONE_MISSING);
                    return false;
                }
                else
                {
                    // VRCParentConstraintが設定されたTransform（Constraintの親）
                    // おそらく、eyeOffsetTransformの親がParentConstraintを持つGameObjectを想定
                    Transform constraintParentTransform = eyeOffsetTransform.parent;
                    // 制約の親の位置がVector3.zeroだとおかしいのでチェック
                    if (constraintParentTransform == null || constraintParentTransform.position == Vector3.zero)
                    {
                        EditorUtility.DisplayDialog("エラー", GimmickConstants.MSG_CONSTRAINT_UPDATE_FAILED_PARENT_POS_ZERO, "OK");
                        return false;
                    }

                    // ViewPositionはアバターローカル座標なので、Constraintの親のローカル座標に変換して設定
                    Vector3 viewPositionInConstraintParentLocal = constraintParentTransform.InverseTransformPoint(avatarDescriptor.ViewPosition);
                    eyeOffsetTransform.localPosition = viewPositionInConstraintParentLocal;

                    // Headボーンの回転の逆をEyeOffsetのローカル回転に設定（Headの回転を打ち消すことで視線に合わせる？）
                    eyeOffsetTransform.localRotation = Quaternion.Inverse(headBone.rotation);
                    eyeOffsetTransform.localScale = Vector3.one;

                    EditorUtility.SetDirty(eyeOffsetTransform); // 変更を保存
                    Debug.Log(string.Format(GimmickConstants.LOG_EYEOFFSET_ADJUSTED, eyeOffsetTransform.localPosition, eyeOffsetTransform.localRotation.eulerAngles, eyeOffsetTransform.localScale));
                    Debug.Log(string.Format(GimmickConstants.LOG_EYEOFFSET_ADJUSTMENT_SUMMARY, avatarDescriptor.ViewPosition, eyeOffsetTransform.position, eyeOffsetTransform.rotation.eulerAngles));

                    return true;
                }
            }
            else // EyeOffsetTransformが見つからない場合
            {
                EditorUtility.DisplayDialog("エラー", "eyeOffsetTransformが見つかりませんでした。", "OK");
                Debug.LogWarning(GimmickConstants.MSG_EYEOFFSET_NOT_FOUND);
                return false;
            }
        }

        /// <summary>
        /// Constraintオブジェクトの現在のTransform情報をデバッグログに出力します。
        /// 主に開発者モードでの情報確認用です。
        /// </summary>
        /// <param name="avatarRootObject">アバターのルートGameObject。</param>
        public static void LogConstraintPosition(GameObject avatarRootObject)
        {
            if (avatarRootObject == null) return;

            string gimmickPrefabName = GimmickConstants.GIMMICK_PREFAB_PATH.Substring(GimmickConstants.GIMMICK_PREFAB_PATH.LastIndexOf('/') + 1);
            Transform gimmickInstanceTransform = HierarchyUtility.FindChildRecursive(avatarRootObject.transform, gimmickPrefabName);

            if (gimmickInstanceTransform != null)
            {
                VRCParentConstraint parentConstraint = HierarchyUtility.FindConstraintInHierarchy(gimmickInstanceTransform, GimmickConstants.CONSTRAINT_PATH_INSIDE_PREFAB);
                if (parentConstraint != null)
                {
                    Transform constraintTransform = parentConstraint.transform;
                    Debug.Log($"<color=green>Constraint GameObject: {constraintTransform.name}</color>");
                    Debug.Log($"<color=green>Constraint Local Position: {constraintTransform.localPosition}</color>");
                    Debug.Log($"<color=green>Constraint World Position: {constraintTransform.position}</color>");
                    Debug.Log($"<color=green>Constraint Local Rotation: {constraintTransform.localRotation.eulerAngles}</color>");
                    Debug.Log($"<color=green>Constraint World Rotation: {constraintTransform.rotation.eulerAngles}</color>");
                    if (parentConstraint.TargetTransform != null)
                    {
                        Debug.Log($"<color=green>Constraint Target (Hips/Head) World Position: {parentConstraint.TargetTransform.position}</color>");
                        Debug.Log($"<color=green>Constraint Target (Hips/Head) World Rotation: {parentConstraint.TargetTransform.rotation.eulerAngles}</color>");
                    }
                }
                else
                {
                    Debug.LogWarning(GimmickConstants.LOG_CONSTRAINT_GO_NOT_FOUND);
                }
            }
            else
            {
                Debug.LogWarning(string.Format(GimmickConstants.LOG_GIMMICK_INSTANCE_NOT_FOUND, gimmickPrefabName));
            }
        }

        private static void ClearCurrentCallbackContext()
        {
            _updateCallback = null;
            _avatarRootObject = null;
            _gimmickPrefabInstance = null;
            _currentHeadBone = null;
        }
    }
}
