using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System;
using UnityEngine;

public class DTXInputOutput : MonoBehaviour
{
    #region Internal Structs
    public enum OperationType
    {
        Read,
        Write
    }

    public struct CommandObject
    {
        public string Command;
        public string Value;
    }

    [System.Serializable]
    public struct FileInformation
    {
        public string AbsoluteFilePath;
        public string AbsoluteFolderPath;
        public DateTime LastModified;
        public long FileSize;
    }

    [System.Serializable]
    public struct MusicInfo
    {
        public string Title;
        public string ArtistName;
        public string Comment;
        public string Genre;
        public string PreviewImage;
        public string PreviewMovie;
        public string PreviewSound;
        public string BackgroundImage;
        public string Level;
        public double BPM;
        public int Duration;
    }

    [System.Serializable]
    public struct ChipInfo
    {
        public AudioClip AudioClip;
        public AudioSource TargetAudioSource;
        public bool IsChipLoaded;
        public bool IsBGM;
        public int Volume;
        public int Pan;

        public bool IsVisible;
    }

    [System.Serializable]
    public struct Lane
    {
        public Color Color;
        public Texture2D Icon;
    }

    [System.Serializable]
    public struct Chip
    {
        public int ChipIndex;
        public double Time;

        public int LaneIndex;
    }
    #endregion

    #region Static Methods
    public static CommandObject BuildCommand(string commandLine)
    {
        CommandObject commandObject = new CommandObject();
        
        List<string> commandArr = new List<string>(commandLine.Substring(1).Split(':'));

        commandObject.Command = commandArr[0];
        commandArr.RemoveAt(0);
        commandObject.Value = string.Concat(commandArr).Trim();

        return commandObject;
    }
    #endregion

    #region Constants
    private const char CommandPrefix = '#';
    private const int LaneIndexBPM = 8; // 08

    private const int InvalidSongChipIndex = 0;
    #endregion

    #region Fields
    private OperationType ioType;
    public MusicInfo musicInfo;
    public FileInformation fileInfo;

    public Dictionary<int, ChipInfo> chipInfoList;
    public Dictionary<int, AudioSource> audioSourceList;

    public Dictionary<int, Lane> laneList;

    public Dictionary<int, int> BPMList;

    public List<Chip> chipList;
    #endregion

    #region Methods
    private string BuildAbsolutePath(string relativePath)
    {
        string songDirectory = Application.dataPath + "/Resources/Songs";
        string filePath = string.Format("{0}/{1}", songDirectory, relativePath);
        return filePath;
    }
    
    private void SetupFileInfo(string absolutePath)
    {
        FileInfo fi = new System.IO.FileInfo(absolutePath);

        fileInfo = new FileInformation();
        fileInfo.AbsoluteFilePath = fi.FullName;
        fileInfo.AbsoluteFolderPath = System.IO.Path.GetDirectoryName(fileInfo.AbsoluteFilePath);

        fileInfo.FileSize = fi.Length;
        fileInfo.LastModified = fi.LastWriteTime;
    }

    private string ProcessInputBuffer(string inputBuffer)
    {
        inputBuffer = inputBuffer.Replace(Environment.NewLine, "\n");
        inputBuffer = inputBuffer.Replace('\t', ' ');

        return inputBuffer;
    }

    public bool LoadFile(string relativePath)
    {
        SetupFileInfo(BuildAbsolutePath(relativePath));
        Debug.Log(fileInfo.AbsoluteFilePath);

        StreamReader inputStream = new StreamReader(fileInfo.AbsoluteFilePath, Encoding.GetEncoding( "shift-jis" ));
        string textInputBuffer = ProcessInputBuffer(inputStream.ReadToEnd());
        inputStream.Close();
        string[] textInputArray = textInputBuffer.Split(new string[]{"\n\n"}, StringSplitOptions.RemoveEmptyEntries);

        musicInfo = new MusicInfo();

        foreach(string fileLine in textInputArray)
        {
            string[] commandGroup = fileLine.Trim().Split('\n');
            CommandObject commandObject = BuildCommand(commandGroup[0]);
            if (IsMusicInfo(commandObject))
            {
                SetupMusicInfo(commandGroup);
            }
            else if (IsChipInfo(commandObject))
            {
                SetupChipInfo(commandGroup);
            }
            else if (IsAVIInfo(commandObject))
            {
                SetupAVIInfo(commandGroup);
            }
            else if (IsBPMInfo(commandObject))
            {
                SetupBPMInfo(commandGroup);
            }
            else if (IsSongChipInfo(commandObject))
            {
                SetupSongChipInfo(commandGroup);
            }
        }

        return true;
    }

