#!/usr/bin/env python3
"""variants.json içindeki alternatif cümleleri tek bir sesle üretir (out/variants/)."""
import asyncio, json
from pathlib import Path
import re
import bake  # mkssml dil yaması ve synth() burada
import edge_tts.communicate as _C

# İsteğe bağlı: metni <lang xml:lang='tr-TR'> ile sar (çok dilli seslerin dili yanlış tahmin ettiği cümleler için).
WRAP = {"on": False}
_prev = _C.mkssml
def _wrap(tc, text):
    s = _prev(tc, text)
    if WRAP["on"] and bake.LANG["v"]:
        s = re.sub(r"(<prosody[^>]*>)", r"\1<lang xml:lang='%s'>" % bake.LANG["v"], s, count=1)
        s = s.replace("</prosody>", "</lang></prosody>", 1)
    return s
_C.mkssml = _wrap

ROOT = Path(__file__).resolve().parent

async def main():
    d = json.loads((ROOT / "variants.json").read_text("utf-8"))
    bake.LANG["v"] = d.get("lang")
    out = ROOT / "out" / "variants"; out.mkdir(parents=True, exist_ok=True)
    for ln in d["lines"]:
        dest = out / f"{ln['id']}.mp3"
        WRAP["on"] = bool(ln.get("wrap"))
        for attempt in range(4):
            try:
                await bake.synth(ln["say"], d["voice"], d["rate"], d["pitch"], dest)
                if dest.stat().st_size > 1000:
                    print("ok", ln["id"]); break
            except Exception as e:  # noqa: BLE001
                print("retry", ln["id"], type(e).__name__, e)
            await asyncio.sleep(3 * (attempt + 1))

if __name__ == "__main__":
    asyncio.run(main())
