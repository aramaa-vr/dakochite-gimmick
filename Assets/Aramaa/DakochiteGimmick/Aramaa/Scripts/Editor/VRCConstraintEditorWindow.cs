using System.IO;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Dynamics.Constraint.Components;
using System.Threading.Tasks;

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
        private const float WINDOW_WIDTH = 700f; // ウィンドウの幅は共通
        private static readonly Vector2 NORMAL_WINDOW_SIZE = new Vector2(WINDOW_WIDTH, 300f); // 通常モードのサイズ
        private static readonly Vector2 DEVELOPER_WINDOW_SIZE = new Vector2(WINDOW_WIDTH, 800f); // 開発者モードのサイズ

        /// <summary>
        /// 操作対象となるVRChatアバターのルートGameObject。
        /// Inspector上でユーザーが Hierarchyからドラッグ＆ドロップして設定します。
        /// このオブジェクトにはAnimatorコンポーネントがアタッチされており、Humanoidアバターとして設定されている必要があります。
        /// </summary>
        private GameObject _targetAvatarRootObject;

        /// <summary>
        /// 開発者向けの詳細情報を表示するかどうかのフラグ。
        /// </summary>
        private bool _showDeveloperInfo = false; // デフォルトは非表示

        /// <summary>
        /// ウィンドウ内部に表示するロゴ画像。OnEnableで一度だけロードされます。
        /// </summary>
        private Texture2D _logoTexture;

        /// <summary>
        /// ロゴ画像のパス（Resourcesフォルダからの相対パス、拡張子なし）。
        /// 例: Assets/Resources/Images/dako_gimmick_icon.png に配置した場合、"Images/dako_gimmick_icon"
        /// </summary>
        private const string LOGO_RESOURCES_PATH = "Aramaa/HoldGimick/Images/dako_gimmick_icon"; // Resources.Load用にパスを修正

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
            window._targetAvatarRootObject = selectedGameObject; // 選択中のアバターをセット
            window.maxSize = NORMAL_WINDOW_SIZE;
            window.minSize = NORMAL_WINDOW_SIZE;
        }

        // ====================================================================================================
        // EditorWindow ライフサイクルメソッド
        // ====================================================================================================

        /// <summary>
        /// ウィンドウが有効になったときに呼び出される関数。ロゴ画像をロードします。
        /// </summary>
        private async void OnEnable() // async キーワードを追加
        {
            // titleContentを設定（ウィンドウタブやタイトルバーの表示）
            titleContent = new GUIContent(GimmickConstants.WINDOW_TITLE);

            // --- ウィンドウ内部に表示するロゴ画像をロード (Resources.Loadを使用) ---
            _logoTexture = Resources.Load<Texture2D>(LOGO_RESOURCES_PATH); // Resources.Loadに変更
            if (_logoTexture == null)
            {
                Debug.LogWarning($"だこちてギミック設定ツールのロゴ画像が見つかりません。Resourcesフォルダ内（例: Assets/Resources/{LOGO_RESOURCES_PATH}.png）に配置してください。");
            }

            await CheckPackageUpdateStatus(); // 非同期で更新チェックを実行
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

        /// <summary>
        /// EditorWindow が開かれ、GUIが描画されるたびに呼び出されます。
        /// ここでウィンドウのレイアウトとUI要素を定義します。
        /// </summary>
        private void OnGUI()
        {
            // ウィンドウサイズの動的調整
            if (_showDeveloperInfo)
            {
                // 開発者モードがオンの場合、ウィンドウを大きくする
                minSize = DEVELOPER_WINDOW_SIZE;
                maxSize = DEVELOPER_WINDOW_SIZE;
            }
            else
            {
                // 開発者モードがオフの場合、ウィンドウを小さくする
                minSize = NORMAL_WINDOW_SIZE;
                maxSize = NORMAL_WINDOW_SIZE;
            }

            // --- ロゴ表示 ---
            DrawLogoSection();

            // 青文字のリンクボタンのように見せるために LinkButton を使用
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("説明書", EditorStyles.linkLabel)) {Application.OpenURL("https://docs.google.com/document/d/141h1qxOo8ZeFPDXLFmx2fjn6jsYxf7dL6XJkSFxztec/edit?usp=sharing");}
            if (GUILayout.Button("Booth", EditorStyles.linkLabel)) { Application.OpenURL("https://aramaa.booth.pm/items/7016968"); }
            if (GUILayout.Button("GitHub", EditorStyles.linkLabel)) { Application.OpenURL("https://github.com/aramaa-vr/dakochite-gimmick"); }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            DrawUpdateStatusSection();

            EditorGUILayout.Space();

            // アバター選択フィールド
            _targetAvatarRootObject = (GameObject)EditorGUILayout.ObjectField(
                "アバターのルート",
                _targetAvatarRootObject,
                typeof(GameObject),
                true // シーン内のオブジェクトのみ
            );

            EditorGUILayout.Space();

            // ギミック生成/再生成ボタン
            if (GUILayout.Button(new GUIContent(GimmickConstants.BUTTON_GENERATE_OR_REGENERATE_TEXT, GimmickConstants.BUTTON_GENERATE_OR_REGENERATE_TOOLTIP)))
            {
                // セットアップ処理をサービスに委譲
                // PerformFullSetupは既存ギミックを自動で削除し、新規生成を行います
                ConstraintSetupService.PerformFullSetup(_targetAvatarRootObject);
                Repaint(); // UI情報を更新し、DeveloperInfoに最新情報を表示
            }

            EditorGUILayout.Space();

            // 開発者モードのトグル
            _showDeveloperInfo = EditorGUILayout.Toggle("開発者モード", _showDeveloperInfo);
            EditorGUILayout.Space();

            // 開発者モードが有効な場合のみ詳細情報を表示
            if (_showDeveloperInfo)
            {
                DrawDeveloperInfoSection();
            }
        }

        /// <summary>
        /// ウィンドウが閉じられるときに呼び出されます。
        /// ConstraintSetupService 内のキャッシュをクリアし、メモリを解放します。
        /// </summary>
        private void OnDestroy()
        {
            // リフレクションを使用して、ConstraintSetupServiceのプライベート静的フィールドをクリア
            // これにより、ウィンドウが閉じられたときに古いプレハブアセットへの参照が残るのを防ぎます。
            typeof(ConstraintSetupService).GetField("_gimmickPrefabAssetCache", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)?
                                         .SetValue(null, null);
            Debug.Log("[VRCConstraintEditorWindow] ConstraintSetupServiceのプレハブキャッシュをクリアしました。");
        }

        // ====================================================================================================
        // プライベート描画ヘルパーメソッド
        // ====================================================================================================

        /// <summary>
        /// ウィンドウ上部にロゴ画像を表示するヘルパーメソッド。
        /// </summary>
        private void DrawLogoSection()
        {
            if (_logoTexture != null)
            {
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
                EditorGUILayout.Space(); // ロゴの下にスペース
            }
            else
            {
                // Resources.Loadにしたので、パスのヒントをResourcesフォルダ内である旨に変更
                EditorGUILayout.HelpBox($"ロゴ画像が見つかりません (Resources/{LOGO_RESOURCES_PATH}.png など)。", MessageType.Warning);
            }
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

        /// <summary>
        /// 開発者モード時に表示される詳細情報セクションを描画するヘルパーメソッド。
        /// </summary>
        private void DrawDeveloperInfoSection()
        {
            EditorGUILayout.HelpBox("開発者モードが有効です。詳細情報が表示されています。", MessageType.Info);
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("現在のギミックとConstraint情報:", EditorStyles.boldLabel);

            if (_targetAvatarRootObject != null)
            {
                // GIMMICK_PREFAB_PATHもResources.Loadに変更する必要があるが、
                // これはVRCConstraintEditorWindowの責務ではないため、そのままにしておく。
                // ConstraintSetupService内で同様の修正が必要。
                string gimmickPrefabName = Path.GetFileNameWithoutExtension(GimmickConstants.GIMMICK_PREFAB_PATH);
                Transform gimmickInstanceTransform = HierarchyUtility.FindChildRecursive(_targetAvatarRootObject.transform, gimmickPrefabName);

                if (gimmickInstanceTransform != null)
                {
                    VRCParentConstraint foundConstraint = HierarchyUtility.FindConstraintInHierarchy(gimmickInstanceTransform, GimmickConstants.CONSTRAINT_PATH_INSIDE_PREFAB);
                    if (foundConstraint != null)
                    {
                        EditorGUILayout.ObjectField("見つかったConstraint", foundConstraint.gameObject, typeof(GameObject), true);
                        if (foundConstraint.TargetTransform != null)
                        {
                            EditorGUILayout.ObjectField("現在のTarget Transform", foundConstraint.TargetTransform, typeof(Transform), true);
                        }
                        else
                        {
                            EditorGUILayout.LabelField("現在のTarget Transform: 未設定", EditorStyles.label);
                        }
                    }
                    else
                    {
                        EditorGUILayout.HelpBox($"プレハブインスタンス '{gimmickInstanceTransform.name}' 内のパス ({GimmickConstants.CONSTRAINT_PATH_INSIDE_PREFAB}) にVRCParentConstraintが見つかりません。\nプレハブの構造が正しいか確認してください。", MessageType.Info);
                    }

                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("EyeOffsetオブジェクト情報:", EditorStyles.boldLabel);
                    Transform eyeOffsetTransform = HierarchyUtility.FindChildTransformByRelativePath(gimmickInstanceTransform, GimmickConstants.EYEOFFSET_PATH_INSIDE_PREFAB);
                    if (eyeOffsetTransform != null)
                    {
                        EditorGUILayout.ObjectField("見つかったEyeOffset", eyeOffsetTransform.gameObject, typeof(GameObject), true);
                        EditorGUILayout.Vector3Field("現在のローカル座標", eyeOffsetTransform.localPosition);
                        EditorGUILayout.Vector3Field("現在のローカル回転 (オイラー)", eyeOffsetTransform.localRotation.eulerAngles);
                        EditorGUILayout.Vector3Field("現在のワールド回転 (オイラー)", eyeOffsetTransform.rotation.eulerAngles);
                    }
                    else
                    {
                        EditorGUILayout.HelpBox($"プレハブインスタンス '{gimmickInstanceTransform.name}' 内のパス ({GimmickConstants.EYEOFFSET_PATH_INSIDE_PREFAB}) にEyeOffsetオブジェクトが見つかりません。", MessageType.Info);
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox($"アバター直下にギミックプレハブ '{gimmickPrefabName}' のインスタンスが見つかりません。", MessageType.Info);
                }

                EditorGUILayout.Space();

                // Animator Hips & Head ボーン情報の表示
                EditorGUILayout.LabelField("アバターのHipsボーン情報:", EditorStyles.boldLabel);
                Transform hipsBone = AvatarUtility.GetAnimatorHipsBone(_targetAvatarRootObject);
                if (hipsBone != null)
                {
                    EditorGUILayout.ObjectField("見つかったAnimator Hipsボーン", hipsBone, typeof(Transform), true);
                }
                else
                {
                    EditorGUILayout.HelpBox(string.Format(GimmickConstants.LOG_ANIMATOR_NOT_FOUND, _targetAvatarRootObject.name) + " またはAnimatorがHumanoid型ではありません。", MessageType.Warning);
                }

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("アバターのHeadボーン情報:", EditorStyles.boldLabel);
                Transform headBone = AvatarUtility.GetAnimatorHeadBone(_targetAvatarRootObject);
                if (headBone != null)
                {
                    EditorGUILayout.ObjectField("見つかったAnimator Headボーン", headBone, typeof(Transform), true);
                }
                else
                {
                    EditorGUILayout.HelpBox(string.Format(GimmickConstants.LOG_ANIMATOR_NOT_FOUND, _targetAvatarRootObject.name) + " またはAnimatorがHumanoid型ではありません。（Headボーン）", MessageType.Warning);
                }

                // VRCAvatarDescriptorのView位置の表示
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("VRCAvatarDescriptor View位置:", EditorStyles.boldLabel);
                VRCAvatarDescriptor avatarDescriptor = _targetAvatarRootObject.GetComponent<VRCAvatarDescriptor>();
                if (avatarDescriptor != null)
                {
                    EditorGUILayout.Vector3Field("View Position (Avatar Local)", avatarDescriptor.ViewPosition);
                }
                else
                {
                    EditorGUILayout.HelpBox(GimmickConstants.MSG_AVATARDESCRIPTOR_NOT_FOUND, MessageType.Warning);
                }
            }
            else
            {
                EditorGUILayout.LabelField("アバターのルートオブジェクトを選択してください。（詳細情報）", EditorStyles.label);
            }
        }
    }
}
