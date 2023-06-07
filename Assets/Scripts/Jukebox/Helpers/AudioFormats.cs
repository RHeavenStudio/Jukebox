using System.IO;
using UnityEngine;

namespace Jukebox
{
    public class AudioFormats
    {
        /// <summary>
        /// reads a file and tries to determine an audio type based on contents
        /// </summary>
        /// <param name="path">path to an audio file</param>
        /// <param name="specificType">if a file type has multiple variants, a specifier will be put here</param>
        /// <returns>Unity AudioType value of the determined audio</returns>
        /// <exception cref="System.IO.FileNotFoundException">path doesn't point to a valid file</exception>
        public static AudioType GetAudioType(string path, out string specificType)
        {
            if (!File.Exists(path)) throw new System.IO.FileNotFoundException("path", $"file does not exist at path {path}");

            AudioType audioType = AudioType.UNKNOWN;
            specificType = "Unknown";
            // determine audio type based on file contents, not extension
            using (FileStream fs = File.OpenRead(path))
            {
                byte[] buffer = new byte[4];
                fs.Read(buffer, 0, 4);
                string head = System.Text.Encoding.UTF8.GetString(buffer);
                string sub;
                switch (head)
                {
                    case "OggS":
                        audioType = AudioType.OGGVORBIS;
                        specificType = "OggVorbis";
                        break;
                    case "RIFF":
                        fs.Read(buffer, 8, 4);
                        sub = System.Text.Encoding.UTF8.GetString(buffer);
                        if (sub == "WAVE")
                        {
                            audioType = AudioType.WAV;
                            specificType = "WAV";
                        }
                        break;
                    case "FORM":
                        fs.Read(buffer, 8, 4);
                        sub = System.Text.Encoding.UTF8.GetString(buffer);
                        if (sub == "AIFF")
                        {
                            audioType = AudioType.AIFF;
                            specificType = "AIFF";
                        }
                        else if (sub == "AIFC")
                        {
                            audioType = AudioType.AIFF;
                            specificType = "AIFC";
                        }
                        break;
                    default:
                        byte[] buffer3 = new byte[3];
                        fs.Read(buffer3, 0, 3);
                        sub = System.Text.Encoding.UTF8.GetString(buffer3);
                        if (sub == "ID3")
                        {
                            audioType = AudioType.MPEG;
                            specificType = "mp3";
                        }
                        else if (buffer3[0] == 0xFF && (buffer3[1] & 0x0A) == 0x0A)
                        {
                            // this condition can literally trip out of chance
                            audioType = AudioType.MPEG;
                            specificType = "mp3";
                        }
                        break;
                }
            }
            return audioType;
        }
    }
}