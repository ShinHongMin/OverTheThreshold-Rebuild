using System;
using System.IO;
using System.Text;
using UnityEngine;

public enum SaveReadStatus
{
    Missing, Ready, Invalid, Unavailable
}

/// <summary>노드 저장 파일 하나의 입출력. 임시 파일 기록이 끝난 뒤 기존 파일을 교체한다.</summary>
public sealed class SaveManager
{
    public string FilePath { get; }
    private readonly string _temporaryPath;
    public SaveManager(string filePath)
    {
        FilePath = Path.GetFullPath(filePath);
        _temporaryPath = FilePath + ".tmp";
    }

    public static string DefaultFilePath => Path.Combine(
        Application.isEditor ? Path.GetFullPath(Path.Combine(Application.dataPath, "../UserSettings/RunSave")) : Application.persistentDataPath,
        "run-save.json");

    public SaveReadStatus Read(out RunSaveData data, out string message)
    {
        data = null;
        message = null;
        try
        {
            using (var stream = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                if (stream.Length > 1024 * 1024)
                    throw new ArgumentException("저장 파일이 허용 크기를 초과했습니다.");
                using (var reader = new StreamReader(stream, new UTF8Encoding(false, true)))
                    data = JsonUtility.FromJson<RunSaveData>(reader.ReadToEnd());
            }
            RunSaveRules.Validate(data);
            return SaveReadStatus.Ready;
        }
        catch (FileNotFoundException) { return SaveReadStatus.Missing; }
        catch (DirectoryNotFoundException) { return SaveReadStatus.Missing; }
        catch (Exception error) when (error is ArgumentException || error is DecoderFallbackException)
        {
            data = null;
            message = "저장 기록이 손상되었거나 현재 버전과 맞지 않습니다.";
            return SaveReadStatus.Invalid;
        }
        catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is System.Security.SecurityException)
        {
            data = null;
            message = "저장 기록을 읽을 수 없습니다. 파일 접근 권한을 확인해 주세요.";
            return SaveReadStatus.Unavailable;
        }
    }

    public bool TryWrite(RunSaveData data, out string message)
    {
        message = null;
        try
        {
            RunSaveRules.Validate(data);
            var bytes = new UTF8Encoding(false).GetBytes(JsonUtility.ToJson(data, true));
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
            using (var stream = new FileStream(_temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(true);
            }
            if (File.Exists(FilePath))
                File.Replace(_temporaryPath, FilePath, null);
            else
                File.Move(_temporaryPath, FilePath);
            return true;
        }
        catch (Exception error) when (error is ArgumentException || error is IOException || error is UnauthorizedAccessException || error is System.Security.SecurityException)
        {
            message = "진행 기록을 저장하지 못했습니다.\n디스크 공간과 파일 접근 권한을 확인해 주세요.";
            return false;
        }
    }

    public bool TryClear(out string message)
    {
        message = null;
        try
        {
            File.Delete(FilePath);
            File.Delete(_temporaryPath);
            return true;
        }
        catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is System.Security.SecurityException)
        {
            message = "종료된 런의 저장 기록을 정리하지 못했습니다.\n파일 접근 권한을 확인해 주세요.";
            return false;
        }
    }
}
