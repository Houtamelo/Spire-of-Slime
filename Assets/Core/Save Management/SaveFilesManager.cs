using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Core.Game_Manager.Scripts;
using Core.Save_Management.SaveObjects;
using Core.Utils.Extensions;
using Core.Utils.Patterns;
using JetBrains.Annotations;
using NetFabric.Hyperlinq;
using UnityEngine;
using UnityEngine.Pool;
using Save = Core.Save_Management.SaveObjects.Save;

namespace Core.Save_Management
{
    public sealed class SaveFilesManager : Singleton<SaveFilesManager>
    {
        private const string SaveFileExtension = ".json";
        private const string SaveFilePattern = "*" + SaveFileExtension;
        private const float SaveFileUpdateInterval = 60f; // in seconds
        private const float BackupFileUpdateInterval = 600f; // in seconds

        public event Action<IReadOnlyCollection<SaveRecord>> OnSaveFilesChanged;

        private readonly SortedSet<SaveRecord> _records = new(new SaveRecordComparer());
        public IReadOnlyCollection<SaveRecord> GetRecords => _records;

        private string _saveDirectoryName;
        private float _saveUpdateCounter;

        private  string _backupDirectoryName;
        private float _backupUpdateCounter;
        private int _backupIndexCounter;

        private bool _savePending;

        protected override void Awake()
        {
            _saveDirectoryName = $"{Application.streamingAssetsPath}/Saves";
            _backupDirectoryName = $"{Application.streamingAssetsPath}/Backups";
            base.Awake();
        }

        private void Start()
        {
            try
            {
                if (Directory.Exists(_saveDirectoryName) == false)
                    Directory.CreateDirectory(_saveDirectoryName);

                if (Directory.Exists(_backupDirectoryName) == false)
                    Directory.CreateDirectory(_backupDirectoryName);
            }
            catch (Exception exception)
            {
                string message = $"Could not locate or create save directory. \nMain path: {_saveDirectoryName}\nBackup path: {_backupDirectoryName}\n {exception}";
                message += "YOU WILL NOT BE ABLE TO SAVE OR LOAD ANYTHING.";
                message += $"\n \nLog: {exception.StackTrace}";
                Debug.LogError(message, context: this);
                StartCoroutine(WarningRoutine());

                IEnumerator WarningRoutine()
                {
                    while (GameManager.Instance.IsNone)
                        yield return null;
                    
                    GameManager.Instance.Value.ShowUrgentWarning(message);
                }
                
                
                return;
            }
            
            ParseRecordsFromDisk();
        }
        private void Update()
        {
            if (Save.Current == null)
                return;
            
            _saveUpdateCounter += Time.deltaTime;
            if (_saveUpdateCounter >= SaveFileUpdateInterval || _savePending)
            {
                _saveUpdateCounter = 0f;
                WriteCurrentSessionToDisk(log: true);
            }
            
            _backupUpdateCounter += Time.deltaTime;
            if (_backupUpdateCounter >= BackupFileUpdateInterval)
            {
                _backupUpdateCounter = 0f;
                BackupCurrentSession(log: true);
            }
        }

        public void AddRecord([NotNull] SaveRecord record)
        {
            using Lease<SaveRecord> lease = _records.AsValueEnumerable().ToArray(ArrayPool<SaveRecord>.Shared);
            foreach (SaveRecord element in lease)
            {
                if (element.Name != record.Name)
                    continue;

                if (element.Date < record.Date)
                {
                    _records.Remove(element);
                    _records.Add(record);
                    WriteRecordsToDisk();
                }
                
                return;
            }
            
            _records.Add(record);
        }

        private void ParseRecordsFromDisk()
        {
            _records.Clear();
            try
            {
                string[] savePaths = Directory.GetFiles(path: _saveDirectoryName, searchPattern: SaveFilePattern);
                foreach (string filePath in savePaths)
                {
                    Result<SaveRecord> saveOption = ParseRecord(filePath: filePath);
                    if (saveOption.IsErr)
                    {
                        Debug.LogWarning(saveOption.Reason);
                        continue;
                    }

                    _records.Add(saveOption.Value);
                }

                OnSaveFilesChanged?.Invoke(_records);
            }
            catch (Exception e)
            {
                Debug.LogWarning(e.ToString(), this);
                Debug.LogWarning("Failed to load save files", this);
            }
        }

        public void DeleteSave([NotNull] SaveRecord record)
        {
            string filePath = $"{_saveDirectoryName}/{record.Name}{SaveFileExtension}";
            try
            {
                if (File.Exists(filePath))
                    File.Delete(filePath);
                else
                    Debug.LogWarning($"SaveRecord file on: {filePath} does not exist. Can't delete.");
                
                _records.Remove(record);
                OnSaveFilesChanged?.Invoke(_records);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Couldn't delete save file at path: {filePath}, reason: \n{exception}");
            }
        }

