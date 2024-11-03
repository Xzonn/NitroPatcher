import os
import re

EXTENSIONS = {".cs", ".xaml"}
PATTERN = re.compile(r'(版本：|Version\("\|(?:version|Version|versionName)=")(\d+\.\d+\.\d+)(?:-[0-9a-f]{7})?')

version = os.getenv("XZ_VERSION", "").removeprefix("v")
git = not version
if git:
  with os.popen("git rev-parse HEAD") as reader:
    version = reader.read().strip()[:7]

for root, dirs, files in os.walk("."):
  for file_name in files:
    ext = os.path.splitext(file_name)[1]
    if ext not in EXTENSIONS:
      continue

    file_path = os.path.join(root, file_name)
    with open(file_path, "r", -1, "utf8") as reader:
      text = reader.read()

    text = PATTERN.sub(rf"\1\2-{version}" if git else rf"\1{version}", text)

    with open(file_path, "w", -1, "utf8", None, "\n") as writer:
      writer.write(text)
