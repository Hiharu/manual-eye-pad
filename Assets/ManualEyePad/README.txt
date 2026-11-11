Manual Eye Pad 1.0.1

VRChatのアクションメニューで、アバターの両目を上下左右に動かすギミックです。
必要なものは Unity 2022.3、VRChat SDK - Avatars、Modular Avatar です。
確認環境: Unity 2022.3.22f1、SDK 3.10.5、Modular Avatar 1.18.7。
HumanoidのHeadと、Headの下にある左右の目ボーンが必要です。
目ボーンは Avatar Descriptor → Eye Look の指定を優先し、無ければHumanoid設定を使います。

導入
1. Unityプロジェクトに .unitypackage をインポートします。
2. Hierarchyで、シーン上のアバターを選びます。
3. Tools → Manual Eye Pad → Install on Selected Avatar を選びます。
   Synced は周りのプレイヤーにも目線を共有し、同期枠を33ビット使用します。
   Local Only は自分のクライアントで表示し、同期枠を使用しません。
   既存ギミックを含む残り容量はVRChat SDKのBuildパネルで確認してください。
4. アバター直下に「Manual Eye Pad」が追加されます。配置先はこのオブジェクトの
   Modular Avatar Menu Installer → Select Menu で変更できます。
5. シーンを保存し、VRChatで確認してください。

使い方
・「目線パッド」→「手動目線」をオンにして「上下左右パッド」を開きます。
・マウスまたはスティックで両目を動かせます。パッドを閉じると最後の方向を保持します。
・「正面に戻す」で中央に戻り、「手動目線」をオフにすると普段の目線と自動まばたきに戻ります。
・手動目線中はVRChat標準の目線追従と自動まばたきが停止します。
・対応するSTAREEEが入っている場合は、その「視線追従」をオフにして使ってください。
  STAREEEがオンの間や追従の解除中は、手動目線への切り替えを待ちます。
・ほかの目線・フェイストラッキングギミックも、同時使用は避けてください。

外すときはHierarchyから「Manual Eye Pad」を削除します。
インストーラーは Assets/ManualEyePadGenerated 以下にアバターごとの設定を生成します。
生成した設定を他のアバターに流用せず、そのアバター上でインストーラーを実行してください。
このパッケージにはSTAREEEやアバター本体、衣装などの素材は含まれていません。

1.0.1: 正面の目線設定が未入力のアバターで導入に失敗する問題を修正。
