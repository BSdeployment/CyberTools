# AES File Encryption Utility

This project is a simple, user-friendly utility for encrypting and decrypting files in a directory using the AES (Advanced Encryption Standard) algorithm. It is designed to ensure the confidentiality of files by allowing users to encrypt their sensitive data with a password.

## Features

- **Encrypt files**: Encrypt all files in the current directory using AES with a user-provided password.
- **Decrypt files**: Decrypt AES-encrypted files in the current directory with the same password.
- **Exclude executable**: Automatically excludes the program's executable file (`.exe`) from encryption and decryption.
- **Strong security**:
  - Uses a password-based key derivation function (`PBKDF2`) with SHA-256 for generating cryptographic keys.
  - Supports a high iteration count (default: 100,000) for enhanced resistance to brute-force attacks.
  - Generates a unique Initialization Vector (IV) for every file for added security.
- **Safe processing**: Processes files only in the current directory (does not include subdirectories).
- **Clear logging**: Provides clear feedback on each encrypted or decrypted file.

## Requirements

- **.NET 6 or higher**: This utility requires .NET 6 or later versions to run.

## How to Use

1. Clone or download the repository.
2. Open the solution in Visual Studio or your preferred IDE.
3. Build and run the project.

### Options on Run:
- **Encrypt files**: Enter `1` to encrypt all files in the current directory. The encrypted files will have the `.aes` extension appended to their names.
- **Decrypt files**: Enter `2` to decrypt all `.aes` files in the current directory. Decrypted files will be restored to their original names.

### Example Workflow:
1. Place the executable in the folder containing the files you want to encrypt or decrypt.
2. Run the executable.
3. Follow the prompts to encrypt or decrypt files.

## Security Notes

- The password is not stored anywhere; make sure to remember it! You won't be able to decrypt files without the correct password.
- The utility is designed for personal use to secure files. Use it responsibly and do not encrypt critical system files.

## License

This project is licensed under the [MIT License](LICENSE). Feel free to use, modify, and share it under the terms of the license.

## Contributions

Contributions are welcome! If you have ideas for improvements or find a bug, feel free to open an issue or submit a pull request.

## Disclaimer

This utility is provided "as-is" without any guarantees. Ensure you test it on non-critical files before use. The author is not responsible for any data loss or misuse of the software.

