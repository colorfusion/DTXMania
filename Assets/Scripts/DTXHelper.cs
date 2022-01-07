using System;
using System.Collections;
using System.Text;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class DTXHelper
{
    public static readonly string strBase16Characters = "0123456789ABCDEFabcdef";
    public static readonly string strBase36Characters = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
    
    public static int Base36ToInt(string strNum)  // n36進数2桁の文字列を数値に変換して返す
    {
        if( strNum.Length < 2 )
            return -1;

        int digit2 = strBase36Characters.IndexOf( strNum[ 0 ] );
        if( digit2 < 0 )
            return -1;

        if( digit2 >= 36 )
            digit2 -= (36 - 10);		// A,B,C... -> 1,2,3...

        int digit1 = strBase36Characters.IndexOf( strNum[ 1 ] );
        if( digit1 < 0 )
            return -1;

        if( digit1 >= 36 )
            digit1 -= (36 - 10);

        return digit2 * 36 + digit1;
    }

    public static IEnumerator GetAudioClip(string audioPath, Action<AudioClip> successCallback, Action<string> failCallback)
    {
        // Debug.Log(string.Format("Loading {0}",audioPath));
        using(UnityWebRequest uwr = UnityWebRequestMultimedia.GetAudioClip(audioPath, AudioType.OGGVORBIS))
        {
            yield return uwr.SendWebRequest();
            
            if (uwr.result != UnityWebRequest.Result.Success) {
                // Debug.LogError(uwr.error);
                failCallback(uwr.error);
                yield break;
            }

            successCallback(DownloadHandlerAudioClip.GetContent(uwr));
        }
    }

    public static string ReadInputFile(string fullPath, bool processBuffer = true)
    {
        StreamReader inputStream = new StreamReader(fullPath, Encoding.GetEncoding( "shift-jis" ));
        string textInputBuffer = inputStream.ReadToEnd();
        if (processBuffer)
        {
            textInputBuffer = ProcessInputBuffer(textInputBuffer);
        }
        inputStream.Close();

        return textInputBuffer;
    }
    
    public static string ProcessInputBuffer(string inputBuffer)
    {
        inputBuffer = inputBuffer.Replace(Environment.NewLine, "\n");
        inputBuffer = inputBuffer.Replace('\t', ' ');

        return inputBuffer;
    }

    public static bool IsValidCommand(string command)
    {
        const char CommandPrefix = '#';
        return command.Length != 0 && command[0] == CommandPrefix && command.Split(':').Length >= 2;
    }
}
