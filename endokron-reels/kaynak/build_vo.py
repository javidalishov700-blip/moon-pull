import json, sys, subprocess, wave, numpy as np
voice=sys.argv[1]; outp=sys.argv[2]
OUT='/home/user/moon-pull/endokron-reels/voice/out/'+voice+'/'
S=json.load(open('script.json')); sr=24000
def load(p):
  raw=subprocess.run(['ffmpeg','-loglevel','error','-i',p,'-ac','1','-ar',str(sr),'-f','s16le','-'],capture_output=True).stdout
  return np.frombuffer(raw,dtype=np.int16).astype(np.float32)/32768
chunks=[np.zeros(int(0.6*sr),np.float32)]; t=0.6; tl=[]
for i,s in enumerate(S):
  x=load(OUT+f'l{i:02d}.mp3'); idx=np.where(np.abs(x)>0.01)[0]; x=x[max(idx[0]-240,0):idx[-1]+int(0.12*sr)]
  dur=len(x)/sr; gap=0.6 if (i+1<len(S) and S[i+1]['scene']!=s['scene']) else 0.22
  tl.append(dict(s,start=round(t,3),end=round(t+dur,3))); chunks+= [x,np.zeros(int(gap*sr),np.float32)]; t+=dur+gap
chunks.append(np.zeros(int(1.6*sr),np.float32)); y=np.concatenate(chunks); y=y/np.max(np.abs(y))*0.9
with wave.open(outp,'wb') as w: w.setnchannels(1);w.setsampwidth(2);w.setframerate(sr);w.writeframes((y*32767).astype(np.int16).tobytes())
json.dump(tl,open(outp.replace('.wav','.json'),'w'),ensure_ascii=False,indent=1); print(voice,len(y)/sr)
