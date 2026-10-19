import json, sys, subprocess, wave, re, numpy as np
voice=sys.argv[1]; outp=sys.argv[2]; scr=sys.argv[3] if len(sys.argv)>3 else 'script3.json'
OUT='/home/user/moon-pull/viral-reels/voice/out/'+voice+'/'
S=json.load(open(scr)); sr=24000
def load(p):
  raw=subprocess.run(['ffmpeg','-loglevel','error','-i',p,'-ac','1','-ar',str(sr),'-f','s16le','-'],capture_output=True).stdout
  return np.frombuffer(raw,dtype=np.int16).astype(np.float32)/32768
norm=lambda s: re.sub(r'[^\wçğıöşüâîû]','',s.lower().replace('İ','i').replace('I','ı'))
chunks=[np.zeros(int(0.25*sr),np.float32)]; t=0.25; tl=[]
for i,s in enumerate(S):
  fn=s.get('file',f'l{i:02d}'); x=load(OUT+fn+'.mp3'); bw=json.load(open(OUT+fn+'.words.json'))
  idx=np.where(np.abs(x)>0.008)[0]; a0=max(idx[0]-240,0); x=x[a0:idx[-1]+int(0.15*sr)]; off=a0/sr
  dur=len(x)/sr; toks=s['say'].split(); words=[]; j=0
  for k,tok in enumerate(toks):
    n=norm(tok); hit=None
    for jj in range(j,min(j+4,len(bw))):
      if norm(bw[jj]['w'])==n or (n and norm(bw[jj]['w']).startswith(n[:3])): hit=jj;break
    if hit is not None:
      ws=t+bw[hit]['o']-off; we=ws+bw[hit]['d']; j=hit+1
    else:
      ws=words[-1]['e'] if words else t; we=ws+0.25
    words.append({'w':{'Ce':'C'}.get(tok,tok),'s':round(max(ws,t),3),'e':round(we,3)})
  gap=0.35 if (i+1<len(S) and S[i+1]['scene']!=s['scene']) else 0.15
  tl.append(dict(s,start=round(t,3),end=round(t+dur,3),words=words)); chunks+=[x,np.zeros(int(gap*sr),np.float32)]; t+=dur+gap
chunks.append(np.zeros(int(3.2*sr),np.float32)); y=np.concatenate(chunks); y=y/np.max(np.abs(y))*0.9
with wave.open(outp,'wb') as w: w.setnchannels(1);w.setsampwidth(2);w.setframerate(sr);w.writeframes((y*32767).astype(np.int16).tobytes())
json.dump(tl,open(outp.replace('.wav','.json'),'w'),ensure_ascii=False,indent=1); print(voice,round(len(y)/sr,2))
