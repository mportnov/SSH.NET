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

        [TestMethod]
        public void Get_and_delete_symbolic_link_do_not_follow_the_link()
        {
            const string testDirectory = "/home/sshnet/sshnet-symlink-test";
            const string targetPath = testDirectory + "/target.txt";
            const string linkPath = testDirectory + "/link.txt";

            using var sshClient = new SshClient(SshServerHostName, SshServerPort, User.UserName, User.Password);
            sshClient.Connect();
            sshClient.RunCommand($"rm -rf {testDirectory} && mkdir -p {testDirectory} && echo content > {targetPath} && ln -s {targetPath} {linkPath}");

            try
            {
                var link = _sftpClient.Get(linkPath);
                Assert.IsTrue(link.IsSymbolicLink);
                Assert.AreEqual(linkPath, link.FullName);

                _sftpClient.DeleteFile(linkPath);

                Assert.IsFalse(_sftpClient.Exists(linkPath));
                Assert.IsTrue(_sftpClient.Exists(targetPath));
                Assert.IsTrue(_sftpClient.Get(targetPath).IsRegularFile);
            }
            finally
            {
                sshClient.RunCommand($"rm -rf {testDirectory}");
            }
        }

        [TestMethod]
        public async Task Get_and_delete_symbolic_link_do_not_follow_the_link_async()
        {
            const string testDirectory = "/home/sshnet/sshnet-symlink-test-async";
            const string targetPath = testDirectory + "/target.txt";
            const string linkPath = testDirectory + "/link.txt";

            using var sshClient = new SshClient(SshServerHostName, SshServerPort, User.UserName, User.Password);
            await sshClient.ConnectAsync(CancellationToken.None).ConfigureAwait(false);
            sshClient.RunCommand($"rm -rf {testDirectory} && mkdir -p {testDirectory} && echo content > {targetPath} && ln -s {targetPath} {linkPath}");

            try
            {
                var link = await _sftpClient.GetAsync(linkPath, CancellationToken.None).ConfigureAwait(false);
                Assert.IsTrue(link.IsSymbolicLink);
                Assert.AreEqual(linkPath, link.FullName);

                await _sftpClient.DeleteFileAsync(linkPath, CancellationToken.None).ConfigureAwait(false);

                Assert.IsFalse(await _sftpClient.ExistsAsync(linkPath).ConfigureAwait(false));
                Assert.IsTrue(await _sftpClient.ExistsAsync(targetPath).ConfigureAwait(false));
            }
            finally
            {
                sshClient.RunCommand($"rm -rf {testDirectory}");
            }
        }

        [TestMethod]
        public void Canonical_path_of_dot_dot_and_trailing_slash_is_resolved()
        {
            _sftpClient.ChangeDirectory("/home/sshnet");

            Assert.AreEqual("/home", _sftpClient.Get("..").FullName);
            Assert.AreEqual("/home/sshnet", _sftpClient.Get("/home/sshnet/").FullName);
        }

        [TestMethod]
        public void SftpClient_on_SshClient_session_shares_the_connection()
        {
            using var sshClient = new SshClient(SshServerHostName, SshServerPort, User.UserName, User.Password);
            sshClient.Connect();

            using (var sftpClient = new SftpClient(sshClient))
            {
                sftpClient.Connect();
                Assert.IsTrue(sftpClient.IsConnected);
                Assert.IsTrue(sftpClient.Exists("/home/sshnet"));

                // Shell commands and SFTP requests use the same connection.
                Assert.AreEqual("ok", sshClient.RunCommand("echo ok").Result.Trim());
                Assert.IsTrue(sftpClient.ListDirectory("/home/sshnet").Any());

                sftpClient.Disconnect();
                Assert.IsFalse(sftpClient.IsConnected);
                Assert.IsTrue(sshClient.IsConnected);

                // The borrowed session can be used again.
                sftpClient.Connect();
                Assert.IsTrue(sftpClient.Exists("/home/sshnet"));
            }

            // Disposing the SFTP client leaves the connection open.
            Assert.IsTrue(sshClient.IsConnected);
            Assert.AreEqual("still", sshClient.RunCommand("echo still").Result.Trim());
        }

        [TestMethod]
        public async Task SftpClient_on_SshClient_session_connects_async_and_follows_owner_disconnect()
        {
            using var sshClient = new SshClient(SshServerHostName, SshServerPort, User.UserName, User.Password);
            await sshClient.ConnectAsync(CancellationToken.None).ConfigureAwait(false);

            using var sftpClient = new SftpClient(sshClient);
            await sftpClient.ConnectAsync(CancellationToken.None).ConfigureAwait(false);
            Assert.IsTrue(await sftpClient.ExistsAsync("/home/sshnet").ConfigureAwait(false));

            sshClient.Disconnect();
            Assert.IsFalse(sftpClient.IsConnected);
        }

        [TestMethod]
        public void SftpClient_on_SshClient_session_requires_connected_owner()
        {
            using var sshClient = new SshClient(SshServerHostName, SshServerPort, User.UserName, User.Password);
            using var sftpClient = new SftpClient(sshClient);

            Assert.ThrowsExactly<SshConnectionException>(sftpClient.Connect);
        }
    }
}
