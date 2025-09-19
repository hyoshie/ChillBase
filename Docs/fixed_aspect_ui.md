# Unity モバイル UI：アスペクト比固定（FixedAspect）運用ガイド

このドキュメントは、**Canvas
内に"固定アスペクトのコンテナ"を作る方式**で、端末の解像度や比率が違っても
UI
を歪ませずに表示するための手順と運用ノウハウをまとめたものです。個々のパネルの細かな設定例は含めません（別途）。

縦横の回転は未対応

※カメラを調整してやる場合の参考サイト
https://note.com/yamasho55/n/nfa0041bc0dcb

---

## 目的とメリット

- **UI の縦横比を保持**（例：常に 9:16 の画面設計）
- 端末ごとの余白は
  **レターボックス/ピラーボックス**として処理（歪みなし）
- Layout Group / Anchors / Pivot による **柔軟なリフロー**が可能

---

## 基本構成（推奨ヒエラルキー）

    Canvas (Screen Space - Overlay)
     └─ Root                ← 画面全体をカバー（Stretch）
         └─ FixedAspect     ← 比率固定の“入れ物”（中央）
             └─ ScreenRoot ← 画面ごとの UI を配置

- **Canvas**
  - Canvas Scaler: `Scale With Screen Size`
  - Reference Resolution: 例 `1080 x 1920`（縦長基準）
  - Screen Match Mode: `Match Width Or Height`（Match=1.0 目安 /
    必要に応じ調整）
- **Root（Panel）**
  - Anchors: **Min(0,0) / Max(1,1)**（四隅ストレッチ）
  - 背景色や模様はここに設定（余白部分の見た目用）
- **FixedAspect（Panel）**
  - Anchors: **中央**（Min=Max=0.5,0.5）、Pivot=0.5,0.5
  - **Aspect Ratio Fitter** を追加
    - Mode: **Fit In Parent**
    - Aspect Ratio: **0.5625**（= 9/16。横長基準にしたいなら
      16/9=1.777... など）
  - ここに**すべての UI**を入れる

---

## 運用：アスペクト比の"変更"とバリエーション

### 1) 既定の比率を変える（例：9:16 → 9:19.5）

- `FixedAspect` の **Aspect Ratio**
  を新しい値に変更（例：`9f/19.5f = 0.4615…`）
- 参照解像度を変える場合は **Canvas Scaler の Reference Resolution**
  も合わせて更新
- 影響範囲：UI の**歪みは出ない**が、余白の出方が変わる（左右 or 上下）

### 2) 端末カテゴリ毎に比率やパディングを切り替える

- スクリプトで画面アスペクトや短辺/長辺を判定し、**Aspect Ratio Fitter
  の値**や **ScreenRoot の Padding** を切替
- 例：
  - Phone（縦長）→ 9:16
  - Small Tablet → 3:4（=0.75）
  - Large Tablet/横向き → 16:10（=0.625）

### 3) "比率は固定、UI 密度だけ調整"

- 比率は 9:16 のまま、`ScreenRoot` 直下の Layout Group の
  Padding/Spacing、フォントの `Auto Size` の Min/Max
  を**ブレークポイント**的に変更

---

## Anchors / Pivot の原則（抜粋）

- **固定したい方向**は **Min=Max**（同一点アンカー）
- **伸ばしたい方向**は **Min と Max を離す**（ストレッチ）
- **角固定**：Anchors=(0/1,0/1), Pivot 同値、Pos をマージン扱い
- **バー**：横ストレッチ（MinX=0/MaxX=1）、Pivot は中央 or 端、Height
  固定
- **中央配置**：Anchors=Pivot=0.5,0.5、Pos=0

---

## レイアウト運用のコツ

- **親に Layout Group、子に Layout Element**（Min/Preferred/Flexible）
- **Content Size Fitter**
  は"中身に合わせて親が伸びる"必要がある箇所に限定（Layout Group
  と併用注意）
- 正方形/16:9 の要素は各要素に **Aspect Ratio Fitter**
  を付ける（`Preferred Size`）
- 画像は **9 スライス（Sprite Borders）** で拡大縮小時の太さ崩れ防止

---

## テスト手順（チェックリスト）

- [ ] Game View の **Aspect** を複数（9:16, 18.5:9, 2:1, 4:3,
      16:10）で確認
- [ ] 実機での挙動確認（iPhone、Pixel 等）
- [ ] 画面回転（Portrait/Landscape）時の `FixedAspect`
      の余白変化を確認
- [ ] テキスト言語切替（長文）で折返し/はみ出しがない
- [ ] 重要 UI に `Layout Element.minWidth/minHeight` を設定済み

---

## よくある落とし穴

- `FixedAspect` の**外**に UI を置いてしまう →
  比率固定外で伸びて崩れる
- 親に `Vertical/Horizontal Layout Group` があるのに、親に
  `Content Size Fitter` も付ける → 競合
- Canvas Scaler の `Match` だけで**比率固定できると勘違い** → 必ず
  `Aspect Ratio Fitter` を使う

---

## 既存 UI からの移行手順（ダイジェスト）

1.  Canvas 配下に **Root / FixedAspect / ScreenRoot** を作る
2.  既存の UI を **ScreenRoot の内側**へ移動
3.  FixedAspect に **Aspect Ratio Fitter: Fit In Parent + 目標比率**
    を設定
4.  各 UI の Anchors/Pivot を原則に従って整理（個別ドキュメント参照）

---

## まとめ

- **FixedAspect** 方式は、UI の
  **歪みゼロ**・**余白許容**を前提にした最もシンプルで拡張性の高い手法。
- 端末差は **余白**と**レイアウト（Padding/Spacing/Auto
  Size）**で吸収、核となる見た目は常に同一比率で維持できます。

> 個々のパネルの修正レシピ（Anchors/Pivot/レイアウト値）は別シートへ。
