# Castorice 表情生成工作区

本目录集中保存 Castorice 桌宠表情扩展所需的提示词、状态清单、生成记录和中间文件说明。以后进行视频生成、截帧或接入程序时，从这里开始，不再把过程文件散落到 `docs/` 或仓库根目录。

## 文件

- [`prompt-template.md`](prompt-template.md)：长期使用的 6 秒视频生成提示词、动作词典、输出命名、后处理与验收规则。
- [`expression-status.md`](expression-status.md)：已经生成并投入使用的表情/动作，以及仍处于候选状态的扩展表情。
- [`generation-records.md`](generation-records.md)：历史视频记录和以后每次生成必须填写的记录模板。
- [`intermediate/`](intermediate/)：源视频、采样帧、候选帧、抠图帧、调色帧、320×320 输出、接触表和处理脚本。

## 边界

- 权威人物绿幕图仍位于 `src/CastoPet/Assets/CandidateSet/Source/Castorice.png`。
- 程序实际使用的正式资源仍位于 `src/CastoPet/Assets/Runtime/Castorice/`，不得移动到本工作区。
- `intermediate/` 中的大体积过程文件保留在本地但不提交 Git；其目录说明和记录文档需要提交。
- 候选资源通过验收并写入 `skin.json` 后，才可在状态表中标记为“已接入”。

## 推荐流程

1. 在 `generation-records.md` 复制一份新记录并先填写计划表情、平台、参考图和完整提示词。
2. 将下载的原始 MP4 放入 `intermediate/source-videos/<记录 ID>/`。
3. 逐帧检查并人工选择候选帧；禁止机械等间隔截帧。
4. 按“去绿幕 → 边缘去绿 → 人物区域调色 → 320×320 对齐”的顺序处理。
5. 生成接触表并按 `prompt-template.md` 的验收清单复核。
6. 合格后按命名规范复制到正式运行时目录，更新 `skin.json`、测试和 `expression-status.md`。
