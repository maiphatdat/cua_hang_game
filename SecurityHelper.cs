using System;
using System.Text;
using System.Security.Cryptography;
using System.IO;

namespace QuanLyGame_Final
{
    public static class SecurityHelper
    {
        // "Chìa khóa" bí mật. KHÔNG ĐƯỢC QUÊN chuỗi này, nếu đổi là không đọc được dữ liệu cũ.
        private static readonly string Key = "DuyNghia_GameManager_SecretKey_2025";

        public static string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return plainText;

            try
            {
                byte[] iv = new byte[16];
                byte[] array;

                using (Aes aes = Aes.Create())
                {
                    // Tạo key từ chuỗi bí mật (băm ra 32 byte cho chuẩn AES-256)
                    using (SHA256 sha256 = SHA256.Create())
                    {
                        aes.Key = sha256.ComputeHash(Encoding.UTF8.GetBytes(Key));
                    }
                    aes.IV = iv;

                    ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        using (CryptoStream cryptoStream = new CryptoStream((Stream)memoryStream, encryptor, CryptoStreamMode.Write))
                        {
                            using (StreamWriter streamWriter = new StreamWriter((Stream)cryptoStream))
                            {
                                streamWriter.Write(plainText);
                            }
                            array = memoryStream.ToArray();
                        }
                    }
                }
                return Convert.ToBase64String(array);
            }
            catch
            {
                return plainText; // Lỗi thì trả về gốc
            }
        }

        public static string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText)) return cipherText;

            try
            {
                byte[] iv = new byte[16];
                byte[] buffer = Convert.FromBase64String(cipherText);

                using (Aes aes = Aes.Create())
                {
                    using (SHA256 sha256 = SHA256.Create())
                    {
                        aes.Key = sha256.ComputeHash(Encoding.UTF8.GetBytes(Key));
                    }
                    aes.IV = iv;

                    ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                    using (MemoryStream memoryStream = new MemoryStream(buffer))
                    {
                        using (CryptoStream cryptoStream = new CryptoStream((Stream)memoryStream, decryptor, CryptoStreamMode.Read))
                        {
                            using (StreamReader streamReader = new StreamReader((Stream)cryptoStream))
                            {
                                return streamReader.ReadToEnd();
                            }
                        }
                    }
                }
            }
            catch
            {
                // Mẹo: Nếu giải mã lỗi (do pass cũ chưa mã hóa), thì trả về pass cũ luôn
                return cipherText;
            }
        }
    }
}