"""Compile original-resolution Godot textures; never export or back up a PCK."""
import argparse,hashlib,json,re,shutil,struct,subprocess
from pathlib import Path

def digest(path):return hashlib.sha256(path.read_bytes()).hexdigest()
def main():
 p=argparse.ArgumentParser();p.add_argument('--project',type=Path,required=True);p.add_argument('--godot',type=Path,required=True);p.add_argument('--cache',type=Path,required=True);args=p.parse_args()
 root=args.project.resolve();cache=args.cache.resolve();cache.mkdir(parents=True,exist_ok=True)
 images=root/'MaidenSuccubus/images';output=root/'MaidenSuccubus/textures';output.mkdir(parents=True,exist_ok=True)
 # PNG IHDR and JPEG dimensions are read with Pillow only during asset compilation.
 from PIL import Image
 manifest_path=output/'manifest.json';old=json.loads(manifest_path.read_text()) if manifest_path.exists() else {}
 manifest={};needs=[]
 for source in sorted(images.rglob('*')):
  if source.suffix.lower() not in ['.png','.jpg','.jpeg']:continue
  rel=source.relative_to(images).as_posix();compiled=output/(rel+'.ctex');sha=digest(source)
  with Image.open(source) as image:w,h=image.size
  # Keep exact HUD pixels and tiny symbols lossless; BC7 stores alpha and original dimensions.
  gpu=w*h>=65536 and not rel.startswith('ui/core/')
  previous=old.get(rel,{})
  if previous.get('source_sha256')==sha and previous.get('gpu')==gpu and compiled.exists() and previous.get('texture_sha256')==digest(compiled):
   manifest[rel]=previous;continue
  staged=cache/'images'/rel;staged.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(source,staged)
  settings='[remap]\nimporter="texture"\ntype="CompressedTexture2D"\n[params]\ncompress/mode='+('2' if gpu else '0')+'\ncompress/high_quality=true\nmipmaps/generate=false\nprocess/fix_alpha_border=true\nprocess/premult_alpha=false\nprocess/size_limit=0\ndetect_3d/compress_to=0\n'
  Path(str(staged)+'.import').write_text(settings,encoding='utf-8')
  needs.append((rel,staged,compiled,sha,w,h,gpu))
 (cache/'project.godot').write_text('config_version=5\n[application]\nconfig/name="HibikiAmane Texture Compiler"\n[rendering]\ntextures/vram_compression/import_s3tc_bptc=true\n',encoding='utf-8')
 if needs:
  with (cache/'import.log').open('w',encoding='utf-8') as log:
   subprocess.run([str(args.godot.resolve()),'--headless','--editor','--path',str(cache),'--import','--log-file',str(cache/'godot-import.log')],stdout=log,stderr=subprocess.STDOUT,check=True,timeout=900)
 for rel,staged,compiled,sha,w,h,gpu in needs:
  remap=Path(str(staged)+'.import').read_text();target=re.search(r'path(?:\.bptc)?="res://([^\"]+\.ctex)"',remap)
  if not target:raise RuntimeError('No native import: '+rel)
  data=(cache/target[1]).read_bytes()
  if data[:4]!=b'GST2' or struct.unpack_from('<II',data,8)!=(w,h):raise RuntimeError('Texture dimensions changed: '+rel)
  fmt=struct.unpack_from('<I',data,48)[0]
  if gpu and fmt!=22:raise RuntimeError('Expected BC7 RGBA: '+rel+' format='+str(fmt))
  compiled.parent.mkdir(parents=True,exist_ok=True);compiled.write_bytes(data)
  gpu_bytes=((w+3)//4)*((h+3)//4)*16 if gpu else w*h*(3 if fmt==4 else 4)
  manifest[rel]={'source_sha256':sha,'texture_sha256':digest(compiled),'width':w,'height':h,'gpu':gpu,'format':fmt,'gpu_bytes':gpu_bytes,'file_bytes':len(data)}
 for rel,asset in manifest.items():
  native_path="res://MaidenSuccubus/textures/"+rel+".ctex"
  remap='[remap]\nimporter="texture"\ntype="CompressedTexture2D"\npath="'+native_path+'"\nmetadata={"vram_texture": '+str(asset['gpu']).lower()+'}\n'
  Path(str(images/rel)+'.import').write_text(remap,encoding='utf-8')
 manifest_path.write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
 summary={'assets':len(manifest),'compiled_now':len(needs),'rgba_bytes':sum(v['width']*v['height']*4 for v in manifest.values()),'texture_bytes':sum(v['gpu_bytes'] for v in manifest.values()),'packed_bytes':sum(v['file_bytes'] for v in manifest.values())}
 (cache/'summary.json').write_text(json.dumps(summary,indent=2));print(json.dumps(summary))
if __name__=='__main__':main()
