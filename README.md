# ▶　Animatable Volume Component　◀

**Animatable Volume Component** provides an interface for animating the URP PostProcessing Volumes. Use this component to give your game more interactive and dynamic look.

**Animatable Volume Component** は、URP 及び HDRP のポストプロセス Volume をアニメーションさせるためのインターフェースを提供します。このコンポーネントを使うことで、より臨場感のあるゲームシーンを作ることができます。

＜ここにロゴ＞

# 特徴

Animatable Volume Component 以下の特徴を有します：

1. `Volume Profile` に対して、いつもの操作で簡単にアニメーションのキーを打ち、再生させることが出来る
2. コード分析・自動生成により、
   - Unity のバージョンごとのパラメータ変更を吸収できる
   - カスタムな `Volume Component` に対してもアニメーションさせることができる
3. ツール導入前後、アニメーション時以外の Volume の操作は一切変わらない

# 使い方

0. （一度のみ）アニメーション用の補助コンポーネントの自動生成
1. アニメーション用の補助コンポーネントを Volume のゲームオブジェクトにアタッチ
2. アニメーションキーイング

## (下準備) 補助コンポーネントの自動生成

本コンポーネントの UnityPackage を導入後、メニューの `Tools > Animatable Volume > Animatable Volume Wizard` を開きます。すると、下記のようなポップアップが開きます。

＜Wizard_1.png＞

こちらは現在プロジェクト内に存在する `VolumeComponent` のリストです。

アニメーションさせたい `VolumeComponent` にチェックを入れ、［生成］ボタンを押して、`VolumeComponent` に対応した補助コンポーネント（`Animatable + 元のコンポーネント名`）を生成します。

＜Wizard_2.png＞

これでアニメーションをさせるための下準備は完了です。

## アニメーションキーイング

アニメーションをさせたい `Volume` のゲームオブジェクトに、`Animator` 及び `AnimatableVolumeHelper` をアタッチします。

＜ここに画像＞

`AnimatableVolumeHelper` のインスペクターの［Add Component］を押して、各 `VolumeComponent` に対応した補助コンポーネントを追加します。

補助コンポーネントはランタイムの Profile にのみ作動するので、`AnimatableVolumeHelper` の［Create Runtime Profile］を押して、Profile アセットのコピーを生成します。 （自動で生成される場合があります）

＜ここに画像１，２＞

後はいつものアニメーションのやり方です。

アニメーションウィンドウを開き、アニメーションさせたい時間点に、Profile の値を設定して、キーを打っていきます。

＜ここに gif 画像＞

アニメーションをプレビューして、出来を確認します。

＜ここに gif 画像＞