        private static Result<SaveRecord> ParseRecord(string filePath)
        {
            try
            {
                if (File.Exists(filePath) == false)
                    return Result<SaveRecord>.Error($"File does not exist.\n Path: {filePath}");

                string json = File.ReadAllText(path: filePath);
                Utils.Patterns.Option<object> anonymousObject = json.CustomDeserialize(logErrors: true);
                if (anonymousObject.IsNone)
                    return Result<SaveRecord>.Error($"Failed to parse json.\n Path: {filePath} \n Json contents:\n{json}");

                if (anonymousObject.Value is SaveRecord record)
                    return Result<SaveRecord>.Ok(record);

                return Result<SaveRecord>.Error($"Parsed json but the result was an unexpected type, expected: {typeof(SaveRecord)} ; actual: {anonymousObject.Value.GetType()}\n Path: {filePath}\n Json contents:\n{json}");
            }
            catch (Exception e)
            {
                return Result<SaveRecord>.Error(e.Message);
            }
        }

        private void WriteRecordsToDisk()
        {
            foreach (SaveRecord record in _records)
            {
                if (record.IsDirty == false)
                    continue;

                string filePath = $"{_saveDirectoryName}/{record.Name}{SaveFileExtension}";
                Utils.Patterns.Option<string> json = record.CustomSerialize(logErrors: true);
                if (json.IsNone)
                    Debug.LogWarning($"Failed to serialize save file, this should not happen. Save file not written to disk.\n Save: {(record != null ? record.ToString() : "null") }");

                try
                {
                    File.WriteAllText(filePath, json.Value);
                    record.IsDirty = false;
                }
                catch (Exception e)
                {
                    Debug.LogWarning(e.ToString(), this);
                    Debug.LogWarning($"Failed to write save file. \nPath: {filePath}\nSave: {record}", this);
                }
            }
        }

        public bool WriteCurrentSessionToDisk(bool log)
        {
            Save save = Save.Current;
            if (save == null)
            {
                if (log)
                    Debug.LogWarning("Current save is null, this should not be called, current session not saved but the game should still be working.", this);
                
                return false;
            }

            if (save.GetMostRecentRecord().TrySome(out SaveRecord record) == false)
            {
                _savePending = true;
                return false;
            }

            AddRecord(record);
            WriteRecordsToDisk();
            OnSaveFilesChanged?.Invoke(_records);
            _savePending = false;
            return true;
        }

        private void BackupCurrentSession(bool log)
        {
            Save save = Save.Current;
            if (save == null)
            {
                if (log)
                    Debug.LogWarning("Current save is null, this should not be called, backup failed but the game should still be working.", this);
                
                return;
            }
            
            try
            {
                DirectoryInfo backupFolder = new(_backupDirectoryName);
                DirectoryInfo[] folders = backupFolder.GetDirectories();

                DirectoryInfo targetFolder = folders.Length < 10 ? CreateBackupFolder(folders) : FindOldestBackupFolder(folders);

                int count = 0;
                foreach (SaveRecord record in save.RecentRecords)
                {
                    string filePath = $"{targetFolder.FullName}/{record.Name}_{count.ToString("0")}{SaveFileExtension}";
                    Utils.Patterns.Option<string> json = record.CustomSerialize(logErrors: true);
                    if (json.IsNone)
                        continue;

                    count++;
                    File.WriteAllText(filePath, json.Value);
                }
            }
            catch (Exception e)
            {
                if (log)
                {
                    Debug.LogWarning("Failed to backup save", this);
                    Debug.LogWarning(e.ToString(),            this);
                }
            }
        }

        [NotNull]
        private DirectoryInfo CreateBackupFolder([NotNull] DirectoryInfo[] folders)
        {
            for (int i = 0; i < 10; i++)
            {
                string folderName = i.ToString();
                bool found = false;
                foreach (DirectoryInfo folder in folders)
                {
                    if (folder.Name == folderName)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                    return Directory.CreateDirectory($"{_backupDirectoryName}/{folderName}");
            }

            throw new Exception(message: "Impossible");
        }

        private DirectoryInfo FindOldestBackupFolder([NotNull] DirectoryInfo[] folders)
        {
            using PooledObject<List<(DirectoryInfo, FileInfo)>> pool = ListPool<(DirectoryInfo, FileInfo)>.Get(out List<(DirectoryInfo, FileInfo)> saves);

            foreach (DirectoryInfo folder in folders)
            {
                FileInfo[] files = folder.GetFiles(searchPattern: "*" + SaveFileExtension);
                if (files.Length == 0)
                    return folder;
                    
                FileInfo file = files[0];
                saves.Add((folder, file));
            }
                
            (DirectoryInfo, FileInfo) best = saves[index: 0];
            foreach ((DirectoryInfo, FileInfo) tuple in saves)
            {
                if (tuple.Item2.LastWriteTime > best.Item2.LastWriteTime)
                    best = tuple;
            }

            return best.Item1;
        }

        private class SaveRecordComparer : IComparer<SaveRecord>
        {
            public int Compare(SaveRecord x, SaveRecord y)
            {
                if (x == null)
                    return y == null ? 0 : -1;

                if (y == null)
                    return 1;

                if (x.Name == y.Name)
                    return 0;

                int dateComparison = y.Date.CompareTo(value: x.Date);
                return dateComparison == 0 ? 1 : dateComparison;
            }
        }
    }
}