# Manual Eye Pad 1.0.1

VRChatのアクションメニューで、アバターの両目を上下左右に動かすギミックです。

## 前提条件

必要なものは Unity 2022.3、VRChat SDK - Avatars、Modular Avatar です。
確認環境: Unity 2022.3.22f1、SDK 3.10.5、Modular Avatar 1.18.7。
HumanoidのHeadと、Headの下にある左右の目ボーンが必要です。
目ボーンは Avatar Descriptor → Eye Look の指定を優先し、無ければHumanoid設定を使います。

## 準備とダウンロード

1. VRChat用の機能を使う場合は、Creator CompanionなどでAvatarsプロジェクトを用意し、VRChat SDK - Avatarsを導入します。
2. 対象プロジェクトにModular Avatarを導入します。関連する依存パッケージも解決し、Unityのコンパイル完了を待ちます。
3. 下記のGitHub Releasesを開き、Assets欄から、このツール名の付いた拡張子が .unitypackage のファイルをダウンロードします。
4. Unityの Assets → Import Package → Custom Package からファイルを選び、Importを押します。
5. Consoleに赤いコンパイルエラーがある場合は、内容を確認して解消してからツールを開きます。

SDKの導入はCreator Companionの案内、Modular Avatarの追加は公式導入手順を参照してください。 [Creator Companion](https://vcc.docs.vrchat.com/) ・ [Modular Avatarの導入](https://modular-avatar.nadena.dev/docs/intro)

## アバターへ導入する

1. Unityプロジェクトに .unitypackage をインポートします。
2. Hierarchyで、シーン上のアバターを選びます。
3. Tools → Manual Eye Pad → Install on Selected Avatar を選びます。
   Synced は周りのプレイヤーにも目線を共有し、同期枠を33ビット使用します。
   Local Only は自分のクライアントで表示し、同期枠を使用しません。
   既存ギミックを含む残り容量はVRChat SDKのBuildパネルで確認してください。
4. アバター直下に「Manual Eye Pad」が追加されます。配置先はこのオブジェクトの
   Modular Avatar Menu Installer → Select Menu で変更できます。
5. シーンを保存し、対象アバターをSDKでBuild & Testします。公開済みアバターへ反映する場合は再アップロードしてください。

## VRChatでの使い方

- 「目線パッド」→「手動目線」をオンにして「上下左右パッド」を開きます。
- マウスまたはスティックで両目を動かせます。パッドを閉じると最後の方向を保持します。
- 「正面に戻す」で中央に戻り、「手動目線」をオフにすると普段の目線と自動まばたきに戻ります。
- 手動目線中はVRChat標準の目線追従と自動まばたきが停止します。
- 対応するSTAREEEが入っている場合は、その「視線追従」をオフにして使ってください。
  STAREEEがオンの間や追従の解除中は、手動目線への切り替えを待ちます。
- ほかの目線・フェイストラッキングギミックも、同時使用は避けてください。

## 取り外しと生成データ

外すときは対象アバター直下の「Manual Eye Pad」を削除し、シーンを保存して再ビルドします。
インストーラーは Assets/ManualEyePadGenerated 以下にアバターごとの設定を生成します。
生成した設定を他のアバターに流用せず、そのアバター上でインストーラーを実行してください。
このパッケージにはSTAREEEやアバター本体、衣装などの素材は含まれていません。

配布ファイルは [GitHub Releases](https://github.com/Hiharu/manual-eye-pad/releases/latest) からダウンロードできます。

## ライセンス

MIT License。詳細は [LICENSE](LICENSE) を参照してください。

## 動作確認

Unity 2022.3.22f1、VRChat SDK - Avatars 3.10.5、Modular Avatar 1.18.7、NDMF 1.14.8で検証しました。最低対応バージョンは未検証です。

SDK付属サンプルで、ローカル版と同期版の導入、ビルド時のメニュー合成、同期パラメータの使用量を確認しました。VRChat実機での目の動きと同期は未検証です。

## 1.0.1の変更

正面の目線設定が未入力のアバターで導入に失敗する問題を修正しました。未入力時は目ボーンの現在の向きを基準にします。

## 不具合報告

[Issues](https://github.com/Hiharu/manual-eye-pad/issues) に、使用バージョン、再現手順、期待した動作、実際の動作、Consoleのエラーを記載してください。

## 更新する

1. 新しい .unitypackage を同じプロジェクトへインポートし、コンパイル完了を待ちます。
2. 既存アバターの生成済み設定にも修正を反映する場合は、導入先メニューを控えてからアバター直下の「Manual Eye Pad」を削除します。
3. 同じアバターを選択し、SyncedまたはLocal Onlyの導入を再実行します。必要に応じてSelect Menuを設定し直します。
4. シーンを保存し、再ビルドして確認します。

生成フォルダーは導入のたびに作られます。古い Assets/ManualEyePadGenerated 以下のアセットは、参照がなくなったものだけ削除してください。

## 困ったとき

| 症状 | 確認すること |
| --- | --- |
| ToolsにManual Eye Padが出ない | SDK・Modular Avatarの導入とConsoleのコンパイルエラー |
| Humanoidが必要と表示される | アバターのAnimatorに有効なHumanoid Avatarが設定されているか |
| 左右の目ボーンを求められる | Head配下に別々の左右目ボーンがあり、Avatar DescriptorのEye LookまたはHumanoidに割り当てられているか |
| 導入済みと表示される | 既存の「Manual Eye Pad」を使うか、更新手順に沿って再導入 |
| 同期容量が足りない | Syncedに33ビットの空きがあるか。Local Onlyは同期枠を使いません |
| VRChatのメニューに出ない | 生成オブジェクトとMA Menu Installerが有効か、導入先とビルドしたアバターが正しいか |
| 手動目線へ切り替わらない | 対応するSTAREEEの視線追従を解除したか、他の目線制御と競合していないか |

1.0で正面の目線設定が未入力のときに導入エラーが出る場合は、1.0.1へ更新してください。
