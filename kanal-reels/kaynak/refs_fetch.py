"""Kaynak doğrulama: Europe PMC'den künye, özet ve açık erişimli tam metinden
konuyla ilgili paragrafları iş günlüğüne yazar (dosya commit etmez)."""
import json, re, sys, time, urllib.parse, urllib.request, html
from pathlib import Path

BASE = "https://www.ebi.ac.uk/europepmc/webservices/rest/"
KEYS = ["rubber dam", "isolation", "orifice", "barrier", "coronal seal", "coronal leakage",
        "microleakage", "sealer", "gutta", "pulp chamber", "cusp", "marginal ridge",
        "as soon as", "delay", "days", "temporary", "interim", "direct", "composite",
        "incremental", "bulk", "survival", "fracture", "adhesive", "bond", "hypochlorite",
        "occlus", "recommend"]

def get(url, tries=3):
    for i in range(tries):
        try:
            req = urllib.request.Request(url, headers={"User-Agent": "refs-check/1.0"})
            with urllib.request.urlopen(req, timeout=60) as r:
                return r.read().decode("utf-8", "replace")
        except Exception as e:
            print(f"  ! {e} ({url[:90]})")
            time.sleep(3 * (i + 1))
    return ""

def paras(xml):
    body = xml.split("<body", 1)[-1]
    out = []
    for m in re.finditer(r"<(p|title)[^>]*>(.*?)</\1>", body, re.S):
        t = html.unescape(re.sub(r"<[^>]+>", "", m.group(2)))
        t = re.sub(r"\s+", " ", t).strip()
        if t:
            out.append((m.group(1), t))
    return out

def main():
    qs = json.loads(Path(__file__).with_name("refs_queries.json").read_text())["queries"]
    for q in qs:
        n = q.get("n", 1)
        url = BASE + "search?" + urllib.parse.urlencode(
            {"query": q["q"], "format": "json", "resultType": "core", "pageSize": n})
        print(f"\n######## {q['name']} :: {q['q']}")
        txt = get(url)
        if not txt:
            continue
        res = json.loads(txt).get("resultList", {}).get("result", [])
        if not res:
            print("  (no results)")
        for r in res[:n]:
            ji = r.get("journalInfo", {})
            print(f"== {r.get('title')}")
            print(f"   {r.get('authorString')} | {ji.get('journal', {}).get('isoabbreviation') or ji.get('journal', {}).get('title')} "
                  f"{ji.get('yearOfPublication') or r.get('pubYear')};{ji.get('volume')}({ji.get('issue')}):{r.get('pageInfo')} "
                  f"| doi {r.get('doi')} | pmid {r.get('pmid')} | pmcid {r.get('pmcid')} | OA {r.get('isOpenAccess')}")
            ab = re.sub(r"<[^>]+>", "", r.get("abstractText", "") or "")
            if not q.get("noabs"): print("   ABSTRACT: " + re.sub(r"\s+", " ", html.unescape(ab)).strip()[:2200])
            if q.get("full", True) and r.get("pmcid"):
                xml = get(BASE + r["pmcid"] + "/fullTextXML")
                ps = paras(xml)
                keys = q.get("keys", KEYS)
                print(f"   FULLTEXT paragraphs: {len(ps)}")
                shown = 0
                for kind, t in ps:
                    low = t.lower()
                    hits = [k for k in keys if k in low]
                    if kind == "p" and hits and shown < q.get("maxp", 999):
                        shown += 1
                        print(f"   [{','.join(hits)}] {t[:1100]}")
                refl = re.findall(r"<ref[ >].*?</ref>", xml, re.S)
                for i in q.get("refs", []):
                    if 0 < i <= len(refl):
                        r_ = html.unescape(re.sub(r"<[^>]+>", " ", refl[i - 1]))
                        print(f"   REF {i}: " + re.sub(r"\s+", " ", r_).strip()[:400])
        time.sleep(1)

if __name__ == "__main__":
    main()
