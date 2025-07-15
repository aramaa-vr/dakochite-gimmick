using UnityEditor;
using UnityEngine;
using VRC.SDK3.Dynamics.Constraint.Components;
using VRC.SDK3.Avatars.Components;

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
        private const int DELAY_FRAMES_FOR_CONSTRAINT_UPDATE = 5; // コンストレイントの更新待ちフレーム数

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
            // Resources.Loadを使用するため、Path.GetFileNameWithoutExtensionは不要。
            // ただし、既存ギミックの検索名としてはファイル名部分が必要なので注意。
            // ここではGIMMICK_PREFAB_PATHに拡張子を含めない形式がConstantsで定義されていると仮定し、
            // 末尾のパスセグメントを直接利用します。
            string gimmickPrefabName = GimmickConstants.GIMMICK_PREFAB_PATH.Substring(GimmickConstants.GIMMICK_PREFAB_PATH.LastIndexOf('/') + 1);
            Transform existingGimmickInstance = HierarchyUtility.FindChildRecursive(avatarRootObject.transform, gimmickPrefabName);

            if (existingGimmickInstance != null)
            {
                Debug.Log($"既存のギミック '{existingGimmickInstance.name}' を削除しました。");
                GameObject.DestroyImmediate(existingGimmickInstance.gameObject); // 既存ギミックを即座に削除
                EditorUtility.DisplayDialog("削除完了", GimmickConstants.MSG_EXISTING_GIMMICK_DELETED, "OK");
                // ここで処理を終了し、再実行を促す、または削除と生成の間に間隔を設けるべきかの検討が必要
                return false; // 通常、既存削除後はメッセージを出して終了させるのが親切
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
                // エラーダイアログはPhysBoneGimmickAutomationで表示されるためここでは省略
            }

            // ギミックプレハブのロード
            if (_gimmickPrefabAssetCache == null)
            {
                _gimmickPrefabAssetCache = Resources.Load<GameObject>(GimmickConstants.GIMMICK_PREFAB_PATH); // Resources.Loadに変更
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

            gimmickPrefabInstance.transform.SetParent(avatarRootObject.transform, false); // ワールド座標を維持しない
            gimmickPrefabInstance.transform.localPosition = Vector3.zero;
            gimmickPrefabInstance.transform.localRotation = Quaternion.identity;
            gimmickPrefabInstance.transform.localScale = Vector3.one;

            EditorUtility.SetDirty(gimmickPrefabInstance);

            // VRCParentConstraintの設定
            // ギミックプレハブ内の特定のパスにあるVRCParentConstraintを探す
            VRCParentConstraint parentConstraint = HierarchyUtility.FindConstraintInHierarchy(gimmickPrefabInstance.transform, GimmickConstants.CONSTRAINT_PATH_INSIDE_PREFAB);
            if (parentConstraint == null)
            {
                EditorUtility.DisplayDialog("エラー", GimmickConstants.MSG_CONSTRAINT_NOT_FOUND, "OK");
                Debug.LogError(string.Format(GimmickConstants.LOG_CONSTRAINT_NOT_FOUND_IN_PREFAB, gimmickPrefabInstance.name, GimmickConstants.CONSTRAINT_PATH_INSIDE_PREFAB));
                return false;
            }

            parentConstraint.TargetTransform = hipsBone; // Hipsボーンをターゲットに設定
            EditorUtility.SetDirty(parentConstraint);

            // EyeOffsetの調整処理をdelayCallで呼び出す
            // VRCParentConstraintがワールド座標を正しく更新するのに数フレームかかる場合があるため、遅延実行
            _delayedCallFrameCounter = 0; // フレームカウンターをリセット
            EditorApplication.CallbackFunction delayedAction = null;

            delayedAction = () =>
            {
                _delayedCallFrameCounter++;
                if (_delayedCallFrameCounter < DELAY_FRAMES_FOR_CONSTRAINT_UPDATE)
                {
                    // 指定フレーム数に達するまで待機を継続
                    EditorApplication.delayCall += delayedAction;
                    return;
                }

                // delayCallの実行が完了したので、デリゲートから削除
                EditorApplication.delayCall -= delayedAction;

                // 遅延実行中にアバターやギミックが削除されていないかチェック
                if (avatarRootObject == null || gimmickPrefabInstance == null)
                {
                    Debug.LogWarning(GimmickConstants.LOG_EYEOFFSET_ADJUSTMENT_SKIPPED_NULL);
                    return;
                }

                bool eyeOffsetAdjusted = AdjustEyeOffset(avatarRootObject, gimmickPrefabInstance.transform, headBone);

                // EyeOffsetの調整が成功した場合のみPhysBoneのセットアップに進む
                if (eyeOffsetAdjusted)
                {
                    // PhysBoneGimmickAutomationを呼び出す（PhysBone関連の追加設定を行う別スクリプト）
                    bool physBoneSetupSuccess = PhysBoneGimmickAutomation.GeneratePhysBoneHoldGimmickSetup(avatarRootObject);

                    if (!physBoneSetupSuccess)
                    {
                        EditorUtility.DisplayDialog("エラー", "PhysBone Hold Gimmick Setupの実行に失敗しました。", "OK");
                    }
                }
            };

            EditorApplication.delayCall += delayedAction; // 遅延実行を登録

            return true;
        }

        /// <summary>
        /// EyeOffsetオブジェクトのワールドTransformをVRCAvatarDescriptorのViewPositionに合わせて調整します。
        /// このメソッドは、EditorApplication.delayCallを介して、Unityの更新処理後に呼び出されることを想定しています。
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
    }
}
