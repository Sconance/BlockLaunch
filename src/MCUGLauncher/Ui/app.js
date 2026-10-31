/* Phase 1 prototype only. All content is local mock data; no network calls. */
function parseQuery(search) {
  const params = new URLSearchParams(search);
  return {
    screen: params.get('screen') || 'home',
    id: params.get('id') || '',
    state: params.get('state') || 'normal',
    type: params.get('type') || 'server',
    anon: params.get('anon') === '1',
    quote: params.get('quote') || 'text',
    qi: params.has('qi') ? Math.max(0, Math.min(4, Number(params.get('qi')) || 0)) : (params.get('quote') === 'image' || params.get('quote') === 'fallback' ? 1 : 0),
    ann: Math.max(0, Math.min(2, Number(params.get('ann')) || 0)),
    menu: params.get('menu') || '',
    modal: params.get('modal') || '',
    endpoint: params.get('endpoint') === '1',
    merr: params.get('merr') || '',
    tip: params.get('tip') === '1',
    panel: params.get('panel') || '',
    mode: params.get('mode') || 'instance',
    switchMenu: params.get('switch') === '1',
    inst: params.get('inst') || '1.20.1-OptiFine_I6',
    skin: params.get('skin') || '',
    img: params.get('img') === '1',
  };
}

const Q = parseQuery(location.search);

function go(url) {
  const next = new URL(url, location.href);
  if (next.origin !== location.origin || next.pathname !== location.pathname) {
    location.href = next.href;
    return;
  }
  if (next.search !== location.search) {
    history.pushState(null, '', next.pathname + next.search);
  }
  Object.assign(Q, parseQuery(next.search));
  render();
  window.scrollTo(0, 0);
}

window.mcugGo = go;

document.addEventListener('click', (event) => {
  const link = event.target.closest('a[href]');
  if (!link || link.target || event.defaultPrevented || event.button !== 0
      || event.metaKey || event.ctrlKey || event.shiftKey || event.altKey) return;
  const next = new URL(link.href, location.href);
  if (next.origin !== location.origin || next.pathname !== location.pathname) return;
  event.preventDefault();
  go(next.href);
}, true);

window.addEventListener('popstate', () => {
  Object.assign(Q, parseQuery(location.search));
  render();
});

const icon = (name, cls = '') => `<svg class="ico ${cls}" aria-hidden="true"><use href="#i-${name}"></use></svg>`;

const BrandLogo = () => `<img class="brand-logo" src="assets/logo.png" width="34" height="34" alt="">`;

const Avatar = () => `
<svg class="avatar-svg" viewBox="0 0 16 16" shape-rendering="crispEdges" aria-hidden="true">
  <rect width="16" height="16" fill="#BFDCC9"/>
  <rect x="3" y="2" width="10" height="3" fill="#4B3126"/>
  <rect x="2" y="4" width="3" height="7" fill="#4B3126"/>
  <rect x="11" y="4" width="3" height="7" fill="#4B3126"/>
  <rect x="4" y="4" width="8" height="2" fill="#4B3126"/>
  <rect x="4" y="5" width="8" height="6" fill="#DDAA84"/>
  <rect x="4" y="5" width="2" height="2" fill="#4B3126"/>
  <rect x="10" y="5" width="2" height="2" fill="#4B3126"/>
  <rect x="6" y="7" width="1" height="2" fill="#2D2421"/>
  <rect x="9" y="7" width="1" height="2" fill="#2D2421"/>
  <rect x="7" y="9" width="2" height="1" fill="#B97C63"/>
  <rect x="3" y="11" width="10" height="2" fill="#CE966F"/>
  <rect x="2" y="13" width="12" height="3" fill="#235B3A"/>
  <rect x="7" y="13" width="2" height="3" fill="#17452A"/>
</svg>`;

const SkinFigure = () => `
<svg class="avatar-svg" viewBox="0 0 16 24" shape-rendering="crispEdges" aria-hidden="true">
  <rect width="16" height="24" fill="#DCEFE3"/>
  <rect x="3" y="17" width="4" height="6" fill="#1C4B31"/>
  <rect x="9" y="17" width="4" height="6" fill="#1C4B31"/>
  <rect x="2" y="9" width="2" height="8" fill="#CE966F"/>
  <rect x="12" y="9" width="2" height="8" fill="#CE966F"/>
  <rect x="4" y="9" width="8" height="8" fill="#2C6B47"/>
  <rect x="4" y="2" width="8" height="7" fill="#DDAA84"/>
  <rect x="3" y="1" width="10" height="3" fill="#4B3126"/>
  <rect x="2" y="3" width="2" height="6" fill="#4B3126"/>
  <rect x="12" y="3" width="2" height="6" fill="#4B3126"/>
  <rect x="5" y="5" width="2" height="2" fill="#2D2421"/>
  <rect x="9" y="5" width="2" height="2" fill="#2D2421"/>
  <rect x="7" y="7" width="2" height="1" fill="#B97C63"/>
</svg>`;

const QuotePhoto = () => `
<svg viewBox="0 0 208 132" preserveAspectRatio="xMidYMid slice" shape-rendering="crispEdges" aria-hidden="true">
  <defs>
    <linearGradient id="sky" x1="0" y1="0" x2="0" y2="1">
      <stop offset="0" stop-color="#8BC7C9"/><stop offset=".55" stop-color="#C9E3D0"/><stop offset="1" stop-color="#F7D8A8"/>
    </linearGradient>
  </defs>
  <rect width="208" height="132" fill="url(#sky)"/>
  <rect x="146" y="24" width="22" height="22" fill="#FFF0C4"/>
  <rect x="150" y="20" width="14" height="30" fill="#FFF0C4" opacity=".35"/>
  <path d="M0 78 28 58 48 68 70 45 98 70 126 54 158 74 184 58 208 76V132H0Z" fill="#78A88B"/>
  <path d="M0 92 30 80 56 88 82 72 112 90 140 80 170 92 208 82V132H0Z" fill="#4E8063"/>
  <rect y="105" width="208" height="27" fill="#4E8C8F"/>
  <path d="M0 105h208v4H0z" fill="#B8DDC7" opacity=".55"/>
  <path d="M20 63v34M22 74h-8M22 84h-9M184 66v33M186 76h8M186 88h-8" stroke="#315B43" stroke-width="4"/>
  <path d="M8 55h24l-4 9H12ZM176 56h26l-4 9h-18Z" fill="#2E6647"/>
  <path d="M34 93v13M172 96v12" stroke="#2D5A40" stroke-width="3"/>
  <rect x="18" y="118" width="6" height="6" fill="#E8D39B"/>
  <rect x="182" y="116" width="6" height="6" fill="#E8D39B"/>
</svg>`;

const servers = [
  { id:'s1', name:'暮色森林', version:'1.20.1', motd:'轻生存 · 无经济 · 公益', players:12, max:50, ping:32, online:true, color:'green', icon:'server' },
  { id:'s2', name:'红石小镇', version:'1.20.1', motd:'建筑党聚集地 · 每周五合建', players:28, max:60, ping:48, online:true, color:'teal', icon:'box' },
  { id:'s3', name:'旧日港湾', version:'1.19.2', motd:'离线养老 · 不删档', players:0, max:20, ping:null, online:false, color:'slate', icon:'globe' },
  { id:'s4', name:'云杉谷', version:'1.20.1', motd:'原版生存 · 只加防作弊与备份', players:19, max:40, ping:41, online:true, color:'blue', icon:'box' },
];

const announcements = [
  { id:'a1', tag:'更新', title:'0.22.3 社区功能测试开启', text:'群友推荐、服务器状态与数据源切换开始试运行，欢迎反馈。', date:'10/30', time:'10 月 30 日', image:'scene' },
  { id:'a2', tag:'活动', title:'周五晚 8 点 · 群友合建', text:'红石小镇集合，主题是「山谷里的小镇」。', date:'10/29', time:'10 月 29 日' },
  { id:'a3', tag:'提醒', title:'社区数据源维护通知', text:'今晚 23:00 起短暂切换 GitHub Raw 镜像。', date:'10/28', time:'10 月 28 日' },
];

const recs = [
  { id:'r1', type:'server', name:'暮色森林 · 轻生存', author:'阿澈', intro:'无经济、无地皮，只想好好种田。', tags:['1.20.1','公益','轻生存'], players:12, max:50, ping:32, addr:'play.mclc.cn', color:'green', icon:'server' },
  { id:'r2', type:'material', name:'清爽 UI · 1.20', author:'小鹿', intro:'只改界面与字体，保留原版手感。', tags:['16x','1.20','界面'], size:'12.5 MB', extra:'16x', color:'orange', icon:'palette' },
  { id:'r3', type:'shader', name:'Complementary Reimagined', author:'北岸', intro:'柔和的体积光，帧数依然友好。', tags:['1.20.1','中配','体积光'], size:'8.3 MB', extra:'中配', color:'purple', icon:'sun' },
  { id:'r4', type:'server', name:'红石小镇 · 创造服', author:'纸鸢', intro:'建筑党聚集地，每周五一起合建。', tags:['1.20.1','创造','建筑'], players:28, max:60, ping:48, addr:'build.mclc.cn', color:'teal', icon:'box' },
  { id:'r5', type:'material', name:'像素田园 · 32x', author:'栗子', intro:'暖色调的田园风，适合长期生存。', tags:['32x','1.19+','田园'], size:'24.1 MB', extra:'32x', color:'green', icon:'palette' },
  { id:'r6', type:'shader', name:'BSL · 轻量版', author:'阿岚', intro:'老显卡也能开，保留水面反射。', tags:['1.20.1','低配','水面'], size:'3.2 MB', extra:'低配', color:'blue', icon:'sun' },
  { id:'r7', type:'server', name:'旧日港湾 · 离线养老', author:'北岸', intro:'不删档、无管理，适合慢慢玩。', tags:['1.19.2','离线','养老'], players:0, max:20, ping:null, online:false, addr:'old.mclc.cn', color:'slate', icon:'globe' },
  { id:'r8', type:'material', name:'柔光 · 16x', author:'纸鸢', intro:'降低对比度，夜里不那么刺眼。', tags:['16x','1.20','护眼'], size:'6.8 MB', extra:'16x', color:'blue', icon:'palette' },
  { id:'r9', type:'shader', name:'SEUS · 梦幻', author:'小鹿', intro:'高饱和的天空与云层，截图很好看。', tags:['1.20.1','高配','云层'], size:'11.6 MB', extra:'高配', color:'teal', icon:'sun' },
  { id:'r10', type:'server', name:'云杉谷 · 原版生存', author:'栗子', intro:'原版规则，只加了防作弊与备份。', tags:['1.20.1','原版','白名单'], players:19, max:40, ping:41, addr:'spruce.mclc.cn', color:'green', icon:'server' },
  { id:'r11', type:'material', name:'旧纸 · 复古材质', author:'阿岚', intro:'像旧版本一样粗糙，但很温柔。', tags:['16x','1.18+','复古'], size:'9.4 MB', extra:'16x', color:'amber', icon:'palette' },
  { id:'r12', type:'shader', name:'MakeUp · 柔光', author:'阿澈', intro:'偏暖的柔光，适合日常生存。', tags:['1.20.1','中配','柔光'], size:'5.1 MB', extra:'中配', color:'amber', icon:'sun' },
  { id:'r13', type:'server', name:'星灯 · 空岛', author:'阿岚', intro:'从一棵树开始，慢慢点亮整片空域。', tags:['1.20.1','空岛','休闲'], players:7, max:30, ping:56, addr:'sky.mclc.cn', color:'purple', icon:'sparkles' },
  { id:'r14', type:'material', name:'青岚 · 国风 32x', author:'北岸', intro:'青绿山水与木质界面，克制耐看。', tags:['32x','1.20','国风'], size:'18.7 MB', extra:'32x', color:'teal', icon:'palette' },
  { id:'r15', type:'shader', name:'Rethinking Voxels', author:'纸鸢', intro:'体积云与真实阴影，需要独显。', tags:['1.20.1','高配','体积云'], size:'14.2 MB', extra:'高配', color:'purple', icon:'sun' },
  { id:'r16', type:'server', name:'深岩 · 模组生存', author:'小鹿', intro:'Create 与 Farmer\'s Delight，慢节奏。', tags:['1.20.1','模组','Create'], players:16, max:32, ping:63, addr:'deep.mclc.cn', color:'orange', icon:'layers' },
  { id:'r17', type:'material', name:'极简 · 纯色 UI', author:'阿澈', intro:'去掉多余纹理，只留下清楚的层级。', tags:['16x','1.20','极简'], size:'4.3 MB', extra:'16x', color:'purple', icon:'palette' },
  { id:'r18', type:'shader', name:'Photon · 中配', author:'栗子', intro:'日光与水面很干净，帧数稳定。', tags:['1.20.1','中配','水面'], size:'7.7 MB', extra:'中配', color:'green', icon:'sun' },
];

const quotes = [
  { type:'text', text:'先把今天的矿挖完，再去想明天的事。', author:'阿澈', meta:'暮色森林' },
  { type:'image', text:'周末合影 · 群友服务器', author:'北岸', meta:'红石小镇 · 2026.10.26' },
  { type:'text', text:'服务器可以重开，朋友散了就真的散了。', author:'纸鸢', meta:'红石小镇' },
  { type:'text', text:'我不是在挂机，我是在等小麦长大。', author:'栗子', meta:'云杉谷' },
  { type:'text', text:'欢迎回家，床还在原来的位置。', author:'小鹿', meta:'旧日港湾' },
];

const typeMeta = {
  server: { label:'服务器', icon:'server' },
  material: { label:'材质包', icon:'palette' },
  shader: { label:'光影', icon:'sun' },
};

const tileClass = (c) => `tile-${c || 'green'}`;

