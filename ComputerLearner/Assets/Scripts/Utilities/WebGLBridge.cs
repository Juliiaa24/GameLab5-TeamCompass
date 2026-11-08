using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace ComputerLearning
{
    /// <summary>
    /// Provides WebGL browser integration (file downloads via Blob, IndexedDB filesystem sync,
    /// browser shortcut/context-menu interception, and pure-C# in-memory ZIP archive creation).
    /// </summary>
    public static class WebGLBridge
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void WebGL_DownloadFile(string filename, string mimeType, byte[] data, int dataLength);

        [DllImport("__Internal")]
        private static extern void WebGL_SyncFileSystem();

        [DllImport("__Internal")]
        private static extern void WebGL_InstallBrowserInterceptors();
#endif

        public static bool IsWebGLBuild
        {
            get
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                return true;
#else
                return false;
#endif
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                WebGL_InstallBrowserInterceptors();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[WebGLBridge] Failed to install browser interceptors: {ex.Message}");
            }
#endif
        }

        /// <summary>
        /// Triggers a file download in the browser when running in WebGL.
        /// </summary>
        public static bool DownloadFile(string fileName, string mimeType, byte[] data)
        {
            if (string.IsNullOrEmpty(fileName) || data == null || data.Length == 0)
            {
                return false;
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                WebGL_DownloadFile(fileName, string.IsNullOrEmpty(mimeType) ? "application/octet-stream" : mimeType, data, data.Length);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WebGLBridge] DownloadFile failed for '{fileName}': {ex.Message}");
                return false;
            }
#else
            return false;
#endif
        }

        /// <summary>
        /// Flushes PlayerPrefs and synchronizes Emscripten's IDBFS virtual file system to IndexedDB in WebGL.
        /// </summary>
        public static void SyncFileSystem()
        {
            PlayerPrefs.Save();
#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                WebGL_SyncFileSystem();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[WebGLBridge] SyncFileSystem failed: {ex.Message}");
            }
