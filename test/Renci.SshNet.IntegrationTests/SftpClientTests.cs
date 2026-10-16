using Renci.SshNet.Common;

namespace Renci.SshNet.IntegrationTests
{
    /// <summary>
    /// The SFTP client integration tests
    /// </summary>
    [TestClass]
    public class SftpClientTests : IntegrationTestBase
    {
        private readonly SftpClient _sftpClient;

        public SftpClientTests()
        {
            _sftpClient = new SftpClient(SshServerHostName, SshServerPort, User.UserName, User.Password);
        }

        [TestInitialize]
        public async Task InitializeAsync()
        {
            await _sftpClient.ConnectAsync(CancellationToken.None).ConfigureAwait(false);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _sftpClient.Disconnect();
            _sftpClient.Dispose();
        }

        [TestMethod]
        public void Create_directory_with_contents_and_list_it()
        {
            var testDirectory = "/home/sshnet/sshnet-test";
            var testFileName = "test-file.txt";
            var testFilePath = $"{testDirectory}/{testFileName}";
            var testContent = "file content";

            // Create new directory and check if it exists
            _sftpClient.CreateDirectory(testDirectory);
            Assert.IsTrue(_sftpClient.Exists(testDirectory));

            // Upload file and check if it exists
            using var fileStream = new MemoryStream(Encoding.UTF8.GetBytes(testContent));
            _sftpClient.UploadFile(fileStream, testFilePath);
            Assert.IsTrue(_sftpClient.Exists(testFilePath));

            // Check if ListDirectory works
            var expectedFiles = new List<(string FullName, bool IsRegularFile, bool IsDirectory)>()
                {
                    ("/home/sshnet/sshnet-test/.", IsRegularFile: false, IsDirectory: true),
                    ("/home/sshnet/sshnet-test/..", IsRegularFile: false, IsDirectory: true),
                    ("/home/sshnet/sshnet-test/test-file.txt", IsRegularFile: true, IsDirectory: false),
                };

            var actualFiles = _sftpClient.ListDirectory(testDirectory)
                .Select(f => (f.FullName, f.IsRegularFile, f.IsDirectory))
                .ToList();

            _sftpClient.DeleteFile(testFilePath);
            _sftpClient.DeleteDirectory(testDirectory);

            CollectionAssert.AreEquivalent(expectedFiles, actualFiles);
        }

        [TestMethod]
        public async Task UploadFileAsync_at_offset_resumes_without_truncating()
        {
            var testFilePath = "/home/sshnet/sshnet-upload-offset.bin";
            var content = new byte[300_000];
            new Random(42).NextBytes(content);
            const int split = 123_457;

            using (var head = new MemoryStream(content, 0, split))
            {
                await _sftpClient.UploadFileAsync(head, testFilePath).ConfigureAwait(false);
            }

            ulong lastReported = 0;
            var progress = new Progress<UploadFileProgressReport>(r => lastReported = r.TotalBytesUploaded);
            using (var tail = new MemoryStream(content, split, content.Length - split))
            {
                await _sftpClient.UploadFileAsync(tail, testFilePath, split, progress).ConfigureAwait(false);
            }

            using var result = new MemoryStream();
            await _sftpClient.DownloadFileAsync(testFilePath, result).ConfigureAwait(false);
            await _sftpClient.DeleteFileAsync(testFilePath, CancellationToken.None).ConfigureAwait(false);

            CollectionAssert.AreEqual(content, result.ToArray());
            Assert.IsTrue(lastReported <= (ulong)content.Length);
        }

        [TestMethod]
        public async Task UploadFileAsync_at_offset_zero_truncates()
        {
            var testFilePath = "/home/sshnet/sshnet-upload-offset-zero.txt";

            using (var longer = new MemoryStream(Encoding.UTF8.GetBytes("a longer original content")))
            {
                await _sftpClient.UploadFileAsync(longer, testFilePath).ConfigureAwait(false);
            }

            using (var shorter = new MemoryStream(Encoding.UTF8.GetBytes("short")))
            {
                await _sftpClient.UploadFileAsync(shorter, testFilePath, 0).ConfigureAwait(false);
            }

            var text = _sftpClient.ReadAllText(testFilePath);
            await _sftpClient.DeleteFileAsync(testFilePath, CancellationToken.None).ConfigureAwait(false);

            Assert.AreEqual("short", text);
        }

        [TestMethod]
        public async Task Create_directory_with_contents_and_list_it_async()
        {
            var testDirectory = "/home/sshnet/sshnet-test";
            var testFileName = "test-file.txt";
            var testFilePath = $"{testDirectory}/{testFileName}";
            var testContent = "file content";

            // Create new directory and check if it exists
            await _sftpClient.CreateDirectoryAsync(testDirectory, CancellationToken.None).ConfigureAwait(false);
            Assert.IsTrue(await _sftpClient.ExistsAsync(testDirectory));

            // Upload file and check if it exists
            using var fileStream = new MemoryStream(Encoding.UTF8.GetBytes(testContent));
            await _sftpClient.UploadFileAsync(fileStream, testFilePath).ConfigureAwait(false);
            Assert.IsTrue(await _sftpClient.ExistsAsync(testFilePath));

            // Check if ListDirectory works
            var expectedFiles = new List<(string FullName, bool IsRegularFile, bool IsDirectory)>()
                {
                    ("/home/sshnet/sshnet-test/.", IsRegularFile: false, IsDirectory: true),
                    ("/home/sshnet/sshnet-test/..", IsRegularFile: false, IsDirectory: true),
                    ("/home/sshnet/sshnet-test/test-file.txt", IsRegularFile: true, IsDirectory: false),
                };

            var actualFiles = new List<(string FullName, bool IsRegularFile, bool IsDirectory)>();

            await foreach (var file in _sftpClient.ListDirectoryAsync(testDirectory, CancellationToken.None))
            {
                actualFiles.Add((file.FullName, file.IsRegularFile, file.IsDirectory));
            }

            await _sftpClient.DeleteFileAsync(testFilePath, CancellationToken.None);
            await _sftpClient.DeleteDirectoryAsync(testDirectory, CancellationToken.None);

            CollectionAssert.AreEquivalent(expectedFiles, actualFiles);
        }

