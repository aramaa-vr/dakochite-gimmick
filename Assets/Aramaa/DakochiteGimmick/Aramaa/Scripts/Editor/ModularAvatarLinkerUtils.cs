using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using VRC.SDK3.Avatars.Components;
using nadena.dev.modular_avatar.core;

namespace Aramaa.DakochiteGimmick.Editor
{
    /// <summary>
    /// Modular Avatar関連のユーティリティメソッドを提供します。
    /// ModularAvatarMergeAnimatorへのAnimatorControllerのリンクなど。
    /// </summary>
    public static class ModularAvatarLinkerUtility
    {
        /// <summary>
        /// 指定されたAnimatorControllerを、アバター階層内の特定のGameObjectに存在する
        /// ModularAvatarMergeAnimatorコンポーネントに割り当てます。
        /// </summary>
        /// <param name="avatarRoot">アバターのルートGameObject (VRCAvatarDescriptorを持つオブジェクト)。</param>
        /// <param name="controllerToLink">ModularAvatarMergeAnimatorに割り当てるAnimatorController。</param>
        /// <param name="mergeTargetParentGOName">ModularAvatarMergeAnimatorがアタッチされるGameObjectの親のパスセグメント名。</param>
        /// <param name="mergeTargetChildGOName">ModularAvatarMergeAnimatorがアタッチされる最終的なGameObjectのパスセグメント名。</param>
        /// <param name="layerType">ModularAvatarMergeAnimatorのLayer Type。</param>
        /// <returns>操作が成功した場合はtrue、失敗した場合はfalse。</returns>
        public static bool LinkAnimatorToMergeAnimator(
            GameObject avatarRoot,
            AnimatorController controllerToLink,
            string mergeTargetParentGOName,
            string mergeTargetChildGOName,
            VRCAvatarDescriptor.AnimLayerType layerType = VRCAvatarDescriptor.AnimLayerType.FX)
        {
            if (avatarRoot == null || controllerToLink == null)
            {
                Debug.LogError("[ModularAvatarLinkerUtility] アバターのルートまたは割り当てるAnimatorControllerがnullです。");
                return false;
            }

            GameObject maTargetParent = FindChildGameObjectRecursive(avatarRoot, mergeTargetParentGOName);
            GameObject maTargetGo = null;

            if (maTargetParent != null)
            {
                maTargetGo = FindChildGameObjectRecursive(maTargetParent, mergeTargetChildGOName);
            }

            if (maTargetGo != null)
            {
                // 既存の ModularAvatarMergeAnimator コンポーネントを取得、なければ追加
                ModularAvatarMergeAnimator maMergeAnimator = maTargetGo.GetComponent<ModularAvatarMergeAnimator>();
                if (maMergeAnimator == null)
                {
                    maMergeAnimator = maTargetGo.AddComponent<ModularAvatarMergeAnimator>();
                    Debug.Log($"[ModularAvatarLinkerUtility] Added new ModularAvatarMergeAnimator to '{maTargetGo.name}'.", maTargetGo);
                }
                else
                {
                    Debug.Log($"[ModularAvatarLinkerUtility] Found existing ModularAvatarMergeAnimator on '{maTargetGo.name}'. Updating it.", maTargetGo);
                }

                // 生成した AnimatorController を割り当てる
                maMergeAnimator.animator = controllerToLink;
                maMergeAnimator.layerType = layerType;
                maMergeAnimator.deleteAttachedAnimator = true; // 必要に応じて設定
                maMergeAnimator.pathMode = MergeAnimatorPathMode.Absolute; // 必要に応じて設定

                // 変更を Unity に通知し、保存されるようにマーク
                EditorUtility.SetDirty(maMergeAnimator);
                EditorUtility.SetDirty(maTargetGo); // GameObject自体が変更されたことをマーク

                Debug.Log($"[ModularAvatarLinkerUtility] Assigned generated Animator Controller '{controllerToLink.name}' to ModularAvatarMergeAnimator on '{maTargetGo.name}'.", maTargetGo);
                return true;
            }
            else
            {
                Debug.LogWarning($"[ModularAvatarLinkerUtility] Target GameObject for ModularAvatarMergeAnimator not found. Expected path: '{mergeTargetParentGOName}/{mergeTargetChildGOName}' under avatar root. Please ensure the GameObject exists.", avatarRoot);
                EditorUtility.DisplayDialog("警告", $"Modular Avatar Merge Animator を設定するターゲット GameObject '{mergeTargetParentGOName}/{mergeTargetChildGOName}' が見つかりませんでした。 GameObjectが存在するか確認してください。", "OK");
                return false;
            }
        }

        /// <summary>
        /// 指定されたGameObjectの子階層から、特定の名前のGameObjectを再帰的に検索するヘルパー関数。
        /// 非アクティブなオブジェクトも検索対象に含みます。
        /// </summary>
        /// <param name="parent">検索を開始する親GameObject。</param>
        /// <param name="nameToFind">検索するGameObjectの名前。</param>
        /// <returns>見つかったGameObject、または見つからなかった場合はnull。</returns>
        public static GameObject FindChildGameObjectRecursive(GameObject parent, string nameToFind)
        {
            if (parent == null) return null;

            // Transform.Findはアクティブな子しか検索しないため、GetComponentsInChildren<Transform>(true) を使用して全ての子を検索
            Transform[] children = parent.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in children)
            {
                if (child.name.Equals(nameToFind) && child.parent == parent.transform) // 直接の子であることを確認
                {
                    return child.gameObject;
                }
            }

            // 直接の子で見つからなければ、すべての子孫を再帰的に検索
            foreach (Transform child in children)
            {
                if (child != parent.transform) // 自身を除外
                {
                    if (child.name.Equals(nameToFind))
                    {
                        return child.gameObject;
                    }
                }
            }
            return null;
        }
    }
}
