with open("a.txt", "w") as f:
    f.write("1" * 10**7)  # 10 מיליון תווים
import zipfile

with zipfile.ZipFile("a.zip", "w", zipfile.ZIP_DEFLATED) as zipf:
    zipf.write("a.txt")


import zipfile

with zipfile.ZipFile("a.zip", "r") as zipf:
    for info in zipf.infolist():
        ratio = info.file_size / info.compress_size if info.compress_size else 0
        print(f"{info.filename}: ratio = {ratio:.2f}")

        if ratio > 1000:
            print("⚠️ חשד לפצצת ZIP!")
            print(f"size {info.file_size} compress {info.compress_size}")


