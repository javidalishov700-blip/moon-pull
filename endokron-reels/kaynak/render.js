const { chromium } = require('/opt/node-tools/node_modules/playwright');
const fs=require('fs');const path=require('path');
(async()=>{
  const dir=__dirname;const tl=JSON.parse(fs.readFileSync(process.argv[5]||dir+'/vo.json'));
  const total=parseFloat(process.argv[2]);const fps=30;const only=process.argv[3];
  let html=fs.readFileSync(dir+'/video.html','utf8').replace('__TIMELINE__',JSON.stringify(tl)).replace('__TOTAL__',total);
  fs.writeFileSync(dir+'/_v_'+path.basename(process.argv[4]||'frames')+'.html',html);
  const b=await chromium.launch({executablePath:'/opt/pw-browsers/chromium'});
  const pg=await b.newPage({viewport:{width:1080,height:1920}});
  pg.on('pageerror',e=>console.log('ERR',e.message));
  await pg.goto('file://'+dir+'/_v_'+path.basename(process.argv[4]||'frames')+'.html');await pg.waitForTimeout(800);
  fs.mkdirSync((process.argv[4]||dir+'/frames'),{recursive:true});
  const times= only? only.split(',').map(Number) : [...Array(Math.ceil(total*fps)).keys()].map(i=>i/fps);
  for(let i=0;i<times.length;i++){
    await pg.evaluate(t=>window.render(t),times[i]);
    const f= only? `${dir}/preview_${times[i]}.jpg` : `${process.argv[4]||dir+'/frames'}/f${String(i).padStart(5,'0')}.jpg`;
    await pg.screenshot({path:f,type:'jpeg',quality:92});
    if(i%300===0)console.log(i);
  }
  await b.close();
})();
