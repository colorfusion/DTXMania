using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public enum DifficultyType
{
    Basic,
    Advanced,
    Extreme,
    Master,
    DTX
}

[Serializable]
public struct DifficultyInfo
{
    public string Name;
    public string Level;
    public string FilePath;
    public DifficultyType Type;
}
    
[Serializable]    
public class SongInfo
{
    public string Name;
    public string Artist;

    public string Path;

    public List<DifficultyInfo> DifficultyList;

    public SongInfo()
    {
        DifficultyList = new List<DifficultyInfo>();
    }
}

public class SongManager : MonoBehaviour
{

    public string songFolderPath;

    private string fullSongFolderPath;

    private string baseAssetsPath = Application.streamingAssetsPath;

    public bool IsLoaded = false;

    public List<SongInfo> songList;

    // Start is called before the first frame update
    void Start()
    {
        songList = new List<SongInfo>();

        DirectoryInfo songDirectoryInfo = new DirectoryInfo(baseAssetsPath + songFolderPath);
        fullSongFolderPath = songDirectoryInfo.FullName;
        ParseSongDirectory(songDirectoryInfo);
        
        IsLoaded = true;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void ParseSongDirectory(DirectoryInfo dirInfo)
    {
        FileInfo[] setFiles = null;
        FileInfo[] dtxFiles = null;
        FileInfo[] files = null;
        DirectoryInfo[] subDirs = null;

        try
        {
            setFiles = dirInfo.GetFiles("SET.def");
            dtxFiles = dirInfo.GetFiles("*.dtx");
            files = dirInfo.GetFiles("*");
        }
        catch (UnauthorizedAccessException e)
        {
            Debug.LogError(e.Message);
        }
        catch (DirectoryNotFoundException e)
        {
            Debug.LogWarning(e.Message);
        }

        if (files != null)
        {
            if (setFiles != null && setFiles.Length != 0)
            {
                foreach (FileInfo fileInfo in setFiles)
                {
                    if (fileInfo.Name.ToLower().Equals("set.def"))
                    {
                        // only process SET.def
                        SongInfo songInfo = new SongInfo();
                        songInfo.Path = dirInfo.FullName.Replace(fullSongFolderPath, "");
                        ProcessSetFile(fileInfo, ref songInfo);
                        songList.Add(songInfo);
                        break;
                    }
                }
            }
            else if (dtxFiles != null && dtxFiles.Length != 0)
            {
                foreach (FileInfo fileInfo in dtxFiles)
                {    
                    SongInfo songInfo = new SongInfo();
                    songInfo.Path = dirInfo.FullName.Replace(fullSongFolderPath, "");

                    MusicInfo musicInfo = new MusicInfo();

                    string textInputBuffer = DTXHelper.ReadInputFile(fileInfo.FullName);
                    string[] textInputArray = textInputBuffer.Split(new string[]{"\n\n"}, StringSplitOptions.RemoveEmptyEntries);

                    foreach(string fileLine in textInputArray)
                    {
                        string[] commandGroup = fileLine.Trim().Split('\n');
                        CommandObject commandObject = CommandObject.BuildCommand(commandGroup[0]);

                        if (MusicInfo.IsValidData(commandGroup))
                        {
                            musicInfo.Setup(commandGroup);
                            break;
                        }
                    }

                    songInfo.Name = musicInfo.Title;
                    songInfo.Artist = musicInfo.ArtistName;

                    DifficultyInfo difficultyInfo = new DifficultyInfo();
                    difficultyInfo.Type = DifficultyType.DTX;
                    difficultyInfo.Level = musicInfo.Level;
                    difficultyInfo.FilePath = fileInfo.Name;
                    songInfo.DifficultyList.Add(difficultyInfo);

                    songList.Add(songInfo);
                }

            }

            subDirs = dirInfo.GetDirectories();

            foreach (DirectoryInfo subDirInfo in subDirs)
            {
                ParseSongDirectory(subDirInfo);
            }
        }
    }

    bool BuildCommand(string input, out Tuple<string, string> command)
    {
        if (input[0] != '#')
        {
            command = new Tuple<string, string>("", "");
            return false;
        }

        int splitIndex = input.IndexOf(' ');
        if (splitIndex == -1)
        {
            command = new Tuple<string, string>("", "");
            return false;
        }

        string commandString = input.Substring(1, splitIndex - 1).ToLower();
        string valueString = input.Substring(splitIndex + 1);

        command = new Tuple<string, string>(commandString, valueString);
        return true;
    }

    void ProcessSetFile(FileInfo fileInfo, ref SongInfo songInfo)
    {
        string inputBuffer = DTXHelper.ReadInputFile(fileInfo.FullName);
        string[] inputGroups = inputBuffer.Split(new string[]{"\n\n"}, StringSplitOptions.RemoveEmptyEntries);

        foreach (string inputText in inputGroups)
        {
            string[] commandGroups = inputText.Split('\n');
            Tuple<string, string> command;
            if (BuildCommand(commandGroups[0], out command))
            {
                if (command.Item1.Contains("title"))
                {
                    songInfo.Name = command.Item2;
                }
                else if (command.Item1.Contains("label"))
                {
                    Tuple<string, string> filePath;
                    if (commandGroups.Length >= 2 && BuildCommand(commandGroups[1], out filePath))
                    {
                        if (filePath.Item1.Contains("file"))
                        {
                            int levelIndex = Convert.ToInt32(command.Item1.Substring(1, 1)) - 1;
                            DifficultyInfo difficultyInfo = new DifficultyInfo();
                            difficultyInfo.Name = command.Item2;
                            difficultyInfo.Type = (DifficultyType)levelIndex;
                            difficultyInfo.FilePath = filePath.Item2;
                            songInfo.DifficultyList.Add(difficultyInfo);
                        }
                    }
                }
            }
        }
    }
}