const versionGroups = [
  { id:'vanilla', label:'原版游戏', count:5, versions:[
    ['26.3', '26.3 · vanilla'],
    ['26.4-snapshot-6', '26.4-snapshot-6 · vanilla'],
    ['方舟肉鸽专用客户端1.21.1', '1.21.1 · vanilla'],
    ['滑索骑士', '26.2 · vanilla'],
    ['梦乐园', '1.21.4 · vanilla'],
  ]},
  { id:'fabric', label:'FABRIC 版本', count:9, versions:[
    ['1.21.11-Fabric 0.20.1', '1.21.11 · fabric 0.20.1'],
    ['26.2原版优化', '26.2 · fabric 0.19.5'],
    ['26.3-Fabric 0.19.5', '26.3 · fabric 0.19.5'],
  ]},
];

const instances = [
  { name:'1.20.1-OptiFine_I6', loader:'Fabric 1.20.1', mods:42, updates:4 },
  { name:'1.21.11-Fabric 0.20.1', loader:'Fabric 1.21.11', mods:6, updates:1 },
  { name:'26.3', loader:'Vanilla 26.3', mods:3, updates:0 },
  { name:'26.4-snapshot-6', loader:'Vanilla 26.4', mods:2, updates:0 },
  { name:'方舟肉鸽专用客户端1.21.1', loader:'Fabric 1.21.1', mods:9, updates:2 },
  { name:'梦乐园', loader:'Fabric 1.21.4', mods:4, updates:0 },
];

const appVersion = () => window.state?.appVersion || "预览";

function titlebar(active) {
  const navs = [
    ['home', '启动', 'play'],
    ['download', '下载', 'download'],
    ['options', '设置', 'settings'],
  ];
  return `<header class="titlebar">
    <div class="brand">
      ${BrandLogo()}
      <span class="brand-name">MCUG Launcher</span>
      <span class="brand-version">${appVersion()}</span>
    </div>
    <nav class="topnav">
      ${navs.map(([id, label, ic]) => `<a class="nav-item ${active === id ? 'active' : ''}" href="?screen=${id}">${icon(ic)}<span>${label}</span></a>`).join('')}
    </nav>
    <div class="win-ctrl">
      <button class="win-btn" aria-label="最小化"><svg viewBox="0 0 12 12"><path d="M2 6h8"/></svg></button>
      <button class="win-btn" aria-label="最大化"><svg viewBox="0 0 12 12"><rect x="2.5" y="2.5" width="7" height="7"/></svg></button>
      <button class="win-btn close" aria-label="关闭"><svg viewBox="0 0 12 12"><path d="m2.5 2.5 7 7M9.5 2.5l-7 7"/></svg></button>
    </div>
  </header>`;
}

function browseSidebar(active) {
  const item = (id, label, ic, href, badge = '') => `<a class="side-item ${active === id ? 'active' : ''}" href="${href}">${icon(ic)}<span>${label}</span>${badge ? `<span class="side-badge">${badge}</span>` : ''}</a>`;
  return `<aside class="sidebar">
    <div class="side-group">
      <div class="side-label">开始</div>
      ${item('home', '主页', 'home', '?screen=home')}
    </div>
    <div class="side-group">
      <div class="side-label">社区</div>
      ${item('community', '群友推荐', 'users', '?screen=download&type=server', '新')}
    </div>
    <div class="side-group">
      <div class="side-label">下载</div>
      ${item('vanilla', '原版游戏', 'grid', '?screen=download')}
      ${item('mod', '模组', 'box', '?screen=download')}
      ${item('pack', '整合包', 'layers', '?screen=download')}
      ${item('resource', '资源包', 'image', '?screen=download')}
      ${item('shader', '光影', 'sun', '?screen=download')}
      ${item('datapack', '数据包', 'database', '?screen=download')}
    </div>
    <div class="side-foot"><span class="live-dot"></span><span>社区内容</span></div>
  </aside>`;
}

function settingsSidebar(active) {
  const item = (id, label, ic, href) => `<a class="side-item ${active === id ? 'active' : ''}" href="${href}">${icon(ic)}<span>${label}</span></a>`;
  const page = (id, label, ic) => item(id, label, ic, `?screen=settings&panel=${id}`);
  return `<aside class="sidebar settings-sidebar">
    <div class="side-group">
      <div class="side-label">启动与环境</div>
      ${page('launch', '启动设置', 'play')}
      ${page('java', 'Java 管理', 'terminal')}
      ${page('install', '安装与导入', 'box')}
      ${page('network', '下载与网络', 'download')}
    </div>
    <div class="side-group">
      <div class="side-label">个性化</div>
      ${page('appearance', '界面与背景', 'image')}
    </div>
    <div class="side-group">
      <div class="side-label">社区</div>
      ${item('options', '更多选项', 'users', '?screen=options')}
      ${item('admin', '管理员', 'shield', '?screen=admin')}
      ${item('datasource', '数据源', 'database', '?screen=datasource')}
    </div>
    <div class="side-group">
      <div class="side-label">游戏工具</div>
      ${page('servers', '服务器收藏', 'server')}
      ${page('games', '运行中的游戏', 'play')}
      ${page('bookmarks', '我的收藏', 'heart')}
      ${page('saves', '存档保护', 'box')}
    </div>
    <div class="side-group">
      <div class="side-label">任务与诊断</div>
      ${page('tasks', '下载任务', 'download')}
      ${page('recovery', '任务与恢复', 'refresh')}
      ${page('logs', '运行日志', 'terminal')}
      ${page('diagnose', '诊断与反馈', 'alert')}
    </div>
    <div class="side-group">
      <div class="side-label">关于</div>
      ${page('about', '关于与许可', 'info')}
    </div>
    <div class="side-foot"><span class="live-dot"></span><span>社区内容</span></div>
  </aside>`;
}

function downloadSidebar(active = 'community') {
  const item = (id, label, ic, href, badge = '') => `<a class="side-item ${active === id ? 'active' : ''}" href="${href}">${icon(ic)}<span>${label}</span>${badge ? `<span class="side-badge">${badge}</span>` : ''}</a>`;
  return `<aside class="sidebar">
    <div class="side-group">
      <div class="side-label">社区</div>
      ${item('community', '群友推荐', 'users', '?screen=download&type=server', '新')}
    </div>
    <div class="side-group">
      <div class="side-label">下载</div>
      ${item('vanilla', '原版游戏', 'grid', '?screen=catalog&type=vanilla')}
      ${item('mod', '模组', 'box', '?screen=catalog&type=mod')}
      ${item('modpack', '整合包', 'layers', '?screen=catalog&type=modpack')}
      ${item('resourcepack', '资源包', 'image', '?screen=catalog&type=resourcepack')}
      ${item('shader', '光影', 'sun', '?screen=catalog&type=shader')}
      ${item('datapack', '数据包', 'database', '?screen=catalog&type=datapack')}
      ${item('install', '安装与导入', 'box', '?screen=settings&panel=install')}
    </div>
    <div class="side-foot"><span class="live-dot"></span><span>社区内容</span></div>
  </aside>`;
}

function pageHead(title, sub, right = '') {
  return `<div class="page-head">
    <div><div class="page-title">${title}</div><div class="page-sub">${sub}</div></div>
    <div class="page-note">${right}</div>
  </div>`;
}

/* ---------------- home ---------------- */
function accountCard() {
  return `<section class="card account-card">
    <div class="acc-tabs" role="tablist" aria-label="账号类型">
      <button class="acc-tab active" data-account="microsoft">正版</button>
      <button class="acc-tab" data-account="offline">离线</button>
      <button class="acc-tab" data-account="mua">MUA 登录</button>
    </div>
    <a class="acc-avatar-wrap" href="?screen=home&modal=skin" title="皮肤与披风" aria-label="打开皮肤与披风">
      <div class="acc-avatar">${Avatar()}</div>
      <span class="avatar-overlay">${icon('shirt')} 皮肤与披风</span>
    </a>
    <div class="acc-name">waibibabu1998</div>
    <div class="acc-meta">${icon('shield')} 正版账号 · 启动时验证</div>
    <button class="btn btn-soft acc-btn">切换微软账号</button>
    <a class="acc-link" href="?screen=options">账号设置</a>
    <div class="acc-foot"><span>账号保存在本机</span><strong>已保存</strong></div>
  </section>`;
}

function launchPanel(expanded = false) {
  return `<div class="launch-wrap">
    <div class="launch-row">
      <button class="launch-btn" aria-label="启动 1.20.1-OptiFine_I6">
        <span class="launch-glyph">${icon('play')}</span>
        <span class="launch-text"><b>启动游戏</b><small>1.20.1-OptiFine_I6 · 4096 MB</small></span>
      </button>
      <div class="launch-side">
        <a class="launch-mini ${expanded ? 'open' : ''}" href="${expanded ? '?screen=home' : '?screen=home&panel=versions'}" aria-label="${expanded ? '收起实例列表' : '选择实例'}" title="${expanded ? '收起实例列表' : '选择实例'}">${icon(expanded ? 'chevron-left' : 'chevron-right')}</a>
        <a class="launch-mini" href="?screen=manage" aria-label="版本内容管理" title="版本内容管理">${icon('settings')}</a>
      </div>
    </div>
  </div>`;
}

function instancePanel() {
  const groups = versionGroups.map((g, gi) => `<section class="version-group">
    <div class="version-group-head"><span class="version-caret">${icon('chevron-down')}</span><b>${g.label}</b><span class="version-count">(${g.count})</span></div>
    <div class="version-list">${g.versions.map((v, i) => `<div class="version-row ${v[0] === Q.inst ? 'selected' : ''}">
      <div class="version-copy"><b>${v[0]}</b><span>${v[1]}</span></div>
      <button class="version-icon" aria-label="启动 ${v[0]}">${icon('play')}</button>
      <a class="version-icon" href="?screen=version&v=${encodeURIComponent(v[0])}" aria-label="版本设置 ${v[0]}">${icon('settings')}</a>
    </div>`).join('')}</div>
  </section>`).join('');
  return `<section class="card version-panel">
    <div class="version-head">
      <div><div class="version-title">游戏目录</div><div class="version-sub">选择实例后可直接启动，或进入版本设置。</div></div>
      <div class="version-head-actions"><button class="btn btn-ghost btn-sm">${icon('download')} 导入整合包</button><button class="btn btn-ghost btn-sm">${icon('refresh')} 刷新版本</button><a class="version-close" href="?screen=home" aria-label="收起游戏目录">${icon('chevron-left')} 收起</a></div>
    </div>
    <div class="version-path-row"><div class="version-path">${icon('folder')} C:\\Users\\TianXuanL\\Desktop\\神奇小玩意\\MCCCC\\.minecraft <span class="version-path-caret">${icon('chevron-down')}</span></div><button class="version-add" aria-label="添加游戏目录">${icon('plus')}</button></div>
    <div class="version-search">${icon('search')}<input placeholder="点此处搜索实例"><span class="search-hint">按名称、版本或加载器筛选</span></div>
    <div class="version-groups">${groups}</div>
  </section>`;
}

function serverTile(s) {
  const pingClass = !s.online ? 'bad' : s.ping < 45 ? 'good' : 'mid';
  return `<article data-server-id="${s.id}" class="server-tile ${s.online ? '' : 'offline'}">
    <div class="server-top">
      <div class="server-icon ${tileClass(s.color)}">${icon(s.icon)}</div>
      <div class="flex1">
        <div class="server-name">${s.name}</div>
        <div class="server-sub"><span class="dot ${s.online ? 'online' : 'offline'}"></span>${s.online ? '在线' : '离线'} · ${s.version}</div>
      </div>
    </div>
    <button class="server-more" data-server-menu="${s.id}" aria-label="服务器操作">${icon('more')}</button>
    <div class="server-motd">${s.motd}</div>
    <div class="server-bottom">
      <span class="server-stat">${icon('users')}${s.online ? `<b>${s.players}</b> / ${s.max}` : '<span class="offline-label">上次在线人数未知</span>'}</span>
      <span class="server-ping ${pingClass}">${s.online ? `${s.ping} ms` : '离线'}</span>
    </div>
    ${Q.menu === s.id ? `<div class="mini-menu">
      <a href="?screen=home&modal=edit-server&sid=${encodeURIComponent(s.id)}">${icon('edit')} 编辑服务器</a>
      <button class="danger" data-delete-server="${s.id}">${icon('trash')} 移除</button>
    </div>` : ''}
  </article>`;
}

function serverAddTile() {
  return `<a class="server-add" href="?screen=home&modal=add-server">
    <span class="add-circle">${icon('plus')}</span>
    <span>添加服务器</span>
  </a>`;
}

