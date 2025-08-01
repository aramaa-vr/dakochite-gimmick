using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using VRC.SDK3.Dynamics.PhysBone.Components;
using VRC.SDK3.Avatars.Components;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace Aramaa.DakochiteGimmick.Editor
{
    /// <summary>
    /// PhysBoneの「掴み」に関するアニメーション、Animator Controller、およびVRCExpressionParametersの自動設定を処理します。
    /// このクラスは、アバターのPhysBoneのallowGrabbingプロパティを制御するギミックのセットアップを自動化します。
    /// </summary>
    public static class PhysBoneGimmickAutomation
    {
        /// <summary>
        /// 指定されたアバターのPhysBoneに対して、ホールドギミックのアニメーションとAnimator Controllerを生成し、
        /// Modular Avatar Merge Animator に割り当て、VRCExpressionParameters を設定します。
        /// </summary>
        public static bool GeneratePhysBoneHoldGimmickSetup(GameObject avatarRoot, List<GameObject> additionalExcludeGameObjects)
        {
            if (avatarRoot == null)
            {
                EditorErrorDialog.DisplayDialog(GimmickError.AvatarObjectRemovedDuringSetup);
                return false;
            }

            // 除外パスを考慮してPhysBoneを検索
            List<VRCPhysBone> targetPhysBones = FindEligiblePhysBones(avatarRoot, additionalExcludeGameObjects);

            // phys boneの数が0の場合、エラー回避のためにhysBoneHoldGimickGeneratorを削除して完了
            if (!targetPhysBones.Any())
            {
                GameObject holdGimickAndCamera = ModularAvatarLinkerUtility.FindChildGameObjectRecursive(avatarRoot, GimmickConstants.MA_TARGET_PARENT_GO_NAME);
                if (holdGimickAndCamera != null)
                {
                    GameObject physBoneHoldGimickGenerator = ModularAvatarLinkerUtility.FindChildGameObjectRecursive(holdGimickAndCamera, GimmickConstants.MA_TARGET_CHILD_GO_NAME);
                    if (physBoneHoldGimickGenerator != null)
                    {
                        GameObject.DestroyImmediate(physBoneHoldGimickGenerator.gameObject);
                        physBoneHoldGimickGenerator = null;
                    }
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                return true;
            }

            // アセット出力パスの準備
            string timestamp = System.DateTime.Now.ToString("yyyyMMDD_HHmmss");
            string outputDirectory = Path.Combine(GimmickConstants.PHYSBONE_OUTPUT_BASE_PATH, $"{avatarRoot.name}_{timestamp}");

            // outputDirectoryがAssets/からの相対パスであることを確認し、必要な親フォルダを作成
            // 例: "Assets/Aramaa/GeneratedAssets/HoldGimick/PhysBoneToggleAnimatorGenerator"
            // outputDirectoryはこれにさらにアバター名とタイムスタンプが追加される
            // "Assets/Aramaa/GeneratedAssets/HoldGimick/AvatarName_Timestamp"
            string currentPath = "Assets";
            string[] pathSegments = outputDirectory.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            for (int i = 0; i < pathSegments.Length; i++)
            {
                if (pathSegments[i] == "Assets" && i == 0) continue; // "Assets"自体は作成不要

                string nextPath = Path.Combine(currentPath, pathSegments[i]);
                if (!AssetDatabase.IsValidFolder(nextPath))
                {
                    string guid = AssetDatabase.CreateFolder(currentPath, pathSegments[i]);
                    if (string.IsNullOrEmpty(guid))
                    {
                        EditorErrorDialog.DisplayDialog(GimmickError.OutputFolderCreationFailed, nextPath);
                        return false;
                    }
                }
                currentPath = nextPath;
            }

            // 最終的なoutputDirectoryが存在することを確認
            if (!AssetDatabase.IsValidFolder(outputDirectory))
            {
                EditorErrorDialog.DisplayDialog(GimmickError.OutputFolderCreationFailed, outputDirectory);
                return false;
            }

            // PhysBoneの「Grab無効」と「初期状態維持」のアニメーションクリップを生成
            // Clip name for allowGrabbing = false (Gimmick ON)
            AnimationClip grabOffClip = CreatePhysBoneAnimationClip(targetPhysBones, false, $"{avatarRoot.name}_Grab_Off", outputDirectory, avatarRoot.transform);
            // Clip name for retaining initial allowGrabbing state (Gimmick OFF / Default state)
            AnimationClip grabDefaultClip = CreatePhysBoneAnimationClip(targetPhysBones, true, $"{avatarRoot.name}_Grab_Default", outputDirectory, avatarRoot.transform);

            if (grabOffClip == null || grabDefaultClip == null)
            {
                EditorErrorDialog.DisplayDialog(GimmickError.AnimationClipCreationFailed);
                return false;
            }

            // Animator Controllerの生成と設定
            AnimatorController animatorController = CreatePhysBoneAnimatorController(avatarRoot.name, outputDirectory, grabDefaultClip, grabOffClip);
            if (animatorController == null)
            {
                EditorErrorDialog.DisplayDialog(GimmickError.AnimatorControllerCreationFailed);
                return false;
            }

            // Modular Avatar Merge Animatorへのリンク
            bool maLinkSuccess = ModularAvatarLinkerUtility.LinkAnimatorToMergeAnimator(
                avatarRoot,
                animatorController,
                GimmickConstants.MA_TARGET_PARENT_GO_NAME,
                GimmickConstants.MA_TARGET_CHILD_GO_NAME,
                VRCAvatarDescriptor.AnimLayerType.FX
            );

            if (!maLinkSuccess)
            {
                EditorErrorDialog.DisplayDialog(GimmickError.ModularAvatarLinkFailed);
                return false;
            }

            // VRCExpressionParametersの設定
            // GimmickConstants.PHYSBONE_PARAMETER_NAME を使用
            // bool paramSuccess = SetupVRCExpressionParameters(avatarRoot, GimmickConstants.PHYSBONE_PARAMETER_NAME);
            // if (!paramSuccess)
            // {
            //     // VRCExpressionParametersの設定は必須ではないため、警告に留める
            //     Debug.LogWarning("[PhysBoneGimmickAutomation] VRCExpressionParameters の設定に問題がありました。手動での確認が必要です。");
            // }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Debug.Log($"[PhysBoneGimmickAutomation] Successfully generated PhysBone grab toggle animations and Animator Controller in: {outputDirectory}");
            return true;
        }

        /// <summary>
        /// 指定されたGameObjectの子階層からすべてのVRCPhysBoneを検索し、除外パスを考慮します。
        /// </summary>
        /// <param name="root">アバターのルートGameObject。</param>
        /// <param name="additionalExcludeGameObjects">追加で除外するGameObjectのリスト。</param>
        /// <returns>除外パスに該当しないPhysBoneのリスト。</returns>
        private static List<VRCPhysBone> FindEligiblePhysBones(GameObject root, List<GameObject> additionalExcludeGameObjects)
        {
            List<VRCPhysBone> foundPhysBones = new List<VRCPhysBone>();
            VRCPhysBone[] allPhysBonesInRoot = root.GetComponentsInChildren<VRCPhysBone>(true);

            // 除外対象のGameObjectのリストを格納するためのもの
            List<GameObject> excludeTargetGameObjects = new List<GameObject>();

            // GimmickConstants.PHYSBONE_EXCLUDE_PATH_SEGMENTS から除外対象を追加
            foreach (string pathSegment in GimmickConstants.PHYSBONE_EXCLUDE_PATH_SEGMENTS)
            {
                GameObject excludeObj = ModularAvatarLinkerUtility.FindChildGameObjectRecursive(root, pathSegment);
                if (excludeObj != null)
                {
                    excludeTargetGameObjects.Add(excludeObj);
                }
            }

            // additionalExcludeGameObjects が指定されていれば追加
            if (additionalExcludeGameObjects != null)
            {
                // additionalExcludeGameObjects 内の個々の要素が null でないことを保証しながら追加
                foreach (GameObject obj in additionalExcludeGameObjects)
                {
                    if (obj != null)
                    {
                        excludeTargetGameObjects.Add(obj);
                    }
                }
            }

            foreach (VRCPhysBone pb in allPhysBonesInRoot)
            {
                if (pb == null) continue;

                bool isExcluded = false;
                // 検出された除外対象のGameObjectをすべてチェック
                foreach (GameObject excludeTargetGameObject in excludeTargetGameObjects)
                {
                    if (excludeTargetGameObject == null) continue;

                    // PhysBoneが除外対象の子孫であるかチェック
                    Transform current = pb.transform;
                    while (current != null && current != root.transform)
                    {
                        if (current == excludeTargetGameObject.transform)
                        {
                            isExcluded = true;
                            break; // このPhysBoneはこの除外対象に該当するので、他の除外対象はチェック不要
                        }
                        current = current.parent;
                    }
                    if (isExcluded) break; // このPhysBoneは既に除外対象と判断されたので、次のPhysBoneへ
                }

                if (!isExcluded)
                {
                    foundPhysBones.Add(pb);
                }
            }
            return foundPhysBones;
        }

        /// <summary>
        /// PhysBoneのAllow Grabbing On/Offアニメーションクリップを生成します。
        /// </summary>
        /// <param name="physBones">アニメーションを生成するPhysBoneのリスト。</param>
        /// <param name="isDefaultStateClip">初期状態維持クリップ（allowGrabbingの現在の値を使用）を生成する場合はtrue、Grab無効クリップ（allowGrabbingをfalseに設定）を生成する場合はfalse。</param>
        /// <param name="clipName">生成するクリップの名前。</param>
        /// <param name="outputPath">クリップを保存するアセットパス。</param>
        /// <param name="avatarRootTransform">アバターのルートTransform (パス生成用)。</param>
        /// <returns>生成されたAnimationClip、またはエラーの場合はnull。</returns>
        private static AnimationClip CreatePhysBoneAnimationClip(List<VRCPhysBone> physBones, bool isDefaultStateClip, string clipName, string outputPath, Transform avatarRootTransform)
        {
            AnimationClip clip = new AnimationClip();
            clip.name = clipName;
            clip.legacy = false;

            foreach (VRCPhysBone pb in physBones)
            {
                if (pb == null) continue;

                string physBonePath = AnimationUtilityExtension.GetGameObjectPath(pb.gameObject, avatarRootTransform);

                AnimationCurve curve = new AnimationCurve();
                float keyValue;

                if (isDefaultStateClip)
                {
                    // Grab_Defaultクリップの場合、PhysBoneの現在のallowGrabbing状態を取得し、その値を使用
                    keyValue = (pb.allowGrabbing == VRCPhysBone.AdvancedBool.True) ? GimmickConstants.ADVANCED_BOOL_TRUE_VALUE : GimmickConstants.ADVANCED_BOOL_FALSE_VALUE;
                    // Debug.Log($"[Animation Curve Setup] For '{clip.name}', PhysBone '{pb.gameObject.name}' initial allowGrabbing: {pb.allowGrabbing}, setting to: {keyValue}");
                }
                else
                {
                    // Grab_Offクリップの場合、allowGrabbingを常にfalseに設定
                    keyValue = GimmickConstants.ADVANCED_BOOL_FALSE_VALUE;
                    // Debug.Log($"[Animation Curve Setup] For '{clip.name}', PhysBone '{pb.gameObject.name}' setting to: {keyValue} (False)");
                }

                curve.AddKey(new Keyframe(0f, keyValue, 0f, 0f, 0f, 0f));
                AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(physBonePath, typeof(VRCPhysBone), GimmickConstants.PHYSBONE_PROPERTY_NAME), curve);

                // Debug.Log($"[Animation Curve Setup] Clip: '{clip.name}', GameObject Path: '{physBonePath}', Property: '{GimmickConstants.PHYSBONE_PROPERTY_NAME}', Set Value: {keyValue} (Expected Bool State: {(isDefaultStateClip ? (pb.allowGrabbing == VRCPhysBone.AdvancedBool.True ? "True" : "False") : "False")})");
            }

            string fullPath = Path.Combine(outputPath, $"{clip.name}.anim");
            if (string.IsNullOrEmpty(fullPath))
            {
                Debug.LogError($"[PhysBoneGimmickAutomation] Generated fullPath is empty for clip '{clip.name}'. Output Path: '{outputPath}'");
                return null;
            }

            fullPath = AssetDatabase.GenerateUniqueAssetPath(fullPath);
            AssetDatabase.CreateAsset(clip, fullPath);

            return clip;
        }

        /// <summary>
        /// PhysBone制御用のアニメーターコントローラーを生成し設定します。
        /// </summary>
        private static AnimatorController CreatePhysBoneAnimatorController(string avatarName, string outputPath, AnimationClip defaultClip, AnimationClip offClip)
        {
            string controllerPath = Path.Combine(outputPath, $"{avatarName}_PhysBoneGrabController.controller");
            controllerPath = AssetDatabase.GenerateUniqueAssetPath(controllerPath);

            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            if (controller == null)
            {
                Debug.LogError($"[PhysBoneGimmickAutomation] Animator Controllerの作成に失敗しました: {controllerPath}");
                return null;
            }

            // GimmickConstants.PHYSBONE_PARAMETER_NAME を使用し、型をIntに変更
            controller.AddParameter(GimmickConstants.PHYSBONE_PARAMETER_NAME, AnimatorControllerParameterType.Int);

            AnimatorControllerLayer baseLayer = controller.layers[0];
            baseLayer.name = "Grab Toggle Layer";
            baseLayer.defaultWeight = 1f;
            AnimatorStateMachine stateMachine = baseLayer.stateMachine;

            AnimatorState defaultState = stateMachine.AddState("Grab_Default");
            defaultState.motion = defaultClip;
            defaultState.writeDefaultValues = false;

            AnimatorState offState = stateMachine.AddState("Grab_Off");
            offState.motion = offClip;
            offState.writeDefaultValues = false;

            stateMachine.defaultState = defaultState; // 初期状態をGrab_Defaultに設定 (HoldType=0)

            // トランジション条件を調整
            // Default -> Off のトランジション: HoldTypeが0以外になったらOffへ
            AnimatorStateTransition defaultToOffDirect = defaultState.AddTransition(offState);
            // GimmickConstants.PHYSBONE_PARAMETER_NAME を使用
            defaultToOffDirect.AddCondition(AnimatorConditionMode.NotEqual, 0, GimmickConstants.PHYSBONE_PARAMETER_NAME); // HoldTypeが0以外ならOffへ
            defaultToOffDirect.hasExitTime = false;
            defaultToOffDirect.duration = 0f;

            // Off -> Default のトランジション: HoldTypeが0になったらDefaultへ
            AnimatorStateTransition offToDefaultDirect = offState.AddTransition(defaultState);
            // GimmickConstants.PHYSBONE_PARAMETER_NAME を使用
            offToDefaultDirect.AddCondition(AnimatorConditionMode.Equals, 0, GimmickConstants.PHYSBONE_PARAMETER_NAME); // HoldTypeが0ならDefaultへ
            offToDefaultDirect.hasExitTime = false;
            offToDefaultDirect.duration = 0f;

            return controller;
        }

        /// <summary>
        /// VRCExpressionParametersにPhysBone制御用パラメータを追加します。
        /// これは、既にプレハブの方に追加してあるので不要と思う一旦コメントアウト
        /// </summary>
        // private static bool SetupVRCExpressionParameters(GameObject avatarRoot, string parameterName)
        // {
        //     VRCAvatarDescriptor avatarDescriptor = avatarRoot.GetComponent<VRCAvatarDescriptor>();
        //     VRCExpressionParameters currentParams = avatarDescriptor?.expressionParameters;
        // 
        //     if (currentParams == null)
        //     {
        //         Debug.LogWarning("[PhysBoneGimmickAutomation] VRCExpressionParameters not found on avatar. Please add a VRC Avatar Descriptor and Expression Parameters asset manually if needed, then re-run this tool.");
        //         return false;
        //     }
        // 
        //     var paramList = new List<VRCExpressionParameters.Parameter>(currentParams.parameters);
        //     // GimmickConstants.PHYSBONE_PARAMETER_NAME を使用
        //     VRCExpressionParameters.Parameter existingParam = paramList.FirstOrDefault(p => p.name == parameterName);
        // 
        //     if (existingParam == null)
        //     {
        //         var newParam = new VRCExpressionParameters.Parameter
        //         {
        //             name = parameterName, // GimmickConstants.PHYSBONE_PARAMETER_NAME を使用
        //             valueType = VRCExpressionParameters.ValueType.Int, // 型をIntに変更
        //             defaultValue = 0f, // 初期状態がGrab_Default (0)
        //             saved = true
        //         };
        //         paramList.Add(newParam);
        //         currentParams.parameters = paramList.ToArray();
        //         EditorUtility.SetDirty(currentParams);
        //         Debug.Log($"[PhysBoneGimmickAutomation] Added '{parameterName}' parameter to VRCExpressionParameters: {AssetDatabase.GetAssetPath(currentParams)}");
        //     }
        //     else
        //     {
        //         // 既存のパラメータがInt型であるかをチェック
        //         if (existingParam.valueType != VRCExpressionParameters.ValueType.Int)
        //         {
        //             Debug.LogWarning($"[PhysBoneGimmickAutomation] Parameter '{parameterName}' already exists but is not of type Int. Please verify manually.");
        //         }
        //         else
        //         {
        //             Debug.LogWarning($"[PhysBoneGimmickAutomation] Parameter '{parameterName}' already exists in VRCExpressionParameters. Please ensure its 'Default' value is set to 0 if you want 'Grab_Default' as initial state.");
        //         }
        //     }
        //     return true;
        // }
    }
}
