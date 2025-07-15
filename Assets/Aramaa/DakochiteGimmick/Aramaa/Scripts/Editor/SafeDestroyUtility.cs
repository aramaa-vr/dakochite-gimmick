using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using VRC.Dynamics;

namespace Aramaa.DakochiteGimmick.Editor
{
    public static class SafeDestroyUtility
    {
        public static void SafeDestroyGameObject(GameObject objectToDestroy)
        {
            if (objectToDestroy == null)
            {
                Debug.LogWarning("SafeDestroyUtility: 削除対象のGameObjectがnullです。");
                return;
            }

            // GameObjectを即時削除
            GameObject.DestroyImmediate(objectToDestroy);

            Debug.Log("SafeDestroyUtility: オブジェクトの削除が完了しました。");
        }
    }
}

/*

namespace Aramaa.DakochiteGimmick.Editor
{
    /// <summary>
    /// VRChat SDK公式APIを使って、安全にGameObjectを削除し、SDKの状態も更新するユーティリティ。
    /// </summary>
    public static class SafeDestroyUtility
    {
        /// <summary>
        /// 指定されたGameObjectをDestroyImmediateで削除する前に、VRChat SDKのキャッシュを正しく更新します。
        /// </summary>
        /// <param name="objectToDestroy">削除対象のGameObject</param>
        public static void SafeDestroyGameObject(GameObject objectToDestroy)
        {
            if (objectToDestroy == null)
            {
                Debug.LogWarning("SafeDestroyUtility: 削除対象のGameObjectがnullです。");
                return;
            }

            // PrefabインスタンスであればUnpack
            if (PrefabUtility.IsPartOfPrefabInstance(objectToDestroy))
            {
                Debug.Log($"SafeDestroyUtility: '{objectToDestroy.name}' はPrefabインスタンスです。Unpackします。");
                PrefabUtility.UnpackPrefabInstance(
                    PrefabUtility.GetOutermostPrefabInstanceRoot(objectToDestroy),
                    PrefabUnpackMode.Completely,
                    InteractionMode.AutomatedAction);
            }

            // VRCConstraintBase を収集してキャッシュを更新
            var constraints = objectToDestroy.GetComponentsInChildren<VRCConstraintBase>(true);
            if (constraints != null && constraints.Length > 0)
            {
                Debug.Log($"SafeDestroyUtility: {constraints.Length} 個のVRCConstraintBaseをリフレッシュします。");
                VRCConstraintManager.Sdk_ManuallyRefreshGroups(constraints);
            }

            // GameObjectを即時削除
            GameObject.DestroyImmediate(objectToDestroy);

            // Unityに変更を通知（保存対象としてマーク）
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

            Debug.Log("SafeDestroyUtility: オブジェクトの削除とSDK更新が完了しました。");
        }
    }
}

*/