function serverPanel(mode = 'normal') {
  const onlineCount = servers.filter(s => s.online).length;
  const status = mode === 'loading' ? '读取中…' : mode === 'error' ? '状态不可用' : mode === 'empty' || servers.length === 0 ? '0 台服务器' : `${onlineCount} 在线 · ${servers.length - onlineCount} 离线`;
  const head = `<div class="card-head">
    <span class="card-title">${icon('server')} 正在开设的服务器</span>
    <span class="server-status">${status}</span>
    <div class="rail-tools"><button class="rail-arrow" data-rail-prev aria-label="上一台">${icon('chevron-left')}</button><button class="rail-arrow" data-rail-next aria-label="下一台">${icon('chevron-right')}</button></div>
    <a class="card-action" href="?screen=home&modal=add-server">${icon('plus')} 添加服务器</a>
  </div>`;
  if (mode === 'loading') {
    return `<section class="card server-panel">${head}<div class="server-rail">
      ${[1,2,3,4].map(() => `<div class="server-tile"><div class="server-top"><div class="sk" style="width:33px;height:33px;border-radius:8px"></div><div class="flex1"><div class="sk sk-line tall" style="width:72%"></div><div class="sk sk-line" style="width:44%;margin-top:7px"></div></div></div><div class="sk sk-line" style="width:92%;margin-top:14px"></div><div class="sk sk-line" style="width:70%;margin-top:6px"></div><div class="sk sk-line" style="width:48%;margin-top:22px"></div></div>`).join('')}
    </div></section>`;
  }
  if (mode === 'empty') {
    return `<section class="card server-panel">${head}<div class="server-empty">
      <div class="mini-empty-icon">${icon('server')}</div>
      <div><b>还没有添加服务器</b><span>填入 host:port，就能在这里看到实时人数与延迟。</span></div>
      <a class="btn btn-soft btn-sm" href="?screen=home&modal=add-server">${icon('plus')} 添加</a>
    </div></section>`;
  }
  if (mode === 'error') {
    return `<section class="card server-panel">${head}<div class="mini-error">
      ${icon('alert')}
      <div><b>服务器状态加载失败</b><span>请检查网络，或稍后重试。</span></div>
      <button class="btn btn-ghost btn-xs">${icon('refresh')} 重试</button>
    </div></section>`;
  }
  if (servers.length === 0) {
    return `<section class="card server-panel">${head}<div class="server-empty">
      <div class="mini-empty-icon">${icon('server')}</div>
      <div><b>还没有添加服务器</b><span>填入 host:port，就能在这里看到实时人数与延迟。</span></div>
      <a class="btn btn-soft btn-sm" href="?screen=home&modal=add-server">${icon('plus')} 添加</a>
    </div></section>`;
  }
  return `<section class="card server-panel">${head}<div class="server-rail">${servers.map(serverTile).join('')}</div></section>`;
}

function annImage(a) {
  if (!a.image) return '';
  return a.image.startsWith('data:') || /^https?:/.test(a.image)
    ? `<img src="${a.image}" alt="公告配图">`
    : QuotePhoto();
}

function annPanel(mode = 'normal') {
  const head = `<div class="card-head"><span class="card-title">${icon('bell')} 群公告</span><a class="card-action" href="?screen=home&modal=announcements">查看全部 ${icon('chevron-right')}</a></div>`;
  if (mode === 'loading') {
    return `<section class="card ann-panel">${head}<div class="ann-body"><div class="sk" style="width:36px;height:38px;border-radius:7px"></div><div class="flex1"><div class="sk sk-line tall" style="width:64%"></div><div class="sk sk-line" style="width:88%;margin-top:7px"></div></div></div></section>`;
  }
  if (mode === 'empty') {
    return `<section class="card ann-panel">${head}<div class="ann-body"><div class="ann-empty">${icon('bell')}<span>暂无公告，新的消息会出现在这里。</span></div></div></section>`;
  }
  if (mode === 'error') {
    return `<section class="card ann-panel">${head}<div class="ann-body"><div class="mini-error compact">${icon('alert')}<div><b>公告加载失败</b><span>数据源可能暂时不可用。</span></div><button class="btn-link">重试</button></div></div></section>`;
  }
  if (!announcements.length) return annPanel("empty");
  const a = announcements[Q.ann % announcements.length];
  const month = a.date.split('/')[0];
  const day = a.date.split('/')[1];
  return `<section class="card ann-panel">${head}<div class="ann-body">
    <div class="ann-date"><b>${day}</b><span>${month} 月</span></div>
    ${a.image ? `<div class="ann-thumb">${annImage(a)}</div>` : ''}
    <div class="ann-copy"><div class="ann-title"><span class="tag green">${a.tag}</span>${a.title}</div><div class="ann-text">${a.text}</div></div>
    <div class="ann-pager"><button class="ann-arrow" data-ann-prev aria-label="上一条">${icon('chevron-left')}</button><span class="pager-count">${Q.ann + 1} / 3</span><button class="ann-arrow" data-ann-next aria-label="下一条">${icon('chevron-right')}</button></div>
  </div></section>`;
}

function recRow(r) {
  return `<a class="rec-row" href="?screen=download&type=${r.type}">
    <span class="rec-icon ${tileClass(r.color)}">${icon(r.icon)}</span>
    <span class="rec-copy"><span class="rec-name">${r.name}</span><span class="rec-meta"><span class="rec-type">${typeMeta[r.type].label}</span>由 ${r.author} 推荐</span></span>
    <span class="rec-arrow">${icon('chevron-right')}</span>
  </a>`;
}

function recPanel(mode = 'normal') {
  const head = `<div class="card-head"><span class="card-title">${icon('sparkles')} 群友推荐</span><button class="card-action link-action" data-recs-manage>管理</button><a class="card-action" href="?screen=download&type=server">查看更多 ${icon('chevron-right')}</a></div>`;
  if (mode === 'loading') {
    return `<section class="card rec-panel">${head}<div class="rec-list">${[1,2,3].map(() => `<div class="rec-row"><div class="sk" style="width:28px;height:28px;border-radius:7px"></div><div class="flex1"><div class="sk sk-line" style="width:60%"></div><div class="sk sk-line" style="width:40%;margin-top:6px"></div></div></div>`).join('')}</div></section>`;
  }
  if (mode === 'empty') {
    return `<section class="card rec-panel">${head}<div class="rec-empty">${icon('sparkles')}<div><b>暂无群友推荐</b><span>等群友整理好，第一时间放上来。</span></div></div></section>`;
  }
  if (mode === 'error') {
    return `<section class="card rec-panel">${head}<div class="mini-error compact">${icon('alert')}<div><b>推荐加载失败</b><span>请检查推荐数据源。</span></div><button class="btn-link">重试</button></div></section>`;
  }
  return `<section class="card rec-panel">${head}<div class="rec-list">${recs.slice(0,3).map(recRow).join('')}</div></section>`;
}

function quoteStrip(mode = 'normal') {
  if (mode === 'loading') {
    return `<section class="card quote-strip"><div class="sk" style="width:52px;height:36px"></div><div class="flex1"><div class="sk sk-line tall" style="width:62%"></div><div class="sk sk-line" style="width:28%;margin-top:9px"></div></div></section>`;
  }
  if (mode === 'empty') {
    return `<section class="card quote-strip"><div class="quote-label"><div class="qmark">“</div><b>群友语录</b></div><div class="quote-main"><div class="quote-fallback">${icon('message')}<span>还没有语录。等第一条来自群友的话。</span></div></div></section>`;
  }
  if (mode === 'error') {
    return `<section class="card quote-strip"><div class="quote-label"><div class="qmark">“</div><b>群友语录</b></div><div class="quote-main"><div class="quote-fallback">${icon('alert')}<span>语录加载失败。</span></div></div><div class="quote-side"><button class="btn-link">${icon('refresh')} 重试</button></div></section>`;
  }
  const q = quotes[Q.qi % Math.max(1, quotes.length)] || {type:"text",text:"暂无群友语录",author:"",meta:""};
  const isImage = q.type === 'image';
  const showFallback = Q.quote === 'fallback' && isImage;
  const dots = `<div class="quote-dots">${quotes.map((_, i) => `<span class="qdot ${i === Q.qi ? 'on' : ''}"></span>`).join('')}</div>`;
  if (showFallback) {
    return `<section class="card quote-strip">
      <div class="quote-label"><div class="qmark">“</div><b>群友语录</b></div>
      <div class="quote-main quote-anim"><div class="quote-photo is-broken">${icon('image-off')}</div><div class="quote-photo-copy"><div class="quote-photo-title">图片暂时无法加载</div><div class="quote-photo-meta">已自动回退为文字：${q.text}</div><div class="quote-hint" style="margin-top:6px">${icon('refresh')} 点击换一条，或稍后重试</div></div></div>
      <div class="quote-side">${dots}<span class="quote-hint">图片模式</span></div>
    </section>`;
  }
  if (isImage) {
    return `<section class="card quote-strip" title="点击查看下一条">
      <div class="quote-label"><div class="qmark">“</div><b>群友语录</b></div>
      <div class="quote-main quote-anim"><div class="quote-photo">${QuotePhoto()}</div><div class="quote-photo-copy"><div class="quote-photo-title">${q.text}</div><div class="quote-photo-meta">来自 ${q.author} · ${q.meta}</div><div class="quote-hint" style="margin-top:6px">${icon('image')} 点击空白处查看下一条</div></div></div>
      <div class="quote-side">${dots}<span class="quote-hint">${icon('refresh')} 点击切换</span></div>
    </section>`;
  }
  return `<section class="card quote-strip" title="点击查看下一条">
    <div class="quote-label"><div class="qmark">“</div><b>群友语录</b></div>
    <div class="quote-main quote-anim"><div class="quote-text">${q.text}<span class="author">—— ${q.author} · ${q.meta}</span></div></div>
    <div class="quote-side">${dots}<span class="quote-hint">${icon('refresh')} 点击切换</span></div>
  </section>`;
}

function homePage() {
  const m = Q.state;
  const modes = m === 'loading' || m === 'empty' || m === 'error' ? m : 'normal';
  const versions = Q.panel === 'versions';
  if (versions) {
    return shell('home', '', `<div class="home-main version-open">
      <div class="home-left">${accountCard()}${launchPanel(true)}</div>
      <div class="home-right version-right">${instancePanel()}</div>
    </div>`);
  }
  return shell('home', '', `<div class="home-main">
      <div class="home-left">${accountCard()}${launchPanel()}</div>
      <div class="home-right">${serverPanel(modes)}${annPanel(modes)}${recPanel(modes)}</div>
    </div>
    ${quoteStrip(modes)}`);
}

/* ---------------- download ---------------- */
function recCard(r) {
  const isServer = r.type === 'server';
  const meta = isServer
    ? `<span>${icon('users')} ${r.online === false ? '离线' : `${r.players} / ${r.max}`}</span><span class="sep"></span><span class="${r.online === false ? 'muted' : 'green-text'}">${r.online === false ? '—' : `${r.ping} ms`}</span>`
    : `<span>${r.size}</span><span class="sep"></span><span>${r.extra}</span><span class="sep"></span><span>1.20.1</span>`;
  const foot = isServer
    ? `<span class="addr">${r.addr}</span><button class="btn btn-soft ${r.online === false ? 'off' : ''}">${icon('arrow-up-right')} 查看</button>`
    : `<span class="inline muted" style="font-size:9px">${icon('shield')} 已校验</span><button class="btn btn-soft">${icon('download')} 前往下载</button>`;
  return `<article class="card rec-card">
    <div class="r-card-top">
      <div class="r-card-icon ${tileClass(r.color)}">${icon(r.icon)}</div>
      <div class="r-card-heading">
        <div class="r-card-title">${r.name}</div>
        <div class="r-card-by">由 <b>${r.author}</b> 推荐 · ${typeMeta[r.type].label}</div>
      </div>
    </div>
    <div class="r-card-tags">${r.tags.map((t, i) => `<span class="tag ${i === 0 ? 'green' : ''}">${t}</span>`).join('')}</div>
    <div class="r-card-desc">${r.intro}</div>
    <div class="r-card-meta">${meta}</div>
    <div class="r-card-foot">${foot}</div>
  </article>`;
}

function downloadPage() {
  const type = ['server', 'material', 'shader'].includes(Q.type) ? Q.type : 'server';
  const list = recs.filter(r => r.type === type);
  const counts = { server: recs.filter(r => r.type === 'server').length, material: recs.filter(r => r.type === 'material').length, shader: recs.filter(r => r.type === 'shader').length };
  if (Q.state === 'empty') counts[type] = 0;
  const head = `<div class="dl-head">
    <div class="dl-title-wrap">
      <div class="dl-big-icon">${icon('users')}</div>
      <div><div class="page-title">群友推荐</div><div class="page-sub">群友亲自挑过的服务器、材质包与光影。</div></div>
    </div>
    <div class="dl-head-side"><span class="dl-count">由社区维护 · 5 分钟前更新</span><button class="btn btn-ghost btn-sm">${icon('refresh')} 刷新</button></div>
  </div>`;
  const seg = `<div class="segbar">
    <div class="segmented" role="tablist" aria-label="推荐类型">
      ${['server','material','shader'].map(t => `<a class="seg-btn ${type === t ? 'active' : ''}" href="?screen=download&type=${t}">${icon(typeMeta[t].icon)}${typeMeta[t].label}<span class="count">${counts[t]}</span></a>`).join('')}
    </div>
    <span class="muted" style="font-size:9.5px">点击卡片可在分类间切换 · 数据与主页同源</span>
  </div>`;
  let body = '';
  if (Q.state === 'loading') {
    body = `<div class="state-skeleton-grid">${[1,2,3,4,5,6].map(() => `<div class="card state-skeleton-card"><div class="top"><div class="sk" style="width:48px;height:48px;border-radius:10px"></div><div class="flex1"><div class="sk sk-line tall" style="width:72%"></div><div class="sk sk-line" style="width:45%;margin-top:8px"></div></div></div><div class="sk sk-line" style="width:100%;margin-top:8px"></div><div class="sk sk-line" style="width:82%;margin-top:6px"></div><div class="sk sk-line" style="width:100%;margin-top:18px"></div><div class="sk sk-line" style="width:64%;margin-top:20px"></div></div>`).join('')}</div>`;
  } else if (Q.state === 'empty') {
    body = `<div class="state-page" style="height:588px"><div class="state-box">
      <div class="state-icon">${icon('sparkles')}</div>
      <div class="state-title">暂无群友推荐</div>
      <div class="state-copy">这一分类还没有内容。群友正在整理，稍后再来看看，或者去其他分类逛逛。</div>
      <div class="state-actions"><button class="btn btn-soft">${icon('refresh')} 刷新</button><a class="btn btn-ghost" href="?screen=download&type=server">看看服务器</a></div>
    </div></div>`;
  } else if (Q.state === 'error') {
    body = `<div class="state-page" style="height:588px"><div class="state-box">
      <div class="state-icon error">${icon('alert')}</div>
      <div class="state-title">推荐加载失败</div>
      <div class="state-copy">无法读取推荐数据源（超时或返回格式不正确）。你的游戏与下载不受影响，可以重试或切换数据源。</div>
      <div class="state-actions"><button class="btn btn-primary">${icon('refresh')} 重试</button><a class="btn btn-ghost" href="?screen=datasource">${icon('database')} 打开数据源设置</a></div>
    </div></div>`;
  } else {
    body = `<div class="dl-grid">${list.map(recCard).join('')}</div>
      <div class="dl-footnote"><span class="inline">${icon('info')} 推荐内容由群友维护，启动器只负责展示与跳转。</span><span>共 ${list.length} 条 · 按最近更新排序</span></div>`;
  }
  return shell('download', downloadSidebar('community'), `${head}${seg}${body}`);
}

