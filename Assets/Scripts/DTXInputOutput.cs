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
    #endregion

    #region Static Methods
    public static DTXInputOutput Load(string filePath)
    {
        DTXInputOutput dtxIO = new DTXInputOutput();
        dtxIO.LoadFile(filePath);
        return dtxIO;
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
    
    public void SetupFileInfo(string absolutePath)
    {
        fileInfo = new FileInformation();
        fileInfo.AbsoluteFilePath = absolutePath;
        fileInfo.AbsoluteFolderPath = System.IO.Path.GetDirectoryName(fileInfo.AbsoluteFilePath);

        FileInfo fi = new System.IO.FileInfo(absolutePath);
        fileInfo.FileSize = fi.Length;
        fileInfo.LastModified = fi.LastWriteTime;
    }

    public string ProcessInputBuffer(string inputBuffer)
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
        string[] textInputArray = textInputBuffer.Split('\n');

        musicInfo = new MusicInfo();

        foreach(string fileLine in textInputArray)
        {
            if (fileLine[0] != CommandPrefix)
            {
                // ignore lines that are not command parameters
                continue;
            }


        }

        return true;
    }
    #endregion
}
