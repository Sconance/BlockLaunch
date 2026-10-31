// Run with PLAYWRIGHT_MODULE set to an installed playwright module path, or npm-installed playwright.
const {chromium}=require(process.env.PLAYWRIGHT_MODULE || 'playwright');
const {pathToFileURL}=require('node:url');
const path=require('node:path');
const fs=require('node:fs');
const assert=require('node:assert/strict');
(async()=>{
 const browser=await chromium.launch({headless:true,channel:process.env.BROWSER_CHANNEL || 'msedge'});
 const page=await browser.newPage(); const errors=[];
 page.on('pageerror',error=>errors.push(error.message));
 await page.addInitScript(()=>{
  window.messages=[]; const listeners=[];
  window.deliver=message=>listeners.forEach(fn=>fn({data:message}));
  window.chrome={webview:{addEventListener:(type,fn)=>listeners.push(fn),postMessage:message=>{
   window.messages.push(message);
   if(message.type==='ready')queueMicrotask(()=>window.deliver({type:'state',player:'Fixture',accountType:'offline',root:'fixture',java:'',ram:4096,width:1280,height:720,selectedVersion:'1.21.1',selectedInstance:'1.21.1',versionGroups:[{label:'原版游戏',count:1,versions:[['1.21.1','1.21.1 · vanilla']]}],instances:[{name:'1.21.1',loader:'原版',mods:0,updates:0}],mods:[],servers:[{name:'Fixture Server',address:'localhost:25565'}],dataSources:{}}));
  }}};
 });
 const base=pathToFileURL(path.resolve(__dirname,'../../src/MCUGLauncher/Ui/index.html')).href;
 const output=path.resolve(__dirname,'../../work/verify/report-ui'); fs.mkdirSync(output,{recursive:true});
 for(const [width,height] of [[960,640],[1200,780],[1920,1080]]){
  await page.setViewportSize({width,height});
  for(const screen of ['home','settings&panel=install','settings&panel=tasks','settings&panel=saves','settings&panel=bookmarks','settings&panel=games','catalog&type=mod','mods','mod','manage','version','options','admin','datasource']){
   await page.goto(base+'?screen='+screen); await page.locator('.app').waitFor();
   const bounds=await page.locator('.app').boundingBox();
   assert.equal(Math.round(bounds.width),width);assert.equal(Math.round(bounds.height),height);
   assert.equal(await page.locator('.titlebar').evaluate(n=>n.scrollWidth<=n.clientWidth),true);
   await page.screenshot({path:path.join(output,`${width}-${screen.replaceAll('&','-').replaceAll('=','-')}.png`)});
  }
 }
 await page.goto(base+'?screen=home&modal=add-server');
 await page.locator('[data-server-name]').fill('Keep input');
 await page.evaluate(()=>window.deliver({type:'serverPing',id:'localhost:25565',online:true,players:1,max:20,ping:5,motd:'Fixture',version:'1.21.1'}));
 assert.equal(await page.locator('[data-server-name]').inputValue(),'Keep input');
 assert.equal(await page.locator('[data-server-name]').evaluate(n=>n===document.activeElement),true);
 console.log('PASS server ping preserves modal input and focus');
 await page.goto(base+'?screen=settings&panel=tasks');
 await page.evaluate(()=>window.deliver({type:'tasks',items:[{id:'fixture',name:'<img src=x>',state:'Running',percent:35,message:'Downloading'}]}));
 assert.equal(await page.locator('[data-task-list] img').count(),0);
 await page.getByRole('button',{name:'取消',exact:true}).click();
 assert.equal(await page.evaluate(()=>window.messages.some(m=>m.type==='taskAction'&&m.id==='fixture'&&m.action==='cancel')),true);
 console.log('PASS task rendering escapes names and emits cancel command');
 await page.goto(base+'?screen=settings&panel=install');
 await page.locator('[data-install-game]').fill('1.21.1');
 await page.getByRole('button',{name:'读取加载器版本',exact:true}).click();
 await page.evaluate(()=>window.deliver({type:'loaderList',version:'1.20.1',loader:'fabric',versions:['wrong']}));
 assert.equal(await page.locator('[data-loader-install]').isDisabled(),true);
 await page.evaluate(()=>window.deliver({type:'loaderList',version:'1.21.1',loader:'fabric',versions:['0.16.0']}));
 await page.locator('[data-loader-install]').click();
 assert.equal(await page.evaluate(()=>window.messages.some(m=>m.type==='installLoader'&&m.version==='1.21.1'&&m.loaderVersion==='0.16.0')),true);
 console.log('PASS loader form rejects stale results and emits selected version');
 assert.deepEqual(errors,[]); await browser.close();
 console.log('ALL_WEB_UI_CHECKS_PASSED (browser with simulated native messages; not live WebView/gameplay)');
})().catch(error=>{console.error(error);process.exit(1);});
