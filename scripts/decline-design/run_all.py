"""Run the whole design in one go.
Usage:
  python run_all.py "Strings decline.dcf" "North stopes export 261009.dcf" "decline keep away.dcf" "SITE - new design.dcf"
Needs Python 3 with numpy, scipy and lxml.
"""
import sys, subprocess, os, pickle
dec, stp, ka, out = sys.argv[1:5]
def run(*a):
    print('>>', ' '.join(a), flush=True)
    r = subprocess.run([sys.executable, *a])
    if r.returncode: sys.exit(r.returncode)
if os.path.exists('acc_len.json'): os.remove('acc_len.json')
run('prepare_inputs.py', dec, stp, ka)
for _ in range(3):                     # access grades settle after a few passes
    run('design3.py'); run('design3b.py')
import solids
pickle.dump((solids.build(pickle.load(open('keepaway.pkl','rb'))).data, solids.build(pickle.load(open('stp_ab.pkl','rb'))).data), open('solid_pts.pkl','wb'))
run('rar_opt.py')
run('checks.py')
run('write_dcf3.py', out, dec)
print('Done:', out)
