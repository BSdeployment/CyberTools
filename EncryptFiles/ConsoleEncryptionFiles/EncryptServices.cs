using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

public class EncryptServices
{
    // Encrypt a single file
    public static void EncryptFile(string inputFilePath, string outputFilePath, string key)
    {
        using (Aes aes = Aes.Create())
        {
            var keyBytes = GenerateKeyFromPassword(key, aes.KeySize / 8);
            aes.Key = keyBytes.key;
            aes.IV = keyBytes.iv;

            using (FileStream outputStream = new FileStream(outputFilePath, FileMode.Create))
            {
                outputStream.Write(aes.IV, 0, aes.IV.Length);

                using (CryptoStream cryptoStream = new CryptoStream(outputStream, aes.CreateEncryptor(), CryptoStreamMode.Write))
                using (FileStream inputStream = new FileStream(inputFilePath, FileMode.Open))
                {
                    inputStream.CopyTo(cryptoStream);
                }
            }
        }
    }

    // Decrypt a single file
    public static void DecryptFile(string inputFilePath, string outputFilePath, string key)
    {
        using (Aes aes = Aes.Create())
        {
            using (FileStream inputStream = new FileStream(inputFilePath, FileMode.Open))
            {
                byte[] iv = new byte[aes.BlockSize / 8];
                inputStream.ReadExactly(iv, 0, iv.Length);

                var keyBytes = GenerateKeyFromPassword(key, aes.KeySize / 8);
                aes.Key = keyBytes.key;
                aes.IV = iv;

                using (CryptoStream cryptoStream = new CryptoStream(inputStream, aes.CreateDecryptor(), CryptoStreamMode.Read))
                using (FileStream outputStream = new FileStream(outputFilePath, FileMode.Create))
                {
                    cryptoStream.CopyTo(outputStream);
                }
            }
        }
    }

    // Generate AES key and IV from password
    private static (byte[] key, byte[] iv) GenerateKeyFromPassword(string password, int keySize)
    {
        using (var keyDerivationFunction = new Rfc2898DeriveBytes(password, Encoding.UTF8.GetBytes("SaltValueHere"), 100000, HashAlgorithmName.SHA256))

        {
            byte[] key = keyDerivationFunction.GetBytes(keySize);
            byte[] iv = keyDerivationFunction.GetBytes(16);
            return (key, iv);
        }
    }

    // Encrypt all files in the current directory (no subdirectories)
    public static void EncryptAllFilesInDirectory(string directoryPath, string key)
    {
        var exeFileName = Path.GetFileName(Environment.ProcessPath);
        // Get only files in the current directory (not in subdirectories)
        var files = Directory.GetFiles(directoryPath, "*", SearchOption.TopDirectoryOnly);

        foreach (var file in files)
        {
            if (Path.GetFileName(file).Equals(exeFileName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            string outputFilePath = file + ".aes";
            EncryptFile(file, outputFilePath, key);
            Console.WriteLine($"Encrypted: {file} -> {outputFilePath}");
            File.Delete(file);
        }
    }

    // Decrypt all files in the current directory (no subdirectories)
    public static void DecryptAllFilesInDirectory(string directoryPath, string key)
    {
        // Get only .aes files in the current directory (not in subdirectories)
        var files = Directory.GetFiles(directoryPath, "*.aes", SearchOption.TopDirectoryOnly);

        foreach (var file in files)
        {
            string outputFilePath = file.Substring(0, file.Length - 4); // Remove ".aes"
            DecryptFile(file, outputFilePath, key);
            Console.WriteLine($"Decrypted: {file} -> {outputFilePath}");
            File.Delete(file);
        }
    }

    
       
    
}