const mods = [  { id:'sodium', name:'Sodium', author:'JellySquid', desc:'高性能渲染优化，大幅提升帧率并降低内存占用。', tags:['客户端','优化','Fabric'], size:'1.2 MB', downloads:'12.5M', v:'0.5.8', color:'green', ic:'zap' },
  { id:'iris', name:'Iris Shaders', author:'IrisShaders', desc:'支持光影的渲染模组，与 Sodium 完美配合。', tags:['客户端','光影','Fabric'], size:'8.3 MB', downloads:'8.3M', v:'1.7.0', color:'purple', ic:'image' },
  { id:'lithium', name:'Lithium', author:'JellySquid', desc:'优化游戏性能，减少卡顿，兼容大多数整合包。', tags:['客户端','优化','Fabric'], size:'1.8 MB', downloads:'6.1M', v:'0.11.2', color:'violet', ic:'layers' },
  { id:'jei', name:'JEI', author:'mezz', desc:'查看物品合成配方与用途，模组整合包必备。', tags:['客户端','实用','Fabric'], size:'18.9 MB', downloads:'18.9M', v:'15.2.0.27', color:'amber', ic:'grid' },
  { id:'fabric-api', name:'Fabric API', author:'FabricMC', desc:'Fabric 模组的前置核心库，绝大多数模组需要。', tags:['库文件','前置','Fabric'], size:'2.3 MB', downloads:'25.6M', v:'0.92.2', color:'blue', ic:'box' },
  { id:'mod-menu', name:'Mod Menu', author:'Prospector', desc:'在游戏内查看和管理模组，支持快速配置。', tags:['客户端','管理','Fabric'], size:'1.1 MB', downloads:'10.2M', v:'7.2.2', color:'slate', ic:'terminal' }
];

window.mcugData = { announcements, recs, servers, quotes, mods };
window.state = {
  servers, announcements, recs, quotes, mods,
  root: '', java: '', ram: 4096, width: 1280, height: 720,
  dataSources: { announcements: '', servers: '', recommendations: '', quotes: '' },
  javaList: [], logText: '', diagText: '',
  catalog: {}, catalogLoading: {}, vanilla: []
};

function modCard(m) {
  return `<a class="mod-card" href="?screen=mod&id=${m.id}">
    <div class="mod-card-top"><span class="mod-icon ${m.color}">${icon(m.ic)}</span><div class="mod-card-id"><div class="mod-card-name">${m.name}</div><div class="mod-card-author">${m.author}</div></div><span class="mod-card-arrow">${icon('chevron-right')}</span></div>
    <div class="mod-card-desc">${m.desc}</div>
    <div class="mod-card-tags">${m.tags.map((t, i) => `<span class="tag ${i === 0 ? 'green' : ''}">${t}</span>`).join('')}</div>
    <div class="mod-card-foot"><span>${icon('download')} ${m.downloads}</span><span>${m.size}</span><span class="mod-open">查看</span></div>
  </a>`;
}

function modListPage() {
  const head = `<div class="dl-head">
    <div class="dl-title-wrap"><div class="dl-big-icon mod">${icon('box')}</div><div><div class="page-title">模组</div><div class="page-sub">安装、更新和管理这个实例的模组。</div></div></div>
    <div class="dl-head-side"><span class="inline">实例 1.20.1-OptiFine_I6</span><button class="btn btn-primary btn-sm">${icon('plus')} 添加模组</button></div>
  </div>`;
  const search = `<div class="mod-search-row">
    <div class="mod-filters"><span class="filter-chip active">全部</span><span class="filter-chip">已安装</span><span class="filter-chip">可更新</span><span class="filter-chip">客户端</span><span class="filter-chip">服务端</span></div>
    <label class="mod-search">${icon('search')}<input placeholder="搜索模组名称、作者或描述"><span class="search-hint">支持中文与英文</span></label>
  </div>`;
  return shell('download', downloadSidebar('mod'), `${head}${search}
    <div class="mod-grid">${mods.map(modCard).join('')}</div>
    <div class="dl-footnote"><span class="inline">${icon('info')} 点击卡片查看详情；下载前会检查加载器与游戏版本。</span><span>共 6 个结果 · 按相关度排序</span></div>`);
}

function catalogPage(type = 'mod', query = '') {
  const meta = {
    vanilla: { title: '原版游戏', sub: '安装 Mojang 官方正式版，不修改现有实例。', icon: 'grid', code: 'grid' },
    mod: { title: '模组', sub: '从 Modrinth 搜索并下载到当前实例。', icon: 'box', code: 'mod' },
    modpack: { title: '整合包', sub: '下载整合包后，在安装与导入页面选择文件。', icon: 'layers', code: 'layers' },
    resourcepack: { title: '资源包', sub: '下载后放入当前实例的 resourcepacks。', icon: 'image', code: 'image' },
    shader: { title: '光影', sub: '下载后放入当前实例的 shaderpacks。', icon: 'sun', code: 'sun' },
    datapack: { title: '数据包', sub: '保存到下载目录，随后导入具体存档的 datapacks 文件夹。', icon: 'database', code: 'database' }
  }[type] || { title: '下载', sub: '浏览可下载内容。', icon: 'download', code: 'download' };

  if (type === 'vanilla') {
    const items = state.vanilla || [];
    const body = !items.length
      ? `<div class="catalog-empty">${icon('loader')}<b>正在读取原版版本列表</b><span>需要访问 Mojang 版本清单；失败时可稍后重试。</span></div>`
      : `<div class="vanilla-grid">${items.map((v) => `<article class="vanilla-row"><div class="vanilla-id"><b>${v.id}</b><span>${v.released}</span></div><span class="tag">正式版</span><button class="btn btn-primary btn-sm" data-install-vanilla="${v.id}">${icon('download')} 安装</button></article>`).join('')}</div>`;
    return shell('download', downloadSidebar('vanilla'), `
      <div class="catalog-page">
        <div class="dl-head"><div class="dl-title-wrap"><div class="dl-big-icon mod">${icon('grid')}</div><div><div class="page-title">${meta.title}</div><div class="page-sub">${meta.sub}</div></div></div><button class="btn btn-ghost btn-sm" data-reload-vanilla>${icon('refresh')} 刷新列表</button></div>
        ${body}
      </div>`);
  }

  const cached = state.catalog[type];
  const loading = state.catalogLoading[type];
  const items = cached || [];
  let body;
  if (loading || cached === undefined) {
    body = `<div class="catalog-grid">${[1,2,3,4,5,6].map(() => `<div class="content-card skeleton"><div class="sk sk-line" style="width:44%;height:18px"></div><div class="sk sk-line" style="width:88%;margin-top:18px"></div><div class="sk sk-line" style="width:70%;margin-top:8px"></div><div class="sk sk-line" style="width:52%;margin-top:32px"></div></div>`).join('')}</div>`;
  } else if (!items.length) {
    body = `<div class="catalog-empty">${icon('search')}<b>没有找到内容</b><span>换一个关键词，或确认当前实例的版本与加载器。</span></div>`;
  } else {
    body = `<div class="catalog-grid">${items.map((item) => `<article class="content-card" data-content-id="${item.id}" data-content-kind="${type}">
      <div class="content-top">${item.icon ? `<img class="content-icon" src="${item.icon}" alt="">` : `<span class="content-icon fallback">${(item.name || 'M').slice(0, 1)}</span>`}<div class="content-id"><b>${item.name}</b><span>${item.author || 'Modrinth'}</span></div></div>
      <div class="content-desc">${item.description || ''}</div>
      <div class="content-tags">${(item.tags || '').split(' · ').filter(Boolean).slice(0, 3).map((t) => `<span class="tag">${t}</span>`).join('')}</div>
      <div class="content-foot"><span>${icon('download')} ${Number(item.downloads || 0).toLocaleString()}</span><button class="btn btn-soft btn-sm" data-content-install="${item.id}" data-content-category="${type}">${type === 'modpack' ? '下载整合包' : '下载'}</button></div>
    </article>`).join('')}</div>`;
  }

  return shell('download', downloadSidebar(type), `
    <div class="catalog-page">
      <div class="dl-head"><div class="dl-title-wrap"><div class="dl-big-icon mod">${icon(meta.icon)}</div><div><div class="page-title">${meta.title}</div><div class="page-sub">${meta.sub}</div></div></div><span class="inline">${state.selectedInstance || '当前实例'} · ${state.selectedVersion || ''}</span></div>
      <div class="catalog-search-row"><div class="catalog-search">${icon('search')}<input id="catalog-query" placeholder="搜索${meta.title}名称、作者或描述" value="${query}"><button class="btn btn-primary btn-sm" data-content-search="${type}">搜索</button></div></div>
      ${body}
      <div class="dl-footnote">${icon('info')} 内容来自 Modrinth；下载前会检查当前实例的游戏版本与加载器。整合包仅下载 .mrpack 文件。</div>
    </div>`);
}

