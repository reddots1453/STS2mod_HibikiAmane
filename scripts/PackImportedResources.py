"""Write only the project's current PCK. No full-pack copy or deployment backup."""
import argparse,hashlib,json,struct
from pathlib import Path
def main():
 p=argparse.ArgumentParser();p.add_argument('--project',type=Path,required=True);args=p.parse_args();root=args.project.resolve()
 content=root/'MaidenSuccubus';manifest=json.loads((content/'textures/manifest.json').read_text())
 files=[]
 for file in sorted(content.rglob('*')):
  if not file.is_file():continue
  rel=file.relative_to(content).as_posix()
  # Native .ctex replaces original PNG/JPEG in releases. Sources remain in the project.
  if rel.startswith('images/') and rel[7:] in manifest:continue
  if file.suffix=='.tmp' or '.godot' in file.parts:continue
  files.append(file)
 target=root/'MaidenSuccubus.pck';base=112
 # PCK v3 data offsets and directory format match the installed game pack.
 header=b'GDPC'+struct.pack('<5I',3,4,5,1,2)+struct.pack('<QQ',base,0)+bytes(72)
 with target.open('wb+') as f:
  f.write(header);entries=[]
  for path in files:
   data=path.read_bytes();name=path.relative_to(root).as_posix();f.write(bytes(-f.tell()%16));offset=f.tell()-base;f.write(data)
   entries.append((name,offset,len(data),hashlib.md5(data).digest()))
  f.write(bytes(-f.tell()%16));directory=f.tell();f.write(struct.pack('<I',len(entries)))
  for name,offset,size,digest in entries:
   encoded=name.encode()+b'\0';encoded+=bytes(-len(encoded)%4)
   f.write(struct.pack('<I',len(encoded))+encoded+struct.pack('<QQ',offset,size)+digest+struct.pack('<I',0))
  f.seek(32);f.write(struct.pack('<Q',directory))
 report={'pck':str(target),'entries':len(entries),'bytes':target.stat().st_size,'sha256':hashlib.sha256(target.read_bytes()).hexdigest(),'imported_textures':len(manifest)}
 print(json.dumps(report))
if __name__=='__main__':main()
