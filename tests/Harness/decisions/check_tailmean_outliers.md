# check_tailmean_outliers.py

A working script of an earlier wave, kept as provenance because this node's prose cites
it. It is held inside this document rather than beside it as a `.py` file on purpose: a
directory holding a code file is a node of the tree (AGENTS.md §1) and would owe a
`BOOT.md` and an `API.md`, which a folder of historical notes has no business carrying.
It is not run by anything and is not maintained.

```python
import re

def parse_array(path, name):
    text = open(path, encoding='utf-8').read()
    lines = text.splitlines()
    joined = []
    buf = ''
    cont = False
    for l in lines:
        buf = buf + l if cont else l
        if buf.rstrip().endswith('...'):
            buf = buf.rstrip()[:-3]
            cont = True
        else:
            joined.append(buf)
            cont = False
    full = '\n'.join(joined)
    m = re.search(re.escape(name) + r'\s*=\s*\[([^\]]*)\]', full)
    if not m:
        return None
    nums = re.findall(r'[+-]?(?:\d+\.?\d*|\.\d+)(?:[eE][+-]?\d+)?', m.group(1))
    return [float(n) for n in nums]

cases = [
    ('HMX', 14, 28),
    ('inpt', 1, 35),
    ('PSAN02n', 5, 27),
    ('P33', 14, 6),
]

for formulation, flagged, i0 in cases:
    lengths = []
    for k in range(1, {'HMX': 17, 'inpt': 17, 'PSAN02n': 17, 'P33': 17}[formulation]):
        dk = parse_array(f'tests/Fixtures/replicas-lagged/{formulation}/{k}.m.txt', 'Dkarmcat')
        length = len(dk) if dk else 0
        lengths.append((k, length, max(0, length - i0)))
    print(formulation, 'i0=', i0)
    for k, length, adaptive in lengths:
        marker = ' <== flagged' if k == flagged else ''
        print(f'  replica {k}: Dkarmcat len={length}, adaptive rows={adaptive}{marker}')
    adaptive_counts = [a for _, _, a in lengths]
    print('  mean adaptive rows (all):', sum(adaptive_counts) / len(adaptive_counts))
    print()
```
