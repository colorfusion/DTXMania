using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System;
using UnityEngine;

public struct CommandObject
{
    public string Command;
    public string Value;
    
    public static CommandObject BuildCommand(string commandLine)
    {
        CommandObject commandObject = new CommandObject();
        
        // strip comments from command if required
        if (commandLine.IndexOf(';') != -1)
        {
            Debug.Log(commandLine);
            commandLine = commandLine.Substring(0, commandLine.IndexOf(';')).Trim();
        }

        List<string> commandArr = new List<string>(commandLine.Substring(1).Split(':'));

        commandObject.Command = commandArr[0];
        commandArr.RemoveAt(0);
        commandObject.Value = string.Concat(commandArr).Trim();

        return commandObject;
    }
}

[System.Serializable]
public class MusicInfo
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

    public static bool IsValidData(string[] commandGroup)
    {
        CommandObject commandObject = CommandObject.BuildCommand(commandGroup[0]);
        return commandObject.Command.ToLower().Equals("title");
    }

    public void Setup(string inputBuffer)
    {
        Setup(inputBuffer.Trim().Split('\n'));
    }

    public void Setup(string[] commandGroup)
    {
        Debug.Log("Loading Music Info");
        foreach(string commandString in commandGroup)
        {
            if (!DTXHelper.IsValidCommand(commandString))
            {
                // ignore lines that are not command parameters
                continue;
            }

            CommandObject commandObject = CommandObject.BuildCommand(commandString);
            string command = commandObject.Command.ToLower();

            if (command.Equals("title"))
            {
                this.Title = commandObject.Value;
            }
            else if (command.Equals("artist"))
            {
                this.ArtistName = commandObject.Value;
            }
            else if (command.Equals("preview"))
            {
                this.PreviewSound = commandObject.Value;
            }
            else if (command.Equals("preimage"))
            {
                this.PreviewImage = commandObject.Value;
            }
            else if (command.Equals("premovie"))
            {
                this.PreviewMovie = commandObject.Value;
            }
            else if (command.Equals("background"))
            {
                this.BackgroundImage = commandObject.Value;
            }
            else if (command.Equals("bpm"))
            {
                this.BPM = Convert.ToDouble(commandObject.Value);
            }
            else if (command.Equals("dlevel"))
            {
                this.Level = commandObject.Value;
            }
        }
    }
}

public class DTXInputOutput : MonoBehaviour
{
    #region Internal Structs
    public enum OperationType
    {
        Read,
        Write
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
    public struct ChipInfo
    {
        public int ChipIndex;
        public string AudioPath;
        public AudioClip AudioClip;
        public AudioSource TargetAudioSource;
        public bool IsChipLoaded;
        public bool IsBGM;
        public float Volume;
        public float Pan;

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
    
    #endregion

    #region Constants
    private const int LaneIndexBPM = 8; // 08

    private const int InvalidSongChipIndex = 0;
    #endregion

    #region Fields
    private OperationType ioType;

    public SoundManager soundManager;
    public MusicInfo musicInfo;
    public FileInformation fileInfo;

    public Dictionary<int, ChipInfo> chipInfoList;
    public Dictionary<int, AudioSource> audioSourceList;

    public Dictionary<int, Lane> laneList;

    public Dictionary<int, int> BPMList;

    public List<Chip> chipList;

    private bool isAutoPlay = false;
    private double startTime;
    public double playbackDelay = 2.0;
    private int currentChipIndex = 0;

    private double preloadTime = 2.0;
    #endregion

