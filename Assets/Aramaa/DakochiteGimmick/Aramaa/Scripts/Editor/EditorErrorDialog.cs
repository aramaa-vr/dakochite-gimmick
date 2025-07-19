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
        public static void DisplayDialog(GimmickError gimmickError, string param = "")
        {
            var (message, solutionSuggestion) = ErrorInfo.Get(gimmickError);

            string displayMessage = "内容\n";
            displayMessage += $"{message}\n";
            if (param != string.Empty)
            {
                displayMessage += $"{param}\n";
            }
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
            displayMessage += $"エラーコード: {(int)gimmickError}";

            EditorUtility.DisplayDialog("エラーが発生しました", displayMessage, "OK");

            Debug.LogError($"[エラーコード: {(int)gimmickError}] {message}");
        }
    }
}
