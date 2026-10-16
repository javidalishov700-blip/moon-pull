#!/usr/bin/env python3
"""Endokron Reels seslendirmesi — Microsoft Edge neural sesleri (edge-tts).

Bulut oturumu Microsoft ses servisine bağlanamadığı için bu betik GitHub
Actions üzerinde çalışır ve sonucu dala geri commit eder (meditation
reposundaki ne-olardi-voice yaklaşımıyla aynı).
lines.json -> out/<ses>/<id>.mp3 + <id>.words.json (kelime zamanları, saniye)
"""
import asyncio, json, sys
from pathlib import Path
import edge_tts

ROOT = Path(__file__).resolve().parent
LOG = []

def log(*a):
    line = " ".join(str(x) for x in a)
    print(line, flush=True); LOG.append(line)

async def synth(text, voice, rate, pitch, dest):
    try:
        c = edge_tts.Communicate(text, voice, rate=rate, pitch=pitch, boundary="WordBoundary")
    except TypeError:
        c = edge_tts.Communicate(text, voice, rate=rate, pitch=pitch)
    words = []
    with open(dest, "wb") as f:
        async for ch in c.stream():
            if ch["type"] == "audio":
                f.write(ch["data"])
            elif ch["type"] == "WordBoundary":
                words.append({"o": ch["offset"] / 1e7, "d": ch["duration"] / 1e7, "w": ch["text"]})
    Path(str(dest)[:-4] + ".words.json").write_text(json.dumps(words, ensure_ascii=False), "utf-8")

async def main():
    d = json.loads((ROOT / "lines.json").read_text("utf-8"))
    for voice in d["voices"]:
        out = ROOT / "out" / voice; out.mkdir(parents=True, exist_ok=True)
        for ln in d["lines"]:
            dest = out / f"{ln['id']}.mp3"
            for attempt in range(4):
                try:
                    await synth(ln["say"], voice, d["rate"], d["pitch"], dest)
                    if dest.stat().st_size > 1000:
                        log("ok", voice, ln["id"], dest.stat().st_size); break
                    log("empty", voice, ln["id"])
                except Exception as e:  # noqa: BLE001
                    log("retry", voice, ln["id"], type(e).__name__, e)
                await asyncio.sleep(3 * (attempt + 1))
            else:
                log("FAILED", voice, ln["id"])
    (ROOT / "out" / "log.txt").write_text("\n".join(LOG), "utf-8")

if __name__ == "__main__":
    asyncio.run(main())
