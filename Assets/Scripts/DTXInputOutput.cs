using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System;
using UnityEngine;

public class DTXInputOutput
{
    #region Internal Structs
    public enum OperationType
    {
        Read,
        Write
    }

    public struct FileInformation
    {
        public string AbsoluteFilePath;
        public string AbsoluteFolderPath;
        public DateTime LastModified;
        public long FileSize;
    }

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

    public struct CommandObject
    {
        public string Command;
        public string Value;
    }
    #endregion

    #region Static Methods
    public static DTXInputOutput Load(string filePath)
    {
        DTXInputOutput dtxIO = new DTXInputOutput();
        dtxIO.LoadFile(filePath);
        return dtxIO;
    }

    public static CommandObject BuildCommand(string commandLine)
    {
        CommandObject commandObject = new CommandObject();
        
        List<string> commandArr = new List<string>(commandLine.Substring(1).Split(':'));

        commandObject.Command = commandArr[0];
        commandArr.RemoveAt(0);
        commandObject.Value = string.Concat(commandArr);

        return commandObject;
    }
    #endregion

    #region Constants
    private const char CommandPrefix = '#';
    #endregion

    #region Fields
    private OperationType ioType;
    public MusicInfo musicInfo;
    public FileInformation fileInfo;
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
        fileInfo = new FileInformation();
        fileInfo.AbsoluteFilePath = absolutePath;
        fileInfo.AbsoluteFolderPath = System.IO.Path.GetDirectoryName(fileInfo.AbsoluteFilePath);

        FileInfo fi = new System.IO.FileInfo(absolutePath);
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
            if (IsMusicInfo(commandGroup))
            {
                SetupMusicInfo(commandGroup);
            }
        }

        return true;
    }

    private void SetupMusicInfo(string[] commandGroup)
    {
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
            else if (command.Equals("PREVIEW"))
            {
                musicInfo.PreviewSound = commandObject.Value;
            }
            else if (command.Equals("PREIMAGE"))
            {
                musicInfo.PreviewImage = commandObject.Value;
            }
            else if (command.Equals("PREMOVIE"))
            {
                musicInfo.PreviewMovie = commandObject.Value;
            }
            else if (command.Equals("BACKGROUND"))
            {
                musicInfo.BackgroundImage = commandObject.Value;
            }
            else if (command.Equals("BPM"))
            {
                musicInfo.BPM = Convert.ToDouble(commandObject.Value);
            }
            else if (command.Equals("DLEVEL"))
            {
                musicInfo.Level = commandObject.Value;
            }
        }
    }

    private bool IsValidCommand(string command)
    {
        return command.Length != 0 && command[0] == CommandPrefix && command.Split(':').Length >= 2;
    }

    private bool IsMusicInfo(string[] commandGroup)
    {
        CommandObject commandObject = BuildCommand(commandGroup[0]);
        return IsMusicInfoStart(commandObject);
    }

    private bool IsMusicInfoStart(CommandObject commandObject)
    {
        return commandObject.Command.ToLower().Equals("title");
    }
    #endregion
}
