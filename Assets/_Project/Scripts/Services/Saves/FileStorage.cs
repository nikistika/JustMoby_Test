using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _Project.Scripts.Services.Saves
{
    public sealed class FileStorage : IDataStorage
    {
        private const string UserDataDirectoryName = "UserData";
        private readonly string _dirPath;

        public FileStorage()
        {
            _dirPath = Path.Combine(Application.persistentDataPath, UserDataDirectoryName);
            EnsureDirectoryExists();
        }

        public async UniTask<T> LoadState<T>() where T : class, new()
        {
            var path = GetFilePath<T>();
            if (!File.Exists(path))
                return new T();

            var json = await ReadAllTextAsync(path);
            if (string.IsNullOrWhiteSpace(json))
                return new T();

            var obj = JsonUtility.FromJson<T>(json);
            return obj ?? new T();
        }

        public async UniTask SaveState<T>(T state) where T : class
        {
            EnsureDirectoryExists();

            var path = GetFilePath<T>();
            var tmpPath = path + ".tmp";

            var json = JsonUtility.ToJson(state, prettyPrint: true);
            await WriteAllTextAsync(tmpPath, json);

            if (File.Exists(path))
                File.Delete(path);
            File.Move(tmpPath, path);
        }

        private void EnsureDirectoryExists()
        {
            if (!Directory.Exists(_dirPath))
                Directory.CreateDirectory(_dirPath);
        }

        private static async UniTask<string> ReadAllTextAsync(string path)
        {
            return await File.ReadAllTextAsync(path).AsUniTask();
        }

        private static async UniTask WriteAllTextAsync(string path, string content)
        {
            await File.WriteAllTextAsync(path, content).AsUniTask();
        }

        private string GetFilePath<T>() => Path.Combine(_dirPath, $"{typeof(T).Name}.json");
    }
}