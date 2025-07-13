using UnityEngine;

namespace Aramaa.DakochiteGimmick.Editor
{
    /// <summary>
    /// アニメーション関連のユーティリティメソッドを提供します。
    /// </summary>
    public static class AnimationUtilityExtension
    {
        /// <summary>
        /// GameObjectのルートからの相対パスを取得するヘルパー関数。
        /// AnimatorのAnimationCurveのパス設定などに使用されます。
        /// </summary>
        /// <param name="gameObject">パスを取得するGameObject。</param>
        /// <param name="rootTransform">パスの基準となるルートTransform。</param>
        /// <returns>ルートからの相対パス文字列。</returns>
        public static string GetGameObjectPath(GameObject gameObject, Transform rootTransform)
        {
            string path = gameObject.name;
            Transform current = gameObject.transform;
            while (current != null && current != rootTransform)
            {
                if (current.parent != null && current.parent != rootTransform)
                {
                    path = current.parent.name + "/" + path;
                }
                current = current.parent;
            }
            return path;
        }
    }
}