        [TestMethod]
        public void Test_Sftp_ListDirectory_Permission_Denied()
        {
            Assert.ThrowsExactly<SftpPermissionDeniedException>(() => _sftpClient.ListDirectory("/root"));
        }

        [TestMethod]
        public async Task Create_directory_and_delete_it_async()
        {
            var testDirectory = "/home/sshnet/sshnet-test";

            // Create new directory and check if it exists
            await _sftpClient.CreateDirectoryAsync(testDirectory);
            Assert.IsTrue(await _sftpClient.ExistsAsync(testDirectory).ConfigureAwait(false));

            await _sftpClient.DeleteDirectoryAsync(testDirectory, CancellationToken.None).ConfigureAwait(false);

            Assert.IsFalse(await _sftpClient.ExistsAsync(testDirectory).ConfigureAwait(false));
        }

        [TestMethod]
        public async Task Create_directory_with_contents_and_delete_contents_then_directory_async()
        {
            var testDirectory = "/home/sshnet/sshnet-test";
            var testFileName = "test-file.txt";
            var testFilePath = $"{testDirectory}/{testFileName}";
            var testContent = "file content";

            // Create new directory and check if it exists
            await _sftpClient.CreateDirectoryAsync(testDirectory).ConfigureAwait(false);
            Assert.IsTrue(await _sftpClient.ExistsAsync(testDirectory).ConfigureAwait(false));

            // Upload file and check if it exists
            using var fileStream = new MemoryStream(Encoding.UTF8.GetBytes(testContent));
            await _sftpClient.UploadFileAsync(fileStream, testFilePath).ConfigureAwait(false);
            Assert.IsTrue(await _sftpClient.ExistsAsync(testFilePath).ConfigureAwait(false));

            await _sftpClient.DeleteFileAsync(testFilePath, CancellationToken.None).ConfigureAwait(false);

            Assert.IsFalse(await _sftpClient.ExistsAsync(testFilePath).ConfigureAwait(false));
            Assert.IsTrue(await _sftpClient.ExistsAsync(testDirectory).ConfigureAwait(false));

            await _sftpClient.DeleteDirectoryAsync(testDirectory, CancellationToken.None).ConfigureAwait(false);

            Assert.IsFalse(await _sftpClient.ExistsAsync(testDirectory).ConfigureAwait(false));
        }

        [TestMethod]
        public async Task Create_directory_and_delete_it_using_DeleteAsync()
        {
            var testDirectory = "/home/sshnet/sshnet-test";

            // Create new directory and check if it exists
            await _sftpClient.CreateDirectoryAsync(testDirectory).ConfigureAwait(false);
            Assert.IsTrue(await _sftpClient.ExistsAsync(testDirectory).ConfigureAwait(false));

            await _sftpClient.DeleteAsync(testDirectory, CancellationToken.None).ConfigureAwait(false);

            Assert.IsFalse(await _sftpClient.ExistsAsync(testDirectory).ConfigureAwait(false));
        }

        [TestMethod]
        public async Task Create_file_and_delete_using_DeleteAsync()
        {
            var testFileName = "test-file.txt";
            var testContent = "file content";

            // Upload file and check if it exists
            using var fileStream = new MemoryStream(Encoding.UTF8.GetBytes(testContent));
            await _sftpClient.UploadFileAsync(fileStream, testFileName).ConfigureAwait(false);
            Assert.IsTrue(await _sftpClient.ExistsAsync(testFileName).ConfigureAwait(false));

            await _sftpClient.DeleteAsync(testFileName, CancellationToken.None).ConfigureAwait(false);

            Assert.IsFalse(await _sftpClient.ExistsAsync(testFileName).ConfigureAwait(false));
        }

        [TestMethod]
        public void Create_file_and_use_GetAttributes()
        {
            var testFileName = "test-file.txt";
            var testContent = "file content";

            using var fileStream = new MemoryStream(Encoding.UTF8.GetBytes(testContent));
            _sftpClient.UploadFile(fileStream, testFileName);

            var attributes = _sftpClient.GetAttributes(testFileName);
            Assert.IsNotNull(attributes);
            Assert.IsTrue(attributes.IsRegularFile);
        }

        [TestMethod]
        public async Task Create_file_and_use_GetAttributesAsync()
        {
            var testFileName = "test-file.txt";
            var testContent = "file content";

            using var fileStream = new MemoryStream(Encoding.UTF8.GetBytes(testContent));
            await _sftpClient.UploadFileAsync(fileStream, testFileName).ConfigureAwait(false);

            var attributes = await _sftpClient.GetAttributesAsync(testFileName, CancellationToken.None).ConfigureAwait(false);
            Assert.IsNotNull(attributes);
            Assert.IsTrue(attributes.IsRegularFile);
        }
    }
}
