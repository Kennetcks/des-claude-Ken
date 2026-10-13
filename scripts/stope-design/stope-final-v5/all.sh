cd /home/claude/work
bash st_final_v5/run.sh
cd st_final_v5/stp && python3 stopes.py > ../post.log 2>&1 && python3 geom.py >> ../post.log 2>&1 && python3 write_geom.py v5_stopes_geometry.txt >> ../post.log
cd ../cmp && python3 compare.py > ../cmp.log 2>&1
cd /home/claude/work && export CEIL_R=15 HUP=30 HBLIND=26 MODEL=s2 MIN_G=1.5 SPAN_PICK=shape SHAPE_REF=2.6 SHAPE_DRIFT=0.2 ST_FILE=/home/claude/work/st_final_v5/st_strings_moved.csv && python3 st_final_v5/vein/stage_v5vein.py > st_final_v5/vein/stage.log 2>&1
cd st_final_v5/vein && python3 stopes.py > post.log 2>&1 && python3 geom.py >> post.log 2>&1 && python3 write_geom.py v5_vein_stopes_geometry.txt >> post.log
echo ALLDONE >> /home/claude/work/st_final_v5/post.log
