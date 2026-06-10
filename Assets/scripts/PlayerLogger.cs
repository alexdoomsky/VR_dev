using UnityEngine;
using UnityEngine.Events;
using System.IO;
using System;
//DEBUG
//GIVEN

// MyFileLogHandler — кастомная реализация интерфейса ILogHandler Unity
// ILogHandler = низкоуровневый интерфейс системы логирования Unity
// Позволяет перехватывать ВСЕ Debug.Log / LogWarning / LogError вызовы
// и переопределять их поведение (например запись в файл)
public class MyFileLogHandler : ILogHandler
{
    // публичное свойство только для чтения
    // возвращает путь к файлу логов
    public string LogPath => _filePath;

    // FileStream — низкоуровневый поток записи в файл (byte-level I/O)
    private FileStream _fileStream;

    // StreamWriter — обёртка над FileStream для записи текста (string → bytes)
    private StreamWriter _streamWriter;

    // ссылка на стандартный Unity log handler
    // Debug.unityLogger.logHandler = встроенная система логирования Unity
    // сохраняем её чтобы НЕ потерять стандартный вывод в консоль
    private ILogHandler _defaultLogHandler = Debug.unityLogger.logHandler;

    // путь к файлу логов
    private string _filePath;

    // конструктор вызывается при создании экземпляра логгера
    public MyFileLogHandler()
    {
        // Application.persistentDataPath
        // платформенно-независимая папка для хранения данных приложения:
        // Windows: AppData
        // Android: persistent app storage
        // iOS: application sandbox
        //
        // Path.Combine — безопасное соединение путей (OS-independent)
        _filePath = Path.Combine(
            Application.persistentDataPath,
            "log_" + DateTime.Now.ToString("HH-mm-ss_dd-MM-yyyy") + ".txt"
        );

        // FileStream:
        // FileMode.OpenOrCreate — открыть файл или создать если не существует
        // FileAccess.ReadWrite — разрешить чтение и запись
        _fileStream = new FileStream(_filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite);

        // StreamWriter — текстовая запись в поток
        _streamWriter = new StreamWriter(_fileStream);

        // ПОДМЕНА системного логгера Unity:
        // теперь ВСЕ Debug.Log / LogWarning / LogError идут через ЭТОТ класс
        Debug.unityLogger.logHandler = this;
    }

    // LogFormat — основной метод интерфейса ILogHandler
    // вызывается Unity при любом Debug.LogFormat / LogWarningFormat / LogErrorFormat
    //
    // параметры:
    // logType  — тип лога (Log, Warning, Error, Exception)
    // context  — Unity Object, к которому привязан лог (например GameObject)
    // format   — строка формата ("value = {0}")
    // args     — массив параметров для подстановки в format
    public void LogFormat(LogType logType, UnityEngine.Object context, string format, params object[] args)
    {
        // string.Format:
        // заменяет {0}, {1}, ... на значения из args
        // пример: "x={0}" + 5 → "x=5"
        string formattedMessage = string.Format(format, args);

        // добавляем timestamp (время логирования)
        string line = DateTime.Now.ToString("HH-mm-ss") + ": " + formattedMessage;

        // запись строки в файл
        _streamWriter.WriteLine(line);

        // Flush — принудительная запись буфера в файл
        // без Flush данные могут оставаться в памяти и не попасть на диск сразу
        _streamWriter.Flush();

        // отправка в UI систему логов
        // PlayerLogger.NotepadLogEvent — UnityEvent<string>
        // используется для UI отображения логов (Observer pattern)
        //
        // ВАЖНО:
        // вызывается formattedMessage (без timestamp), чтобы UI не дублировал время
        PlayerLogger.NotepadLogEvent?.Invoke(formattedMessage);

        // проброс логирования в стандартную систему Unity
        // чтобы консоль Unity продолжала работать как обычно
        _defaultLogHandler.LogFormat(logType, context, format, args);
    }

    // обработка исключений (Exception logging)
    public void LogException(Exception exception, UnityEngine.Object context)
    {
        // просто передаём в стандартный Unity handler
        // Unity сама форматирует stack trace и выводит в консоль
        _defaultLogHandler.LogException(exception, context);
    }

    // закрытие файловых потоков
    // важно: освобождение unmanaged ресурсов (file handle)
    public void Close()
    {
        // ?. — null-safe оператор
        // закрывает StreamWriter если он существует
        _streamWriter?.Close();

        // закрывает FileStream если он существует
        _fileStream?.Close();
    }
}


// PlayerLogger — статический фасад (Facade pattern) над системой логирования
// цель:
// - централизовать логирование
// - подключить UI + файл + Unity console одновременно
public static class PlayerLogger
{
    // UnityEvent<string> — event system Unity (наблюдатель)
    // позволяет подписать UI или другие системы на приход логов
    public static UnityEvent<string> NotepadLogEvent = new UnityEvent<string>();

    // ILogger — интерфейс Unity logging system
    // Debug.unityLogger — глобальный логгер Unity
    private static ILogger _logger = Debug.unityLogger;

    // ссылка на кастомный file logger
    private static MyFileLogHandler _myFileLogHandler;

    // инициализация логгера
    public static void Initialize()
    {
        // защита от повторной инициализации
        if (_myFileLogHandler == null)
        {
            // создаётся новый логгер
            _myFileLogHandler = new MyFileLogHandler();

            // лог о том где сохранён файл
            // попадёт в:
            // 1) файл
            // 2) UI (если подписан)
            // 3) Unity Console
            _logger.Log("Log file saved at: " + _myFileLogHandler.LogPath);
        }
    }

    // завершение работы логгера
    public static void Shutdown()
    {
        // закрытие файловых потоков
        _myFileLogHandler?.Close();

        // удаление ссылки (GC сможет собрать объект)
        _myFileLogHandler = null;
    }

    // основной метод логирования сообщений
    public static void Message(string message)
    {
        // ILogger.Log(string)
        // вызывает LogFormat внутри Unity
        // дальше сообщение идёт через наш MyFileLogHandler
        _logger.Log(message);
    }
}
