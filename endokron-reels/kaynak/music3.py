import numpy as np, wave, json, sys
sr=44100; T=float(sys.argv[1]); n=int(sr*T); rng=np.random.default_rng(5)
note=lambda m:440*2**((m-69)/12)
mix=np.zeros(n); bpm=84; beat=60/bpm; bar=4*beat
chords=[[57,60,64,67],[53,57,60,64],[48,52,55,59],[55,59,62,65]]  # Am7 Fmaj7 Cmaj7 G7 (warm, jazzy)
def ep(f,dur,vel):
  tt=np.arange(int(dur*sr))/sr
  env=np.exp(-tt*2.2)*(1-np.exp(-tt*120))
  s=np.sin(2*np.pi*f*tt)+0.35*np.sin(2*np.pi*2*f*tt)*np.exp(-tt*5)+0.12*np.sin(2*np.pi*3*f*tt)*np.exp(-tt*9)
  return vel*env*s*(1+0.08*np.sin(2*np.pi*5*tt))
for k in range(int(T/bar)+1):
  c=chords[k%4]
  for hit,vel in ((0,1.0),(2.5,0.6)):
    s=int((k*bar+hit*beat)*sr)
    if s>=n: continue
    for j,m in enumerate(c):
      x=ep(note(m),2.6,0.05*vel); s2=s+int(j*0.012*sr); e=min(n,s2+len(x)); mix[s2:e]+=x[:e-s2]
    x=ep(note(c[0]-24),2.8,0.13*vel); e=min(n,s+len(x)); mix[s:e]+=x[:e-s]
# soft kick + shaker
for i in range(int(T/beat)):
  s=int(i*beat*sr)
  if i>=4 and i%2==0:
    L=int(0.3*sr); e=min(n,s+L); tt=np.arange(e-s)/sr; mix[s:e]+=0.12*np.exp(-tt*14)*np.sin(2*np.pi*(48+50*np.exp(-tt*35))*tt)
  for off in (0.5,):
    hs=int((i+off)*beat*sr); he=min(n,hs+int(0.07*sr))
    if i>=8 and hs<n:
      nz=rng.standard_normal(he-hs); nz=np.diff(np.r_[0,nz]); mix[hs:he]+=0.012*nz*np.exp(-np.arange(he-hs)/sr*60)
# vinyl crackle
cr=np.zeros(n); idx=rng.integers(0,n,int(T*18)); cr[idx]=rng.uniform(-1,1,len(idx))*0.03; mix+=np.convolve(cr,np.exp(-np.arange(40)/6),'same')
mix+=0.0015*rng.standard_normal(n)
# gentle low-pass (one-pole) for warmth
y=np.zeros(n); a=0.18; acc=0.0
for i in range(0,n,1): pass
from scipy.signal import lfilter
mix=lfilter([a],[1,-(1-a)],mix)
tl=json.load(open(sys.argv[2])); prev=None
for c in tl:
  if c['scene']!=prev and prev is not None:
    s=int((c['start']-0.35)*sr); L=int(0.35*sr); nz=rng.standard_normal(L); nz=lfilter([0.3],[1,-0.7],nz)
    mix[s:s+L]+=0.05*nz*np.sin(np.pi*np.linspace(0,1,L))**2
  prev=c['scene']
f=np.ones(n); fo=int(3*sr); f[-fo:]=np.linspace(1,0,fo); fi=int(1*sr); f[:fi]=np.linspace(0,1,fi); mix*=f
mix=mix/np.max(np.abs(mix))*0.9
with wave.open(sys.argv[3],'wb') as w: w.setnchannels(1);w.setsampwidth(2);w.setframerate(sr);w.writeframes((mix*32767).astype(np.int16).tobytes())
