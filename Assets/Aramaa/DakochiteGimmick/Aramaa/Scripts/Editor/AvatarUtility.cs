using UnityEngine;

namespace Aramaa.DakochiteGimmick.Editor
{
    /// <summary>
    /// VRChatアバターのHumanoidボーン関連のユーティリティメソッドを提供します。
    /// </summary>
    public static class AvatarUtility
    {
        /// <summary>
        /// 指定されたアバターのルートオブジェクトからHipsボーンのTransformを取得します。
        /// アバターにAnimatorコンポーネントがアタッチされており、Humanoid型である必要があります。
        /// </summary>
        /// <param name="avatarRootObject">アバターのルートGameObject。</param>
        /// <returns>HipsボーンのTransform。見つからない場合はnull。</returns>
        public static Transform GetAnimatorHipsBone(GameObject avatarRootObject)
        {
            if (avatarRootObject == null)
            {
                Debug.LogError(GimmickConstants.LOG_AVATAR_ROOT_NULL);
                return null;
            }

            Animator animator = avatarRootObject.GetComponent<Animator>();
            if (animator == null)
            {
                Debug.LogWarning(string.Format(GimmickConstants.LOG_ANIMATOR_NOT_FOUND, avatarRootObject.name));
                return null;
            }

            if (!animator.isHuman)
            {
                Debug.LogWarning(string.Format(GimmickConstants.LOG_ANIMATOR_NOT_HUMANOID, avatarRootObject.name));
                return null;
            }

            return animator.GetBoneTransform(HumanBodyBones.Hips);
        }

        /// <summary>
        /// 指定されたアバターのルートオブジェクトからHeadボーンのTransformを取得します。
        /// アバターにAnimatorコンポーネントがアタッチされており、Humanoid型である必要があります。
        /// </summary>
        /// <param name="avatarRootObject">アバターのルートGameObject。</param>
        /// <returns>HeadボーンのTransform。見つからない場合はnull。</returns>
        public static Transform GetAnimatorHeadBone(GameObject avatarRootObject)
        {
            if (avatarRootObject == null)
            {
                Debug.LogError(GimmickConstants.LOG_AVATAR_ROOT_NULL);
                return null;
            }

            Animator animator = avatarRootObject.GetComponent<Animator>();
            if (animator == null)
            {
                Debug.LogWarning(string.Format(GimmickConstants.LOG_ANIMATOR_NOT_FOUND, avatarRootObject.name));
                return null;
            }

            if (!animator.isHuman)
            {
                Debug.LogWarning(string.Format(GimmickConstants.LOG_ANIMATOR_NOT_HUMANOID, avatarRootObject.name));
                return null;
            }

            return animator.GetBoneTransform(HumanBodyBones.Head);
        }
    }
}