function modDetailPage() {
  if (window.chrome?.webview) {
    const m = mods.find(x => x.id === Q.id) || mods[0];
    const safe = value => String(value ?? '').replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
    return shell('download',downloadSidebar('mod'),`<a class="back-link" href="?screen=mods">返回模组列表</a><section class="card" style="padding:24px;margin-top:16px"><h2>${safe(m?.name || '未选择本地模组')}</h2><p>${safe(m?.desc || '请选择本地模组，或到下载分类搜索资源。')}</p><p>${safe(m?.v || '')}</p><button class="btn btn-ghost" data-open-folder="mods">打开模组目录</button><a class="btn btn-primary" href="?screen=catalog&type=mod">搜索资源</a></section>`);
  }
  const m = mods.find(x => x.id === Q.id) || mods[0];
  const local = Q.mode === 'local';
  const shots = [0,1,2].map(i => `<div class="shot ${i === 0 ? 'on' : ''}" style="filter:hue-rotate(${i * 18}deg)">${QuotePhoto()}</div>`).join('');
  return shell('download', downloadSidebar('mod'), `
    <div class="detail-back"><a class="back-link" href="?screen=mods">${icon('chevron-left')} 返回模组列表</a><span class="inline">1.20.1-OptiFine_I6 · Fabric</span></div>
    <div class="mod-detail">
      <div class="mod-detail-main">
        <div class="mod-hero">
          <span class="mod-icon big ${m.color}">${icon(m.ic)}</span>
          <div class="mod-hero-copy"><div class="mod-hero-title">${m.name} <span class="mod-verified">${icon('check')} 已校验</span></div><div class="mod-hero-by">作者：${m.author}</div><div class="mod-card-tags">${m.tags.map((t, i) => `<span class="tag ${i === 0 ? 'green' : ''}">${t}</span>`).join('')}</div></div>
          <div class="mod-hero-stats"><span>${icon('download')} ${m.downloads}</span><span>${icon('clock')} 2026-10-28</span></div>
        </div>
        <div class="detail-tabs"><span class="detail-tab active">介绍</span><span class="detail-tab">版本</span><span class="detail-tab">依赖</span></div>
        <div class="shot-strip">${shots}</div>
        <div class="detail-copy"><b>关于 ${m.name}</b><p>${m.desc} 适用于 Minecraft 1.20.1 与 Fabric 加载器，安装后无需额外配置。</p><p>此页面展示的版本、依赖与下载地址由模组源提供；启动器只负责安装到目标实例。</p></div>
        <div class="detail-log"><div class="detail-log-title">更新记录</div>
          <div class="log-row"><b>${m.v}</b><span>适配 Minecraft 1.20.1，修复与部分光影的渲染冲突。</span><em>2026-10-28</em></div>
          <div class="log-row"><b>${m.v.replace(/\d+$/, n => Math.max(0, Number(n) - 1))}</b><span>优化启动时的资源加载，减少首次进入世界的卡顿。</span><em>2026-09-14</em></div>
        </div>
      </div>
      <aside class="mod-install card">
        <div class="mod-install-title">${local ? '下载选项' : '安装选项'} <span class="install-either">二选一</span></div>
        <div class="install-mode">
          <a class="${local ? '' : 'active'}" href="?screen=mod&id=${m.id}">安装到实例</a>
          <a class="${local ? 'active' : ''}" href="?screen=mod&id=${m.id}&mode=local">自定义位置</a>
        </div>
        ${local ? `
          <label class="field-label">模组版本</label>
          <div class="select-like">${m.v} + mc1.20.1 ${icon('chevron-down')}</div>
          <label class="field-label">保存位置</label>
          <div class="path-pick"><label class="path-field">${icon('folder')}<input value="D:\\MCUG\\Downloads\\mods" aria-label="下载目录"></label><button class="btn btn-ghost btn-sm">选择文件夹</button></div>
          <div class="local-summary">${icon('download')}<div><b>${m.id}-fabric-${m.v}.jar</b><span>${m.size} · 下载后不自动安装</span></div></div>
          <button class="btn btn-primary mod-install-btn">${icon('download')} 下载到本地</button>
          <div class="mod-install-note">只下载 .jar 文件，不检查前置依赖，也不写入实例；下载后需手动放入 mods 文件夹。</div>
        ` : `
          <label class="field-label">目标实例</label>
          <div class="select-like">1.20.1-OptiFine_I6 ${icon('chevron-down')}</div>
          <label class="field-label">模组版本</label>
          <div class="select-like">${m.v} + mc1.20.1 ${icon('chevron-down')}</div>
          <div class="dep-title">前置依赖</div>
          <div class="dep-row"><span class="dep-check">${icon('check')}</span><span>Fabric API</span><span class="dep-ver">0.92.2+1.20.1</span></div>
          <div class="dep-row"><span class="dep-check">${icon('check')}</span><span>Fabric Loader</span><span class="dep-ver">0.15.11</span></div>
          <button class="btn btn-primary mod-install-btn">${icon('download')} 下载并安装</button>
          <div class="mod-install-note">安装前会备份当前模组列表；可随时在版本内容管理中禁用或删除。</div>
        `}
        <div class="file-info"><div class="file-info-title">文件信息</div><div><span>${local ? '保存文件名' : '文件名'}</span><b>${m.id}-fabric-${m.v}.jar</b></div><div><span>文件大小</span><b>${m.size}</b></div><div><span>加载器</span><b>Fabric 0.15.11</b></div><div><span>许可证</span><b>LGPL-3.0</b></div></div>
      </aside>
    </div>`);
}
function versionManagePage() {
  const managed = mods;
  const updateFor = id => mods.find(item => item.id === id)?.update || '';
  const current = instances.find(i => i.name === Q.inst) || instances[0] || {name:"未选择实例",loader:"",mods:0,updates:0};
  const visible = managed.slice(0, Math.min(8, current.mods));
  const rows = visible.map((m) => {
    const disabled = m.enabled === false;
    const update = updateFor(m.id);
    const state = disabled ? '<span class="mstat off">已禁用</span>' : '<span class="mstat on"><i></i>已启动</span>';
    const actions = disabled
      ? `<button class="mact start">${icon('play')} 启动</button><button class="micon danger" title="删除模组">${icon('trash')}</button>`
      : `${update ? `<button class="mact update">${icon('refresh')} 更新</button><button class="mact quiet" title="固定当前版本，不再检查更新">禁止更新</button>` : ''}<button class="mact quiet">禁用</button><button class="micon danger" title="删除模组">${icon('trash')}</button>`;
    return `<div class="mtable-row">
      <div class="mfile"><span class="mod-icon ${m.color}">${icon(m.ic)}</span><div><b>${m.name}</b><span>${m.file || `${m.id}-fabric-${m.v}.jar`} · ${m.size}</span></div></div>
      <div class="mver">${m.v}</div>
      <div>${state}</div>
      <div>${update ? `<span class="update-tag">可更新至 ${update}</span>` : '<span class="inline">已是最新</span>'}</div>
      <div class="mactions">${actions}</div>
    </div>`;
  }).join('');
  const switchMenu = Q.switchMenu ? `<div class="instance-menu">
    <div class="instance-menu-head"><b>切换实例</b><span>${instances.length} 个</span></div>
    ${instances.map(i => `<a class="instance-option ${i.name === current.name ? 'active' : ''}" href="?screen=manage&inst=${encodeURIComponent(i.name)}"><span class="instance-check">${i.name === current.name ? icon('check') : ''}</span><span class="instance-option-copy"><b>${i.name}</b><span>${i.loader} · ${i.mods} 个模组</span></span></a>`).join('')}
    <a class="instance-menu-foot" href="?screen=home&panel=versions">${icon('folder')} 打开游戏目录 ${icon('chevron-right')}</a>
  </div>` : '';
  return shell('home', '', `<div class="vm-head">
    <a class="back-link" href="?screen=home">${icon('chevron-left')} 返回启动</a>
    <div class="vm-title"><span class="vm-icon">${icon('folder')}</span><div><div class="page-title">版本内容管理</div><div class="page-sub">${current.mods} 个模组 · ${current.updates} 个可更新</div></div></div>
    <div class="vm-instance"><span class="inst-label">当前实例</span><a class="instance-pill ${Q.switchMenu ? 'open' : ''}" href="?screen=manage&inst=${encodeURIComponent(current.name)}&switch=${Q.switchMenu ? '0' : '1'}"><span class="inst-cube">${icon('box')}</span><span class="inst-copy"><b>${current.name}</b><span>${current.loader}</span></span>${icon('chevron-down')}</a>${switchMenu}</div>
    <div class="vm-actions"><button class="btn btn-ghost btn-sm">${icon('folder')} 打开文件夹</button><a class="btn btn-soft btn-sm" href="?screen=version&v=${encodeURIComponent(current.name)}">${icon('settings')} 版本设置</a></div>
  </div>
  <div class="vm-tabs">${['模组 ' + current.mods,'资源包 0','光影 0','存档 0','服务器 0','截图 0','蓝图 0'].map((t, i) => `<span class="vm-tab ${i === 0 ? 'active' : ''}">${t}</span>`).join('')}</div>
  <div class="vm-toolbar"><div><b>已安装模组</b><span class="inline">启动游戏时只加载已启用的模组</span></div><div class="vm-tools"><label class="mini-search">${icon('search')}<input placeholder="搜索模组"></label><span class="filter-chip active">全部</span><span class="filter-chip">可更新</span><span class="filter-chip">已禁用</span></div></div>
  <div class="mtable"><div class="mtable-head"><span>模组</span><span>版本</span><span>状态</span><span>更新</span><span>操作</span></div>${rows}</div>
  <div class="vm-foot"><span class="inline">${icon('info')} 删除操作只移除当前实例中的模组文件，不会影响其他版本。</span><span>共 ${current.mods} 个 · 显示 ${visible.length} 个</span></div>`);
}

function versionSettingsPage() {
  return shell('home', '', `<div class="vm-head">
    <a class="back-link" href="?screen=manage">${icon('chevron-left')} 返回版本内容管理</a>
    <div class="vm-title"><span class="vm-icon">${icon('box')}</span><div><div class="page-title">版本设置</div><div class="page-sub">1.20.1-OptiFine_I6 · Minecraft 1.20.1 · optifine HD U I6</div></div></div>
    <div class="vm-actions"><button class="btn btn-primary btn-sm">${icon('play')} 启动此版本</button></div>
  </div>
  <div class="vs-layout">
    <aside class="vs-nav"><span class="vs-item active">${icon('info')} 概览</span><span class="vs-item">${icon('settings')} 设置</span><span class="vs-item">${icon('edit')} 修改</span><span class="vs-item">${icon('external')} 导出</span></aside>
    <div class="vs-content">
      <section class="card vs-card vs-hero"><span class="mod-icon big green">${icon('box')}</span><div><div class="vs-name">1.20.1-OptiFine_I6</div><div class="vs-meta">Minecraft 1.20.1 · optifine HD U I6</div><div class="mod-card-tags"><span class="tag green">Fabric</span><span class="tag">OptiFine</span><span class="tag">已安装</span></div></div><span class="vs-stamp">${icon('check')} 文件完整</span></section>
      <section class="card vs-card"><div class="vs-card-title">个性化</div>
        <div class="vs-form-row"><label>图标</label><div class="select-like">自动 ${icon('chevron-down')}</div><label>分类</label><div class="select-like">自动 ${icon('chevron-down')}</div><button class="btn btn-soft btn-sm">保存分类</button></div>
        <div class="vs-chips"><button>修改版本名</button><button>修改版本描述</button><button>加入收藏夹</button><button>自定义分类</button></div>
      </section>
      <section class="card vs-card"><div class="vs-card-title">快捷方式</div><div class="vs-chips"><button>${icon('folder')} 存档与截图库</button><button>版本文件夹</button><button>存档文件夹</button><button>Mod 文件夹</button><button>截图文件夹</button></div></section>
      <section class="card vs-card"><div class="vs-card-title">启动概览</div><div class="vs-launch-grid"><div><span>Java</span><b>${window.state?.java || "自动选择"}</b></div><div><span>内存</span><b>${window.state?.ram || 4096} MB</b></div><div><span>游戏窗口</span><b>${window.state?.width || 1280} × ${window.state?.height || 720}</b></div><div><span>运行状态</span><b>请查看运行中的游戏</b></div></div></section>
      <section class="card vs-card"><div class="vs-card-title">高级管理</div><div class="vs-chips"><button>${icon('external')} 导出启动脚本</button><button>${icon('refresh')} 补全文件</button><button class="danger-text" data-delete-version="${Q.v || ''}">${icon('trash')} 删除版本</button></div><div class="vs-note">删除版本会移到本机回收目录，存档与截图默认保留。</div></section>
    </div>
  </div>`);
}

function settingsPage(panel = 'launch') {
  const card = (title, desc, body, foot = '') => `<section class="card settings-card"><div class="settings-card-head"><div><div class="settings-card-title">${title}</div><div class="settings-card-desc">${desc}</div></div>${foot}</div>${body}</section>`;
  const field = (label, input) => `<div class="field"><div class="field-label">${label}</div>${input}</div>`;
  const serverRows = (state.servers || []).map((s) => `<div class="server-manage-row"><div class="server-manage-copy"><b>${s.name || s.address}</b><span>${s.address}</span></div><div class="server-manage-actions"><button class="btn btn-ghost btn-sm" data-ping-server="${s.address}">查询</button><button class="btn btn-ghost btn-sm" data-server-menu="${s.id}">管理</button></div></div>`).join('') || `<div class="empty-line">还没有收藏服务器。</div>`;

  let body = '';
  let title = '启动设置';
  let sub = '游戏目录、Java 与默认启动参数。';

  if (panel === 'java') {
    title = 'Java 管理';
    sub = '扫描本机 Java，并指定启动时优先使用的版本。';
    const rows = (state.javaList || []).map((j) => `<div class="java-row ${j.error ? 'bad' : ''}"><div><b>Java ${j.major || '?'} · ${j.arch || ''} ${j.vendor || ''}</b><span>${j.path}</span>${j.error ? `<em>${j.error}</em>` : ''}</div><button class="btn btn-ghost btn-sm" data-use-java="${j.path}">使用</button></div>`).join('') || '<div class="empty-line">还没有扫描结果。点击「扫描本机 Java」。</div>';
    body = card('Java 运行时', '留空时由启动核心按版本自动选择；指定后优先使用该路径。', `
      ${field('当前 Java 路径', `<div class="path-row"><input class="input" data-setting-java value="${state.java || ''}" placeholder="留空自动选择"><button class="btn btn-ghost" data-pick="java">选择</button></div>`)}
      <div class="settings-actions"><button class="btn btn-ghost" data-scan-java>扫描本机 Java</button><button class="btn btn-primary" data-save-launch>保存</button></div>`)
      + card('扫描结果', '列出本机常见位置中存在的 java.exe；不会修改游戏版本。', `<div class="java-list" id="java-list">${rows}</div>`);
  } else if (panel === 'network') {
    title = '下载与网络';
    sub = '数据源、超时与网络诊断。';
    body = card('社区数据源', '公告、服务器、推荐、语录分别读取独立 JSON。', `
      <div class="info-row"><span>公告</span><code>${state.dataSources?.announcements || ''}</code></div>
      <div class="info-row"><span>服务器</span><code>${state.dataSources?.servers || ''}</code></div>
      <div class="info-row"><span>推荐</span><code>${state.dataSources?.recommendations || ''}</code></div>
      <div class="info-row"><span>语录</span><code>${state.dataSources?.quotes || ''}</code></div>
      <div class="settings-actions"><a class="btn btn-primary" href="?screen=datasource">打开数据源设置</a></div>`)
      + card('连接测试', '测试当前默认社区地址是否可访问。', `<div class="settings-actions"><button class="btn btn-ghost" data-test-all>测试四类地址</button><button class="btn btn-ghost" data-open-folder="root">打开游戏目录</button></div>`);
  } else if (panel === 'appearance') {
    title = '界面与背景';
    sub = '蓝白主题与窗口显示。';
    body = card('主题', '默认使用蓝白主题。', `
      <div class="theme-preview"><span class="theme-dot"></span><div><b>蓝白 · 默认</b><span>#006DB8 强调色 / #F3F7F9 页面底色</span></div><span class="tag">当前</span></div>
      <div class="settings-actions"><button class="btn btn-ghost" data-reset-window>恢复默认窗口 1200×780</button></div>`);
  } else if (panel === 'servers') {
    title = '服务器收藏';
    sub = '收藏的服务器会出现在主页右侧。';
    body = card('已收藏', `${(state.servers || []).length} 台服务器，状态由 SLP 实时查询。`, `<div class="server-manage-list">${serverRows}</div><div class="settings-actions"><a class="btn btn-primary" href="?screen=home&modal=add-server">添加服务器</a><a class="btn btn-ghost" href="?screen=home">返回主页</a></div>`);
  } else if (panel === 'saves') {
    title = '存档保护';
    sub = '打开存档目录，或把当前存档打包备份。';
    body = card('存档备份', '备份会写到本地数据目录的 backups 文件夹，不修改原存档。', `
      <div class="settings-actions"><button class="btn btn-primary" data-backup-saves>立即备份</button><button class="btn btn-ghost" data-open-folder="saves">打开存档文件夹</button></div>
      <div class="inline-note">备份按当前实例分别保存，恢复会创建新存档。</div><div data-save-backups></div>`);
  } else if (panel === 'bookmarks') {
    title = '我的收藏'; sub = '收藏保存在本地，可以继续下载或取消收藏。';
    body = card('资源收藏', '下载时按当前实例重新检查兼容版本。', '<div data-bookmark-list></div>');
  } else if (panel === 'games') {
    title = '运行中的游戏';
    sub = '各个游戏独立运行，关闭前会再次确认。';
    body = card('游戏会话', '关闭启动器不会自动关闭游戏。', '<div data-games-list></div>');
  } else if (panel === 'install') {
    title = '安装与导入';
    sub = '使用现有 Core 安装加载器或导入整合包，执行进度在下载任务中查看。';
    body = card('游戏安装', '支持 Fabric、Quilt、Forge 和 NeoForge。', '<div data-install-tools></div>');
  } else if (panel === 'tasks') {
    title = '下载任务';
    sub = '下载任务独立于当前页面，支持排队、暂停、取消和重试。';
    body = card('下载队列', '中断的任务可以继续；取消保留已校验缓存。', '<div data-task-list></div>');
  } else if (panel === 'recovery') {
    title = '任务与恢复';
    sub = '窗口、界面缓存与配置目录。';
    body = card('恢复操作', '用于窗口尺寸异常或界面资源损坏时。', `
      <div class="settings-actions"><button class="btn btn-ghost" data-reset-window>恢复窗口尺寸</button><button class="btn btn-ghost" data-reset-ui>重新加载界面</button><button class="btn btn-ghost" data-open-folder="config">打开配置目录</button></div>
      <div class="inline-note">重新加载不会删除账号、服务器或社区数据。</div>`);
  } else if (panel === 'logs') {
    title = '运行日志';
    sub = '最近 400 行启动与启动游戏记录。';
    body = card('日志', '日志保存在 %LOCALAPPDATA%\\MCUGLauncher\\launcher.log。', `
      <div class="settings-actions"><button class="btn btn-primary" data-read-log>刷新日志</button><button class="btn btn-ghost" data-open-folder="config">打开日志目录</button></div>
      <pre class="log-box" id="log-box">${state.logText || '点击「刷新日志」读取。'}</pre>`);
  } else if (panel === 'diagnose') {
    title = '诊断与反馈';
    sub = '查看运行环境，定位启动或显示问题。';
    body = card('系统信息', '反馈问题时请附上这些信息。', `
      <div class="settings-actions"><button class="btn btn-primary" data-system-info>读取系统信息</button><button class="btn btn-ghost" data-copy-diag>复制</button></div>
      <pre class="log-box" id="diag-box">${state.diagText || '点击「读取系统信息」。'}</pre>`);
  } else if (panel === 'about') {
    title = '关于与许可';
    sub = 'MCUG Launcher ' + appVersion() + '。';
    body = card('MCUG Launcher', '面向 Windows 的 Minecraft Java 版启动器。', `
      <div class="about-hero"><img src="assets/logo.png" alt=""><div><b>MCUG Launcher</b><span>${appVersion()} · WPF + WebView2</span></div></div>
      <div class="info-row"><span>社区数据仓库</span><a href="https://github.com/waibibabu1239/MCUGL-storage" target="_blank" rel="noreferrer">waibibabu1239/MCUGL-storage</a></div>
      <div class="info-row"><span>管理员文档</span><a href="docs/admin-and-community.md" target="_blank">docs/admin-and-community.md</a></div>
      <div class="info-row"><span>许可</span><span>沿用 MCUG Launcher 原仓库策略</span></div>`);
  } else {
    body = card('启动设置', '修改后会写入本机配置；下次启动生效。', `
      ${field('游戏根目录', `<div class="path-row"><input class="input" data-setting-root value="${state.root || ''}"><button class="btn btn-ghost" data-pick="root">选择</button></div>`)}
      ${field('Java 路径（可留空）', `<div class="path-row"><input class="input" data-setting-java value="${state.java || ''}" placeholder="留空自动选择"><button class="btn btn-ghost" data-pick="java">选择</button></div>`)}
      <div class="three-grid">
        ${field('最大内存 / MB', `<input class="input" type="number" min="512" step="512" data-setting-ram value="${state.ram || 4096}">`)}
        ${field('游戏窗口宽', `<input class="input" type="number" min="640" data-setting-width value="${state.width || 1280}">`)}
        ${field('游戏窗口高', `<input class="input" type="number" min="480" data-setting-height value="${state.height || 720}">`)}
      </div>
      <div class="settings-actions"><button class="btn btn-primary" data-save-launch>保存设置</button><a class="btn btn-ghost" href="?screen=settings&panel=java">Java 管理</a></div>`);
  }

  return shell('settings', settingsSidebar(panel), `<div class="settings-content">${pageHead(title, sub)}${body}</div>`);
}

