import numpy as np, wave, json, sys
from scipy.signal import lfilter, butter
sr=44100; T=float(sys.argv[1]); n=int(sr*T); rng=np.random.default_rng(11)
tl=json.load(open(sys.argv[2])); out=sys.argv[3]
note=lambda m:440*2**((m-69)/12)
mix=np.zeros(n); bpm=88; beat=60/bpm; bar=4*beat
# Am9 - Fmaj7 - Cmaj7 - G6 (calm, mature)
prog=[[57,60,64,67,71],[53,57,60,64,67],[48,52,55,59,64],[55,59,62,64,67]]
def add(x,s,vol=1.0):
  s=int(s*sr)
  if s>=n or s<0: return
  e=min(n,s+len(x)); mix[s:e]+=vol*x[:e-s]
def piano(f,dur,vel):
  tt=np.arange(int(dur*sr))/sr
  env=np.exp(-tt*2.6)*(1-np.exp(-tt*300))
  x=np.zeros_like(tt)
  for k,a in [(1,1.0),(2,0.42),(3,0.18),(4,0.08),(5,0.04)]:
    x+=a*np.sin(2*np.pi*f*k*(1+0.0004*k*k)*tt)*np.exp(-tt*(1.5*k))
  return vel*env*x
def pad(f,dur,vel):
  tt=np.arange(int(dur*sr))/sr; env=np.minimum(1,tt/1.2)*np.minimum(1,np.maximum(0,(dur-tt))/0.8)
  x=sum(np.sin(2*np.pi*f*(1+d)*tt) for d in (-0.003,0,0.003))/3
  return vel*env*x
for k in range(int(T/bar)+1):
  c=prog[k%4]; t0=k*bar
  for m in c[:4]: add(pad(note(m),bar+0.9,0.022),t0)
  add(pad(note(c[0]-12),bar+0.9,0.05),t0)
  # gentle broken-chord piano: 1, &2, 3, &4
  seq=[(0,c[0]+12),(1.5,c[2]+12),(2,c[4]+12 if len(c)>4 else c[3]+12),(3.5,c[1]+24)]
  for b,m in seq: add(piano(note(m),2.4,0.06),t0+b*beat+0.004*rng.standard_normal())
  if k>=1:
    L=int(0.3*sr); tt=np.arange(L)/sr; kick=np.exp(-tt*12)*np.sin(2*np.pi*(48+40*np.exp(-tt*25))*tt)
    add(kick,t0,0.07); add(kick,t0+2*beat,0.05)
    for i in range(8):
      L=int(0.05*sr); nz=lfilter([1,-1],[1],rng.standard_normal(L))*np.exp(-np.arange(L)/sr*70); add(nz,t0+i*beat/2+0.012,0.006 if i%2 else 0.004)
# soft lowpass for warmth
b,a=butter(2,5200/(sr/2)); mix=lfilter(b,a,mix)
# simple reverb (multi-tap)
rev=np.zeros(n)
for d,g in [(0.031,0.32),(0.047,0.27),(0.071,0.22),(0.113,0.16),(0.173,0.11)]:
  k=int(d*sr); rev[k:]+=g*mix[:-k]
mix=mix+0.6*rev
# very subtle transition swooshes
prev=None
for c in tl:
  if prev and c['scene']!=prev:
    L=int(0.6*sr); nz=lfilter([0.2],[1,-0.85],rng.standard_normal(L)); add(nz*np.sin(np.pi*np.linspace(0,1,L))**2,c['start']-0.55,0.035)
  prev=c['scene']
f=np.ones(n); fi=int(0.4*sr); f[:fi]=np.linspace(0.3,1,fi); fo=int(2.2*sr); f[-fo:]=np.linspace(1,0,fo); mix*=f
mix=mix/np.max(np.abs(mix))*0.9
with wave.open(out,'wb') as w: w.setnchannels(1);w.setsampwidth(2);w.setframerate(sr);w.writeframes((mix*32767).astype(np.int16).tobytes())
