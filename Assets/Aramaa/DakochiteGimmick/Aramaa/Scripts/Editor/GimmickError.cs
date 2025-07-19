namespace Aramaa.DakochiteGimmick.Editor
{
    public enum GimmickError : int
    {
        None = 1000,
        /// <summary>
        /// アバターのルートオブジェクトがnullです。
        /// </summary>
        AvatarRootIsNull,
        /// <summary>
        /// アバターにVRCAvatarDescriptorが見つかりません。
        /// </summary>
        AvatarDescriptorNotFound,
        /// <summary>
        /// アバターのルートオブジェクトの座標が(0, 0, 0)ではありません。
        /// </summary>
        AvatarRootPositionIsNotZero,
        /// <summary>
        /// アバターのArmatureにHipsボーンが見つかりません。
        /// </summary>
        HipsBoneNotFound,
        /// <summary>
        /// アバターのArmatureにHipsボーンが見つかりません。
        /// </summary>
        HeadBoneNotFound,
        /// <summary>
        /// 必要なギミックプレハブが見つかりません。
        /// </summary>
        GimmickPrefabNotFound,
        /// <summary>
        /// ギミックプレハブのインスタンス生成に失敗しました。
        /// </summary>
        GimmickPrefabInstantiationFailed,
        /// <summary>
        /// ギミックプレハブのデータが破損している可能性があります。
        /// </summary>
        GimmickPrefabCorrupted,
        /// <summary>
        /// 処理中にギミックのインスタンスがヒエラルキーから削除されました。
        /// </summary>
        GimmickInstanceRemovedDuringSetup,
        /// <summary>
        /// 処理中にアバターオブジェクト自体がヒエラルキーから削除されました。
        /// <br/>解決策：
        /// </summary>
        AvatarObjectRemovedDuringSetup,
        /// <summary>
        /// PhysBone Hold Gimmickの設定スクリプトの実行に失敗しました。
        /// </summary>
        PhysBoneSetupExecutionFailed,
        /// <summary>
        /// ギミック内のEyeOffsetTransformオブジェクトが見つかりませんでした。
        /// </summary>
        EyeOffsetTransformNotFoundInGimmick,
        /// <summary>
        /// 処理中にアバターのルートオブジェクトからVRCAvatarDescriptorが削除された可能性があります。
        /// <br/>解決策：ギミック生成中は、アバターやヒエラルキーの操作を控えてください。
        /// </summary>
        AvatarDescriptorRemovedDuringSetup,
        /// <summary>
        /// EyeOffsetの調整に必要なHeadボーンが見つかりません。
        /// <br/>解決策：アバターのAnimator設定（Humanoidボーンマップ）を確認してください。（Headのないアバターでは正確なEyeOffset調整ができません）
        /// </summary>
        EyeOffsetHeadBoneNotFound,
        /// <summary>
        /// VRCParentConstraintの更新がタイムアウトしたか、正しく設定されませんでした。
        /// <br/>解決策：もう一度ギミックを生成し直してください。コンソールにMissingReferenceExceptionのようなエラーがある場合は、Unityを再起動してから再度お試しください。
        /// </summary>
        ConstraintUpdateFailedOrTimedOut,
        /// <summary>
        /// アセット出力フォルダの作成に失敗した場合
        /// </summary>
        OutputFolderCreationFailed,
        /// <summary>
        /// アニメーションクリップの生成に失敗した場合
        /// </summary>
        AnimationClipCreationFailed,
        /// <summary>
        /// Animator Controllerの作成に失敗した場合
        /// </summary>
        AnimatorControllerCreationFailed,
        /// <summary>
        /// Modular Avatar Merge Animator へのリンクに失敗した場合
        /// </summary>
        ModularAvatarLinkFailed
    }
}
