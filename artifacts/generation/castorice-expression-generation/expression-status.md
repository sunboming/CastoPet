# Castorice 生成与接入状态

更新时间：2026-08-07

状态含义：

- **已生成并使用**：正式文件已存在于 Runtime，且 `skin.json` 正在引用。
- **候选已生成**：已有 Source/Transparent 候选静态图，但尚未写入 Runtime 清单。
- **未生成**：尚无符合长期模板要求的候选素材。

## 正式轮盘表情

以下 8 种表情均已生成并使用。每种表情包含一张正式终态 PNG 和 6 张专属入场过渡帧；程序在退出表情时倒序播放相同过渡帧。

| 表情 | 状态 | 正式终态 | 正式过渡目录 | 轮盘使用 |
|---|---|---|---|---|
| Happy | 已生成并使用 | `Expressions/Castorice.Expression.Happy.png` | `Expressions/Happy/Transition/` | 是 |
| Shy | 已生成并使用 | `Expressions/Castorice.Expression.Shy.png` | `Expressions/Shy/Transition/` | 是 |
| Sleepy | 已生成并使用 | `Expressions/Castorice.Expression.Sleepy.png` | `Expressions/Sleepy/Transition/` | 是 |
| Surprised | 已生成并使用 | `Expressions/Castorice.Expression.Surprised.png` | `Expressions/Surprised/Transition/` | 是 |
| Pouting | 已生成并使用 | `Expressions/Castorice.Expression.Pouting.png` | `Expressions/Pouting/Transition/` | 是 |
| Confused | 已生成并使用 | `Expressions/Castorice.Expression.Confused.png` | `Expressions/Confused/Transition/` | 是 |
| Proud | 已生成并使用 | `Expressions/Castorice.Expression.Proud.png` | `Expressions/Proud/Transition/` | 是 |
| Crying | 已生成并使用 | `Expressions/Castorice.Expression.Crying.png` | `Expressions/Crying/Transition/` | 是 |

正式路径均相对于 `src/CastoPet/Assets/Runtime/Castorice/`。

## 长期扩展候选

以下 4 种表情已有候选静态图，但尚无正式专属过渡帧，也未进入轮盘。

| 表情 | 状态 | 绿幕候选 | 透明候选 | 下一步 |
|---|---|---|---|---|
| Worried | 候选已生成 | `CandidateSet/Source/Expressions/Castorice.Expression.Worried.png` | `CandidateSet/Transparent/Expressions/Castorice.Expression.Worried.png` | 生成并验收 6 帧入场过渡 |
| Excited | 候选已生成 | `CandidateSet/Source/Expressions/Castorice.Expression.Excited.png` | `CandidateSet/Transparent/Expressions/Castorice.Expression.Excited.png` | 生成并验收 6 帧入场过渡 |
| Bored | 候选已生成 | `CandidateSet/Source/Expressions/Castorice.Expression.Bored.png` | `CandidateSet/Transparent/Expressions/Castorice.Expression.Bored.png` | 生成并验收 6 帧入场过渡 |
| Affection | 候选已生成 | `CandidateSet/Source/Expressions/Castorice.Expression.Affection.png` | `CandidateSet/Transparent/Expressions/Castorice.Expression.Affection.png` | 生成并验收 6 帧入场过渡 |

候选路径均相对于 `src/CastoPet/Assets/`。

## 本轮已生成并投入使用的基础动作

| 动作 | 当前播放帧 | 状态 | 备注 |
|---|---:|---|---|
| Idle | 8 | 已生成并使用 | 125 ms/帧 |
| Blink | 3 | 已生成并使用 | 随机 3–7 秒触发 |
| Petting | 8 | 已生成并使用 | 80 ms/帧 |
| MoveLeft | 5 | 已生成并使用 | 播放 `.01–.05`；异常眼神帧不播放 |
| MoveRight | 7 | 已生成并使用 | 播放 `.01–.07` |
| TurnLeft | 6 | 已生成并使用 | 左右独立生成，禁止镜像 |
| TurnRight | 6 | 已生成并使用 | 左右独立生成，禁止镜像 |
