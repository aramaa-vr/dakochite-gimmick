using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace Aramaa.DakochiteGimmick.Editor
{
    /// <summary>
    /// Unityエディタ内でエラーダイアログを表示し、デバッグログに出力するユーティリティ。
    /// </summary>
    public static class EditorErrorDialog
    {
        /// <summary>
        /// エラーダイアログを表示し、関連情報を出力します。
        /// </summary>
        public static void DisplayDialog(string errorCode)
        {
            var (message, solutionSuggestion) = ErrorInfo.Get(errorCode);

            string displayMessage = "内容\n";
            displayMessage += $"{message}\n";
            displayMessage += "\n";
            displayMessage += "対応方法\n";
            displayMessage += $"{solutionSuggestion}\n";
            displayMessage += "\n";
            displayMessage += "その他\n";
            displayMessage += "同じエラーが続く場合、Unityを再起動してください。\n";
            displayMessage += "再起動しても解決しない場合\n";
            displayMessage += "\n";
            displayMessage += "Unityのスクショと詳しい状況を添えて、\n";
            displayMessage += "Boothへご連絡ください。\n";
            displayMessage += "\n";
            displayMessage += $"エラーコード: {errorCode}";

            EditorUtility.DisplayDialog("エラーが発生しました", displayMessage, "OK");

            Debug.LogError($"[エラーコード: {errorCode}] {message}");
        }
    }

    public static class ErrorInfo
    {
        public const string CODE_001 = "001";
        public const string CODE_002 = "002";
        public const string CODE_003 = "003";
        public const string CODE_004 = "004";
        public const string CODE_005 = "005";
        public const string CODE_006 = "006";
        public const string CODE_007 = "007";
        public const string CODE_008 = "008";
        public const string CODE_009 = "009";
        public const string CODE_010 = "010";
        public const string CODE_011 = "011";
        public const string CODE_012 = "012";
        public const string CODE_013 = "013";
        public const string CODE_014 = "014";

        // 内部でエラー情報をIDをキーとして保持するDictionary
        // ここでは、IDをキーとして、メッセージと解決策の提案をValueTupleとして保持します。
        // readonlyキーワードにより、静的コンストラクタでのみ初期化が可能
        private static readonly Dictionary<string, (string message, string solutionSuggestion)> _errorDetailsMap;

        // 静的コンストラクタで、エラー情報をDictionaryに直接ハードコードで定義・登録します。
        static ErrorInfo()
        {
            _errorDetailsMap = new Dictionary<string, (string message, string solutionSuggestion)>
            {
                {
                    CODE_001,
                    (
                        message: "アバターが選択されていません。",
                        solutionSuggestion: "アバターをドラッグ＆ドロップして指定してください。"
                    )
                },
                {
                    CODE_014,
                    (
                        message: "アバターのルートにVRCAvatarDescriptorがありません。",
                        solutionSuggestion:  "アバターを選択しているか確認してください。"
                    )
                },
                {
                    CODE_002,
                    (
                        message: "アバターのルートの座標が（0, 0, 0）ではありません",
                        solutionSuggestion: "アバターのルートの座標を（0, 0, 0）にしてください"
                    )
                },
                {
                    CODE_003,
                    (
                        message: "Hipsボーンが見つかりません。",
                        solutionSuggestion: "Animatorの設定を確認してください。\n（Hipsのないアバターは利用できません）"
                    )
                },
                {
                    CODE_004,
                    (
                        message: " プレハブが見つかりません。",
                        solutionSuggestion:  $"Manage Packagesからdakochite-gimmick - みんなでつかめるだこちてギミックをアンインストール、インストール"
                    )
                },
                {
                    CODE_005,
                    (
                        message: "プレハブの生成に失敗しました。",
                        solutionSuggestion:  $"Manage Packagesからdakochite-gimmick - みんなでつかめるだこちてギミックをアンインストール、インストール"
                    )
                },
                {
                    CODE_006,
                    (
                        message: "プレハブが破損している可能性があります。",
                        solutionSuggestion:  $"Manage Packagesからdakochite-gimmick - みんなでつかめるだこちてギミックをアンインストール、インストール"
                    )
                },
                {
                    CODE_007,
                    (
                        message: "PhysBone Hold Gimmick Setupの実行に失敗しました。",
                        solutionSuggestion: ""
                    )
                },
                {
                    CODE_008,
                    (
                        message: "アバターのルートからVRCAvatarDescriptorが削除された可能性があります。",
                        solutionSuggestion:  $"生成中は操作しないでください"
                    )
                },
                {
                    CODE_009,
                    (
                        message: "EyeOffsetの調整に必要なHeadボーンが見つからりません。",
                        solutionSuggestion: "Animatorの設定を確認してください。\n（Headのないアバターは利用できません）"
                    )
                },
                {
                    CODE_010,
                    (
                        message: "タイムアウトしました。\nVRCParentConstraintが正しく更新されていません。",
                        solutionSuggestion: "もう一度、生成してください。\n\nコンソールにMissingReferenceExceptionのエラーがある場合はunityを再起動してください。"
                    )
                },
                {
                    CODE_011,
                    (
                        message: "eyeOffsetTransformが見つかりませんでした。",
                        solutionSuggestion:  $"Manage Packagesからdakochite-gimmick - みんなでつかめるだこちてギミックをアンインストール、インストールしなおしてください"
                    )
                },
                {
                    CODE_012,
                    (
                        message: "ギミックが削除された可能性があります。",
                        solutionSuggestion:  $"生成中は操作しないでください"
                    )
                },
                {
                    CODE_013,
                    (
                        message: "アバターが削除された可能性があります。",
                        solutionSuggestion:  $"生成中は操作しないでください"
                    )
                }
            };
        }

        /// <summary>
        /// 指定されたIDのエラーメッセージと解決策の提案を取得します。
        /// </summary>
        /// <param name="id">取得するエラーのID。</param>
        /// <returns>指定されたIDのメッセージと解決策の提案を含むValueTuple。見つからない場合はnullを返します。</returns>
        public static (string message, string solutionSuggestion) Get(string id) // Nullable ValueTupleを返す
        {
            if (_errorDetailsMap.TryGetValue(id, out var errorDetails))
            {
                return errorDetails;
            }
            Debug.LogWarning($"[ErrorInfo] エラーID '{id}' は見つかりませんでした。");
            return (string.Empty, string.Empty); // 見つからない場合はnullを返す
        }
    }
}
