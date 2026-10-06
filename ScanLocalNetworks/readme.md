<div dir="rtl">

# סורק רשת (WPF)

אפליקציית Windows קלה שמגלה את כל המכשירים המחוברים כעת לרשת המקומית ומציגה כתובת IP, כתובת MAC, יצרן, שם מכשיר ועוד — בטבלה שכל הטקסט בה ניתן לסימון והעתקה.

[English](README.md)

<!-- להוסיף צילום מסך: ![Screenshot](docs/screenshot.png) -->

## תכונות

- **סריקה בלחיצה אחת** של תת-הרשת (IPv4) של כרטיס הרשת שנבחר
- לכל מכשיר: IP, MAC, יצרן (לפי OUI), שם מכשיר (Reverse DNS), תפקיד (נתב / המחשב הזה), זמן תגובה, הערכת מערכת הפעלה (לפי TTL)
- מזהה גם מכשירים שחוסמים Ping, באמצעות טבלת ה-ARP של Windows
- **סריקה מעמיקה** (שני סבבים וזמן המתנה ארוך) למכשירים רדומים
- **העתקה מכל מקום**: סימון טקסט בתא + `Ctrl+C`, העתקת שורות נבחרות, או העתקת כל הטבלה (מופרדת בטאבים, מודבקת ישר לאקסל)
- **ייצוא ל-CSV** (UTF-8 עם BOM, עברית תקינה באקסל)
- מיון עמודות (ה-IP ממוין מספרית)
- מאגר יצרנים מלא של IEEE שיורד פעם אחת ונשמר מקומית; מאגר מובנה קטן כגיבוי ללא אינטרנט
- ללא הרשאות מנהל וללא חבילות NuGet חיצוניות

## דרישות

- Windows 10 או 11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (נכלל בעומס העבודה **.NET desktop development** של Visual Studio)
- אופציונלי: Visual Studio 2022 גרסה 17.8 ומעלה

> WPF רץ על Windows בלבד. הפרויקט לא ייבנה ולא יפעל ב-Linux או macOS.

## הרצה מקומית

### אפשרות 1 — Visual Studio

```bash
git clone https://github.com/<your-username>/network-scanner-wpf.git
cd network-scanner-wpf
```

1. פתח את `NetworkScanner.sln` ב-Visual Studio 2022.
2. לחץ **F5** (עם דיבאגר) או **Ctrl+F5** (בלי).
3. לחץ **סרוק**.

### אפשרות 2 — שורת פקודה

```bash
git clone https://github.com/<your-username>/network-scanner-wpf.git
cd network-scanner-wpf
dotnet run --project NetworkScanner/NetworkScanner.csproj
```

### בניית גרסה להפצה

```bash
dotnet publish NetworkScanner/NetworkScanner.csproj -c Release -r win-x64 --self-contained false -o publish
```

קובץ ההרצה: `publish/NetworkScanner.exe` (דורש .NET 8 Desktop Runtime במחשב היעד). עם `--self-contained true` ה-Runtime נארז בפנים.

## שימוש

| פעולה | איך |
|---|---|
| סריקה | בחר כרטיס רשת ולחץ **סרוק** |
| עצירה | **עצור** |
| העתקת טקסט מתא | סימון בעכבר ← `Ctrl+C` |
| העתקת שורות | בחירת שורות ← `Ctrl+C` או **העתק שורות נבחרות** |
| העתקת הכל | **העתק הכל** |
| ייצוא | **ייצא ל-CSV** |
| עדכון מאגר יצרנים | **עדכן מאגר יצרנים** |

## איך זה עובד

1. **Ping sweep** — עד 128 בקשות ICMP במקביל על פני תת-הרשת.
2. **טבלת ARP** — נקראת דרך `GetIpNetTable` (`iphlpapi.dll`) כדי לתפוס מכשירים שלא עונים ל-Ping.
3. **יצרן** — 3 הבתים הראשונים של ה-MAC מול רשימת OUI של IEEE (נשמרת ב-`%LocalAppData%\NetworkScanner\oui.csv`).
4. **שם מכשיר** — Reverse DNS, בדרך כלל מהנתב.
5. **מערכת הפעלה** — הערכה גסה לפי TTL (עד 64: Linux/Android/iOS/macOS/IoT, עד 128: Windows).

## מבנה הפרויקט

```
NetworkScanner.sln
NetworkScanner/
├─ NetworkScanner.csproj
├─ App.xaml / App.xaml.cs
├─ MainWindow.xaml / .xaml.cs       ממשק והתנהגות
├─ Models/NetworkDevice.cs          מודל מכשיר
└─ Services/
   ├─ NetworkScannerService.cs      Ping, ARP, DNS, כרטיסי רשת
   └─ OuiLookup.cs                  MAC ← יצרן
```

## מגבלות

- IPv4 בלבד; רשתות גדולות מ-/22 נסרקות כ-/24 סביב הכתובת שלך.
- טלפונים עם MAC פרטי (אקראי) יוצגו בלי יצרן — זה צפוי.
- מכשירים במצב חיסכון עמוק עלולים לפספס סריקה רגילה; נסה **סריקה מעמיקה**.
- שמות שמופצים רק ב-mDNS/NetBIOS עשויים להופיע כ-"—".
- זיהוי מערכת ההפעלה הוא היוריסטיקה, לא fingerprint.

## שימוש אחראי

סרוק רק רשתות שבבעלותך או שיש לך הרשאה לסרוק.

## רעיונות להמשך

סריקת פורטים, שמות דרך mDNS/NetBIOS, היסטוריית סריקות עם התראה על מכשיר חדש, סריקות מתוזמנות, Wake-on-LAN.

## רישיון

MIT חופשי לחלוטין
</div>
