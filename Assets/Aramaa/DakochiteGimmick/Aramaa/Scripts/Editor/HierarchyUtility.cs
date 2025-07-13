using UnityEngine;
using VRC.SDK3.Dynamics.Constraint.Components;

namespace Aramaa.DakochiteGimmick.Editor
{
    /// <summary>
    /// UnityのGameObject階層内を検索するためのユーティリティメソッドを提供します。
    /// 非アクティブなオブジェクトも検索対象に含みます。
    /// </summary>
    public static class HierarchyUtility
    {
        /// <summary>
        /// 指定された親のTransformから、指定された名前の子GameObjectを再帰的に検索します。
        /// 非アクティブなGameObjectも検索対象に含みます。
        /// </summary>
        /// <param name="parent">検索を開始する親のTransform。</param>
        /// <param name="childName">検索する子GameObjectの名前。</param>
        /// <returns>見つかった子GameObjectのTransform。見つからない場合はnull。</returns>
        public static Transform FindChildRecursive(Transform parent, string childName)
        {
            if (parent == null || string.IsNullOrEmpty(childName))
            {
                return null;
            }

            // GetComponentsInChildren(true) を使用して、非アクティブな子も含めて全ての子孫を検索
            foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
            {
                if (child.name.Equals(childName))
                {
                    // 親との関連性は問わず、名前が一致する最初の子孫を返す
                    return child;
                }
            }
            return null;
        }

        /// <summary>
        /// 指定された親のTransformから、相対パスで指定されたTransformを検索します。
        /// 非アクティブなGameObjectも検索対象に含みます。
        /// 例: "Objects/Camera/Constraint/EyeOffset"
        /// </summary>
        /// <param name="parent">検索を開始する親のTransform。</param>
        /// <param name="relativePath">検索するTransformへの相対パス。</param>
        /// <returns>見つかったTransform。見つからない場合はnull。</returns>
        public static Transform FindChildTransformByRelativePath(Transform parent, string relativePath)
        {
            if (parent == null || string.IsNullOrEmpty(relativePath))
            {
                return null;
            }

            Transform currentTransform = parent;
            string[] pathParts = relativePath.Split('/');

            foreach (string part in pathParts)
            {
                if (currentTransform == null)
                {
                    return null; // 途中でパスが見つからなくなった
                }

                Transform nextTransform = null;
                // 非アクティブな子も含むために、直接の子をイテレートして名前で検索
                // Transform.Find(string path) はアクティブな子オブジェクトのみを検索するため、
                // 非アクティブなオブジェクトも検索できるよう、手動でイテレートして名前比較を行います。
                foreach (Transform child in currentTransform)
                {
                    if (child.name.Equals(part))
                    {
                        nextTransform = child;
                        break;
                    }
                }

                currentTransform = nextTransform;
            }
            return currentTransform;
        }

        /// <summary>
        /// 指定されたプレハブインスタンスのルートから、相対パスで指定されたGameObject内のVRCParentConstraintを検索します。
        /// 非アクティブなGameObjectも検索対象に含みます。
        /// </summary>
        /// <param name="prefabInstanceRoot">プレハブインスタンスのルートTransform。</param>
        /// <param name="constraintPathInsidePrefab">VRCParentConstraintがアタッチされているGameObjectへの相対パス。</param>
        /// <returns>見つかったVRCParentConstraintコンポーネント。見つからない場合はnull。</returns>
        public static VRCParentConstraint FindConstraintInHierarchy(Transform prefabInstanceRoot, string constraintPathInsidePrefab)
        {
            if (prefabInstanceRoot == null || string.IsNullOrEmpty(constraintPathInsidePrefab))
            {
                return null;
            }

            Transform constraintTransform = FindChildTransformByRelativePath(prefabInstanceRoot, constraintPathInsidePrefab);
            if (constraintTransform != null)
            {
                return constraintTransform.GetComponent<VRCParentConstraint>();
            }
            return null;
        }
    }
}