/*

namespace Aramaa.DakochiteGimmick.Editor
{
    public static class SafeDestroyUtility
    {
        /// <summary>
        /// DestroyImmediate実行時にVRCConstraintManagerでMissingReferenceExceptionが発生する場合があるため、
        /// PrefabのアンパックとVRCConstraintBaseのリフレッシュを行い、
        /// 親子関係を解除した上で対象オブジェクトとその子オブジェクトを安全に削除します。
        /// </summary>
        /// <param name="objectToDestroy">削除対象のGameObject</param>
        public static void SafeDestroyGameObject(GameObject objectToDestroy)
        {
            if (objectToDestroy == null)
            {
                Debug.LogWarning("SafeDestroyUtility: 削除対象のGameObjectがNullです。");
                return;
            }

            // Prefabインスタンスなら完全にアンパックしてPrefab依存を解除
            if (PrefabUtility.IsPartOfPrefabInstance(objectToDestroy))
            {
                Debug.Log($"SafeDestroyUtility: '{objectToDestroy.name}' はPrefabインスタンスです。Unpackします。");
                PrefabUtility.UnpackPrefabInstance(
                    PrefabUtility.GetOutermostPrefabInstanceRoot(objectToDestroy),
                    PrefabUnpackMode.Completely,
                    InteractionMode.AutomatedAction);
            }
            
            Debug.Log($"SafeDestroyUtility: オブジェクト '{objectToDestroy.name}' の削除処理を開始します。");
            
            // VRCConstraintBaseコンポーネントを取得しSDKの内部参照をリフレッシュ
            var constraints = objectToDestroy.GetComponentsInChildren<VRCConstraintBase>(true);
            
            if (constraints != null && constraints.Length > 0)
            {
                Debug.Log($"SafeDestroyUtility: {constraints.Length} 個のVRCConstraintBaseをSDKにリフレッシュ要求します。");
                VRCConstraintManager.Sdk_ManuallyRefreshGroups(constraints);
            }
            
            Debug.Log($"SafeDestroyUtility: オブジェクト '{objectToDestroy.name}' の削除が完了しました。");
            
            // 親子関係を解除（子オブジェクトも個別に削除が必要なため）
            Transform[] allTransforms = objectToDestroy.GetComponentsInChildren<Transform>(true);
            VRCConstraintBase[] allConstraints = objectToDestroy.GetComponentsInChildren<VRCConstraintBase>(true);
            VRCPhysBoneBase[] allBones = objectToDestroy.GetComponentsInChildren<VRCPhysBoneBase>(true);
            
            foreach (var tf in allTransforms)
            {
                if (tf.parent != null)
                {
                    tf.SetParent(null);
                }
            }
            
            // VRCConstraintBaseを全て即時削除
            foreach (var tf in allConstraints)
            {
                if (tf != null)
                {
                    GameObject.DestroyImmediate(tf);
                }
            }
            
            // VRCPhysBoneBaseを全て即時削除
            foreach (var tf in allBones)
            {
                if (tf != null)
                {
                    GameObject.DestroyImmediate(tf);
                }
            }
            
            // 解除した全てのTransformのGameObjectを個別に削除
            foreach (var tf in allTransforms)
            {
                if (tf != null)
                {
                    GameObject.DestroyImmediate(tf.gameObject);
                }
            }
        }
    }
}

*/

