using UnityEngine;
using System.Collections.Generic;

namespace Aramaa.DakochiteGimmick.Editor
{
    public static class ErrorInfo
    {
        // 内部でエラー情報をIDをキーとして保持するDictionary
        // ここでは、IDをキーとして、メッセージと解決策の提案をValueTupleとして保持します。
        // readonlyキーワードにより、静的コンストラクタでのみ初期化が可能
        private static readonly Dictionary<GimmickError, (string message, string solutionSuggestion)> _errorDetailsMap;

        // 静的コンストラクタで、エラー情報をDictionaryに直接ハードコードで定義・登録します。
        static ErrorInfo()
        {
            _errorDetailsMap = new Dictionary<GimmickError, (string message, string solutionSuggestion)>
            {
                {
                    GimmickError.AvatarRootIsNull,
                    (
                        message: "アバターのルートオブジェクトがnullです。",
                        solutionSuggestion: "アバターをドラッグ＆ドロップして指定してください。"
                    )
                },
                {
                    GimmickError.AvatarDescriptorNotFound,
                    (
                        message: "アバターにVRCAvatarDescriptorが見つかりません。",
                        solutionSuggestion: "アバターをドラッグ＆ドロップして指定してください。"
                    )
                },
                {
                    GimmickError.AvatarRootPositionIsNotZero,
                    (
                        message: "アバターのルートオブジェクトの座標が(0, 0, 0)ではありません。",
                        solutionSuggestion: "アバターのルートの座標を（0, 0, 0）にしてください"
                    )
                },
                {
                    GimmickError.HipsBoneNotFound,
                    (
                        message: "アバターのArmatureにHipsボーンが見つかりません。",
                        solutionSuggestion: "Hipsのないアバターは利用できません。"
                    )
                },
                {
                    GimmickError.HeadBoneNotFound,
                    (
                        message: "アバターのArmatureにHeadボーンが見つかりません。",
                        solutionSuggestion: "Headのないアバターは利用できません。"
                    )
                },
                {
                    GimmickError.GimmickPrefabNotFound,
                    (
                        message: "必要なギミックプレハブが見つかりません。",
                        solutionSuggestion:  "Unityの「Window」メニューから「VRChat SDK」>「Manage Packages」を開き、「dakochite-gimmick - みんなでつかめるだこちてギミック」を**一度アンインストールしてから再インストール**してください。"
                    )
                },
                {
                    GimmickError.GimmickPrefabInstantiationFailed,
                    (
                        message: "ギミックプレハブのインスタンス生成に失敗しました。",
                        solutionSuggestion:  "Unityの「Window」メニューから「VRChat SDK」>「Manage Packages」を開き、「dakochite-gimmick - みんなでつかめるだこちてギミック」を**一度アンインストールしてから再インストール**してください。"
                    )
                },
                {
                    GimmickError.GimmickPrefabCorrupted,
                    (
                        message: "ギミックプレハブのデータが破損している可能性があります。",
                        solutionSuggestion:  "Unityの「Window」メニューから「VRChat SDK」>「Manage Packages」を開き、「dakochite-gimmick - みんなでつかめるだこちてギミック」を**一度アンインストールしてから再インストール**してください。"
                    )
                },
                {
                    GimmickError.GimmickInstanceRemovedDuringSetup,
                    (
                        message: "処理中にギミックのインスタンスがヒエラルキーから削除されました。",
                        solutionSuggestion:  "ギミック生成中は、アバターやヒエラルキーの操作を控えてください。"
                    )
                },
                {
                    GimmickError.AvatarObjectRemovedDuringSetup,
                    (
                        message: "処理中にアバターオブジェクト自体がヒエラルキーから削除されました。",
                        solutionSuggestion:  "ギミック生成中は、アバターやヒエラルキーの操作を控えてください。"
                    )
                },
                {
                    GimmickError.PhysBoneSetupExecutionFailed,
                    (
                        message: "PhysBone Hold Gimmickの設定スクリプトの実行に失敗しました。",
                        solutionSuggestion:  ""
                    )
                },
                {
                    GimmickError.EyeOffsetTransformNotFoundInGimmick,
                    (
                        message: "ギミック内のEyeOffsetTransformオブジェクトが見つかりませんでした。",
                        solutionSuggestion:  "Unityの「Window」メニューから「VRChat SDK」>「Manage Packages」を開き、「dakochite-gimmick - みんなでつかめるだこちてギミック」を**一度アンインストールしてから再インストール**してください。"
                    )
                },
                {
                    GimmickError.AvatarDescriptorRemovedDuringSetup,
                    (
                        message: "処理中にアバターのルートオブジェクトからVRCAvatarDescriptorが削除された可能性があります。",
                        solutionSuggestion:  "ギミック生成中は、アバターやヒエラルキーの操作を控えてください。"
                    )
                },
                {
                    GimmickError.EyeOffsetHeadBoneNotFound,
                    (
                        message: "EyeOffsetの調整に必要なHeadボーンが削除された可能性があります。",
                        solutionSuggestion:  "ギミック生成中は、アバターやヒエラルキーの操作を控えてください。"
                    )
                },
                {
                    GimmickError.ConstraintUpdateFailedOrTimedOut,
                    (
                        message: "VRCParentConstraintの更新がタイムアウトしたか、正しく設定されませんでした。",
                        solutionSuggestion:  "もう一度ギミックを生成し直してください。コンソールにMissingReferenceExceptionのようなエラーがある場合は、Unityを再起動してから再度お試しください。"
                    )
                }
            };
        }

        /// <summary>
        /// 指定されたIDのエラーメッセージと解決策の提案を取得します。
        /// </summary>
        public static (string message, string solutionSuggestion) Get(GimmickError gimmickError)
        {
            if (_errorDetailsMap.TryGetValue(gimmickError, out var errorDetails))
            {
                return errorDetails;
            }
            Debug.LogWarning($"[ErrorInfo] エラーID '{gimmickError}' は見つかりませんでした。");
            return (string.Empty, string.Empty); // 見つからない場合はnullを返す
        }
    }
}
