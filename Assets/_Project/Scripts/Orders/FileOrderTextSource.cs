using System;
using System.IO;

namespace MediKitTwin.Orders
{
    /// <summary>
    /// 경로로 지정한 파일을 읽는다. 경로는 호출하는 쪽이 정한다(빌드에서도 읽도록 StreamingAssets 경로를 넘긴다).
    /// </summary>
    public sealed class FileOrderTextSource : IOrderTextSource
    {
        private readonly string _path;

        public FileOrderTextSource(string path)
        {
            _path = path;
        }

        public string Description => _path;

        public bool TryReadAll(out string text)
        {
            text = null;
            try
            {
                if (string.IsNullOrEmpty(_path) || !File.Exists(_path))
                {
                    return false;
                }

                text = File.ReadAllText(_path);
                return true;
            }
            catch (IOException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
        }
    }
}
