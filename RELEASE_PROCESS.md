# バージョン更新作業

---

## 1. ブランチの作成

* master から develop/x.y.z ブランチを切る
* develop/x.y.z から feature/xxxxx ブランチを切る

## 2. バージョン情報の更新

* package.json の 「"version": "x.y.z",」 を変更
* package.json の 「"url": ".../x.y.z/jp.aramaa.dakochite-gimmick-x.y.z.zip?」 パスを変更
* HoldGimickMenuMain.assetの「だこちて ver x.y.z」を変更
* PackageUpdater.csの 「LOCAL_INSTALLED_VERSION = "x.y.z"」 を変更

## 3. コミットとプッシュ

* 修正をコミット
* ブランチをプッシュ

## 4. プルリクエストとマージ (develop/x.y.zブランチ向け)

* develop/x.y.z に対して feature/xxxxx のプルリクエストを作成
* プルリクエストのタイトルに [x.y.z] を記載
* マージ後、x.y.z のリリースと ZIP ファイル、タグを作成
    * ZIP はコマンドで作成します

## 5. VPMリポジトリの更新 (開発用)

* 以下のファイルを作成し
    * develop/redirect-ver-x.y.z-develop.html
    * develop/vpm-ver-x.y.z-develop.json
* プルリクエストを作成し、マージ
* 限定公開して何人かのユーザーに見てもらう

## 6. VCCでの動作確認

* VCCで更新後の動作を確認

## 7. VPMリポジトリの更新 (本番用)

* vpm.json に新しいパッケージ情報を追加
* プルリクエストを作成し、マージ

## 8. Masterブランチへのマージと告知

* master に develop/x.y.z のプルリクエストを作成し、マージ（これによりツール側でバージョンアップ告知が表示されます）
* Twitterでアップデートを告知

---

### パッケージ版に関する注意点

* パッケージ版は基本的に作成しません。
* もし作成する場合は、依存パッケージは追加しないでください
* （依存パッケージ有で追加すると、パッケージがない場合にシーン上でロードが始まり複雑なウィンドウやエラーが表示され、初心者に優しくないため）
* パッケージ作成ツールで依存関係入れないようにする方法でも良さそうなので後で調査
* 取り合えず、1.0.2からは依存関係戻すのでパッケージ作成時は注意

    "vpmDependencies": {
        "com.vrchat.avatars": ">=3.8.2",
        "com.vrchat.base": ">=3.8.2",
        "nadena.dev.modular-avatar": ">=1.12.5"
    },
