# Castorice 长期视频表情扩展提示词模板

## 用途

用于生成可拆分为桌宠帧动画的 6 秒绿幕视频。模板优先保证角色一致性、饰品方向、稳定构图和表情过渡，适合后续扩展轮盘表情。

当前生成和接入情况见 [`expression-status.md`](expression-status.md)，每次实际生成必须登记到 [`generation-records.md`](generation-records.md)。

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
| Excited | a bright but restrained excited smile | shoulders lift once and hands gather eagerly near the chest |
| Bored | a quiet bored expression with relaxed half-lidded eyes | shoulders relax and the head lowers only slightly |
| Affection | a soft affectionate smile with light blush | hands gather over the chest and head tilts only slightly |

## 单表情降级模板

复杂表情出现变形、饰品漂移或过渡不清晰时，只保留一个表情：使用相同正文，把 `2.8-4.2s` 改为继续保持第一个表情，把 `4.2-4.8s` 用于回到 Idle。单个视频只承载一个复杂表情通常更稳定。

## 输出命名与目录规范

正式资源根目录：`src/CastoPet/Assets/Runtime/Castorice/`。

单个表情 `{Name}` 必须使用 PascalCase 英文语义名：

```text
Expressions/
  Castorice.Expression.{Name}.png
  {Name}/
    Transition/
      Castorice.Expression.{Name}.Transition.00.png
      Castorice.Expression.{Name}.Transition.01.png
      Castorice.Expression.{Name}.Transition.02.png
      Castorice.Expression.{Name}.Transition.03.png
      Castorice.Expression.{Name}.Transition.04.png
      Castorice.Expression.{Name}.Transition.05.png
```

- 终态文件必须是经过验收的 320×320 透明 PNG。
- 入场过渡固定保留 6 帧，编号从 `.00` 到 `.05`，不得跳号。
- `.00` 应无跳变衔接标准 Idle；`.05` 应自然衔接表情终态。
- 退场不生成单独文件，程序倒序播放同一组 Transition。
- 写入正式目录后同步更新 `skin.json`、资源测试和 `expression-status.md`。

## 截帧与筛选

- Idle：从 `0.0-0.6s` 取 3–5 帧，避开首帧压缩异常。
- Blink：从 `0.6-1.0s` 取睁眼、半闭、全闭、半开、睁眼。
- 表情 A：过渡帧取自 `1.0-1.5s`，稳定终态取自 `1.5-2.3s`。
- 表情 B：过渡帧取自 `2.8-3.3s`，稳定终态取自 `3.3-4.2s`。
- 回程不必单独生成；程序可倒序播放对应入场过渡帧。

**不要机械等间隔截帧。** 先以较高频率提取审查帧并制作带时间点的接触表，再人工选择动作连续、人物一致的 6 帧。生成视频中若某帧出现眼神漂移、饰品换边、突然转头、身体缩放、肢体幅度过大或细节形变，应直接舍弃；不得为了凑满 6 帧强行使用。合格帧不足时重新生成或改用单表情模板。

## 后处理顺序

1. 始终以 `src/CastoPet/Assets/CandidateSet/Source/Castorice.png` 为颜色权威。
2. 先对原尺寸帧去除绿幕，生成透明 Alpha；不要先整体调色。
3. 再处理半透明边缘的绿色溢色，重点检查浅紫头发、白色衣服、花瓣和手指边缘。
4. 仅对人物的非绿色、非透明区域进行颜色校正，匹配权威图的头发紫色、肤色、眼睛和服装；不得用全图色相偏移污染透明边缘。
5. 完成抠图与调色后再按统一锚点缩放/对齐到 320×320，保持脚底、人物中心、比例和边距一致。
6. 最后检查透明角落、绿色残边、锯齿、白边、颜色跳帧和相邻帧位置跳动。

## 明确验收清单

接入前必须逐项通过：

- [ ] 使用本目录指定的权威绿幕图作为本次上传参考，未使用视频末帧或派生图。
- [ ] 人物身份、脸型、发型、瞳色、服装、身体比例与权威图一致。
- [ ] 每朵花、蝴蝶结、丝带、发饰和不对称服装细节未换边、漂移、增删或重绘。
- [ ] 全程正面、中心和尺寸稳定，无镜像、侧身、透视或镜头运动。
- [ ] 眼睛仅按目标表情变化，无无意眨眼、瞳孔方向漂移或左右眼不同步。
- [ ] 手部无畸形，四肢数量正确，无重影、重复部件或身体融合。
- [ ] 动作符合内向、娇羞、可爱的人设，幅度小且没有突然转头或过度转身。
- [ ] 6 张入场帧经过人工挑选，不是机械等间隔截取，且相邻动作连续。
- [ ] `.00` 可无跳变衔接 Idle，`.05` 可无跳变衔接终态；倒序播放能自然回到 Idle。
- [ ] 终态表情清晰、至少有一段稳定保持区，未混入另一表情。
- [ ] 绿幕已完全透明，浅色头发和白色衣服边缘无绿色反光、绿边、白边或孔洞。
- [ ] 颜色只校正人物非绿色区域，并与权威图一致；同一序列无亮度或色相闪烁。
- [ ] 所有正式 PNG 为 320×320、透明角落、统一锚点、统一人物比例和边距。
- [ ] 文件名、编号、目录、`skin.json` 和测试一致，接触表与生成记录完整。

## 生成记录要求

每次生成都必须在 [`generation-records.md`](generation-records.md) 保存：平台与模型、日期、参考图、实际完整提示词、原始 MP4、视频规格、平台参数、人工选帧时间点、舍弃帧及原因、处理参数、接触表、验收结果、正式输出路径和关联提交。记录必须引用 `intermediate/` 中的真实文件，不得只写“使用通用模板”。
