using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SslTcpSession.BlockChain
{
   public static class DataEncryptor
   {
      public static bool EncryptFile(string inputFile, string encryptedFileName, string encryptedFileLocation, X509Certificate2 myCertificate)
      {

         // Check if file exist
         if(string.IsNullOrEmpty(inputFile) || !File.Exists(inputFile))
         {
            return false;
         }

         // Create new file location directory if dont exist
         if (!Directory.Exists(encryptedFileLocation))
         {
            Directory.CreateDirectory(encryptedFileLocation);
         }

         string encryptedFileNameWithPath = Path.Combine(encryptedFileLocation, encryptedFileName);

         // Získanie verejného kľúča z certifikátu
         RSA? rsaEncryptor = myCertificate.GetRSAPublicKey();

         // Check if we get public key
         if (rsaEncryptor == null)
         {
            return false;
         }

         using (Aes aes = Aes.Create())
         {
            aes.GenerateKey();
            aes.GenerateIV();

            aes.Padding = PaddingMode.PKCS7;

            // Zašifrovanie kľúča a IV pomocou RSA
            byte[] encryptedAesKey = rsaEncryptor.Encrypt(aes.Key, RSAEncryptionPadding.OaepSHA256);
            byte[] encryptedAesIV = rsaEncryptor.Encrypt(aes.IV, RSAEncryptionPadding.OaepSHA256);

            // Zašifrovanie obsahu súboru pomocou AES
            using (FileStream fileStream = File.OpenRead(inputFile))
            using (FileStream outFileStream = File.Create(encryptedFileNameWithPath))
            using (CryptoStream cryptoStream = new CryptoStream(outFileStream, aes.CreateEncryptor(), CryptoStreamMode.Write))
            {
               fileStream.CopyTo(cryptoStream);
            }

            // Uloženie zašifrovaných kľúča a IV na koniec súboru
            using (FileStream outFileStream = new FileStream(encryptedFileNameWithPath, FileMode.Append))
            {
               outFileStream.Write(encryptedAesKey, 0, encryptedAesKey.Length);
               outFileStream.Write(encryptedAesIV, 0, encryptedAesIV.Length);
            }
         }
         return true;
      }

      public static bool DecryptFile(string encryptedFile, string decryptedFile, X509Certificate2 myCertificate)
      {

         if (!myCertificate.HasPrivateKey)
         {
            return false;
         }

         // Získanie súkromného kľúča z certifikátu
         RSA? rsaDecryptor = myCertificate.GetRSAPrivateKey();

         // Check if we get public key
         if (rsaDecryptor == null)
         {
            return false;
         }

         byte[] encryptedAesKey, encryptedAesIV;
         // Predpokladáme, že zašifrované kľúče sú na konci súboru a ich dĺžka je známa
         using (FileStream inFileStream = File.OpenRead(encryptedFile))
         {
            encryptedAesKey = new byte[rsaDecryptor.KeySize / 8]; // Veľkosť kľúča závisí od veľkosti RSA kľúča
            encryptedAesIV = new byte[rsaDecryptor.KeySize / 8];
            inFileStream.Seek(-2 * encryptedAesKey.Length, SeekOrigin.End);
            inFileStream.Read(encryptedAesKey, 0, encryptedAesKey.Length);
            inFileStream.Read(encryptedAesIV, 0, encryptedAesIV.Length);
         }

         // Dešifrovanie AES kľúča a IV pomocou RSA
         byte[] aesKey = rsaDecryptor.Decrypt(encryptedAesKey, RSAEncryptionPadding.OaepSHA256);
         byte[] aesIV = rsaDecryptor.Decrypt(encryptedAesIV, RSAEncryptionPadding.OaepSHA256);

         // Dešifrovanie obsahu súboru pomocou AES
         using (Aes aes = Aes.Create())
         {
            aes.Key = aesKey;
            aes.IV = aesIV;

            aes.Padding = PaddingMode.PKCS7;

            using (FileStream inFile = File.OpenRead(encryptedFile))
            using (FileStream outFile = File.Create(decryptedFile))
            using (CryptoStream cryptoStream = new CryptoStream(outFile, aes.CreateDecryptor(), CryptoStreamMode.Write))
            {
               // Preskočíme dĺžku zašifrovaných kľúčov na začiatku súboru
               long totalBytesToCopy = inFile.Length - 2 * encryptedAesKey.Length; // Vypočítajte, koľko bajtov treba skutočne skopírovať
               byte[] buffer = new byte[4096]; // Buffer pre čítanie dát
               int bytesRead;
               long totalBytesRead = 0;
               long remainingBytes = totalBytesToCopy;

               while (totalBytesRead < totalBytesToCopy)
               {
                  int toRead = remainingBytes > buffer.Length ? buffer.Length : (int)remainingBytes;
                  bytesRead = inFile.Read(buffer, 0, toRead);
                  if (bytesRead == 0) // Ak už neexistujú žiadne dáta na čítanie
                  {
                     break;
                  }
                  cryptoStream.Write(buffer, 0, bytesRead);
                  totalBytesRead += bytesRead;
                  remainingBytes -= bytesRead;
               }
            }
         }

         return true;
      }
   }
}
