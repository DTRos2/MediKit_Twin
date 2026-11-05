using System;
using System.IO;
using System.Text;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 알려 준 자세 모음을 JSON 파일 하나에 읽고 쓴다. 쓸 때는 임시 파일에 먼저 쓴 뒤 바꿔 끼워, 쓰다 멈춰도 기존 파일이 깨지지 않게 한다.
    /// 어느 경로를 쓸지는 호출하는 쪽이 정한다(기본 위치는 DefaultPickPoseStorage).
    /// </summary>
    public sealed class FilePickPoseStorage : IPickPoseStorage
    {
        public const string FileName = "TaughtPickPoses.json";

        private readonly string _path;

        public FilePickPoseStorage(string path)
        {
            _path = string.IsNullOrWhiteSpace(path) ? throw new ArgumentException("파일 경로가 비어 있다.", nameof(path)) : path;
        }

        /// <summary>자세 파일의 전체 경로. 패널이 저장 위치를 알려 주는 데 쓴다.</summary>
        public string FilePath => _path;

        public string Read()
        {
            return File.Exists(_path) ? File.ReadAllText(_path, Encoding.UTF8) : string.Empty;
        }

        public void Write(string text)
        {
            string directory = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string temp = _path + ".tmp";
            File.WriteAllText(temp, text ?? string.Empty, new UTF8Encoding(false));
            File.Copy(temp, _path, true);
            File.Delete(temp);
        }
    }
}
