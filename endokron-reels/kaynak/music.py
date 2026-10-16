import numpy as np, wave, json
import sys
sr=44100; T=float(sys.argv[1]); n=int(sr*T); t=np.arange(n)/sr
rng=np.random.default_rng(3)
def note(f): return 440*2**((f-69)/12)
mix=np.zeros(n)
# pad: Am F C G, 4 beats each at 96bpm
bpm=96; beat=60/bpm; bar=4*beat
chords=[[57,60,64,69],[53,57,60,65],[48,55,60,64],[55,59,62,67]]
for k in range(int(T/bar)+1):
  c=chords[k%4]; s=int(k*bar*sr); e=min(n,int((k+1)*bar*sr)+int(0.8*sr))
  if s>=n: break
  tt=np.arange(e-s)/sr; env=np.minimum(1,tt/0.6)*np.exp(-np.maximum(0,tt-bar)*3)
  for m in c:
    for d in (-0.08,0.08):
      mix[s:e]+=0.05*env*np.sin(2*np.pi*note(m+d)*tt)*(0.7+0.3*np.sin(2*np.pi*0.3*tt))
  # bass
  mix[s:e]+=0.10*env*np.sin(2*np.pi*note(c[0]-12)*tt)
# arpeggio pluck (8ths)
for i in range(int(T/(beat/2))):
  s=int(i*beat/2*sr); c=chords[int(i*beat/2/bar)%4]; m=c[[0,2,1,3,2,1,3,2][i%8]]+12
  L=int(0.35*sr); e=min(n,s+L); tt=np.arange(e-s)/sr
  mix[s:e]+=0.035*np.exp(-tt*9)*np.sin(2*np.pi*note(m)*tt)*(1 if i>8 else i/8)
# soft kick + hat
for i in range(int(T/beat)):
  s=int(i*beat*sr); L=int(0.25*sr); e=min(n,s+L); tt=np.arange(e-s)/sr
  if i>=4: mix[s:e]+=0.16*np.exp(-tt*18)*np.sin(2*np.pi*(50+80*np.exp(-tt*30))*tt)
  hs=s+int(beat/2*sr); he=min(n,hs+int(0.05*sr))
  if i>=8 and hs<n: mix[hs:he]+=0.015*rng.standard_normal(he-hs)*np.exp(-np.arange(he-hs)/sr*80)
# whoosh at scene changes
tl=json.load(open(sys.argv[2])); starts=[]; prev=None
for c in tl:
  if c['scene']!=prev: starts.append(c['start']-0.35); prev=c['scene']
for st in starts[1:]:
  s=int((st-0.25)*sr); L=int(0.7*sr); e=min(n,s+L)
  noise=rng.standard_normal(e-s); 
  # crude bandpass sweep via moving average differences
  k=np.linspace(40,4,e-s).astype(int); out=np.zeros(e-s); cs=np.cumsum(np.r_[0,noise])
  idx=np.arange(e-s); lo=np.maximum(0,idx-k); out=(cs[idx+1]-cs[lo])/np.maximum(1,idx+1-lo)
  env=np.sin(np.pi*np.linspace(0,1,e-s))**2
  mix[s:e]+=0.35*out*env
# pops for big reveals
for st in [tl[1]['start']+0.6, tl[7]['start']+2.0]:
  s=int(st*sr); L=int(0.18*sr); tt=np.arange(L)/sr
  mix[s:s+L]+=0.12*np.exp(-tt*25)*np.sin(2*np.pi*(900-500*tt/0.18)*tt)
fade=np.ones(n); fo=int(2.5*sr); fade[-fo:]=np.linspace(1,0,fo); fi=int(0.8*sr); fade[:fi]=np.linspace(0,1,fi)
mix*=fade; mix/=np.max(np.abs(mix)); mix*=0.9
with wave.open(sys.argv[3],'wb') as w:
  w.setnchannels(1);w.setsampwidth(2);w.setframerate(sr);w.writeframes((mix*32767).astype(np.int16).tobytes())
