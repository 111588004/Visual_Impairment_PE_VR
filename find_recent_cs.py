import os
import glob

files = glob.glob('Assets/**/*.cs', recursive=True)
files.sort(key=os.path.getctime, reverse=True)
print("Recently created files:")
for f in files[:10]:
    print(f)
