namespace Aramaa.DakochiteGimmick
{
    /// <summary>
    /// ギミック設定ツールで使用される各種定数を定義する静的クラス。
    /// </summary>
    public static class GimmickConstants
    {
        public const string VER = "ver 1.0.0";

        // 説明書
        public const string DOCUMENTATION_URL = "https://docs.google.com/document/d/141h1qxOo8ZeFPDXLFmx2fjn6jsYxf7dL6XJkSFxztec/edit?usp=sharing";
        public const string DOCUMENTATION_TEXT = "説明書";

        // ====================================================================================================
        // UI / メニュー
        // ====================================================================================================
        public static readonly string WINDOW_TITLE = $"{VER} dakochite gimmick みんなでつかめるだこちてギミック";
        public const string MENU_PATH = "Aramaa/dakochite gimmick みんなでつかめるだこちてギミック";
        public const string BUTTON_GENERATE_OR_REGENERATE_TEXT = "ギミックを生成 / 削除";
        public const string BUTTON_GENERATE_OR_REGENERATE_TOOLTIP = "選択したアバターにギミックを生成します。既にギミックがある場合は削除されます。";
        public const string DIALOG_SUCCESS_TITLE = "完了";
        public const string DIALOG_CANCELED_TITLE = "キャンセル";

        // ====================================================================================================
        // アセットパス / 階層
        // ====================================================================================================
        /// <summary>
        /// Unityプロジェクト内で生成するプレハブのアセットパス。
        /// </summary>
        public const string GIMMICK_PREFAB_PATH = "Aramaa/HoldGimick/Prefabs/HoldGimickAndCamera";

        /// <summary>
        /// 生成されるギミックプレハブのルートオブジェクトからの相対パスで、
        /// VRCParentConstraintがアタッチされているGameObjectへのパス。
        /// </summary>
        public const string CONSTRAINT_PATH_INSIDE_PREFAB = "Objects/BoneToHips/Constraint";

        /// <summary>
        /// 生成されるギミックプレハブのルートオブジェクトからの相対パスで、
        /// EyeOffsetオブジェクトへのパス。
        /// </summary>
        public const string EYEOFFSET_PATH_INSIDE_PREFAB = "Objects/Camera/Constraint/EyeOffset";

        /// <summary>
        /// PhysBoneアニメーション関連のアセットを生成するベース出力パス。
        /// </summary>
        public const string PHYSBONE_OUTPUT_BASE_PATH = "Assets/Aramaa/GeneratedAssets/HoldGimick/PhysBoneToggleAnimatorGenerator";

        /// <summary>
        /// Modular Avatar Merge AnimatorがアタッチされるGameObjectの親のパスセグメント名。
        /// </summary>
        public const string MA_TARGET_PARENT_GO_NAME = "HoldGimickAndCamera";

        /// <summary>
        /// Modular Avatar Merge Animatorがアタッチされる最終的なGameObjectのパスセグメント名。
        /// </summary>
        public const string MA_TARGET_CHILD_GO_NAME = "PhysBoneHoldGimickGenerator";

        // ====================================================================================================
        // PhysBone Automation 関連
        // ====================================================================================================
        public const string PHYSBONE_PARAMETER_NAME = "HoldType"; // ここを "HoldType" に変更
        public static readonly string[] PHYSBONE_EXCLUDE_PATH_SEGMENTS = {
            "HoldGimickAndCamera",
            "DressUpCloset",
            "DressUpClosetLite"
        };
        public const string PHYSBONE_PROPERTY_NAME = "allowGrabbing";
        public const float ADVANCED_BOOL_FALSE_VALUE = 0.0f;
        public const float ADVANCED_BOOL_TRUE_VALUE = 1e-45f; // VRCPhysBoneのAdvancedBool.Trueに相当

        // ====================================================================================================
        // メッセージ (ユーザー向け)
        // ====================================================================================================
        public const string MSG_AVATAR_NOT_SELECTED = "アバターが選択されていません。";
        public const string MSG_HIPS_NOT_FOUND = "アバターのHipsボーンが見つかりません。Animatorの設定を確認してください。";
        public const string MSG_PREFAB_NOT_FOUND = "ギミックの元となるプレハブが見つかりません。ツールが正しくインポートされているか確認してください。";
        public const string MSG_PREFAB_INSTANTIATION_FAILED = "プレハブの生成に失敗しました。";
        public const string MSG_CONSTRAINT_NOT_FOUND = "生成されたギミック内で必要なコンポーネントが見つかりません。プレハブが破損している可能性があります。";
        public const string MSG_AVATARDESCRIPTOR_NOT_FOUND = "アバターにVRCAvatarDescriptorが見つかりません。EyeOffsetの設定をスキップしました。";
        public const string MSG_EYEOFFSET_NOT_FOUND = "EyeOffsetオブジェクトが見つかりませんでした。EyeOffsetの設定をスキップしました。";
        public const string MSG_HEAD_NOT_FOUND = "Headボーンが見つからないため、EyeOffsetの位置設定をスキップしました。";
        public const string MSG_EXISTING_GIMMICK_DELETED = "みんなでつかめるだこちてギミックを\nアバターから削除しました。";
        public const string MSG_PROCESS_CANCELED = "処理がキャンセルされました。";
        public const string MSG_PHYSBONE_NOT_FOUND_FOR_ANIMATION = "除外パスを考慮した結果、アニメーション対象のPhysBoneが見つかりませんでした。PhysBoneが存在し、除外パスの下にないことを確認してください。";
        public const string MSG_ANIMATION_CLIP_GENERATION_FAILED = "アニメーションクリップの生成に失敗しました。詳細についてはコンソールを確認してください。";
        public const string MSG_ANIMATOR_CONTROLLER_CREATION_FAILED = "Animator Controllerの作成に失敗しました。";
        public const string MSG_MA_LINK_FAILED = "Modular Avatar Merge Animator のリンクに失敗しました。";
        public const string MSG_EXPRESSION_PARAM_EXISTS_WRONG_TYPE = "パラメータ '{0}' は既に存在しますが、Bool型ではありません。手動で確認してください。";
        public const string MSG_EXPRESSION_PARAM_EXISTS_INFO = "パラメータ '{0}' は既に存在します。初期状態を 'Grab_Default' にするには、'Default' 値が 0 (false) に設定されていることを確認してください。";
        public const string MSG_CONSTRAINT_UPDATE_FAILED_PARENT_POS_ZERO = "VRCParentConstraintが正しく更新されていません。削除して生成してください";
        public const string MSG_EYEOFFSET_HEAD_BONE_MISSING = "EyeOffsetの調整に必要なHeadボーンが見つからないため、正確な調整ができませんでした。";