#endif
        }

        /// <summary>
        /// Builds a standard uncompressed (Store, method 0) ZIP archive in memory from a dictionary of relative paths and byte arrays.
        /// Zero external dependencies; works identically in Editor, Standalone, and WebGL (IL2CPP).
        /// </summary>
        public static byte[] CreateZipArchive(IDictionary<string, byte[]> files)
        {
            if (files == null || files.Count == 0)
            {
                return Array.Empty<byte>();
            }

            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(ms, Encoding.UTF8))
            {
                var centralDirectoryEntries = new List<ZipEntryMetadata>(files.Count);
                DateTime now = DateTime.Now;
                ushort dosTime = ToDosTime(now);
                ushort dosDate = ToDosDate(now);

                foreach (KeyValuePair<string, byte[]> kvp in files)
                {
                    if (string.IsNullOrWhiteSpace(kvp.Key)) continue;

                    string normalizedName = kvp.Key.Replace('\\', '/').TrimStart('/');
                    byte[] nameBytes = Encoding.UTF8.GetBytes(normalizedName);
                    byte[] contentBytes = kvp.Value ?? Array.Empty<byte>();
                    uint crc = ComputeCrc32(contentBytes);
                    uint localHeaderOffset = (uint)ms.Position;

                    // Local file header signature (0x04034b50)
                    writer.Write((uint)0x04034b50);
                    writer.Write((ushort)20);              // Version needed to extract (2.0)
                    writer.Write((ushort)0x0800);          // General purpose bit flag (bit 11 = UTF-8)
                    writer.Write((ushort)0);               // Compression method (0 = Store)
                    writer.Write(dosTime);                 // Last mod file time
                    writer.Write(dosDate);                 // Last mod file date
                    writer.Write(crc);                     // CRC-32
                    writer.Write((uint)contentBytes.Length); // Compressed size
                    writer.Write((uint)contentBytes.Length); // Uncompressed size
                    writer.Write((ushort)nameBytes.Length);  // File name length
                    writer.Write((ushort)0);               // Extra field length
                    writer.Write(nameBytes);
                    if (contentBytes.Length > 0)
                    {
                        writer.Write(contentBytes);
                    }

                    centralDirectoryEntries.Add(new ZipEntryMetadata
                    {
                        NameBytes = nameBytes,
                        Crc32 = crc,
                        Size = (uint)contentBytes.Length,
                        LocalHeaderOffset = localHeaderOffset,
                        DosTime = dosTime,
                        DosDate = dosDate
                    });
                }

                uint centralDirOffset = (uint)ms.Position;

                foreach (ZipEntryMetadata entry in centralDirectoryEntries)
                {
                    // Central directory file header signature (0x02014b50)
                    writer.Write((uint)0x02014b50);
                    writer.Write((ushort)20);              // Version made by
                    writer.Write((ushort)20);              // Version needed to extract
                    writer.Write((ushort)0x0800);          // General purpose bit flag (UTF-8)
                    writer.Write((ushort)0);               // Compression method (0 = Store)
                    writer.Write(entry.DosTime);
                    writer.Write(entry.DosDate);
                    writer.Write(entry.Crc32);
                    writer.Write(entry.Size);              // Compressed size
                    writer.Write(entry.Size);              // Uncompressed size
                    writer.Write((ushort)entry.NameBytes.Length);
                    writer.Write((ushort)0);               // Extra field length
                    writer.Write((ushort)0);               // File comment length
                    writer.Write((ushort)0);               // Disk number start
                    writer.Write((ushort)0);               // Internal file attributes
                    writer.Write((uint)0);                 // External file attributes
                    writer.Write(entry.LocalHeaderOffset);
                    writer.Write(entry.NameBytes);
                }

                uint centralDirSize = (uint)ms.Position - centralDirOffset;
                ushort entryCount = (ushort)centralDirectoryEntries.Count;

                // End of central directory record (0x06054b50)
                writer.Write((uint)0x06054b50);
                writer.Write((ushort)0);                   // Number of this disk
                writer.Write((ushort)0);                   // Disk where central directory starts
                writer.Write(entryCount);                  // Number of central directory records on this disk
                writer.Write(entryCount);                  // Total number of central directory records
                writer.Write(centralDirSize);              // Size of central directory
                writer.Write(centralDirOffset);            // Offset of start of central directory
                writer.Write((ushort)0);                   // Comment length

                writer.Flush();
                return ms.ToArray();
            }
        }

        private struct ZipEntryMetadata
        {
            public byte[] NameBytes;
            public uint Crc32;
            public uint Size;
            public uint LocalHeaderOffset;
            public ushort DosTime;
            public ushort DosDate;
        }

        private static ushort ToDosTime(DateTime dt)
        {
            return (ushort)(((dt.Hour & 0x1F) << 11) | ((dt.Minute & 0x3F) << 5) | ((dt.Second / 2) & 0x1F));
        }

        private static ushort ToDosDate(DateTime dt)
        {
            int year = Mathf.Clamp(dt.Year, 1980, 2107);
            return (ushort)((((year - 1980) & 0x7F) << 9) | ((dt.Month & 0x0F) << 5) | (dt.Day & 0x1F));
        }

        private static readonly uint[] Crc32Table = CreateCrc32Table();

        private static uint[] CreateCrc32Table()
        {
            uint[] table = new uint[256];
            for (uint i = 0; i < 256; i++)
            {
                uint c = i;
                for (int k = 0; k < 8; k++)
                {
                    c = (c & 1) != 0 ? (0xEDB88320u ^ (c >> 1)) : (c >> 1);
                }
                table[i] = c;
            }
            return table;
        }

        public static uint ComputeCrc32(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0) return 0u;
            uint crc = 0xFFFFFFFFu;
            for (int i = 0; i < bytes.Length; i++)
            {
                byte index = (byte)((crc ^ bytes[i]) & 0xFF);
                crc = (crc >> 8) ^ Crc32Table[index];
            }
            return ~crc;
        }
    }
}