    #region Methods
    private string BuildAbsolutePath(string relativePath)
    {
        string songDirectory = Application.streamingAssetsPath + "/Songs";
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

    public bool LoadFile(string relativePath)
    {
        SetupFileInfo(BuildAbsolutePath(relativePath));
        Debug.Log(string.Format("Loading {0}", relativePath));

        string textInputBuffer = DTXHelper.ReadInputFile(fileInfo.AbsoluteFilePath);
        string[] textInputArray = textInputBuffer.Split(new string[]{"\n\n"}, StringSplitOptions.RemoveEmptyEntries);

        musicInfo = new MusicInfo();

        foreach(string fileLine in textInputArray)
        {
            string[] commandGroup = fileLine.Trim().Split('\n');
            CommandObject commandObject = CommandObject.BuildCommand(commandGroup[0]);

            if (MusicInfo.IsValidData(commandGroup))
            {
                musicInfo.Setup(commandGroup);
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

    public bool IsSongReady()
    {
        // check all chip info whether the audio file is loaded successfully
        foreach(ChipInfo chipInfo in chipInfoList.Values)
        {
            if (!chipInfo.IsChipLoaded)
            {
                return false;
            }
        }

        return true;
    }

    public void AutoPlaySong()
    {
        if (soundManager != null)
        {
            Debug.Log("Auto playing song");
            isAutoPlay = true;
            startTime = AudioSettings.dspTime + playbackDelay;
        }
    }

    public void Update()
    {
        if (isAutoPlay)
        {
            double currentTime = AudioSettings.dspTime;
            double timeLapsed = currentTime - startTime;
            // preload audio clips in advance
            timeLapsed += preloadTime; 

            while(currentChipIndex < chipList.Count && chipList[currentChipIndex].Time <= timeLapsed)
            {
                Chip chip = chipList[currentChipIndex];
                ChipInfo chipInfo;
                if (chipInfoList.TryGetValue(chip.ChipIndex, out chipInfo))
                {
                    if (chipInfo.AudioClip != null)
                    {
                        SoundManager.AudioArgs playAudioArgs = new SoundManager.AudioArgs();
                        playAudioArgs.audioClip = chipInfo.AudioClip;
                        playAudioArgs.pan = chipInfo.Pan;
                        playAudioArgs.volume = chipInfo.Volume;
                        playAudioArgs.scheduledTime = startTime + chip.Time;
                        soundManager.PlayAudio(playAudioArgs);
                    }
                    else
                    {
                        Debug.Log("Cannot find audio source to play chip");
                    }
                }

                ++currentChipIndex;
            }
        }
    }

    private void SetupChipInfo(string[] commandGroup)
    {
        Debug.Log("Loading Chip Info");
        chipInfoList = new Dictionary<int, ChipInfo>();
        audioSourceList = new Dictionary<int, AudioSource>();

        ChipInfo currentChip = new ChipInfo();
        int lastChipIndex = -1;
        foreach(string commandString in commandGroup)
        {
            if (!DTXHelper.IsValidCommand(commandString))
            {
                // ignore lines that are not command parameters
                continue;
            }

            CommandObject commandObject = CommandObject.BuildCommand(commandString);
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

                currentChip.Volume = 1.0f;
                currentChip.AudioPath = commandObject.Value;
                currentChip.ChipIndex = lastChipIndex;

                string filePath = GetFileAbsolutePath(commandObject.Value);
                int targetChipIndex = lastChipIndex;
                StartCoroutine(DTXHelper.GetAudioClip(filePath, (audioClip) => {
                    ChipInfo currentChipInfo = chipInfoList[targetChipIndex];
                    audioClip.name = commandObject.Value;
                    currentChipInfo.AudioClip = audioClip;
                    currentChipInfo.IsChipLoaded = true;

                    chipInfoList[targetChipIndex] = currentChipInfo;

                    // Debug.Log(string.Format("Chip loaded into {0}", targetChipIndex));
                }, (errorMsg) => {
                    ChipInfo currentChipInfo = chipInfoList[targetChipIndex];
                    currentChipInfo.IsChipLoaded = true;

                    chipInfoList[targetChipIndex] = currentChipInfo;

                    // Debug.LogError(string.Format("Error loading {0}", filePath));
                }));
            }
            else if (chipCommandPrefix.Equals("pan"))
            {
                currentChip.Pan = (float)Convert.ToInt32(commandObject.Value) / 100.0f;
            }
            else if (chipCommandPrefix.Equals("volume"))
            {
                currentChip.Volume = (float)Convert.ToInt32(commandObject.Value) / 100.0f;
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
            if (!DTXHelper.IsValidCommand(commandString))
            {
                // ignore lines that are not command parameters
                continue;
            }

            CommandObject commandObject = CommandObject.BuildCommand(commandString);
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
        double measureLength = 60d / currentBPM * 4;
        int currentMeasureNumber = 0;
        foreach(string commandString in commandGroup)
        {
            if (!DTXHelper.IsValidCommand(commandString))
            {
                // ignore lines that are not command parameters
                continue;
            }

            CommandObject commandObject = CommandObject.BuildCommand(commandString);
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
                    measureLength = 60d / currentBPM * 4;
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
