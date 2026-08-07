using System.Text.Encodings.Web;
using System.Text.Json;

namespace CastoPet.StabilityReport;

public static class StabilityReportHtml
{
    private const int MaximumChartPoints = 1200;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.Default,
    };

    public static string Render(StabilityReportAnalysis analysis, IReadOnlyList<ReportSample> samples)
    {
        ArgumentNullException.ThrowIfNull(analysis);
        ArgumentNullException.ThrowIfNull(samples);

        var series = new Dictionary<string, object>
        {
            ["petCpu"] = ChartSeries("CastoPet", "%", "#a897ff", Select(samples, "pet", sample => sample.CpuPercent)),
            ["gameCpu"] = ChartSeries("游戏", "%", "#55d6c2", Select(samples, "game", sample => sample.CpuPercent)),
            ["systemCpu"] = ChartSeries("系统", "%", "#ffc66d", SelectSystem(samples, sample => sample.SystemCpuPercent)),
            ["petPrivate"] = ChartSeries("私有内存", "MiB", "#a897ff", Select(samples, "pet", sample => ToMiB(sample.PrivateBytes))),
            ["petWorking"] = ChartSeries("工作集", "MiB", "#66c7ff", Select(samples, "pet", sample => ToMiB(sample.WorkingSetBytes))),
            ["gamePrivate"] = ChartSeries("私有内存", "MiB", "#55d6c2", Select(samples, "game", sample => ToMiB(sample.PrivateBytes))),
            ["gameWorking"] = ChartSeries("工作集", "MiB", "#ff9f7b", Select(samples, "game", sample => ToMiB(sample.WorkingSetBytes))),
            ["petHandles"] = ChartSeries("句柄", "个", "#a897ff", Select(samples, "pet", sample => sample.HandleCount)),
            ["petThreads"] = ChartSeries("线程", "个", "#55d6c2", Select(samples, "pet", sample => sample.ThreadCount)),
            ["petGdi"] = ChartSeries("GDI", "个", "#ffc66d", Select(samples, "pet", sample => sample.GdiObjects)),
            ["petUser"] = ChartSeries("USER", "个", "#ff8fbc", Select(samples, "pet", sample => sample.UserObjects)),
            ["gameHandles"] = ChartSeries("句柄", "个", "#55d6c2", Select(samples, "game", sample => sample.HandleCount)),
            ["gameThreads"] = ChartSeries("线程", "个", "#ff9f7b", Select(samples, "game", sample => sample.ThreadCount)),
            ["availableMemory"] = ChartSeries("可用内存", "GiB", "#66c7ff", SelectSystem(samples, sample => ToGiB(sample.SystemAvailableMemoryBytes))),
        };

        var payload = JsonSerializer.Serialize(new { analysis, series }, JsonOptions)
            .Replace("</script", "<\\/script", StringComparison.OrdinalIgnoreCase);
        return Template.Replace("__REPORT_DATA__", payload, StringComparison.Ordinal);
    }

    private static object ChartSeries(string label, string unit, string color, IReadOnlyList<ChartPoint> points) => new
    {
        label,
        unit,
        color,
        points = ChartDownsampler.MinMax(points, MaximumChartPoints).Select(point => new[] { point.X, point.Y }).ToArray(),
    };

    private static IReadOnlyList<ChartPoint> Select(
        IEnumerable<ReportSample> samples,
        string role,
        Func<ReportSample, double?> selector) => samples
        .Where(sample => sample.Role == role && sample.Running)
        .Select(sample => (Sample: sample, Value: selector(sample)))
        .Where(item => item.Value is not null)
        .Select(item => new ChartPoint(item.Sample.ElapsedSeconds, item.Value!.Value))
        .OrderBy(point => point.X)
        .ToArray();

    private static IReadOnlyList<ChartPoint> SelectSystem(
        IEnumerable<ReportSample> samples,
        Func<ReportSample, double?> selector) => samples
        .Where(sample => sample.Role == "pet")
        .Select(sample => (Sample: sample, Value: selector(sample)))
        .Where(item => item.Value is not null)
        .Select(item => new ChartPoint(item.Sample.ElapsedSeconds, item.Value!.Value))
        .OrderBy(point => point.X)
        .ToArray();

    private static double? ToMiB(long? bytes) => bytes is null ? null : bytes.Value / 1024d / 1024;
    private static double? ToGiB(ulong? bytes) => bytes is null ? null : bytes.Value / 1024d / 1024 / 1024;

    private const string Template = """
<!doctype html>
<html lang="zh-CN">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width,initial-scale=1">
  <title>CastoPet 稳定性报告</title>
  <style>
    :root{color-scheme:dark;--bg:#090b11;--panel:rgba(24,26,36,.82);--panel-2:rgba(31,33,45,.68);--line:rgba(205,210,230,.13);--text:#f1f2f7;--muted:#9a9eae;--violet:#a897ff;--mint:#55d6c2;--amber:#ffc66d;--danger:#ff7d91}
    *{box-sizing:border-box}html{background:var(--bg)}body{margin:0;min-width:320px;color:var(--text);font-family:"Microsoft YaHei UI","HarmonyOS Sans SC","Segoe UI",sans-serif;background-color:var(--bg);background-image:linear-gradient(rgba(168,151,255,.035) 1px,transparent 1px),linear-gradient(90deg,rgba(168,151,255,.035) 1px,transparent 1px),linear-gradient(135deg,#090b11 0%,#11131b 52%,#0b1013 100%);background-size:28px 28px,28px 28px,100% 100%;letter-spacing:0}
    body:before{content:"";position:fixed;inset:0;pointer-events:none;background:linear-gradient(115deg,transparent 0 38%,rgba(168,151,255,.045) 38% 42%,transparent 42% 100%)}
    button{font:inherit}.shell{width:min(1480px,calc(100% - 40px));margin:0 auto;padding:34px 0 60px;position:relative}.masthead{display:flex;align-items:flex-end;justify-content:space-between;gap:24px;padding:12px 2px 26px;border-bottom:1px solid var(--line)}
    .eyebrow{margin:0 0 8px;color:var(--mint);font-size:12px;font-weight:700;text-transform:uppercase}.masthead h1{font-family:"Microsoft JhengHei UI","Microsoft YaHei UI",sans-serif;font-size:clamp(26px,4vw,44px);line-height:1.05;margin:0;font-weight:650}.meta{margin-top:12px;color:var(--muted);font-size:13px}.session-stamp{text-align:right;color:var(--muted);font-size:12px;line-height:1.8}.session-stamp strong{display:block;color:var(--text);font-size:15px}
    .verdict{margin:24px 0;display:grid;grid-template-columns:minmax(210px,.55fr) 1.45fr;border:1px solid rgba(168,151,255,.32);background:rgba(27,27,40,.74);backdrop-filter:blur(18px) saturate(125%);box-shadow:0 20px 60px rgba(0,0,0,.24),inset 0 1px rgba(255,255,255,.06)}
    .verdict-main{padding:26px;border-right:1px solid var(--line);position:relative;overflow:hidden}.verdict-main:after{content:"";position:absolute;left:0;bottom:0;width:100%;height:3px;background:linear-gradient(90deg,var(--violet),var(--mint))}.verdict-label{color:var(--muted);font-size:12px}.verdict-state{font-size:32px;margin-top:7px;font-weight:680}.verdict-state.stable{color:var(--mint)}.verdict-state.watch{color:var(--amber)}.verdict-state.issue{color:var(--danger)}
    .findings{padding:20px 26px;margin:0;display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:12px 26px;list-style:none}.findings li{position:relative;padding-left:16px;color:#c9ccd7;font-size:13px;line-height:1.6}.findings li:before{content:"";position:absolute;left:0;top:.65em;width:5px;height:5px;background:var(--violet);transform:rotate(45deg)}
    .toolbar{position:sticky;top:0;z-index:10;display:flex;justify-content:space-between;align-items:center;gap:16px;padding:12px 0;background:rgba(9,11,17,.84);backdrop-filter:blur(16px);border-bottom:1px solid var(--line)}.tabs,.range{display:flex;gap:4px}.tabs button,.range button{border:1px solid transparent;background:transparent;color:var(--muted);padding:9px 14px;cursor:pointer}.tabs button:hover,.range button:hover{color:var(--text)}.tabs button.active{color:var(--text);border-color:rgba(168,151,255,.28);background:rgba(168,151,255,.12)}.range button{padding:7px 10px;font-size:12px}.range button.active{color:#11131a;background:var(--violet)}
    .view{display:none;padding-top:20px}.view.active{display:block}.metric-grid{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:10px;margin-bottom:18px}.metric{min-height:112px;padding:18px;border:1px solid var(--line);background:var(--panel-2);backdrop-filter:blur(15px)}.metric .label{color:var(--muted);font-size:12px}.metric .value{margin-top:12px;font-size:24px;font-variant-numeric:tabular-nums;font-weight:620}.metric .sub{margin-top:7px;color:#7f8496;font-size:11px}
    .chart-grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:12px}.chart-card{position:relative;min-width:0;border:1px solid var(--line);background:var(--panel);backdrop-filter:blur(18px);padding:18px;box-shadow:0 12px 30px rgba(0,0,0,.14)}.chart-card.wide{grid-column:1/-1}.chart-head{display:flex;justify-content:space-between;align-items:flex-start;gap:16px;margin-bottom:10px}.chart-head h2{font-size:15px;margin:0;font-weight:620}.chart-head p{font-size:11px;color:var(--muted);margin:5px 0 0}.legend{display:flex;flex-wrap:wrap;justify-content:flex-end;gap:10px;color:var(--muted);font-size:11px}.legend span:before{content:"";display:inline-block;width:7px;height:7px;margin-right:5px;background:var(--series-color)}.chart-wrap{height:260px;position:relative}.chart-wrap canvas{width:100%;height:100%;display:block}.tooltip{display:none;position:absolute;z-index:3;pointer-events:none;min-width:150px;padding:10px 12px;background:rgba(8,10,15,.94);border:1px solid rgba(168,151,255,.35);box-shadow:0 10px 30px rgba(0,0,0,.35);font-size:11px;color:var(--muted)}.tooltip b{color:var(--text);font-weight:600}.tooltip-row{display:flex;justify-content:space-between;gap:18px;margin-top:5px}.tooltip-row i{width:6px;height:6px;background:var(--series-color);display:inline-block;margin-right:6px}
    .detail-table{width:100%;border-collapse:collapse;font-size:12px}.detail-table th,.detail-table td{text-align:left;padding:12px;border-bottom:1px solid var(--line);font-variant-numeric:tabular-nums}.detail-table th{color:var(--muted);font-weight:500}.detail-table td{color:#d8dae3}.event-list{display:grid;gap:8px}.event{display:grid;grid-template-columns:160px 150px 1fr 90px;gap:12px;padding:13px 15px;background:rgba(255,255,255,.025);border-left:2px solid var(--violet);font-size:12px}.event time,.event .pid{color:var(--muted)}.event .type{color:var(--mint);font-weight:600}.notice{margin-top:18px;padding:16px 18px;border:1px solid rgba(255,198,109,.24);background:rgba(255,198,109,.055);color:#c9c3b5;font-size:12px;line-height:1.7}.footer{margin-top:24px;padding-top:18px;border-top:1px solid var(--line);display:flex;justify-content:space-between;color:#727787;font-size:11px}
    @media(max-width:900px){.shell{width:min(100% - 24px,1480px)}.masthead{align-items:flex-start;flex-direction:column}.session-stamp{text-align:left}.verdict{grid-template-columns:1fr}.verdict-main{border-right:0;border-bottom:1px solid var(--line)}.findings{grid-template-columns:1fr}.metric-grid{grid-template-columns:repeat(2,minmax(0,1fr))}.chart-grid{grid-template-columns:1fr}.chart-card.wide{grid-column:auto}.toolbar{align-items:flex-start;flex-direction:column}.event{grid-template-columns:1fr 1fr}.event .message{grid-column:1/-1}}
    @media(max-width:520px){.metric-grid{grid-template-columns:1fr}.tabs{width:100%;overflow:auto}.tabs button{white-space:nowrap;padding-inline:10px}.chart-card{padding:14px}.chart-wrap{height:230px}}
    @media(prefers-reduced-motion:no-preference){.verdict,.metric,.chart-card{animation:enter .45s ease both}.metric:nth-child(2){animation-delay:.04s}.metric:nth-child(3){animation-delay:.08s}.metric:nth-child(4){animation-delay:.12s}@keyframes enter{from{opacity:0;transform:translateY(8px)}to{opacity:1;transform:none}}}
  </style>
</head>
<body>
  <main class="shell">
    <header class="masthead"><div><p class="eyebrow">Runtime observation / local report</p><h1>CastoPet 稳定性报告</h1><div class="meta" id="session-meta"></div></div><div class="session-stamp"><span>测试区间</span><strong id="session-range"></strong><span id="sample-count"></span></div></header>
    <section class="verdict"><div class="verdict-main"><div class="verdict-label">自动判定</div><div class="verdict-state" id="verdict-state"></div></div><ul class="findings" id="findings"></ul></section>
    <nav class="toolbar"><div class="tabs" role="tablist"><button class="active" data-tab="overview">总览</button><button data-tab="pet">CastoPet</button><button data-tab="game">游戏</button><button data-tab="system">系统与事件</button></div><div class="range" aria-label="时间范围"><button data-hours="0" class="active">全部</button><button data-hours="6">6 小时</button><button data-hours="1">1 小时</button><button data-hours=".5">30 分钟</button></div></nav>
    <section class="view active" id="overview"><div class="metric-grid" id="overview-metrics"></div><div class="chart-grid"><article class="chart-card wide" data-chart="petCpu,gameCpu,systemCpu"><div class="chart-head"><div><h2>处理器占用</h2><p>进程值已按逻辑处理器数量归一化</p></div><div class="legend"></div></div><div class="chart-wrap"><canvas></canvas><div class="tooltip"></div></div></article><article class="chart-card" data-chart="petPrivate,petWorking"><div class="chart-head"><div><h2>桌宠内存</h2><p>私有内存与驻留工作集</p></div><div class="legend"></div></div><div class="chart-wrap"><canvas></canvas><div class="tooltip"></div></div></article><article class="chart-card" data-chart="gamePrivate,gameWorking"><div class="chart-head"><div><h2>游戏内存</h2><p>仅观察，不代表由桌宠导致</p></div><div class="legend"></div></div><div class="chart-wrap"><canvas></canvas><div class="tooltip"></div></div></article><article class="chart-card wide" data-chart="availableMemory"><div class="chart-head"><div><h2>系统可用内存</h2><p>用于定位整机内存压力时段</p></div><div class="legend"></div></div><div class="chart-wrap"><canvas></canvas><div class="tooltip"></div></div></article></div></section>
    <section class="view" id="pet"><div class="metric-grid" id="pet-metrics"></div><div class="chart-grid"><article class="chart-card wide" data-chart="petCpu"><div class="chart-head"><div><h2>CastoPet CPU</h2><p>一秒采样曲线</p></div><div class="legend"></div></div><div class="chart-wrap"><canvas></canvas><div class="tooltip"></div></div></article><article class="chart-card" data-chart="petPrivate,petWorking"><div class="chart-head"><div><h2>内存轨迹</h2><p>观察稳态斜率和回收行为</p></div><div class="legend"></div></div><div class="chart-wrap"><canvas></canvas><div class="tooltip"></div></div></article><article class="chart-card" data-chart="petHandles,petThreads"><div class="chart-head"><div><h2>句柄与线程</h2><p>持续单向增长需要进一步排查</p></div><div class="legend"></div></div><div class="chart-wrap"><canvas></canvas><div class="tooltip"></div></div></article><article class="chart-card wide" data-chart="petGdi,petUser"><div class="chart-head"><div><h2>Windows 对象</h2><p>GDI 与 USER 对象计数</p></div><div class="legend"></div></div><div class="chart-wrap"><canvas></canvas><div class="tooltip"></div></div></article></div><div class="chart-card" style="margin-top:12px"><div class="chart-head"><div><h2>桌宠详细统计</h2><p>精确值来自全部原始样本</p></div></div><table class="detail-table" id="pet-table"></table></div></section>
    <section class="view" id="game"><div class="metric-grid" id="game-metrics"></div><div class="chart-grid"><article class="chart-card wide" data-chart="gameCpu"><div class="chart-head"><div><h2>游戏 CPU</h2><p>ZenlessZoneZero 进程观测</p></div><div class="legend"></div></div><div class="chart-wrap"><canvas></canvas><div class="tooltip"></div></div></article><article class="chart-card" data-chart="gamePrivate,gameWorking"><div class="chart-head"><div><h2>游戏内存</h2><p>不同场景与资源加载会造成显著波动</p></div><div class="legend"></div></div><div class="chart-wrap"><canvas></canvas><div class="tooltip"></div></div></article><article class="chart-card" data-chart="gameHandles,gameThreads"><div class="chart-head"><div><h2>游戏资源计数</h2><p>句柄与线程变化</p></div><div class="legend"></div></div><div class="chart-wrap"><canvas></canvas><div class="tooltip"></div></div></article></div><div class="notice">本次测试没有“不启动 CastoPet”的相同场景基线，也未采集 FPS 和帧时间。此页能够说明游戏进程和系统在测试期间的状态，不能单独证明 CastoPet 对游戏性能造成或没有造成影响。</div><div class="chart-card" style="margin-top:12px"><div class="chart-head"><div><h2>游戏详细统计</h2><p>受反作弊保护的不可读字段保持为空</p></div></div><table class="detail-table" id="game-table"></table></div></section>
    <section class="view" id="system"><div class="metric-grid" id="system-metrics"></div><div class="chart-grid"><article class="chart-card" data-chart="systemCpu"><div class="chart-head"><div><h2>系统 CPU</h2><p>整机总占用</p></div><div class="legend"></div></div><div class="chart-wrap"><canvas></canvas><div class="tooltip"></div></div></article><article class="chart-card" data-chart="availableMemory"><div class="chart-head"><div><h2>可用内存</h2><p>低谷用于识别内存压力</p></div><div class="legend"></div></div><div class="chart-wrap"><canvas></canvas><div class="tooltip"></div></div></article></div><article class="chart-card" style="margin-top:12px"><div class="chart-head"><div><h2>事件时间线</h2><p>启动、附加、退出、重启与采样异常</p></div></div><div class="event-list" id="event-list"></div></article></section>
    <footer class="footer"><span>CastoPet StabilityReport · 单文件离线报告</span><span>曲线经极值保留降采样，统计值使用全部样本</span></footer>
  </main>
  <script id="data-report" type="application/json">__REPORT_DATA__</script>
  <script>
    const report=JSON.parse(document.getElementById('data-report').textContent),a=report.analysis,s=report.series;let rangeHours=0;
    const q=(x,r=document)=>r.querySelector(x),qa=(x,r=document)=>[...r.querySelectorAll(x)],esc=(v)=>String(v).replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
    const number=(v,d=2)=>v==null?'—':Number(v).toLocaleString('zh-CN',{maximumFractionDigits:d,minimumFractionDigits:d});
    const bytes=(v)=>{if(v==null)return'—';const u=['B','KiB','MiB','GiB'],i=Math.min(3,Math.floor(Math.log(Math.max(1,v))/Math.log(1024)));return number(v/1024**i,i>1?2:0)+' '+u[i]};
    const signedBytes=(v)=>v==null?'—':(v>=0?'+':'')+number(v/1024/1024,3)+' MiB/h';
    const duration=(h)=>h>=1?number(h,2)+' 小时':number(h*60,1)+' 分钟';
    const local=(v)=>new Date(v).toLocaleString('zh-CN',{hour12:false});
    q('#session-range').textContent=local(a.startedUtc)+' — '+local(a.endedUtc);q('#session-meta').textContent=`持续 ${duration(a.durationHours)} · 每秒采样 · 离线分析`;q('#sample-count').textContent=`${a.pet.sampleCount.toLocaleString()} 个采样周期`;
    const state=q('#verdict-state');state.textContent=a.status;state.className='verdict-state '+(a.status==='稳定'?'stable':a.status==='需要观察'?'watch':'issue');q('#findings').innerHTML=a.findings.map(x=>`<li>${esc(x)}</li>`).join('');
    function cards(id,items){q(id).innerHTML=items.map(x=>`<div class="metric"><div class="label">${x[0]}</div><div class="value">${x[1]}</div><div class="sub">${x[2]||''}</div></div>`).join('')}
    cards('#overview-metrics',[['运行结论',a.status,'基于桌宠资源趋势与采样完整性'],['桌宠平均 CPU',number(a.pet.cpu.average,3)+' %','P95 '+number(a.pet.cpu.p95,3)+' %'],['桌宠私有内存',bytes(a.pet.privateEndBytes),'峰值 '+bytes(a.pet.privateMaximumBytes)],['系统最低可用内存',bytes(a.system.availableMemoryMinimumBytes),'平均 '+bytes(a.system.availableMemoryAverageBytes)]]);
    cards('#pet-metrics',[['稳态内存斜率',signedBytes(a.pet.privateSteadySlopeBytesPerHour),'从进程启动 5 分钟后计算'],['句柄变化',`${a.pet.handleStart??'—'} → ${a.pet.handleEnd??'—'}`,'峰值 '+(a.pet.handleMaximum??'—')],['线程变化',`${a.pet.threadStart??'—'} → ${a.pet.threadEnd??'—'}`,'峰值 '+(a.pet.threadMaximum??'—')],['前台占比',number(a.pet.foregroundPercent,2)+' %','桌宠成为前台窗口的样本占比']]);
    cards('#game-metrics',[['运行时长',duration(a.game.runningHours),'游戏退出后继续记录空状态'],['平均 CPU',number(a.game.cpu.average,2)+' %','P95 '+number(a.game.cpu.p95,2)+' %'],['私有内存峰值',bytes(a.game.privateMaximumBytes),'结束前 '+bytes(a.game.privateEndBytes)],['句柄峰值',number(a.game.handleMaximum,0),'线程峰值 '+number(a.game.threadMaximum,0)]]);
    cards('#system-metrics',[['平均 CPU',number(a.system.cpu.average,2)+' %','P95 '+number(a.system.cpu.p95,2)+' %'],['CPU 峰值',number(a.system.cpu.maximum,2)+' %','整机瞬时占用'],['采样间隔 P95',number(a.system.sampleGapP95Seconds,3)+' 秒','最大 '+number(a.system.sampleGapMaximumSeconds,3)+' 秒'],['生命周期事件',a.events.length.toLocaleString(),'无事件不等于无样本']]);
    function table(id,r){q(id).innerHTML=`<thead><tr><th>指标</th><th>起始</th><th>结束</th><th>峰值 / 趋势</th></tr></thead><tbody>${[['工作集',bytes(r.workingSetStartBytes),bytes(r.workingSetEndBytes),bytes(r.workingSetMaximumBytes)],['私有内存',bytes(r.privateStartBytes),bytes(r.privateEndBytes),bytes(r.privateMaximumBytes)],['句柄',r.handleStart??'—',r.handleEnd??'—',r.handleMaximum??'—'],['线程',r.threadStart??'—',r.threadEnd??'—',r.threadMaximum??'—'],['GDI',r.gdiStart??'—',r.gdiEnd??'—',r.gdiMaximum??'—'],['USER',r.userStart??'—',r.userEnd??'—',r.userMaximum??'—'],['CPU','平均 '+number(r.cpu.average,3)+' %','P95 '+number(r.cpu.p95,3)+' %','峰值 '+number(r.cpu.maximum,3)+' %']].map(x=>`<tr>${x.map(y=>`<td>${y}</td>`).join('')}</tr>`).join('')}</tbody>`}
    table('#pet-table',a.pet);table('#game-table',a.game);
    q('#event-list').innerHTML=a.events.length?a.events.map(e=>`<div class="event"><time>${local(e.timestampUtc)}</time><span class="type">${esc(e.type)}</span><span class="message">${esc(e.message)}</span><span class="pid">PID ${e.processId??'—'}</span></div>`).join(''):'<div class="notice">没有记录到生命周期事件。</div>';
    qa('[data-tab]').forEach(b=>b.onclick=()=>{qa('[data-tab]').forEach(x=>x.classList.toggle('active',x===b));qa('.view').forEach(x=>x.classList.toggle('active',x.id===b.dataset.tab));requestAnimationFrame(renderVisible)});
    qa('[data-hours]').forEach(b=>b.onclick=()=>{rangeHours=Number(b.dataset.hours);qa('[data-hours]').forEach(x=>x.classList.toggle('active',x===b));renderVisible()});
    function yText(v,unit){if(unit==='MiB'&&Math.abs(v)>=2048)return number(v/1024,1)+' GiB';return number(v,unit==='%'?1:unit==='个'?0:2)+' '+unit}
    function timeText(sec){const h=Math.floor(sec/3600),m=Math.floor(sec%3600/60);return h?`${h}h ${m}m`:`${m}m`}
    function render(card){const names=card.dataset.chart.split(','),sets=names.map(n=>s[n]).filter(Boolean),wrap=q('.chart-wrap',card),canvas=q('canvas',card),tip=q('.tooltip',card),legend=q('.legend',card);legend.innerHTML=sets.map(x=>`<span style="--series-color:${x.color}">${x.label}</span>`).join('');const end=a.durationHours*3600,start=rangeHours?Math.max(0,end-rangeHours*3600):0,filtered=sets.map(x=>({...x,points:x.points.filter(p=>p[0]>=start)}));const rect=wrap.getBoundingClientRect(),dpr=Math.min(2,devicePixelRatio||1),w=Math.max(300,rect.width),h=Math.max(180,rect.height);canvas.width=w*dpr;canvas.height=h*dpr;const c=canvas.getContext('2d');c.setTransform(dpr,0,0,dpr,0,0);c.clearRect(0,0,w,h);const pad={l:58,r:16,t:12,b:30},pw=w-pad.l-pad.r,ph=h-pad.t-pad.b,vals=filtered.flatMap(x=>x.points.map(p=>p[1]));if(!vals.length)return;let ymin=Math.min(...vals),ymax=Math.max(...vals);const delta=(ymax-ymin)||Math.max(1,Math.abs(ymax)*.1);ymin=Math.max(0,ymin-delta*.08);ymax+=delta*.12;const xx=x=>pad.l+(x-start)/Math.max(1,end-start)*pw,yy=y=>pad.t+(ymax-y)/(ymax-ymin)*ph;c.font='10px Microsoft YaHei UI';c.lineWidth=1;c.textBaseline='middle';for(let i=0;i<5;i++){const y=pad.t+ph*i/4,v=ymax-(ymax-ymin)*i/4;c.strokeStyle='rgba(205,210,230,.09)';c.beginPath();c.moveTo(pad.l,y);c.lineTo(w-pad.r,y);c.stroke();c.fillStyle='#777c8d';c.textAlign='right';c.fillText(yText(v,sets[0].unit),pad.l-9,y)}for(let i=0;i<5;i++){const x=pad.l+pw*i/4,sec=start+(end-start)*i/4;c.fillStyle='#777c8d';c.textAlign=i===0?'left':i===4?'right':'center';c.fillText(timeText(sec),x,h-10)}filtered.forEach(set=>{c.strokeStyle=set.color;c.lineWidth=1.6;c.lineJoin='round';c.beginPath();set.points.forEach((p,i)=>i?c.lineTo(xx(p[0]),yy(p[1])):c.moveTo(xx(p[0]),yy(p[1])));c.stroke()});canvas._chart={filtered,start,end,xx,yy,w,h,pad};if(!canvas.dataset.bound){canvas.dataset.bound='1';canvas.addEventListener('pointerleave',()=>tip.style.display='none');canvas.addEventListener('pointermove',e=>hover(canvas,tip,e))}}
    function hover(canvas,tip,e){const m=canvas._chart;if(!m)return;const r=canvas.getBoundingClientRect(),px=e.clientX-r.left,sec=m.start+(px-m.pad.l)/(m.w-m.pad.l-m.pad.r)*(m.end-m.start);if(sec<m.start||sec>m.end){tip.style.display='none';return}const rows=m.filtered.map(set=>{let best=null,d=Infinity;for(const p of set.points){const z=Math.abs(p[0]-sec);if(z<d){d=z;best=p}}return best?{set,p:best}:null}).filter(Boolean);if(!rows.length)return;tip.innerHTML=`<b>${timeText(rows[0].p[0])}</b>${rows.map(x=>`<div class="tooltip-row"><span><i style="--series-color:${x.set.color}"></i>${x.set.label}</span><b>${yText(x.p[1],x.set.unit)}</b></div>`).join('')}`;tip.style.display='block';tip.style.left=Math.min(m.w-170,Math.max(4,px+12))+'px';tip.style.top='8px'}
    function renderVisible(){qa('.view.active [data-chart]').forEach(render)}new ResizeObserver(()=>renderVisible()).observe(document.querySelector('.shell'));renderVisible();
  </script>
</body>
</html>
""";
}