/*

MissingReferenceException: The object of type 'VRCPhysBone' has been destroyed but you are still trying to access it.
Your script should either check if it is null or you should not destroy the object.
VRC.Dynamics.VRCPhysBoneBase.GetRootTransform () (at <f5bf20752b824e4fba16c3917af8405a>:0)
VRC.Dynamics.VRCConstraintBase.DeterminePhysBoneDependency(UnityEngine.Transform constraintEffectiveTarget, VRC.Dynamics.VRCPhysBoneBase physBone)(at<f5bf20752b824e4fba16c3917af8405a>:0)
VRC.Dynamics.VRCConstraintBase.ReEvaluatePhysBoneOrder()(at<f5bf20752b824e4fba16c3917af8405a>:0)
VRC.Dynamics.VRCConstraintManager.RegisterConstraint(VRC.Dynamics.VRCConstraintBase constraint)(at<f5bf20752b824e4fba16c3917af8405a>:0)
VRC.Dynamics.VRCConstraintManager.Sdk_ManuallyRefreshGroups(VRC.Dynamics.VRCConstraintBase[] proxiedConstraints)(at<f5bf20752b824e4fba16c3917af8405a>:0)
VRC.SDK3A.Editor.VRCSdkControlPanelAvatarBuilder.OnGUIAvatarCheck(VRC.SDKBase.VRC_AvatarDescriptor avatar)(at./ Packages / com.vrchat.avatars / Editor / VRCSDK / SDK3A / VRCSdkControlPanelAvatarBuilder.cs:317)
VRC.SDK3A.Editor.VRCSdkControlPanelAvatarBuilder.CreateValidationsGUI(UnityEngine.UIElements.VisualElement root)(at./ Packages / com.vrchat.avatars / Editor / VRCSDK / SDK3A / VRCSdkControlPanelAvatarBuilder.cs:253)
VRCSdkControlPanel.RunValidations()(at./ Packages / com.vrchat.base / Editor / VRCSDK / Dependencies / VRChat / ControlPanel / VRCSdkControlPanelBuilder.cs:883)
VRCSdkControlPanel +<> c__DisplayClass204_0.< ShowBuilders > b__1()(at./ Packages / com.vrchat.base / Editor / VRCSDK / Dependencies / VRChat / ControlPanel / VRCSdkControlPanelBuilder.cs:822)
UnityEngine.UIElements.VisualElement + SimpleScheduledItem.PerformTimerUpdate(UnityEngine.UIElements.TimerState state)(at < 332857d8803a4878904bcf8f9581ec33 >:0)
UnityEngine.UIElements.TimerEventScheduler.UpdateScheduledEvents()(at < 332857d8803a4878904bcf8f9581ec33 >:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.UpdateSchedulers()(at < 332857d8803a4878904bcf8f9581ec33 >:0)
UnityEngine.UIElements.UIEventRegistration.UpdateSchedulers()(at < 332857d8803a4878904bcf8f9581ec33 >:0)
UnityEditor.RetainedMode.UpdateSchedulers()(at<cc76bab7efe9480f901125fd04a708b6>:0)

MissingReferenceException: The object of type 'VRCParentConstraint' has been destroyed but you are still trying to access it.
Your script should either check if it is null or you should not destroy the object.
VRC.Dynamics.VRCConstraintBase.GetEffectiveTargetTransform () (at <f5bf20752b824e4fba16c3917af8405a>:0)
VRC.Dynamics.VRCConstraintBase.RequiresReallocation (VRC.Dynamics.VRCConstraintJobData& jobData, System.Boolean& sameGameObjectOnly) (at <f5bf20752b824e4fba16c3917af8405a>:0)
VRC.Dynamics.VRCConstraintBase.CheckReallocation (VRC.Dynamics.VRCConstraintJobData& jobData) (at <f5bf20752b824e4fba16c3917af8405a>:0)
VRC.Dynamics.VRCConstraintManager.UpdateConstraints () (at <f5bf20752b824e4fba16c3917af8405a>:0)
UnityEngine.Debug:LogException(Exception)
VRC.Dynamics.VRCConstraintManager:UpdateConstraints()
VRC.Dynamics.VRCAvatarDynamicsScheduler:UpdateConstraints(Boolean)
VRC.SystemsPlayerLoop:OnVRCConstraintsUpdate()

MissingReferenceException: The object of type 'VRCParentConstraint' has been destroyed but you are still trying to access it.
Your script should either check if it is null or you should not destroy the object.
VRC.Dynamics.VRCConstraintBase.get_DependencyRoot () (at <f5bf20752b824e4fba16c3917af8405a>:0)
VRC.Dynamics.VRCConstraintGrouper.PrepareGroupsForReorganize (System.Collections.Generic.IReadOnlyList`1[VRC.Dynamics.VRCConstraintBase]& constraintsManaged) (at <f5bf20752b824e4fba16c3917af8405a>:0)
VRC.Dynamics.VRCConstraintGrouper.RefreshGroups (System.Collections.Generic.IReadOnlyList`1[VRC.Dynamics.VRCConstraintBase]& constraintsManaged) (at <f5bf20752b824e4fba16c3917af8405a>:0)
VRC.Dynamics.VRCConstraintManager.UpdateConstraints () (at <f5bf20752b824e4fba16c3917af8405a>:0)
VRC.Dynamics.VRCAvatarDynamicsScheduler.UpdateConstraints (System.Boolean finalizeImmediately) (at <f5bf20752b824e4fba16c3917af8405a>:0)
VRC.SystemsPlayerLoop.OnVRCConstraintsUpdate () (at <7715a28ca31f45ddb0d8441fc8fe586c>:0)

*/