function optionsPage() {
  const st = ['default','verifying','error','verified'].includes(Q.state) ? Q.state : 'default';
  const anon = Q.anon || st === 'verified';
  const publicName = st === 'verified' ? '阿澈' : 'waibibabu1998';
  const publicMeta = st === 'verified' ? '社区身份 #A7F3' : '玩家 ID · 暮色森林';
  const anonCard = `<section class="card settings-card">
    <div class="settings-card-head">
      <div class="settings-card-icon">${icon('users')}</div>
      <div><div class="settings-card-title">匿名模式 <span class="help-wrap ${Q.tip ? 'open' : ''}"><button class="help-dot" aria-label="匿名模式说明">?</button><span class="tooltip">开启后，社区请求使用随机别名，隐藏玩家 ID、头像和实例名称；不改变游戏、下载或微软登录。</span></span></div><div class="settings-card-desc">在社区功能中隐藏你的身份。只影响社区请求，不影响游戏与账号。</div></div>
      <div class="settings-card-aside"><span class="switch-text">${anon ? '已开启' : '已关闭'}</span><button class="switch ${anon ? 'on' : ''}" role="switch" aria-checked="${anon}" data-anon></button></div>
    </div>
    <div class="anon-scope"><span class="scope-label">影响</span><span class="tag green">公告</span><span class="tag green">服务器</span><span class="tag green">推荐</span><span class="tag green">语录</span><span class="scope-sep"></span><span class="scope-label">不影响</span><span class="tag">游戏启动</span><span class="tag">下载</span><span class="tag">微软登录</span></div>
    <div class="anon-preview">
      <div class="who-card ${anon ? '' : 'on'}">
        <span class="who-label">关闭时</span>
        <div class="who-id"><span class="who-avatar">${Avatar()}</span><div><b>${publicName}</b><span>${publicMeta}</span></div></div>
      </div>
      <span class="anon-arrow">${icon('arrow-right')}</span>
      <div class="who-card ${anon ? 'on' : ''}">
        <span class="who-label">开启后</span>
        <div class="who-id"><span class="who-avatar ghost">${icon('hash')}</span><div><b>群友 #A7F3</b><span>匿名实例 · 社区请求</span></div></div>
      </div>
    </div>
    ${anon ? `<div class="status-bar green">${icon('shield')}<div><b>匿名模式已开启</b> · 社区请求使用随机别名，不显示玩家 ID、实例名称与头像。</div></div>` : `<div class="status-bar info">${icon('info')}<div>关闭后，社区功能会显示你的玩家 ID 与实例信息。不会收集其他使用数据。</div></div>`}
  </section>`;

  let keyBody = '';
  if (st === 'verified') {
    keyBody = `<div class="verified-box">
      <div class="verified-avatar">${Avatar()}</div>
      <div><div class="verified-name">阿澈 <span class="identity-badge">${icon('check')} 已验证</span></div><div class="verified-meta">社区身份 #A7F3 · 密钥已保存在本机</div></div>
      <div class="verified-actions"><button class="btn-link" data-verify-reset>更换密钥</button><button class="btn-link muted">退出</button></div>
    </div>
    <div class="key-helper">${icon('lock')} 密钥仅保存在本机，不会随启动器版本更新失效；清除配置或手动退出后需要重新验证。</div>`;
  } else if (st === 'verifying') {
    keyBody = `<div class="key-row"><div class="field"><div class="field-label">社区密钥</div><input class="input" value="mck_live_••••••••••••4f2a" disabled></div><button class="btn btn-primary" disabled><span class="spinner"></span>验证中…</button></div>
      <div class="status-bar info" style="margin-top:10px">${icon('loader')}<div>正在向社区服务验证密钥，请稍候。不会上传游戏或账号信息。</div></div>`;
  } else if (st === 'error') {
    keyBody = `<div class="key-row"><div class="field"><div class="field-label">社区密钥</div><input class="input error" value="mck_old_7d2c9a1b" autofocus></div><button class="btn btn-primary">重新验证</button></div>
      <div class="field-error" style="margin-top:7px">${icon('alert')} 密钥无效或已过期，请向群管理获取新密钥。</div>
      <div class="key-helper">${icon('lock')} 密钥仅保存在本机，不会上传到除社区验证接口以外的任何地方。</div>`;
  } else {
    keyBody = `<div class="key-row"><div class="field"><div class="field-label">社区密钥</div><input class="input" placeholder="粘贴群管理发给你的密钥" data-key-input></div><button class="btn btn-primary" data-verify>验证</button></div>
      <div class="key-helper">${icon('lock')} 粘贴时会自动去除首尾空格。密钥仅保存在本机，不会随版本更新失效。</div>`;
  }

  const keyUses = ['default', 'error'].includes(st) ? `<div class="key-uses">
    <span>${icon('check')} 显示社区身份标签</span>
    <span>${icon('check')} 未来可用于推荐投稿</span>
    <span class="no">${icon('x')} 不是激活码，不验证正版</span>
  </div>` : '';

  const keyCard = `<section class="card settings-card">
    <div class="settings-card-head">
      <div class="settings-card-icon">${icon('key')}</div>
      <div><div class="settings-card-title">社区身份密钥</div><div class="settings-card-desc">用于标识你在社区中的身份，不是激活码，也不验证正版账号。</div></div>
      <div class="settings-card-aside">${st === 'verified' ? `<span class="identity-badge">${icon('shield')} 已认证</span>` : `<span class="tag gray">未验证</span>`}</div>
    </div>
    ${keyBody}${keyUses}
  </section>`;

  return shell('options', settingsSidebar('options'), pageHead('更多选项', '社区身份、匿名与本地保存方式。', `<span class="inline">${icon('lock')} 设置仅保存在本机</span>`) + `<div class="settings-body options-body">${anonCard}${keyCard}</div>`);
}

/* ---------------- settings: admin ---------------- */
function adminPage() {
  let st = ['before','verifying','after','published'].includes(Q.state) ? Q.state : 'before';
  if (Q.modal === 'publish' && (Q.state === 'saved' || Q.state === 'normal' || Q.state === 'default')) st = 'after';
  const authed = st === 'after' || st === 'published';
  const published = st === 'published';
  const endpoint = published || Q.endpoint;
  if (!authed) {
    const verifying = st === 'verifying';
    const failed = Q.state === 'error';
    return shell('admin', settingsSidebar('admin'), pageHead('管理员', '仅群管理员使用。认证后可发布公告。', `<span class="inline">${icon('lock')} 本机认证</span>`) + `
      <div class="admin-gate">
        <section class="card settings-card admin-auth-card">
          <div class="settings-card-head">
            <div class="settings-card-icon">${icon('shield')}</div>
            <div><div class="settings-card-title">管理员认证</div><div class="settings-card-desc">首次输入会设置本机管理员密钥；之后必须输入同一密钥。</div></div>
            <div class="settings-card-aside"><span class="tag ${failed ? 'red' : 'gray'}">${verifying ? '验证中' : failed ? '认证失败' : '未认证'}</span></div>
          </div>
          <div class="key-row"><div class="field"><div class="field-label">管理员密钥</div><input class="input ${failed ? 'error' : ''}" placeholder="粘贴管理员密钥" ${verifying || failed ? 'value="mck_admin_••••••••••8c1d"' : ''} ${verifying ? 'disabled' : ''}></div><button class="btn btn-primary" ${verifying ? 'disabled' : ''}>${verifying ? '<span class="spinner"></span>验证中…' : failed ? '重新验证' : '验证'}</button></div>
          ${failed ? `<div class="field-error" style="margin-top:7px">${icon('alert')} 管理员密钥无效或已过期，请向群主确认。</div>` : verifying ? `<div class="status-bar info" style="margin-top:10px">${icon('loader')}<div>正在验证管理员身份…</div></div>` : `<div class="key-helper">${icon('lock')} 管理员密钥与普通社区密钥相互独立，仅保存在本机。认证后才会出现发布入口。</div>`}
        </section>
      </div>`);
  }

  const adminIdentity = `<div class="admin-identity">
    <div class="admin-shield">${icon('shield')}</div>
    <div><div class="admin-name">TianXuan <span class="identity-badge">${icon('check')} 管理员</span></div><div class="admin-meta">群主 · 密钥已保存在本机 · 认证于 10 月 30 日</div></div>
    <div class="verified-actions"><button class="btn-link muted">退出管理</button></div>
  </div>`;

  return shell('admin', settingsSidebar('admin'), pageHead('管理员', '发布公告、查看最近的发布记录。', `<span class="inline">${icon('shield')} 已认证</span>`) + `
    <div class="settings-body">
      <section class="card settings-card">
        ${adminIdentity}
        <div class="admin-publish-row">
          <div class="admin-publish-copy"><div class="admin-publish-title">发布一条新公告</div><div class="admin-publish-desc">标题 + 正文，可选过期时间。发布后出现在主页公告栏。</div></div>
          <a class="btn btn-primary btn-sm" href="?screen=admin&state=after&modal=publish${endpoint ? '&endpoint=1' : ''}">${icon('send')} 发布公告</a>
        </div>
        ${endpoint ? `<div class="status-bar green" style="margin-top:12px">${icon('check')}<div><b>写入接口已连接</b> · 发布会立即同步到社区数据源。</div></div>` : `<div class="status-bar amber" style="margin-top:12px">${icon('alert')}<div><b>写入接口未配置</b> · 发布会先保存为「待发布」，你可以在数据源设置中填写写入接口后再同步。</div></div>`}
      </section>
      <section class="card settings-card">
        <div class="settings-card-head"><div class="settings-card-icon">${icon('clock')}</div><div><div class="settings-card-title">最近发布</div><div class="settings-card-desc">仅显示本机记录，不会暴露给普通用户。</div></div><a class="card-action" href="?screen=datasource">${icon('database')} 数据源设置</a></div>
        <div class="recent-list">
          <div class="recent-row"><span class="tag green">已发布</span><span class="title">0.22.3 社区功能测试开启</span><span class="time">10 月 30 日 23:10</span></div>
          <div class="recent-row"><span class="tag amber">待发布</span><span class="title">周五晚 8 点 · 群友合建</span><span class="time">10 月 29 日 18:42</span></div>
        </div>
      </section>
      <section class="card settings-card admin-preview-card">
        <div class="settings-card-head"><div class="settings-card-icon">${icon('monitor')}</div><div><div class="settings-card-title">主页预览</div><div class="settings-card-desc">普通用户看到的公告栏样式，发布前先确认措辞。</div></div><a class="card-action" href="?screen=home">${icon('external')} 打开主页</a></div>
        <div class="preview-ann">
          <div class="preview-ann-date"><b>30</b><span>10 月</span></div>
          <div class="preview-ann-image">${QuotePhoto()}</div>
          <div class="preview-ann-copy"><div class="preview-ann-title"><span class="tag green">更新</span>${announcements[0].title}</div><div class="preview-ann-text">${announcements[0].text}</div></div>
          <span class="preview-ann-more">查看全部 ${icon('chevron-right')}</span>
        </div>
      </section>
      ${published ? `<div class="status-bar green">${icon('check')}<div><b>公告已发布</b> · 1 分钟前同步到主页公告栏，可在「查看全部」中管理。</div></div>` : ''}
    </div>`);
}

