# File Encryption Tool (Educational Project)

This project is a **simple file encryption and decryption tool** built using **C#**, **Avalonia UI**, and **AES cryptography**. It allows you to encrypt and decrypt single files or multiple files, providing basic functionality such as progress reporting, filename handling, and optional deletion of original files.

> **Important:** This application is an **educational project** created for personal learning. It is **not intended for high‑security or production use**.

---

## ✅ Features

* Encrypt a single file or multiple files.
* Optional random encrypted filename or original filename preservation.
* AES‑based encryption using `Aes.Create()` and `CryptoStream`.
* File metadata embedded (original filename).
* Progress bar support using `IProgress<double>`.
* Option to delete original files after encryption/decryption.
* Works on multiple platforms through Avalonia.

---

## ⚠️ Educational Nature of This Project

This project was built to explore and practice:

* File I/O operations in C#
* Symmetric encryption with AES
* Key derivation using `Rfc2898DeriveBytes`
* UI development with Avalonia
* Asynchronous operations and progress reporting

Because of its educational purpose, the project **does not implement all security best practices**. It is not suitable for protecting sensitive or confidential data.

---

## ✅ How It Works

1. The user selects a file or multiple files.
2. A password is entered to generate an AES key.
3. The file is encrypted using an AES encryptor and written to a new file.
4. Metadata such as the original filename is stored inside the encrypted file.
5. During decryption, this metadata is restored.
6. A progress bar shows the encryption or decryption progress.

---

## ✅ What This Project Is Good For

* Learning the basics of AES encryption.
* Understanding how to structure file encryption functions.
* Practicing stream operations in .NET.
* Learning Avalonia UI components and event handling.
* Gaining experience with async background work.

---

## 🚀 Recommendations for Real‑World or Production Use

If you want to evolve this project into a secure production‑ready encryption tool, here are recommended improvements:

### ✅ 1. Use a Real Salt

Currently a static string is used as the salt in key derivation. A real system should:

* Generate a random salt per file
* Store it in the encrypted file header
* Use it when deriving the decryption key

### ✅ 2. Add Authentication (Integrity Checking)

AES‑CBC alone does not guarantee data integrity. For secure designs:

* Use **AES‑GCM** (provides authenticity via tags), **or**
* Add an **HMAC (SHA‑256)** for encrypted content

### ✅ 3. Define a Structured File Format

Instead of relying on raw writing of metadata:

* Create a stable header format
* Include version, salt, IV, file name length, etc.
* Prevent malformed metadata from breaking parsing

### ✅ 4. Improve Error Handling

Handle specific exceptions (e.g., invalid key, corrupted file, unauthorized access) rather than generic catch blocks.

### ✅ 5. Refactor Into Clean Service Layers

Move encryption logic into dedicated services and keep UI thin. This improves testing and maintainability.

### ✅ 6. Add Unit Tests

Cover core encryption, metadata handling, and error scenarios.

---

## 📦 Technologies Used

* **.NET 8 / C#**
* **Avalonia UI** (cross‑platform desktop framework)
* **AES Encryption** (`System.Security.Cryptography`)
* **Async/await**, `Task.Run`, and `IProgress<T>`

---

## ✅ Conclusion

This project is a valuable learning tool and demonstrates many core programming concepts clearly. With further improvements (salt handling, authentication, structured metadata, and cleaner architecture), it can evolve into a more robust encryption system.

Feel free to explore, modify, and learn from it!

