# ▶　Animatable Volume Component

**Animatable Volume Component** provides an interface for animating the URP PostProcessing Volumes.<br/>Use this component to give your game more interactive and dynamic look.

**Animatable Volume Component** は、URP 及び HDRP のポストプロセス Volume をアニメーションさせるためのインターフェースを提供します。<br/>このコンポーネントを使用して、より臨場感のあるシーンや演出を作ることができます。

<img src="https://github.com/cji3bp62000/AnimatableVolumeComponent/assets/34641639/7867ddcd-bafb-4426-a4cd-89d1fb42d75f" width="750">

<br/><br/>

# 特徴

Animatable Volume Component 以下の特徴を有します：

1. `Volume Profile` に対して、いつもの操作で簡単にアニメーションのキーを打ち、再生させることが出来る
2. コード分析・自動生成により、
   - Unity のバージョンごとのパラメータ変更を吸収できる
   - カスタムな `Volume Component` に対してもアニメーションさせることができる
3. ツール導入前後、アニメーション時以外の Volume の操作は一切変わらない

<br/>

# 使い方

0. （一度のみ）アニメーション用の補助コンポーネントの自動生成
1. アニメーション用の補助コンポーネントを Volume のゲームオブジェクトにアタッチ
2. アニメーションキーイング

<br/>

## (下準備) 補助コンポーネントの自動生成

本コンポーネントの UnityPackage を導入後、メニューの `Tools > Animatable Volume > Animatable Volume Wizard` を選択します。下記のようなポップアップが表示されます。

　![Wizard_1](https://github.com/cji3bp62000/AnimatableVolumeComponent/assets/34641639/699e7d41-a663-4f15-9bb3-a057b898438d)
> プロジェクト内に存在する `VolumeComponent` のリスト

<br/>
アニメーションさせたい `VolumeComponent` にチェックを入れ、［生成］ボタンを押して、`VolumeComponent` に対応した補助コンポーネント（`Animatable + 元のコンポーネント名`）を生成します。

　![Wizard_2](https://github.com/cji3bp62000/AnimatableVolumeComponent/assets/34641639/71206bca-377d-475f-a860-0e03f1ad12bb)

これでアニメーションをさせるための下準備は完了です。

<br/>

## 補助コンポーネントのアタッチ

アニメーションをさせたい `Volume` のゲームオブジェクトに、`Animator` 及び `AnimatableVolumeHelper` をアタッチします。

　![Helper_1](https://github.com/cji3bp62000/AnimatableVolumeComponent/assets/34641639/cebd5a21-0792-4dfb-963b-4712e1b69028)

<br/>

`AnimatableVolumeHelper` のインスペクターの［Add Corresponding Animatable Component］を押して、各 `VolumeComponent` に対応した補助コンポーネントを追加します。

　![Helper_2-all](https://github.com/cji3bp62000/AnimatableVolumeComponent/assets/34641639/a3f015ef-3d88-4cc1-bfd7-b2abc8d2593f)

<br/>

補助コンポーネントはランタイムの Profile にのみ作動するので、`AnimatableVolumeHelper` の［Create Runtime Profile］を押して、Profile アセットのコピーを生成します。 （自動で生成される場合があります）

　![Helper_3-all](https://github.com/cji3bp62000/AnimatableVolumeComponent/assets/34641639/5fd157b2-91d6-4b3e-9275-dd6039773949)

</br>

## アニメーションキーイング

補助コンポーネントをアタッチ後、後はいつものアニメーションのやり方です。
</br>アニメーションウィンドウを開き、アニメーションさせたい時間点に、Profile の値を設定して、キーを打っていきます。

　![AnimationKeying](https://github.com/cji3bp62000/AnimatableVolumeComponent/assets/34641639/98f673ab-5220-4f61-8b04-6b3ff4e3884a)

キーを打ち終わった後、アニメーションをプレビューして、出来を確認します。

　![AnimationPreview](https://github.com/cji3bp62000/AnimatableVolumeComponent/assets/34641639/2779f90e-6f29-4e80-9a3e-26441a37fe19)