/* ---------------- settings: datasource ---------------- */
const DS = [
  { key:'announcements', label:'公告', hint:'主页公告栏与全部公告', url:'https://raw.githubusercontent.com/waibibabu1239/MCUGL-storage/main/announcements.json' },
  { key:'servers', label:'服务器', hint:'主页正在开设的服务器', url:'https://raw.githubusercontent.com/waibibabu1239/MCUGL-storage/main/servers.json' },
  { key:'recommendations', label:'推荐', hint:'主页摘要与下载页群友推荐', url:'https://raw.githubusercontent.com/waibibabu1239/MCUGL-storage/main/recommendations.json' },
  { key:'quotes', label:'语录', hint:'主页群友语录（文字或图片）', url:'https://raw.githubusercontent.com/waibibabu1239/MCUGL-storage/main/quotes.json' },
];

function datasourcePage() {
  const err = Q.state === 'error';
  const urls = {
    announcements: err ? 'https://raw.githubusercontent.com/waibibabu1239/MCUGL-storage/main/announcements-v2.json' : DS[0].url,
    servers: DS[1].url,
    recommendations: err ? 'github.com/waibibabu1239/MCUGL-storage/recommendations.json' : DS[2].url,
    quotes: DS[3].url,
  };
  const results = err ? {
    announcements: ['bad','HTTP 404 · 文件不存在'],
    servers: ['bad','连接超时（8s）'],
    recommendations: ['bad','地址需以 https:// 开头'],
    quotes: ['ok','连接正常 · 104 ms'],
  } : {
    announcements: ['ok','连接正常 · 128 ms'],
    servers: ['ok','连接正常 · 96 ms'],
    recommendations: ['','尚未测试'],
    quotes: ['','尚未测试'],
  };
  const rows = DS.map(d => `<div class="ds-row">
    <div class="ds-label"><b>${d.label}</b><span>${d.hint}</span></div>
    <div class="ds-input-wrap"><input class="input ${err && (d.key === 'announcements' || d.key === 'recommendations') ? 'error' : ''}" value="${urls[d.key]}"></div>
    <button class="btn btn-ghost ds-test">${results[d.key][0] === 'ok' ? icon('check') : icon('wifi')} 测试</button>
    <div class="ds-result ${results[d.key][0]}">${results[d.key][0] === 'ok' ? icon('check') : results[d.key][0] === 'bad' ? icon('alert') : icon('clock')} ${results[d.key][1]}</div>
  </div>`).join('');

  return shell('datasource', settingsSidebar('datasource'), pageHead('数据源', '四类数据分别读取独立 JSON，切换只影响对应的区块。', `<span class="inline">${icon('database')} 支持 GitHub Raw / 自有服务器</span>`) + `
    <div class="settings-body">
      <section class="card settings-card">
        <div class="ds-preset">
          <div class="segmented"><button class="seg-btn ${err ? '' : 'active'}">GitHub 官方源</button><button class="seg-btn ${err ? 'active' : ''}">自定义</button></div>
          <span class="ds-preset-note">${err ? '当前为自定义源，四项可分别填写。' : '官方源跟随 waibibabu1239/MCUGL-storage 仓库更新。'}</span>
        </div>
        <div class="ds-rows">${rows}</div>
        <div class="ds-write">
          <div class="ds-label"><b>写入接口</b><span>管理员发布，可选</span></div>
          <input class="input" placeholder="https://api.example.com/announcements（留空则发布为待发布）" value="${err ? '' : ''}">
          <div class="ds-write-note">只用于管理员发布公告；普通用户不会调用。留空时发布的内容只保存在本机，需导出 JSON 手动上传。</div>
        </div>
        <div class="ds-footer">
          <button class="btn btn-primary">保存数据源</button>
          <button class="btn btn-ghost">恢复默认</button>
          <span class="spacer"></span>
          <span class="ds-footer-note">${icon('info')} 四项独立生效，保存后立即刷新对应区块</span>
        </div>
      </section>
      <div class="status-bar ${err ? 'red' : 'info'}">${icon(err ? 'alert' : 'info')}<div>${err ? '<b>有 3 个数据源不可用。</b> 红色结果只影响对应区块，其他内容仍会正常显示。修正后逐项测试，再保存。' : '修改任一项后点击「保存数据源」才会生效。测试只请求该地址，不会写入任何内容。'}</div></div>
    </div>`);
}

/* ---------------- modals ---------------- */
function addServerModal() {
  const edit = Q.modal === 'edit-server';
  const editing = edit ? servers.find((s) => s.id === Q.sid) : null;
  const err = (Q.merr === '1' || Q.state === 'error') && !edit;
  return `<div class="overlay">
    <section class="modal" role="dialog" aria-modal="true" aria-labelledby="add-title">
      <div class="modal-head"><div class="modal-icon">${icon('server')}</div><div><div class="modal-title" id="add-title">${edit ? '编辑服务器' : '添加服务器'}</div><div class="modal-sub">支持 host 或 host:port，默认端口 25565。</div></div><a class="modal-close" href="?screen=home" aria-label="关闭">${icon('x')}</a></div>
      <div class="modal-body">
        <div class="field"><div class="field-label">名称 <span class="opt">可选</span></div><input class="input" data-server-name placeholder="例如：暮色森林" value="${editing?.name || (edit ? '暮色森林' : err ? '暮色森林' : '')}"></div>
        <div class="field"><div class="field-label">服务器地址</div><input class="input ${err ? 'error' : ''}" data-server-address placeholder="play.example.com 或 play.example.com:25565" value="${editing?.address || (edit ? 'play.mclc.cn:25565' : err ? 'play.mclc.cn:2556' : '')}"><input type="hidden" data-server-old value="${editing?.address || ''}"></div>
        ${err ? `<div class="field-error">${icon('alert')} 端口 2556 无法连接。请检查端口，或只填写域名使用默认 25565。</div>` : `<div class="field-help">${edit ? '修改后会立即重新查询状态，并更新本机保存的地址。' : '添加后会立即查询一次状态；之后只在卡片可见时刷新，每 30 秒最多一次。'}</div>`}
        ${err ? `<div class="modal-status red">${icon('alert')}<div><b>无法连接到该服务器</b> · 请确认地址、端口与服务器是否在线。</div></div>` : `<div class="modal-status ${edit ? 'info' : 'green'}">${icon(edit ? 'info' : 'wifi')}<div>${edit ? '只修改本机收藏，不会影响服务器本身。' : '支持 Java 版服务器列表 ping；不解析 DNS SRV，必要时请填写实际端口。'}</div></div>`}
      </div>
      <div class="modal-foot"><a class="btn btn-ghost" href="?screen=home">取消</a>${edit ? `<button class="btn-link danger-link">${icon('trash')} 移除收藏</button>` : ''}<span class="spacer"></span><button class="btn btn-primary">${err ? `${icon('refresh')} 重试` : edit ? `${icon('check')} 保存修改` : `${icon('plus')} 添加并查询`}</button></div>
    </section>
  </div>`;
}

function publishModal() {
  const saved = Q.state === 'saved';
  if (saved && Q.endpoint) {
    return `<div class="overlay"><section class="modal wide" role="dialog" aria-modal="true">
      <div class="modal-head"><div class="modal-icon">${icon('check')}</div><div><div class="modal-title">公告已发布</div><div class="modal-sub">已同步到社区数据源，主页公告栏随即更新。</div></div><a class="modal-close" href="?screen=admin&state=after&endpoint=1" aria-label="关闭">${icon('x')}</a></div>
      <div class="modal-body">
        <div class="modal-status green">${icon('check')}<div><b>1 分钟前同步完成</b> · 公告已写入 announcements.json，其他用户刷新主页即可看到。</div></div>
        <div class="publish-preview"><div class="publish-preview-image">${QuotePhoto()}</div><div><div class="publish-preview-label">已发布内容</div><div class="publish-preview-title">0.22.3 社区功能测试开启</div><div class="publish-preview-text">群友推荐、服务器状态与数据源切换开始试运行，欢迎反馈。</div><div class="publish-preview-meta">10 月 31 日 · 管理员 TianXuan · 不过期</div></div></div>
      </div>
      <div class="modal-foot"><a class="btn btn-ghost" href="?screen=admin&state=after&modal=publish&endpoint=1">${icon('plus')} 再发一条</a><span class="spacer"></span><a class="btn btn-primary" href="?screen=home">查看主页</a></div>
    </section></div>`;
  }
  if (saved) {
    return `<div class="overlay"><section class="modal wide" role="dialog" aria-modal="true">
      <div class="modal-head"><div class="modal-icon">${icon('clock')}</div><div><div class="modal-title">已保存为待发布</div><div class="modal-sub">内容已保存在本机，不会自动同步到社区。</div></div><a class="modal-close" href="?screen=admin&state=after" aria-label="关闭">${icon('x')}</a></div>
      <div class="modal-body">
        <div class="modal-status amber">${icon('alert')}<div><b>公告草稿已保存</b> · 你可以在数据源设置中填写写入接口后重新发布，或导出 JSON 手动上传到 GitHub。</div></div>
        <div class="publish-preview"><div class="publish-preview-image">${QuotePhoto()}</div><div><div class="publish-preview-label">预览</div><div class="publish-preview-title">0.22.3 社区功能测试开启</div><div class="publish-preview-text">群友推荐、服务器状态与数据源切换开始试运行，欢迎反馈。</div><div class="publish-preview-meta">10 月 31 日 · 管理员 TianXuan</div></div></div>
      </div>
      <div class="modal-foot"><button class="btn btn-ghost">${icon('copy')} 导出 JSON</button><span class="spacer"></span><a class="btn btn-primary" href="?screen=admin&state=after">完成</a></div>
    </section></div>`;
  }
  return `<div class="overlay"><section class="modal wide" role="dialog" aria-modal="true" aria-labelledby="pub-title">
    <div class="modal-head"><div class="modal-icon">${icon('send')}</div><div><div class="modal-title" id="pub-title">发布公告</div><div class="modal-sub">发布后会出现在主页公告栏，普通用户只读。</div></div><a class="modal-close" href="?screen=admin&state=after" aria-label="关闭">${icon('x')}</a></div>
    <div class="modal-body">
      <div class="field"><div class="field-label">标题</div><input class="input" value="0.22.3 社区功能测试开启"></div>
      <div class="field"><div class="field-label">正文</div><textarea class="textarea">群友推荐、服务器状态与数据源切换开始试运行，欢迎反馈。</textarea><div class="char-count">37 / 500</div></div>
      <div class="field"><div class="field-label">配图 <span class="opt">可选</span></div>
        <div class="image-upload has-image"><div class="image-upload-preview">${QuotePhoto()}</div><div class="image-upload-copy"><b>公告封面图已选择</b><span>1024 × 640 · JPG · 218 KB</span><div class="image-upload-actions"><button class="btn-link pick-publish-image">${icon('image')} 更换图片</button><button class="btn-link muted remove-publish-image">移除</button></div></div></div>
        <input type="file" id="publish-image-input" accept="image/png,image/jpeg,image/webp" hidden>
      </div>
      <div class="expiry-row"><label class="checkbox-line"><span class="checkbox checked">${icon('check')}</span>设置过期时间</label><input class="input" value="2026-11-15 23:59" aria-label="过期时间"><span class="muted" style="font-size:9px">到期后自动隐藏</span></div>
      <div class="modal-status ${Q.endpoint ? 'green' : 'amber'}">${icon(Q.endpoint ? 'check' : 'alert')}<div>${Q.endpoint ? '<b>写入接口已连接</b> · 点击发布后会立即同步到社区数据源。' : '<b>写入接口未配置</b> · 本次发布只会保存为「待发布」，可在数据源设置中配置后再同步。'}</div></div>
    </div>
    <div class="modal-foot"><a class="btn btn-ghost" href="?screen=admin&state=after">取消</a><span class="spacer"></span><button class="btn btn-soft">${icon('edit')} 存为草稿</button><button class="btn btn-primary">${icon('send')} ${Q.endpoint ? '发布公告' : '保存为待发布'}</button></div>
  </section></div>`;
}

