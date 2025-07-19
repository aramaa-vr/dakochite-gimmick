using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Aramaa.DakochiteGimmick.Editor
{
    /// <summary>
    /// VRChatアバターに特定のプレハブを生成し、そのプレハブ内のVRCParentConstraintのTarget Transformを
    /// アバターのHipsボーンに自動設定し、さらにEyeOffsetオブジェクトのTransformも設定するためのUnity Editorウィンドウ。
    /// 非アクティブなオブジェクトも検出できるように改良されています。
    /// また、既存ギミックの削除と再生成を単一のボタンで制御します。
    /// </summary>
    public class VRCConstraintEditorWindow : EditorWindow
    {
        // ====================================================================================================
        // フィールド (EditorWindow UIおよび内部状態)
        // ====================================================================================================
        private const float WINDOW_WIDTH = 700f;
        private const float WINDOW_HEIGHT = 300f;
        private const float WINDOW_HEIGHT_ADD_DEVELOPER = 450f;
        private static readonly Vector2 NORMAL_WINDOW_SIZE = new Vector2(WINDOW_WIDTH, WINDOW_HEIGHT); // 通常モードのサイズ
        private static readonly Vector2 DEVELOPER_WINDOW_SIZE = new Vector2(WINDOW_WIDTH, WINDOW_HEIGHT + WINDOW_HEIGHT_ADD_DEVELOPER); // 開発者モードのサイズ

        private GameObject _selectedGameObject = null;

        GimmickData _gimmickData = new GimmickData();

        /// <summary>
        /// ウィンドウ内部に表示するロゴ画像。OnEnableで一度だけロードされます。
        /// </summary>
        private Texture2D _logoTexture;

        /// <summary>
        /// ロゴ画像のパス
        /// </summary>
        private const string LOGO_RESOURCES_PATH = "Aramaa/HoldGimick/Images/dako_gimmick_icon";

        private PackageUpdater.PackageUpdateState _currentUpdateState = PackageUpdater.PackageUpdateState.Unknown;
        private string _updateMessage = "バージョン情報を確認中...";

        // ====================================================================================================
        // Unity Editor メニュー項目
        // ====================================================================================================

        /// <summary>
        /// Unity Editor のメニューバーにウィンドウを表示するための項目を追加します。
        /// </summary>
        [MenuItem("Tools/" + GimmickConstants.MENU_PATH, false, 10)]
        public static void ShowToolWindow()
        {
            // GetWindowを使用してウィンドウのインスタンスを取得または作成
            var window = GetWindow<VRCConstraintEditorWindow>(
                true, // Utilityウィンドウとして表示 (通常のウィンドウと同じように振る舞うが、タブがない)
                GimmickConstants.WINDOW_TITLE, // タイトルバーのテキスト
                true // フォーカス
            );
            // ウィンドウの最大サイズと最小サイズを設定
            window.maxSize = NORMAL_WINDOW_SIZE;
            window.minSize = NORMAL_WINDOW_SIZE;
        }

        /// <summary>
        /// GameObjectのコンテキストメニューにウィンドウを開く項目を追加します。
        /// 選択中のGameObjectをアバターとして自動設定します。
        /// </summary>
        /// <param name="command">メニューコマンドデータ。</param>
        [MenuItem("GameObject/" + GimmickConstants.MENU_PATH, false, 10)]
        private static void OpenWindowWithSelectedAvatar(MenuCommand command)
        {
            var selectedGameObject = command.context as GameObject;
            if (selectedGameObject == null) return; // GameObjectが選択されていない場合は何もしない

            var window = GetWindow<VRCConstraintEditorWindow>(
                true, // Utilityウィンドウとして表示
                GimmickConstants.WINDOW_TITLE, // タイトルバーのテキスト
                true // フォーカス
            );
            window._selectedGameObject = selectedGameObject; // 選択中のアバターをセット
            window._gimmickData.AvatarRootObject = selectedGameObject; // 選択中のアバターをセット
            window.maxSize = NORMAL_WINDOW_SIZE;
            window.minSize = NORMAL_WINDOW_SIZE;
        }

        // ====================================================================================================
        // EditorWindow ライフサイクルメソッド
        // ====================================================================================================

        /// <summary>
        /// ウィンドウが有効になったときに呼び出される関数。ロゴ画像をロードします。
        /// </summary>
        private async void OnEnable()
        {
            if (_gimmickData == null)
            {
                _gimmickData = new GimmickData();
            }

            _gimmickData.ResetData();
            _gimmickData.AvatarRootObject = _selectedGameObject;

            titleContent = new GUIContent(GimmickConstants.WINDOW_TITLE);

            _logoTexture = Resources.Load<Texture2D>(LOGO_RESOURCES_PATH);

            // ウィンドウサイズを初期化
            ChangeWindowSize();

            // 非同期で更新チェックを実行
            await CheckPackageUpdateStatus(); 
        }

        /// <summary>
        /// PackageUpdaterを使用してパッケージの更新状態を確認し、結果をフィールドに格納します。
        /// </summary>
        private async Task CheckPackageUpdateStatus()
        {
            _currentUpdateState = PackageUpdater.PackageUpdateState.Unknown;
            _updateMessage = "バージョン情報を確認中...";
            Repaint(); // UIを即座に更新して「確認中...」を表示

            var (state, message) = await PackageUpdater.CheckForUpdateAsync();

            _currentUpdateState = state;
            _updateMessage = message;
            Repaint(); // UIを更新して最新の状態を表示
        }

        private void OnGUI()
        {
            DrawLogoSection();

            if (GUILayout.Button("説明書", EditorStyles.linkLabel)) { Application.OpenURL("https://docs.google.com/document/d/141h1qxOo8ZeFPDXLFmx2fjn6jsYxf7dL6XJkSFxztec/edit?usp=sharing"); }

            EditorGUILayout.Space();

            DrawUpdateStatusSection();

            EditorGUILayout.Space();

            // アバター選択フィールド
            _gimmickData.AvatarRootObject = (GameObject)EditorGUILayout.ObjectField("アバターのルート", _gimmickData.AvatarRootObject, typeof(GameObject), true);
            _selectedGameObject = _gimmickData.AvatarRootObject;

            EditorGUILayout.Space();

            if (_gimmickData.CallbackState == UpdateCallbackState.Waiting)
            {
                EditorGUILayout.HelpBox($"{GimmickConstants.WINDOW_TITLE}生成中...", MessageType.Info);
            }
            else
            {
                // ギミック生成/再生成ボタン
                if (GUILayout.Button(new GUIContent(GimmickConstants.BUTTON_GENERATE_OR_REGENERATE_TEXT, GimmickConstants.BUTTON_GENERATE_OR_REGENERATE_TOOLTIP)))
                {
                    ConstraintSetupService.PerformFullSetup(_gimmickData);
                    Repaint();
                }
            }

            EditorGUILayout.Space();

            ToggleChangeWindowSize();

            // 開発者モードが有効な場合のみ詳細情報を表示
            if (_gimmickData.ShowDeveloperInfo)
            {
                DeveloperDebugInfoDrawer.DrawAvatarDebugInfo(_gimmickData.AvatarRootObject);
            }
        }

        private void ToggleChangeWindowSize()
        {
            // 変更前の値を一時的に保存
            var currentShowDeveloperInfo = _gimmickData.ShowDeveloperInfo;
            _gimmickData.ShowDeveloperInfo = EditorGUILayout.Toggle("開発者モード", _gimmickData.ShowDeveloperInfo);

            // ShowDeveloperInfo の値が変更されたかをチェック
            if (_gimmickData.ShowDeveloperInfo == currentShowDeveloperInfo)
            {
                return;
            }

            ChangeWindowSize();
            Repaint();
        }

        private void ChangeWindowSize()
        {
            // 値が変更された場合のみウィンドウサイズを調整
            if (_gimmickData.ShowDeveloperInfo)
            {
                minSize = DEVELOPER_WINDOW_SIZE;
                maxSize = DEVELOPER_WINDOW_SIZE;
            }
            else
            {
                minSize = NORMAL_WINDOW_SIZE;
                maxSize = NORMAL_WINDOW_SIZE;
            }
        }

        /// <summary>
        /// ウィンドウが閉じられるときに呼び出されます。
        /// ConstraintSetupService 内のキャッシュをクリアし、メモリを解放します。
        /// </summary>
        private void OnDestroy()
        {
            if (_gimmickData != null)
            {
                _gimmickData.ResetData();
                _gimmickData = null;
                Debug.Log("[VRCConstraintEditorWindow] Gimmick data cleaned up.");
            }

            _selectedGameObject = null;
            _logoTexture = null;
        }

        // ====================================================================================================
        // プライベート描画ヘルパーメソッド
        // ====================================================================================================

        /// <summary>
        /// ウィンドウ上部にロゴ画像を表示するヘルパーメソッド。
        /// </summary>
        private void DrawLogoSection()
        {
            if (_logoTexture == null)
            {
                EditorGUILayout.HelpBox($"ロゴ画像が見つかりません (Resources/{LOGO_RESOURCES_PATH}.png など)。", MessageType.Warning);
                return;
            }

            float originalWidth = _logoTexture.width;
            float originalHeight = _logoTexture.height;
            float aspectRatio = originalWidth / originalHeight;

            float availableWidth = position.width;
            float maxHeight = 185f; // ロゴ画像の元の高さに合わせて調整

            float finalWidth;
            float finalHeight;

            // ウィンドウ幅に合わせてアスペクト比を維持したサイズを計算
            float widthBasedHeight = originalHeight * (availableWidth / originalWidth);

            if (widthBasedHeight > maxHeight)
            {
                // ウィンドウ幅に合わせると高さがmaxHeightを超える場合、高さをmaxHeightに制限
                finalHeight = maxHeight;
                finalWidth = originalWidth * (maxHeight / originalHeight);
            }
            else
            {
                // ウィンドウ幅に収まる、またはmaxHeight以下で収まる場合、ウィンドウ幅に合わせる
                finalWidth = availableWidth;
                finalHeight = widthBasedHeight;
            }

            // 最終的に、ウィンドウの幅を超えないようにする（念のため）
            if (finalWidth > availableWidth)
            {
                finalWidth = availableWidth;
                finalHeight = finalWidth / aspectRatio;
            }

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace(); // 左に余白を置いて中央寄せ
            GUILayout.Box(_logoTexture, GUIStyle.none, GUILayout.Width(finalWidth), GUILayout.Height(finalHeight));
            GUILayout.FlexibleSpace(); // 右に余白
            GUILayout.EndHorizontal();

            EditorGUILayout.Space();
        }

        /// <summary>
        /// パッケージの更新状態をシンプルにテキストと色分けで表示するヘルパーメソッド。
        /// </summary>
        private void DrawUpdateStatusSection()
        {
            Color originalColor = GUI.contentColor; // 元の色を保存

            string displayMessage;
            // 明るい色を定義
            Color lightGreen = new Color(0.6f, 1.0f, 0.6f);  // 明るい緑
            Color lightOrange = new Color(1.0f, 0.8f, 0.4f); // 明るいオレンジ
            Color lightRed = new Color(1.0f, 0.6f, 0.6f);    // 明るい赤
            Color lightGray = new Color(0.8f, 0.8f, 0.8f);   // 明るい灰色

            switch (_currentUpdateState)
            {
                case PackageUpdater.PackageUpdateState.UpToDate:
                    GUI.contentColor = lightGreen;
                    displayMessage = _updateMessage;
                    break;
                case PackageUpdater.PackageUpdateState.UpdateAvailable:
                    GUI.contentColor = lightOrange;
                    displayMessage = _updateMessage;
                    break;
                case PackageUpdater.PackageUpdateState.Error:
                    GUI.contentColor = lightRed;
                    displayMessage = $"更新チェック中にエラーが発生しました: {_updateMessage}";
                    break;
                case PackageUpdater.PackageUpdateState.Unknown:
                default:
                    GUI.contentColor = lightGray;
                    displayMessage = _updateMessage; // 「バージョン情報を確認中...」など
                    break;
            }

            EditorGUILayout.LabelField(displayMessage, EditorStyles.boldLabel); // 太字で表示
            GUI.contentColor = originalColor; // 色を元に戻す
        }
    }
}
