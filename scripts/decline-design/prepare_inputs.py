"""Step 0: read the three Deswik .dcf input files and save them as working files for the other scripts.

Usage:
  python prepare_inputs.py "Strings decline.dcf" "North stopes export 261009.dcf" "decline keep away.dcf"

- Strings decline.dcf      : must contain the main decline string with Transformation Details = "Kennet new"
- North stopes export.dcf  : stope solids with Transformation Details = "Group A" / "Group B" (typo "Gourp A" also accepted)
- decline keep away.dcf    : solids the decline must stay 15 m away from
"""
import sys, os, zipfile, tempfile, pickle, glob
import dcf

def read_all(path):
    tmp = tempfile.mkdtemp()
    with zipfile.ZipFile(path) as z:
        for n in z.namelist():
            out = os.path.join(tmp, os.path.basename(n))
            with open(out, 'wb') as f: f.write(z.read(n))
    ents = []
    for f in sorted(glob.glob(os.path.join(tmp, '*'))):
        ents += dcf.entities(f)
    return ents

dec_path, stp_path, ka_path = sys.argv[1:4]
dec = read_all(dec_path)
if not any(e['props'].get('Transformation Details') == 'Kennet new' for e in dec):
    sys.exit('No string with Transformation Details = "Kennet new" found in ' + dec_path)
stp = [e for e in read_all(stp_path) if 'Polyface' in e['type'] and (e['props'].get('Transformation Details') or '')[-1:] in ('A', 'B')]
ka = [e for e in read_all(ka_path) if 'Polyface' in e['type']]
pickle.dump((dec, []), open('ents.pkl', 'wb'))
pickle.dump(stp, open('stp_ab.pkl', 'wb'))
pickle.dump(ka, open('keepaway.pkl', 'wb'))
print(f'decline strings: {len(dec)}  stopes: {len(stp)} (A {sum(e["props"]["Transformation Details"].endswith("A") for e in stp)}, B {sum(e["props"]["Transformation Details"].endswith("B") for e in stp)})  keep-away solids: {len(ka)}')