function recManageModal() {
  return `<div class="overlay"><section class="modal" role="dialog" aria-modal="true" aria-labelledby="rec-manage-title">
    <div class="modal-head"><div class="modal-icon">${icon('sparkles')}</div><div><div class="modal-title" id="rec-manage-title">管理群友推荐</div><div class="modal-sub">删除只影响这台电脑的显示；管理员可在数据源中同步修改。</div></div><a class="modal-close" href="?screen=home" aria-label="关闭">${icon('x')}</a></div>
    <div class="modal-body rec-manage-list">
      ${recs.map((r) => `<div class="rec-manage-row"><span class="rec-icon ${tileClass(r.color)}">${icon(r.icon)}</span><div class="rec-manage-copy"><b>${r.name}</b><span>${typeMeta[r.type].label} · 由 ${r.author} 推荐</span></div><button class="micon danger" data-delete-rec="${r.id}" title="删除推荐" aria-label="删除推荐">${icon('trash')}</button></div>`).join('')}
    </div>
    <div class="modal-foot"><span class="modal-note">共 ${recs.length} 条推荐。</span><span class="spacer"></span><a class="btn btn-primary" href="?screen=home">完成</a></div>
  </section></div>`;
}

function announcementsModal() {
  return `<div class="overlay"><section class="modal wide" role="dialog" aria-modal="true" aria-labelledby="ann-title">
    <div class="modal-head"><div class="modal-icon">${icon('bell')}</div><div><div class="modal-title" id="ann-title">全部公告</div><div class="modal-sub">由群管理员发布，按时间倒序排列。</div></div><a class="modal-close" href="?screen=home" aria-label="关闭">${icon('x')}</a></div>
    <div class="modal-body ann-modal-body">
      ${announcements.map((a, i) => `<article class="ann-modal-row ${i === 0 ? 'latest' : ''}">
        <div class="ann-modal-date"><b>${a.date.split('/')[1]}</b><span>${a.date.split('/')[0]} 月</span></div>
        ${a.image ? `<div class="ann-modal-image">${annImage(a)}</div>` : ''}
        <div class="ann-modal-copy"><div class="ann-modal-title"><span class="tag ${i === 0 ? 'green' : 'gray'}">${a.tag}</span>${a.title}</div><div class="ann-modal-text">${a.text}</div><div class="ann-modal-time">${a.time} · 管理员 TianXuan</div></div>
        <button class="ann-delete" data-delete-announcement="${a.id}" title="删除公告" aria-label="删除公告">${icon('trash')}</button>
      </article>`).join('')}
    </div>
    <div class="modal-foot"><span class="modal-note">公告会保留到过期时间；过期后自动隐藏。</span><span class="spacer"></span><a class="btn btn-primary" href="?screen=home">完成</a></div>
  </section></div>`;
}

function microsoftLoginModal() {
  return `<div class="overlay"><section class="modal" role="dialog" aria-modal="true" aria-labelledby="ms-title">
    <div class="modal-head"><div class="modal-icon">${icon('users')}</div><div><div class="modal-title" id="ms-title">正版账号登录</div><div class="modal-sub">使用 Microsoft 账号完成正版验证，登录后可启动正版实例。</div></div><a class="modal-close" href="?screen=home" aria-label="关闭">${icon('x')}</a></div>
    <div class="modal-body">
      <div class="inline-note">${icon('info')} 点击下方按钮后，会打开系统默认浏览器完成 Microsoft 授权；授权完成后回到启动器即可。</div>
      <div class="modal-status green" style="margin-top:14px">${icon('check')}<div><b>启动器不会保存你的密码</b><br>只保存用于启动游戏的授权令牌，可在「切换本地账号」中退出。</div></div>
    </div>
    <div class="modal-foot"><a class="btn btn-ghost" href="?screen=home">取消</a><span class="spacer"></span><button class="btn btn-primary" type="button" data-ms-login>${icon('external')} 打开浏览器登录</button></div>
  </section></div>`;
}

function skinModal() {
  const verified = Q.skin === 'verified';
  return `<div class="overlay"><section class="modal skin-modal" role="dialog" aria-modal="true" aria-labelledby="skin-title">
    <div class="modal-head"><div class="modal-icon">${icon('shirt')}</div><div><div class="modal-title" id="skin-title">皮肤与披风</div><div class="modal-sub">只改变角色外观，不影响账号安全与游戏启动。</div></div><a class="modal-close" href="?screen=home" aria-label="关闭">${icon('x')}</a></div>
    <div class="modal-body skin-body">
      <div class="skin-hero">
        <div class="skin-figure">${SkinFigure()}</div>
        <div class="skin-hero-copy"><b>waibibabu1998</b><span>正版账号 · 当前使用本地皮肤</span><div class="skin-tags"><span class="tag green">经典皮肤</span><span class="tag">Alex 模型</span></div></div>
      </div>
      <div class="skin-section">
        <div class="skin-section-head"><div class="skin-section-icon">${icon('image')}</div><div><b>当前皮肤</b><span>本地皮肤只保存在这台电脑，不会自动上传。</span></div></div>
        <div class="skin-actions"><button class="btn btn-soft btn-sm">${icon('image')} 更换皮肤文件</button><button class="btn btn-ghost btn-sm">${icon('shield')} 使用正版皮肤</button><span class="skin-file">waibibabu1998.png · 64×64</span></div>
      </div>
      <div class="skin-section">
        <div class="skin-section-head"><div class="skin-section-icon">${icon('key')}</div><div><b>皮肤站认证</b><span>${verified ? '已连接 LittleSkin，可同步皮肤与披风。' : '认证后可同步皮肤站里的皮肤与披风；不认证也能使用本地皮肤。'}</span></div><span class="skin-state ${verified ? 'ok' : ''}">${verified ? '已认证' : '未认证'}</span></div>
        <div class="skin-station"><div class="skin-station-id"><b>${verified ? 'LittleSkin · 阿澈' : 'LittleSkin'}</b><span>${verified ? '上次同步 2 分钟前 · 自动同步已开启' : '支持 Blessing Skin 系皮肤站'}</span></div><button class="btn btn-ghost btn-sm">${verified ? '更换绑定' : '认证皮肤站'}</button></div>
      </div>
      <div class="skin-section">
        <div class="skin-section-head"><div class="skin-section-icon">${icon('cape')}</div><div><b>披风</b><span>仅在支持披风显示的服务器中可见。</span></div></div>
        <div class="cape-options">
          <label class="cape-option"><span class="cape-swatch none">${icon('x')}</span><span>不显示</span></label>
          <label class="cape-option selected"><span class="cape-swatch custom"></span><span>自定义披风</span><span class="cape-check">${icon('check')}</span></label>
          <label class="cape-option"><span class="cape-swatch mojang"></span><span>Mojang 披风</span><span class="skin-lock">正版账号</span></label>
        </div>
      </div>
    </div>
    <div class="modal-foot"><a class="btn btn-ghost" href="?screen=home">取消</a><span class="spacer"></span><button class="btn btn-primary">${icon('check')} 保存更改</button></div>
  </section></div>`;
}

/* ---------------- shell ---------------- */
function muaLoginModal() {
  return `<div class="overlay"><section class="modal" role="dialog" aria-modal="true" aria-labelledby="mua-title">
    <div class="modal-head"><div class="modal-icon">${icon('key')}</div><div><div class="modal-title" id="mua-title">MUA 登录</div><div class="modal-sub">使用 LittleSkin / MUA 账号密码登录，仅用于外置登录。</div></div><a class="modal-close" href="?screen=home" aria-label="关闭">${icon('x')}</a></div>
    <div class="modal-body">
      <div class="field"><div class="field-label">MUA 账号</div><input class="input" id="mua-account" autocomplete="username" placeholder="邮箱或用户名"></div>
      <div class="field"><div class="field-label">密码</div><input class="input" id="mua-password" type="password" autocomplete="current-password" placeholder="MUA 密码"></div>
      <div class="modal-status info">${icon('info')}<div>登录后启动器会用 authlib-injector 连接 MUA 皮肤站；密码只发送给 skin.mualliance.ltd。</div></div>
    </div>
    <div class="modal-foot"><a class="btn btn-ghost" href="?screen=home">取消</a><span class="spacer"></span><button class="btn btn-primary" data-mua-submit>登录 MUA</button></div>
  </section></div>`;
}

function shell(active, sidebar, content) {
  let overlay = '';
  if (Q.modal === 'add-server' || Q.modal === 'edit-server') overlay = addServerModal();
  if (Q.modal === 'publish') overlay = publishModal();
  if (Q.modal === 'announcements') overlay = announcementsModal();
  if (Q.modal === 'recs-manage') overlay = recManageModal();
  if (Q.modal === 'skin') overlay = skinModal();
  if (Q.modal === 'ms') overlay = microsoftLoginModal();
  if (Q.modal === 'mua') overlay = muaLoginModal();
  return `<div class="app">
    ${titlebar(active)}
    <div class="workspace">${sidebar}<main class="content">${content}</main></div>
    ${overlay}
  </div>`;
}

function render() {
  let html = '';
  if (Q.screen === 'download') html = downloadPage();
  else if (Q.screen === 'mods') html = modListPage();
  else if (Q.screen === 'mod') html = modDetailPage();
  else if (Q.screen === 'catalog') html = catalogPage(Q.type || 'mod', Q.q || '');
  else if (Q.screen === 'manage') html = versionManagePage();
  else if (Q.screen === 'version') html = versionSettingsPage();
  else if (Q.screen === 'settings') html = settingsPage(Q.panel || 'launch');
  else if (Q.screen === 'options') html = optionsPage();
  else if (Q.screen === 'admin') html = adminPage();
  else if (Q.screen === 'datasource') html = datasourcePage();
  else html = homePage();
  document.getElementById('app').innerHTML = html;
  bind();
  window.McugTasks?.mount();
  window.McugInstall?.mount();
  window.McugGames?.mount();
  window.McugBookmarks?.mount();
  window.McugSaves?.mount();
}

function bind() {
  const anon = document.querySelector('[data-anon]');
  if (anon) anon.addEventListener('click', () => {
    const next = !anon.classList.contains('on');
    const u = new URL(location.href);
    u.searchParams.set('screen', 'options');
    if (next) u.searchParams.set('anon', '1'); else u.searchParams.delete('anon');
    if (Q.state === 'verified') u.searchParams.set('state', 'verified');
    go(u.toString());
  });
  const verify = document.querySelector('[data-verify]');
  if (verify) verify.addEventListener('click', () => {
    const u = new URL(location.href);
    u.searchParams.set('screen', 'options');
    u.searchParams.set('state', 'verified');
    go(u.toString());
  });
  const reset = document.querySelector('[data-verify-reset]');
  if (reset) reset.addEventListener('click', () => {
    const u = new URL(location.href);
    u.searchParams.set('screen', 'options');
    u.searchParams.delete('state');
    go(u.toString());
  });
  const help = document.querySelector('.help-dot');
  if (help) help.addEventListener('click', () => help.parentElement.classList.toggle('open'));
  const rail = document.querySelector('.server-rail');
  const railPrev = document.querySelector('[data-rail-prev]');
  const railNext = document.querySelector('[data-rail-next]');
  if (rail && railPrev) railPrev.addEventListener('click', () => rail.scrollBy({ left: -206, behavior: 'smooth' }));
  if (rail && railNext) railNext.addEventListener('click', () => rail.scrollBy({ left: 206, behavior: 'smooth' }));
  const quote = document.querySelector('.quote-strip');
  if (quote && !Q.modal) quote.addEventListener('click', (event) => {
    if (event.target.closest('button, a')) return;
    const u = new URL(location.href);
    u.searchParams.set('screen', 'home');
    u.searchParams.set('qi', String((Q.qi + 1) % quotes.length));
    u.searchParams.delete('quote');
    go(u.toString());
  });
  const prev = document.querySelector('[data-ann-prev]');
  const next = document.querySelector('[data-ann-next]');
  if (prev) prev.addEventListener('click', () => {
    const u = new URL(location.href);
    u.searchParams.set('ann', String((Q.ann + announcements.length - 1) % announcements.length));
    go(u.toString());
  });
  if (next) next.addEventListener('click', () => {
    const u = new URL(location.href);
    u.searchParams.set('ann', String((Q.ann + 1) % announcements.length));
    go(u.toString());
  });
  document.querySelector('[data-recs-manage]')?.addEventListener('click', () => {
    go('?screen=home&modal=recs-manage');
  });
  document.querySelectorAll('[data-delete-announcement]').forEach((button) => {
    button.addEventListener('click', () => {
      const id = button.getAttribute('data-delete-announcement');
      if (!confirm('确定删除这条公告？')) return;
      const index = announcements.findIndex((a) => a.id === id);
      if (index >= 0) announcements.splice(index, 1);
      window.chrome?.webview?.postMessage({ type: 'saveAnnouncements', items: announcements });
    });
  });
  document.querySelectorAll('[data-delete-rec]').forEach((button) => {
    button.addEventListener('click', () => {
      const id = button.getAttribute('data-delete-rec');
      if (!confirm('确定删除这条群友推荐？')) return;
      const index = recs.findIndex((r) => r.id === id);
      if (index >= 0) recs.splice(index, 1);
      window.chrome?.webview?.postMessage({ type: 'saveRecommendations', items: recs });
    });
  });
  document.querySelectorAll('[data-server-menu]').forEach(menu => menu.addEventListener('click', () => {
    const id = menu.getAttribute('data-server-menu');
    const u = new URL(location.href);
    if (Q.menu === id) u.searchParams.delete('menu'); else u.searchParams.set('menu', id);
    go(u.toString());
  }));
  document.querySelectorAll('[data-delete-server]').forEach((button) => {
    button.addEventListener('click', () => {
      const address = button.getAttribute('data-delete-server');
      if (confirm(`确定移除服务器“${address}”？`)) {
        window.chrome?.webview?.postMessage({ type: 'deleteServer', address });
      }
    });
  });
}
// Mock content is available only in standalone browser previews.
if (window.chrome?.webview) {
  for (const collection of [servers, announcements, recs, quotes, versionGroups, instances, mods]) collection.splice(0);
}
render();






