SITE - Decline and level design scripts (IS design trial, October 2026)

What they do
  Rebuild the north area decline and levels around the Group A / Group B stopes, following
  the design rules in "SITE - Decline and level design - Lessons learnt". Output is a Deswik .dcf
  with attributes (LEVEL, ACT_TYPE, LODE = GROUP A / GROUP B) on every string.

What you need
  - Python 3 with numpy, scipy and lxml
  - Three Deswik files:
      1. Decline strings file. The main decline string must have Transformation Details = "Kennet new".
         The first 300 m of it is kept; everything below is redesigned.
      2. Stopes export. Each stope solid must have Transformation Details = "Group A" or "Group B".
      3. Keep-away solids. The decline is kept at least 15 m from these.

How to run (all files in one folder)
  python run_all.py "Strings decline.dcf" "North stopes export 261009.dcf" "decline keep away.dcf" "SITE - new design.dcf"

  run_all.py calls, in order:
    prepare_inputs.py  reads the three .dcf files
    design3.py         decline: 1:7 stacked loops (22 m radius), timed to the Group A levels,
                       moved to keep the standoffs. Settings via environment variables:
                       DYT (north shift over full depth, default 50), DRL (west step per level, default -10),
                       XMIN (most westerly loop position, default 8105). These are specific to the north area.
    design3b.py        top ramp; one ACC per group per level (perpendicular 10 m, then turns),
                       ISP, ore drives. Run 3 times so access grades settle.
    rar_opt.py         places each RAR with the most clearance (target 10 m or more)
    checks.py          prints the rule checks (standoffs, take-offs, clearances)
    write_dcf3.py      writes the new .dcf using the decline strings file as the template
  dcf.py and solids.py are helper modules.

Important
  - These are working tools written for this trial, not validated software. Some settings
    (waypoints for the top ramp, drift values, level offsets) are specific to the north area and
    need changing for another area.
  - Always open the result in Deswik.CAD and review it. The design is a concept for engineering
    review, not an approved design.
  - Tested: running the full set on the October 2026 inputs reproduces
    "SITE - IS design trial - Group A and B v4.dcf" (142 strings, same check results).
  - Prepared with AI assistance (Claude).