    public string GetFileAbsolutePath(string relativePath)
    {
        return string.Format("{0}\\{1}", fileInfo.AbsoluteFolderPath, relativePath);
    }

    private void SetupMusicInfo(string[] commandGroup)
    {
        Debug.Log("Loading Music Info");
        musicInfo = new MusicInfo();
        foreach(string commandString in commandGroup)
        {
            if (!IsValidCommand(commandString))
            {
                // ignore lines that are not command parameters
                continue;
            }

            CommandObject commandObject = BuildCommand(commandString);
            string command = commandObject.Command.ToLower();

            if (command.Equals("title"))
            {
                musicInfo.Title = commandObject.Value;
            }
            else if (command.Equals("artist"))
            {
                musicInfo.ArtistName = commandObject.Value;
            }
            else if (command.Equals("preview"))
            {
                musicInfo.PreviewSound = commandObject.Value;
            }
            else if (command.Equals("preimage"))
            {
                musicInfo.PreviewImage = commandObject.Value;
            }
            else if (command.Equals("premovie"))
            {
                musicInfo.PreviewMovie = commandObject.Value;
            }
            else if (command.Equals("background"))
            {
                musicInfo.BackgroundImage = commandObject.Value;
            }
            else if (command.Equals("bpm"))
            {
                musicInfo.BPM = Convert.ToDouble(commandObject.Value);
            }
            else if (command.Equals("dlevel"))
            {
                musicInfo.Level = commandObject.Value;
            }
        }
    }

    private void SetupChipInfo(string[] commandGroup)
    {
        Debug.Log("Loading Chip Info");
        chipInfoList = new Dictionary<int, ChipInfo>();

        ChipInfo currentChip = new ChipInfo();
        int lastChipIndex = -1;
        foreach(string commandString in commandGroup)
        {
            if (!IsValidCommand(commandString))
            {
                // ignore lines that are not command parameters
                continue;
            }

            CommandObject commandObject = BuildCommand(commandString);
            string chipCommand = commandObject.Command;
            string chipCommandLower = chipCommand.ToLower();

            // special case to handle command without number suffix
            if (chipCommandLower.Equals("bgmwav"))
            {
                currentChip.IsBGM = true;
                continue;
            }

            string chipCommandPrefix = chipCommandLower.Substring(0, chipCommandLower.Length - 2);
            if (chipCommandPrefix.Equals("wav"))
            {
                if (lastChipIndex != -1)
                {
                    chipInfoList.Add(lastChipIndex, currentChip);
                }

                lastChipIndex = DTXHelper.Base36ToInt(chipCommand.Substring(chipCommand.Length - 2));
                currentChip = new ChipInfo();

                string filePath = GetFileAbsolutePath(commandObject.Value);
                int targetChipIndex = lastChipIndex;
                StartCoroutine(DTXHelper.GetAudioClip(filePath, (audioClip) => {
                    ChipInfo currentChipInfo = chipInfoList[targetChipIndex];
                    currentChipInfo.AudioClip = audioClip;
                    currentChipInfo.IsChipLoaded = true;

                    chipInfoList[targetChipIndex] = currentChipInfo;
                    Debug.Log(string.Format("Chip loaded into {0}", targetChipIndex));
                }, (errorMsg) => {
                    Debug.LogError(string.Format("Error loading {0}", filePath));
                }));
            }
            else if (chipCommandPrefix.Equals("pan"))
            {
                currentChip.Pan = Convert.ToInt32(commandObject.Value);
            }
            else if (chipCommandPrefix.Equals("volume"))
            {
                currentChip.Volume = Convert.ToInt32(commandObject.Value);
            }
            else
            {
                Debug.LogWarning(string.Format("Unsupported chip command {0}", chipCommand));
                continue;
            }
        }

        if (lastChipIndex != -1)
        {
            // add last chip if it is valid
            chipInfoList.Add(lastChipIndex, currentChip);
        }
    }

