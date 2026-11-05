from pathlib import Path
import csv, sys
comparison=Path(sys.argv[1]) if len(sys.argv)>1 else Path(__file__).parent/'comparison'
graphs=comparison.parent/'graphs'; graphs.mkdir(exist_ok=True)
def rows(version,name):
    with (comparison/version/'results'/name).open(encoding='utf-8-sig') as f: return list(csv.DictReader(f))
def svg(title, xlabel, ylabel, series, xmax, ymax, filename, bars=False):
    W,H,L,T,PW,PH=960,520,85,65,820,360
    colors=['#2463eb','#d24a27']
    parts=[f'<svg xmlns="http://www.w3.org/2000/svg" width="960" height="520" viewBox="0 0 960 520"><rect width="960" height="520" fill="white"/><g font-family="Arial" fill="#172033"><text x="85" y="30" font-size="21">{title}</text>']
    for i in range(6):
        y=T+PH-i*PH/5; v=ymax*i/5
        parts += [f'<path d="M {L} {y} H {L+PW}" stroke="#e2e8f0"/><text x="{L-10}" y="{y+5}" text-anchor="end" font-size="12">{v:.3g}</text>']
        x=L+i*PW/5
        parts += [f'<text x="{x}" y="{T+PH+22}" text-anchor="middle" font-size="12">{xmax*i/5:.0f}</text>']
    parts += [f'<path d="M {L} {T} V {T+PH} H {L+PW}" fill="none" stroke="#172033"/>',f'<text x="495" y="480" text-anchor="middle" font-size="15">{xlabel}</text>',f'<text transform="translate(23,245) rotate(-90)" text-anchor="middle" font-size="15">{ylabel}</text>']
    for j,(name,values) in enumerate(series):
        color=colors[j]
        parts += [f'<rect x="{L+j*170}" y="493" width="16" height="5" fill="{color}"/><text x="{L+23+j*170}" y="501" font-size="13">{name}</text>']
        if bars:
            width=PW*5/xmax/2
            for x,v in values:
                px=L+x/xmax*PW+j*width; py=T+PH-v/ymax*PH
                parts += [f'<rect x="{px}" y="{py}" width="{width-1}" height="{T+PH-py}" fill="{color}"/>']
        else:
            points=' '.join(f'{L+x/xmax*PW},{T+PH-y/ymax*PH}' for x,y in values)
            parts += [f'<polyline points="{points}" stroke="{color}" stroke-width="3" fill="none"/>']
    parts+=['</g></svg>']
    (graphs/filename).write_text(''.join(parts),encoding='utf-8')

perf=[]; hist=[]
for v in ['v0.1','v0.2']:
    perf.append((v,[(float(r['bytes']),float(r['avg_ms'])) for r in rows(v,'performance-summary.csv')]))
    counts=[0]*20
    for r in rows(v,'avalanche-histogram.csv'):
        if r['length']=='all': counts[min(19,int(float(r['bit_percent'])//5))]+=int(r['count'])
    assert sum(counts)==100000
    hist.append((v,[(i*5,n) for i,n in enumerate(counts)]))
svg('Spartos palyginimas','Įvesties dydis (baitai)','Vidutinis laikas vienai maišai (ms)',perf,80000,max(y for _,s in perf for x,y in s)*1.1,'performance.svg')
svg('Lavinos efektas: 100 000 porų kiekvienai versijai','Pasikeitę maišos bitai (%) – 5 p. p. intervalai','Porų skaičius',hist,100,max(y for _,s in hist for x,y in s)*1.12,'avalanche.svg',True)

