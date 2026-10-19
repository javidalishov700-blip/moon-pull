src=open('laser.html').read()
head=src[:src.index('<body>')]
helpers=src[src.index('const TL='):src.index('// ================= HOOK')]
subs=src[src.index('// ================= subtitles'):src.index("  const S=k=>t-SC[k].start, ph=t*9;")+len("  const S=k=>t-SC[k].start, ph=t*9;")]
parts=open('led_parts.txt').read().split('@@SPLIT@@')
open('led.html','w').write(head+parts[0]+helpers+parts[1]+subs+parts[2])
