import os

def delete_shortcuts(folder_path):
    # לולאה על כל הקבצים והתיקיות בתיקיה
    for item in os.listdir(folder_path):
        item_path = os.path.join(folder_path, item)
        
        # בדוק אם הפריט הוא קיצור דרך
        if os.path.isfile(item_path) and os.path.splitext(item_path)[1].lower() == ".lnk":
            try:
                # נסה למחוק את קיצור הדרך
                os.unlink(item_path)
                print(f"Deleted shortcut: {item_path}")
            except Exception as e:
                print(f"Error deleting shortcut {item_path}: {e}")
        elif os.path.isdir(item_path):
            # אם זו תיקיה, קרא לפונקציה באופן רקורסיבי
            delete_shortcuts(item_path)

# קבל את הנתיב של התיקיה הנוכחית שממנה מורץ הסקריפט
current_directory = os.getcwd()

delete_shortcuts(current_directory)
print("Done!")
