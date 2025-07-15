using UnityEngine;
using VRC.Dynamics;
using VRC.SDK3.Dynamics.PhysBone.Components;

namespace Aramaa.DakochiteGimmick.Editor
{
    /// <summary>
    /// VRChatアバター関連のGameObjectを安全に削除するためのユーティリティクラス。
    /// DestroyImmediate時に発生しうるMissingReferenceExceptionを回避します。
    /// </summary>
    public static class SafeDestroyUtility
    {
        /// <summary>
        /// 指定されたGameObjectをVRChat SDKの参照を適切に解除してから安全に削除します。
        /// これにより、MissingReferenceExceptionの発生を抑制します。
        /// 
        /// 処理手順:
        /// 1. 削除対象のGameObjectが存在するかチェックします。
        /// 2. オブジェクトの親子関係を一時的に解除し、SDKへの参照解除を確実にします。
        /// 3. オブジェクトとその子階層にある全てのVRCConstraintBaseコンポーネントを取得します。
        /// 4. VRChat SDKのVRCConstraintManagerをリフレッシュし、内部参照を更新・解除します。
        /// 5. GameObject.DestroyImmediate を使用して、オブジェクトを即座に削除します。
        /// </summary>
        /// <param name="objectToDestroy">削除するGameObject。</param>
        public static void SafeDestroyGameObject(GameObject objectToDestroy)
        {
            if (objectToDestroy == null)
            {
                Debug.LogWarning("SafeDestroyUtility: 削除対象のGameObjectがNullです。");
                return;
            }

            Debug.Log($"SafeDestroyUtility: オブジェクト '{objectToDestroy.name}' を安全に削除します。");

            // 1. 親子関係を一時的に切り離す
            // これにより、SDKの参照解除をより確実にする試みも行います。
            Transform originalParent = objectToDestroy.transform.parent;
            if (originalParent != null)
            {
                Debug.Log($"SafeDestroyUtility: オブジェクト '{objectToDestroy.name}' の親子関係を一時的に解除します。");
                objectToDestroy.transform.SetParent(null);
            }

            // 2. オブジェクトとその子階層にある全てのVRCConstraintBaseコンポーネントを取得
            // 非アクティブなコンポーネントも対象とする (true)
            VRCConstraintBase[] constraintsToRefresh = objectToDestroy.GetComponentsInChildren<VRCConstraintBase>(true);

            // 3. VRChat SDKのConstraintManagerを明示的にリフレッシュ
            if (constraintsToRefresh != null && constraintsToRefresh.Length > 0)
            {
                Debug.Log($"SafeDestroyUtility: '{objectToDestroy.name}' に関連する {constraintsToRefresh.Length} 個のVRCConstraintBaseをSDKからリフレッシュします。");
                VRCConstraintManager.Sdk_ManuallyRefreshGroups(constraintsToRefresh);
                // 必要であれば、ここで少しのディレイ（例: EditorUtility.DisplayProgressBar など）を挟むことも検討できますが、
                // 通常は即時削除の前に同期的に実行される想定です。
            }
            else
            {
                Debug.Log($"SafeDestroyUtility: '{objectToDestroy.name}' に関連するVRCConstraintBaseが見つかりませんでした。SDKリフレッシュは不要です。");
            }

            Debug.Log($"SafeDestroyUtility: オブジェクト '{objectToDestroy.name}' の削除が完了しました。");

            // 4. GameObject.DestroyImmediate を使用して、オブジェクトを即座に削除
            // 親子関係を解除した後は、元の親に戻す必要はありません。この直後に削除されるためです。
            GameObject.DestroyImmediate(objectToDestroy);
        }
    }
}


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

*/




