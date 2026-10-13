cd /home/claude/work
export CEIL_R=15 HUP=30 HBLIND=26 MODEL=s2 MIN_G=1.5 SPAN_PICK=shape SHAPE_REF=2.6 SHAPE_DRIFT=0.2 ST_FILE=/home/claude/work/st_final_v5/st_strings_moved.csv
python3 st_stage1v5.py > st_final_v5/stage1.log 2>&1 && python3 st_stage2v5.py > st_final_v5/stage2.log 2>&1 && python3 intersections.py st_final_v5/kept_stx.pkl st_final_v5/kept_stxi.pkl > st_final_v5/inter.log 2>&1; echo DONE $? >> st_final_v5/inter.log
