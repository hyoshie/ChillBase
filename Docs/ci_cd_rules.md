# CI/CD 運用ルール

本プロジェクトの GitHub Actions を利用した CI/CD 運用ルールをまとめます。  
目的は **安定したリリース** と **効率的な開発** を両立することです。

---

## ブランチ運用

- **main**

  - リリース用のブランチ
  - 常にリリース可能な状態を維持する
  - 保護ルールを適用（直接 push 禁止、PR 必須、必須チェックあり）

- **dev**

  - 開発統合用のブランチ
  - feature/ui ブランチからの PR を受け入れる
  - 保護ルールを適用（直接 push 禁止、PR 必須、必須チェックあり）

- **feature/xx**, **ui/xx**など
  - 作業用ブランチ
  - 命名は `feature/機能名`、`ui/修正内容` を推奨
  - PR で dev へマージする

---

## CI/CD の実行タイミング

### ✅ テスト

- **対象**: main, dev への PR
- **内容**: Unity EditMode テスト
- **目的**: 開発段階とリリース前の両方で不具合を早期発見する

### ✅ ビルド

- **対象**: main への PR
- **内容**: Android, iOS ビルド
- **目的**: リリース前にビルド可能であることを保証する

### 🔄 オプション

- 作業ブランチへの PR でもテストを走らせたい場合は、命名規則に従って対象を追加する（例: `feature/**`, `ui/**`）。

---

## Branch Protection 設定（推奨）

- Require a pull request before merging（PR 必須）
- Require status checks to pass
  - 必須チェック:
    - `Test in editmode`
    - `Build for Android`
    - `Build for iOS`
- Require branches to be up to date before merging（推奨）
- Block force pushes（強制 push 禁止）
- Restrict deletions（削除禁止）

---

## 運用フローまとめ

1. 開発者は `feature/xx` や `ui/xx` などで作業
2. 完了したら `dev` へ PR
   - テストが走り、必須チェックが通ればマージ可能
3. リリース前に `dev → main` へ PR
   - テスト＋ビルドが走り、必須チェックが通ればマージ可能
4. main が更新されたらリリース可能

---
