using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.IO;
using System.IO.Compression;
using ICSharpCode.SharpZipLib.Zip; 
 
 public class ZipHelper
 {
    // check if the file is a zip file
    public static bool IsZipFile(string filePath)
    {
        byte[] zipSignature = { 0x50, 0x4B, 0x03, 0x04 };
        using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
        {
            byte[] buffer = new byte[4];
            int bytesRead = fs.Read(buffer, 0, 4);
            if (bytesRead == 4 && buffer.SequenceEqual(zipSignature))
            {
                return true;
            }
        }

        return false;
    }

    // zip single file
    public static bool ZipFile(string zipOutputFilePath, string sourceFilePath, int compressionLevel = 3, string password = null)
    {
        if (!File.Exists(sourceFilePath))
        {
            Console.WriteLine("Source file does not exist!");
            return false;
        }

        try
        {
            using (FileStream fs = File.Create(zipOutputFilePath))
            using (ZipOutputStream zipStream = new ZipOutputStream(fs))
            {
                if (!string.IsNullOrEmpty(password))
                {
                    zipStream.Password = password;
                }

                zipStream.SetLevel(compressionLevel); // Compression level (0-9)

                // Create a new ZIP entry for the file
                string fileName = Path.GetFileName(sourceFilePath);
                ZipEntry entry = new ZipEntry(fileName);
                zipStream.PutNextEntry(entry);

                // Write the file content to the ZIP stream
                using (FileStream fileStream = File.OpenRead(sourceFilePath))
                {
                    byte[] buffer = new byte[4096];
                    int bytesRead;
                    while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        zipStream.Write(buffer, 0, bytesRead);
                    }
                }

                Console.WriteLine("Zipping completed successfully!");
                return true;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Zipping failed: {ex.Message}");
            return false;
        }
    }

    // unzip to target directory
    public static bool UnzipFile(string zipFilePath, string outputDirectory, string password = null)
    {
        if (!File.Exists(zipFilePath))
        {
            Console.WriteLine("Zip file not exist!");
            return false;
        }

        if (!Directory.Exists(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        try
        {
            using (FileStream fs = File.OpenRead(zipFilePath))
            using (ZipInputStream zipStream = new ZipInputStream(fs))
            {
                if (!string.IsNullOrEmpty(password))
                {
                    zipStream.Password = password;
                }

                ZipEntry entry;
                while ((entry = zipStream.GetNextEntry()) != null)
                {
                    if (entry.IsDirectory)
                    {
                        // 如果是目录，创建目录
                        string directoryPath = Path.Combine(outputDirectory, entry.Name);
                        Directory.CreateDirectory(directoryPath);
                        continue;
                    }

                    // 如果是文件，解压文件
                    string filePath = Path.Combine(outputDirectory, entry.Name);
                    string directoryName = Path.GetDirectoryName(filePath);

                    if (!Directory.Exists(directoryName))
                    {
                        Directory.CreateDirectory(directoryName);
                    }

                    using (FileStream outputStream = File.Create(filePath))
                    {
                        byte[] buffer = new byte[4096];
                        int bytesRead;
                        while ((bytesRead = zipStream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            outputStream.Write(buffer, 0, bytesRead);
                        }
                    }
                }
            }

            Console.WriteLine("Unzip complete!");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unzip failed:{ex.Message}");
            return false;
        }
    }

    // unzip to memory
    public static Dictionary<string, byte[]> UnzipToMemory(string zipFilePath)
    {
        var result = new Dictionary<string, byte[]>();

        using (FileStream zipFileStream = File.OpenRead(zipFilePath))
        using (ZipInputStream zipStream = new ZipInputStream(zipFileStream))
        {
            ZipEntry entry;
            while ((entry = zipStream.GetNextEntry()) != null)
            {
                if (!entry.IsDirectory)
                {
                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        byte[] buffer = new byte[4096];
                        int bytesRead;
                        while ((bytesRead = zipStream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            memoryStream.Write(buffer, 0, bytesRead);
                        }

                        memoryStream.Seek(0, SeekOrigin.Begin);
                        result[entry.Name] = memoryStream.ToArray();
                    }
                }
            }
        }

        return result;
    }
 }