#!/usr/bin/env python3
"""variants.json içindeki alternatif cümleleri tek bir sesle üretir (out/variants/)."""
import asyncio, json
from pathlib import Path
import bake  # mkssml dil yaması ve synth() burada

ROOT = Path(__file__).resolve().parent

async def main():
    d = json.loads((ROOT / "variants.json").read_text("utf-8"))
    bake.LANG["v"] = d.get("lang")
    out = ROOT / "out" / "variants"; out.mkdir(parents=True, exist_ok=True)
    for ln in d["lines"]:
        dest = out / f"{ln['id']}.mp3"
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
