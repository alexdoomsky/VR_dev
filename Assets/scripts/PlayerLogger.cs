using UnityEngine;
using UnityEngine.Events;
using System.IO;
using System;

public class MyFileLogHandler : ILogHandler
{
    public string LogPath => _filePath;

    private FileStream _fileStream;
    private StreamWriter _streamWriter;
    private ILogHandler _defaultLogHandler = Debug.unityLogger.logHandler;
    private string _filePath;

    public MyFileLogHandler()
    {
        _filePath = Path.Combine(Application.persistentDataPath, "log_" + DateTime.Now.ToString("HH-mm-ss_dd-MM-yyyy") + ".txt");
        _fileStream = new FileStream(_filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
        _streamWriter = new StreamWriter(_fileStream);

        // Подменяем обработчик логов Unity
        Debug.unityLogger.logHandler = this;
    }

    public void LogFormat(LogType logType, UnityEngine.Object context, string format, params object[] args)
    {
        string formattedMessage = string.Format(format, args);
        string line = DateTime.Now.ToString("HH-mm-ss") + ": " + formattedMessage;

        // Запись в файл
        _streamWriter.WriteLine(line);
        _streamWriter.Flush();

        // Отправка в UI (без повторного вызова логгера, чтобы избежать рекурсии)
        PlayerLogger.NotepadLogEvent?.Invoke(formattedMessage);

        // Передаём в стандартный обработчик (консоль Unity)
        _defaultLogHandler.LogFormat(logType, context, format, args);
    }

    public void LogException(Exception exception, UnityEngine.Object context)
    {
        _defaultLogHandler.LogException(exception, context);
    }

    public void Close()
    {
        _streamWriter?.Close();
        _fileStream?.Close();
    }
}

public static class PlayerLogger
{
    public static UnityEvent<string> NotepadLogEvent = new UnityEvent<string>();
    private static ILogger _logger = Debug.unityLogger;
    private static MyFileLogHandler _myFileLogHandler;

    public static void Initialize()
    {
        if (_myFileLogHandler == null)
        {
            _myFileLogHandler = new MyFileLogHandler();
            _logger.Log("Log file saved at: " + _myFileLogHandler.LogPath);
        }
    }

    public static void Shutdown()
    {
        _myFileLogHandler?.Close();
        _myFileLogHandler = null;
    }

    public static void Message(string message)
    {
        _logger.Log(message); // автоматически попадёт в новый обработчик и в UI
    }
}
