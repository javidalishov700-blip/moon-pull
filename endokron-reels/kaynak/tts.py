import json, sys, wave, numpy as np, sherpa_onnx
B='/tmp/claude-0/-home-user-moon-pull/73b25e77-1809-5fb5-985e-d07bb6c1775f/scratchpad/'
voice=sys.argv[1]; ls=float(sys.argv[2]); out=sys.argv[3]
d=B+'tts/vits-piper-tr_TR-'+voice+'/'
import glob
cfg=sherpa_onnx.OfflineTtsConfig(model=sherpa_onnx.OfflineTtsModelConfig(vits=sherpa_onnx.OfflineTtsVitsModelConfig(
  model=glob.glob(d+'*.onnx')[0], tokens=d+'tokens.txt', data_dir=d+'espeak-ng-data', length_scale=ls),num_threads=4))
tts=sherpa_onnx.OfflineTts(cfg)
S=json.load(open(B+'build/script.json'))
sr=None; chunks=[]; t=0.6; timeline=[]
lead=np.zeros(int(0.6*22050),dtype=np.float32)
for i,s in enumerate(S):
  a=tts.generate(s['tts'],sid=0,speed=1.0); sr=a.sample_rate
  x=np.array(a.samples,dtype=np.float32)
  # trim silence
  idx=np.where(np.abs(x)>0.01)[0]; x=x[max(idx[0]-200,0):idx[-1]+2600]
  dur=len(x)/sr
  gap=0.25
  if i+1<len(S) and S[i+1]['scene']!=s['scene']: gap=0.6
  timeline.append(dict(s,start=round(t,3),end=round(t+dur,3)))
  chunks.append(x); chunks.append(np.zeros(int(gap*sr),dtype=np.float32)); t+=dur+gap
y=np.concatenate([np.zeros(int(0.6*sr),dtype=np.float32)]+chunks+[np.zeros(int(1.5*sr),dtype=np.float32)])
y=y/np.max(np.abs(y))*0.9
with wave.open(out,'wb') as w:
  w.setnchannels(1);w.setsampwidth(2);w.setframerate(sr);w.writeframes((y*32767).astype(np.int16).tobytes())
json.dump(timeline,open(out.replace('.wav','.json'),'w'),ensure_ascii=False,indent=1)
print(voice,'total',len(y)/sr)
