using UnityEditor;
using UnityEngine;
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
        // コンストレイントの更新待ちフレーム数 (自分のPCだと1フレームで完了するが、低スペックPCのために待たせる)
        private const int DELAY_FRAMES_FOR_CONSTRAINT_UPDATE = 240;

        /// <summary>
        /// VRChatアバターに対するギミックのフルセットアップを実行します。
        /// 既存ギミックがある場合はそれを削除し、新規に生成します。
        /// </summary>
        public static void PerformFullSetup(GimmickData gimmickData)
        {
            gimmickData.ClearCurrentCallbackContext();

            if (gimmickData.AvatarRootObject == null)
            {
                EditorErrorDialog.DisplayDialog(GimmickError.AvatarRootIsNull);
                return;
            }

            gimmickData.AvatarDescriptor = gimmickData.AvatarRootObject.GetComponent<VRCAvatarDescriptor>();
            if (gimmickData.AvatarDescriptor == null)
            {
                EditorErrorDialog.DisplayDialog(GimmickError.AvatarDescriptorNotFound);
                return;
            }

            // 既存ギミックの検出と削除
            Transform existingGimmickInstance = HierarchyUtility.FindChildRecursive(gimmickData.AvatarRootObject.transform, GimmickConstants.HOLD_GIMMICK_NAME);
            if (existingGimmickInstance != null)
            {
                // MissingReferenceExceptionの調査をしたが、不明点が多く解決しないので一旦保留
                // SafeDestroyUtility.SafeDestroyGameObject(existingGimmickInstance.gameObject);

                GameObject.DestroyImmediate(existingGimmickInstance.gameObject);
                EditorUtility.DisplayDialog("削除完了", GimmickConstants.MSG_EXISTING_GIMMICK_DELETED, "OK");
                return;
            }

            // 疑似ビューポイントがずれるため、アバターのルートの座標が (0,0,0) からわずかでも離れている場合にエラーとする
            // if (Vector3.Distance(gimmickData.AvatarRootObject.transform.position, Vector3.zero) > GimmickConstants.AVATAR_ROOT_POSITION_TOLERANCE)
            // {
            // EditorErrorDialog.DisplayDialog(GimmickError.AvatarRootPositionIsNotZero);
            // return;
            // }

            // Hipsボーンの取得
            gimmickData.HipsBone = AvatarUtility.GetAnimatorHipsBone(gimmickData.AvatarRootObject);
            if (gimmickData.HipsBone == null)
            {
                EditorErrorDialog.DisplayDialog(GimmickError.HipsBoneNotFound);
                return;
            }

            // Headボーンの取得
            gimmickData.HeadBone = AvatarUtility.GetAnimatorHeadBone(gimmickData.AvatarRootObject);
            if (gimmickData.HeadBone == null)
            {
                EditorErrorDialog.DisplayDialog(GimmickError.HeadBoneNotFound);
                return;
            }

            // ギミックプレハブのロード
            if (gimmickData.GimmickPrefabAssetCache == null)
            {
                gimmickData.GimmickPrefabAssetCache = Resources.Load<GameObject>(GimmickConstants.HOLD_GIMMICK_PREFAB_RESOURCE_PATH);
            }

            if (gimmickData.GimmickPrefabAssetCache == null)
            {
                EditorErrorDialog.DisplayDialog(GimmickError.GimmickPrefabNotFound);
                return;
            }

            // ギミックプレハブのインスタンス化とアバターへのアタッチ
            gimmickData.GimmickPrefabInstance = PrefabUtility.InstantiatePrefab(gimmickData.GimmickPrefabAssetCache) as GameObject;
            if (gimmickData.GimmickPrefabInstance == null)
            {
                EditorErrorDialog.DisplayDialog(GimmickError.GimmickPrefabInstantiationFailed);
                return;
            }

            gimmickData.GimmickPrefabInstance.transform.SetParent(gimmickData.AvatarRootObject.transform, false);
            gimmickData.GimmickPrefabInstance.transform.localPosition = Vector3.zero;
            gimmickData.GimmickPrefabInstance.transform.localRotation = Quaternion.identity;
            gimmickData.GimmickPrefabInstance.transform.localScale = Vector3.one;

            EditorUtility.SetDirty(gimmickData.GimmickPrefabInstance);

            // VRCParentConstraintの設定
            VRCParentConstraint parentConstraint = HierarchyUtility.FindConstraintInHierarchy(gimmickData.GimmickPrefabInstance.transform, GimmickConstants.CONSTRAINT_PATH_INSIDE_PREFAB);
            if (parentConstraint == null)
            {
                GameObject.DestroyImmediate(gimmickData.GimmickPrefabInstance.gameObject);
                EditorErrorDialog.DisplayDialog(GimmickError.GimmickPrefabCorrupted);
                Debug.LogError(string.Format(GimmickConstants.LOG_CONSTRAINT_NOT_FOUND_IN_PREFAB, gimmickData.GimmickPrefabInstance.name, GimmickConstants.CONSTRAINT_PATH_INSIDE_PREFAB));
                return;
            }

            parentConstraint.TargetTransform = gimmickData.HipsBone; // Hipsボーンをターゲットに設定
            EditorUtility.SetDirty(parentConstraint);

            // RefreshEditorWindows(gimmickData.GimmickPrefabInstance); // 強制再描画で反映を促す

            gimmickData.SetWaiting();

            // EditorApplication.update に登録する CallbackFunction 型のラムダ式を定義
            gimmickData.AddUpdateCallback(() =>
            {
                // 保険
                if (gimmickData.CallbackState == UpdateCallbackState.None)
                {
                    gimmickData.RemoveUpdateCallbackIfNeeded();
                    return;
                }

                gimmickData.DelayedCallFrameCounter++;
                if (gimmickData.DelayedCallFrameCounter <= DELAY_FRAMES_FOR_CONSTRAINT_UPDATE)
                {
                    // 指定フレーム数に達するまで待機を継続
                    gimmickData.CallbackState = UpdateCallbackState.Waiting;
                    return;
                }

                // 指定フレーム数に達したので、デリゲートから削除
                gimmickData.RemoveUpdateCallbackIfNeeded();

                gimmickData.CallbackState = UpdateCallback(gimmickData);

                if (gimmickData.CallbackState == UpdateCallbackState.Error)
                {
                    gimmickData.DestroyInstanceImmediateIfNeeded();
                }

                gimmickData.ClearCurrentCallbackContext();
            });
        }

        private static UpdateCallbackState UpdateCallback(GimmickData gimmickData)
        {

            // 遅延実行中にギミックが削除されていないかチェック
            if (gimmickData.GimmickPrefabInstance == null)
            {
                EditorErrorDialog.DisplayDialog(GimmickError.GimmickInstanceRemovedDuringSetup);
                return UpdateCallbackState.Error;
            }

            // 遅延実行中にアバターが削除されていないかチェック
            if (gimmickData.AvatarRootObject == null)
            {
                EditorErrorDialog.DisplayDialog(GimmickError.AvatarObjectRemovedDuringSetup);
                return UpdateCallbackState.Error;
            }

            // EyeOffsetの調整が成功した場合のみPhysBoneのセットアップに進む
            bool eyeOffsetAdjusted = AdjustEyeOffset(gimmickData);
            if (!eyeOffsetAdjusted)
            {
                return UpdateCallbackState.Error;
            }

            // PhysBoneGimmickAutomationを呼び出す（PhysBone関連の追加設定を行う別スクリプト）
            bool physBoneSetupSuccess = PhysBoneGimmickAutomation.GeneratePhysBoneHoldGimmickSetup(gimmickData.AvatarRootObject, gimmickData.IgnoreGameObjects);
            if (!physBoneSetupSuccess)
            {
                return UpdateCallbackState.Error;
            }

            EditorUtility.DisplayDialog("セットアップ完了", "みんなでつかめるだこちてギミックを\nアバターに設定しました。", "OK");
            return UpdateCallbackState.Success;
        }

        // 一旦コメントアウト
        // エラーダイアログが出せない問題があるのを回避
        // MissingReferenceExceptionが発生している場合、Sdk_ManuallyRefreshGroupsを実行した時点でエラーになり進行が停止する。
        /// <summary>
        /// 強制的にModular AvatarやVRCConstraintに更新を書けるための保険処理
        /// </summary>
        /// <param name="gimmickPrefabInstance"></param>
        // private static void RefreshEditorWindows(GameObject gimmickPrefabInstance)
        // {
        //     var constraints = gimmickPrefabInstance.GetComponentsInChildren<VRCConstraintBase>(true);
        //     if (constraints != null && constraints.Length > 0)
        //     {
        //         Debug.Log($"SafeDestroyUtility: {constraints.Length} 個のVRCConstraintBaseをリフレッシュします。");
        // 
        //         // 注意！このメソッドは非推奨のため将来使えなくなる可能性がある
        //         VRCConstraintManager.Sdk_ManuallyRefreshGroups(constraints);
        //     }
        // 
        //     AssetDatabase.SaveAssets();
        //     AssetDatabase.Refresh();
        // 
        //     foreach (EditorWindow window in Resources.FindObjectsOfTypeAll<EditorWindow>())
        //     {
        //         window.Repaint();
        //     }
        // 
        //     SceneView.RepaintAll();
        //     EditorApplication.QueuePlayerLoopUpdate();
        // }

        /// <summary>
        /// EyeOffsetオブジェクトのワールドTransformをVRCAvatarDescriptorのViewPositionに合わせて調整します。
        /// </summary>
        /// <param name="avatarRootObject">アバターのルートGameObject。</param>
        /// <param name="gimmickRootTransform">生成されたギミックプレハブのルートTransform。</param>
        /// <param name="headBone">アバターのHeadボーンのTransform。</param>
        /// <returns>EyeOffsetの調整が正常に完了したかどうか。</returns>
        private static bool AdjustEyeOffset(GimmickData gimmickData)
        {
            // ギミック内のEyeOffsetに対応するTransformを探す
            Transform eyeOffsetTransform = HierarchyUtility.FindChildTransformByRelativePath(gimmickData.GimmickPrefabInstance.transform, GimmickConstants.EYEOFFSET_PATH_INSIDE_PREFAB);
            if (eyeOffsetTransform == null)
            {
                EditorErrorDialog.DisplayDialog(GimmickError.EyeOffsetTransformNotFoundInGimmick);
                return false;
            }

            if (gimmickData.AvatarDescriptor == null)
            {
                EditorErrorDialog.DisplayDialog(GimmickError.AvatarDescriptorRemovedDuringSetup);
                return false;
            }

            if (gimmickData.HeadBone == null) // Headボーンが見つからない場合は調整できない
            {
                EditorErrorDialog.DisplayDialog(GimmickError.EyeOffsetHeadBoneNotFound);
                return false;
            }

            // VRCParentConstraintが設定されたTransform（Constraintの親）
            Transform constraintParentTransform = eyeOffsetTransform.parent;
            Transform avatarRoot = gimmickData.AvatarRootObject.transform; // アバタールートTransformを取得

            // Headの座標がVector3.zeroの場合、座標が更新されていないためエラー
            if (constraintParentTransform == null || constraintParentTransform.localPosition == Vector3.zero)
            {
                EditorErrorDialog.DisplayDialog(GimmickError.ConstraintUpdateFailedOrTimedOut);
                return false;
            }

            // ver 1.0.7 アバターのルートオブジェクトの座標が(0, 0, 0)では無くてもビューポイントの自動生成ができるようにする対応

            // 1. アバターのルートが原点にあると仮定した仮想のワールド座標でのViewPositionを取得
            //    (ViewPositionからアバタールートのワールド座標を加算)
            Vector3 virtualViewPositionWorld = gimmickData.AvatarDescriptor.ViewPosition + avatarRoot.position;

            // 2. Constraintの親のワールド→ローカル変換行列を取得
            Matrix4x4 constraintParentWorldToLocalMatrix = constraintParentTransform.worldToLocalMatrix;

            // 3. 仮想のワールド座標を、行列を使ってConstraintの親のローカル座標に変換
            Vector3 viewPositionInConstraintParentLocal = constraintParentWorldToLocalMatrix.MultiplyPoint(virtualViewPositionWorld);

            eyeOffsetTransform.localPosition = viewPositionInConstraintParentLocal;

            // Headボーンの回転の逆をEyeOffsetのローカル回転に設定（Headの回転を打ち消すことで視線に合わせる）
            eyeOffsetTransform.localRotation = Quaternion.Inverse(gimmickData.HeadBone.rotation);
            eyeOffsetTransform.localScale = Vector3.one;

            EditorUtility.SetDirty(eyeOffsetTransform); // 変更を保存
            Debug.Log(string.Format(GimmickConstants.LOG_EYEOFFSET_ADJUSTED, eyeOffsetTransform.localPosition, eyeOffsetTransform.localRotation.eulerAngles, eyeOffsetTransform.localScale));
            Debug.Log(string.Format(GimmickConstants.LOG_EYEOFFSET_ADJUSTMENT_SUMMARY, gimmickData.AvatarDescriptor.ViewPosition, eyeOffsetTransform.position, eyeOffsetTransform.rotation.eulerAngles));

            return true;
        }
    }
}
