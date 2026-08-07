# Castorice 视频表情扩展提示词模板

## 用途

用于生成可拆分为桌宠帧动画的 6 秒绿幕视频。模板优先保证角色一致性、饰品方向、稳定构图和表情过渡，适合后续扩展轮盘表情。

上传参考图：`src/CastoPet/Assets/CandidateSet/Source/Castorice.png`

该图是人物身份、颜色、服装、身体比例、花冠、蝴蝶结、发饰及所有不对称细节的唯一基准。每次生成都应重新上传原图，不要使用视频末帧或已生成表情图作为人物参考。

## 变量

- `{EXPRESSION_A}`：第一个目标表情，例如 shy smile、pouting、worried。
- `{ACTION_A}`：配套小动作，限制为头部、肩部、双手和上身的小幅动作。
- `{EXPRESSION_B}`：第二个目标表情。
- `{ACTION_B}`：第二个表情的配套小动作。
- `{A_PERSONALITY}`、`{B_PERSONALITY}`：表情气质补充，默认使用 `introverted, bashful and cute`。

## 6 秒双表情通用提示词

以下正文可直接复制。替换四个动作变量后，尽量保持总长度在 2000 字符以内。

```text
Use the uploaded green image as the exact character and color authority. Subject: The same full-body chibi girl with lavender hair, purple eyes, floral crown and white-purple asymmetric dress. Retain every flower, bow, ribbon, ornament and clothing detail at its original anatomical position and side. Scene: flat solid chroma green RGB #00FF00. Style: clean Q-version 2D sprite animation, centered full body, constant size, static orthographic front camera.

Timeline: 0.0-0.6s hold the exact neutral front idle with one subtle breath. 0.6-1.0s stay still, blink once, then fully reopen the eyes. 1.0-1.5s smoothly enter {EXPRESSION_A}; {ACTION_A}; personality: {A_PERSONALITY}. 1.5-2.3s clearly hold it. 2.3-2.8s smoothly return to exact idle. 2.8-3.3s smoothly enter {EXPRESSION_B}; {ACTION_B}; personality: {B_PERSONALITY}. 3.3-4.2s clearly hold it. 4.2-4.8s smoothly return to exact idle. 4.8-6.0s hold neutral, eyes open and body still. Make continuous, extractable transition frames. Only the face and small upper-body gestures may change.

Do not mirror or flip. Do not move, swap, duplicate, remove or redesign accessories or asymmetric details. Do not change identity, face, hair, colors, costume or proportions. No turn, side view, walking, large motion, gaze drift, repeated blink, abrupt switch, morphing, flicker, deformed hands, extra limbs or duplicate parts. Fix position, scale and facing. No camera motion, zoom, perspective change, blur, shadow, reflection, particles, external hands, props, text, logos or watermarks. Keep every background pixel uniformly #00FF00.
```

## 推荐动作写法

人物性格按“内向、娇羞、可爱”处理，动作幅度宁小勿大。

| 表情 | `{EXPRESSION}` | `{ACTION}` |
|---|---|---|
| Happy | a restrained warm happy smile | eyes soften, shoulders lift slightly, hands gently gather near the chest |
| Shy | a bashful shy smile with light blush | chin lowers slightly, gaze stays forward, hands meet gently in front of the chest |
| Sleepy | a soft sleepy expression with half-closed eyes | head lowers slightly and shoulders relax, without swaying |
| Surprised | a cute mild surprised expression, not frightened | eyes widen slightly, shoulders rise once, hands lift only a little |
| Pouting | a small cute pout with mildly puffed cheeks | brows lower slightly and hands rest close to the waist |
| Confused | a gentle puzzled expression | head tilts slightly, one hand rises near the chest, then holds |
| Proud | a quiet pleased and modestly proud smile | posture straightens slightly and hands settle neatly in front |
| Worried | a hesitant worried expression with a tiny frown | shoulders draw inward and hands clasp softly near the chest |
| Crying | a cute teary sad expression, not distressed | shoulders lower and hands rise near the face without covering it |
| Affection | a soft affectionate smile with light blush | hands gather over the chest and head tilts only slightly |

## 单表情降级模板

复杂表情出现变形、饰品漂移或过渡不清晰时，只保留一个表情：使用相同正文，把 `2.8-4.2s` 改为继续保持第一个表情，把 `4.2-4.8s` 用于回到 Idle。单个视频只承载一个复杂表情通常更稳定。

## 截帧建议

- Idle：从 `0.0-0.6s` 取 3–5 帧，避开首帧压缩异常。
- Blink：从 `0.6-1.0s` 取睁眼、半闭、全闭、半开、睁眼。
- 表情 A：过渡帧取自 `1.0-1.5s`，稳定终态取自 `1.5-2.3s`。
- 表情 B：过渡帧取自 `2.8-3.3s`，稳定终态取自 `3.3-4.2s`。
- 回程不必单独生成；程序可倒序播放对应入场过渡帧。

截帧后仍需统一完成绿幕去除、颜色校正、边缘去绿、320×320 对齐和透明边缘检查。
