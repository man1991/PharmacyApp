using System.Text.Json;
using PharmacyApp.Api.Exceptions;

namespace PharmacyApp.Api.Services
{
    /// <summary>
    /// Minimal, generic "repository" that persists a list of <typeparamref name="T"/>
    /// to a JSON file on disk, per the requirement to store data as JSON server-side.
    ///
    /// A <see cref="SemaphoreSlim"/> guards every read/write so concurrent requests
    /// (e.g. two people adding medicines at the same time) don't corrupt the file or
    /// clobber each other's changes - a real risk with a flat-file "database".
    /// </summary>
    public class JsonFileStore<T>
    {
        private readonly string _filePath;
        private readonly SemaphoreSlim _fileLock = new(1, 1);
        private readonly JsonSerializerOptions _serializerOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public JsonFileStore(string filePath)
        {
            _filePath = filePath;
        }

        /// <summary>Reads the full list from disk. Returns an empty list if the file is missing/empty.</summary>
        public async Task<List<T>> ReadAllAsync()
        {
            await _fileLock.WaitAsync();
            try
            {
                return await ReadUnlockedAsync();
            }
            catch (JsonException)
            {
                // Bubble up as-is; the global middleware turns this into a friendly message.
                throw;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                throw new DataStoreException($"Unable to read data file '{Path.GetFileName(_filePath)}'.", ex);
            }
            finally
            {
                _fileLock.Release();
            }
        }

        /// <summary>
        /// Performs a read-modify-write cycle under a single lock so callers can safely
        /// mutate the collection (add/update/remove) without a race condition between
        /// the read and the write.
        /// </summary>
        public async Task<List<T>> ReadModifyWriteAsync(Action<List<T>> mutate)
        {
            await _fileLock.WaitAsync();
            try
            {
                var items = await ReadUnlockedAsync();
                mutate(items);
                await WriteUnlockedAsync(items);
                return items;
            }
            catch (JsonException)
            {
                throw;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                throw new DataStoreException($"Unable to update data file '{Path.GetFileName(_filePath)}'.", ex);
            }
            finally
            {
                _fileLock.Release();
            }
        }

        private async Task<List<T>> ReadUnlockedAsync()
        {
            if (!File.Exists(_filePath))
            {
                return new List<T>();
            }

            await using var stream = File.OpenRead(_filePath);
            if (stream.Length == 0)
            {
                return new List<T>();
            }

            var items = await JsonSerializer.DeserializeAsync<List<T>>(stream, _serializerOptions);
            return items ?? new List<T>();
        }

        private async Task WriteUnlockedAsync(List<T> items)
        {
            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Write to a temp file first, then swap it in. This avoids leaving a
            // half-written / corrupt JSON file behind if the process is interrupted mid-write.
            var tempFilePath = _filePath + ".tmp";
            await using (var stream = File.Create(tempFilePath))
            {
                await JsonSerializer.SerializeAsync(stream, items, _serializerOptions);
            }

            File.Copy(tempFilePath, _filePath, overwrite: true);
            File.Delete(tempFilePath);
        }
    }
}