    private void SetupAVIInfo(string[] commandGroup)
    {

    }

    private void SetupBPMInfo(string[] commandGroup)
    {
        Debug.Log("Loading BPM info");
        BPMList = new Dictionary<int, int>();

        foreach(string commandString in commandGroup)
        {
            if (!IsValidCommand(commandString))
            {
                // ignore lines that are not command parameters
                continue;
            }

            CommandObject commandObject = BuildCommand(commandString);
            string chipCommand = commandObject.Command;

            if (!chipCommand.Substring(0, 3).Equals("BPM"))
            {
                // ignore command if it not a bpm setting
                continue;
            }

            int bpmIndex = DTXHelper.Base36ToInt(chipCommand.Substring(chipCommand.Length - 2));

            BPMList.Add(bpmIndex, Convert.ToInt32(commandObject.Value));
        }
    }

    private void SetupSongChipInfo(string[] commandGroup)
    {
        Debug.Log("Loading song chip info");
        chipList = new List<Chip>();
        
        int currentBPM = 120;
        double currentTime = 0;
        // assume time signature is 4/4
        double measureLength = 60000.0 / currentBPM * 4;
        int currentMeasureNumber = 0;
        foreach(string commandString in commandGroup)
        {
            if (!IsValidCommand(commandString))
            {
                // ignore lines that are not command parameters
                continue;
            }

            CommandObject commandObject = BuildCommand(commandString);
            string chipCommand = commandObject.Command;
            string commandValue = commandObject.Value;

            int measureNumber = Convert.ToInt32(chipCommand.Substring(0, 3));
            int laneIndex = DTXHelper.Base36ToInt(chipCommand.Substring(chipCommand.Length - 2));

            if (measureNumber != currentMeasureNumber)
            {
                currentTime += measureLength * (measureNumber - currentMeasureNumber);
                currentMeasureNumber = measureNumber;
            }

            if (laneIndex == LaneIndexBPM)
            {
                int bpmIndex = DTXHelper.Base36ToInt(commandObject.Value);
                int bpm;
                if (BPMList.TryGetValue(bpmIndex, out bpm))
                {
                    currentBPM = bpm;
                    measureLength = 60000.0 / currentBPM * 4;
                }
            }
            else
            {
                // song chip lane handling
                int totalBeats = commandObject.Value.Length / 2;
                double beatLength = measureLength / totalBeats;

                for (int beatIndex = 0; beatIndex < totalBeats; ++beatIndex)
                {
                    int chipIndex = DTXHelper.Base36ToInt(commandValue.Substring(beatIndex * 2, 2));

                    if (chipIndex == InvalidSongChipIndex)
                    {
                        // ignore beat if no chip is used
                        continue;
                    }

                    Chip songChip = new Chip();

                    songChip.ChipIndex = chipIndex;
                    songChip.LaneIndex = laneIndex;
                    songChip.Time = currentTime + beatIndex * beatLength;

                    chipList.Add(songChip);
                }
            }
        }
    }

    private bool IsValidCommand(string command)
    {
        return command.Length != 0 && command[0] == CommandPrefix && command.Split(':').Length >= 2;
    }

    private bool IsMusicInfo(CommandObject commandObject)
    {
        return commandObject.Command.ToLower().Equals("title");
    }

    private bool IsChipInfo(CommandObject commandObject)
    {
        return commandObject.Command.Substring(0, commandObject.Command.Length - 2).ToLower().Equals("wav");
    }

    private bool IsAVIInfo(CommandObject commandObject)
    {
        return commandObject.Command.Substring(0, 3).ToLower().Contains("avi");
    }

    private bool IsBPMInfo(CommandObject commandObject)
    {
        return commandObject.Command.Substring(0, 3).ToLower().Contains("bpm");
    }

    private bool IsSongChipInfo(CommandObject commandObject)
    {
        return Regex.IsMatch(commandObject.Command.ToLower(), @"[0-9A-Z]{5}");
    }
    #endregion
}
