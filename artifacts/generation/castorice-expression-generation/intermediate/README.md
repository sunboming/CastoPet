# 中间文件目录

本目录保存可复现生成流程所需的大体积本地文件，默认不提交 Git。

建议结构：

```text
intermediate/
  source-videos/<记录 ID>/       原始下载 MP4，不转码、不覆盖
  sampled-frames/<记录 ID>/      为审查提取的全部或高频采样帧
  selected-frames/<记录 ID>/     人工挑选的候选帧
  keyed/<记录 ID>/               已去绿幕、尚未调色的透明帧
  color-corrected/<记录 ID>/     已去边缘溢色并按权威图校色的帧
  resized-320/<记录 ID>/         对齐后的 320×320 候选成品
  contact-sheets/<记录 ID>/      带帧号/时间点的接触表
  scripts/<记录 ID>/             该批次实际运行的处理脚本和参数
  legacy-2026-08-06/             本对话早期处理中已有的 tmp 内容
```

规则：

- 文件夹使用与 `generation-records.md` 完全相同的记录 ID。
- 原始视频只读保留；派生文件写入后续阶段目录。
- 禁止把 `selected-frames` 当作机械等间隔采样结果；每帧必须经过人工视觉判断。
- 正式运行时资源不放在这里，验收后复制到 `src/CastoPet/Assets/Runtime/Castorice/`。