        // ====================================================================================================
        // ログメッセージ (詳細、開発者向け)
        // ====================================================================================================
        public const string LOG_AVATAR_ROOT_NULL = "[Error] アバターのルートオブジェクトがnullです。";
        public const string LOG_ANIMATOR_NOT_FOUND = "[Warning] アバター '{0}' にAnimatorコンポーネントが見つかりません。";
        public const string LOG_ANIMATOR_NOT_HUMANOID = "[Warning] アバター '{0}' のAnimatorはHumanoid型ではありません。";
        public const string LOG_PREFAB_NOT_FOUND_AT_PATH = "[Error] 指定されたプレハブが見つかりません: {0}";
        public const string LOG_PREFAB_INSTANTIATION_FAILED = "[Error] プレハブのインスタンス化に失敗しました。";
        public const string LOG_CONSTRAINT_NOT_FOUND_IN_PREFAB = "[Error] 新しく生成されたギミック '{0}' のパス ({1}) にVRCParentConstraintが見つかりません。";
        public const string LOG_AVATARDESCRIPTOR_NOT_FOUND = "[Warning] アバターのルートオブジェクトに VRCAvatarDescriptor コンポーネントが見つかりません。EyeOffsetの位置設定をスキップします。";
        public const string LOG_EYEOFFSET_ADJUSTED = "EyeOffsetオブジェクトをVRCAvatarDescriptorのView位置とHeadボーンの反転回転に基づいて修正しました。 LocalPos: {0}, WorldRot: {1}, LocalScale: {2}";
        public const string LOG_EYEOFFSET_ADJUSTMENT_SKIPPED_NULL = "アバターまたはギミックのインスタンスがnullのため、EyeOffset調整をスキップしました。";
        public const string LOG_CONSTRAINT_GO_NOT_FOUND = "LogConstraintPosition: Constraint GameObjectが見つかりませんでした。";
        public const string LOG_GIMMICK_INSTANCE_NOT_FOUND = "LogConstraintPosition: ギミックプレハブのインスタンス '{0}' がアバター直下に見つかりませんでした。";
        public const string LOG_MA_LINKER_NULL_PARAMS = "[MA Linker] アバターのルートまたは割り当てるAnimatorControllerがnullです。";
        public const string LOG_MA_LINKER_ADDED_COMPONENT = "[MA Linker] Added new ModularAvatarMergeAnimator to '{0}'.";
        public const string LOG_MA_LINKER_FOUND_EXISTING = "[MA Linker] Found existing ModularAvatarMergeAnimator on '{0}'. Updating it.";
        public const string LOG_MA_LINKER_ASSIGNED_CONTROLLER = "[MA Linker] Assigned generated Animator Controller '{0}' to ModularAvatarMergeAnimator on '{1}'.";
        public const string LOG_MA_LINKER_TARGET_NOT_FOUND = "[MA Linker] Target GameObject for ModularAvatarMergeAnimator not found. Expected path: '{0}/{1}' under avatar root.";
        public const string LOG_ANIMATION_CURVE_SETUP = "[Animation Curve Setup] Clip: '{0}', GameObject Path: '{1}', Property: '{2}', Set Value: {3} (Expected Bool State: {4})";
        public const string LOG_ANIMATION_CURVE_INITIAL_STATE = "[Animation Curve Setup] For '{0}', PhysBone '{1}' initial allowGrabbing: {2}, setting to: {3}";
        public const string LOG_ANIMATION_CURVE_FALSE_STATE = "[Animation Curve Setup] For '{0}', PhysBone '{1}' setting to: {2} (False)";
        public const string LOG_GENERATED_FULL_PATH_EMPTY = "[PhysBoneGimmickAutomation] Generated fullPath is empty for clip '{0}'. Output Path: '{1}'";
        public const string LOG_ADDED_EXPRESSION_PARAM = "[PhysBoneGimmickAutomation] Added '{0}' parameter to VRCExpressionParameters: {1}";

        public const string LOG_EYEOFFSET_ADJUSTMENT_SUMMARY = "<color=cyan>--- EyeOffset Adjustment Summary ---</color>\n" +
                                                               "<color=cyan>Target View Position (Avatar Local): {0}</color>\n" +
                                                               "<color=cyan>Actual EyeOffset World Position: {1}</color>\n" +
                                                               "<color=cyan>Actual EyeOffset World Rotation: {2}</color>\n" +
                                                               "<color=cyan>----------------------------------</color>";

    }
}
