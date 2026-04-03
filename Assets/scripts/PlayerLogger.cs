using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.IO;
using System;

public class MyFileLogHandler : ILogHandler
{
    public string LogPath
    {
        get
        {
            return _filePath;
        }
    }

    private FileStream _fileStream;
    private StreamWriter _streamWriter;
    private ILogHandler _defaultLogHandler = Debug.unityLogger.logHandler;
    private string _filePath;

    public MyFileLogHandler()
    {
        _filePath = Path.Combine(Application.persistentDataPath, "log" + DateTime.Now.ToString("HH-mm-ss dd-MM-yyyy") + ".txt");
        _fileStream = new FileStream(_filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
        _streamWriter = new StreamWriter(_fileStream);

        // Replace the default debug log handler
        Debug.unityLogger.logHandler = this;
    }

    public void LogFormat(LogType logType, UnityEngine.Object context, string format, params object[] args)
    {
        _streamWriter.WriteLine(DateTime.Now.ToString("HH-mm-ss") + ": " + String.Format(format, args));
        _streamWriter.Flush();
        _defaultLogHandler.LogFormat(logType, context, format, args);
    }

    public void LogException(Exception exception, UnityEngine.Object context)
    {
        if (exception == null)
            return;
        if (context == null)
            return;

        _defaultLogHandler.LogException(exception, context);
    }

    public void Close()
    {
        _streamWriter.Close();
    }
}

public static class PlayerLogger
{
    public static UnityEvent<string> NotepadLogEvent = new UnityEvent<string>();
    private static ILogger _logger = Debug.unityLogger;
    private static MyFileLogHandler _myFileLogHandler;

    public static void Initialize()
    {
        _myFileLogHandler = new MyFileLogHandler();
        _logger.Log("Log saved at " + _myFileLogHandler.LogPath);
    }

    public static void Shutdown()
    {
        _myFileLogHandler.Close();
    }

    public static void Message(string message)
    {
        _logger.Log(message);
        NotepadLogEvent.Invoke(message);
    }
}