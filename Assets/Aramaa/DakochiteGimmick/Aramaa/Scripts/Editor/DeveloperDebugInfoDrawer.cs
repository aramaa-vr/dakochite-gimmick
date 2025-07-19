using System.IO;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Dynamics.Constraint.Components;

namespace Aramaa.DakochiteGimmick.Editor
{
    public static class DeveloperDebugInfoDrawer
    {
        /// <summary>
        /// 開発者モード時に、指定されたアバターの詳細情報をUnity Editorに描画します。
        /// ギミックのインスタンス、Constraint、EyeOffset、Hips/Headボーン、VRCAvatarDescriptorのView位置などを表示します。
        /// </summary>
        /// <param name="avatarRootObject">詳細情報を表示する対象のアバターのルートGameObject。</param>
        public static void DrawAvatarDebugInfo(GameObject avatarRootObject)
        {
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox("開発者モードが有効です。詳細情報が表示されています。", MessageType.Info);
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("現在のギミックとConstraint情報:", EditorStyles.boldLabel);

            if (avatarRootObject == null)
            {
                EditorGUILayout.LabelField("アバターのルートオブジェクトを選択してください。（詳細情報）", EditorStyles.label);
                return;
            }

            Transform gimmickInstanceTransform = HierarchyUtility.FindChildRecursive(avatarRootObject.transform, GimmickConstants.HOLD_GIMMICK_NAME);
            if (gimmickInstanceTransform == null)
            {
                EditorGUILayout.HelpBox($"アバター直下にギミックプレハブ '{GimmickConstants.HOLD_GIMMICK_NAME}' のインスタンスが見つかりません。", MessageType.Info);
                return;
            }

            VRCParentConstraint foundConstraint = HierarchyUtility.FindConstraintInHierarchy(gimmickInstanceTransform, GimmickConstants.CONSTRAINT_PATH_INSIDE_PREFAB);
            if (foundConstraint == null)
            {
                EditorGUILayout.HelpBox($"プレハブインスタンス '{gimmickInstanceTransform.name}' 内のパス ({GimmickConstants.CONSTRAINT_PATH_INSIDE_PREFAB}) にVRCParentConstraintが見つかりません。\nプレハブの構造が正しいか確認してください。", MessageType.Info);
                return;
            }

            EditorGUILayout.ObjectField("見つかったConstraint", foundConstraint.gameObject, typeof(GameObject), true);
            if (foundConstraint.TargetTransform == null)
            {
                EditorGUILayout.LabelField("現在のTarget Transform: 未設定", EditorStyles.label);
                return;
            }

            EditorGUILayout.ObjectField("現在のTarget Transform", foundConstraint.TargetTransform, typeof(Transform), true);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("EyeOffsetオブジェクト情報:", EditorStyles.boldLabel);
            Transform eyeOffsetTransform = HierarchyUtility.FindChildTransformByRelativePath(gimmickInstanceTransform, GimmickConstants.EYEOFFSET_PATH_INSIDE_PREFAB);
            if (eyeOffsetTransform == null)
            {
                EditorGUILayout.HelpBox($"プレハブインスタンス '{gimmickInstanceTransform.name}' 内のパス ({GimmickConstants.EYEOFFSET_PATH_INSIDE_PREFAB}) にEyeOffsetオブジェクトが見つかりません。", MessageType.Info);
                return;
            }

            EditorGUILayout.ObjectField("見つかったEyeOffset", eyeOffsetTransform.gameObject, typeof(GameObject), true);
            EditorGUILayout.Vector3Field("現在のローカル座標", eyeOffsetTransform.localPosition);
            EditorGUILayout.Vector3Field("現在のローカル回転 (オイラー)", eyeOffsetTransform.localRotation.eulerAngles);
            EditorGUILayout.Vector3Field("現在のワールド回転 (オイラー)", eyeOffsetTransform.rotation.eulerAngles);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("アバターのHipsボーン情報:", EditorStyles.boldLabel);
            var hipsBone = AvatarUtility.GetAnimatorHipsBone(avatarRootObject);
            if (hipsBone == null)
            {
                EditorGUILayout.HelpBox(string.Format(GimmickConstants.LOG_ANIMATOR_NOT_FOUND, avatarRootObject.name) + " またはAnimatorがHumanoid型ではありません。", MessageType.Warning);
                return;
            }

            EditorGUILayout.ObjectField("見つかったAnimator Hipsボーン", hipsBone, typeof(Transform), true);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("アバターのHeadボーン情報:", EditorStyles.boldLabel);
            var headBone = AvatarUtility.GetAnimatorHeadBone(avatarRootObject);
            if (headBone == null)
            {
                EditorGUILayout.HelpBox(string.Format(GimmickConstants.LOG_ANIMATOR_NOT_FOUND, avatarRootObject.name) + " またはAnimatorがHumanoid型ではありません。（Headボーン）", MessageType.Warning);
                return;
            }

            EditorGUILayout.ObjectField("見つかったAnimator Headボーン", headBone, typeof(Transform), true);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("VRCAvatarDescriptor View位置:", EditorStyles.boldLabel);
            var avatarDescriptor = avatarRootObject.GetComponent<VRCAvatarDescriptor>();
            if (avatarDescriptor == null)
            {
                EditorGUILayout.HelpBox("アバターにVRCAvatarDescriptorが見つかりません。", MessageType.Warning);
                return;
            }

            EditorGUILayout.Vector3Field("View Position (Avatar Local)", avatarDescriptor.ViewPosition);
        }

        /// <summary>
        /// Constraintオブジェクトの現在のTransform情報をデバッグログに出力します。
        /// 主に開発者モードでの情報確認用です。
        /// </summary>
        /// <param name="avatarRootObject">アバターのルートGameObject。</param>
        public static void LogConstraintPosition(GameObject avatarRootObject)
        {
            if (avatarRootObject == null)
            {
                return;
            }

            Transform gimmickInstanceTransform = HierarchyUtility.FindChildRecursive(avatarRootObject.transform, GimmickConstants.HOLD_GIMMICK_NAME);
            if (gimmickInstanceTransform == null)
            {
                Debug.LogWarning(string.Format(GimmickConstants.LOG_GIMMICK_INSTANCE_NOT_FOUND, GimmickConstants.HOLD_GIMMICK_NAME));
                return;
            }

            VRCParentConstraint parentConstraint = HierarchyUtility.FindConstraintInHierarchy(gimmickInstanceTransform, GimmickConstants.CONSTRAINT_PATH_INSIDE_PREFAB);
            if (parentConstraint == null)
            {
                Debug.LogWarning(GimmickConstants.LOG_CONSTRAINT_GO_NOT_FOUND);
                return;
            }

            Transform constraintTransform = parentConstraint.transform;
            Debug.Log($"<color=green>Constraint GameObject: {constraintTransform.name}</color>");
            Debug.Log($"<color=green>Constraint Local Position: {constraintTransform.localPosition}</color>");
            Debug.Log($"<color=green>Constraint World Position: {constraintTransform.position}</color>");
            Debug.Log($"<color=green>Constraint Local Rotation: {constraintTransform.localRotation.eulerAngles}</color>");
            Debug.Log($"<color=green>Constraint World Rotation: {constraintTransform.rotation.eulerAngles}</color>");
            if (parentConstraint.TargetTransform == null)
            {
                return;
            }

            Debug.Log($"<color=green>Constraint Target (Hips/Head) World Position: {parentConstraint.TargetTransform.position}</color>");
            Debug.Log($"<color=green>Constraint Target (Hips/Head) World Rotation: {parentConstraint.TargetTransform.rotation.eulerAngles}</color>");
        }
    }
}
