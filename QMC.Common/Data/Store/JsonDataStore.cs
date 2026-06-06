using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace QMC.Common.Data.Store
{
    /// <summary>DataContractJsonSerializer 기반 공통 JSON Store입니다.</summary>
    public static class JsonDataStore
    {
        public static DataStoreResult<T> Load<T>(string path) where T : new()
        {
            try
            {
                if (string.IsNullOrEmpty(path))
                    return DataStoreResult<T>.Ok(new T(), string.Empty, true, "Path is empty.");

                if (!File.Exists(path))
                    return DataStoreResult<T>.Ok(new T(), path, true, "File does not exist.");

                using (var fs = File.OpenRead(path))
                {
                    var serializer = new DataContractJsonSerializer(typeof(T), CreateSettings());
                    object value = serializer.ReadObject(fs);
                    return DataStoreResult<T>.Ok(value == null ? new T() : (T)value, path, false);
                }
            }
            catch (Exception ex)
            {
                return DataStoreResult<T>.Fail(path, ex.Message, ex);
            }
            finally
            {
            }
        }

        public static DataStoreResult Save<T>(T data, string path)
        {
            string tempPath = null;

            try
            {
                if ((object)data == null)
                    return DataStoreResult.Fail(path, "Data is null.");

                if (string.IsNullOrEmpty(path))
                    return DataStoreResult.Fail(path, "Path is empty.");

                string dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);

                tempPath = path + ".tmp";
                using (var fs = File.Create(tempPath))
                {
                    JsonPrettySerializer.WriteObject(fs, typeof(T), data, CreateSettings());
                }

                if (File.Exists(path))
                    File.Replace(tempPath, path, null);
                else
                    File.Move(tempPath, path);

                return DataStoreResult.Ok(path);
            }
            catch (Exception ex)
            {
                try
                {
                    if (!string.IsNullOrEmpty(tempPath) && File.Exists(tempPath))
                        File.Delete(tempPath);
                }
                catch
                {
                }

                return DataStoreResult.Fail(path, ex.Message, ex);
            }
            finally
            {
            }
        }

        public static DataStoreResult DeleteDirectory(string path)
        {
            try
            {
                if (string.IsNullOrEmpty(path))
                    return DataStoreResult.Fail(path, "Path is empty.");

                if (!Directory.Exists(path))
                    return DataStoreResult.Ok(path, "Directory does not exist.");

                Directory.Delete(path, true);
                return DataStoreResult.Ok(path);
            }
            catch (Exception ex)
            {
                return DataStoreResult.Fail(path, ex.Message, ex);
            }
            finally
            {
            }
        }

        public static DataStoreResult CopyDirectory(string sourcePath, string targetPath)
        {
            try
            {
                if (string.IsNullOrEmpty(sourcePath) || string.IsNullOrEmpty(targetPath))
                    return DataStoreResult.Fail(targetPath, "Source or target path is empty.");

                if (!Directory.Exists(sourcePath))
                    return DataStoreResult.Fail(sourcePath, "Source directory does not exist.");

                Directory.CreateDirectory(targetPath);
                foreach (string sourceFile in Directory.GetFiles(sourcePath))
                {
                    string fileName = Path.GetFileName(sourceFile);
                    File.Copy(sourceFile, Path.Combine(targetPath, fileName), true);
                }

                return DataStoreResult.Ok(targetPath);
            }
            catch (Exception ex)
            {
                return DataStoreResult.Fail(targetPath, ex.Message, ex);
            }
            finally
            {
            }
        }

        public static DataStoreResult RenameDirectory(string oldPath, string newPath)
        {
            try
            {
                if (string.IsNullOrEmpty(oldPath) || string.IsNullOrEmpty(newPath))
                    return DataStoreResult.Fail(newPath, "Old or new path is empty.");

                if (!Directory.Exists(oldPath))
                    return DataStoreResult.Fail(oldPath, "Source directory does not exist.");

                if (Directory.Exists(newPath))
                    return DataStoreResult.Fail(newPath, "Target directory already exists.");

                Directory.Move(oldPath, newPath);
                return DataStoreResult.Ok(newPath);
            }
            catch (Exception ex)
            {
                return DataStoreResult.Fail(newPath, ex.Message, ex);
            }
            finally
            {
            }
        }

        private static DataContractJsonSerializerSettings CreateSettings()
        {
            try
            {
                return new DataContractJsonSerializerSettings
                {
                    UseSimpleDictionaryFormat = true,
                    EmitTypeInformation = EmitTypeInformation.Never
                };
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }
    }
}
