import numpy as np, wave, json, sys, re
from scipy.signal import lfilter
sr=44100; T=float(sys.argv[1]); n=int(sr*T); rng=np.random.default_rng(7)
tl=json.load(open(sys.argv[2])); out=sys.argv[3]
note=lambda m:440*2**((m-69)/12)
mix=np.zeros(n); bpm=112; beat=60/bpm; bar=4*beat
prog=[[60,64,67],[55,59,62],[57,60,64],[53,57,60]]  # C G Am F
def add(x,s):
  s=int(s*sr)
  if s>=n or s<0: return
  e=min(n,s+len(x)); mix[s:e]+=x[:e-s]
def marimba(f,dur,vel):
  tt=np.arange(int(dur*sr))/sr
  return vel*(np.sin(2*np.pi*f*tt)*np.exp(-tt*7)+0.35*np.sin(2*np.pi*4*f*tt)*np.exp(-tt*20)+0.15*np.sin(2*np.pi*10*f*tt)*np.exp(-tt*40))
def pad(f,dur,vel):
  tt=np.arange(int(dur*sr))/sr; env=np.minimum(1,tt/0.3)*np.exp(-np.maximum(0,tt-dur+0.4)*6)
  return vel*env*(np.sin(2*np.pi*f*tt)+0.3*np.sin(2*np.pi*2*f*tt))
for k in range(int(T/bar)+1):
  c=prog[k%4]; t0=k*bar
  for m in c: add(pad(note(m),bar+0.2,0.018),t0)
  add(pad(note(c[0]-12),bar+0.2,0.05),t0)
  pat=[0,2,1,2,0,2,1,2]
  for i in range(8):
    m=c[pat[i]]+12+(12 if i in (3,7) else 0)
    add(marimba(note(m),0.5,0.05 if i%2==0 else 0.035),t0+i*beat/2)
  if k>=1:
    for b in (1,3):
      L=int(0.12*sr); nz=rng.standard_normal(L); nz=lfilter([1,-1],[1],nz)*np.exp(-np.arange(L)/sr*40)
      add(0.05*nz,t0+b*beat)
    for i in range(8):
      L=int(0.04*sr); nz=lfilter([1,-1],[1],rng.standard_normal(L))*np.exp(-np.arange(L)/sr*90); add(0.012*nz,t0+i*beat/2+0.01)
  for b in (0,2):
    L=int(0.25*sr); tt=np.arange(L)/sr; add(0.09*np.exp(-tt*16)*np.sin(2*np.pi*(52+60*np.exp(-tt*30))*tt),t0+b*beat)
mix=lfilter([0.35],[1,-0.65],mix)
# SFX
norm=lambda s: re.sub(r'[^a-zçğıöşü0-9%]','',s.lower().replace('İ','i').replace('I','ı'))
def wt(scene,li,pre):
  L=[x for x in tl if x['scene']==scene][li]
  for w in L['words']:
    if norm(w['w']).startswith(pre): return w['s']
  return L['words'][0]['s']
def popfx(t0,f=880,v=0.12):
  L=int(0.16*sr); tt=np.arange(L)/sr; add(v*np.exp(-tt*28)*np.sin(2*np.pi*(f+700*np.exp(-tt*40))*tt),t0)
def ding(t0):
  L=int(0.6*sr); tt=np.arange(L)/sr; add(0.06*np.exp(-tt*6)*(np.sin(2*np.pi*1320*tt)+0.5*np.sin(2*np.pi*1980*tt)),t0)
def drop(t0):
  L=int(0.35*sr); tt=np.arange(L)/sr; add(0.08*np.exp(-tt*8)*np.sin(2*np.pi*(700-500*tt/0.35)*tt),t0)
def swoosh(t0):
  L=int(0.45*sr); nz=lfilter([0.25],[1,-0.75],rng.standard_normal(L)); add(0.06*nz*np.sin(np.pi*np.linspace(0,1,L))**2,t0-0.22)
prev=None
for c in tl:
  if prev and c['scene']!=prev: swoosh(c['start']-0.25)
  prev=c['scene']
for sc,li,pre,kind in [('rank',0,'güvenil','pop'),('rank',0,'fark','pop'),('rank',1,'kanıt','pop'),('rank',2,'riskl','ding'),('tip1',0,'bir','pop'),('tip1',0,'kaybol','drop'),
  ('tip2',0,'iki','pop'),('tip2',0,'yüzde','ding'),('tip3',0,'matris','pop'),('tip3',0,'ışınl','pop'),('tip4',0,'yarı','drop'),('tip4',0,'ölç','pop'),('outro',0,'uygula','ding'),('outro',0,'kaydet','pop')]:
  t0=wt(sc,li,pre); {'pop':popfx,'ding':ding,'drop':drop}[kind](t0)
f=np.ones(n); fo=int(2*sr); f[-fo:]=np.linspace(1,0,fo); mix*=f
mix=mix/np.max(np.abs(mix))*0.9
with wave.open(out,'wb') as w: w.setnchannels(1);w.setsampwidth(2);w.setframerate(sr);w.writeframes((mix*32767).astype(np.int16).tobytes())
