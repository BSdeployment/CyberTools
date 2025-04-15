import zipfile
import os
import tkinter as tk
from tkinter import filedialog, scrolledtext, messagebox

# הגדרות סף
MAX_COMPRESSION_RATIO = 1000
MAX_TOTAL_UNCOMPRESSED_SIZE_MB = 1000  # 1 GB
MAX_NUM_FILES = 1000
MAX_NESTED_ZIP_DEPTH = 3

def analyze_zip(zip_path, output_box):
    try:
        with zipfile.ZipFile(zip_path, 'r') as zipf:
            total_uncompressed_size = 0
            nested_zip_count = 0
            suspicious_files = []
            max_depth = 0

            output = [f"📦 ניתוח הקובץ: {os.path.basename(zip_path)}"]

            for info in zipf.infolist():
                if info.compress_size == 0:
                    ratio = float('inf')
                else:
                    ratio = info.file_size / info.compress_size

                total_uncompressed_size += info.file_size
                depth = info.filename.count(os.sep)
                max_depth = max(max_depth, depth)

                if info.filename.endswith('.zip'):
                    nested_zip_count += 1

                if ratio > MAX_COMPRESSION_RATIO:
                    suspicious_files.append((info.filename, ratio))

            output.append(f"- מספר קבצים: {len(zipf.infolist())}")
            output.append(f"- גודל כולל (לא דחוס): {total_uncompressed_size / (1024*1024):.2f} MB")
            output.append(f"- קובצי ZIP בתוך ZIP: {nested_zip_count}")
            output.append(f"- עומק תיקיות מקסימלי: {max_depth}")

            if len(zipf.infolist()) > MAX_NUM_FILES:
                output.append("⚠️ יותר מדי קבצים — חשוד")
            if total_uncompressed_size > MAX_TOTAL_UNCOMPRESSED_SIZE_MB * 1024 * 1024:
                output.append("⚠️ הגודל הלא דחוס גבוה מדי — חשוד")
            if nested_zip_count > 0:
                output.append("⚠️ נמצא קובץ ZIP בתוך ZIP — חשוד")
            if max_depth > MAX_NESTED_ZIP_DEPTH:
                output.append("⚠️ עומק תיקיות חריג — חשוד")

            if suspicious_files:
                output.append("⚠️ קבצים עם יחס דחיסה חריג:")
                for name, ratio in suspicious_files:
                    output.append(f"  - {name} | יחס דחיסה: {ratio:.2f}")
            if not suspicious_files and nested_zip_count == 0:
                output.append("✅ הקובץ לא נראה חשוד")

            output_box.delete(1.0, tk.END)
            output_box.insert(tk.END, "\n".join(output))

    except zipfile.BadZipFile:
        messagebox.showerror("שגיאה", "הקובץ אינו ZIP תקני או פגום.")
    except Exception as e:
        messagebox.showerror("שגיאה כללית", str(e))

def select_file(output_box):
    file_path = filedialog.askopenfilename(filetypes=[("ZIP Files", "*.zip")])
    if file_path:
        analyze_zip(file_path, output_box)

# GUI
def create_gui():
    window = tk.Tk()
    window.title("Zip Bomb Detector")
    window.geometry("600x500")

    title = tk.Label(window, text="🔎 בודק קובצי ZIP חשודים", font=("Arial", 16))
    title.pack(pady=10)

    btn = tk.Button(window, text="בחר קובץ ZIP", command=lambda: select_file(output), font=("Arial", 12))
    btn.pack(pady=10)

    output = scrolledtext.ScrolledText(window, wrap=tk.WORD, width=70, height=25, font=("Courier", 10))
    output.pack(padx=10, pady=10)

    window.mainloop()

create_gui()

