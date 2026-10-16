#!/usr/bin/env python3
"""Wikimedia Commons'tan ağız içi tarayıcı fotoğrafları (lisans bilgisiyle) indirir.
Bulut oturumu Commons'a erişemediği için GitHub Actions üzerinde çalışır."""
import json, urllib.parse, urllib.request
from pathlib import Path
OUT = Path(__file__).resolve().parent / "commons"; OUT.mkdir(exist_ok=True)
UA = {"User-Agent": "endokron-reels/1.0 (github.com/javidalishov700-blip/moon-pull)"}
def get(url):
    return urllib.request.urlopen(urllib.request.Request(url, headers=UA), timeout=60).read()
meta, seen = [], set()
for q in ["intraoral scanner", "intra-oral scanner", "dental intraoral scan", "iTero scanner", "TRIOS scanner", "dental 3D scanner", "digital dental impression"]:
    api = "https://commons.wikimedia.org/w/api.php?" + urllib.parse.urlencode({
        "action": "query", "format": "json", "generator": "search", "gsrsearch": q, "gsrnamespace": 6,
        "gsrlimit": 10, "prop": "imageinfo", "iiprop": "url|extmetadata|mime", "iiurlwidth": 1080})
    try:
        pages = json.loads(get(api)).get("query", {}).get("pages", {})
    except Exception as e:  # noqa: BLE001
        print("search fail", q, e); continue
    for p in pages.values():
        t = p["title"]
        if t in seen or not p.get("imageinfo"): continue
        ii = p["imageinfo"][0]
        if ii.get("mime") not in ("image/jpeg", "image/png"): continue
        seen.add(t); em = ii.get("extmetadata", {})
        name = f"c{len(meta):02d}.jpg"
        try:
            (OUT / name).write_bytes(get(ii.get("thumburl") or ii["url"]))
        except Exception as e:  # noqa: BLE001
            print("dl fail", t, e); continue
        meta.append({"file": name, "title": t, "query": q, "page": ii.get("descriptionurl"),
                     "license": em.get("LicenseShortName", {}).get("value"),
                     "artist": em.get("Artist", {}).get("value"), "desc": em.get("ImageDescription", {}).get("value", "")[:300]})
        print("ok", name, t)
        if len(meta) >= 30: break
    if len(meta) >= 30: break
(OUT / "meta.json").write_text(json.dumps(meta, ensure_ascii=False, indent=1), "utf-8")
